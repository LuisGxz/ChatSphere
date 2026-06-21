using ChatSphere.Api.Hubs;
using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Models;
using ChatSphere.Domain.Enums;
using ChatSphere.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatSphere.Api.Demo;

/// <summary>
/// Makes the live demo feel alive for a solo visitor: seeded "teammates" periodically type and post
/// ephemeral messages into the public channels. These are broadcast over SignalR but never persisted —
/// they're ambient activity, not real history, so the database stays clean.
/// </summary>
public sealed class DemoActivityService(
    IHubContext<ChatHub> hub,
    IServiceScopeFactory scopes,
    ILogger<DemoActivityService> logger) : BackgroundService
{
    private static readonly string[] Lines =
    [
        "shipping the empty-state illustrations now 🎨",
        "anyone got eyes on the toast spacing PR?",
        "the new onboarding numbers look great 📈",
        "pushed a fix for the avatar flicker",
        "deploy is green ✅",
        "can someone double-check the dark-mode contrast?",
        "standup in 5 ⏰",
        "loving the new reactions tbh",
        "moved the settings search to the top",
        "coffee run — anyone? ☕",
    ];

    private record Bot(Guid Id, string Name, string Color, string? Title);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(6), ct);

        List<Bot> bots;
        List<Guid> channels;
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ChatSphereDbContext>();
            // The "teammates" are the seeded users that aren't the headline demo logins.
            var demoLogins = new[] { "luis@chatsphere.app", "priya@chatsphere.app", "marcus@chatsphere.app" };
            bots = await db.Users
                .Where(u => !demoLogins.Contains(u.Email))
                .Select(u => new Bot(u.Id, u.DisplayName, u.AvatarColor, u.Title))
                .ToListAsync(ct);
            channels = await db.Channels
                .Where(c => c.ServerId != null && !c.IsPrivate)
                .Select(c => c.Id)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Demo activity simulator could not load seed data; disabling.");
            return;
        }

        if (bots.Count == 0 || channels.Count == 0)
            return;

        var rng = new Random();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(rng.Next(9, 22)), ct);

                var bot = bots[rng.Next(bots.Count)];
                var channelId = channels[rng.Next(channels.Count)];
                var group = hub.Clients.Group($"channel:{channelId}");

                await group.SendAsync("UserTyping", new TypingDto(channelId, bot.Id, bot.Name), ct);
                await Task.Delay(TimeSpan.FromMilliseconds(rng.Next(1400, 2800)), ct);

                var message = new MessageDto(
                    Guid.NewGuid(), channelId,
                    new UserMiniDto(bot.Id, bot.Name, bot.Color, bot.Title),
                    Lines[rng.Next(Lines.Length)], MessageType.Text,
                    DateTimeOffset.UtcNow, null, [], [], false);
                await group.SendAsync("ReceiveMessage", message, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Demo activity tick failed; continuing.");
            }
        }
    }
}
