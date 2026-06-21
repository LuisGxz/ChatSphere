using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>A user's membership in a channel/DM, plus their read cursor for unread counts.</summary>
public class ChannelMember : Entity
{
    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The last message this member has read; everything after it counts as unread.</summary>
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
}
