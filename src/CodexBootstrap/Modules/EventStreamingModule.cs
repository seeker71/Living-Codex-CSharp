using System.Collections.Concurrent;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Event streaming module for live updates on node/edge changes
/// </summary>
public sealed class EventStreamingModule : ModuleBase
{
    private readonly ConcurrentQueue<StreamEvent> _eventHistory = new();
    private readonly ConcurrentDictionary<string, EventSubscription> _subscriptions = new();
    private readonly ConcurrentDictionary<string, CrossServiceSubscription> _crossServiceSubscriptions = new();
    private readonly object _lock = new object();
    private int _maxHistorySize = 1000;

    public override string Name => "Event Streaming Module";
    public override string Description => "Event streaming module for live updates on node/edge changes";
    public override string Version => "1.0.0";

    public EventStreamingModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        // No direct module dependencies - use api-router for inter-module communication
    }

    // Helper method to publish system events via api-router
    private async Task PublishSystemEventViaApiRouter(string eventType, StreamEvent streamEvent, string? userId = null)
    {
        try
        {
            if (_apiRouter == null)
            {
                _logger.Warn("ApiRouter not available - cannot publish system event");
                return;
            }

            // Call RealtimeModule's PublishSystemEvent API via router
            var request = JsonSerializer.Serialize(new
            {
                eventType = eventType,
                eventData = streamEvent,
                userId = userId
            });

            var requestElement = JsonDocument.Parse(request).RootElement;
            
            if (_apiRouter.TryGetHandler("codex.realtime", "PublishSystemEvent", out var handler))
            {
                await handler(requestElement);
                _logger.Debug($"Published system event {eventType} via api-router");
            }
            else
            {
                _logger.Warn($"RealtimeModule PublishSystemEvent handler not found - event {eventType} not published");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to publish system event via api-router: {ex.Message}");
        }
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.event-streaming",
            name: "Event Streaming Module",
            version: "0.1.0",
            description: "Provides event streaming for live updates on node/edge changes",
            tags: new[] { "event-streaming", "real-time", "updates", "subscription" },
            capabilities: new[] { "event_streaming", "event_history", "event_subscription", "event_filtering", "event_aggregation", "event_replay" },
            spec: "codex.spec.event-streaming"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Event Streaming API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store CoreApiService reference for cross-service communication
        _coreApiService = coreApi;
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

    [ApiRoute("POST", "/events/subscribe", "SubscribeToEventStream", "Subscribe to specific event types", "codex.event-streaming")]
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

    /// <summary>
    /// Publish event to other services in the ecosystem
    /// </summary>
    [ApiRoute("POST", "/events/publish-cross-service", "PublishCrossServiceEvent", "Publish event to other services", "codex.event-streaming")]
    public async Task<object> PublishCrossServiceEventAsync([ApiParameter("request", "Cross-service event publish request")] CrossServiceEventRequest request)
    {
        try
        {
            var streamEvent = new StreamEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = request.EventType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Data = request.Data,
                UserId = request.UserId,
                Timestamp = DateTimeOffset.UtcNow,
                Description = request.Description ?? $"{request.EventType} for {request.EntityType} {request.EntityId}",
                SourceServiceId = request.SourceServiceId,
                TargetServices = request.TargetServices
            };

            // Add to local history
            _eventHistory.Enqueue(streamEvent);

            // Maintain history size
            lock (_lock)
            {
                while (_eventHistory.Count > _maxHistorySize)
                {
                    _eventHistory.TryDequeue(out _);
                }
            }

            // Notify local real-time subscribers
            if (_realtimeModule != null)
            {
                await _realtimeModule.PublishSystemEventAsync(request.EventType, streamEvent, request.UserId);
            }

            // Notify local subscribers
            await NotifySubscribersAsync(streamEvent);

            // Publish to target services
            var crossServiceTasks = new List<Task>();
            foreach (var targetService in request.TargetServices ?? new List<string>())
            {
                crossServiceTasks.Add(PublishToServiceAsync(targetService, streamEvent));
            }

            await Task.WhenAll(crossServiceTasks);

            _logger.Info($"Published cross-service event: {request.EventType} to {request.TargetServices?.Count ?? 0} services");
            
            return new
            {
                success = true,
                eventId = streamEvent.EventId,
                publishedToServices = request.TargetServices?.Count ?? 0,
                message = "Cross-service event published successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error publishing cross-service event: {ex.Message}", ex);
            return new ErrorResponse($"Failed to publish cross-service event: {ex.Message}");
        }
    }

    /// <summary>
    /// Exchange a concept between services using event-driven approach
    /// </summary>
    [ApiRoute("POST", "/events/exchange-concept", "ExchangeConcept", "Exchange a concept between services", "codex.event-streaming")]
    public async Task<object> ExchangeConceptAsync([ApiParameter("request", "Concept exchange request")] ConceptExchangeRequest request)
    {
        try
        {
            if (_coreApiService == null)
            {
                return new ErrorResponse("CoreApiService not available for concept exchange");
            }

            // Publish concept exchange request event
            var exchangeEvent = new StreamEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventType = "concept_exchange_request",
                EntityType = "concept",
                EntityId = request.ConceptId,
                Data = new Dictionary<string, object>
                {
                    ["sourceServiceId"] = request.SourceServiceId,
                    ["targetServiceId"] = request.TargetServiceId,
                    ["conceptId"] = request.ConceptId,
                    ["targetBeliefSystem"] = request.TargetBeliefSystem ?? new Dictionary<string, object>(),
                    ["translationPreferences"] = request.TranslationPreferences ?? new Dictionary<string, object>()
                },
                UserId = request.UserId,
                Timestamp = DateTimeOffset.UtcNow,
                Description = $"Concept exchange request for {request.ConceptId}",
                SourceServiceId = request.SourceServiceId,
                TargetServices = new List<string> { request.TargetServiceId }
            };

            // Publish the exchange event
            var publishRequest = new CrossServiceEventRequest(
                EventType: "concept_exchange_request",
                EntityType: "concept",
                EntityId: request.ConceptId,
                Data: exchangeEvent.Data,
                UserId: request.UserId,
                SourceServiceId: request.SourceServiceId,
                TargetServices: new List<string> { request.TargetServiceId },
                Description: $"Concept exchange request for {request.ConceptId}"
            );

            var publishResult = await PublishCrossServiceEventAsync(publishRequest);

            return new
            {
                success = true,
                exchangeId = exchangeEvent.EventId,
                message = "Concept exchange request published successfully",
                targetService = request.TargetServiceId
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error exchanging concept: {ex.Message}", ex);
            return new ErrorResponse($"Failed to exchange concept: {ex.Message}");
        }
    }

    /// <summary>
    /// Subscribe to events from other services
    /// </summary>
    [ApiRoute("POST", "/events/subscribe-cross-service", "SubscribeToCrossServiceEvents", "Subscribe to events from other services", "codex.event-streaming")]
    public async Task<object> SubscribeToCrossServiceEventsAsync([ApiParameter("request", "Cross-service subscription request")] CrossServiceSubscriptionRequest request)
    {
        try
        {
            var subscription = new CrossServiceSubscription(
                SubscriptionId: Guid.NewGuid().ToString(),
                SourceServiceId: request.SourceServiceId,
                EventTypes: request.EventTypes ?? new List<string>(),
                EntityTypes: request.EntityTypes ?? new List<string>(),
                EntityIds: request.EntityIds ?? new List<string>(),
                CallbackUrl: request.CallbackUrl,
                Filters: request.Filters ?? new Dictionary<string, object>(),
                CreatedAt: DateTimeOffset.UtcNow,
                IsActive: true
            );

            _crossServiceSubscriptions[subscription.SubscriptionId] = subscription;

            _logger.Info($"Created cross-service subscription: {subscription.SubscriptionId} for service: {request.SourceServiceId}");
            
            return new
            {
                success = true,
                subscriptionId = subscription.SubscriptionId,
                message = "Cross-service subscription created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating cross-service subscription: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create cross-service subscription: {ex.Message}");
        }
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

    /// <summary>
    /// Publish event to a specific service using CoreApiService
    /// </summary>
    private async Task PublishToServiceAsync(string serviceId, StreamEvent streamEvent)
    {
        try
        {
            if (_coreApiService == null)
            {
                _logger.Warn("CoreApiService not available for cross-service publishing");
                return;
            }

            // Use CoreApiService to call the target service's event handler
            var call = new DynamicCall(
                ModuleId: serviceId,
                Api: "event-receive",
                Args: JsonSerializer.SerializeToElement(new
                {
                    EventId = streamEvent.EventId,
                    EventType = streamEvent.EventType,
                    EntityType = streamEvent.EntityType,
                    EntityId = streamEvent.EntityId,
                    Data = streamEvent.Data,
                    UserId = streamEvent.UserId,
                    Timestamp = streamEvent.Timestamp,
                    Description = streamEvent.Description,
                    SourceServiceId = streamEvent.SourceServiceId
                })
            );

            await _coreApiService.ExecuteDynamicCall(call);
            _logger.Debug($"Published event {streamEvent.EventId} to service {serviceId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error publishing event to service {serviceId}: {ex.Message}", ex);
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

    public record ConceptExchangeRequest(
        string SourceServiceId,
        string TargetServiceId,
        string ConceptId,
        string? UserId = null,
        Dictionary<string, object>? TargetBeliefSystem = null,
        Dictionary<string, object>? TranslationPreferences = null
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

    public record CrossServiceEventRequest(
        string EventType,
        string EntityType,
        string EntityId,
        object Data,
        string? UserId = null,
        string? Description = null,
        string? SourceServiceId = null,
        List<string>? TargetServices = null
    );

    public record CrossServiceSubscriptionRequest(
        string SourceServiceId,
        List<string>? EventTypes = null,
        List<string>? EntityTypes = null,
        List<string>? EntityIds = null,
        string? CallbackUrl = null,
        Dictionary<string, object>? Filters = null
    );

    public record CrossServiceSubscription(
        string SubscriptionId,
        string SourceServiceId,
        List<string> EventTypes,
        List<string> EntityTypes,
        List<string> EntityIds,
        string? CallbackUrl,
        Dictionary<string, object> Filters,
        DateTimeOffset CreatedAt,
        bool IsActive
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
        string? Description = null,
        string? SourceServiceId = null,
        List<string>? TargetServices = null
    )
    {
        public StreamEvent() : this("", "", "", "", new { }, null, DateTimeOffset.UtcNow, null, null, null) { }
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
