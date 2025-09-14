using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly ISignalRService _signalRService;
    private readonly IAuthenticationService _authService;
    private readonly HashSet<string> _subscribedEventTypes = new();
    private readonly HashSet<string> _subscribedNodes = new();
    private string? _currentUserId;

    public event EventHandler<RealtimeEvent>? EventReceived;
    public event EventHandler<ContributionEvent>? ContributionEventReceived;
    public event EventHandler<AbundanceEvent>? AbundanceEventReceived;
    public event EventHandler<SystemEvent>? SystemEventReceived;

    public bool IsConnected => _signalRService.IsConnected;

    public RealtimeNotificationService(ISignalRService signalRService, IAuthenticationService authService)
    {
        _signalRService = signalRService;
        _authService = authService;
        
        // Subscribe to SignalR events
        _signalRService.NodeEventReceived += OnNodeEventReceived;
        _signalRService.ContributionEventReceived += OnContributionEventReceived;
        _signalRService.AbundanceEventReceived += OnAbundanceEventReceived;
        _signalRService.SystemEventReceived += OnSystemEventReceived;
    }

    public async Task ConnectAsync()
    {
        await _signalRService.ConnectAsync();
        _currentUserId = _authService.CurrentUser?.Id;
    }

    public async Task DisconnectAsync()
    {
        await _signalRService.DisconnectAsync();
        _currentUserId = null;
        _subscribedEventTypes.Clear();
        _subscribedNodes.Clear();
    }

    public async Task SubscribeToUserEventsAsync(string userId)
    {
        if (!IsConnected) return;

        _currentUserId = userId;
        
        // Subscribe to user-specific event types
        var userEventTypes = new[]
        {
            "contribution_recorded",
            "contribution_amplified",
            "abundance_event",
            "user_profile_updated",
            "user_permissions_updated"
        };

        foreach (var eventType in userEventTypes)
        {
            await SubscribeToEventTypeAsync(eventType);
        }
    }

    public async Task UnsubscribeFromUserEventsAsync(string userId)
    {
        if (!IsConnected) return;

        var userEventTypes = new[]
        {
            "contribution_recorded",
            "contribution_amplified",
            "abundance_event",
            "user_profile_updated",
            "user_permissions_updated"
        };

        foreach (var eventType in userEventTypes)
        {
            await UnsubscribeFromEventTypeAsync(eventType);
        }
    }

    public async Task SubscribeToConceptEventsAsync(string conceptId)
    {
        if (!IsConnected) return;

        await _signalRService.SubscribeToNodeAsync(conceptId);
        _subscribedNodes.Add(conceptId);
    }

    public async Task UnsubscribeFromConceptEventsAsync(string conceptId)
    {
        if (!IsConnected) return;

        await _signalRService.UnsubscribeFromNodeAsync(conceptId);
        _subscribedNodes.Remove(conceptId);
    }

    public async Task SubscribeToEventTypeAsync(string eventType)
    {
        if (!IsConnected) return;

        await _signalRService.SubscribeToEventTypeAsync(eventType);
        _subscribedEventTypes.Add(eventType);
    }

    public async Task UnsubscribeFromEventTypeAsync(string eventType)
    {
        if (!IsConnected) return;

        await _signalRService.UnsubscribeFromEventTypeAsync(eventType);
        _subscribedEventTypes.Remove(eventType);
    }

    public async Task SendNotificationAsync(string title, string message, string? data = null)
    {
        if (!IsConnected) return;

        var notification = new
        {
            Title = title,
            Message = message,
            Data = data,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Send to all connected clients in the user's group
        if (_currentUserId != null)
        {
            await _signalRService.BroadcastToGroupAsync($"user:{_currentUserId}", notification);
        }
    }

    private void OnNodeEventReceived(object? sender, RealtimeEvent nodeEvent)
    {
        EventReceived?.Invoke(this, nodeEvent);
    }

    private void OnContributionEventReceived(object? sender, ContributionEvent contributionEvent)
    {
        ContributionEventReceived?.Invoke(this, contributionEvent);
    }

    private void OnAbundanceEventReceived(object? sender, AbundanceEvent abundanceEvent)
    {
        AbundanceEventReceived?.Invoke(this, abundanceEvent);
    }

    private void OnSystemEventReceived(object? sender, SystemEvent systemEvent)
    {
        SystemEventReceived?.Invoke(this, systemEvent);
    }
}


