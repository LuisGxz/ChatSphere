using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>An emoji reaction by a user on a message (unique per message + user + emoji).</summary>
public class Reaction : Entity
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Emoji { get; set; } = string.Empty;
}
