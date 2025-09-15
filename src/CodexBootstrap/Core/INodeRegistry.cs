using CodexBootstrap.Core.Storage;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for the unified NodeRegistry that manages all nodes and edges
/// with integrated Ice (persistent) and Water (semi-persistent) storage
/// </summary>
public interface INodeRegistry
{
    /// <summary>
    /// Initialize the registry with storage backends
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Store or update a node
    /// </summary>
    void Upsert(Node node);

    /// <summary>
    /// Store or update an edge
    /// </summary>
    void Upsert(Edge edge);

    /// <summary>
    /// Try to get a node by ID (synchronous)
    /// </summary>
    bool TryGet(string id, out Node node);

    /// <summary>
    /// Get a node by ID (asynchronous)
    /// </summary>
    Task<Node?> GetNodeAsync(string id);

    /// <summary>
    /// Get all nodes (synchronous - returns only in-memory nodes)
    /// </summary>
    IEnumerable<Node> AllNodes();

    /// <summary>
    /// Get all nodes (asynchronous - returns all nodes from all storage)
    /// </summary>
    Task<IEnumerable<Node>> AllNodesAsync();

    /// <summary>
    /// Get nodes by type (synchronous - returns only in-memory nodes)
    /// </summary>
    IEnumerable<Node> GetNodesByType(string typeId);

    /// <summary>
    /// Get nodes by type (asynchronous - returns all nodes from all storage)
    /// </summary>
    Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId);

    /// <summary>
    /// Get nodes by state
    /// </summary>
    IEnumerable<Node> GetNodesByState(ContentState state);

    /// <summary>
    /// Get nodes by state (asynchronous)
    /// </summary>
    Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state);

    /// <summary>
    /// Get all edges (synchronous - returns only in-memory edges)
    /// </summary>
    IEnumerable<Edge> AllEdges();

    /// <summary>
    /// Get all edges (asynchronous - returns all edges from storage)
    /// </summary>
    Task<IEnumerable<Edge>> AllEdgesAsync();

    /// <summary>
    /// Get a node by ID (synchronous)
    /// </summary>
    Node? GetNode(string id);

    /// <summary>
    /// Remove a node from all storage
    /// </summary>
    void RemoveNode(string nodeId);

    /// <summary>
    /// Get unified storage statistics
    /// </summary>
    Task<UnifiedStorageStats> GetStatsAsync();

    /// <summary>
    /// Clean up expired Water nodes
    /// </summary>
    Task CleanupExpiredWaterNodesAsync();
}
