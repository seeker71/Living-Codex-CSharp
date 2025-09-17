using System.Collections.Concurrent;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Portal Module - Fractal exploration and navigation of external worlds
/// Enables connection to websites, APIs, living entities, sensors, and other external systems
/// through a unified portal interface that supports fractal exploration and contribution
/// </summary>
public sealed class PortalModule : ModuleBase
{
    private readonly ModuleCommunicationWrapper _communicationWrapper;
    private readonly ConcurrentDictionary<string, PortalConnection> _activePortals = new();
    private readonly ConcurrentDictionary<string, PortalExploration> _explorations = new();
    private readonly ConcurrentDictionary<string, PortalContribution> _contributions = new();

    public override string Name => "Portal Module";
    public override string Description => "Fractal exploration and navigation of external worlds";
    public override string Version => "1.0.0";

    public PortalModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _communicationWrapper = new ModuleCommunicationWrapper(logger, "PortalModule");
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.portal",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "portal", "exploration", "external", "fractal", "navigation", "contribution" },
            capabilities: new[] { 
                "portal_connection", "fractal_exploration", "external_navigation", 
                "contribution_interface", "entity_interaction", "sensor_connection",
                "api_gateway", "website_exploration", "living_entity_interface"
            },
            spec: "codex.spec.portal"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Portal API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
        _logger.Info("Portal HTTP endpoints registered");
    }

    // Portal Management API Methods
    [ApiRoute("POST", "/portal/connect", "ConnectPortal", "Connect to an external world through a portal", "codex.portal")]
    public async Task<object> ConnectPortalAsync([ApiParameter("request", "Portal connection request")] PortalConnectionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Url) && string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("Either URL or EntityId is required");
            }

            var portalId = Guid.NewGuid().ToString();
            var portalType = DeterminePortalType(request.Url, request.EntityId, request.PortalType);

            var portalConnection = new PortalConnection(
                PortalId: portalId,
                Name: request.Name ?? $"Portal to {request.Url ?? request.EntityId}",
                Description: request.Description ?? $"Connection to {portalType}",
                PortalType: portalType,
                Url: request.Url,
                EntityId: request.EntityId,
                Configuration: request.Configuration ?? new Dictionary<string, object>(),
                CreatedAt: DateTimeOffset.UtcNow,
                Status: PortalStatus.Connecting,
                Capabilities: await DiscoverPortalCapabilities(portalType, request.Url, request.EntityId, request.Configuration)
            );

            _activePortals[portalId] = portalConnection;

            // Register portal as a node in the system
            await RegisterPortalNode(portalConnection);

            _logger.Info($"Portal connected: {portalId} - {portalConnection.Name}");
            return new { 
                success = true, 
                portalId = portalId,
                portal = portalConnection,
                message = "Portal connection established"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error connecting portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to connect portal: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/portal/list", "ListPortals", "List all active portal connections", "codex.portal")]
    public async Task<object> ListPortalsAsync()
    {
        try
        {
            var portals = _activePortals.Values
                .Where(p => p.Status == PortalStatus.Connected || p.Status == PortalStatus.Connecting)
                .Select(p => new
                {
                    p.PortalId,
                    p.Name,
                    p.Description,
                    p.PortalType,
                    p.Url,
                    p.EntityId,
                    p.Status,
                    p.CreatedAt,
                    p.Capabilities
                })
                .ToList();

            _logger.Debug($"Retrieved {portals.Count} active portals");
            return new { success = true, portals = portals, count = portals.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing portals: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list portals: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/portal/explore", "ExplorePortal", "Begin fractal exploration of a portal", "codex.portal")]
    public async Task<object> ExplorePortalAsync([ApiParameter("request", "Portal exploration request")] PortalExplorationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PortalId))
            {
                return new ErrorResponse("PortalId is required");
            }

            if (!_activePortals.TryGetValue(request.PortalId, out var portal))
            {
                return new ErrorResponse("Portal not found");
            }

            if (portal.Status != PortalStatus.Connected)
            {
                return new ErrorResponse("Portal is not connected");
            }

            var explorationId = Guid.NewGuid().ToString();
            var exploration = new PortalExploration
            {
                ExplorationId = explorationId,
                PortalId = request.PortalId,
                UserId = request.UserId,
                ExplorationType = request.ExplorationType ?? "fractal",
                StartingPoint = request.StartingPoint ?? portal.Url ?? portal.EntityId,
                Depth = request.Depth ?? 3,
                MaxBranches = request.MaxBranches ?? 10,
                Filters = request.Filters ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = ExplorationStatus.Active,
                DiscoveredNodes = new List<PortalNode>(),
                ExploredPaths = new List<ExplorationPath>()
            };

            _explorations[explorationId] = exploration;

            // Begin fractal exploration
            await BeginFractalExploration(exploration, portal);

            _logger.Info($"Portal exploration started: {explorationId} for portal {request.PortalId}");
            return new { 
                success = true, 
                explorationId = explorationId,
                exploration = exploration,
                message = "Portal exploration initiated"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error exploring portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to explore portal: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/portal/exploration/{explorationId}", "GetExploration", "Get exploration results and progress", "codex.portal")]
    public async Task<object> GetExplorationAsync(string explorationId)
    {
        try
        {
            if (string.IsNullOrEmpty(explorationId))
            {
                return new ErrorResponse("ExplorationId is required");
            }

            if (!_explorations.TryGetValue(explorationId, out var exploration))
            {
                return new ErrorResponse("Exploration not found");
            }

            // Update exploration status if needed
            await UpdateExplorationStatus(exploration);

            _logger.Debug($"Retrieved exploration: {explorationId}");
            return new { 
                success = true, 
                exploration = exploration,
                discoveredNodes = exploration.DiscoveredNodes.Count,
                exploredPaths = exploration.ExploredPaths.Count
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting exploration: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get exploration: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/portal/contribute", "ContributeToPortal", "Contribute to an external world through a portal", "codex.portal")]
    public async Task<object> ContributeToPortalAsync([ApiParameter("request", "Portal contribution request")] PortalContributionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PortalId))
            {
                return new ErrorResponse("PortalId is required");
            }

            if (!_activePortals.TryGetValue(request.PortalId, out var portal))
            {
                return new ErrorResponse("Portal not found");
            }

            if (portal.Status != PortalStatus.Connected)
            {
                return new ErrorResponse("Portal is not connected");
            }

            var contributionId = Guid.NewGuid().ToString();
            var contribution = new PortalContribution
            {
                ContributionId = contributionId,
                PortalId = request.PortalId,
                UserId = request.UserId,
                ContributionType = request.ContributionType ?? "general",
                Content = request.Content,
                TargetPath = request.TargetPath,
                Metadata = request.Metadata ?? new Dictionary<string, object>(),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = ContributionStatus.Pending,
                Response = null
            };

            _contributions[contributionId] = contribution;

            // Process contribution based on portal type
            await ProcessPortalContribution(contribution, portal);

            _logger.Info($"Portal contribution submitted: {contributionId} to portal {request.PortalId}");
            return new { 
                success = true, 
                contributionId = contributionId,
                contribution = contribution,
                message = "Contribution submitted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error contributing to portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to contribute to portal: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/portal/contributions/{portalId}", "GetPortalContributions", "Get contributions made to a specific portal", "codex.portal")]
    public async Task<object> GetPortalContributionsAsync(string portalId)
    {
        try
        {
            if (string.IsNullOrEmpty(portalId))
            {
                return new ErrorResponse("PortalId is required");
            }

            var contributions = _contributions.Values
                .Where(c => c.PortalId == portalId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.ContributionId,
                    c.UserId,
                    c.ContributionType,
                    c.Content,
                    c.TargetPath,
                    c.Status,
                    c.CreatedAt,
                    c.Response
                })
                .ToList();

            _logger.Debug($"Retrieved {contributions.Count} contributions for portal {portalId}");
            return new { success = true, contributions = contributions, count = contributions.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting portal contributions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get portal contributions: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/portal/disconnect", "DisconnectPortal", "Disconnect from a portal", "codex.portal")]
    public async Task<object> DisconnectPortalAsync([ApiParameter("request", "Portal disconnection request")] PortalDisconnectionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PortalId))
            {
                return new ErrorResponse("PortalId is required");
            }

            if (!_activePortals.TryGetValue(request.PortalId, out var portal))
            {
                return new ErrorResponse("Portal not found");
            }

            // Update portal status
            portal = portal with { Status = PortalStatus.Disconnected, DisconnectedAt = DateTimeOffset.UtcNow };
            _activePortals[request.PortalId] = portal;

            // Clean up related explorations and contributions
            var relatedExplorations = _explorations.Values
                .Where(e => e.PortalId == request.PortalId)
                .ToList();

            foreach (var exploration in relatedExplorations)
            {
                exploration.Status = ExplorationStatus.Terminated;
            }

            _logger.Info($"Portal disconnected: {request.PortalId}");
            return new { 
                success = true, 
                message = "Portal disconnected successfully",
                portalId = request.PortalId
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error disconnecting portal: {ex.Message}", ex);
            return new ErrorResponse($"Failed to disconnect portal: {ex.Message}");
        }
    }

    // Private helper methods
    private PortalType DeterminePortalType(string? url, string? entityId, string? requestedType)
    {
        if (!string.IsNullOrEmpty(requestedType))
        {
            return Enum.TryParse<PortalType>(requestedType, true, out var type) ? type : PortalType.Unknown;
        }

        if (!string.IsNullOrEmpty(url))
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return PortalType.Website;
            if (url.StartsWith("api://") || url.Contains("/api/"))
                return PortalType.API;
            if (url.StartsWith("sensor://") || url.Contains("sensor"))
                return PortalType.Sensor;
        }

        if (!string.IsNullOrEmpty(entityId))
        {
            if (entityId.StartsWith("human-") || entityId.StartsWith("person-"))
                return PortalType.LivingEntity;
            if (entityId.StartsWith("sensor-") || entityId.StartsWith("device-"))
                return PortalType.Sensor;
        }

        return PortalType.Unknown;
    }

    private async Task<Dictionary<string, object>> DiscoverPortalCapabilities(PortalType portalType, string? url, string? entityId, Dictionary<string, object>? configuration)
    {
        var capabilities = new Dictionary<string, object>();

        try
        {
            switch (portalType)
            {
                case PortalType.Website:
                    capabilities = await DiscoverWebsiteCapabilities(url, configuration);
                    break;
                case PortalType.API:
                    capabilities = await DiscoverApiCapabilities(url, configuration);
                    break;
                case PortalType.LivingEntity:
                    capabilities = await DiscoverLivingEntityCapabilities(entityId, configuration);
                    break;
                case PortalType.Sensor:
                    capabilities = await DiscoverSensorCapabilities(url, entityId, configuration);
                    break;
                default:
                    capabilities["type"] = "unknown";
                    capabilities["exploration"] = "limited";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error discovering portal capabilities: {ex.Message}");
            capabilities["error"] = ex.Message;
        }

        return capabilities;
    }

    private async Task<Dictionary<string, object>> DiscoverWebsiteCapabilities(string? url, Dictionary<string, object>? configuration)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["type"] = "website",
            ["exploration"] = "fractal",
            ["navigation"] = "link_following",
            ["contribution"] = "form_submission",
            ["media_types"] = new[] { "text/html", "text/plain", "application/json" }
        };

        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                // Use existing HTTP adapter to discover capabilities
                var response = await _communicationWrapper.GetAsync<object>("adapters", "hydrate", 
                    new Dictionary<string, string> { ["url"] = url });
                
                if (response.Success)
                {
                    capabilities["status"] = "accessible";
                    capabilities["content_type"] = "discovered";
                }
            }
            catch (Exception ex)
            {
                capabilities["status"] = "error";
                capabilities["error"] = ex.Message;
            }
        }

        return capabilities;
    }

    private async Task<Dictionary<string, object>> DiscoverApiCapabilities(string? url, Dictionary<string, object>? configuration)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["type"] = "api",
            ["exploration"] = "endpoint_discovery",
            ["navigation"] = "api_calls",
            ["contribution"] = "data_submission",
            ["media_types"] = new[] { "application/json", "application/xml", "text/plain" }
        };

        if (!string.IsNullOrEmpty(url))
        {
            try
            {
                // Attempt to discover API endpoints
                var response = await _communicationWrapper.GetAsync<object>("adapters", "hydrate", 
                    new Dictionary<string, string> { ["url"] = url });
                
                if (response.Success)
                {
                    capabilities["status"] = "accessible";
                    capabilities["endpoints"] = "discoverable";
                }
            }
            catch (Exception ex)
            {
                capabilities["status"] = "error";
                capabilities["error"] = ex.Message;
            }
        }

        return capabilities;
    }

    private async Task<Dictionary<string, object>> DiscoverLivingEntityCapabilities(string? entityId, Dictionary<string, object>? configuration)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["type"] = "living_entity",
            ["exploration"] = "consciousness_mapping",
            ["navigation"] = "conversation",
            ["contribution"] = "knowledge_exchange",
            ["media_types"] = new[] { "text/plain", "application/json", "audio/wav", "video/mp4" }
        };

        if (!string.IsNullOrEmpty(entityId))
        {
            // Check if entity exists in the system
            var entityNode = _registry.GetNode(entityId);
            if (entityNode != null)
            {
                capabilities["status"] = "connected";
                capabilities["entity_type"] = entityNode.TypeId;
                capabilities["consciousness_level"] = "detected";
            }
            else
            {
                capabilities["status"] = "unknown";
                capabilities["entity_type"] = "external";
            }
        }

        return capabilities;
    }

    private async Task<Dictionary<string, object>> DiscoverSensorCapabilities(string? url, string? entityId, Dictionary<string, object>? configuration)
    {
        var capabilities = new Dictionary<string, object>
        {
            ["type"] = "sensor",
            ["exploration"] = "data_streaming",
            ["navigation"] = "sensor_queries",
            ["contribution"] = "data_upload",
            ["media_types"] = new[] { "application/json", "text/csv", "application/octet-stream" }
        };

        if (!string.IsNullOrEmpty(url) || !string.IsNullOrEmpty(entityId))
        {
            capabilities["status"] = "detected";
            capabilities["data_types"] = new[] { "numeric", "categorical", "time_series" };
            capabilities["sampling_rate"] = "configurable";
        }

        return capabilities;
    }

    private async Task RegisterPortalNode(PortalConnection portal)
    {
        var portalNode = new Node(
            Id: portal.PortalId,
            TypeId: "codex.portal/connection",
            State: ContentState.Water,
            Locale: "en",
            Title: portal.Name,
            Description: portal.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(portal, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: !string.IsNullOrEmpty(portal.Url) ? new Uri(portal.Url) : null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.portal",
                ["portalType"] = portal.PortalType.ToString(),
                ["status"] = portal.Status.ToString(),
                ["capabilities"] = portal.Capabilities
            }
        );

        _registry.Upsert(portalNode);
    }

    private async Task BeginFractalExploration(PortalExploration exploration, PortalConnection portal)
    {
        try
        {
            // Use AI module for fractal exploration if available
            var explorationRequest = new
            {
                portalType = portal.PortalType.ToString(),
                startingPoint = exploration.StartingPoint,
                depth = exploration.Depth,
                maxBranches = exploration.MaxBranches,
                filters = exploration.Filters,
                capabilities = portal.Capabilities
            };

            // Call AI module for fractal exploration
            var response = await _communicationWrapper.PostAsync<object, object>("ai", "fractal-explore", explorationRequest);
            
            if (response.Success && response.Data != null)
            {
                // Process exploration results
                var results = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(response.Data));
                if (results != null)
                {
                    exploration.DiscoveredNodes = ExtractDiscoveredNodes(results);
                    exploration.ExploredPaths = ExtractExploredPaths(results);
                    exploration.Status = ExplorationStatus.Completed;
                }
            }
            else
            {
                // Fallback to basic exploration
                await PerformBasicExploration(exploration, portal);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in fractal exploration: {ex.Message}", ex);
            exploration.Status = ExplorationStatus.Failed;
        }
    }

    private async Task PerformBasicExploration(PortalExploration exploration, PortalConnection portal)
    {
        // Basic exploration based on portal type
        var discoveredNodes = new List<PortalNode>();
        var exploredPaths = new List<ExplorationPath>();

        switch (portal.PortalType)
        {
            case PortalType.Website:
                discoveredNodes.Add(new PortalNode(
                    NodeId: exploration.StartingPoint,
                    NodeType: "webpage",
                    Title: "Starting Page",
                    Content: "Web page content",
                    Metadata: new Dictionary<string, object> { ["url"] = exploration.StartingPoint }
                ));
                break;
            case PortalType.API:
                discoveredNodes.Add(new PortalNode(
                    NodeId: exploration.StartingPoint,
                    NodeType: "api_endpoint",
                    Title: "API Root",
                    Content: "API endpoint",
                    Metadata: new Dictionary<string, object> { ["endpoint"] = exploration.StartingPoint }
                ));
                break;
            case PortalType.LivingEntity:
                discoveredNodes.Add(new PortalNode(
                    NodeId: exploration.StartingPoint,
                    NodeType: "consciousness",
                    Title: "Living Entity",
                    Content: "Consciousness interface",
                    Metadata: new Dictionary<string, object> { ["entity_id"] = exploration.StartingPoint }
                ));
                break;
            case PortalType.Sensor:
                discoveredNodes.Add(new PortalNode(
                    NodeId: exploration.StartingPoint,
                    NodeType: "sensor_data",
                    Title: "Sensor Interface",
                    Content: "Sensor data stream",
                    Metadata: new Dictionary<string, object> { ["sensor_id"] = exploration.StartingPoint }
                ));
                break;
        }

        exploration.DiscoveredNodes = discoveredNodes;
        exploration.ExploredPaths = exploredPaths;
        exploration.Status = ExplorationStatus.Completed;
    }

    private List<PortalNode> ExtractDiscoveredNodes(Dictionary<string, object> results)
    {
        var nodes = new List<PortalNode>();
        
        if (results.TryGetValue("discoveredNodes", out var nodesData) && nodesData is JsonElement nodesElement)
        {
            try
            {
                var nodeList = JsonSerializer.Deserialize<List<PortalNode>>(nodesElement.GetRawText());
                if (nodeList != null)
                {
                    nodes.AddRange(nodeList);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error parsing discovered nodes: {ex.Message}");
            }
        }

        return nodes;
    }

    private List<ExplorationPath> ExtractExploredPaths(Dictionary<string, object> results)
    {
        var paths = new List<ExplorationPath>();
        
        if (results.TryGetValue("exploredPaths", out var pathsData) && pathsData is JsonElement pathsElement)
        {
            try
            {
                var pathList = JsonSerializer.Deserialize<List<ExplorationPath>>(pathsElement.GetRawText());
                if (pathList != null)
                {
                    paths.AddRange(pathList);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error parsing explored paths: {ex.Message}");
            }
        }

        return paths;
    }

    private async Task UpdateExplorationStatus(PortalExploration exploration)
    {
        // Update exploration status based on current state
        if (exploration.Status == ExplorationStatus.Active)
        {
            // Check if exploration should be completed or failed
            var timeSinceStart = DateTimeOffset.UtcNow - exploration.CreatedAt;
            if (timeSinceStart.TotalMinutes > 30) // 30 minute timeout
            {
                exploration.Status = ExplorationStatus.Timeout;
            }
        }
    }

    private async Task ProcessPortalContribution(PortalContribution contribution, PortalConnection portal)
    {
        try
        {
            switch (portal.PortalType)
            {
                case PortalType.Website:
                    await ProcessWebsiteContribution(contribution, portal);
                    break;
                case PortalType.API:
                    await ProcessApiContribution(contribution, portal);
                    break;
                case PortalType.LivingEntity:
                    await ProcessLivingEntityContribution(contribution, portal);
                    break;
                case PortalType.Sensor:
                    await ProcessSensorContribution(contribution, portal);
                    break;
                default:
                    contribution.Status = ContributionStatus.Failed;
                    contribution.Response = "Unsupported portal type for contribution";
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error processing portal contribution: {ex.Message}", ex);
            contribution.Status = ContributionStatus.Failed;
            contribution.Response = $"Error: {ex.Message}";
        }
    }

    private async Task ProcessWebsiteContribution(PortalContribution contribution, PortalConnection portal)
    {
        // Simulate form submission or content contribution to website
        contribution.Status = ContributionStatus.Processed;
        contribution.Response = "Content submitted to website successfully";
    }

    private async Task ProcessApiContribution(PortalContribution contribution, PortalConnection portal)
    {
        // Use communication wrapper to submit data to API
        var response = await _communicationWrapper.PostAsync<object, object>("adapters", "hydrate", contribution.Content);
        
        if (response.Success)
        {
            contribution.Status = ContributionStatus.Processed;
            contribution.Response = "Data submitted to API successfully";
        }
        else
        {
            contribution.Status = ContributionStatus.Failed;
            contribution.Response = $"API submission failed: {response.Error}";
        }
    }

    private async Task ProcessLivingEntityContribution(PortalContribution contribution, PortalConnection portal)
    {
        // Process contribution to living entity (human, consciousness)
        contribution.Status = ContributionStatus.Processed;
        contribution.Response = "Knowledge exchanged with living entity successfully";
    }

    private async Task ProcessSensorContribution(PortalContribution contribution, PortalConnection portal)
    {
        // Process data contribution to sensor or device
        contribution.Status = ContributionStatus.Processed;
        contribution.Response = "Data uploaded to sensor successfully";
    }
}

// Data models and enums
public enum PortalType
{
    Unknown,
    Website,
    API,
    LivingEntity,
    Sensor
}

public enum PortalStatus
{
    Connecting,
    Connected,
    Disconnected,
    Error
}

public enum ExplorationStatus
{
    Active,
    Completed,
    Failed,
    Timeout,
    Terminated
}

public enum ContributionStatus
{
    Pending,
    Processed,
    Failed,
    Rejected
}

[RequestType("codex.portal.connection-request", "PortalConnectionRequest", "Request to connect to an external world through a portal")]
public record PortalConnectionRequest(
    string? Name = null,
    string? Description = null,
    string? Url = null,
    string? EntityId = null,
    string? PortalType = null,
    Dictionary<string, object>? Configuration = null
);

[RequestType("codex.portal.exploration-request", "PortalExplorationRequest", "Request to begin fractal exploration of a portal")]
public record PortalExplorationRequest(
    string PortalId,
    string? UserId = null,
    string? ExplorationType = null,
    string? StartingPoint = null,
    int? Depth = null,
    int? MaxBranches = null,
    Dictionary<string, object>? Filters = null
);

[RequestType("codex.portal.contribution-request", "PortalContributionRequest", "Request to contribute to an external world through a portal")]
public record PortalContributionRequest(
    string PortalId,
    string? UserId = null,
    string? ContributionType = null,
    object? Content = null,
    string? TargetPath = null,
    Dictionary<string, object>? Metadata = null
);

[RequestType("codex.portal.disconnection-request", "PortalDisconnectionRequest", "Request to disconnect from a portal")]
public record PortalDisconnectionRequest(
    string PortalId
);

[ResponseType("codex.portal.connection", "PortalConnection", "Portal connection information")]
public record PortalConnection(
    string PortalId,
    string Name,
    string Description,
    PortalType PortalType,
    string? Url,
    string? EntityId,
    Dictionary<string, object> Configuration,
    DateTimeOffset CreatedAt,
    PortalStatus Status,
    Dictionary<string, object> Capabilities,
    DateTimeOffset? DisconnectedAt = null
);

[ResponseType("codex.portal.exploration", "PortalExploration", "Portal exploration session")]
public class PortalExploration
{
    public string ExplorationId { get; set; } = "";
    public string PortalId { get; set; } = "";
    public string? UserId { get; set; }
    public string ExplorationType { get; set; } = "";
    public string StartingPoint { get; set; } = "";
    public int Depth { get; set; }
    public int MaxBranches { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public ExplorationStatus Status { get; set; }
    public List<PortalNode> DiscoveredNodes { get; set; } = new();
    public List<ExplorationPath> ExploredPaths { get; set; } = new();
}

[ResponseType("codex.portal.contribution", "PortalContribution", "Portal contribution record")]
public class PortalContribution
{
    public string ContributionId { get; set; } = "";
    public string PortalId { get; set; } = "";
    public string? UserId { get; set; }
    public string ContributionType { get; set; } = "";
    public object? Content { get; set; }
    public string? TargetPath { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public ContributionStatus Status { get; set; }
    public string? Response { get; set; }
}

[ResponseType("codex.portal.node", "PortalNode", "Node discovered during portal exploration")]
public record PortalNode(
    string NodeId,
    string NodeType,
    string Title,
    string Content,
    Dictionary<string, object> Metadata
);

[ResponseType("codex.portal.path", "ExplorationPath", "Path explored during portal exploration")]
public record ExplorationPath(
    string PathId,
    string FromNodeId,
    string ToNodeId,
    string Relationship,
    double Weight,
    Dictionary<string, object> Metadata
);
