using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkspaceStressSystem.Api.DTOs.Invitations;
using WorkspaceStressSystem.Api.Services.Interfaces;

namespace WorkspaceStressSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost("workspaces/{workspaceId:int}/invitations")]
    public async Task<IActionResult> CreateInvitation(int workspaceId, [FromBody] CreateInvitationRequest request)
    {
        var actorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _invitationService.CreateInvitationAsync(actorUserId, workspaceId, request);
        return StatusCode(201, result);
    }

    [HttpPost("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _invitationService.AcceptInvitationAsync(userId, request);
        return Ok(result);
    }
}