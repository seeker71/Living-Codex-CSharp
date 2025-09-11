using System.Text.Json;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for persistent storage backends
/// </summary>
public interface IStorageBackend
{
    /// <summary>
    /// Initialize the storage backend
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Store a node
    /// </summary>
    Task StoreNodeAsync(Node node);

    /// <summary>
    /// Retrieve a node by ID
    /// </summary>
    Task<Node?> GetNodeAsync(string id);

    /// <summary>
    /// Get all nodes
    /// </summary>
    Task<IEnumerable<Node>> GetAllNodesAsync();

    /// <summary>
    /// Get nodes by type
    /// </summary>
    Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId);

    /// <summary>
    /// Store an edge
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
    /// Delete a node
    /// </summary>
    Task DeleteNodeAsync(string id);

    /// <summary>
    /// Delete an edge
    /// </summary>
    Task DeleteEdgeAsync(string fromId, string toId, string role);

    /// <summary>
    /// Check if storage is available
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get storage statistics
    /// </summary>
    Task<StorageStats> GetStatsAsync();
}

/// <summary>
/// Storage statistics
/// </summary>
public record StorageStats(
    int NodeCount,
    int EdgeCount,
    long TotalSizeBytes,
    DateTime LastUpdated
);

/// <summary>
/// Storage configuration
/// </summary>
public record StorageConfig(
    string BackendType,
    string ConnectionString,
    Dictionary<string, string> Options
);
