using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>
/// A conversation stream. Server channels have a <see cref="ServerId"/> and a name; direct messages are
/// channels with <see cref="IsDirect"/> = true, no server, and exactly two members. Unifying both under
/// one type keeps messaging, membership and read-state identical for channels and DMs.
/// </summary>
public class Channel : Entity
{
    public Guid? ServerId { get; set; }
    public Server? Server { get; set; }

    /// <summary>Channel name (e.g. "design-crit"); null for direct messages.</summary>
    public string? Name { get; set; }
    public string? Topic { get; set; }

    public bool IsPrivate { get; set; }
    public bool IsDirect { get; set; }
    public int Position { get; set; }

    public ICollection<ChannelMember> Members { get; set; } = new List<ChannelMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
