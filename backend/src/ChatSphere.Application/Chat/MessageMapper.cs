using ChatSphere.Application.Common.Models;
using ChatSphere.Domain.Entities;

namespace ChatSphere.Application.Chat;

/// <summary>Shared mapping from a loaded <see cref="Message"/> (with reactions/attachments/mentions) to DTOs.</summary>
public static class MessageMapper
{
    public static MessageDto ToDto(Message m, Guid forUserId)
    {
        var reactions = m.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionGroupDto(g.Key, g.Count(), g.Any(r => r.UserId == forUserId)))
            .OrderByDescending(g => g.Count)
            .ToList();

        var attachments = m.Attachments
            .Select(a => new AttachmentDto(a.Id, a.Url, a.FileName, a.ContentType, a.Width, a.Height))
            .ToList();

        return new MessageDto(
            m.Id,
            m.ChannelId,
            new UserMiniDto(m.Author!.Id, m.Author.DisplayName, m.Author.AvatarColor, m.Author.Title),
            m.Body, m.Type, m.CreatedAt, m.EditedAt, reactions, attachments,
            m.Mentions.Any(mn => mn.MentionedUserId == forUserId));
    }

    /// <summary>Reaction state for realtime broadcast — includes user ids so each client computes "mine".</summary>
    public static ReactionStateDto ToReactionState(Message m)
    {
        var groups = m.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionUserGroup(g.Key, g.Count(), g.Select(r => r.UserId).ToList()))
            .OrderByDescending(g => g.Count)
            .ToList();
        return new ReactionStateDto(m.ChannelId, m.Id, groups);
    }
}
