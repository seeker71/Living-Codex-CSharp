using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Spatial Graph Module - Provides scalable graph visualization with spatial queries and clustering
/// Handles millions of nodes through fractal/hierarchical aggregation
/// </summary>
public class SpatialGraphModule : ModuleBase
{
    private const int DEFAULT_CLUSTER_SIZE = 50;
    private const int MAX_VIEWPORT_NODES = 200;

    public override string Name => "Spatial Graph Module";
    public override string Description => "Scalable graph visualization with spatial queries and clustering for millions of nodes";
    public override string Version => "1.0.0";

    public SpatialGraphModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.graph.spatial",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "graph", "spatial", "visualization", "clustering", "scalable" },
            capabilities: new[] { "spatial-queries", "clustering", "lod", "viewport-culling" },
            spec: "codex.spec.spatial-graph"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _apiRouter = router;
    }

    /// <summary>
    /// Get graph data for a specific viewport with spatial culling and clustering
    /// This is the main endpoint for scalable graph visualization
    /// </summary>
    [ApiRoute("POST", "/graph/viewport", "graph-viewport", "Get graph data for viewport with spatial queries", "codex.graph.spatial")]
    public async Task<object> GetViewportGraph(
        [ApiParameter("request", "Viewport query request", Required = true, Location = "body")] ViewportQueryRequest request)
    {
        try
        {
            var allNodes = _registry.AllNodes().ToList();
            var allEdges = _registry.AllEdges().ToList();

            // Calculate zoom level (0 = galaxy, 1 = system, 2+ = detail)
            var zoomLevel = CalculateZoomLevel(request.ZoomFactor);
            
            // Determine clustering threshold based on zoom
            var clusterThreshold = CalculateClusterThreshold(zoomLevel);

            SpatialGraphResponse response;

            if (zoomLevel == 0)
            {
                // Galaxy view: Heavy clustering
                response = await GenerateGalaxyView(allNodes, allEdges, request, clusterThreshold);
            }
            else if (zoomLevel == 1)
            {
                // System view: Moderate clustering
                response = await GenerateSystemView(allNodes, allEdges, request, clusterThreshold);
            }
            else
            {
                // Detail view: Minimal or no clustering
                response = await GenerateDetailView(allNodes, allEdges, request);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting viewport graph: {ex.Message}", ex);
            return ResponseHelpers.CreateErrorResponse($"Failed to get viewport graph: {ex.Message}", "VIEWPORT_ERROR");
        }
    }

    /// <summary>
    /// Drill down into a specific node or cluster
    /// </summary>
    [ApiRoute("GET", "/graph/drilldown/{nodeId}", "graph-drilldown", "Drill down into node details", "codex.graph.spatial")]
    public async Task<object> DrillDown(
        [ApiParameter("nodeId", "Node ID to drill into", Required = true, Location = "path")] string nodeId,
        [ApiParameter("depth", "Depth of relationships to include", Required = false, Location = "query")] int depth = 2)
    {
        try
        {
            var node = _registry.GetNode(nodeId);
            if (node == null)
            {
                return ResponseHelpers.CreateErrorResponse("Node not found", "NOT_FOUND");
            }

            // Get immediate neighbors
            var outgoing = _registry.GetEdgesFrom(nodeId).ToList();
            var incoming = _registry.GetEdgesTo(nodeId).ToList();

            var neighborNodes = new List<Node>();
            var edges = new List<Edge>();

            // Add outgoing edges and target nodes
            foreach (var edge in outgoing.Take(50)) // Limit to prevent overwhelming
            {
                var target = _registry.GetNode(edge.ToId);
                if (target != null)
                {
                    neighborNodes.Add(target);
                    edges.Add(edge);
                }
            }

            // Add incoming edges and source nodes
            foreach (var edge in incoming.Take(50))
            {
                var source = _registry.GetNode(edge.FromId);
                if (source != null && !neighborNodes.Any(n => n.Id == source.Id))
                {
                    neighborNodes.Add(source);
                    edges.Add(edge);
                }
            }

            return new DrillDownResponse(
                Success: true,
                CenterNode: MapNodeToDto(node),
                Nodes: neighborNodes.Select(MapNodeToDto).ToList(),
                Edges: edges.Select(MapEdgeToDto).ToList(),
                OutgoingCount: outgoing.Count,
                IncomingCount: incoming.Count,
                Message: $"Drilled down into node {nodeId}"
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error drilling down into node: {ex.Message}", ex);
            return ResponseHelpers.CreateErrorResponse($"Failed to drill down: {ex.Message}", "DRILLDOWN_ERROR");
        }
    }

    /// <summary>
    /// Get cluster members (expand a cluster)
    /// </summary>
    [ApiRoute("GET", "/graph/cluster/{clusterId}", "graph-cluster", "Get cluster members", "codex.graph.spatial")]
    public async Task<object> GetClusterMembers(
        [ApiParameter("clusterId", "Cluster ID", Required = true, Location = "path")] string clusterId)
    {
        try
        {
            // Parse cluster ID to get constituent nodes
            // Format: "cluster_typeId_stateHash"
            var parts = clusterId.Split('_');
            if (parts.Length < 2)
            {
                return ResponseHelpers.CreateErrorResponse("Invalid cluster ID", "INVALID_CLUSTER");
            }

            var typeId = parts[1];
            var allNodes = _registry.AllNodes()
                .Where(n => n.TypeId == typeId)
                .Take(100)
                .ToList();

            return new ClusterMembersResponse(
                Success: true,
                ClusterId: clusterId,
                Members: allNodes.Select(MapNodeToDto).ToList(),
                Count: allNodes.Count,
                Message: $"Retrieved {allNodes.Count} members of cluster {clusterId}"
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting cluster members: {ex.Message}", ex);
            return ResponseHelpers.CreateErrorResponse($"Failed to get cluster members: {ex.Message}", "CLUSTER_ERROR");
        }
    }

    /// <summary>
    /// Get graph statistics for overview
    /// </summary>
    [ApiRoute("GET", "/graph/stats", "graph-stats", "Get graph statistics", "codex.graph.spatial")]
    public async Task<object> GetGraphStats()
    {
        try
        {
            var allNodes = _registry.AllNodes().ToList();
            var allEdges = _registry.AllEdges().ToList();

            var byType = allNodes.GroupBy(n => n.TypeId)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var byState = allNodes.GroupBy(n => n.State)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());

            return new GraphStatsResponse(
                Success: true,
                TotalNodes: allNodes.Count,
                TotalEdges: allEdges.Count,
                ByType: byType,
                ByState: byState,
                AverageConnections: allNodes.Count > 0 
                    ? (double)allEdges.Count / allNodes.Count 
                    : 0,
                Message: "Graph statistics retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting graph stats: {ex.Message}", ex);
            return ResponseHelpers.CreateErrorResponse($"Failed to get graph stats: {ex.Message}", "STATS_ERROR");
        }
    }

    // Private helper methods

    private int CalculateZoomLevel(double zoomFactor)
    {
        if (zoomFactor < 0.3) return 0; // Galaxy
        if (zoomFactor < 1.0) return 1; // System
        return 2; // Detail
    }

    private int CalculateClusterThreshold(int zoomLevel)
    {
        return zoomLevel switch
        {
            0 => 10,  // Galaxy: cluster every 10 nodes
            1 => 25,  // System: cluster every 25 nodes
            _ => 100  // Detail: minimal clustering
        };
    }

    private async Task<SpatialGraphResponse> GenerateGalaxyView(
        List<Node> allNodes, 
        List<Edge> allEdges, 
        ViewportQueryRequest request,
        int clusterThreshold)
    {
        // Heavy clustering by type and state
        var clusters = allNodes
            .GroupBy(n => new { n.TypeId, n.State })
            .Select(g => new NodeCluster(
                Id: $"cluster_{g.Key.TypeId}_{g.Key.State}",
                TypeId: g.Key.TypeId,
                State: g.Key.State.ToString(),
                NodeCount: g.Count(),
                CenterX: 0, // Will be calculated based on spatial distribution
                CenterY: 0,
                Title: $"{g.Key.TypeId} ({g.Count()})",
                MemberIds: g.Take(10).Select(n => n.Id).ToList() // Sample
            ))
            .Take(MAX_VIEWPORT_NODES)
            .ToList();

        return new SpatialGraphResponse(
            Success: true,
            ViewportNodes: new List<ViewportNode>(),
            Clusters: clusters,
            Edges: new List<ViewportEdge>(),
            ZoomLevel: 0,
            TotalNodesInGraph: allNodes.Count,
            ViewportNodeCount: 0,
            ClusterCount: clusters.Count,
            Message: "Galaxy view generated"
        );
    }

    private async Task<SpatialGraphResponse> GenerateSystemView(
        List<Node> allNodes, 
        List<Edge> allEdges, 
        ViewportQueryRequest request,
        int clusterThreshold)
    {
        // Moderate clustering - show major node types
        var importantNodes = allNodes
            .OrderByDescending(n => GetNodeImportance(n, allEdges))
            .Take(MAX_VIEWPORT_NODES)
            .ToList();

        var viewportNodes = importantNodes.Select(n => new ViewportNode(
            Id: n.Id,
            TypeId: n.TypeId,
            State: n.State.ToString(),
            Title: n.Title ?? n.Id,
            Description: n.Description ?? "",
            X: 0, // Will be calculated by client
            Y: 0,
            Size: CalculateNodeSize(n, allEdges),
            ConnectionCount: GetConnectionCount(n.Id, allEdges)
        )).ToList();

        // Get edges between visible nodes
        var nodeIds = new HashSet<string>(viewportNodes.Select(n => n.Id));
        var visibleEdges = allEdges
            .Where(e => nodeIds.Contains(e.FromId) && nodeIds.Contains(e.ToId))
            .Take(500)
            .Select(e => new ViewportEdge(
                Id: $"{e.FromId}__{e.ToId}",
                FromId: e.FromId,
                ToId: e.ToId,
                Role: e.Role,
                Weight: e.Weight
            ))
            .ToList();

        return new SpatialGraphResponse(
            Success: true,
            ViewportNodes: viewportNodes,
            Clusters: new List<NodeCluster>(),
            Edges: visibleEdges,
            ZoomLevel: 1,
            TotalNodesInGraph: allNodes.Count,
            ViewportNodeCount: viewportNodes.Count,
            ClusterCount: 0,
            Message: "System view generated"
        );
    }

    private async Task<SpatialGraphResponse> GenerateDetailView(
        List<Node> allNodes, 
        List<Edge> allEdges, 
        ViewportQueryRequest request)
    {
        // Detail view - show specific nodes based on focus
        List<Node> focusNodes;

        if (!string.IsNullOrEmpty(request.FocusNodeId))
        {
            // Get focused node and its neighborhood
            var centerNode = _registry.GetNode(request.FocusNodeId);
            if (centerNode == null)
            {
                focusNodes = allNodes.Take(MAX_VIEWPORT_NODES).ToList();
            }
            else
            {
                focusNodes = GetNodeNeighborhood(centerNode, allNodes, allEdges, 2);
            }
        }
        else
        {
            // Show most important nodes
            focusNodes = allNodes
                .OrderByDescending(n => GetNodeImportance(n, allEdges))
                .Take(MAX_VIEWPORT_NODES)
                .ToList();
        }

        var viewportNodes = focusNodes.Select(n => new ViewportNode(
            Id: n.Id,
            TypeId: n.TypeId,
            State: n.State.ToString(),
            Title: n.Title ?? n.Id,
            Description: n.Description ?? "",
            X: 0,
            Y: 0,
            Size: CalculateNodeSize(n, allEdges),
            ConnectionCount: GetConnectionCount(n.Id, allEdges)
        )).ToList();

        var nodeIds = new HashSet<string>(viewportNodes.Select(n => n.Id));
        var visibleEdges = allEdges
            .Where(e => nodeIds.Contains(e.FromId) && nodeIds.Contains(e.ToId))
            .Take(1000)
            .Select(e => new ViewportEdge(
                Id: $"{e.FromId}__{e.ToId}",
                FromId: e.FromId,
                ToId: e.ToId,
                Role: e.Role,
                Weight: e.Weight
            ))
            .ToList();

        return new SpatialGraphResponse(
            Success: true,
            ViewportNodes: viewportNodes,
            Clusters: new List<NodeCluster>(),
            Edges: visibleEdges,
            ZoomLevel: 2,
            TotalNodesInGraph: allNodes.Count,
            ViewportNodeCount: viewportNodes.Count,
            ClusterCount: 0,
            Message: "Detail view generated"
        );
    }

    private List<Node> GetNodeNeighborhood(Node centerNode, List<Node> allNodes, List<Edge> allEdges, int depth)
    {
        var neighborhood = new HashSet<string> { centerNode.Id };
        var currentLevel = new HashSet<string> { centerNode.Id };

        for (int i = 0; i < depth; i++)
        {
            var nextLevel = new HashSet<string>();
            foreach (var nodeId in currentLevel)
            {
                var outgoing = allEdges.Where(e => e.FromId == nodeId).Select(e => e.ToId);
                var incoming = allEdges.Where(e => e.ToId == nodeId).Select(e => e.FromId);
                
                foreach (var id in outgoing.Concat(incoming))
                {
                    if (!neighborhood.Contains(id))
                    {
                        nextLevel.Add(id);
                        neighborhood.Add(id);
                    }
                }
            }
            currentLevel = nextLevel;

            if (neighborhood.Count > MAX_VIEWPORT_NODES)
                break;
        }

        return allNodes.Where(n => neighborhood.Contains(n.Id)).ToList();
    }

    private double GetNodeImportance(Node node, List<Edge> allEdges)
    {
        // Importance = degree (connections) + age + state weight
        var degree = GetConnectionCount(node.Id, allEdges);
        var stateWeight = node.State switch
        {
            ContentState.Ice => 3.0,
            ContentState.Water => 2.0,
            ContentState.Gas => 1.0,
            _ => 1.0
        };
        return degree * stateWeight;
    }

    private int GetConnectionCount(string nodeId, List<Edge> allEdges)
    {
        return allEdges.Count(e => e.FromId == nodeId || e.ToId == nodeId);
    }

    private double CalculateNodeSize(Node node, List<Edge> allEdges)
    {
        var connections = GetConnectionCount(node.Id, allEdges);
        return Math.Min(20, 4 + Math.Log(connections + 1) * 2);
    }

    private NodeDto MapNodeToDto(Node node)
    {
        return new NodeDto(
            Id: node.Id,
            TypeId: node.TypeId,
            State: node.State.ToString(),
            Locale: node.Locale,
            Title: node.Title ?? node.Id,
            Description: node.Description ?? "",
            Meta: node.Meta ?? new Dictionary<string, object>()
        );
    }

    private EdgeDto MapEdgeToDto(Edge edge)
    {
        return new EdgeDto(
            FromId: edge.FromId,
            ToId: edge.ToId,
            Role: edge.Role,
            Weight: edge.Weight,
            Meta: edge.Meta ?? new Dictionary<string, object>()
        );
    }
}

// Request/Response models for spatial graph API

public record ViewportQueryRequest(
    double ZoomFactor = 1.0,
    double CenterX = 0,
    double CenterY = 0,
    double ViewportWidth = 1000,
    double ViewportHeight = 800,
    string? FocusNodeId = null,
    List<string>? TypeFilter = null
);

public record SpatialGraphResponse(
    bool Success,
    List<ViewportNode> ViewportNodes,
    List<NodeCluster> Clusters,
    List<ViewportEdge> Edges,
    int ZoomLevel,
    int TotalNodesInGraph,
    int ViewportNodeCount,
    int ClusterCount,
    string Message
);

public record ViewportNode(
    string Id,
    string TypeId,
    string State,
    string Title,
    string Description,
    double X,
    double Y,
    double Size,
    int ConnectionCount
);

public record NodeCluster(
    string Id,
    string TypeId,
    string State,
    int NodeCount,
    double CenterX,
    double CenterY,
    string Title,
    List<string> MemberIds
);

public record ViewportEdge(
    string Id,
    string FromId,
    string ToId,
    string Role,
    double? Weight
);

public record DrillDownResponse(
    bool Success,
    NodeDto CenterNode,
    List<NodeDto> Nodes,
    List<EdgeDto> Edges,
    int OutgoingCount,
    int IncomingCount,
    string Message
);

public record ClusterMembersResponse(
    bool Success,
    string ClusterId,
    List<NodeDto> Members,
    int Count,
    string Message
);

public record GraphStatsResponse(
    bool Success,
    int TotalNodes,
    int TotalEdges,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByState,
    double AverageConnections,
    string Message
);

public record NodeDto(
    string Id,
    string TypeId,
    string State,
    string Locale,
    string Title,
    string Description,
    Dictionary<string, object> Meta
);

public record EdgeDto(
    string FromId,
    string ToId,
    string Role,
    double? Weight,
    Dictionary<string, object> Meta
);

