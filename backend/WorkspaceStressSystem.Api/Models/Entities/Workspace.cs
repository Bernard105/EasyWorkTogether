namespace WorkspaceStressSystem.Api.Models.Entities;

public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Config { get; set; }   // JSON string
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public ICollection<WorkspaceInvitation> Invitations { get; set; } = new List<WorkspaceInvitation>();
}