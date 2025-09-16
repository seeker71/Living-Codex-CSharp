using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Real-time communication module providing WebSocket and SignalR support
/// </summary>
public sealed class RealtimeModule : ModuleBase
{
    private readonly ConcurrentDictionary<string, WebSocket> _webSockets = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, RealtimeSession> _sessions = new();

    public override string Name => "Realtime Module";
    public override string Description => "Real-time communication module providing WebSocket and SignalR support";
    public override string Version => "1.0.0";

    public RealtimeModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.realtime",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "realtime", "websocket", "signalr", "communication" },
            capabilities: new[] { "websocket_connection", "signalr_hub", "event_streaming", "push_notifications", "collaborative_editing", "subscription_management", "session_management" },
            spec: "codex.spec.realtime"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Real-time Communication API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Register WebSocket endpoint
        app.MapGet("/ws", async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var sessionId = Guid.NewGuid().ToString();
                _webSockets[sessionId] = webSocket;
                
                _logger.Info($"WebSocket connection established: {sessionId}");
                
                await HandleWebSocketConnection(sessionId, webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection required");
            }
        });

        // Register SignalR hub
        app.MapHub<RealtimeHub>("/realtime-hub");
        
        _logger.Info("Real-time HTTP endpoints registered");
    }

    // WebSocket Management API Methods
    [ApiRoute("GET", "/realtime/connections", "GetConnections", "Get active WebSocket connections", "codex.realtime")]
    public async Task<object> GetConnectionsAsync()
    {
        try
        {
            var connections = _webSockets.Select(kvp => new
            {
                SessionId = kvp.Key,
                State = kvp.Value.State.ToString(),
                IsConnected = kvp.Value.State == WebSocketState.Open
            }).ToList();

            _logger.Debug($"Retrieved {connections.Count} active connections");
            return new { success = true, connections = connections };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting connections: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get connections: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/realtime/broadcast", "BroadcastMessage", "Broadcast message to all connected clients", "codex.realtime")]
    public async Task<object> BroadcastMessageAsync([ApiParameter("body", "Broadcast request")] BroadcastRequest request)
    {
        try
        {
            var message = new RealtimeMessage(
                Type: request.Type,
                Data: request.Data,
                Timestamp: DateTimeOffset.UtcNow,
                Sender: "system"
            );

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            var tasks = _webSockets.Where(kvp => kvp.Value.State == WebSocketState.Open)
                .Select(async kvp =>
                {
                    try
                    {
                        await kvp.Value.SendAsync(
                            new ArraySegment<byte>(messageBytes),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error sending message to {kvp.Key}: {ex.Message}", ex);
                        _webSockets.TryRemove(kvp.Key, out _);
                    }
                });

            await Task.WhenAll(tasks);

            _logger.Info($"Broadcasted message to {_webSockets.Count} clients");
            return new { success = true, sentTo = _webSockets.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error broadcasting message: {ex.Message}", ex);
            return new ErrorResponse($"Failed to broadcast message: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/realtime/subscribe", "SubscribeToRealtimeEvents", "Subscribe to realtime event types", "codex.realtime")]
    public async Task<object> SubscribeToEventsAsync([ApiParameter("body", "Subscription request")] SubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SessionId))
            {
                return new ErrorResponse("Session ID is required");
            }

            if (request.EventTypes?.Any() != true)
            {
                return new ErrorResponse("Event types are required");
            }

            _subscriptions.AddOrUpdate(
                request.SessionId,
                new HashSet<string>(request.EventTypes),
                (key, existing) =>
                {
                    foreach (var eventType in request.EventTypes)
                    {
                        existing.Add(eventType);
                    }
                    return existing;
                });

            _logger.Info($"Session {request.SessionId} subscribed to events: {string.Join(", ", request.EventTypes)}");
            return new { success = true, subscribedTo = request.EventTypes };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error subscribing to events: {ex.Message}", ex);
            return new ErrorResponse($"Failed to subscribe to events: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/realtime/unsubscribe", "UnsubscribeFromRealtimeEvents", "Unsubscribe from specific event types", "codex.realtime")]
    public async Task<object> UnsubscribeFromEventsAsync([ApiParameter("body", "Unsubscription request")] UnsubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SessionId))
            {
                return new ErrorResponse("Session ID is required");
            }

            if (_subscriptions.TryGetValue(request.SessionId, out var subscriptions))
            {
                if (request.EventTypes?.Any() == true)
                {
                    foreach (var eventType in request.EventTypes)
                    {
                        subscriptions.Remove(eventType);
                    }
                }
                else
                {
                    subscriptions.Clear();
                }

                _logger.Info($"Session {request.SessionId} unsubscribed from events");
                return new { success = true, remainingSubscriptions = subscriptions.ToList() };
            }

            return new ErrorResponse("Session not found");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error unsubscribing from events: {ex.Message}", ex);
            return new ErrorResponse($"Failed to unsubscribe from events: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/realtime/sessions", "GetSessions", "Get active real-time sessions", "codex.realtime")]
    public async Task<object> GetSessionsAsync()
    {
        try
        {
            var sessions = _sessions.Select(kvp => new
            {
                SessionId = kvp.Key,
                UserId = kvp.Value.UserId,
                ConnectedAt = kvp.Value.ConnectedAt,
                LastActivity = kvp.Value.LastActivity,
                Subscriptions = kvp.Value.Subscriptions.ToList(),
                IsActive = kvp.Value.IsActive
            }).ToList();

            _logger.Debug($"Retrieved {sessions.Count} active sessions");
            return new { success = true, sessions = sessions };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting sessions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get sessions: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/realtime/notify", "SendRealtimeNotification", "Send notification to specific session", "codex.realtime")]
    public async Task<object> SendNotificationAsync([ApiParameter("body", "Notification request")] NotificationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SessionId))
            {
                return new ErrorResponse("Session ID is required");
            }

            if (_webSockets.TryGetValue(request.SessionId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
            var notification = new RealtimeMessage(
                Type: "notification",
                Data: request.Data,
                Timestamp: DateTimeOffset.UtcNow,
                Sender: "system"
            );

                var messageJson = JsonSerializer.Serialize(notification);
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);

                _logger.Info($"Notification sent to session {request.SessionId}");
                return new { success = true };
            }

            return new ErrorResponse("Session not found or not connected");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending notification: {ex.Message}", ex);
            return new ErrorResponse($"Failed to send notification: {ex.Message}");
        }
    }

    // Event Publishing Methods
    public async Task PublishNodeEventAsync(string eventType, Node node, string? userId = null)
    {
        try
        {
            var eventData = new
            {
                EventType = eventType,
                NodeId = node.Id,
                NodeType = node.TypeId,
                NodeState = node.State.ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                UserId = userId
            };

            await PublishEventAsync("node_event", eventData);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error publishing node event: {ex.Message}", ex);
        }
    }

    public async Task PublishEdgeEventAsync(string eventType, Edge edge, string? userId = null)
    {
        try
        {
            var eventData = new
            {
                EventType = eventType,
                FromId = edge.FromId,
                ToId = edge.ToId,
                Role = edge.Role,
                Timestamp = DateTimeOffset.UtcNow,
                UserId = userId
            };

            await PublishEventAsync("edge_event", eventData);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error publishing edge event: {ex.Message}", ex);
        }
    }

    public async Task PublishSystemEventAsync(string eventType, object data, string? userId = null)
    {
        try
        {
            var eventData = new
            {
                EventType = eventType,
                Data = data,
                Timestamp = DateTimeOffset.UtcNow,
                UserId = userId
            };

            await PublishEventAsync("system_event", eventData);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error publishing system event: {ex.Message}", ex);
        }
    }

    // Private helper methods
    private async Task HandleWebSocketConnection(string sessionId, WebSocket webSocket)
    {
        var session = new RealtimeSession
        {
            SessionId = sessionId,
            ConnectedAt = DateTimeOffset.UtcNow,
            LastActivity = DateTimeOffset.UtcNow,
            IsActive = true,
            Subscriptions = new HashSet<string>()
        };

        _sessions[sessionId] = session;

        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleWebSocketMessage(sessionId, message);
                    // session.LastActivity = DateTimeOffset.UtcNow; // Cannot modify init-only property
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"WebSocket connection error for {sessionId}: {ex.Message}", ex);
        }
        finally
        {
            _webSockets.TryRemove(sessionId, out _);
            _sessions.TryRemove(sessionId, out _);
            _subscriptions.TryRemove(sessionId, out _);
            _logger.Info($"WebSocket connection closed: {sessionId}");
        }
    }

    private async Task HandleWebSocketMessage(string sessionId, string message)
    {
        try
        {
            var messageObj = JsonSerializer.Deserialize<RealtimeMessage>(message);
            if (messageObj == null) return;

            switch (messageObj.Type.ToLowerInvariant())
            {
                case "ping":
                    await SendPongAsync(sessionId);
                    break;
                case "subscribe":
                    if (messageObj.Data is JsonElement data && data.TryGetProperty("eventTypes", out var eventTypes))
                    {
                        var types = eventTypes.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).Cast<string>().ToList();
                        if (types.Any())
                        {
                            _subscriptions.AddOrUpdate(
                                sessionId,
                                new HashSet<string>(types),
                                (key, existing) =>
                                {
                                    foreach (var type in types)
                                    {
                                        existing.Add(type);
                                    }
                                    return existing;
                                });
                        }
                    }
                    break;
                case "unsubscribe":
                    if (messageObj.Data is JsonElement unsubData && unsubData.TryGetProperty("eventTypes", out var unsubEventTypes))
                    {
                        var unsubTypes = unsubEventTypes.EnumerateArray().Select(e => e.GetString()).Where(s => s != null).Cast<string>().ToList();
                        if (_subscriptions.TryGetValue(sessionId, out var subs))
                        {
                            foreach (var type in unsubTypes)
                            {
                                subs.Remove(type);
                            }
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling WebSocket message from {sessionId}: {ex.Message}", ex);
        }
    }

    private async Task SendPongAsync(string sessionId)
    {
        if (_webSockets.TryGetValue(sessionId, out var webSocket) && webSocket.State == WebSocketState.Open)
        {
            var pong = new RealtimeMessage(
                Type: "pong",
                Data: new { timestamp = DateTimeOffset.UtcNow },
                Timestamp: DateTimeOffset.UtcNow,
                Sender: "system"
            );

            var messageJson = JsonSerializer.Serialize(pong);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            await webSocket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }

    private async Task PublishEventAsync(string eventType, object eventData)
    {
        var message = new RealtimeMessage(
            Type: eventType,
            Data: eventData,
            Timestamp: DateTimeOffset.UtcNow,
            Sender: "system"
        );

        var messageJson = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageJson);

        var tasks = _webSockets
            .Where(kvp => kvp.Value.State == WebSocketState.Open && 
                         (!_subscriptions.TryGetValue(kvp.Key, out var subs) || subs.Contains(eventType)))
            .Select(async kvp =>
            {
                try
                {
                    await kvp.Value.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error sending event to {kvp.Key}: {ex.Message}", ex);
                    _webSockets.TryRemove(kvp.Key, out _);
                }
            });

        await Task.WhenAll(tasks);
    }

    // Data models
    public record BroadcastRequest(
        string Type,
        object Data
    );

    public record SubscriptionRequest(
        string SessionId,
        string[] EventTypes
    );

    public record UnsubscriptionRequest(
        string SessionId,
        string[]? EventTypes = null
    );

    public record NotificationRequest(
        string SessionId,
        object Data
    );

    public record RealtimeMessage(
        string Type,
        object Data,
        DateTimeOffset Timestamp,
        string Sender
    );

    public record RealtimeSession(
        string SessionId,
        string? UserId = null,
        DateTimeOffset ConnectedAt = default,
        DateTimeOffset LastActivity = default,
        HashSet<string> Subscriptions = null,
        bool IsActive = true
    )
    {
        public RealtimeSession() : this("", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new HashSet<string>(), true) { }
    }
}

/// <summary>
/// SignalR Hub for real-time communication
/// </summary>
[ResponseType("codex.realtime.hub", "RealtimeHub", "SignalR Hub for real-time communication")]
public class RealtimeHub : Hub
{
    private readonly Core.ICodexLogger _logger;
    private readonly NodeRegistry _registry;
    private readonly RealtimeModule _realtimeModule;

    public RealtimeHub(NodeRegistry registry, RealtimeModule realtimeModule)
    {
        _logger = new Log4NetLogger(typeof(RealtimeHub));
        _registry = registry;
        _realtimeModule = realtimeModule;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.Info($"SignalR client connected: {Context.ConnectionId}");
        await Clients.Caller.SendAsync("Connected", new { ConnectionId = Context.ConnectionId, Timestamp = DateTimeOffset.UtcNow });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.Info($"SignalR client disconnected: {Context.ConnectionId}");
        if (exception != null)
        {
            _logger.Error($"SignalR disconnect error: {exception.Message}", exception);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.Info($"Client {Context.ConnectionId} joined group: {groupName}");
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.Info($"Client {Context.ConnectionId} left group: {groupName}");
    }

    public async Task SubscribeToNode(string nodeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"node:{nodeId}");
        _logger.Info($"Client {Context.ConnectionId} subscribed to node: {nodeId}");
    }

    public async Task UnsubscribeFromNode(string nodeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"node:{nodeId}");
        _logger.Info($"Client {Context.ConnectionId} unsubscribed from node: {nodeId}");
    }

    public async Task SubscribeToEventType(string eventType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event:{eventType}");
        _logger.Info($"Client {Context.ConnectionId} subscribed to event type: {eventType}");
    }

    public async Task UnsubscribeFromEventType(string eventType)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event:{eventType}");
        _logger.Info($"Client {Context.ConnectionId} unsubscribed from event type: {eventType}");
    }

    public async Task SendMessage(string targetConnectionId, object message)
    {
        await Clients.Client(targetConnectionId).SendAsync("Message", message);
        _logger.Info($"Message sent from {Context.ConnectionId} to {targetConnectionId}");
    }

    public async Task BroadcastToGroup(string groupName, object message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", message);
        _logger.Info($"Message broadcasted to group {groupName} from {Context.ConnectionId}");
    }
}
