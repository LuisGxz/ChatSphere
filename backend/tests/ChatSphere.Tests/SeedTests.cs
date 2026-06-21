using ChatSphere.Domain.Enums;
using ChatSphere.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Tests;

public class SeedTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task<ChatSphereDbContext> SeededAsync()
    {
        var db = new ChatSphereDbContext(
            new DbContextOptionsBuilder<ChatSphereDbContext>()
                .UseInMemoryDatabase($"cs-{Guid.NewGuid()}").Options);
        await DataSeeder.SeedAsync(db, Now);
        return db;
    }

    [Fact]
    public async Task Seed_CreatesServerUsersAndChannels()
    {
        var db = await SeededAsync();

        Assert.Equal(1, await db.Servers.CountAsync());
        Assert.Equal(8, await db.Users.CountAsync());
        Assert.Equal(5, await db.Channels.CountAsync(c => c.ServerId != null));
        Assert.Equal(1, await db.Channels.CountAsync(c => c.IsDirect));

        var owners = await db.ServerMembers.CountAsync(m => m.Role == ServerRole.Owner);
        Assert.Equal(1, owners);
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        var db = await SeededAsync();
        await DataSeeder.SeedAsync(db, Now); // second run must be a no-op
        Assert.Equal(1, await db.Servers.CountAsync());
        Assert.Equal(8, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Seed_DesignCritHasConversationWithReactions()
    {
        var db = await SeededAsync();
        var channel = await db.Channels.FirstAsync(c => c.Name == "design-crit");
        var messages = await db.Messages.Where(m => m.ChannelId == channel.Id).ToListAsync();

        Assert.Equal(4, messages.Count);
        var totalReactions = await db.Reactions
            .CountAsync(r => messages.Select(m => m.Id).Contains(r.MessageId));
        Assert.Equal(9, totalReactions);
    }

    [Fact]
    public async Task Seed_LeavesLuisWithUnreadsAndAMention()
    {
        var db = await SeededAsync();
        var luis = await db.Users.FirstAsync(u => u.Email == "luis@chatsphere.app");

        // eng-frontend: read cursor at the first message → two later messages are unread.
        var eng = await db.Channels.FirstAsync(c => c.Name == "eng-frontend");
        Assert.Equal(2, await UnreadAsync(db, eng.Id, luis.Id));

        // DM: Luis read his own line, Priya sent two more → two unread.
        var dm = await db.Channels.FirstAsync(c => c.IsDirect);
        Assert.Equal(2, await UnreadAsync(db, dm.Id, luis.Id));

        // A pending mention of Luis exists.
        Assert.True(await db.Mentions.AnyAsync(m => m.MentionedUserId == luis.Id));
    }

    private static async Task<int> UnreadAsync(ChatSphereDbContext db, Guid channelId, Guid userId)
    {
        var member = await db.ChannelMembers.FirstAsync(m => m.ChannelId == channelId && m.UserId == userId);
        DateTimeOffset since = DateTimeOffset.MinValue;
        if (member.LastReadMessageId is { } id)
            since = await db.Messages.Where(m => m.Id == id).Select(m => m.CreatedAt).FirstAsync();
        return await db.Messages.CountAsync(m => m.ChannelId == channelId && m.CreatedAt > since);
    }
}
