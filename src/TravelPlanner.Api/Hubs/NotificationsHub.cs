using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TravelPlanner.Api.Hubs;

/// <summary>Каждый пользователь получает сообщения только в свою группу user:{id}.</summary>
[Authorize]
public sealed class NotificationsHub : Hub
{
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var raw = Context.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(raw, out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }
}