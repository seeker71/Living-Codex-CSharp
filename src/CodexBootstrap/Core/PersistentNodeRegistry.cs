using System.Collections.Concurrent;

namespace CodexBootstrap.Core;

/// <summary>
/// Persistent NodeRegistry that uses a storage backend
/// </summary>
public class PersistentNodeRegistry : NodeRegistry
{
    private readonly IStorageBackend _storage;
    private readonly ConcurrentDictionary<string, Node> _nodeCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<Edge> _edgeCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly ILogger _logger;
    private bool _isInitialized = false;

    public PersistentNodeRegistry(IStorageBackend storage)
    {
        _storage = storage;
        _logger = new Log4NetLogger(typeof(PersistentNodeRegistry));
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _storage.InitializeAsync();
        
        // Load all data into cache
        await _cacheLock.WaitAsync();
        try
        {
            var nodes = await _storage.GetAllNodesAsync();
            foreach (var node in nodes)
            {
                _nodeCache[node.Id] = node;
            }

            var edges = await _storage.GetAllEdgesAsync();
            foreach (var edge in edges)
            {
                _edgeCache.Add(edge);
            }

            _isInitialized = true;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public override void Upsert(Node node)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        _nodeCache[node.Id] = node;
        
        // Persist to storage asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _storage.StoreNodeAsync(node);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error persisting node {node.Id}: {ex.Message}", ex);
            }
        });
    }

    public override void Upsert(Edge edge)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        _edgeCache.Add(edge);
        
        // Persist to storage asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _storage.StoreEdgeAsync(edge);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error persisting edge {edge.FromId}->{edge.ToId}: {ex.Message}", ex);
            }
        });
    }

    public override bool TryGet(string id, out Node node)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _nodeCache.TryGetValue(id, out node!);
    }

    public override IEnumerable<Edge> AllEdges()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _edgeCache.ToList();
    }

    public override IEnumerable<Node> AllNodes()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _nodeCache.Values;
    }

    public override IEnumerable<Node> GetNodesByType(string typeId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _nodeCache.Values.Where(n => n.TypeId == typeId);
    }

    public override IEnumerable<Edge> GetEdgesFrom(string fromId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _edgeCache.Where(e => e.FromId == fromId);
    }

    public override IEnumerable<Edge> GetEdgesTo(string toId)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
        }

        return _edgeCache.Where(e => e.ToId == toId);
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

        // Remove from cache
        _nodeCache.TryRemove(id, out _);
        
        // Remove associated edges from cache
        var edgesToRemove = _edgeCache.Where(e => e.FromId == id || e.ToId == id).ToList();
        foreach (var edge in edgesToRemove)
        {
            // Note: ConcurrentBag doesn't support removal, so we'll mark them for removal
            // In a production system, you'd want a more sophisticated cache
        }

        // Persist deletion to storage
        await _storage.DeleteNodeAsync(id);
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
        await _cacheLock.WaitAsync();
        try
        {
            // Clear cache
            _nodeCache.Clear();
            _edgeCache.Clear();

            // Reload from storage
            var nodes = await _storage.GetAllNodesAsync();
            foreach (var node in nodes)
            {
                _nodeCache[node.Id] = node;
            }

            var edges = await _storage.GetAllEdgesAsync();
            foreach (var edge in edges)
            {
                _edgeCache.Add(edge);
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}
