using WorkspaceStressSystem.Api.DTOs.Invitations;

namespace WorkspaceStressSystem.Api.Services.Interfaces;

public interface IInvitationService
{
    Task<CreateInvitationResponse> CreateInvitationAsync(int actorUserId, int workspaceId, CreateInvitationRequest request);
    Task<CreateInvitationResponse> ResendInvitationAsync(int actorUserId, int workspaceId, CreateInvitationRequest request);
    Task<AcceptInvitationResponse> AcceptInvitationAsync(int userId, AcceptInvitationRequest request);
    Task RejectInvitationAsync(int userId, RejectInvitationRequest request);
}