using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSphere.Api.Controllers;

public record OpenDmRequest(Guid OtherUserId);

[ApiController]
[Authorize]
[Route("api/dms")]
public sealed class DmsController(IChatService chat, ICurrentUser currentUser) : ControllerBase
{
    private Guid Me => currentUser.UserId ?? throw new UnauthorizedException("Authentication required.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DmDto>>> List(CancellationToken ct)
        => Ok(await chat.ListDmsAsync(Me, ct));

    [HttpPost]
    public async Task<ActionResult<DmDto>> Open(OpenDmRequest request, CancellationToken ct)
        => Ok(await chat.OpenDmAsync(Me, request.OtherUserId, ct));
}
