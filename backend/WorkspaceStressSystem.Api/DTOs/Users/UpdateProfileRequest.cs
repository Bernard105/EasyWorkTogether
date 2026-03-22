namespace WorkspaceStressSystem.Api.DTOs.Users;

public class UpdateProfileRequest
{
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
}