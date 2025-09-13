using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services
{
    public interface ISignalRService
    {
        event EventHandler<Concept>? ConceptUpdated;
        event EventHandler<Contribution>? ContributionAdded;
        event EventHandler<double>? CollectiveEnergyUpdated;
        event EventHandler<string>? ConnectionStatusChanged;
        event EventHandler<RealtimeEvent>? NodeEventReceived;
        event EventHandler<ContributionEvent>? ContributionEventReceived;
        event EventHandler<AbundanceEvent>? AbundanceEventReceived;
        event EventHandler<SystemEvent>? SystemEventReceived;
        event EventHandler<GroupMessage>? GroupMessageReceived;
        event EventHandler<DirectMessage>? DirectMessageReceived;

        bool IsConnected { get; }
        Task ConnectAsync();
        Task DisconnectAsync();
        Task JoinGroupAsync(string groupName);
        Task LeaveGroupAsync(string groupName);
        Task SubscribeToNodeAsync(string nodeId);
        Task UnsubscribeFromNodeAsync(string nodeId);
        Task SubscribeToEventTypeAsync(string eventType);
        Task UnsubscribeFromEventTypeAsync(string eventType);
        Task SendMessageAsync(string targetConnectionId, object message);
        Task BroadcastToGroupAsync(string groupName, object message);
        void SubscribeToEvent<T>(string eventType, Func<T, Task> handler);
        void UnsubscribeFromEvent(string eventType);
    }
}
