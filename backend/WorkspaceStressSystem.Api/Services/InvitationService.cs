using Microsoft.EntityFrameworkCore;
using WorkspaceStressSystem.Api.Data;
using WorkspaceStressSystem.Api.DTOs.Invitations;
using WorkspaceStressSystem.Api.Helpers;
using WorkspaceStressSystem.Api.Middleware;
using WorkspaceStressSystem.Api.Models.Entities;
using WorkspaceStressSystem.Api.Models.Enums;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Services;

public class InvitationService : IInvitationService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;

    public InvitationService(AppDbContext dbContext, IEmailService emailService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
    }

    public async Task<CreateInvitationResponse> CreateInvitationAsync(int actorUserId, int workspaceId, CreateInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InviteeEmail))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email người được mời không được để trống.");
        }

        var actorMembership = await _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.UserId == actorUserId);

        if (actorMembership == null || (actorMembership.Role != WorkspaceRole.Owner && actorMembership.Role != WorkspaceRole.Admin))
        {
            throw new AppException(403, "FORBIDDEN", "Bạn không có quyền mời thành viên.");
        }

        var workspace = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == workspaceId);
        if (workspace == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy workspace.");
        }

        var inviteeEmail = request.InviteeEmail.Trim().ToLower();

        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == inviteeEmail);
        if (existingUser != null)
        {
            var existingMember = await _dbContext.WorkspaceMembers
                .AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == existingUser.Id);

            if (existingMember)
            {
                throw new AppException(409, "CONFLICT", "Thành viên đã tồn tại trong workspace.");
            }
        }

        var code = TokenGenerator.GenerateCode(8);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            InviterId = actorUserId,
            InviteeEmail = inviteeEmail,
            Code = code,
            ExpiresAt = expiresAt,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkspaceInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        await _emailService.SendWorkspaceInvitationEmailAsync(inviteeEmail, workspace.Name, code, expiresAt);

        return new CreateInvitationResponse
        {
            Code = code,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(int userId, AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Mã lời mời không được để trống.");
        }

        var invitation = await _dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(x => x.Code == request.Code);

        if (invitation == null)
        {
            throw new AppException(404, "NOT_FOUND", "Lời mời không tồn tại.");
        }

        if (invitation.Status != InvitationStatus.Pending || invitation.ExpiresAt < DateTime.UtcNow)
        {
            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _dbContext.SaveChangesAsync();
            }

            throw new AppException(400, "INVALID_INVITATION", "Lời mời không hợp lệ hoặc đã hết hạn.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy người dùng.");
        }

        if (!string.Equals(user.Email, invitation.InviteeEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException(403, "FORBIDDEN", "Lời mời này không thuộc về tài khoản hiện tại.");
        }

        var existed = await _dbContext.WorkspaceMembers
            .AnyAsync(x => x.WorkspaceId == invitation.WorkspaceId && x.UserId == userId);

        if (existed)
        {
            throw new AppException(409, "CONFLICT", "Bạn đã là thành viên của workspace.");
        }

        var member = new WorkspaceMember
        {
            WorkspaceId = invitation.WorkspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.WorkspaceMembers.Add(member);
        invitation.Status = InvitationStatus.Accepted;

        await _dbContext.SaveChangesAsync();

        return new AcceptInvitationResponse
        {
            WorkspaceId = invitation.WorkspaceId,
            Role = "member"
        };
    }
}