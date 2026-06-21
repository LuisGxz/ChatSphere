using ChatSphere.Application.Common.Models;
using ChatSphere.Domain.Enums;

namespace ChatSphere.Application.Chat;

public record ServerSummaryDto(Guid Id, string Name, string Slug, ServerRole Role, int UnreadTotal, bool HasMention);

public record ChannelDto(
    Guid Id, string? Name, string? Topic, bool IsPrivate, bool IsDirect, int Position,
    int Unread, bool HasMention, string? LastMessagePreview, DateTimeOffset? LastMessageAt);

public record MemberDto(UserMiniDto User, ServerRole Role);

public record ServerDetailDto(
    Guid Id, string Name, string Slug, ServerRole Role,
    IReadOnlyList<ChannelDto> Channels, IReadOnlyList<MemberDto> Members);

public record ReactionGroupDto(string Emoji, int Count, bool Mine);

public record AttachmentDto(Guid Id, string Url, string FileName, string ContentType, int? Width, int? Height);

public record MessageDto(
    Guid Id, Guid ChannelId, UserMiniDto Author, string Body, MessageType Type,
    DateTimeOffset CreatedAt, DateTimeOffset? EditedAt,
    IReadOnlyList<ReactionGroupDto> Reactions, IReadOnlyList<AttachmentDto> Attachments, bool MentionsMe);

/// <summary>A page of history, newest-cursor pagination: pass <see cref="NextBefore"/> back as `before`.</summary>
public record MessagePageDto(IReadOnlyList<MessageDto> Messages, bool HasMore, DateTimeOffset? NextBefore);

public record DmDto(Guid ChannelId, UserMiniDto Other, int Unread, string? LastMessagePreview, DateTimeOffset? LastMessageAt);

public record SearchResultDto(Guid ChannelId, string? ChannelName, MessageDto Message);

public record MarkReadRequest(Guid LastMessageId);
