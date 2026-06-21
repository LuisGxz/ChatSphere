using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Domain.Entities;
using ChatSphere.Domain.Enums;
using ChatSphere.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Tests;

public class ChatServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(ChatService svc, ChatSphereDbContext db, Guid luis)> NewAsync()
    {
        var db = new ChatSphereDbContext(
            new DbContextOptionsBuilder<ChatSphereDbContext>()
                .UseInMemoryDatabase($"cs-chat-{Guid.NewGuid()}").Options);
        await DataSeeder.SeedAsync(db, Now);
        var luis = await db.Users.Where(u => u.Email == "luis@chatsphere.app").Select(u => u.Id).FirstAsync();
        return (new ChatService(db), db, luis);
    }

    [Fact]
    public async Task ListServers_ReturnsServerWithUnreadAndMention()
    {
        var (svc, _, luis) = await NewAsync();
        var servers = await svc.ListServersAsync(luis);

        var s = Assert.Single(servers);
        Assert.Equal(ServerRole.Owner, s.Role);
        Assert.Equal(2, s.UnreadTotal);   // two unread in #eng-frontend
        Assert.True(s.HasMention);        // @Luis in #eng-frontend
    }

    [Fact]
    public async Task GetServer_ReturnsChannelsAndMembers()
    {
        var (svc, db, luis) = await NewAsync();
        var serverId = await db.Servers.Select(s => s.Id).FirstAsync();

        var detail = await svc.GetServerAsync(luis, serverId);
        Assert.Equal(5, detail.Channels.Count);
        Assert.Equal(8, detail.Members.Count);
        Assert.Contains(detail.Channels, c => c.Name == "eng-frontend" && c.Unread == 2 && c.HasMention);
    }

    [Fact]
    public async Task GetServer_NonMember_Throws()
    {
        var (svc, db, _) = await NewAsync();
        var serverId = await db.Servers.Select(s => s.Id).FirstAsync();
        var stranger = new User { Email = "stranger@x.test", DisplayName = "Stranger", AvatarColor = "#fff", PasswordHash = "x" };
        db.Users.Add(stranger);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetServerAsync(stranger.Id, serverId));
    }

    [Fact]
    public async Task GetMessages_ReturnsConversationAscending_WithGroupedReactions()
    {
        var (svc, db, luis) = await NewAsync();
        var designCrit = await db.Channels.Where(c => c.Name == "design-crit").Select(c => c.Id).FirstAsync();

        var page = await svc.GetMessagesAsync(luis, designCrit, before: null, take: 30);
        Assert.Equal(4, page.Messages.Count);
        Assert.False(page.HasMore);
        Assert.True(page.Messages[0].CreatedAt <= page.Messages[^1].CreatedAt); // ascending

        var dots = page.Messages.First(m => m.Body.StartsWith("The progress dots"));
        Assert.Contains(dots.Reactions, r => r.Emoji == "👏" && r.Count == 4 && r.Mine);
        Assert.Contains(dots.Reactions, r => r.Emoji == "🔥" && r.Count == 2);
    }

    [Fact]
    public async Task MarkRead_ClearsUnread()
    {
        var (svc, db, luis) = await NewAsync();
        var eng = await db.Channels.Where(c => c.Name == "eng-frontend").Select(c => c.Id).FirstAsync();
        var last = await db.Messages.Where(m => m.ChannelId == eng).OrderByDescending(m => m.CreatedAt).Select(m => m.Id).FirstAsync();

        await svc.MarkReadAsync(luis, eng, last);

        var detail = await svc.GetServerAsync(luis, await db.Servers.Select(s => s.Id).FirstAsync());
        Assert.Equal(0, detail.Channels.First(c => c.Name == "eng-frontend").Unread);
    }

    [Fact]
    public async Task Dms_ListAndOpenAreConsistent()
    {
        var (svc, db, luis) = await NewAsync();

        var dms = await svc.ListDmsAsync(luis);
        var existing = Assert.Single(dms);
        Assert.Equal("Priya Raman", existing.Other.DisplayName);
        Assert.Equal(2, existing.Unread);

        // Opening a brand-new DM, then again, returns the same channel.
        var marcus = await db.Users.Where(u => u.Email == "marcus@chatsphere.app").Select(u => u.Id).FirstAsync();
        var first = await svc.OpenDmAsync(luis, marcus);
        var second = await svc.OpenDmAsync(luis, marcus);
        Assert.Equal(first.ChannelId, second.ChannelId);
    }

    [Fact]
    public async Task Search_FindsMatchingMessages()
    {
        var (svc, db, luis) = await NewAsync();
        var serverId = await db.Servers.Select(s => s.Id).FirstAsync();

        var hits = await svc.SearchAsync(luis, serverId, "onboarding");
        Assert.NotEmpty(hits);
        Assert.All(hits, h => Assert.Contains("onboarding", h.Message.Body, StringComparison.OrdinalIgnoreCase));
    }
}
