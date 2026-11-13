using Microsoft.AspNetCore.SignalR;
using TravelPlanner.Api.Hubs;
using TravelPlanner.Core.Abstractions;
using TravelPlanner.Core.Contracts;

namespace TravelPlanner.Api.Realtime;

public sealed class SignalRNotificationService(IHubContext<NotificationsHub> hub) : INotificationService
{
    public Task NotifyAsync(Guid userId, NotificationDto notification, CancellationToken ct) =>
        hub.Clients.Group(NotificationsHub.UserGroup(userId)).SendAsync("notification", notification, ct);
}