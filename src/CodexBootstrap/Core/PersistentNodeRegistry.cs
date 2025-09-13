using System.Collections.Concurrent;

namespace CodexBootstrap.Core;

/// <summary>
/// Persistent NodeRegistry that uses a storage backend and cache manager
/// </summary>
public class PersistentNodeRegistry : NodeRegistry
{
    private readonly IStorageBackend _storage;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger _logger;
    private bool _isInitialized = false;

    public PersistentNodeRegistry(IStorageBackend storage)
    {
        _storage = storage;
        _cacheManager = new NodeCacheManager(storage);
        _logger = new Log4NetLogger(typeof(PersistentNodeRegistry));
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _cacheManager.InitializeAsync();
        _isInitialized = true;
        _logger.Info("PersistentNodeRegistry initialized with cache manager");
    }

    public override void Upsert(Node node)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Use cache manager to handle different node states appropriately
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheManager.StoreNodeAsync(node);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error storing node {node.Id}: {ex.Message}", ex);
            }
        });
    }

    public override void Upsert(Edge edge)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Use cache manager to store edges
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheManager.StoreEdgeAsync(edge);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error storing edge {edge.FromId}->{edge.ToId}: {ex.Message}", ex);
            }
        });
    }

    public override bool TryGet(string id, out Node node)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Use cache manager to get node (handles Water/Gas generation)
        var nodeTask = _cacheManager.GetNodeAsync(id);
        nodeTask.Wait();
        node = nodeTask.Result!;
        return node != null;
    }

    public override IEnumerable<Edge> AllEdges()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        var edgesTask = _cacheManager.GetAllEdgesAsync();
        edgesTask.Wait();
        return edgesTask.Result.ToList();
    }

    public override IEnumerable<Node> AllNodes()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Get all nodes from all states
        var iceNodesTask = _cacheManager.GetNodesByStateAsync(ContentState.Ice);
        var waterNodesTask = _cacheManager.GetNodesByStateAsync(ContentState.Water);
        
        iceNodesTask.Wait();
        waterNodesTask.Wait();
        
        return iceNodesTask.Result.Concat(waterNodesTask.Result).ToList();
    }

    public override IEnumerable<Node> GetNodesByType(string typeId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        var allNodes = AllNodes();
        return allNodes.Where(n => n.TypeId == typeId).ToArray();
    }

    public override IEnumerable<Edge> GetEdgesFrom(string fromId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        var edgesTask = _cacheManager.GetAllEdgesAsync();
        edgesTask.Wait();
        return edgesTask.Result.Where(e => e.FromId == fromId).ToList();
    }

    public override IEnumerable<Edge> GetEdgesTo(string toId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        var edgesTask = _cacheManager.GetAllEdgesAsync();
        edgesTask.Wait();
        return edgesTask.Result.Where(e => e.ToId == toId).ToList();
    }

    public override IEnumerable<Edge> GetEdges(string? fromId = null, string? toId = null, string? edgeType = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        var edgesTask = _cacheManager.GetAllEdgesAsync();
        edgesTask.Wait();
        return edgesTask.Result.Where(e => 
            (fromId == null || e.FromId == fromId) &&
            (toId == null || e.ToId == toId) &&
            (edgeType == null || e.Role == edgeType)).ToList();
    }

    /// <summary>
    /// Delete a node and its associated edges
    /// </summary>
    public async Task DeleteNodeAsync(string id)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Clear cache for this node
        await _cacheManager.ClearCacheAsync();
        
        // Persist deletion to storage
        await _storage.DeleteNodeAsync(id);
        
        _logger.Info($"Deleted node {id} and cleared cache");
    }

    /// <summary>
    /// Delete an edge
    /// </summary>
    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Note: ConcurrentBag doesn't support removal, so we'll mark for removal
        // In a production system, you'd want a more sophisticated cache

        // Persist deletion to storage
        await _storage.DeleteEdgeAsync(fromId, toId, role);
    }

    /// <summary>
    /// Get storage statistics
    /// </summary>
    public async Task<StorageStats> GetStorageStatsAsync()
    {
        return await _storage.GetStatsAsync();
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<CacheStats> GetCacheStatsAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return await _cacheManager.GetCacheStatsAsync();
    }

    /// <summary>
    /// Check if storage is available
    /// </summary>
    public async Task<bool> IsStorageAvailableAsync()
    {
        return await _storage.IsAvailableAsync();
    }

    /// <summary>
    /// Force sync cache with storage
    /// </summary>
    public async Task SyncWithStorageAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        // Clear cache and reinitialize
        await _cacheManager.ClearCacheAsync();
        await _cacheManager.InitializeAsync();
        
        _logger.Info("Cache synced with storage");
    }
}
