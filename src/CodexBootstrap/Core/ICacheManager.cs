using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for managing node cache with different strategies for different content states
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Initialize the cache manager
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// Store a node in the cache with appropriate strategy based on its state
    /// </summary>
    Task StoreNodeAsync(Node node);
    
    /// <summary>
    /// Retrieve a node from cache, regenerating if necessary
    /// </summary>
    Task<Node?> GetNodeAsync(string id);
    
    /// <summary>
    /// Store an edge in the cache with appropriate strategy based on its state
    /// </summary>
    Task StoreEdgeAsync(Edge edge);
    
    /// <summary>
    /// Retrieve an edge from cache, regenerating if necessary
    /// </summary>
    Task<Edge?> GetEdgeAsync(string fromId, string toId, string role);
    
    /// <summary>
    /// Get all nodes of a specific state
    /// </summary>
    Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state);
    
    /// <summary>
    /// Get all edges
    /// </summary>
    Task<IEnumerable<Edge>> GetAllEdgesAsync();
    
    /// <summary>
    /// Clear the cache
    /// </summary>
    Task ClearCacheAsync();
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStats> GetCacheStatsAsync();
}

/// <summary>
/// Cache statistics
/// </summary>
public record CacheStats(
    int IceNodeCount,
    int WaterNodeCount,
    int GasNodeCount,
    int EdgeCount,
    long TotalMemoryUsage,
    DateTime LastUpdated
);
