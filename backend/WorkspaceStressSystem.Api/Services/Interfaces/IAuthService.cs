using WorkspaceStressSystem.Api.DTOs.Auth;
using WorkspaceStressSystem.Api.DTOs.Users;

namespace WorkspaceStressSystem.Api.Services.Interfaces;

public interface IAuthService
{
    Task<UserProfileResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshRequest request);
    Task LogoutAsync(int userId, string accessToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}