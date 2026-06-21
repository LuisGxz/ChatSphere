using ChatSphere.Application.Auth;
using ChatSphere.Application.Common;
using ChatSphere.Application.Common.Exceptions;
using ChatSphere.Domain.Enums;
using ChatSphere.Infrastructure.Auth;
using ChatSphere.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatSphere.Tests;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(AuthService svc, ChatSphereDbContext db)> NewAsync()
    {
        var db = new ChatSphereDbContext(
            new DbContextOptionsBuilder<ChatSphereDbContext>()
                .UseInMemoryDatabase($"cs-auth-{Guid.NewGuid()}").Options);
        await DataSeeder.SeedAsync(db, Now); // gives us the demo server to auto-join

        var clock = new FakeClock(Now);
        var svc = new AuthService(db, new PasswordHasherAdapter(), new FakeJwt(clock), new TokenHasher(), clock,
            Options.Create(new AuthSettings()),
            new RegisterRequestValidator(), new LoginRequestValidator(), new RefreshRequestValidator());
        return (svc, db);
    }

    [Fact]
    public async Task Register_CreatesUser_AndAutoJoinsDemoServer()
    {
        var (svc, db) = await NewAsync();

        var res = await svc.RegisterAsync(new RegisterRequest("newbie@chatsphere.app", "Sup3rSecret", "New Bie"));

        Assert.Equal("newbie@chatsphere.app", res.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(res.Tokens.AccessToken));

        var server = await db.Servers.FirstAsync();
        var membership = await db.ServerMembers.SingleAsync(m => m.UserId == res.User.Id);
        Assert.Equal(server.Id, membership.ServerId);
        Assert.Equal(ServerRole.Member, membership.Role);

        // Joined every public channel (5 seeded).
        var joined = await db.ChannelMembers.CountAsync(cm => cm.UserId == res.User.Id);
        Assert.Equal(5, joined);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var (svc, _) = await NewAsync();
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => svc.RegisterAsync(new RegisterRequest("luis@chatsphere.app", "Sup3rSecret", "Dup")));
        Assert.Equal("email_taken", ex.Code);
    }

    [Fact]
    public async Task Login_DemoAccount_Succeeds_AndWrongPasswordLocksAfterFive()
    {
        var (svc, _) = await NewAsync();

        var ok = await svc.LoginAsync(new LoginRequest("luis@chatsphere.app", DataSeeder.DemoPassword));
        Assert.False(string.IsNullOrWhiteSpace(ok.Tokens.RefreshToken));

        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => svc.LoginAsync(new LoginRequest("marcus@chatsphere.app", "wrong-pass")));
        var locked = await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("marcus@chatsphere.app", DataSeeder.DemoPassword)));
        Assert.Equal("locked_out", locked.Code);
    }

    [Fact]
    public async Task Refresh_RotatesToken_RevokingTheOldOne()
    {
        var (svc, db) = await NewAsync();
        var login = await svc.LoginAsync(new LoginRequest("luis@chatsphere.app", DataSeeder.DemoPassword));
        var oldRaw = login.Tokens.RefreshToken;

        var refreshed = await svc.RefreshAsync(new RefreshRequest(oldRaw));
        Assert.NotEqual(oldRaw, refreshed.Tokens.RefreshToken);

        var hasher = new TokenHasher();
        var old = await db.RefreshTokens.SingleAsync(t => t.TokenHash == hasher.Hash(oldRaw));
        Assert.NotNull(old.RevokedAt);

        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.RefreshAsync(new RefreshRequest(oldRaw)));
    }
}
