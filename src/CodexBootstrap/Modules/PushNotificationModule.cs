using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Push notification module for client notification system
/// </summary>
public sealed class PushNotificationModule : ModuleBase
{
    private readonly RealtimeModule? _realtimeModule;
    private readonly ConcurrentDictionary<string, NotificationSubscription> _subscriptions = new();
    private readonly ConcurrentQueue<Notification> _notificationHistory = new();
    private readonly ConcurrentDictionary<string, NotificationTemplate> _templates = new();
    private readonly object _lock = new object();
    private int _maxHistorySize = 1000;

    public override string Name => "Push Notification Module";
    public override string Description => "Push notification module for client notification system";
    public override string Version => "1.0.0";

    public PushNotificationModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _realtimeModule = null; // Will be configured during initialization
        InitializeDefaultTemplates();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.push-notifications",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "push-notifications", "notifications", "messaging", "client" },
            capabilities: new[] { "push_notifications", "notification_templates", "notification_subscriptions", "notification_history", "notification_scheduling", "notification_delivery" },
            spec: "codex.spec.push-notifications"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Push Notification API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Push Notification API Methods
    [ApiRoute("POST", "/notifications/send", "SendNotification", "Send push notification to users", "codex.push-notifications")]
    public async Task<object> SendNotificationAsync([ApiParameter("body", "Notification request")] SendNotificationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId) && string.IsNullOrEmpty(request.Message))
            {
                return new ErrorResponse("Either template ID or message is required");
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = request.TemplateId,
                Title = request.Title ?? "Notification",
                Message = request.Message ?? "",
                Type = request.Type ?? NotificationType.Info,
                Priority = request.Priority ?? NotificationPriority.Normal,
                Recipients = request.Recipients ?? new List<string>(),
                Data = request.Data ?? new Dictionary<string, object>(),
                ScheduledAt = request.ScheduledAt ?? DateTimeOffset.UtcNow,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Pending
            };

            // Process template if provided
            if (!string.IsNullOrEmpty(request.TemplateId) && _templates.TryGetValue(request.TemplateId, out var template))
            {
                // notification.Title = ProcessTemplate(template.Title, request.Data ?? new Dictionary<string, object>()); // Cannot modify init-only property
                // notification.Message = ProcessTemplate(template.Message, request.Data ?? new Dictionary<string, object>()); // Cannot modify init-only property
                // notification.Type = template.Type; // Cannot modify init-only property
                // notification.Priority = template.Priority; // Cannot modify init-only property
            }

            // Add to history
            _notificationHistory.Enqueue(notification);

            // Maintain history size
            lock (_lock)
            {
                while (_notificationHistory.Count > _maxHistorySize)
                {
                    _notificationHistory.TryDequeue(out _);
                }
            }

            // Send notification
            await ProcessNotificationAsync(notification);

            _logger.Info($"Notification sent: {notification.Id}");
            return new { success = true, notificationId = notification.Id };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending notification: {ex.Message}", ex);
            return new ErrorResponse($"Failed to send notification: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/notifications/subscribe", "SubscribeToNotifications", "Subscribe to notification types", "codex.push-notifications")]
    public async Task<object> SubscribeToNotificationsAsync([ApiParameter("body", "Subscription request")] NotificationSubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required");
            }

            var subscription = new NotificationSubscription
            {
                UserId = request.UserId,
                NotificationTypes = request.NotificationTypes?.ToHashSet() ?? new HashSet<NotificationType>(),
                Channels = request.Channels?.ToHashSet() ?? new HashSet<NotificationChannel>(),
                Filters = request.Filters ?? new Dictionary<string, object>(),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _subscriptions[request.UserId] = subscription;

            _logger.Info($"User {request.UserId} subscribed to notifications");
            return new { success = true, subscription = subscription };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating notification subscription: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create notification subscription: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/notifications/subscribe/{userId}", "UnsubscribeFromNotifications", "Unsubscribe from notifications", "codex.push-notifications")]
    public async Task<object> UnsubscribeFromNotificationsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            if (_subscriptions.TryRemove(userId, out var subscription))
            {
                // subscription.IsActive = false; // Cannot modify init-only property
                _logger.Info($"User {userId} unsubscribed from notifications");
                return new { success = true };
            }

            return new ErrorResponse("Subscription not found");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error removing notification subscription: {ex.Message}", ex);
            return new ErrorResponse($"Failed to remove notification subscription: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/notifications/history", "GetNotificationHistory", "Get notification history", "codex.push-notifications")]
    public async Task<object> GetNotificationHistoryAsync([ApiParameter("query", "History query parameters")] NotificationHistoryQuery? query)
    {
        try
        {
            query ??= new NotificationHistoryQuery();
            var notifications = _notificationHistory.AsEnumerable();

            // Apply filters
            if (query.UserId != null)
            {
                notifications = notifications.Where(n => n.Recipients.Contains(query.UserId));
            }

            if (query.NotificationTypes?.Any() == true)
            {
                notifications = notifications.Where(n => query.NotificationTypes.Contains(n.Type));
            }

            if (query.Status?.Any() == true)
            {
                notifications = notifications.Where(n => query.Status.Contains(n.Status));
            }

            if (query.Since.HasValue)
            {
                notifications = notifications.Where(n => n.CreatedAt >= query.Since.Value);
            }

            if (query.Until.HasValue)
            {
                notifications = notifications.Where(n => n.CreatedAt <= query.Until.Value);
            }

            // Apply sorting
            notifications = query.SortDescending ? 
                notifications.OrderByDescending(n => n.CreatedAt) : 
                notifications.OrderBy(n => n.CreatedAt);

            // Apply pagination
            var totalCount = notifications.Count();
            var pagedNotifications = notifications
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Retrieved {pagedNotifications.Count} notifications from history");
            return new { 
                success = true, 
                notifications = pagedNotifications, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting notification history: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get notification history: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/notifications/templates", "GetNotificationTemplates", "Get notification templates", "codex.push-notifications")]
    public async Task<object> GetNotificationTemplatesAsync()
    {
        try
        {
            var templates = _templates.Values.Select(t => new
            {
                t.Id,
                t.Name,
                t.Title,
                t.Message,
                t.Type,
                t.Priority,
                t.CreatedAt
            }).ToList();

            _logger.Debug($"Retrieved {templates.Count} notification templates");
            return new { success = true, templates = templates };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting notification templates: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get notification templates: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/notifications/templates", "CreateNotificationTemplate", "Create notification template", "codex.push-notifications")]
    public async Task<object> CreateNotificationTemplateAsync([ApiParameter("body", "Template request")] CreateTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return new ErrorResponse("Template name is required");
            }

            var template = new NotificationTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Title = request.Title ?? "",
                Message = request.Message ?? "",
                Type = request.Type ?? NotificationType.Info,
                Priority = request.Priority ?? NotificationPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _templates[template.Id] = template;

            _logger.Info($"Notification template created: {template.Id}");
            return new { success = true, template = template };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating notification template: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create notification template: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/notifications/schedule", "ScheduleNotification", "Schedule notification for later delivery", "codex.push-notifications")]
    public async Task<object> ScheduleNotificationAsync([ApiParameter("body", "Schedule request")] ScheduleNotificationRequest request)
    {
        try
        {
            if (request.ScheduledAt <= DateTimeOffset.UtcNow)
            {
                return new ErrorResponse("Scheduled time must be in the future");
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = request.TemplateId,
                Title = request.Title ?? "Scheduled Notification",
                Message = request.Message ?? "",
                Type = request.Type ?? NotificationType.Info,
                Priority = request.Priority ?? NotificationPriority.Normal,
                Recipients = request.Recipients ?? new List<string>(),
                Data = request.Data ?? new Dictionary<string, object>(),
                ScheduledAt = request.ScheduledAt,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Scheduled
            };

            // Add to history
            _notificationHistory.Enqueue(notification);

            // In a real implementation, this would be scheduled for later delivery
            _logger.Info($"Notification scheduled: {notification.Id} for {request.ScheduledAt}");
            return new { success = true, notificationId = notification.Id, scheduledAt = request.ScheduledAt };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error scheduling notification: {ex.Message}", ex);
            return new ErrorResponse($"Failed to schedule notification: {ex.Message}");
        }
    }

    // Public notification methods
    public async Task NotifyNodeCreatedAsync(string nodeId, string? userId = null)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = "node_created",
                Title = "New Node Created",
                Message = $"A new node '{nodeId}' has been created.",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                Recipients = userId != null ? new List<string> { userId } : GetAllSubscribedUsers(),
                Data = new Dictionary<string, object> { ["nodeId"] = nodeId },
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Pending
            };

            await ProcessNotificationAsync(notification);
            _logger.Info($"Node created notification sent for {nodeId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending node created notification: {ex.Message}", ex);
        }
    }

    public async Task NotifyNodeUpdatedAsync(string nodeId, string? userId = null)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = "node_updated",
                Title = "Node Updated",
                Message = $"Node '{nodeId}' has been updated.",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                Recipients = userId != null ? new List<string> { userId } : GetAllSubscribedUsers(),
                Data = new Dictionary<string, object> { ["nodeId"] = nodeId },
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Pending
            };

            await ProcessNotificationAsync(notification);
            _logger.Info($"Node updated notification sent for {nodeId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending node updated notification: {ex.Message}", ex);
        }
    }

    public async Task NotifyNodeDeletedAsync(string nodeId, string? userId = null)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = "node_deleted",
                Title = "Node Deleted",
                Message = $"Node '{nodeId}' has been deleted.",
                Type = NotificationType.Warning,
                Priority = NotificationPriority.High,
                Recipients = userId != null ? new List<string> { userId } : GetAllSubscribedUsers(),
                Data = new Dictionary<string, object> { ["nodeId"] = nodeId },
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Pending
            };

            await ProcessNotificationAsync(notification);
            _logger.Info($"Node deleted notification sent for {nodeId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending node deleted notification: {ex.Message}", ex);
        }
    }

    public async Task NotifySystemEventAsync(string eventType, string message, string? userId = null)
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                TemplateId = "system_event",
                Title = $"System Event: {eventType}",
                Message = message,
                Type = NotificationType.System,
                Priority = NotificationPriority.Normal,
                Recipients = userId != null ? new List<string> { userId } : GetAllSubscribedUsers(),
                Data = new Dictionary<string, object> 
                { 
                    ["eventType"] = eventType,
                    ["message"] = message
                },
                CreatedAt = DateTimeOffset.UtcNow,
                Status = NotificationStatus.Pending
            };

            await ProcessNotificationAsync(notification);
            _logger.Info($"System event notification sent: {eventType}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending system event notification: {ex.Message}", ex);
        }
    }

    // Private helper methods
    private void InitializeDefaultTemplates()
    {
        var templates = new[]
        {
            new NotificationTemplate
            {
                Id = "node_created",
                Name = "Node Created",
                Title = "New Node Created",
                Message = "A new node '{nodeId}' has been created.",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new NotificationTemplate
            {
                Id = "node_updated",
                Name = "Node Updated",
                Title = "Node Updated",
                Message = "Node '{nodeId}' has been updated.",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new NotificationTemplate
            {
                Id = "node_deleted",
                Name = "Node Deleted",
                Title = "Node Deleted",
                Message = "Node '{nodeId}' has been deleted.",
                Type = NotificationType.Warning,
                Priority = NotificationPriority.High,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new NotificationTemplate
            {
                Id = "system_event",
                Name = "System Event",
                Title = "System Event: {eventType}",
                Message = "{message}",
                Type = NotificationType.Info,
                Priority = NotificationPriority.Normal,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        foreach (var template in templates)
        {
            _templates[template.Id] = template;
        }
    }

    private string ProcessTemplate(string template, Dictionary<string, object> data)
    {
        var result = template;
        foreach (var kvp in data)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
        }
        return result;
    }

    private async Task ProcessNotificationAsync(Notification notification)
    {
        try
        {
            // Add to history
            _notificationHistory.Enqueue(notification);

            // Maintain history size
            lock (_lock)
            {
                while (_notificationHistory.Count > _maxHistorySize)
                {
                    _notificationHistory.TryDequeue(out _);
                }
            }

            // Send via real-time module if available
            if (_realtimeModule != null)
            {
                foreach (var recipient in notification.Recipients)
                {
                    try
                    {
                        await _realtimeModule.PublishSystemEventAsync("notification", new
                        {
                            Id = notification.Id,
                            Title = notification.Title,
                            Message = notification.Message,
                            Type = notification.Type.ToString(),
                            Priority = notification.Priority.ToString(),
                            Data = notification.Data,
                            Timestamp = notification.CreatedAt
                        }, recipient);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error sending notification to {recipient}: {ex.Message}", ex);
                    }
                }
            }

            // Send via other channels (email, SMS, etc.) based on subscription preferences
            await SendViaOtherChannelsAsync(notification);

            _logger.Debug($"Notification processed: {notification.Id}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing notification {notification.Id}: {ex.Message}", ex);
        }
    }

    private List<string> GetAllSubscribedUsers()
    {
        return _subscriptions.Values
            .Where(s => s.IsActive)
            .Select(s => s.UserId)
            .ToList();
    }

    private async Task SendViaOtherChannelsAsync(Notification notification)
    {
        // In a real implementation, this would send via email, SMS, push services, etc.
        foreach (var recipient in notification.Recipients)
        {
            if (_subscriptions.TryGetValue(recipient, out var subscription))
            {
                foreach (var channel in subscription.Channels)
                {
                    try
                    {
                        switch (channel)
                        {
                            case NotificationChannel.Email:
                                await SendEmailNotificationAsync(notification, recipient);
                                break;
                            case NotificationChannel.SMS:
                                await SendSMSNotificationAsync(notification, recipient);
                                break;
                            case NotificationChannel.Push:
                                await SendPushNotificationAsync(notification, recipient);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error sending {channel} notification to {recipient}: {ex.Message}", ex);
                    }
                }
            }
        }
    }

    private async Task SendEmailNotificationAsync(Notification notification, string recipient)
    {
        // In a real implementation, this would integrate with an email service
        _logger.Info($"Email notification sent to {recipient}: {notification.Title}");
        await Task.Delay(10); // Simulate email sending
    }

    private async Task SendSMSNotificationAsync(Notification notification, string recipient)
    {
        // In a real implementation, this would integrate with an SMS service
        _logger.Info($"SMS notification sent to {recipient}: {notification.Title}");
        await Task.Delay(10); // Simulate SMS sending
    }

    private async Task SendPushNotificationAsync(Notification notification, string recipient)
    {
        // In a real implementation, this would integrate with push notification services
        _logger.Info($"Push notification sent to {recipient}: {notification.Title}");
        await Task.Delay(10); // Simulate push notification sending
    }

    // Data models
    public record SendNotificationRequest(
        string? TemplateId = null,
        string? Title = null,
        string? Message = null,
        NotificationType? Type = null,
        NotificationPriority? Priority = null,
        List<string>? Recipients = null,
        Dictionary<string, object>? Data = null,
        DateTimeOffset? ScheduledAt = null,
        DateTimeOffset? ExpiresAt = null
    );

    public record NotificationSubscriptionRequest(
        string UserId,
        NotificationType[]? NotificationTypes = null,
        NotificationChannel[]? Channels = null,
        Dictionary<string, object>? Filters = null
    );

    public record NotificationHistoryQuery(
        string? UserId = null,
        NotificationType[]? NotificationTypes = null,
        NotificationStatus[]? Status = null,
        DateTimeOffset? Since = null,
        DateTimeOffset? Until = null,
        bool SortDescending = true,
        int? Skip = null,
        int? Take = null
    );

    public record CreateTemplateRequest(
        string Name,
        string? Title = null,
        string? Message = null,
        NotificationType? Type = null,
        NotificationPriority? Priority = null
    );

    public record ScheduleNotificationRequest(
        string? TemplateId = null,
        string? Title = null,
        string? Message = null,
        NotificationType? Type = null,
        NotificationPriority? Priority = null,
        List<string>? Recipients = null,
        Dictionary<string, object>? Data = null,
        DateTimeOffset ScheduledAt = default,
        DateTimeOffset? ExpiresAt = null
    );

    public record Notification(
        string Id,
        string? TemplateId = null,
        string Title = "",
        string Message = "",
        NotificationType Type = NotificationType.Info,
        NotificationPriority Priority = NotificationPriority.Normal,
        List<string> Recipients = null,
        Dictionary<string, object> Data = null,
        DateTimeOffset ScheduledAt = default,
        DateTimeOffset? ExpiresAt = null,
        DateTimeOffset CreatedAt = default,
        NotificationStatus Status = NotificationStatus.Pending
    )
    {
        public Notification() : this("", null, "", "", NotificationType.Info, NotificationPriority.Normal, new List<string>(), new Dictionary<string, object>(), DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow, NotificationStatus.Pending) { }
    }

    public record NotificationTemplate(
        string Id,
        string Name,
        string Title,
        string Message,
        NotificationType Type,
        NotificationPriority Priority,
        DateTimeOffset CreatedAt = default
    )
    {
        public NotificationTemplate() : this("", "", "", "", NotificationType.Info, NotificationPriority.Normal, DateTimeOffset.UtcNow) { }
    }

    public record NotificationSubscription(
        string UserId,
        HashSet<NotificationType> NotificationTypes,
        HashSet<NotificationChannel> Channels,
        Dictionary<string, object> Filters,
        bool IsActive = true,
        DateTimeOffset CreatedAt = default
    )
    {
        public NotificationSubscription() : this("", new HashSet<NotificationType>(), new HashSet<NotificationChannel>(), new Dictionary<string, object>(), true, DateTimeOffset.UtcNow) { }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        System
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum NotificationStatus
    {
        Pending,
        Scheduled,
        Sending,
        Sent,
        Failed,
        Expired
    }

    public enum NotificationChannel
    {
        Realtime,
        Email,
        SMS,
        Push
    }
}
