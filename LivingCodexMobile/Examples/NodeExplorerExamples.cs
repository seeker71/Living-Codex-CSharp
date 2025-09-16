using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.Examples;

/// <summary>
/// Examples of how to use the node explorer service for exploring nodes and edges
/// </summary>
public class NodeExplorerExamples
{
    private readonly INodeExplorerService _nodeExplorerService;
    private readonly IMediaRendererService _mediaRendererService;

    public NodeExplorerExamples(INodeExplorerService nodeExplorerService, IMediaRendererService mediaRendererService)
    {
        _nodeExplorerService = nodeExplorerService;
        _mediaRendererService = mediaRendererService;
    }

    // Example 1: Get all nodes
    public async Task<List<Node>> GetAllNodesAsync()
    {
        return await _nodeExplorerService.GetNodesAsync();
    }

    // Example 2: Get nodes by type
    public async Task<List<Node>> GetNodesByTypeAsync(string typeId)
    {
        var query = new NodeListQuery { TypeId = typeId };
        return await _nodeExplorerService.GetNodesAsync(query);
    }

    // Example 3: Search nodes
    public async Task<List<Node>> SearchNodesAsync(string searchTerm)
    {
        var request = new NodeSearchRequest 
        { 
            Query = searchTerm,
            Limit = 50,
            Skip = 0
        };
        return await _nodeExplorerService.SearchNodesAsync(request);
    }

    // Example 4: Get a specific node
    public async Task<Node?> GetNodeAsync(string nodeId)
    {
        return await _nodeExplorerService.GetNodeAsync(nodeId);
    }

    // Example 5: Get all edges
    public async Task<List<Edge>> GetAllEdgesAsync()
    {
        return await _nodeExplorerService.GetEdgesAsync();
    }

    // Example 6: Get edges from a specific node
    public async Task<List<Edge>> GetEdgesFromNodeAsync(string nodeId)
    {
        return await _nodeExplorerService.GetEdgesFromNodeAsync(nodeId);
    }

    // Example 7: Get edges to a specific node
    public async Task<List<Edge>> GetEdgesToNodeAsync(string nodeId)
    {
        return await _nodeExplorerService.GetEdgesToNodeAsync(nodeId);
    }

    // Example 8: Search the graph
    public async Task<List<GraphQueryResult>> SearchGraphAsync(string query)
    {
        return await _nodeExplorerService.SearchGraphAsync(query);
    }

    // Example 9: Render node content
    public async Task<string> RenderNodeContentAsync(Node node)
    {
        if (node.Content == null)
            return "No content available";

        var renderResult = await _mediaRendererService.RenderContentAsync(node.Content);
        return renderResult.IsSuccess ? renderResult.RenderedContent : $"Error: {renderResult.ErrorMessage}";
    }

    // Example 10: Get node with its connections
    public async Task<NodeWithConnections?> GetNodeWithConnectionsAsync(string nodeId)
    {
        var node = await _nodeExplorerService.GetNodeAsync(nodeId);
        if (node == null)
            return null;

        var edgesFrom = await _nodeExplorerService.GetEdgesFromNodeAsync(nodeId);
        var edgesTo = await _nodeExplorerService.GetEdgesToNodeAsync(nodeId);

        return new NodeWithConnections
        {
            Node = node,
            EdgesFrom = edgesFrom,
            EdgesTo = edgesTo
        };
    }

    // Example 11: Find related nodes
    public async Task<List<Node>> FindRelatedNodesAsync(string nodeId, int maxDepth = 2)
    {
        var relatedNodes = new List<Node>();
        var visited = new HashSet<string>();
        var queue = new Queue<(string nodeId, int depth)>();
        
        queue.Enqueue((nodeId, 0));
        visited.Add(nodeId);

        while (queue.Count > 0)
        {
            var (currentNodeId, depth) = queue.Dequeue();
            
            if (depth >= maxDepth)
                continue;

            // Get edges from this node
            var edgesFrom = await _nodeExplorerService.GetEdgesFromNodeAsync(currentNodeId);
            foreach (var edge in edgesFrom)
            {
                if (!visited.Contains(edge.ToId))
                {
                    visited.Add(edge.ToId);
                    var relatedNode = await _nodeExplorerService.GetNodeAsync(edge.ToId);
                    if (relatedNode != null)
                    {
                        relatedNodes.Add(relatedNode);
                        queue.Enqueue((edge.ToId, depth + 1));
                    }
                }
            }

            // Get edges to this node
            var edgesTo = await _nodeExplorerService.GetEdgesToNodeAsync(currentNodeId);
            foreach (var edge in edgesTo)
            {
                if (!visited.Contains(edge.FromId))
                {
                    visited.Add(edge.FromId);
                    var relatedNode = await _nodeExplorerService.GetNodeAsync(edge.FromId);
                    if (relatedNode != null)
                    {
                        relatedNodes.Add(relatedNode);
                        queue.Enqueue((edge.FromId, depth + 1));
                    }
                }
            }
        }

        return relatedNodes;
    }

    // Example 12: Get nodes by content state
    public async Task<List<Node>> GetNodesByStateAsync(ContentState state)
    {
        var query = new NodeListQuery { State = state };
        return await _nodeExplorerService.GetNodesAsync(query);
    }

    // Example 13: Get nodes with specific metadata
    public async Task<List<Node>> GetNodesWithMetadataAsync(string key, string value)
    {
        var request = new NodeSearchRequest
        {
            Filters = new Dictionary<string, object> { [key] = value }
        };
        return await _nodeExplorerService.SearchNodesAsync(request);
    }
}

// Helper class for node with connections
public class NodeWithConnections
{
    public Node Node { get; set; } = null!;
    public List<Edge> EdgesFrom { get; set; } = new();
    public List<Edge> EdgesTo { get; set; } = new();
}
