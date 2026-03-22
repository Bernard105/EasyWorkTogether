using WorkspaceStressSystem.Api.DTOs.Users;

namespace WorkspaceStressSystem.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserProfileResponse> GetMyProfileAsync(int userId);
    Task<UserProfileResponse> UpdateMyProfileAsync(int userId, UpdateProfileRequest request);
}