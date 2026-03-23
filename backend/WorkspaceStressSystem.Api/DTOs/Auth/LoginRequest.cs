namespace WorkspaceStressSystem.Api.DTOs.Auth;

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? CaptchaToken { get; set; }
}