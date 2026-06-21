using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>Records that a message @-mentions a user, for mention notifications.</summary>
public class Mention : Entity
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid MentionedUserId { get; set; }
    public User? MentionedUser { get; set; }
}
