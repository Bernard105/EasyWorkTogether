using Microsoft.EntityFrameworkCore;
using WorkspaceStressSystem.Api.Data;
using WorkspaceStressSystem.Api.DTOs.Workspaces;
using WorkspaceStressSystem.Api.Middleware;
using WorkspaceStressSystem.Api.Models.Entities;
using WorkspaceStressSystem.Api.Models.Enums;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly AppDbContext _dbContext;

    public WorkspaceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<WorkspaceResponse>> GetMyWorkspacesAsync(int userId)
    {
        var data = await _dbContext.WorkspaceMembers
            .Include(x => x.Workspace)
            .Where(x => x.UserId == userId)
            .Select(x => new WorkspaceResponse
            {
                Id = x.Workspace.Id,
                Name = x.Workspace.Name,
                Config = x.Workspace.Config,
                OwnerId = x.Workspace.OwnerId,
                Role = x.Role.ToString().ToLower(),
                CreatedAt = x.Workspace.CreatedAt
            })
            .ToListAsync();

        return data;
    }

    public async Task<WorkspaceResponse> CreateWorkspaceAsync(int userId, CreateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Tên workspace không được để trống.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var workspace = new Workspace
            {
                Name = request.Name.Trim(),
                OwnerId = userId,
                Config = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Workspaces.Add(workspace);
            await _dbContext.SaveChangesAsync();

            var ownerMember = new WorkspaceMember
            {
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = WorkspaceRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.WorkspaceMembers.Add(ownerMember);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return new WorkspaceResponse
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Config = workspace.Config,
                OwnerId = workspace.OwnerId,
                Role = "owner",
                CreatedAt = workspace.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw new AppException(500, "INTERNAL_ERROR", "Không thể tạo workspace.");
        }
    }

    public async Task<WorkspaceResponse> UpdateWorkspaceAsync(int userId, int workspaceId, UpdateWorkspaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Tên workspace không được để trống.");
        }

        var membership = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId);

        if (membership == null || (membership.Role != WorkspaceRole.Owner && membership.Role != WorkspaceRole.Admin))
        {
            throw new AppException(403, "FORBIDDEN", "Bạn không có quyền quản trị workspace này.");
        }

        var workspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        if (workspace == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy workspace.");
        }

        workspace.Name = request.Name.Trim();
        workspace.Config = request.Config;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return new WorkspaceResponse
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Config = workspace.Config,
            OwnerId = workspace.OwnerId,
            Role = membership.Role.ToString().ToLower(),
            CreatedAt = workspace.CreatedAt
        };
    }

    public async Task DeleteWorkspaceAsync(int userId, int workspaceId)
    {
        var workspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        if (workspace == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy workspace.");
        }

        if (workspace.OwnerId != userId)
        {
            throw new AppException(403, "FORBIDDEN", "Chỉ owner mới được xóa workspace.");
        }

        _dbContext.Workspaces.Remove(workspace);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<MemberResponse>> GetMembersAsync(int userId, int workspaceId)
    {
        var membership = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId);

        if (membership == null)
        {
            throw new AppException(403, "FORBIDDEN", "Bạn không thuộc workspace này.");
        }

        return await _dbContext.WorkspaceMembers
            .Include(x => x.User)
            .Where(x => x.WorkspaceId == workspaceId)
            .Select(x => new MemberResponse
            {
                UserId = x.UserId,
                Name = x.User.Name,
                Email = x.User.Email,
                Role = x.Role.ToString().ToLower(),
                JoinedAt = x.JoinedAt
            })
            .ToListAsync();
    }

    public async Task UpdateMemberRoleAsync(int actorUserId, int workspaceId, int targetUserId, UpdateMemberRoleRequest request)
    {
        var actor = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == actorUserId);

        if (actor == null || actor.Role != WorkspaceRole.Owner)
        {
            throw new AppException(403, "FORBIDDEN", "Chỉ owner mới được thay đổi vai trò.");
        }

        var target = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == targetUserId);

        if (target == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy thành viên.");
        }

        if (target.Role == WorkspaceRole.Owner)
        {
            throw new AppException(403, "FORBIDDEN", "Không thể thay đổi vai trò của owner.");
        }

        var normalizedRole = request.Role.Trim().ToLower();
        target.Role = normalizedRole switch
        {
            "admin" => WorkspaceRole.Admin,
            "member" => WorkspaceRole.Member,
            _ => throw new AppException(400, "VALIDATION_ERROR", "Role không hợp lệ.")
        };

        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(int actorUserId, int workspaceId, int targetUserId)
    {
        var actor = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == actorUserId);

        if (actor == null || actor.Role != WorkspaceRole.Owner)
        {
            throw new AppException(403, "FORBIDDEN", "Chỉ owner mới được xóa thành viên.");
        }

        var target = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == targetUserId);

        if (target == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy thành viên.");
        }

        if (target.Role == WorkspaceRole.Owner)
        {
            throw new AppException(403, "FORBIDDEN", "Không thể xóa owner.");
        }

        _dbContext.WorkspaceMembers.Remove(target);
        await _dbContext.SaveChangesAsync();
    }
}