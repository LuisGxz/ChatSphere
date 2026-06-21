using ChatSphere.Application.Common.Interfaces;
using ChatSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Infrastructure.Data;

public class ChatSphereDbContext(DbContextOptions<ChatSphereDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMember> ServerMembers => Set<ServerMember>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Mention> Mentions => Set<Mention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            e.Property(x => x.AvatarColor).HasMaxLength(9).IsRequired();
            e.Property(x => x.Title).HasMaxLength(80);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Server>(e =>
        {
            e.ToTable("servers");
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<ServerMember>(e =>
        {
            e.ToTable("server_members");
            e.HasIndex(x => new { x.ServerId, x.UserId }).IsUnique();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Server).WithMany(s => s.Members).HasForeignKey(x => x.ServerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.ServerMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Channel>(e =>
        {
            e.ToTable("channels");
            e.HasIndex(x => new { x.ServerId, x.Position });
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Topic).HasMaxLength(500);
            e.HasOne(x => x.Server).WithMany(s => s.Channels).HasForeignKey(x => x.ServerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ChannelMember>(e =>
        {
            e.ToTable("channel_members");
            e.HasIndex(x => new { x.ChannelId, x.UserId }).IsUnique();
            e.HasOne(x => x.Channel).WithMany(c => c.Members).HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            // Read cursor points at a message; NoAction avoids a second cascade path into messages.
            e.HasOne<Message>().WithMany().HasForeignKey(x => x.LastReadMessageId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasIndex(x => new { x.ChannelId, x.CreatedAt });
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Channel).WithMany(c => c.Messages).HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Reaction>(e =>
        {
            e.ToTable("reactions");
            e.HasIndex(x => new { x.MessageId, x.UserId, x.Emoji }).IsUnique();
            e.Property(x => x.Emoji).HasMaxLength(32).IsRequired();
            e.HasOne(x => x.Message).WithMany(m => m.Reactions).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Mention>(e =>
        {
            e.ToTable("mentions");
            e.HasIndex(x => new { x.MessageId, x.MentionedUserId }).IsUnique();
            e.HasIndex(x => x.MentionedUserId);
            e.HasOne(x => x.Message).WithMany(m => m.Mentions).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MentionedUser).WithMany().HasForeignKey(x => x.MentionedUserId).OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Attachment>(e =>
        {
            e.ToTable("attachments");
            e.HasIndex(x => x.MessageId);
            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Message).WithMany(m => m.Attachments).HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
