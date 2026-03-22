namespace WorkspaceStressSystem.Api.DTOs.Invitations;

public class AcceptInvitationResponse
{
    public int WorkspaceId { get; set; }
    public string Role { get; set; } = null!;
}