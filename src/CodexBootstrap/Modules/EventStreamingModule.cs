using System.Collections.Concurrent;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Event streaming module for live updates on node/edge changes
/// </summary>
public sealed class EventStreamingModule : IModule
{
    private readonly Core.ILogger _logger;
    private readonly NodeRegistry _registry;
    private readonly RealtimeModule? _realtimeModule;
    private readonly ConcurrentQueue<StreamEvent> _eventHistory = new();
    private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions = new();
    private readonly object _lock = new object();
    private int _maxHistorySize = 1000;

    public EventStreamingModule(NodeRegistry registry, RealtimeModule? realtimeModule = null)
    {
        _logger = new Log4NetLogger(typeof(EventStreamingModule));
        _registry = registry;
        _realtimeModule = realtimeModule;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.event-streaming",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Event Streaming Module",
            Description: "Provides event streaming for live updates on node/edge changes",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "0.1.0",
                    capabilities = new[]
                    {
                        "event_streaming",
                        "event_history",
                        "event_subscription",
                        "event_filtering",
                        "event_aggregation",
                        "event_replay"
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Event Streaming Module",
                ["version"] = "0.1.0",
                ["description"] = "Provides event streaming for live updates on node/edge changes"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
        _logger.Info("Event Streaming Module registered");
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Event Streaming API handlers registered");
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Event Streaming API Methods
    [ApiRoute("GET", "/events/stream", "GetEventStream", "Get real-time event stream", "codex.event-streaming")]
    public async Task<object> GetEventStreamAsync([ApiParameter("query", "Stream parameters")] EventStreamQuery query)
    {
        try
        {
            var events = _eventHistory.AsEnumerable();

            // Apply filters
            if (query.EventTypes?.Any() == true)
            {
                events = events.Where(e => query.EventTypes.Contains(e.EventType));
            }

            if (query.EntityTypes?.Any() == true)
            {
                events = events.Where(e => query.EntityTypes.Contains(e.EntityType));
            }

            if (query.EntityIds?.Any() == true)
            {
                events = events.Where(e => query.EntityIds.Contains(e.EntityId));
            }

            if (query.Since.HasValue)
            {
                events = events.Where(e => e.Timestamp >= query.Since.Value);
            }

            if (query.Until.HasValue)
            {
                events = events.Where(e => e.Timestamp <= query.Until.Value);
            }

            // Apply pagination
            var totalCount = events.Count();
            var pagedEvents = events
                .OrderByDescending(e => e.Timestamp)
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Retrieved {pagedEvents.Count} events from stream");
            return new { 
                success = true, 
                events = pagedEvents, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting event stream: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get event stream: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/events/subscribe", "SubscribeToEvents", "Subscribe to specific event types", "codex.event-streaming")]
    public async Task<object> SubscribeToEventsAsync([ApiParameter("body", "Subscription request")] EventSubscriptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SubscriptionId))
            {
                return new ErrorResponse("Subscription ID is required");
            }

            var subscription = new EventSubscription
            {
                SubscriptionId = request.SubscriptionId,
                EventTypes = request.EventTypes?.ToHashSet() ?? new HashSet<string>(),
                EntityTypes = request.EntityTypes?.ToHashSet() ?? new HashSet<string>(),
                EntityIds = request.EntityIds?.ToHashSet() ?? new HashSet<string>(),
                Filters = request.Filters ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            _subscriptions[request.SubscriptionId] = subscription;

            _logger.Info($"Event subscription created: {request.SubscriptionId}");
            return new { success = true, subscription = subscription };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating event subscription: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create event subscription: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/events/subscribe/{subscriptionId}", "UnsubscribeFromEvents", "Unsubscribe from events", "codex.event-streaming")]
    public async Task<object> UnsubscribeFromEventsAsync(string subscriptionId)
    {
        try
        {
            if (string.IsNullOrEmpty(subscriptionId))
            {
                return new ErrorResponse("Subscription ID is required");
            }

            if (_subscriptions.TryRemove(subscriptionId, out var subscription))
            {
                // subscription.IsActive = false; // Cannot modify init-only property
                _logger.Info($"Event subscription removed: {subscriptionId}");
                return new { success = true };
            }

            return new ErrorResponse("Subscription not found");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error removing event subscription: {ex.Message}", ex);
            return new ErrorResponse($"Failed to remove event subscription: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/events/subscriptions", "GetSubscriptions", "Get active event subscriptions", "codex.event-streaming")]
    public async Task<object> GetSubscriptionsAsync()
    {
        try
        {
            var subscriptions = _subscriptions.Values
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.SubscriptionId,
                    s.EventTypes,
                    s.EntityTypes,
                    s.EntityIds,
                    s.Filters,
                    s.CreatedAt,
                    s.IsActive
                })
                .ToList();

            _logger.Debug($"Retrieved {subscriptions.Count} active subscriptions");
            return new { success = true, subscriptions = subscriptions };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting subscriptions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get subscriptions: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/events/history", "GetEventHistory", "Get event history with filtering", "codex.event-streaming")]
    public async Task<object> GetEventHistoryAsync([ApiParameter("query", "History query parameters")] EventHistoryQuery query)
    {
        try
        {
            var events = _eventHistory.AsEnumerable();

            // Apply filters
            if (query.EventTypes?.Any() == true)
            {
                events = events.Where(e => query.EventTypes.Contains(e.EventType));
            }

            if (query.EntityTypes?.Any() == true)
            {
                events = events.Where(e => query.EntityTypes.Contains(e.EntityType));
            }

            if (query.EntityIds?.Any() == true)
            {
                events = events.Where(e => query.EntityIds.Contains(e.EntityId));
            }

            if (query.Since.HasValue)
            {
                events = events.Where(e => e.Timestamp >= query.Since.Value);
            }

            if (query.Until.HasValue)
            {
                events = events.Where(e => e.Timestamp <= query.Until.Value);
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchLower = query.SearchTerm.ToLowerInvariant();
                events = events.Where(e => 
                    e.EventType.ToLowerInvariant().Contains(searchLower) ||
                    e.EntityType.ToLowerInvariant().Contains(searchLower) ||
                    e.EntityId.ToLowerInvariant().Contains(searchLower) ||
                    e.Description?.ToLowerInvariant().Contains(searchLower) == true);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                switch (query.SortBy.ToLowerInvariant())
                {
                    case "timestamp":
                        events = query.SortDescending ? 
                            events.OrderByDescending(e => e.Timestamp) : 
                            events.OrderBy(e => e.Timestamp);
                        break;
                    case "eventtype":
                        events = query.SortDescending ? 
                            events.OrderByDescending(e => e.EventType) : 
                            events.OrderBy(e => e.EventType);
                        break;
                    case "entitytype":
                        events = query.SortDescending ? 
                            events.OrderByDescending(e => e.EntityType) : 
                            events.OrderBy(e => e.EntityType);
                        break;
                    default:
                        events = events.OrderByDescending(e => e.Timestamp);
                        break;
                }
            }
            else
            {
                events = events.OrderByDescending(e => e.Timestamp);
            }

            // Apply pagination
            var totalCount = events.Count();
            var pagedEvents = events
                .Skip(query.Skip ?? 0)
                .Take(query.Take ?? 100)
                .ToList();

            _logger.Debug($"Retrieved {pagedEvents.Count} events from history");
            return new { 
                success = true, 
                events = pagedEvents, 
                totalCount = totalCount,
                skip = query.Skip ?? 0,
                take = query.Take ?? 100
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting event history: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get event history: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/events/aggregate", "AggregateEvents", "Aggregate events by criteria", "codex.event-streaming")]
    public async Task<object> AggregateEventsAsync([ApiParameter("body", "Aggregation request")] EventAggregationRequest request)
    {
        try
        {
            var events = _eventHistory.AsEnumerable();

            // Apply filters
            if (request.EventTypes?.Any() == true)
            {
                events = events.Where(e => request.EventTypes.Contains(e.EventType));
            }

            if (request.EntityTypes?.Any() == true)
            {
                events = events.Where(e => request.EntityTypes.Contains(e.EntityType));
            }

            if (request.Since.HasValue)
            {
                events = events.Where(e => e.Timestamp >= request.Since.Value);
            }

            if (request.Until.HasValue)
            {
                events = events.Where(e => e.Timestamp <= request.Until.Value);
            }

            var eventList = events.ToList();

            // Perform aggregation
            var aggregation = new
            {
                TotalEvents = eventList.Count,
                EventTypeCounts = eventList.GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EntityTypeCounts = eventList.GroupBy(e => e.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TimeRange = new
                {
                    Earliest = eventList.Min(e => e.Timestamp),
                    Latest = eventList.Max(e => e.Timestamp)
                },
                UserActivity = eventList.Where(e => !string.IsNullOrEmpty(e.UserId))
                    .GroupBy(e => e.UserId)
                    .ToDictionary(g => g.Key!, g => g.Count())
            };

            _logger.Debug($"Aggregated {eventList.Count} events");
            return new { success = true, aggregation = aggregation };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error aggregating events: {ex.Message}", ex);
            return new ErrorResponse($"Failed to aggregate events: {ex.Message}");
        }
    }

    // Event Publishing Methods
    public async Task PublishNodeCreatedEventAsync(Node node, string? userId = null)
    {
        await PublishEventAsync("node_created", "node", node.Id, new
        {
            NodeId = node.Id,
            NodeType = node.TypeId,
            NodeState = node.State.ToString(),
            Title = node.Title,
            Description = node.Description
        }, userId);
    }

    public async Task PublishNodeUpdatedEventAsync(Node node, string? userId = null)
    {
        await PublishEventAsync("node_updated", "node", node.Id, new
        {
            NodeId = node.Id,
            NodeType = node.TypeId,
            NodeState = node.State.ToString(),
            Title = node.Title,
            Description = node.Description
        }, userId);
    }

    public async Task PublishNodeDeletedEventAsync(string nodeId, string? userId = null)
    {
        await PublishEventAsync("node_deleted", "node", nodeId, new
        {
            NodeId = nodeId
        }, userId);
    }

    public async Task PublishEdgeCreatedEventAsync(Edge edge, string? userId = null)
    {
        await PublishEventAsync("edge_created", "edge", $"{edge.FromId}-{edge.ToId}", new
        {
            FromId = edge.FromId,
            ToId = edge.ToId,
            Role = edge.Role,
            Weight = edge.Weight
        }, userId);
    }

    public async Task PublishEdgeUpdatedEventAsync(Edge edge, string? userId = null)
    {
        await PublishEventAsync("edge_updated", "edge", $"{edge.FromId}-{edge.ToId}", new
        {
            FromId = edge.FromId,
            ToId = edge.ToId,
            Role = edge.Role,
            Weight = edge.Weight
        }, userId);
    }

    public async Task PublishEdgeDeletedEventAsync(string fromId, string toId, string? userId = null)
    {
        await PublishEventAsync("edge_deleted", "edge", $"{fromId}-{toId}", new
        {
            FromId = fromId,
            ToId = toId
        }, userId);
    }

    public async Task PublishSystemEventAsync(string eventType, string description, object? data = null, string? userId = null)
    {
        await PublishEventAsync(eventType, "system", "system", new
        {
            Description = description,
            Data = data
        }, userId);
    }

    // Private helper methods
    private async Task PublishEventAsync(string eventType, string entityType, string entityId, object data, string? userId = null)
    {
        var streamEvent = new StreamEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Data = data,
            UserId = userId,
            Timestamp = DateTimeOffset.UtcNow,
            Description = $"{eventType} for {entityType} {entityId}"
        };

        // Add to history
        _eventHistory.Enqueue(streamEvent);

        // Maintain history size
        lock (_lock)
        {
            while (_eventHistory.Count > _maxHistorySize)
            {
                _eventHistory.TryDequeue(out _);
            }
        }

        // Notify real-time module
        if (_realtimeModule != null)
        {
            await _realtimeModule.PublishSystemEventAsync(eventType, streamEvent, userId);
        }

        // Notify subscribers
        await NotifySubscribersAsync(streamEvent);

        _logger.Debug($"Published event: {eventType} for {entityType} {entityId}");
    }

    private async Task NotifySubscribersAsync(StreamEvent streamEvent)
    {
        var matchingSubscriptions = _subscriptions.Values
            .Where(s => s.IsActive && 
                       (s.EventTypes.Count == 0 || s.EventTypes.Contains(streamEvent.EventType)) &&
                       (s.EntityTypes.Count == 0 || s.EntityTypes.Contains(streamEvent.EntityType)) &&
                       (s.EntityIds.Count == 0 || s.EntityIds.Contains(streamEvent.EntityId)))
            .ToList();

        foreach (var subscription in matchingSubscriptions)
        {
            try
            {
                // In a real implementation, this would send to the subscriber
                _logger.Debug($"Notified subscription {subscription.SubscriptionId} of event {streamEvent.EventId}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error notifying subscription {subscription.SubscriptionId}: {ex.Message}", ex);
            }
        }
    }

    // Data models
    public record EventStreamQuery(
        string[]? EventTypes = null,
        string[]? EntityTypes = null,
        string[]? EntityIds = null,
        DateTimeOffset? Since = null,
        DateTimeOffset? Until = null,
        int? Skip = null,
        int? Take = null
    );

    public record EventHistoryQuery(
        string[]? EventTypes = null,
        string[]? EntityTypes = null,
        string[]? EntityIds = null,
        DateTimeOffset? Since = null,
        DateTimeOffset? Until = null,
        string? SearchTerm = null,
        string? SortBy = null,
        bool SortDescending = true,
        int? Skip = null,
        int? Take = null
    );

    public record EventSubscriptionRequest(
        string SubscriptionId,
        string[]? EventTypes = null,
        string[]? EntityTypes = null,
        string[]? EntityIds = null,
        Dictionary<string, object>? Filters = null
    );

    public record EventAggregationRequest(
        string[]? EventTypes = null,
        string[]? EntityTypes = null,
        DateTimeOffset? Since = null,
        DateTimeOffset? Until = null
    );

    public record StreamEvent(
        string EventId,
        string EventType,
        string EntityType,
        string EntityId,
        object Data,
        string? UserId = null,
        DateTimeOffset Timestamp = default,
        string? Description = null
    )
    {
        public StreamEvent() : this("", "", "", "", new { }, null, DateTimeOffset.UtcNow, null) { }
    }

    public record EventSubscription(
        string SubscriptionId,
        HashSet<string> EventTypes,
        HashSet<string> EntityTypes,
        HashSet<string> EntityIds,
        Dictionary<string, object> Filters,
        DateTimeOffset CreatedAt = default,
        bool IsActive = true
    )
    {
        public EventSubscription() : this("", new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), new Dictionary<string, object>(), DateTimeOffset.UtcNow, true) { }
    }
}
