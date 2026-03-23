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
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(AppDbContext dbContext, IEmailService emailService, ILogger<InvitationService> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CreateInvitationResponse> CreateInvitationAsync(int actorUserId, int workspaceId, CreateInvitationRequest request)
    {
        var workspace = await ValidateCreatePermissionAsync(actorUserId, workspaceId);
        var inviteeEmail = NormalizeInviteeEmail(request.InviteeEmail);

        await EnsureInviteeNotMemberAsync(workspaceId, inviteeEmail);

        var activeInvitation = await _dbContext.WorkspaceInvitations
            .Where(x => x.WorkspaceId == workspaceId && x.InviteeEmail == inviteeEmail && x.Status == InvitationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeInvitation != null && activeInvitation.ExpiresAt > DateTime.UtcNow)
        {
            throw new AppException(409, "INVITATION_EXISTS", "Lời mời vẫn còn hiệu lực. Hãy dùng chức năng gửi lại khi lời mời đã hết hạn.");
        }

        if (activeInvitation != null && activeInvitation.ExpiresAt <= DateTime.UtcNow)
        {
            activeInvitation.Status = InvitationStatus.Expired;
        }

        var invitation = new WorkspaceInvitation
        {
            WorkspaceId = workspaceId,
            InviterId = actorUserId,
            InviteeEmail = inviteeEmail,
            Code = TokenGenerator.GenerateCode(8),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WorkspaceInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync();

        await _emailService.SendWorkspaceInvitationEmailAsync(inviteeEmail, workspace.Name, invitation.Code, invitation.ExpiresAt);
        _logger.LogInformation("INVITATION_CREATED actor={ActorUserId} workspace={WorkspaceId} invitee={InviteeEmail}", actorUserId, workspaceId, inviteeEmail);

        return new CreateInvitationResponse
        {
            Code = invitation.Code,
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task<CreateInvitationResponse> ResendInvitationAsync(int actorUserId, int workspaceId, CreateInvitationRequest request)
    {
        var workspace = await ValidateCreatePermissionAsync(actorUserId, workspaceId);
        var inviteeEmail = NormalizeInviteeEmail(request.InviteeEmail);

        await EnsureInviteeNotMemberAsync(workspaceId, inviteeEmail);

        var invitation = await _dbContext.WorkspaceInvitations
            .Where(x => x.WorkspaceId == workspaceId && x.InviteeEmail == inviteeEmail)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (invitation == null)
        {
            throw new AppException(404, "NOT_FOUND", "Không tìm thấy lời mời để gửi lại.");
        }

        if (invitation.Status == InvitationStatus.Accepted)
        {
            throw new AppException(409, "CONFLICT", "Lời mời đã được chấp nhận, không thể gửi lại.");
        }

        if (invitation.ExpiresAt > DateTime.UtcNow && invitation.Status == InvitationStatus.Pending)
        {
            throw new AppException(409, "INVITATION_STILL_ACTIVE", "Lời mời vẫn còn hiệu lực, chưa cần gửi lại.");
        }

        invitation.Code = TokenGenerator.GenerateCode(8);
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.Status = InvitationStatus.Pending;
        invitation.CreatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _emailService.SendWorkspaceInvitationEmailAsync(inviteeEmail, workspace.Name, invitation.Code, invitation.ExpiresAt);

        _logger.LogInformation("INVITATION_RESENT actor={ActorUserId} workspace={WorkspaceId} invitee={InviteeEmail}", actorUserId, workspaceId, inviteeEmail);

        return new CreateInvitationResponse
        {
            Code = invitation.Code,
            ExpiresAt = invitation.ExpiresAt
        };
    }

    public async Task<AcceptInvitationResponse> AcceptInvitationAsync(int userId, AcceptInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Mã lời mời không được để trống.");
        }

        var invitation = await _dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(x => x.Code == request.Code.Trim());

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

        _logger.LogInformation("INVITATION_ACCEPTED user={UserId} workspace={WorkspaceId} invitee={InviteeEmail}", userId, invitation.WorkspaceId, invitation.InviteeEmail);

        return new AcceptInvitationResponse
        {
            WorkspaceId = invitation.WorkspaceId,
            Role = "member"
        };
    }

    public async Task RejectInvitationAsync(int userId, RejectInvitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Mã lời mời không được để trống.");
        }

        var invitation = await _dbContext.WorkspaceInvitations
            .FirstOrDefaultAsync(x => x.Code == request.Code.Trim());

        if (invitation == null)
        {
            throw new AppException(404, "NOT_FOUND", "Lời mời không tồn tại.");
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

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _dbContext.SaveChangesAsync();
            throw new AppException(400, "INVALID_INVITATION", "Lời mời đã hết hạn.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new AppException(409, "CONFLICT", "Lời mời đã được xử lý trước đó.");
        }

        invitation.Status = InvitationStatus.Rejected;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("INVITATION_REJECTED user={UserId} workspace={WorkspaceId} invitee={InviteeEmail}", userId, invitation.WorkspaceId, invitation.InviteeEmail);
    }

    private async Task<Workspace> ValidateCreatePermissionAsync(int actorUserId, int workspaceId)
    {
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

        return workspace;
    }

    private static string NormalizeInviteeEmail(string inviteeEmail)
    {
        if (string.IsNullOrWhiteSpace(inviteeEmail))
        {
            throw new AppException(400, "VALIDATION_ERROR", "Email người được mời không được để trống.");
        }

        return inviteeEmail.Trim().ToLowerInvariant();
    }

    private async Task EnsureInviteeNotMemberAsync(int workspaceId, string inviteeEmail)
    {
        var existingUser = await _dbContext.Users.FirstOrDefaultAsync(x => x.Email == inviteeEmail);
        if (existingUser == null)
        {
            return;
        }

        var existingMember = await _dbContext.WorkspaceMembers
            .AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == existingUser.Id);

        if (existingMember)
        {
            throw new AppException(409, "CONFLICT", "Thành viên đã tồn tại trong workspace.");
        }
    }
}