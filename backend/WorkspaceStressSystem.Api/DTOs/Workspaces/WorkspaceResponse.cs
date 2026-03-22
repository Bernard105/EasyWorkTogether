namespace WorkspaceStressSystem.Api.DTOs.Workspaces;

public class WorkspaceResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Config { get; set; }
    public int OwnerId { get; set; }
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}