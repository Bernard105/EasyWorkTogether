namespace WorkspaceStressSystem.Api.DTOs.Invitations;

public class CreateInvitationResponse
{
    public string Code { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}