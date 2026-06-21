using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Application.Common.Models;
using ChatSphere.Domain.Entities;
using ChatSphere.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Application.Chat;

public sealed class ChatService(IAppDbContext db) : IChatService
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<ServerSummaryDto>> ListServersAsync(Guid userId, CancellationToken ct = default)
    {
        var memberships = await db.ServerMembers
            .Where(m => m.UserId == userId)
            .Select(m => new { m.Server!.Id, m.Server.Name, m.Server.Slug, m.Role, m.Server.CreatedAt })
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        var result = new List<ServerSummaryDto>();
        foreach (var s in memberships)
        {
            var (unread, mention) = await ServerUnreadAsync(userId, s.Id, ct);
            result.Add(new ServerSummaryDto(s.Id, s.Name, s.Slug, s.Role, unread, mention));
        }
        return result;
    }

    public async Task<ServerDetailDto> GetServerAsync(Guid userId, Guid serverId, CancellationToken ct = default)
    {
        var role = await EnsureServerMemberAsync(userId, serverId, ct);
        var server = await db.Servers.FirstAsync(s => s.Id == serverId, ct);

        // Channels the user belongs to within this server.
        var myChannelIds = await db.ChannelMembers
            .Where(cm => cm.UserId == userId && cm.Channel!.ServerId == serverId)
            .Select(cm => cm.ChannelId)
            .ToListAsync(ct);

        var channels = await db.Channels
            .Where(c => c.ServerId == serverId && myChannelIds.Contains(c.Id))
            .OrderBy(c => c.Position)
            .ToListAsync(ct);

        var channelDtos = new List<ChannelDto>();
        foreach (var c in channels)
            channelDtos.Add(await ToChannelDtoAsync(userId, c, ct));

        var members = await db.ServerMembers
            .Where(m => m.ServerId == serverId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.User!.DisplayName)
            .Select(m => new MemberDto(
                new UserMiniDto(m.User!.Id, m.User.DisplayName, m.User.AvatarColor, m.User.Title), m.Role))
            .ToListAsync(ct);

        return new ServerDetailDto(server.Id, server.Name, server.Slug, role, channelDtos, members);
    }

    public async Task<MessagePageDto> GetMessagesAsync(Guid userId, Guid channelId, DateTimeOffset? before, int take, CancellationToken ct = default)
    {
        await EnsureChannelMemberAsync(userId, channelId, ct);
        take = Math.Clamp(take, 1, 100);

        var query = db.Messages.Where(m => m.ChannelId == channelId);
        if (before is { } cursor)
            query = query.Where(m => m.CreatedAt < cursor);

        // Pull newest-first for the cursor, then present oldest-first.
        var slice = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(take + 1)
            .Include(m => m.Author)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .ToListAsync(ct);

        var hasMore = slice.Count > take;
        var page = slice.Take(take).OrderBy(m => m.CreatedAt).ToList();
        var messages = page.Select(m => MapMessage(m, userId)).ToList();
        var nextBefore = hasMore ? page.FirstOrDefault()?.CreatedAt : null;

        return new MessagePageDto(messages, hasMore, nextBefore);
    }

    public async Task MarkReadAsync(Guid userId, Guid channelId, Guid lastMessageId, CancellationToken ct = default)
    {
        var member = await db.ChannelMembers.FirstOrDefaultAsync(m => m.ChannelId == channelId && m.UserId == userId, ct)
                     ?? throw new ForbiddenException("You are not a member of this channel.");
        var readAt = await db.Messages.Where(m => m.Id == lastMessageId && m.ChannelId == channelId)
            .Select(m => (DateTimeOffset?)m.CreatedAt).FirstOrDefaultAsync(ct);
        if (readAt is null)
            throw new NotFoundException("Message not found in this channel.");

        member.LastReadMessageId = lastMessageId;
        member.LastReadAt = readAt;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DmDto>> ListDmsAsync(Guid userId, CancellationToken ct = default)
    {
        var dmIds = await db.ChannelMembers
            .Where(cm => cm.UserId == userId && cm.Channel!.IsDirect)
            .Select(cm => cm.ChannelId)
            .ToListAsync(ct);

        var dtos = new List<DmDto>();
        foreach (var channelId in dmIds)
            dtos.Add(await ToDmDtoAsync(userId, channelId, ct));
        return dtos.OrderByDescending(d => d.LastMessageAt ?? Epoch).ToList();
    }

    public async Task<DmDto> OpenDmAsync(Guid userId, Guid otherUserId, CancellationToken ct = default)
    {
        if (userId == otherUserId)
            throw new BadRequestException("You can't open a DM with yourself.", "self_dm");
        if (!await db.Users.AnyAsync(u => u.Id == otherUserId, ct))
            throw new NotFoundException("User not found.");

        var existing = await db.Channels
            .Where(c => c.IsDirect
                        && c.Members.Any(m => m.UserId == userId)
                        && c.Members.Any(m => m.UserId == otherUserId))
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (existing == Guid.Empty)
        {
            var channel = new Channel { IsDirect = true };
            channel.Members.Add(new ChannelMember { UserId = userId });
            channel.Members.Add(new ChannelMember { UserId = otherUserId });
            db.Channels.Add(channel);
            await db.SaveChangesAsync(ct);
            existing = channel.Id;
        }

        return await ToDmDtoAsync(userId, existing, ct);
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(Guid userId, Guid serverId, string query, CancellationToken ct = default)
    {
        await EnsureServerMemberAsync(userId, serverId, ct);
        var term = query.Trim();
        if (term.Length < 2)
            return [];

        var myChannelIds = await db.ChannelMembers
            .Where(cm => cm.UserId == userId && cm.Channel!.ServerId == serverId)
            .Select(cm => cm.ChannelId)
            .ToListAsync(ct);

        var hits = await db.Messages
            .Where(m => myChannelIds.Contains(m.ChannelId) && m.Body.Contains(term))
            .OrderByDescending(m => m.CreatedAt)
            .Take(30)
            .Include(m => m.Author)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Include(m => m.Mentions)
            .Include(m => m.Channel)
            .ToListAsync(ct);

        return hits.Select(m => new SearchResultDto(m.ChannelId, m.Channel?.Name, MapMessage(m, userId))).ToList();
    }

    // ── authorization ──────────────────────────────────────────────────────

    private async Task<ServerRole> EnsureServerMemberAsync(Guid userId, Guid serverId, CancellationToken ct)
    {
        var member = await db.ServerMembers.FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId, ct)
                     ?? throw new ForbiddenException("You are not a member of this server.");
        return member.Role;
    }

    private async Task EnsureChannelMemberAsync(Guid userId, Guid channelId, CancellationToken ct)
    {
        var ok = await db.ChannelMembers.AnyAsync(m => m.ChannelId == channelId && m.UserId == userId, ct);
        if (!ok)
            throw new ForbiddenException("You don't have access to this channel.");
    }

    // ── projections ────────────────────────────────────────────────────────

    private async Task<(int Unread, bool Mention)> ServerUnreadAsync(Guid userId, Guid serverId, CancellationToken ct)
    {
        var chans = await db.ChannelMembers
            .Where(cm => cm.UserId == userId && cm.Channel!.ServerId == serverId)
            .Select(cm => new { cm.ChannelId, cm.LastReadAt })
            .ToListAsync(ct);

        var unread = 0;
        var mention = false;
        foreach (var c in chans)
        {
            var since = c.LastReadAt ?? Epoch;
            unread += await db.Messages.CountAsync(m => m.ChannelId == c.ChannelId && m.CreatedAt > since && m.AuthorId != userId, ct);
            if (!mention)
                mention = await db.Mentions.AnyAsync(
                    mn => mn.MentionedUserId == userId && mn.Message!.ChannelId == c.ChannelId && mn.Message.CreatedAt > since, ct);
        }
        return (unread, mention);
    }

    private async Task<ChannelDto> ToChannelDtoAsync(Guid userId, Channel c, CancellationToken ct)
    {
        var member = await db.ChannelMembers.FirstAsync(m => m.ChannelId == c.Id && m.UserId == userId, ct);
        var since = member.LastReadAt ?? Epoch;

        var unread = await db.Messages.CountAsync(m => m.ChannelId == c.Id && m.CreatedAt > since && m.AuthorId != userId, ct);
        var mention = await db.Mentions.AnyAsync(
            mn => mn.MentionedUserId == userId && mn.Message!.ChannelId == c.Id && mn.Message.CreatedAt > since, ct);
        var last = await db.Messages.Where(m => m.ChannelId == c.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.Body, m.CreatedAt })
            .FirstOrDefaultAsync(ct);

        return new ChannelDto(c.Id, c.Name, c.Topic, c.IsPrivate, c.IsDirect, c.Position,
            unread, mention, Preview(last?.Body), last?.CreatedAt);
    }

    private async Task<DmDto> ToDmDtoAsync(Guid userId, Guid channelId, CancellationToken ct)
    {
        var other = await db.ChannelMembers
            .Where(m => m.ChannelId == channelId && m.UserId != userId)
            .Select(m => new UserMiniDto(m.User!.Id, m.User.DisplayName, m.User.AvatarColor, m.User.Title))
            .FirstAsync(ct);

        var member = await db.ChannelMembers.FirstAsync(m => m.ChannelId == channelId && m.UserId == userId, ct);
        var since = member.LastReadAt ?? Epoch;
        var unread = await db.Messages.CountAsync(m => m.ChannelId == channelId && m.CreatedAt > since && m.AuthorId != userId, ct);
        var last = await db.Messages.Where(m => m.ChannelId == channelId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.Body, m.CreatedAt })
            .FirstOrDefaultAsync(ct);

        return new DmDto(channelId, other, unread, Preview(last?.Body), last?.CreatedAt);
    }

    private static MessageDto MapMessage(Message m, Guid userId) => MessageMapper.ToDto(m, userId);

    private static string? Preview(string? body)
        => body is null ? null : body.Length <= 80 ? body : body[..80] + "…";
}
