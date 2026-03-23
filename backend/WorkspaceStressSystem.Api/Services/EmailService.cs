using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
        var subject = "Khôi phục mật khẩu";
        var body = $@"
Xin chào,

Bạn vừa yêu cầu đặt lại mật khẩu.
Mã khôi phục: {code}
Hết hạn lúc (UTC): {expiresAt:O}

Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.
";

        return SendEmailWithRetryAsync(email, subject, body);
    }

    public Task SendWorkspaceInvitationEmailAsync(string email, string workspaceName, string code, DateTime expiresAt)
    {
        var subject = $"Lời mời tham gia workspace {workspaceName}";
        var body = $@"
Xin chào,

Bạn được mời tham gia workspace: {workspaceName}
Mã lời mời: {code}
Hết hạn lúc (UTC): {expiresAt:O}

Vui lòng dùng mã này để chấp nhận lời mời.
";

        return SendEmailWithRetryAsync(email, subject, body);
    }

    private async Task SendEmailWithRetryAsync(string toEmail, string subject, string textBody)
    {
        var mode = _configuration["Email:Mode"] ?? "Console";
        if (!mode.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            LogConsoleFallback(toEmail, subject, textBody, "Email mode không phải SMTP");
            return;
        }

        var host = _configuration["Email:Smtp:Host"];
        var portRaw = _configuration["Email:Smtp:Port"];
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var fromEmail = _configuration["Email:Smtp:FromEmail"] ?? username;
        var fromName = _configuration["Email:Smtp:FromName"] ?? "Workspace Stress System";
        var useSsl = bool.TryParse(_configuration["Email:Smtp:UseSsl"], out var parsedUseSsl) && parsedUseSsl;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portRaw) ||
            !int.TryParse(portRaw, out var port) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fromEmail))
        {
            LogConsoleFallback(toEmail, subject, textBody, "Thiếu cấu hình SMTP");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = textBody.Trim() };

        Exception? lastException = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var client = new SmtpClient();
                var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

                await client.ConnectAsync(host, port, socketOptions);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("EMAIL_SENT mode=SMTP to={ToEmail} subject={Subject} attempt={Attempt}", toEmail, subject, attempt);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "EMAIL_SEND_FAILED to={ToEmail} subject={Subject} attempt={Attempt}", toEmail, subject, attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
        }

        LogConsoleFallback(toEmail, subject, textBody, $"SMTP thất bại sau 3 lần retry: {lastException?.Message}");
    }

    private void LogConsoleFallback(string toEmail, string subject, string textBody, string reason)
    {
        _logger.LogWarning(
            "EMAIL_FALLBACK_CONSOLE reason={Reason} to={ToEmail} subject={Subject} body={Body}",
            reason,
            toEmail,
            subject,
            textBody);
    }
}