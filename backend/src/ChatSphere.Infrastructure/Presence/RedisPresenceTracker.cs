using ChatSphere.Application.Common.Interfaces;
using StackExchange.Redis;

namespace ChatSphere.Infrastructure.Presence;

/// <summary>
/// Redis-backed presence: a SET of connection ids per user (<c>presence:{userId}</c>). Correct across
/// multiple API instances, so a user is "online" if any instance holds a live connection for them.
/// </summary>
public sealed class RedisPresenceTracker(IConnectionMultiplexer redis) : IPresenceTracker
{
    private IDatabase Db => redis.GetDatabase();
    private static string Key(Guid userId) => $"presence:{userId}";

    public async Task<bool> ConnectAsync(Guid userId, string connectionId)
    {
        var db = Db;
        await db.SetAddAsync(Key(userId), connectionId);
        return await db.SetLengthAsync(Key(userId)) == 1; // first connection → just came online
    }

    public async Task<bool> DisconnectAsync(Guid userId, string connectionId)
    {
        var db = Db;
        await db.SetRemoveAsync(Key(userId), connectionId);
        var remaining = await db.SetLengthAsync(Key(userId));
        if (remaining == 0)
            await db.KeyDeleteAsync(Key(userId));
        return remaining == 0;
    }

    public async Task<bool> IsOnlineAsync(Guid userId)
        => await Db.KeyExistsAsync(Key(userId));

    public async Task<IReadOnlyCollection<Guid>> FilterOnlineAsync(IEnumerable<Guid> candidates)
    {
        var db = Db;
        var online = new List<Guid>();
        foreach (var id in candidates.Distinct())
            if (await db.KeyExistsAsync(Key(id)))
                online.Add(id);
        return online;
    }
}
