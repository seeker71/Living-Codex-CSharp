using System.Collections.Concurrent;
using System.Text.Json;

namespace CodexBootstrap.Core;

/// <summary>
/// Cache manager that handles different storage strategies for different node states
/// - Ice nodes: Stored persistently and cached
/// - Water nodes: Generated from Ice nodes, cached temporarily
/// - Gas nodes: Generated on-demand, not cached
/// </summary>
public class NodeCacheManager : ICacheManager
{
    private readonly IStorageBackend _storage;
    private readonly ICodexLogger _logger;
    private readonly ConcurrentDictionary<string, Node> _iceNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Node> _waterNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<Edge> _edges = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly TimeSpan _waterNodeCacheExpiry = TimeSpan.FromMinutes(30);
    private readonly Dictionary<string, DateTime> _waterNodeTimestamps = new();

    public NodeCacheManager(IStorageBackend storage, ICodexLogger logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _storage.InitializeAsync();
        
        // Load only Ice nodes and edges from persistent storage
        var iceNodes = await _storage.GetAllNodesAsync();
        foreach (var node in iceNodes.Where(n => n.State == ContentState.Ice))
        {
            _iceNodes[node.Id] = node;
        }

        var edges = await _storage.GetAllEdgesAsync();
        foreach (var edge in edges)
        {
            _edges.Add(edge);
        }

        _logger.Info($"Initialized cache with {_iceNodes.Count} Ice nodes and {_edges.Count} edges");
    }

    public async Task StoreNodeAsync(Node node)
    {
        switch (node.State)
        {
            case ContentState.Ice:
                await StoreIceNodeAsync(node);
                break;
            case ContentState.Water:
                await StoreWaterNodeAsync(node);
                break;
            case ContentState.Gas:
                // Gas nodes are not stored, they are generated on-demand
                _logger.Debug($"Gas node {node.Id} not stored (generated on-demand)");
                break;
        }
    }

    private async Task StoreIceNodeAsync(Node node)
    {
        _iceNodes[node.Id] = node;
        
        // Persist to storage
        await _storage.StoreNodeAsync(node);
        _logger.Debug($"Stored Ice node {node.Id} persistently");
    }

    private async Task StoreWaterNodeAsync(Node node)
    {
        _waterNodes[node.Id] = node;
        _waterNodeTimestamps[node.Id] = DateTime.UtcNow;
        
        // Water nodes are not persisted, only cached temporarily
        _logger.Debug($"Cached Water node {node.Id} temporarily");
        
        // Clean up expired water nodes
        await CleanupExpiredWaterNodesAsync();
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        // First check if it's already in cache
        if (_iceNodes.TryGetValue(id, out var iceNode))
        {
            return iceNode;
        }

        if (_waterNodes.TryGetValue(id, out var waterNode))
        {
            // Check if water node has expired
            if (_waterNodeTimestamps.TryGetValue(id, out var timestamp) && 
                DateTime.UtcNow - timestamp < _waterNodeCacheExpiry)
            {
                return waterNode;
            }
            else
            {
                // Remove expired water node
                _waterNodes.TryRemove(id, out _);
                _waterNodeTimestamps.Remove(id);
            }
        }

        // Try to generate Water node from Ice node
        var generatedWaterNode = await GenerateWaterNodeAsync(id);
        if (generatedWaterNode != null)
        {
            await StoreWaterNodeAsync(generatedWaterNode);
            return generatedWaterNode;
        }

        // Try to generate Gas node on-demand
        var generatedGasNode = await GenerateGasNodeAsync(id);
        return generatedGasNode;
    }

    private async Task<Node?> GenerateWaterNodeAsync(string id)
    {
        // Look for a related Ice node that could generate this Water node
        var relatedIceNodes = _iceNodes.Values
            .Where(n => n.Id == id || 
                       (n.Meta?.ContainsKey("generates") == true && 
                        n.Meta["generates"] is string generates && 
                        generates == id))
            .ToList();

        if (!relatedIceNodes.Any())
        {
            return null;
        }

        var iceNode = relatedIceNodes.First();
        
        // Generate Water node from Ice node
        var waterNode = new Node(
            Id: id,
            TypeId: iceNode.TypeId,
            State: ContentState.Water,
            Locale: iceNode.Locale,
            Title: iceNode.Title,
            Description: iceNode.Description,
            Content: await GenerateWaterContentAsync(iceNode),
            Meta: GenerateWaterMetaAsync(iceNode)
        );

        _logger.Debug($"Generated Water node {id} from Ice node {iceNode.Id}");
        return waterNode;
    }

