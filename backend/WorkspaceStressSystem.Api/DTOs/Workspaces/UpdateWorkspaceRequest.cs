using System.Text.Json;

namespace WorkspaceStressSystem.Api.DTOs.Workspaces;

public class UpdateWorkspaceRequest
{
    public string Name { get; set; } = null!;
    public JsonElement? Config { get; set; }
}