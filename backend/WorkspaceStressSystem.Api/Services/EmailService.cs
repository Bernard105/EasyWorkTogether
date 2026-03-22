using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string code, DateTime expiresAt)
    {
        var mode = _configuration["Email:Mode"] ?? "Console";

        if (mode.Equals("Console", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "PASSWORD RESET EMAIL -> To: {Email}, Code: {Code}, ExpiresAt(UTC): {ExpiresAt}",
                email, code, expiresAt);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "EMAIL MODE '{Mode}' chưa cấu hình SMTP thực tế. Tạm log ra console. To: {Email}, Code: {Code}",
            mode, email, code);

        return Task.CompletedTask;
    }

    public Task SendWorkspaceInvitationEmailAsync(string email, string workspaceName, string code, DateTime expiresAt)
    {
        var mode = _configuration["Email:Mode"] ?? "Console";

        if (mode.Equals("Console", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "WORKSPACE INVITATION EMAIL -> To: {Email}, Workspace: {WorkspaceName}, Code: {Code}, ExpiresAt(UTC): {ExpiresAt}",
                email, workspaceName, code, expiresAt);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "EMAIL MODE '{Mode}' chưa cấu hình SMTP thực tế. Tạm log ra console. To: {Email}, Workspace: {WorkspaceName}, Code: {Code}",
            mode, email, workspaceName, code);

        return Task.CompletedTask;
    }
}