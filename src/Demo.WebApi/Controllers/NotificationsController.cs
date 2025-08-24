using Microsoft.AspNetCore.Mvc;
using Demo.WebApi.Models;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private static readonly List<Notification> _notifications = [];
    private static int _nextNotificationId = 1;

    /// <summary>
    /// Gets notifications with advanced filtering using complex query parameters
    /// </summary>
    /// <param name="userId">Filter by user ID</param>
    /// <param name="types">Filter by notification types (can specify multiple)</param>
    /// <param name="priorities">Filter by priorities (can specify multiple)</param>
    /// <param name="isRead">Filter by read status</param>
    /// <param name="dateRange">Date range for filtering</param>
    /// <param name="search">Search in title and message</param>
    /// <param name="pagination">Pagination and sorting options</param>
    /// <returns>Filtered and paginated notifications</returns>
    [HttpPost("search")]
    public ActionResult<ApiResponse<List<Notification>>> SearchNotifications([FromBody] NotificationSearchRequest request)
    {
        var filteredNotifications = _notifications.AsEnumerable();

        if (request.UserId.HasValue)
            filteredNotifications = filteredNotifications.Where(n => n.UserId == request.UserId.Value);

        if (request.Types.Any())
            filteredNotifications = filteredNotifications.Where(n => request.Types.Contains(n.Type));

        if (request.Priorities.Any())
            filteredNotifications = filteredNotifications.Where(n => request.Priorities.Contains(n.Priority));

        if (request.IsRead.HasValue)
            filteredNotifications = filteredNotifications.Where(n => n.IsRead == request.IsRead.Value);

        if (request.DateRange != null)
        {
            filteredNotifications = filteredNotifications.Where(n => 
                n.CreatedAt >= request.DateRange.StartDate && 
                n.CreatedAt <= request.DateRange.EndDate);
        }

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchLower = request.Search.ToLower();
            filteredNotifications = filteredNotifications.Where(n => 
                n.Title.ToLower().Contains(searchLower) || 
                n.Message.ToLower().Contains(searchLower));
        }

        var totalCount = filteredNotifications.Count();
        var pagedNotifications = filteredNotifications
            .Skip((request.Pagination.Page - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToList();

        var response = new ApiResponse<List<Notification>>(
            Success: true,
            Data: pagedNotifications,
            Message: $"Found {totalCount} notifications, showing {pagedNotifications.Count}"
        );

        return Ok(response);
    }

    /// <summary>
    /// Creates a notification with complex validation and data
    /// </summary>
    /// <param name="request">Notification creation request with rich data</param>
    /// <returns>Created notification</returns>
    [HttpPost]
    public ActionResult<ApiResponse<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        var errors = ValidateNotificationRequest(request);
        if (errors.Any())
        {
            return BadRequest(new ApiResponse<Notification>(
                Success: false,
                Data: null,
                Message: "Validation failed",
                Errors: errors
            ));
        }

        var notification = new Notification
        {
            Id = _nextNotificationId++,
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            Priority = request.Priority,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            Data = request.Data
        };

        _notifications.Add(notification);

        return CreatedAtAction(
            nameof(GetNotification),
            new { id = notification.Id },
            new ApiResponse<Notification>(
                Success: true,
                Data: notification,
                Message: "Notification created successfully"
            )
        );
    }

    /// <summary>
    /// Gets a notification by ID with optional data expansion
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="includeData">Whether to include the full data payload</param>
    /// <returns>Notification details</returns>
    [HttpGet("{id}")]
    public ActionResult<ApiResponse<Notification>> GetNotification(int id, [FromQuery] bool includeData = true)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification == null)
        {
            return NotFound(new ApiResponse<Notification>(
                Success: false,
                Data: null,
                Message: $"Notification with ID {id} not found"
            ));
        }

        if (!includeData)
        {
            notification = new Notification
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                Priority = notification.Priority,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                Data = new Dictionary<string, object>()
            };
        }

        return Ok(new ApiResponse<Notification>(
            Success: true,
            Data: notification,
            Message: "Notification retrieved successfully"
        ));
    }

    /// <summary>
    /// Bulk operations on notifications (mark as read, delete, etc.)
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <returns>Results of the bulk operation</returns>
    [HttpPost("bulk")]
    public ActionResult<ApiResponse<BulkOperationResult>> BulkOperations([FromBody] BulkNotificationRequest request)
    {
        var results = new BulkOperationResult
        {
            TotalRequested = request.NotificationIds.Count,
            SuccessCount = 0,
            FailureCount = 0,
            Results = []
        };

        foreach (var notificationId in request.NotificationIds)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification == null)
            {
                results.Results[notificationId] = "Notification not found";
                results.FailureCount++;
                continue;
            }

            try
            {
                switch (request.Operation)
                {
                    case BulkOperation.MarkAsRead:
                        notification.IsRead = true;
                        notification.ReadAt = DateTime.UtcNow;
                        break;
                    case BulkOperation.MarkAsUnread:
                        notification.IsRead = false;
                        notification.ReadAt = null;
                        break;
                    case BulkOperation.Delete:
                        _notifications.Remove(notification);
                        break;
                    case BulkOperation.UpdatePriority:
                        if (request.NewPriority.HasValue)
                            notification.Priority = request.NewPriority.Value;
                        break;
                }

                results.Results[notificationId] = "Success";
                results.SuccessCount++;
            }
            catch (Exception ex)
            {
                results.Results[notificationId] = $"Error: {ex.Message}";
                results.FailureCount++;
            }
        }

        return Ok(new ApiResponse<BulkOperationResult>(
            Success: results.SuccessCount > 0,
            Data: results,
            Message: $"Bulk operation completed: {results.SuccessCount} successful, {results.FailureCount} failed"
        ));
    }

    /// <summary>
    /// Gets notification statistics grouped by various dimensions
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="groupBy">Group statistics by type, priority, or date</param>
    /// <param name="period">Time period for statistics</param>
    /// <returns>Notification statistics</returns>
    [HttpGet("statistics")]
    public ActionResult<ApiResponse<NotificationStatistics>> GetStatistics(
        [FromQuery] int? userId = null,
        [FromQuery] string groupBy = "type",
        [FromQuery] int period = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-period);
        var filteredNotifications = _notifications
            .Where(n => n.CreatedAt >= startDate)
            .ToList();

        if (userId.HasValue)
        {
            filteredNotifications = filteredNotifications
                .Where(n => n.UserId == userId.Value)
                .ToList();
        }

        var statistics = new NotificationStatistics
        {
            TotalNotifications = filteredNotifications.Count,
            ReadNotifications = filteredNotifications.Count(n => n.IsRead),
            UnreadNotifications = filteredNotifications.Count(n => !n.IsRead),
            Period = new DateRange
            {
                StartDate = startDate,
                EndDate = DateTime.UtcNow
            },
            ByType = filteredNotifications
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByPriority = filteredNotifications
                .GroupBy(n => n.Priority)
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageResponseTime = TimeSpan.FromHours(2.5), // Mock data
            DailyTrends = GenerateDailyTrends(filteredNotifications, startDate)
        };

        return Ok(new ApiResponse<NotificationStatistics>(
            Success: true,
            Data: statistics,
            Message: "Statistics generated successfully"
        ));
    }

    /// <summary>
    /// Advanced notification templating with dynamic content
    /// </summary>
    /// <param name="request">Template processing request</param>
    /// <returns>Processed notifications ready to send</returns>
    [HttpPost("process-template")]
    public ActionResult<ApiResponse<List<ProcessedNotification>>> ProcessTemplate(
        [FromBody] NotificationTemplateRequest request)
    {
        var processedNotifications = new List<ProcessedNotification>();

        foreach (var recipientData in request.Recipients)
        {
            try
            {
                var processedTitle = ProcessTemplate(request.Template.Title, recipientData.Data);
                var processedMessage = ProcessTemplate(request.Template.Message, recipientData.Data);

                var processed = new ProcessedNotification
                {
                    UserId = recipientData.UserId,
                    Title = processedTitle,
                    Message = processedMessage,
                    Type = request.Template.Type,
                    Priority = request.Template.Priority,
                    Data = MergeData(request.Template.Data, recipientData.Data),
                    Status = "Ready"
                };

                processedNotifications.Add(processed);
            }
            catch (Exception ex)
            {
                processedNotifications.Add(new ProcessedNotification
                {
                    UserId = recipientData.UserId,
                    Status = $"Error: {ex.Message}"
                });
            }
        }

        return Ok(new ApiResponse<List<ProcessedNotification>>(
            Success: true,
            Data: processedNotifications,
            Message: $"Processed {processedNotifications.Count} notification templates"
        ));
    }

    private static Dictionary<string, string[]> ValidateNotificationRequest(CreateNotificationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["Title"] = ["Title is required"];

        if (string.IsNullOrWhiteSpace(request.Message))
            errors["Message"] = ["Message is required"];

        if (request.UserId <= 0)
            errors["UserId"] = ["Valid User ID is required"];

        return errors;
    }

    private static List<DailyNotificationTrend> GenerateDailyTrends(List<Notification> notifications, DateTime startDate)
    {
        var trends = new List<DailyNotificationTrend>();
        var currentDate = startDate.Date;
        var endDate = DateTime.UtcNow.Date;

        while (currentDate <= endDate)
        {
            var dayNotifications = notifications.Where(n => n.CreatedAt.Date == currentDate).ToList();

            trends.Add(new DailyNotificationTrend
            {
                Date = currentDate,
                Total = dayNotifications.Count,
                Read = dayNotifications.Count(n => n.IsRead),
                Unread = dayNotifications.Count(n => !n.IsRead),
                ByType = dayNotifications.GroupBy(n => n.Type).ToDictionary(g => g.Key, g => g.Count())
            });

            currentDate = currentDate.AddDays(1);
        }

        return trends;
    }

    private static string ProcessTemplate(string template, Dictionary<string, object> data)
    {
        var result = template;
        
        foreach (var (key, value) in data)
        {
            var placeholder = $"{{{key}}}";
            result = result.Replace(placeholder, value?.ToString() ?? "");
        }

        return result;
    }

    private static Dictionary<string, object> MergeData(Dictionary<string, object> template, Dictionary<string, object> recipient)
    {
        var merged = new Dictionary<string, object>(template);
        
        foreach (var (key, value) in recipient)
        {
            merged[key] = value;
        }

        return merged;
    }
}

