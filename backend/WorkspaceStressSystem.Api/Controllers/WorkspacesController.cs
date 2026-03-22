using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkspaceStressSystem.Api.DTOs.Workspaces;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspacesController(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _workspaceService.GetMyWorkspacesAsync(userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _workspaceService.CreateWorkspaceAsync(userId, request);
        return StatusCode(201, result);
    }

    [HttpPut("{workspaceId:int}")]
    public async Task<IActionResult> UpdateWorkspace(int workspaceId, [FromBody] UpdateWorkspaceRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _workspaceService.UpdateWorkspaceAsync(userId, workspaceId, request);
        return Ok(result);
    }

    [HttpDelete("{workspaceId:int}")]
    public async Task<IActionResult> DeleteWorkspace(int workspaceId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _workspaceService.DeleteWorkspaceAsync(userId, workspaceId);
        return NoContent();
    }

    [HttpGet("{workspaceId:int}/members")]
    public async Task<IActionResult> GetMembers(int workspaceId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _workspaceService.GetMembersAsync(userId, workspaceId);
        return Ok(result);
    }

    [HttpPatch("{workspaceId:int}/members/{userId:int}")]
    public async Task<IActionResult> UpdateMemberRole(int workspaceId, int userId, [FromBody] UpdateMemberRoleRequest request)
    {
        var actorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _workspaceService.UpdateMemberRoleAsync(actorUserId, workspaceId, userId, request);
        return Ok(new { message = "Cập nhật vai trò thành công." });
    }

    [HttpDelete("{workspaceId:int}/members/{userId:int}")]
    public async Task<IActionResult> RemoveMember(int workspaceId, int userId)
    {
        var actorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _workspaceService.RemoveMemberAsync(actorUserId, workspaceId, userId);
        return NoContent();
    }
}