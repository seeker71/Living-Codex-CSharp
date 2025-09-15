using System.Text.Json;

namespace CodexBootstrap.Core.Storage;

/// <summary>
/// High-performance, federated storage backend for Ice nodes (persistent, immutable)
/// Designed for massive scale and eventual federation across multiple data centers
/// </summary>
public interface IIceStorageBackend
{
    /// <summary>
    /// Initialize the Ice storage backend
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Store an Ice node persistently
    /// </summary>
    Task StoreIceNodeAsync(Node node);

    /// <summary>
    /// Retrieve an Ice node by ID
    /// </summary>
    Task<Node?> GetIceNodeAsync(string id);

    /// <summary>
    /// Get all Ice nodes
    /// </summary>
    Task<IEnumerable<Node>> GetAllIceNodesAsync();

    /// <summary>
    /// Get Ice nodes by type
    /// </summary>
    Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId);

    /// <summary>
    /// Store an edge (edges are always Ice state)
    /// </summary>
    Task StoreEdgeAsync(Edge edge);

    /// <summary>
    /// Get all edges
    /// </summary>
    Task<IEnumerable<Edge>> GetAllEdgesAsync();

    /// <summary>
    /// Get edges from a specific node
    /// </summary>
    Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId);

    /// <summary>
    /// Get edges to a specific node
    /// </summary>
    Task<IEnumerable<Edge>> GetEdgesToAsync(string toId);

    /// <summary>
    /// Delete an Ice node (rare operation)
    /// </summary>
    Task DeleteIceNodeAsync(string id);

    /// <summary>
    /// Delete an edge
    /// </summary>
    Task DeleteEdgeAsync(string fromId, string toId, string role);

    /// <summary>
    /// Check if Ice storage is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get Ice storage statistics
    /// </summary>
    Task<IceStorageStats> GetStatsAsync();

    /// <summary>
    /// Batch operations for high performance
    /// </summary>
    Task BatchStoreIceNodesAsync(IEnumerable<Node> nodes);
    Task BatchStoreEdgesAsync(IEnumerable<Edge> edges);

    /// <summary>
    /// Search operations for complex queries
    /// </summary>
    Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100);
    Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100);
}

/// <summary>
/// Ice storage statistics
/// </summary>
public record IceStorageStats(
    int IceNodeCount,
    int EdgeCount,
    long TotalSizeBytes,
    DateTime LastUpdated,
    string BackendType,
    Dictionary<string, object> BackendStats
);
