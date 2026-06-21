using ChatSphere.Infrastructure.Presence;

namespace ChatSphere.Tests;

public class PresenceTrackerTests
{
    [Fact]
    public async Task FirstConnectionOnline_LastDisconnectionOffline()
    {
        var tracker = new InMemoryPresenceTracker();
        var user = Guid.NewGuid();

        Assert.True(await tracker.ConnectAsync(user, "c1"));   // became online
        Assert.False(await tracker.ConnectAsync(user, "c2"));  // already online (2nd device)
        Assert.True(await tracker.IsOnlineAsync(user));

        Assert.False(await tracker.DisconnectAsync(user, "c1")); // still online via c2
        Assert.True(await tracker.IsOnlineAsync(user));
        Assert.True(await tracker.DisconnectAsync(user, "c2"));  // last connection → offline
        Assert.False(await tracker.IsOnlineAsync(user));
    }

    [Fact]
    public async Task FilterOnline_ReturnsOnlySubsetThatIsConnected()
    {
        var tracker = new InMemoryPresenceTracker();
        Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
        await tracker.ConnectAsync(a, "ca");
        await tracker.ConnectAsync(c, "cc");

        var online = await tracker.FilterOnlineAsync([a, b, c]);
        Assert.Contains(a, online);
        Assert.Contains(c, online);
        Assert.DoesNotContain(b, online);
    }
}