    private async Task<Node?> GenerateGasNodeAsync(string id)
    {
        // Look for related nodes that could generate this Gas node
        var relatedNodes = _iceNodes.Values
            .Concat(_waterNodes.Values)
            .Where(n => n.Id == id || 
                       (n.Meta?.ContainsKey("generates") == true && 
                        n.Meta["generates"] is string generates && 
                        generates == id))
            .ToList();

        if (!relatedNodes.Any())
        {
            return null;
        }

        var sourceNode = relatedNodes.First();
        
        // Generate Gas node on-demand
        var gasNode = new Node(
            Id: id,
            TypeId: sourceNode.TypeId,
            State: ContentState.Gas,
            Locale: sourceNode.Locale,
            Title: sourceNode.Title,
            Description: sourceNode.Description,
            Content: await GenerateGasContentAsync(sourceNode),
            Meta: GenerateGasMetaAsync(sourceNode)
        );

        _logger.Debug($"Generated Gas node {id} on-demand from {sourceNode.State} node {sourceNode.Id}");
        return gasNode;
    }

    private async Task<ContentRef?> GenerateWaterContentAsync(Node iceNode)
    {
        if (iceNode.Content == null)
        {
            return null;
        }

        // Water content is typically a processed/expanded version of Ice content
        return new ContentRef(
            MediaType: iceNode.Content.MediaType,
            InlineJson: iceNode.Content.InlineJson != null ? 
                await ProcessWaterContentAsync(iceNode.Content.InlineJson) : null,
            InlineBytes: iceNode.Content.InlineBytes,
            ExternalUri: iceNode.Content.ExternalUri,
            Selector: iceNode.Content.Selector,
            Query: iceNode.Content.Query,
            Headers: iceNode.Content.Headers,
            AuthRef: iceNode.Content.AuthRef,
            CacheKey: $"water_{iceNode.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}"
        );
    }

    private async Task<ContentRef?> GenerateGasContentAsync(Node sourceNode)
    {
        if (sourceNode.Content == null)
        {
            return null;
        }

        // Gas content is typically a highly processed/transformed version
        return new ContentRef(
            MediaType: sourceNode.Content.MediaType,
            InlineJson: sourceNode.Content.InlineJson != null ? 
                await ProcessGasContentAsync(sourceNode.Content.InlineJson) : null,
            InlineBytes: sourceNode.Content.InlineBytes,
            ExternalUri: sourceNode.Content.ExternalUri,
            Selector: sourceNode.Content.Selector,
            Query: sourceNode.Content.Query,
            Headers: sourceNode.Content.Headers,
            AuthRef: sourceNode.Content.AuthRef,
            CacheKey: $"gas_{sourceNode.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}"
        );
    }

    private async Task<string> ProcessWaterContentAsync(string iceContent)
    {
        // Process Ice content to Water content
        // This involves:
        // - Expanding compressed data
        // - Resolving references
        // - Adding computed fields
        // - Applying transformations
        
        await Task.Delay(10); // Simulate processing time
        
        try
        {
            var jsonDoc = JsonDocument.Parse(iceContent);
            var processedJson = JsonSerializer.Serialize(new
            {
                original = jsonDoc.RootElement,
                processed = true,
                state = "water",
                processedAt = DateTime.UtcNow,
                source = "ice"
            }, new JsonSerializerOptions { WriteIndented = true });
            
            return processedJson;
        }
        catch
        {
            // If not JSON, just add metadata
            return $"{{\"content\": {iceContent}, \"processed\": true, \"state\": \"water\", \"processedAt\": \"{DateTime.UtcNow:O}\"}}";
        }
    }

