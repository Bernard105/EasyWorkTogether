using WorkspaceStressSystem.Api.DTOs.Workspaces;

namespace WorkspaceStressSystem.Api.Services.Interfaces;

public interface IWorkspaceService
{
    Task<List<WorkspaceResponse>> GetMyWorkspacesAsync(int userId);
    Task<WorkspaceResponse> CreateWorkspaceAsync(int userId, CreateWorkspaceRequest request);
    Task<WorkspaceResponse> UpdateWorkspaceAsync(int userId, int workspaceId, UpdateWorkspaceRequest request);
    Task DeleteWorkspaceAsync(int userId, int workspaceId);
    Task<List<MemberResponse>> GetMembersAsync(int userId, int workspaceId);
    Task UpdateMemberRoleAsync(int actorUserId, int workspaceId, int targetUserId, UpdateMemberRoleRequest request);
    Task RemoveMemberAsync(int actorUserId, int workspaceId, int targetUserId);
}