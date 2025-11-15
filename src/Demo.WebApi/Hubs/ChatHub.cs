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
    /// Get online users count
    /// </summary>
    public int GetOnlineUsersCount()
    {
        // This is a simple example; in production, you'd track this properly
        return 5;
    }
}
