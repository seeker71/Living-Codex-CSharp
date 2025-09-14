using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IRealtimeNotificationService
{
    event EventHandler<RealtimeEvent>? EventReceived;
    event EventHandler<ContributionEvent>? ContributionEventReceived;
    event EventHandler<AbundanceEvent>? AbundanceEventReceived;
    event EventHandler<SystemEvent>? SystemEventReceived;
    
    bool IsConnected { get; }
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SubscribeToUserEventsAsync(string userId);
    Task UnsubscribeFromUserEventsAsync(string userId);
    Task SubscribeToConceptEventsAsync(string conceptId);
    Task UnsubscribeFromConceptEventsAsync(string conceptId);
    Task SubscribeToEventTypeAsync(string eventType);
    Task UnsubscribeFromEventTypeAsync(string eventType);
    Task SendNotificationAsync(string title, string message, string? data = null);
}


