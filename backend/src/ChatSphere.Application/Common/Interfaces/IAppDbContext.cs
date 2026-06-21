using ChatSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Application.Common.Interfaces;

/// <summary>Abstraction over the persistence context so Application services stay free of EF wiring.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Server> Servers { get; }
    DbSet<ServerMember> ServerMembers { get; }
    DbSet<Channel> Channels { get; }
    DbSet<ChannelMember> ChannelMembers { get; }
    DbSet<Message> Messages { get; }
    DbSet<Reaction> Reactions { get; }
    DbSet<Mention> Mentions { get; }
    DbSet<Attachment> Attachments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
