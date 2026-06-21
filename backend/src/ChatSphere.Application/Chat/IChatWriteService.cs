namespace ChatSphere.Application.Chat;

/// <summary>Write-side chat operations invoked from the SignalR hub (persist, then the hub broadcasts).</summary>
public interface IChatWriteService
{
    Task<SendMessageResult> SendMessageAsync(Guid userId, Guid channelId, string body, CancellationToken ct = default);
    Task<ReactionStateDto> ToggleReactionAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default);

    /// <summary>Channels the user belongs to — used by the hub to subscribe a connection to its groups.</summary>
    Task<IReadOnlyList<Guid>> GetMemberChannelIdsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Servers the user belongs to — used for presence fan-out to co-members.</summary>
    Task<IReadOnlyList<Guid>> GetMemberServerIdsAsync(Guid userId, CancellationToken ct = default);
}
