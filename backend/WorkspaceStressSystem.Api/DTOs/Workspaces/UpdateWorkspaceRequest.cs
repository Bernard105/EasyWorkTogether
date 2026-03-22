namespace WorkspaceStressSystem.Api.DTOs.Workspaces;

public class UpdateWorkspaceRequest
{
    public string Name { get; set; } = null!;
    public string? Config { get; set; }
}