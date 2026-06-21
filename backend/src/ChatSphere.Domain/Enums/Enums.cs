namespace ChatSphere.Domain.Enums;

/// <summary>Role within a server (community). Owner > Admin > Member.</summary>
public enum ServerRole
{
    Member = 0,
    Admin = 1,
    Owner = 2,
}

/// <summary>Live presence, tracked in Redis and broadcast over SignalR (never persisted to SQL).</summary>
public enum PresenceStatus
{
    Offline = 0,
    Online = 1,
    Away = 2,
}

public enum MessageType
{
    Text = 0,
    System = 1,
}
