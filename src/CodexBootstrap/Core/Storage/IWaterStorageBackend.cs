using System.Text.Json;

namespace CodexBootstrap.Core.Storage;

/// <summary>
/// Semi-persistent storage backend for Water nodes (cache, generated code, derived data)
/// Uses SQLite for simplicity and local performance
/// </summary>
public interface IWaterStorageBackend
{
    /// <summary>
    /// Initialize the Water storage backend
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Store a Water node temporarily
    /// </summary>
    Task StoreWaterNodeAsync(Node node, TimeSpan? expiry = null);

    /// <summary>
    /// Retrieve a Water node by ID
    /// </summary>
    Task<Node?> GetWaterNodeAsync(string id);

    /// <summary>
    /// Get all Water nodes
    /// </summary>
    Task<IEnumerable<Node>> GetAllWaterNodesAsync();

    /// <summary>
    /// Get Water nodes by type
    /// </summary>
    Task<IEnumerable<Node>> GetWaterNodesByTypeAsync(string typeId);

    /// <summary>
    /// Delete a Water node
    /// </summary>
    Task DeleteWaterNodeAsync(string id);

    /// <summary>
    /// Clean up expired Water nodes
    /// </summary>
    Task CleanupExpiredNodesAsync();

    /// <summary>
    /// Check if Water storage is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get Water storage statistics
    /// </summary>
    Task<WaterStorageStats> GetStatsAsync();

    /// <summary>
    /// Batch operations for performance
    /// </summary>
    Task BatchStoreWaterNodesAsync(IEnumerable<Node> nodes, TimeSpan? expiry = null);

    /// <summary>
    /// Search operations
    /// </summary>
    Task<IEnumerable<Node>> SearchWaterNodesAsync(string query, int limit = 100);

    /// <summary>
    /// Store an edge in Water storage (for fluid state edges)
    /// </summary>
    Task StoreWaterEdgeAsync(Edge edge, TimeSpan? expiry = null);

    /// <summary>
    /// Get all Water edges
    /// </summary>
    Task<IEnumerable<Edge>> GetAllWaterEdgesAsync();

    /// <summary>
    /// Get Water edges from a specific node
    /// </summary>
    Task<IEnumerable<Edge>> GetWaterEdgesFromAsync(string fromId);

    /// <summary>
    /// Get Water edges to a specific node
    /// </summary>
    Task<IEnumerable<Edge>> GetWaterEdgesToAsync(string toId);

    /// <summary>
    /// Delete a Water edge
    /// </summary>
    Task DeleteWaterEdgeAsync(string fromId, string toId, string role);
}

/// <summary>
/// Water storage statistics
/// </summary>
public record WaterStorageStats(
    int WaterNodeCount,
    int ExpiredNodeCount,
    long TotalSizeBytes,
    DateTime LastUpdated,
    TimeSpan AverageExpiry,
    Dictionary<string, object> BackendStats
);
