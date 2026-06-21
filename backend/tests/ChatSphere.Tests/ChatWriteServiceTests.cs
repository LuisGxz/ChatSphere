using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Domain.Entities;
using ChatSphere.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Tests;

public class ChatWriteServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(ChatWriteService svc, ChatSphereDbContext db, Guid luis, Guid designCrit)> NewAsync()
    {
        var db = new ChatSphereDbContext(
            new DbContextOptionsBuilder<ChatSphereDbContext>()
                .UseInMemoryDatabase($"cs-write-{Guid.NewGuid()}").Options);
        await DataSeeder.SeedAsync(db, Now);
        var luis = await db.Users.Where(u => u.Email == "luis@chatsphere.app").Select(u => u.Id).FirstAsync();
        var dc = await db.Channels.Where(c => c.Name == "design-crit").Select(c => c.Id).FirstAsync();
        return (new ChatWriteService(db, new FakeClock(Now)), db, luis, dc);
    }

    [Fact]
    public async Task SendMessage_PersistsAndResolvesMention()
    {
        var (svc, db, luis, dc) = await NewAsync();

        var result = await svc.SendMessageAsync(luis, dc, "Hey @Marcus Bell take a look");

        Assert.Equal("Hey @Marcus Bell take a look", result.Message.Body);
        Assert.Equal("Luis Chiquito", result.Message.Author.DisplayName);

        var marcus = await db.Users.Where(u => u.Email == "marcus@chatsphere.app").Select(u => u.Id).FirstAsync();
        Assert.Contains(marcus, result.MentionedUserIds);
        Assert.True(await db.Mentions.AnyAsync(m => m.MentionedUserId == marcus));
    }

    [Fact]
    public async Task SendMessage_EmptyBody_Throws()
    {
        var (svc, _, luis, dc) = await NewAsync();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => svc.SendMessageAsync(luis, dc, "   "));
        Assert.Equal("empty_message", ex.Code);
    }

    [Fact]
    public async Task SendMessage_NonMember_Throws()
    {
        var (svc, db, _, dc) = await NewAsync();
        var stranger = new User { Email = "x@x.test", DisplayName = "X", AvatarColor = "#fff", PasswordHash = "x" };
        db.Users.Add(stranger);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => svc.SendMessageAsync(stranger.Id, dc, "hi"));
    }

    [Fact]
    public async Task ToggleReaction_AddsThenRemoves()
    {
        var (svc, db, luis, dc) = await NewAsync();
        var msg = await db.Messages.Where(m => m.ChannelId == dc).OrderBy(m => m.CreatedAt).Select(m => m.Id).FirstAsync();

        var added = await svc.ToggleReactionAsync(luis, msg, "🎉");
        Assert.Contains(added.Groups, g => g.Emoji == "🎉" && g.Count == 1 && g.UserIds.Contains(luis));

        var removed = await svc.ToggleReactionAsync(luis, msg, "🎉");
        Assert.DoesNotContain(removed.Groups, g => g.Emoji == "🎉");
    }
}
