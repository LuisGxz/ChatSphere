using ChatSphere.Application.Common.Models;

namespace ChatSphere.Application.Chat;

/// <summary>Reaction state pushed over SignalR; clients derive "mine" from <see cref="ReactionUserGroup.UserIds"/>.</summary>
public record ReactionUserGroup(string Emoji, int Count, IReadOnlyList<Guid> UserIds);
public record ReactionStateDto(Guid ChannelId, Guid MessageId, IReadOnlyList<ReactionUserGroup> Groups);

/// <summary>Result of sending a message: the message to broadcast + who was mentioned (to notify directly).</summary>
public record SendMessageResult(MessageDto Message, IReadOnlyList<Guid> MentionedUserIds);

// Realtime event payloads broadcast by the hub.
public record TypingDto(Guid ChannelId, Guid UserId, string DisplayName);
public record PresenceDto(Guid UserId, bool Online);
public record ReadReceiptDto(Guid ChannelId, Guid UserId, Guid LastMessageId, DateTimeOffset At);
public record MentionNotificationDto(Guid ChannelId, string? ChannelName, MessageDto Message);
