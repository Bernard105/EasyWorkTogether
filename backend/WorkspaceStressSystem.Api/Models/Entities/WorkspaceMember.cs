using WorkspaceStressSystem.Api.Models.Enums;

namespace WorkspaceStressSystem.Api.Models.Entities;

public class WorkspaceMember
{
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}