using System.Collections.Concurrent;
using ChatSphere.Application.Common.Interfaces;

namespace ChatSphere.Infrastructure.Presence;

/// <summary>Single-instance presence (dev fallback). Counts connections per user in-process.</summary>
public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();

    public Task<bool> ConnectAsync(Guid userId, string connectionId)
    {
        var becameOnline = false;
        _connections.AddOrUpdate(userId,
            _ => { becameOnline = true; return [connectionId]; },
            (_, set) =>
            {
                lock (set)
                {
                    becameOnline = set.Count == 0;
                    set.Add(connectionId);
                }
                return set;
            });
        return Task.FromResult(becameOnline);
    }

    public Task<bool> DisconnectAsync(Guid userId, string connectionId)
    {
        var wentOffline = false;
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                    wentOffline = true;
                }
            }
        }
        return Task.FromResult(wentOffline);
    }

    public Task<bool> IsOnlineAsync(Guid userId)
        => Task.FromResult(_connections.TryGetValue(userId, out var set) && set.Count > 0);

    public Task<IReadOnlyCollection<Guid>> FilterOnlineAsync(IEnumerable<Guid> candidates)
    {
        var online = candidates.Where(id => _connections.TryGetValue(id, out var s) && s.Count > 0).ToList();
        return Task.FromResult<IReadOnlyCollection<Guid>>(online);
    }
}
