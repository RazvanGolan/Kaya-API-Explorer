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
    /// Get server time with details
    /// </summary>
    public ServerInfo GetServerInfo()
    {
        return new ServerInfo
        {
            CurrentTime = DateTime.UtcNow,
            ServerName = Environment.MachineName,
            Platform = Environment.OSVersion.Platform.ToString(),
            Version = Environment.Version.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        };
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    public NotificationStats GetNotificationStats()
    {
        return new NotificationStats
        {
            TotalSent = 12543,
            TotalDelivered = 12480,
            TotalFailed = 63,
            AverageDeliveryTime = TimeSpan.FromMilliseconds(245),
            TopRecipients = new List<RecipientInfo>
            {
                new() { UserId = "user1", NotificationCount = 125 },
                new() { UserId = "user2", NotificationCount = 98 },
                new() { UserId = "user3", NotificationCount = 87 }
            },
            ByType = new Dictionary<string, int>
            {
                { "Info", 8500 },
                { "Warning", 3200 },
                { "Error", 843 }
            }
        };
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

public class ServerInfo
{
    public DateTime CurrentTime { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public TimeSpan Uptime { get; set; }
}

public class NotificationStats
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public TimeSpan AverageDeliveryTime { get; set; }
    public List<RecipientInfo> TopRecipients { get; set; } = [];
    public Dictionary<string, int> ByType { get; set; } = new();
}

public class RecipientInfo
{
    public string UserId { get; set; } = string.Empty;
    public int NotificationCount { get; set; }
}