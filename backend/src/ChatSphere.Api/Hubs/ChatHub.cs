using System.IdentityModel.Tokens.Jwt;
using ChatSphere.Application.Chat;
using ChatSphere.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatSphere.Api.Hubs;

/// <summary>
/// Real-time chat over WebSockets. On connect, a connection joins a group per channel it belongs to
/// (<c>channel:{id}</c>), per server (<c>server:{id}</c>, for presence fan-out) and its own
/// (<c>user:{id}</c>, for mentions). Writes persist via the Application services, then fan out to groups.
/// </summary>
[Authorize]
public sealed class ChatHub(IChatWriteService write, IChatService chat, IPresenceTracker presence) : Hub
{
    private Guid UserId =>
        Guid.TryParse(Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new HubException("Not authenticated.");

    private string DisplayName => Context.User?.FindFirst("name")?.Value ?? "Someone";

    private static string Channel(Guid id) => $"channel:{id}";
    private static string Server(Guid id) => $"server:{id}";
    private static string UserGroup(Guid id) => $"user:{id}";

    public override async Task OnConnectedAsync()
    {
        var userId = UserId;

        foreach (var channelId in await write.GetMemberChannelIdsAsync(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, Channel(channelId));

        var serverGroups = (await write.GetMemberServerIdsAsync(userId)).Select(Server).ToArray();
        foreach (var g in serverGroups)
            await Groups.AddToGroupAsync(Context.ConnectionId, g);

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        if (await presence.ConnectAsync(userId, Context.ConnectionId) && serverGroups.Length > 0)
            await Clients.Groups(serverGroups).SendAsync("PresenceChanged", new PresenceDto(userId, true));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserId;
        if (await presence.DisconnectAsync(userId, Context.ConnectionId))
        {
            var serverGroups = (await write.GetMemberServerIdsAsync(userId)).Select(Server).ToArray();
            if (serverGroups.Length > 0)
                await Clients.Groups(serverGroups).SendAsync("PresenceChanged", new PresenceDto(userId, false));
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<MessageDto> SendMessage(Guid channelId, string body)
    {
        var result = await write.SendMessageAsync(UserId, channelId, body);
        await Clients.Group(Channel(channelId)).SendAsync("ReceiveMessage", result.Message);
        foreach (var mentioned in result.MentionedUserIds)
            await Clients.Group(UserGroup(mentioned))
                .SendAsync("MentionReceived", new MentionNotificationDto(channelId, null, result.Message));
        return result.Message;
    }

    public async Task ToggleReaction(Guid messageId, string emoji)
    {
        var state = await write.ToggleReactionAsync(UserId, messageId, emoji);
        await Clients.Group(Channel(state.ChannelId)).SendAsync("ReactionUpdated", state);
    }

    public async Task StartTyping(Guid channelId)
        => await Clients.OthersInGroup(Channel(channelId))
            .SendAsync("UserTyping", new TypingDto(channelId, UserId, DisplayName));

    public async Task MarkRead(Guid channelId, Guid lastMessageId)
    {
        await chat.MarkReadAsync(UserId, channelId, lastMessageId);
        await Clients.OthersInGroup(Channel(channelId))
            .SendAsync("MessageRead", new ReadReceiptDto(channelId, UserId, lastMessageId, DateTimeOffset.UtcNow));
    }

    /// <summary>Of the supplied user ids (e.g. the current server's members), which are online right now.</summary>
    public async Task<IReadOnlyCollection<Guid>> WhoIsOnline(Guid[] userIds)
        => await presence.FilterOnlineAsync(userIds);
}
