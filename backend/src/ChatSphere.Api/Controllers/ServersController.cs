using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSphere.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/servers")]
public sealed class ServersController(IChatService chat, ICurrentUser currentUser) : ControllerBase
{
    private Guid Me => currentUser.UserId ?? throw new UnauthorizedException("Authentication required.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServerSummaryDto>>> List(CancellationToken ct)
        => Ok(await chat.ListServersAsync(Me, ct));

    [HttpGet("{serverId:guid}")]
    public async Task<ActionResult<ServerDetailDto>> Get(Guid serverId, CancellationToken ct)
        => Ok(await chat.GetServerAsync(Me, serverId, ct));

    [HttpGet("{serverId:guid}/search")]
    public async Task<ActionResult<IReadOnlyList<SearchResultDto>>> Search(Guid serverId, [FromQuery] string q, CancellationToken ct)
        => Ok(await chat.SearchAsync(Me, serverId, q ?? string.Empty, ct));
}
