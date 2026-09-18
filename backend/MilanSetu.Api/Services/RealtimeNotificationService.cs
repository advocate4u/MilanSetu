using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MilanSetu.Api.Services;

public interface IRealtimeNotificationService
{
    Task PublishAsync(Guid userId, string type, object payload, CancellationToken cancellationToken = default);
}

public sealed class RealtimeNotificationService(IHubContext<NotificationHub> hub) : IRealtimeNotificationService
{
    public Task PublishAsync(Guid userId, string type, object payload, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(NotificationHub.UserGroup(userId)).SendAsync("notification", new { type, payload }, cancellationToken);
}

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Route = "/hubs/notifications";

    public override async Task OnConnectedAsync()
    {
        if (!Guid.TryParse(Context.User?.FindFirst("sub")?.Value, out var userId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public static string UserGroup(Guid userId) => $"user:{userId:D}";
}
