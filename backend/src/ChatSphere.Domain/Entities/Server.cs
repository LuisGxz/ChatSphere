using ChatSphere.Domain.Common;

namespace ChatSphere.Domain.Entities;

/// <summary>A community (workspace) that groups channels and members, e.g. "Driftwood Studio".</summary>
public class Server : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public ICollection<ServerMember> Members { get; set; } = new List<ServerMember>();
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
}
