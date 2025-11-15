using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Demo.WebApi.Hubs;

/// <summary>
/// Real-time notifications hub for sending updates to connected clients
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>
    /// Send a notification to all connected clients
    /// </summary>
    public async Task SendNotification(string message, string severity = "info")
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            Message = message,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    [Authorize]
    public async Task SendToUser(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Severity = "info",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Join a notification group
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", groupName);
    }

    /// <summary>
    /// Leave a notification group
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftGroup", groupName);
    }

    /// <summary>
    /// Send notification to a specific group
    /// </summary>
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Severity = "info",
            Group = groupName,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get server time
    /// </summary>
    public DateTime GetServerTime()
    {
        return DateTime.UtcNow;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
