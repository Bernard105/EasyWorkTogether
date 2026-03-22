namespace WorkspaceStressSystem.Api.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Workspace> OwnedWorkspaces { get; set; } = new List<Workspace>();
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = new List<WorkspaceMember>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<WorkspaceInvitation> SentInvitations { get; set; } = new List<WorkspaceInvitation>();
}