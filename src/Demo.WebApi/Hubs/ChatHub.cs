using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Demo.WebApi.Hubs;

/// <summary>
/// Real-time chat hub for messaging between users
/// </summary>
[Authorize(Roles = "Admin,User")]
public class ChatHub : Hub
{
    /// <summary>
    /// Send a message to all users in the chat
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message, DateTime.UtcNow);
    }

    /// <summary>
    /// Send a private message to a specific user
    /// </summary>
    public async Task SendPrivateMessage(string targetUserId, string message)
    {
        var senderName = Context.User?.Identity?.Name ?? "Anonymous";
        
        await Clients.User(targetUserId).SendAsync("ReceivePrivateMessage", 
            senderName, 
            message, 
            DateTime.UtcNow);
            
        await Clients.Caller.SendAsync("MessageSent", targetUserId);
    }

    /// <summary>
    /// Broadcast typing indicator
    /// </summary>
    public async Task UserTyping(string userName)
    {
        await Clients.Others.SendAsync("UserIsTyping", userName);
    }

    /// <summary>
    /// Get online users with details
    /// </summary>
    public OnlineUsersInfo GetOnlineUsers()
    {
        return new OnlineUsersInfo
        {
            TotalCount = 5,
            Users =
            [
                new() { UserId = "user1", Username = "Alice", Status = "Online", LastActivity = DateTime.UtcNow.AddMinutes(-2) },
                new() { UserId = "user2", Username = "Bob", Status = "Away", LastActivity = DateTime.UtcNow.AddMinutes(-15) },
                new() { UserId = "user3", Username = "Charlie", Status = "Online", LastActivity = DateTime.UtcNow.AddSeconds(-30) },
                new() { UserId = "user4", Username = "Diana", Status = "Busy", LastActivity = DateTime.UtcNow.AddMinutes(-5) },
                new() { UserId = "user5", Username = "Eve", Status = "Online", LastActivity = DateTime.UtcNow.AddMinutes(-1) }
            ],
            ByStatus = new Dictionary<string, int>
            {
                { "Online", 3 },
                { "Away", 1 },
                { "Busy", 1 }
            }
        };
    }

    /// <summary>
    /// Get chat room statistics
    /// </summary>
    public ChatRoomStats GetChatStats()
    {
        return new ChatRoomStats
        {
            TotalMessages = 15234,
            MessagesToday = 456,
            ActiveUsers = 12,
            PeakOnlineUsers = 48,
            AverageResponseTime = TimeSpan.FromSeconds(45),
            MostActiveUser = new UserActivity
            {
                UserId = "user1",
                Username = "Alice",
                MessageCount = 892,
                LastMessage = DateTime.UtcNow.AddMinutes(-3)
            },
            PopularTimes = new Dictionary<int, int>
            {
                { 9, 25 },
                { 12, 42 },
                { 15, 38 },
                { 18, 31 }
            }
        };
    }
}

public class OnlineUsersInfo
{
    public int TotalCount { get; set; }
    public List<UserStatus> Users { get; set; } = [];
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class UserStatus
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
}

public class ChatRoomStats
{
    public int TotalMessages { get; set; }
    public int MessagesToday { get; set; }
    public int ActiveUsers { get; set; }
    public int PeakOnlineUsers { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public UserActivity MostActiveUser { get; set; } = new();
    public Dictionary<int, int> PopularTimes { get; set; } = new();
}

public class UserActivity
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int MessageCount { get; set; }
    public DateTime LastMessage { get; set; }
}
