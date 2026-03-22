using WorkspaceStressSystem.Api.Models.Enums;

namespace WorkspaceStressSystem.Api.Models.Entities;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string AccessToken { get; set; } = null!;
    public string RefreshTokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}