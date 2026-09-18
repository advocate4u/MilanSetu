using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MilanSetu.Api.Services;

public interface IRealtimeNotificationService
{
    Task PublishAsync(Guid userId, string type, object payload, CancellationToken cancellationToken = default);
}

public sealed class RealtimeNotificationService(IHubContext<NotificationHub> hub, ILogger<RealtimeNotificationService> logger) : IRealtimeNotificationService
{
    public async Task PublishAsync(Guid userId, string type, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await hub.Clients.Group(NotificationHub.UserGroup(userId))
                .SendAsync("notification", new { type, payload }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The database write has already completed; a disconnected client should not turn it into a failed API request.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Realtime notification delivery failed for user {UserId} and event {EventType}.", userId, type);
        }
    }
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
