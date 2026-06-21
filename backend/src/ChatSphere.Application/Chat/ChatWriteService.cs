using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Application.Chat;

public sealed class ChatWriteService(IAppDbContext db, IClock clock) : IChatWriteService
{
    public async Task<SendMessageResult> SendMessageAsync(Guid userId, Guid channelId, string body, CancellationToken ct = default)
    {
        await EnsureMemberAsync(userId, channelId, ct);

        var text = (body ?? string.Empty).Trim();
        if (text.Length == 0)
            throw new BadRequestException("Message can't be empty.", "empty_message");
        if (text.Length > 4000)
            throw new BadRequestException("Message is too long (max 4000 characters).", "message_too_long");

        var message = new Message { ChannelId = channelId, AuthorId = userId, Body = text, CreatedAt = clock.UtcNow };
        db.Messages.Add(message);

        var mentioned = await ResolveMentionsAsync(channelId, userId, text, ct);
        foreach (var mid in mentioned)
            db.Mentions.Add(new Mention { MessageId = message.Id, MentionedUserId = mid });

        await db.SaveChangesAsync(ct);

        var saved = await db.Messages
            .Include(m => m.Author)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .FirstAsync(m => m.Id == message.Id, ct);

        return new SendMessageResult(MessageMapper.ToDto(saved, userId), mentioned);
    }

    public async Task<ReactionStateDto> ToggleReactionAsync(Guid userId, Guid messageId, string emoji, CancellationToken ct = default)
    {
        emoji = (emoji ?? string.Empty).Trim();
        if (emoji.Length == 0 || emoji.Length > 32)
            throw new BadRequestException("Invalid emoji.", "invalid_emoji");

        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == messageId, ct)
                      ?? throw new NotFoundException("Message not found.");
        await EnsureMemberAsync(userId, message.ChannelId, ct);

        var existing = await db.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji, ct);
        if (existing is null)
            db.Reactions.Add(new Reaction { MessageId = messageId, UserId = userId, Emoji = emoji });
        else
            db.Reactions.Remove(existing);
        await db.SaveChangesAsync(ct);

        var withReactions = await db.Messages.Include(m => m.Reactions).FirstAsync(m => m.Id == messageId, ct);
        return MessageMapper.ToReactionState(withReactions);
    }

    public async Task<IReadOnlyList<Guid>> GetMemberChannelIdsAsync(Guid userId, CancellationToken ct = default)
        => await db.ChannelMembers.Where(m => m.UserId == userId).Select(m => m.ChannelId).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetMemberServerIdsAsync(Guid userId, CancellationToken ct = default)
        => await db.ServerMembers.Where(m => m.UserId == userId).Select(m => m.ServerId).ToListAsync(ct);

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureMemberAsync(Guid userId, Guid channelId, CancellationToken ct)
    {
        if (!await db.ChannelMembers.AnyAsync(m => m.ChannelId == channelId && m.UserId == userId, ct))
            throw new ForbiddenException("You don't have access to this channel.");
    }

    /// <summary>Resolves @-mentions by matching channel members' display names in the message body.</summary>
    private async Task<IReadOnlyList<Guid>> ResolveMentionsAsync(Guid channelId, Guid authorId, string body, CancellationToken ct)
    {
        if (!body.Contains('@'))
            return [];

        var members = await db.ChannelMembers
            .Where(m => m.ChannelId == channelId && m.UserId != authorId)
            .Select(m => new { m.UserId, m.User!.DisplayName })
            .ToListAsync(ct);

        return members
            .Where(m => body.Contains('@' + m.DisplayName, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.UserId)
            .Distinct()
            .ToList();
    }
}
