namespace WorkspaceStressSystem.Api.DTOs.Workspaces;

public class MemberResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime JoinedAt { get; set; }
}