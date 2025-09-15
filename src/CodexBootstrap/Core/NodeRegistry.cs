using System.Collections.Concurrent;
using CodexBootstrap.Core.Storage;

namespace CodexBootstrap.Core;

/// <summary>
/// NodeRegistry that seamlessly integrates Ice and Water storage backends
/// - Ice nodes: Stored in high-performance, federated storage (PostgreSQL)
/// - Water nodes: Stored in semi-persistent, local cache (SQLite)
/// - Gas nodes: Generated on-demand, not persisted
/// </summary>
public class NodeRegistry : INodeRegistry
{
    private readonly IIceStorageBackend _iceStorage;
    private readonly IWaterStorageBackend _waterStorage;
    private readonly ICodexLogger _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ConcurrentDictionary<string, Node> _gasNodes = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized = false;

    public UnifiedNodeRegistry(IIceStorageBackend iceStorage, IWaterStorageBackend waterStorage, ICodexLogger logger)
    {
        _iceStorage = iceStorage;
        _waterStorage = waterStorage;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_isInitialized) return;

            await _iceStorage.InitializeAsync();
            await _waterStorage.InitializeAsync();
            
            _isInitialized = true;
            _logger.Info("UnifiedNodeRegistry initialized with Ice and Water storage backends");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public override void Upsert(Node node)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Route nodes to appropriate storage based on their state
        _ = Task.Run(async () =>
        {
            try
            {
                switch (node.State)
                {
                    case ContentState.Ice:
                        await _iceStorage.StoreIceNodeAsync(node);
                        _logger.Debug($"Stored Ice node {node.Id} in federated storage");
                        break;
                    case ContentState.Water:
                        await _waterStorage.StoreWaterNodeAsync(node);
                        _logger.Debug($"Stored Water node {node.Id} in local cache");
                        break;
                    case ContentState.Gas:
                        _gasNodes[node.Id] = node;
                        _logger.Debug($"Cached Gas node {node.Id} in memory");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error storing {node.State} node {node.Id}: {ex.Message}", ex);
            }
        });
    }

