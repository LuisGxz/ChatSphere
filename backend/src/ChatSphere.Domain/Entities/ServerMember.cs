using ChatSphere.Domain.Common;
using ChatSphere.Domain.Enums;

namespace ChatSphere.Domain.Entities;

/// <summary>Join entity carrying a user's role within a server.</summary>
public class ServerMember : Entity
{
    public Guid ServerId { get; set; }
    public Server? Server { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ServerRole Role { get; set; } = ServerRole.Member;
}
