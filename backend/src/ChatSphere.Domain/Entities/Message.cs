using ChatSphere.Domain.Common;
using ChatSphere.Domain.Enums;

namespace ChatSphere.Domain.Entities;

/// <summary>A message in a channel or DM. Mentions are stored for notification queries.</summary>
public class Message : Entity
{
    public Guid ChannelId { get; set; }
    public Channel? Channel { get; set; }

    public Guid AuthorId { get; set; }
    public User? Author { get; set; }

    public string Body { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTimeOffset? EditedAt { get; set; }

    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<Mention> Mentions { get; set; } = new List<Mention>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
