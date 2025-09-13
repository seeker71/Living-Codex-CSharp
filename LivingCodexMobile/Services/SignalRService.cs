using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services
{
    public class SignalRService : ISignalRService
    {
        private HubConnection? _hubConnection;
        private readonly string _hubUrl;
        private readonly Dictionary<string, List<Func<object, Task>>> _eventHandlers = new();

        public event EventHandler<Concept>? ConceptUpdated;
        public event EventHandler<Contribution>? ContributionAdded;
        public event EventHandler<double>? CollectiveEnergyUpdated;
        public event EventHandler<string>? ConnectionStatusChanged;
        public event EventHandler<RealtimeEvent>? NodeEventReceived;
        public event EventHandler<ContributionEvent>? ContributionEventReceived;
        public event EventHandler<AbundanceEvent>? AbundanceEventReceived;
        public event EventHandler<SystemEvent>? SystemEventReceived;
        public event EventHandler<GroupMessage>? GroupMessageReceived;
        public event EventHandler<DirectMessage>? DirectMessageReceived;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public SignalRService(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public async Task ConnectAsync()
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // Register core event handlers
                _hubConnection.On<Concept>("ConceptUpdated", OnConceptUpdated);
                _hubConnection.On<Contribution>("ContributionAdded", OnContributionAdded);
                _hubConnection.On<double>("CollectiveEnergyUpdated", OnCollectiveEnergyUpdated);
                _hubConnection.On<string>("ConnectionStatusChanged", OnConnectionStatusChanged);

                // Register real-time event handlers
                _hubConnection.On<RealtimeEvent>("NodeEvent", OnNodeEventReceived);
                _hubConnection.On<ContributionEvent>("ContributionEvent", OnContributionEventReceived);
                _hubConnection.On<AbundanceEvent>("AbundanceEvent", OnAbundanceEventReceived);
                _hubConnection.On<SystemEvent>("SystemEvent", OnSystemEventReceived);
                _hubConnection.On<GroupMessage>("GroupMessage", OnGroupMessageReceived);
                _hubConnection.On<DirectMessage>("Message", OnDirectMessageReceived);

                // Register connection event handlers
                _hubConnection.On<ConnectionInfo>("Connected", OnConnected);
                _hubConnection.On<object>("Message", OnMessageReceived);

                // Connection state change handlers
                _hubConnection.Closed += OnConnectionClosed;
                _hubConnection.Reconnecting += OnReconnecting;
                _hubConnection.Reconnected += OnReconnected;

                await _hubConnection.StartAsync();
                ConnectionStatusChanged?.Invoke(this, "Connected");
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                ConnectionStatusChanged?.Invoke(this, "Disconnected");
            }
        }

        public async Task JoinGroupAsync(string groupName)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("JoinGroup", groupName);
            }
        }

        public async Task LeaveGroupAsync(string groupName)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("LeaveGroup", groupName);
            }
        }

        public async Task SubscribeToNodeAsync(string nodeId)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("SubscribeToNode", nodeId);
            }
        }

        public async Task UnsubscribeFromNodeAsync(string nodeId)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromNode", nodeId);
            }
        }

        public async Task SubscribeToEventTypeAsync(string eventType)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("SubscribeToEventType", eventType);
            }
        }

        public async Task UnsubscribeFromEventTypeAsync(string eventType)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("UnsubscribeFromEventType", eventType);
            }
        }

        public async Task SendMessageAsync(string targetConnectionId, object message)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("SendMessage", targetConnectionId, message);
            }
        }

        public async Task BroadcastToGroupAsync(string groupName, object message)
        {
            if (_hubConnection != null && IsConnected)
            {
                await _hubConnection.InvokeAsync("BroadcastToGroup", groupName, message);
            }
        }

        public void SubscribeToEvent<T>(string eventType, Func<T, Task> handler)
        {
            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Func<object, Task>>();
            }
            
            _eventHandlers[eventType].Add(obj => handler((T)obj));
        }

        public void UnsubscribeFromEvent(string eventType)
        {
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType].Clear();
            }
        }

        private void OnConceptUpdated(Concept concept)
        {
            ConceptUpdated?.Invoke(this, concept);
        }

        private void OnContributionAdded(Contribution contribution)
        {
            ContributionAdded?.Invoke(this, contribution);
        }

        private void OnCollectiveEnergyUpdated(double energy)
        {
            CollectiveEnergyUpdated?.Invoke(this, energy);
        }

        private void OnConnectionStatusChanged(string status)
        {
            ConnectionStatusChanged?.Invoke(this, status);
        }

        private void OnNodeEventReceived(RealtimeEvent nodeEvent)
        {
            NodeEventReceived?.Invoke(this, nodeEvent);
        }

        private void OnContributionEventReceived(ContributionEvent contributionEvent)
        {
            ContributionEventReceived?.Invoke(this, contributionEvent);
        }

        private void OnAbundanceEventReceived(AbundanceEvent abundanceEvent)
        {
            AbundanceEventReceived?.Invoke(this, abundanceEvent);
        }

        private void OnSystemEventReceived(SystemEvent systemEvent)
        {
            SystemEventReceived?.Invoke(this, systemEvent);
        }

        private void OnGroupMessageReceived(GroupMessage groupMessage)
        {
            GroupMessageReceived?.Invoke(this, groupMessage);
        }

        private void OnDirectMessageReceived(DirectMessage directMessage)
        {
            DirectMessageReceived?.Invoke(this, directMessage);
        }

        private void OnConnected(ConnectionInfo connectionInfo)
        {
            ConnectionStatusChanged?.Invoke(this, $"Connected: {connectionInfo.ConnectionId}");
        }

        private void OnMessageReceived(object message)
        {
            // Handle generic messages and route to appropriate handlers
            if (message is JsonElement jsonElement)
            {
                var messageType = jsonElement.GetProperty("type").GetString();
                if (messageType != null && _eventHandlers.ContainsKey(messageType))
                {
                    var handlers = _eventHandlers[messageType];
                    foreach (var handler in handlers)
                    {
                        _ = Task.Run(() => handler(message));
                    }
                }
            }
        }

        private async Task OnConnectionClosed(Exception? exception)
        {
            ConnectionStatusChanged?.Invoke(this, "Connection closed");
            await Task.CompletedTask;
        }

        private async Task OnReconnecting(Exception? exception)
        {
            ConnectionStatusChanged?.Invoke(this, "Reconnecting...");
            await Task.CompletedTask;
        }

        private async Task OnReconnected(string? connectionId)
        {
            ConnectionStatusChanged?.Invoke(this, "Reconnected");
            await Task.CompletedTask;
        }
    }
}
