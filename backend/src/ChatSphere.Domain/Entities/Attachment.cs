using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>An image attachment on a message.</summary>
public class Attachment : Entity
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
}
