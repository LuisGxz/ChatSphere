namespace ChatSphere.Application.Common.Interfaces;

/// <summary>
/// Tracks which users are online by counting their live connections. Backed by Redis in production (so
/// presence is correct across multiple API instances) with an in-memory fallback for single-instance dev.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>Registers a connection. Returns true if the user just came online (their first connection).</summary>
    Task<bool> ConnectAsync(Guid userId, string connectionId);

    /// <summary>Removes a connection. Returns true if the user just went offline (their last connection).</summary>
    Task<bool> DisconnectAsync(Guid userId, string connectionId);

    Task<bool> IsOnlineAsync(Guid userId);

    /// <summary>Of the given candidates, which are currently online.</summary>
    Task<IReadOnlyCollection<Guid>> FilterOnlineAsync(IEnumerable<Guid> candidates);
}
