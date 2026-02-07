using Grpc.Core;

namespace Demo.GrpcService.Services;

/// <summary>
/// Implementation of the NotificationService gRPC service
/// </summary>
public class NotificationServiceImpl(ILogger<NotificationServiceImpl> logger) : NotificationService.NotificationServiceBase
{
    private static readonly Dictionary<string, List<NotificationMessage>> _userNotifications = new();
    private static int _notificationCounter = 1000;

    /// <summary>
    /// Send a single notification (Unary)
    /// </summary>
    public override Task<NotificationResponse> SendNotification(
        SendNotificationRequest request,
        ServerCallContext context)
    {
        logger.LogInformation(
            "Sending notification to {UserId}: {Title}",
            request.UserId,
            request.Title);

        var notificationId = $"NOTIF-{_notificationCounter++:D5}";

        var notification = new NotificationMessage
        {
            NotificationId = notificationId,
            UserId = request.UserId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            Priority = request.Priority,
            Timestamp = DateTime.UtcNow.ToString("o"),
            IsRead = false
        };

        // Store notification
        if (!_userNotifications.ContainsKey(request.UserId))
        {
            _userNotifications[request.UserId] = new List<NotificationMessage>();
        }
        _userNotifications[request.UserId].Add(notification);

        logger.LogInformation(
            "Notification {NotificationId} sent to {UserId}",
            notificationId,
            request.UserId);

        return Task.FromResult(new NotificationResponse
        {
            NotificationId = notificationId,
            Status = "Sent",
            SentAt = notification.Timestamp,
            DeliveryStatus = DeliveryStatus.Delivered
        });
    }

    /// <summary>
    /// Subscribe to notifications stream (Server Streaming)
    /// </summary>
    public override async Task SubscribeToNotifications(
        SubscribeRequest request,
        IServerStreamWriter<NotificationMessage> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation(
            "User {UserId} subscribing to notifications",
            request.UserId);

        // Send existing notifications
        if (_userNotifications.TryGetValue(request.UserId, out var notifications))
        {
            foreach (var notification in notifications)
            {
                // Apply filters
                if (request.Types_.Count > 0 && !request.Types_.Contains(notification.Type))
                {
                    continue;
                }

                if (notification.Priority < request.MinPriority)
                {
                    continue;
                }

                await responseStream.WriteAsync(notification);
                logger.LogInformation("Streamed notification: {NotificationId}", notification.NotificationId);
            }
        }

        // Simulate real-time notifications
        var random = new Random();
        var types = new[] { NotificationType.Info, NotificationType.OrderUpdate, NotificationType.Promotion, NotificationType.Success };
        var priorities = new[] { Priority.Normal, Priority.High, Priority.Low };

        for (int i = 0; i < 3; i++)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(1000); // Simulate delay

            var notification = new NotificationMessage
            {
                NotificationId = $"NOTIF-{_notificationCounter++:D5}",
                UserId = request.UserId,
                Title = $"Real-time notification {i + 1}",
                Body = $"This is a simulated real-time notification generated at {DateTime.UtcNow:HH:mm:ss}",
                Type = types[random.Next(types.Length)],
                Priority = priorities[random.Next(priorities.Length)],
                Timestamp = DateTime.UtcNow.ToString("o"),
                IsRead = false
            };

            // Apply filters
            if (request.Types_.Count > 0 && !request.Types_.Contains(notification.Type))
            {
                continue;
            }

            if (notification.Priority < request.MinPriority)
            {
                continue;
            }

            await responseStream.WriteAsync(notification);
            logger.LogInformation("Streamed real-time notification: {NotificationId}", notification.NotificationId);
        }

        logger.LogInformation("Subscription ended for user {UserId}", request.UserId);
    }

    /// <summary>
    /// Batch send notifications (Client Streaming)
    /// </summary>
    public override async Task<BatchNotificationResponse> BatchSendNotifications(
        IAsyncStreamReader<SendNotificationRequest> requestStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting batch notification send");

        var totalSent = 0;
        var successful = 0;
        var failed = 0;
        var notificationIds = new List<string>();

        await foreach (var request in requestStream.ReadAllAsync())
        {
            totalSent++;

            try
            {
                var notificationId = $"NOTIF-{_notificationCounter++:D5}";

                var notification = new NotificationMessage
                {
                    NotificationId = notificationId,
                    UserId = request.UserId,
                    Title = request.Title,
                    Body = request.Body,
                    Type = request.Type,
                    Priority = request.Priority,
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    IsRead = false
                };

                // Store notification
                if (!_userNotifications.ContainsKey(request.UserId))
                {
                    _userNotifications[request.UserId] = new List<NotificationMessage>();
                }
                _userNotifications[request.UserId].Add(notification);

                notificationIds.Add(notificationId);
                successful++;

                logger.LogInformation(
                    "Batch notification sent: {NotificationId} to {UserId}",
                    notificationId,
                    request.UserId);
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex, "Failed to send batch notification");
            }
        }

        logger.LogInformation(
            "Batch send complete: {Total} total, {Successful} successful, {Failed} failed",
            totalSent,
            successful,
            failed);

        return new BatchNotificationResponse
        {
            TotalSent = totalSent,
            Successful = successful,
            Failed = failed,
            NotificationIds = { notificationIds }
        };
    }

    /// <summary>
    /// Real-time notification bidirectional chat (Bidirectional Streaming)
    /// </summary>
    public override async Task NotificationChat(
        IAsyncStreamReader<ChatMessage> requestStream,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation("Starting bidirectional notification chat");

        await foreach (var message in requestStream.ReadAllAsync())
        {
            logger.LogInformation(
                "Chat message from {FromUser} to {ToUser}: {Content}",
                message.FromUser,
                message.ToUser,
                message.Content);

            // Echo back with system processing
            switch (message.MessageType)
            {
                case ChatMessageType.Text:
                    // Send delivery confirmation
                    await responseStream.WriteAsync(new ChatMessage
                    {
                        MessageId = $"MSG-{Guid.NewGuid():N}",
                        FromUser = "System",
                        ToUser = message.FromUser,
                        Content = $"Message delivered to {message.ToUser}",
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        MessageType = ChatMessageType.SystemMessage
                    });

                    // Simulate response from recipient
                    await Task.Delay(500);
                    await responseStream.WriteAsync(new ChatMessage
                    {
                        MessageId = $"MSG-{Guid.NewGuid():N}",
                        FromUser = message.ToUser,
                        ToUser = message.FromUser,
                        Content = $"Auto-reply: Received your message: '{message.Content}'",
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        MessageType = ChatMessageType.Text
                    });
                    break;

                case ChatMessageType.TypingIndicator:
                    await responseStream.WriteAsync(new ChatMessage
                    {
                        MessageId = $"MSG-{Guid.NewGuid():N}",
                        FromUser = message.ToUser,
                        ToUser = message.FromUser,
                        Content = "is typing...",
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        MessageType = ChatMessageType.TypingIndicator
                    });
                    break;

                case ChatMessageType.ReadReceipt:
                    logger.LogInformation("Read receipt for message {MessageId}", message.MessageId);
                    break;
            }
        }

        logger.LogInformation("Chat session ended");
    }
}