// Additional models for the Notifications controller
public class NotificationSearchRequest
{
    public int? UserId { get; set; }
    public List<NotificationType> Types { get; set; } = [];
    public List<Priority> Priorities { get; set; } = [];
    public bool? IsRead { get; set; }
    public DateRange? DateRange { get; set; }
    public string? Search { get; set; }
    public PaginationRequest Pagination { get; set; } = new();
}

public class BulkNotificationRequest
{
    public List<int> NotificationIds { get; set; } = [];
    public BulkOperation Operation { get; set; }
    public Priority? NewPriority { get; set; }
}

public enum BulkOperation
{
    MarkAsRead,
    MarkAsUnread,
    Delete,
    UpdatePriority
}

public class BulkOperationResult
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public Dictionary<int, string> Results { get; set; } = new();
}

public class NotificationStatistics
{
    public int TotalNotifications { get; set; }
    public int ReadNotifications { get; set; }
    public int UnreadNotifications { get; set; }
    public DateRange Period { get; set; } = new();
    public Dictionary<NotificationType, int> ByType { get; set; } = new();
    public Dictionary<Priority, int> ByPriority { get; set; } = new();
    public TimeSpan AverageResponseTime { get; set; }
    public List<DailyNotificationTrend> DailyTrends { get; set; } = [];
}

public class DailyNotificationTrend
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Read { get; set; }
    public int Unread { get; set; }
    public Dictionary<NotificationType, int> ByType { get; set; } = new();
}

public class NotificationTemplateRequest
{
    public NotificationTemplate Template { get; set; } = new();
    public List<TemplateRecipient> Recipients { get; set; } = [];
}

public class NotificationTemplate
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class TemplateRecipient
{
    public int UserId { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public class ProcessedNotification
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Priority Priority { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
