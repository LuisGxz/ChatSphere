namespace ChatSphere.Application.Chat;

/// <summary>Read-side chat API: servers, channels, history, DMs and search. All access is membership-gated.</summary>
public interface IChatService
{
    Task<IReadOnlyList<ServerSummaryDto>> ListServersAsync(Guid userId, CancellationToken ct = default);
    Task<ServerDetailDto> GetServerAsync(Guid userId, Guid serverId, CancellationToken ct = default);
    Task<MessagePageDto> GetMessagesAsync(Guid userId, Guid channelId, DateTimeOffset? before, int take, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, Guid channelId, Guid lastMessageId, CancellationToken ct = default);
    Task<IReadOnlyList<DmDto>> ListDmsAsync(Guid userId, CancellationToken ct = default);
    Task<DmDto> OpenDmAsync(Guid userId, Guid otherUserId, CancellationToken ct = default);
    Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid userId, Guid serverId, string query, CancellationToken ct = default);
}
