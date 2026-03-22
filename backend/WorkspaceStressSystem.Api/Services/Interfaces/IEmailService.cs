namespace WorkspaceStressSystem.Api.Services.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string code, DateTime expiresAt);
    Task SendWorkspaceInvitationEmailAsync(string email, string workspaceName, string code, DateTime expiresAt);
}