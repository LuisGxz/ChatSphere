using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSphere.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/channels")]
public sealed class ChannelsController(IChatService chat, ICurrentUser currentUser) : ControllerBase
{
    private Guid Me => currentUser.UserId ?? throw new UnauthorizedException("Authentication required.");

    [HttpGet("{channelId:guid}/messages")]
    public async Task<ActionResult<MessagePageDto>> Messages(
        Guid channelId, [FromQuery] DateTimeOffset? before, [FromQuery] int take, CancellationToken ct)
        => Ok(await chat.GetMessagesAsync(Me, channelId, before, take == 0 ? 30 : take, ct));

    [HttpPost("{channelId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid channelId, MarkReadRequest request, CancellationToken ct)
    {
        await chat.MarkReadAsync(Me, channelId, request.LastMessageId, ct);
        return NoContent();
    }
}
