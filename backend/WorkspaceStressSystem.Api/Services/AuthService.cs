using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkspaceStressSystem.Api.Data;
using WorkspaceStressSystem.Api.DTOs.Auth;
using WorkspaceStressSystem.Api.DTOs.Users;
using WorkspaceStressSystem.Api.Helpers;
using WorkspaceStressSystem.Api.Middleware;
using WorkspaceStressSystem.Api.Models.Entities;
using WorkspaceStressSystem.Api.Models.Enums;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Services;

public class AuthService : IAuthService
{
    private const int CaptchaThreshold = 3;
    private const int FailedLoginCacheMinutes = 30;

    private readonly AppDbContext _dbContext;
    private readonly JwtHelper _jwtHelper;
    private readonly IEmailService _emailService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        JwtHelper jwtHelper,
        IEmailService emailService,
        IMemoryCache memoryCache,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtHelper = jwtHelper;
        _emailService = emailService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<UserProfileResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email, mật khẩu và tên là bắt buộc.");
        }

        if (request.Password.Length < 6)
        {
            throw new AppException(400, "VALIDATION_ERROR", "Mật khẩu phải có ít nhất 6 ký tự.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var existed = await _dbContext.Users.AnyAsync(x => x.Email == email);
        if (existed)
        {
            throw new AppException(409, "EMAIL_EXISTS", "Email đã được sử dụng.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            Name = request.Name.Trim(),
            Avatar = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("REGISTER_SUCCESS userId={UserId} email={Email}", user.Id, user.Email);

        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Avatar = user.Avatar,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email và mật khẩu không được để trống.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        ValidateCaptchaIfRequired(email, request.CaptchaToken);

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            var failedCount = IncreaseFailedLoginCount(email);
            _logger.LogWarning("LOGIN_FAILED_UNKNOWN_EMAIL email={Email} failedCount={FailedCount}", email, failedCount);

            if (failedCount >= CaptchaThreshold)
            {
                throw new AppException(403, "CAPTCHA_REQUIRED", "Bạn đã nhập sai quá 3 lần. Vui lòng cung cấp captcha hợp lệ.");
            }

            throw new AppException(401, "UNAUTHORIZED", "Tài khoản không hợp lệ.");
        }

        var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!validPassword)
        {
            var failedCount = IncreaseFailedLoginCount(email);
            _logger.LogWarning(
                "LOGIN_FAILED_BAD_PASSWORD userId={UserId} email={Email} failedCount={FailedCount}",
                user.Id,
                email,
                failedCount);

            if (failedCount >= CaptchaThreshold)
            {
                throw new AppException(403, "CAPTCHA_REQUIRED", "Bạn đã nhập sai quá 3 lần. Vui lòng cung cấp captcha hợp lệ.");
            }

            throw new AppException(401, "UNAUTHORIZED", "Mật khẩu sai.");
        }

        ClearFailedLoginCount(email);
        _logger.LogInformation("LOGIN_SUCCESS userId={UserId} email={Email}", user.Id, email);

        return await CreateSessionAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Refresh token không được để trống.");
        }

        var refreshHash = TokenGenerator.Sha256(request.RefreshToken.Trim());

        var oldSession = await _dbContext.UserSessions
            .FirstOrDefaultAsync(x => x.RefreshTokenHash == refreshHash && x.Status == SessionStatus.Active);

        if (oldSession == null || oldSession.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("REFRESH_FAILED invalidOrExpiredToken");
            throw new AppException(401, "UNAUTHORIZED", "Refresh token hết hạn hoặc không hợp lệ.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == oldSession.UserId);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy người dùng.");
        }

        oldSession.Status = SessionStatus.Revoked;

        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var refreshToken = TokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = TokenGenerator.Sha256(refreshToken);

        var newSession = new UserSession
        {
            UserId = user.Id,
            AccessToken = accessToken,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtHelper.RefreshTokenDays),
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserSessions.Add(newSession);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("REFRESH_ROTATED userId={UserId} oldSessionId={OldSessionId}", user.Id, oldSession.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtHelper.AccessTokenMinutes * 60,
            TokenType = "Bearer"
        };
    }

    public async Task LogoutAsync(int userId, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Access token không hợp lệ.");
        }

        var session = await _dbContext.UserSessions
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.AccessToken == accessToken &&
                x.Status == SessionStatus.Active);

        if (session == null)
        {
            throw new AppException(401, "UNAUTHORIZED", "Phiên đăng nhập không hợp lệ.");
        }

        session.Status = SessionStatus.Revoked;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("LOGOUT_SUCCESS userId={UserId} sessionId={SessionId}", userId, session.Id);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email không được để trống.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Email không tồn tại trong hệ thống.");
        }

        var oldTokens = await _dbContext.PasswordResetTokens
            .Where(x => x.UserId == user.Id && !x.IsUsed)
            .ToListAsync();

        foreach (var token in oldTokens)
        {
            token.IsUsed = true;
        }

        var code = TokenGenerator.GenerateShortCode(8);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, code, expiresAt);
        _logger.LogInformation("PASSWORD_RESET_CODE_SENT userId={UserId} email={Email}", user.Id, user.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Code) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email, mã khôi phục và mật khẩu mới là bắt buộc.");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new AppException(400, "VALIDATION_ERROR", "Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var code = request.Code.Trim().ToUpperInvariant();

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Email không tồn tại trong hệ thống.");
        }

        var resetToken = await _dbContext.PasswordResetTokens
            .Where(x => x.UserId == user.Id && !x.IsUsed && x.Code == code)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (resetToken == null || resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new AppException(400, "INVALID_RESET_CODE", "Mã khôi phục không hợp lệ hoặc đã hết hạn.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
        user.UpdatedAt = DateTime.UtcNow;
        resetToken.IsUsed = true;

        var activeSessions = await _dbContext.UserSessions
            .Where(x => x.UserId == user.Id && x.Status == SessionStatus.Active)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.Status = SessionStatus.Revoked;
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("PASSWORD_RESET_SUCCESS userId={UserId} email={Email}", user.Id, user.Email);
    }

    private async Task<AuthResponse> CreateSessionAsync(User user)
    {
        var accessToken = _jwtHelper.GenerateAccessToken(user);
        var refreshToken = TokenGenerator.GenerateRefreshToken();
        var refreshTokenHash = TokenGenerator.Sha256(refreshToken);

        var session = new UserSession
        {
            UserId = user.Id,
            AccessToken = accessToken,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtHelper.RefreshTokenDays),
            Status = SessionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.UserSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtHelper.AccessTokenMinutes * 60,
            TokenType = "Bearer"
        };
    }

    private void ValidateCaptchaIfRequired(string email, string? captchaToken)
    {
        var failedCount = GetFailedLoginCount(email);
        if (failedCount < CaptchaThreshold)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(captchaToken))
        {
            throw new AppException(403, "CAPTCHA_REQUIRED", "Bạn đã nhập sai quá 3 lần. Vui lòng cung cấp captcha hợp lệ.");
        }

        if (!string.Equals(captchaToken.Trim(), "passed", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(403, "CAPTCHA_REQUIRED", "Captcha không hợp lệ.");
        }
    }

    private int IncreaseFailedLoginCount(string email)
    {
        var cacheKey = GetFailedLoginCacheKey(email);
        var current = GetFailedLoginCount(email);
        var next = current + 1;

        _memoryCache.Set(
            cacheKey,
            next,
            TimeSpan.FromMinutes(FailedLoginCacheMinutes));

        return next;
    }

    private int GetFailedLoginCount(string email)
    {
        var cacheKey = GetFailedLoginCacheKey(email);
        return _memoryCache.TryGetValue<int>(cacheKey, out var count) ? count : 0;
    }

    private void ClearFailedLoginCount(string email)
    {
        var cacheKey = GetFailedLoginCacheKey(email);
        _memoryCache.Remove(cacheKey);
    }

    private static string GetFailedLoginCacheKey(string email)
    {
        return $"failed-login:{email}";
    }
}