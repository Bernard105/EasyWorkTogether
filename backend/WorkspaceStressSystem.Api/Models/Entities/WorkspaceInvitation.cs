using WorkspaceStressSystem.Api.Models.Enums;

namespace WorkspaceStressSystem.Api.Models.Entities;

public class WorkspaceInvitation
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public int InviterId { get; set; }
    public User Inviter { get; set; } = null!;

    public string InviteeEmail { get; set; } = null!;
    public string Code { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}