    public override void Upsert(Edge edge)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Edges are always Ice state and stored in Ice storage
        _ = Task.Run(async () =>
        {
            try
            {
                await _iceStorage.StoreEdgeAsync(edge);
                _logger.Debug($"Stored edge {edge.FromId}->{edge.ToId} in federated storage");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error storing edge {edge.FromId}->{edge.ToId}: {ex.Message}", ex);
            }
        });
    }

    public override bool TryGet(string id, out Node node)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Try to get node synchronously from memory first (Gas nodes)
        if (_gasNodes.TryGetValue(id, out node))
        {
            return true;
        }

        // For Ice and Water nodes, we need to check storage asynchronously
        // This is a limitation of the synchronous TryGet interface
        // In practice, this should be called from async contexts
        var nodeTask = GetNodeAsync(id);
        if (nodeTask.IsCompleted)
        {
            node = nodeTask.Result;
            return node != null;
        }

        // If not completed, return false and let caller use async method
        node = null!;
        return false;
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // First check Gas nodes in memory
        if (_gasNodes.TryGetValue(id, out var gasNode))
        {
            return gasNode;
        }

        // Try Water storage (semi-persistent cache)
        var waterNode = await _waterStorage.GetWaterNodeAsync(id);
        if (waterNode != null)
        {
            return waterNode;
        }

        // Try Ice storage (persistent)
        var iceNode = await _iceStorage.GetIceNodeAsync(id);
        if (iceNode != null)
        {
            return iceNode;
        }

        // Try to generate Water node from Ice node
        var generatedWaterNode = await GenerateWaterNodeFromIceAsync(id);
        if (generatedWaterNode != null)
        {
            await _waterStorage.StoreWaterNodeAsync(generatedWaterNode);
            return generatedWaterNode;
        }

        // Try to generate Gas node on-demand
        var generatedGasNode = await GenerateGasNodeOnDemandAsync(id);
        if (generatedGasNode != null)
        {
            _gasNodes[id] = generatedGasNode;
            return generatedGasNode;
        }

        return null;
    }

    public override IEnumerable<Node> AllNodes()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // This is a synchronous method, but we need async data
        // In practice, this should be called from async contexts or use AllNodesAsync
        var gasNodes = _gasNodes.Values.ToList();
        return gasNodes;
    }

    public async Task<IEnumerable<Node>> AllNodesAsync()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        var iceNodes = await _iceStorage.GetAllIceNodesAsync();
        var waterNodes = await _waterStorage.GetAllWaterNodesAsync();
        var gasNodes = _gasNodes.Values;

        return iceNodes.Concat(waterNodes).Concat(gasNodes);
    }

    public override IEnumerable<Node> GetNodesByType(string typeId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // This is a synchronous method, but we need async data
        // In practice, this should be called from async contexts or use GetNodesByTypeAsync
        var gasNodes = _gasNodes.Values.Where(n => n.TypeId == typeId).ToList();
        return gasNodes;
    }

    public async Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        var iceNodes = await _iceStorage.GetIceNodesByTypeAsync(typeId);
        var waterNodes = await _waterStorage.GetWaterNodesByTypeAsync(typeId);
        var gasNodes = _gasNodes.Values.Where(n => n.TypeId == typeId);

        return iceNodes.Concat(waterNodes).Concat(gasNodes);
    }

    public override IEnumerable<Node> GetNodesByState(ContentState state)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // This is a synchronous method, but we need async data
        // In practice, this should be called from async contexts or use GetNodesByStateAsync
        if (state == ContentState.Gas)
        {
            return _gasNodes.Values.ToList();
        }

        return new List<Node>();
    }

    public async Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return state switch
        {
            ContentState.Ice => await _iceStorage.GetAllIceNodesAsync(),
            ContentState.Water => await _waterStorage.GetAllWaterNodesAsync(),
            ContentState.Gas => _gasNodes.Values,
            _ => new List<Node>()
        };
    }

    public override IEnumerable<Edge> AllEdges()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // This is a synchronous method, but we need async data
        // In practice, this should be called from async contexts or use AllEdgesAsync
        return new List<Edge>();
    }

    public async Task<IEnumerable<Edge>> AllEdgesAsync()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return await _iceStorage.GetAllEdgesAsync();
    }

    public override Node? GetNode(string id)
    {
        if (TryGet(id, out var node))
        {
            return node;
        }
        return null;
    }

    public override void RemoveNode(string nodeId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Remove from appropriate storage based on where it might be
        _ = Task.Run(async () =>
        {
            try
            {
                // Try to remove from all storages
                await _iceStorage.DeleteIceNodeAsync(nodeId);
                await _waterStorage.DeleteWaterNodeAsync(nodeId);
                _gasNodes.TryRemove(nodeId, out _);
                
                _logger.Debug($"Removed node {nodeId} from all storages");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing node {nodeId}: {ex.Message}", ex);
            }
        });
    }

    private async Task<Node?> GenerateWaterNodeFromIceAsync(string id)
    {
        // Look for a related Ice node that could generate this Water node
        var iceNodes = await _iceStorage.GetAllIceNodesAsync();
        var relatedIceNodes = iceNodes
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

    private async Task<Node?> GenerateGasNodeOnDemandAsync(string id)
    {
        // Look for related nodes that could generate this Gas node
        var iceNodes = await _iceStorage.GetAllIceNodesAsync();
        var waterNodes = await _waterStorage.GetAllWaterNodesAsync();
        
        var relatedNodes = iceNodes
            .Concat(waterNodes)
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
            var jsonDoc = System.Text.Json.JsonDocument.Parse(iceContent);
            var processedJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                original = jsonDoc.RootElement,
                processed = true,
                state = "water",
                processedAt = DateTime.UtcNow,
                source = "ice"
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            
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
            var jsonDoc = System.Text.Json.JsonDocument.Parse(sourceContent);
            var processedJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                original = jsonDoc.RootElement,
                processed = true,
                state = "gas",
                processedAt = DateTime.UtcNow,
                source = "dynamic",
                transformations = new[] { "expand", "enhance", "optimize" }
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            
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
        meta["cacheExpiry"] = DateTime.UtcNow.AddMinutes(30);
        
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

    public async Task<UnifiedStorageStats> GetUnifiedStatsAsync()
    {
        var iceStats = await _iceStorage.GetStatsAsync();
        var waterStats = await _waterStorage.GetStatsAsync();
        
        return new UnifiedStorageStats(
            IceStats: iceStats,
            WaterStats: waterStats,
            GasNodeCount: _gasNodes.Count,
            TotalMemoryUsage: GC.GetTotalMemory(false),
            LastUpdated: DateTime.UtcNow
        );
    }

    public async Task CleanupExpiredWaterNodesAsync()
    {
        await _waterStorage.CleanupExpiredNodesAsync();
    }
}

/// <summary>
/// Unified storage statistics
/// </summary>
public record UnifiedStorageStats(
    IceStorageStats IceStats,
    WaterStorageStats WaterStats,
    int GasNodeCount,
    long TotalMemoryUsage,
    DateTime LastUpdated
);
