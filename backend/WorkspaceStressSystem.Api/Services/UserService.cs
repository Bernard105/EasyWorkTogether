using Microsoft.EntityFrameworkCore;
using WorkspaceStressSystem.Api.Data;
using WorkspaceStressSystem.Api.DTOs.Users;
using WorkspaceStressSystem.Api.Middleware;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;

    public UserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileResponse> GetMyProfileAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy người dùng.");
        }
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

    public async Task<UserProfileResponse> UpdateMyProfileAsync(int userId, UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Tên không được để trống.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy người dùng.");
        }

        user.Name = request.Name.Trim();
        user.Avatar = request.Avatar;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

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
}