using System.Collections.Concurrent;

namespace CodexBootstrap.Core;

/// <summary>
/// Distributed cache manager that coordinates caching across multiple nodes
/// </summary>
public class DistributedCacheManager : ICacheManager
{
    private readonly IDistributedStorageBackend _distributedStorage;
    private readonly ICacheManager _localCacheManager;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, Node> _distributedCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public DistributedCacheManager(IDistributedStorageBackend distributedStorage, ICacheManager localCacheManager)
    {
        _distributedStorage = distributedStorage;
        _localCacheManager = localCacheManager;
        _logger = new Log4NetLogger(typeof(DistributedCacheManager));
    }

    public async Task InitializeAsync()
    {
        await _localCacheManager.InitializeAsync();
        
        // Load distributed nodes into local cache
        var distributedNodes = await _distributedStorage.GetClusterNodesAsync();
        foreach (var node in distributedNodes)
        {
            _distributedCache[node.Id] = node;
        }
        
        _logger.Info($"Distributed cache initialized with {distributedNodes.Count()} distributed nodes");
    }

    public async Task StoreNodeAsync(Node node)
    {
        // Store in local cache first
        await _localCacheManager.StoreNodeAsync(node);
        
        // Store in distributed storage
        await _distributedStorage.StoreNodeAsync(node);
        
        // Update distributed cache
        _distributedCache[node.Id] = node;
        
        _logger.Debug($"Stored node {node.Id} in distributed cache");
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        // First check local cache
        var localNode = await _localCacheManager.GetNodeAsync(id);
        if (localNode != null)
        {
            return localNode;
        }
        
        // Check distributed cache
        if (_distributedCache.TryGetValue(id, out var cachedNode))
        {
            return cachedNode;
        }
        
        // Try to get from distributed storage
        try
        {
            var distributedNodes = await _distributedStorage.GetClusterNodesAsync();
            var node = distributedNodes.FirstOrDefault(n => n.Id == id);
            if (node != null)
            {
                _distributedCache[id] = node;
                return node;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get node {id} from distributed storage: {ex.Message}", ex);
        }
        
        return null;
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        // Store in local cache
        await _localCacheManager.StoreEdgeAsync(edge);
        
        // Store in distributed storage
        await _distributedStorage.StoreEdgeAsync(edge);
        
        _logger.Debug($"Stored edge {edge.FromId}->{edge.ToId} in distributed storage");
    }

    public async Task<Edge?> GetEdgeAsync(string fromId, string toId, string role)
    {
        // First check local cache
        var localEdge = await _localCacheManager.GetEdgeAsync(fromId, toId, role);
        if (localEdge != null)
        {
            return localEdge;
        }
        
        // Try to get from distributed storage
        try
        {
            var distributedEdges = await _distributedStorage.GetClusterEdgesAsync();
            var edge = distributedEdges.FirstOrDefault(e => 
                e.FromId == fromId && e.ToId == toId && e.Role == role);
            return edge;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get edge from distributed storage: {ex.Message}", ex);
            return null;
        }
    }

    public async Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state)
    {
        // Get local nodes
        var localNodes = await _localCacheManager.GetNodesByStateAsync(state);
        
        // Get distributed nodes
        var distributedNodes = _distributedCache.Values.Where(n => n.State == state);
        
        // Merge and deduplicate
        var allNodes = localNodes.Concat(distributedNodes)
            .GroupBy(n => n.Id)
            .Select(g => g.First())
            .ToList();
            
        return allNodes;
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        // Get local edges
        var localEdges = await _localCacheManager.GetAllEdgesAsync();
        
        // Get distributed edges
        var distributedEdges = await _distributedStorage.GetClusterEdgesAsync();
        
        // Merge and deduplicate
        var allEdges = localEdges.Concat(distributedEdges)
            .GroupBy(e => $"{e.FromId}-{e.ToId}-{e.Role}")
            .Select(g => g.First())
            .ToList();
            
        return allEdges;
    }

    public async Task ClearCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            // Clear local cache
            await _localCacheManager.ClearCacheAsync();
            
            // Clear distributed cache
            _distributedCache.Clear();
            
            _logger.Info("Distributed cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<CacheStats> GetCacheStatsAsync()
    {
        // Get local cache stats
        var localStats = await _localCacheManager.GetCacheStatsAsync();
        
        // Calculate distributed cache stats
        var distributedIceNodes = _distributedCache.Values.Count(n => n.State == ContentState.Ice);
        var distributedWaterNodes = _distributedCache.Values.Count(n => n.State == ContentState.Water);
        var distributedGasNodes = _distributedCache.Values.Count(n => n.State == ContentState.Gas);
        
        return new CacheStats(
            IceNodeCount: localStats.IceNodeCount + distributedIceNodes,
            WaterNodeCount: localStats.WaterNodeCount + distributedWaterNodes,
            GasNodeCount: localStats.GasNodeCount + distributedGasNodes,
            EdgeCount: localStats.EdgeCount,
            TotalMemoryUsage: localStats.TotalMemoryUsage + GC.GetTotalMemory(false),
            LastUpdated: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Sync with distributed storage
    /// </summary>
    public async Task SyncWithDistributedStorageAsync()
    {
        try
        {
            // Get latest data from distributed storage
            var distributedNodes = await _distributedStorage.GetClusterNodesAsync();
            var distributedEdges = await _distributedStorage.GetClusterEdgesAsync();
            
            // Update distributed cache
            _distributedCache.Clear();
            foreach (var node in distributedNodes)
            {
                _distributedCache[node.Id] = node;
            }
            
            _logger.Info($"Synced with distributed storage: {distributedNodes.Count()} nodes, {distributedEdges.Count()} edges");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to sync with distributed storage: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get cluster health information
    /// </summary>
    public async Task<ClusterHealth> GetClusterHealthAsync()
    {
        return await _distributedStorage.GetClusterHealthAsync();
    }

    /// <summary>
    /// Repair cluster data
    /// </summary>
    public async Task<RepairResult> RepairClusterAsync()
    {
        return await _distributedStorage.RepairClusterAsync();
    }
}