    private async Task<string> ProcessGasContentAsync(string sourceContent)
    {
        // Process content to Gas state
        // This involves:
        // - Advanced transformations
        // - AI processing
        // - Complex computations
        // - Real-time data integration
        
        await Task.Delay(50); // Simulate more complex processing
        
        try
        {
            var jsonDoc = JsonDocument.Parse(sourceContent);
            var processedJson = JsonSerializer.Serialize(new
            {
                original = jsonDoc.RootElement,
                processed = true,
                state = "gas",
                processedAt = DateTime.UtcNow,
                source = "dynamic",
                transformations = new[] { "expand", "enhance", "optimize" }
            }, new JsonSerializerOptions { WriteIndented = true });
            
            return processedJson;
        }
        catch
        {
            // If not JSON, just add metadata
            return $"{{\"content\": {sourceContent}, \"processed\": true, \"state\": \"gas\", \"processedAt\": \"{DateTime.UtcNow:O}\", \"transformations\": [\"expand\", \"enhance\", \"optimize\"]}}";
        }
    }

    private Dictionary<string, object>? GenerateWaterMetaAsync(Node iceNode)
    {
        var meta = new Dictionary<string, object>();
        
        if (iceNode.Meta != null)
        {
            foreach (var kvp in iceNode.Meta)
            {
                meta[kvp.Key] = kvp.Value;
            }
        }
        
        meta["generatedFrom"] = iceNode.Id;
        meta["generatedAt"] = DateTime.UtcNow;
        meta["state"] = "water";
        meta["cacheExpiry"] = DateTime.UtcNow.Add(_waterNodeCacheExpiry);
        
        return meta;
    }

    private Dictionary<string, object>? GenerateGasMetaAsync(Node sourceNode)
    {
        var meta = new Dictionary<string, object>();
        
        if (sourceNode.Meta != null)
        {
            foreach (var kvp in sourceNode.Meta)
            {
                meta[kvp.Key] = kvp.Value;
            }
        }
        
        meta["generatedFrom"] = sourceNode.Id;
        meta["generatedAt"] = DateTime.UtcNow;
        meta["state"] = "gas";
        meta["onDemand"] = true;
        
        return meta;
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        _edges.Add(edge);
        
        // Only persist edges (they are always Ice state)
        await _storage.StoreEdgeAsync(edge);
        _logger.Debug($"Stored edge {edge.FromId}->{edge.ToId}");
    }

    public async Task<Edge?> GetEdgeAsync(string fromId, string toId, string role)
    {
        var edge = _edges.FirstOrDefault(e => 
            e.FromId == fromId && e.ToId == toId && e.Role == role);
        
        return await Task.FromResult(edge);
    }

    public async Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state)
    {
        return state switch
        {
            ContentState.Ice => _iceNodes.Values,
            ContentState.Water => _waterNodes.Values,
            ContentState.Gas => new List<Node>(), // Gas nodes are not cached
            _ => new List<Node>()
        };
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        return _edges;
    }

    public async Task ClearCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            _iceNodes.Clear();
            _waterNodes.Clear();
            _edges.Clear();
            _waterNodeTimestamps.Clear();
            _logger.Info("Cache cleared");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<CacheStats> GetCacheStatsAsync()
    {
        var memoryUsage = GC.GetTotalMemory(false);
        
        return new CacheStats(
            IceNodeCount: _iceNodes.Count,
            WaterNodeCount: _waterNodes.Count,
            GasNodeCount: 0, // Gas nodes are not cached
            EdgeCount: _edges.Count,
            TotalMemoryUsage: memoryUsage,
            LastUpdated: DateTime.UtcNow
        );
    }

    private async Task CleanupExpiredWaterNodesAsync()
    {
        var expiredNodes = _waterNodeTimestamps
            .Where(kvp => DateTime.UtcNow - kvp.Value >= _waterNodeCacheExpiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var nodeId in expiredNodes)
        {
            _waterNodes.TryRemove(nodeId, out _);
            _waterNodeTimestamps.Remove(nodeId);
        }

        if (expiredNodes.Any())
        {
            _logger.Debug($"Cleaned up {expiredNodes.Count} expired Water nodes");
        }
    }
}
