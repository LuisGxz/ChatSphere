using ChatSphere.Domain.Entities;
using ChatSphere.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Infrastructure.Data;

/// <summary>
/// Deterministic demo data mirroring the design mockup: the "Driftwood Studio" server, eight people,
/// five channels, the #design-crit conversation with reactions, a DM with unreads, and read cursors.
/// Every account's password is <c>Password123!</c>.
/// </summary>
public static class DataSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(ChatSphereDbContext db, DateTimeOffset now, CancellationToken ct = default)
    {
        if (await db.Servers.AnyAsync(ct))
            return;

        var hasher = new PasswordHasher<User>();
        User NewUser(string email, string name, string color, string title)
        {
            var u = new User { Email = email, DisplayName = name, AvatarColor = color, Title = title };
            u.PasswordHash = hasher.HashPassword(u, DemoPassword);
            return u;
        }

        var luis = NewUser("luis@chatsphere.app", "Luis Chiquito", "#7C6CF0", "design lead");
        var priya = NewUser("priya@chatsphere.app", "Priya Raman", "#B05CCB", "product designer");
        var marcus = NewUser("marcus@chatsphere.app", "Marcus Bell", "#3E8FD9", "frontend");
        var dana = NewUser("dana@chatsphere.app", "Dana Wolfe", "#4CAF82", "pm");
        var sam = NewUser("sam@chatsphere.app", "Sam Ortiz", "#C8537B", "frontend");
        var yuki = NewUser("yuki@chatsphere.app", "Yuki Tanaka", "#D9763E", "illustrator");
        var noor = NewUser("noor@chatsphere.app", "Noor Haddad", "#5BA8A0", "researcher");
        var felix = NewUser("felix@chatsphere.app", "Felix Gray", "#8A8F9E", "backend");
        var users = new[] { luis, priya, marcus, dana, sam, yuki, noor, felix };
        db.Users.AddRange(users);

        var server = new Server { Name = "Driftwood Studio", Slug = "driftwood-studio", OwnerId = luis.Id };
        db.Servers.Add(server);

        // Roles: Luis owner, Priya admin, the rest members.
        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = luis.Id, Role = ServerRole.Owner });
        db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = priya.Id, Role = ServerRole.Admin });
        foreach (var u in new[] { marcus, dana, sam, yuki, noor, felix })
            db.ServerMembers.Add(new ServerMember { ServerId = server.Id, UserId = u.Id, Role = ServerRole.Member });

        Channel NewChannel(string name, string topic, int pos) =>
            new() { ServerId = server.Id, Name = name, Topic = topic, Position = pos };

        var announcements = NewChannel("announcements", "Studio-wide news", 0);
        var designCrit = NewChannel("design-crit", "Design feedback — ruthless but kind", 1);
        var engFrontend = NewChannel("eng-frontend", "Frontend engineering", 2);
        var product = NewChannel("product", "Roadmap & priorities", 3);
        var random = NewChannel("random", "Off-topic", 4);
        var channels = new[] { announcements, designCrit, engFrontend, product, random };
        db.Channels.AddRange(channels);

        // Everyone is a member of every public channel.
        foreach (var c in channels)
            foreach (var u in users)
                db.ChannelMembers.Add(new ChannelMember { ChannelId = c.Id, UserId = u.Id });

        // ── #design-crit conversation (read by everyone) ───────────────────────
        Message Msg(Channel c, User author, string body, DateTimeOffset at) =>
            new() { ChannelId = c.Id, AuthorId = author.Id, Body = body, CreatedAt = at };

        var dc1 = Msg(designCrit, priya, "Dropped v3 of the onboarding flow in Figma. Killed the carousel — it tested terribly.", now.AddMinutes(-78));
        var dc2 = Msg(designCrit, priya, "The progress dots are now actual steps you can tap.", now.AddMinutes(-77));
        var dc3 = Msg(designCrit, marcus, "Huge improvement. One nit: step 3 copy still says “finish setup” but it's optional now, right?", now.AddMinutes(-69));
        var dc4 = Msg(designCrit, luis, "Good catch — it's optional. I'll rewrite it as “explore your workspace” and link the tour.", now.AddMinutes(-66));
        db.Messages.AddRange(dc1, dc2, dc3, dc4);

        db.Reactions.AddRange(
            new Reaction { MessageId = dc2.Id, UserId = luis.Id, Emoji = "👏" },
            new Reaction { MessageId = dc2.Id, UserId = marcus.Id, Emoji = "👏" },
            new Reaction { MessageId = dc2.Id, UserId = dana.Id, Emoji = "👏" },
            new Reaction { MessageId = dc2.Id, UserId = sam.Id, Emoji = "👏" },
            new Reaction { MessageId = dc2.Id, UserId = priya.Id, Emoji = "🔥" },
            new Reaction { MessageId = dc2.Id, UserId = yuki.Id, Emoji = "🔥" },
            new Reaction { MessageId = dc4.Id, UserId = priya.Id, Emoji = "✅" },
            new Reaction { MessageId = dc4.Id, UserId = marcus.Id, Emoji = "✅" },
            new Reaction { MessageId = dc4.Id, UserId = dana.Id, Emoji = "✅" });

        // ── #announcements ─────────────────────────────────────────────────────
        var an1 = Msg(announcements, luis, "Welcome to Driftwood Studio 👋 Keep #design-crit kind and #random weird.", now.AddDays(-2));
        db.Messages.Add(an1);

        // ── #eng-frontend (leaves Luis with unread + a mention) ────────────────
        var ef1 = Msg(engFrontend, marcus, "Bumped Angular to 20.3 — signals everywhere now.", now.AddMinutes(-40));
        var ef2 = Msg(engFrontend, sam, "Nice. The drag-and-drop board feels way snappier.", now.AddMinutes(-35));
        var ef3 = Msg(engFrontend, marcus, "@Luis Chiquito can you review the design tokens PR when you get a sec?", now.AddMinutes(-12));
        db.Messages.AddRange(ef1, ef2, ef3);
        db.Mentions.Add(new Mention { MessageId = ef3.Id, MentionedUserId = luis.Id });

        // ── Direct message: Priya ↔ Luis (Luis has 2 unread) ───────────────────
        var dm = new Channel { IsDirect = true, Position = 0 };
        db.Channels.Add(dm);
        var dmLuis = new ChannelMember { ChannelId = dm.Id, UserId = luis.Id };
        var dmPriya = new ChannelMember { ChannelId = dm.Id, UserId = priya.Id };
        db.ChannelMembers.AddRange(dmLuis, dmPriya);
        var dm1 = Msg(dm, luis, "Thanks for the onboarding rework — looks great.", now.AddMinutes(-30));
        var dm2 = Msg(dm, priya, "🙏 want to pair on the empty states tomorrow?", now.AddMinutes(-9));
        var dm3 = Msg(dm, priya, "Also — did you see Marcus' token PR?", now.AddMinutes(-8));
        db.Messages.AddRange(dm1, dm2, dm3);

        await db.SaveChangesAsync(ct);

        // ── Read cursors ───────────────────────────────────────────────────────
        // Everyone has read #design-crit up to the last message.
        await SetReadAsync(db, designCrit.Id, dc4.Id, now, ct);
        await SetReadAsync(db, announcements.Id, an1.Id, now, ct);
        // Luis hasn't read the latest eng-frontend messages → unread + a pending mention.
        await SetReadAsync(db, engFrontend.Id, ef1.Id, now, ct, onlyUser: luis.Id);
        // Luis read his own DM line but not Priya's two follow-ups → DM unread = 2.
        dmLuis.LastReadMessageId = dm1.Id;
        dmLuis.LastReadAt = now.AddMinutes(-30);
        dmPriya.LastReadMessageId = dm3.Id;
        dmPriya.LastReadAt = now;

        await db.SaveChangesAsync(ct);
    }

    private static async Task SetReadAsync(ChatSphereDbContext db, Guid channelId, Guid lastMessageId,
        DateTimeOffset now, CancellationToken ct, Guid? onlyUser = null)
    {
        // The read cursor's timestamp is the read message's own CreatedAt, so "unread" = anything after it.
        var readAt = await db.Messages.Where(m => m.Id == lastMessageId).Select(m => m.CreatedAt).FirstAsync(ct);
        var members = await db.ChannelMembers
            .Where(m => m.ChannelId == channelId && (onlyUser == null || m.UserId == onlyUser))
            .ToListAsync(ct);
        foreach (var m in members)
        {
            m.LastReadMessageId = lastMessageId;
            m.LastReadAt = readAt;
        }
    }
}
