using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CodexBootstrap.Core.Storage;

namespace CodexBootstrap.Core;

/// <summary>
/// NodeRegistry that seamlessly integrates Ice and Water storage backends
/// - Ice nodes: Stored in high-performance, federated storage (PostgreSQL)
/// - Water nodes: Stored in semi-persistent, local cache (SQLite)
/// - Gas nodes: Generated on-demand, not persisted
/// <remarks>
/// Edge durability matches the more fluid endpoint: edges persist in the most fluid state of their endpoints (Gas > Water > Ice).
/// This ensures edges can only link from inside out, not outside in.
/// </remarks>
/// </summary>
public class NodeRegistry : INodeRegistry
{
    private readonly IIceStorageBackend _iceStorage;
    private readonly IWaterStorageBackend _waterStorage;
    private readonly ICodexLogger _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ConcurrentDictionary<string, Node> _iceNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Node> _waterNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Node> _gasNodes = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, EdgeRecord> _edgeRecords = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _nodeEdgeIndex = new(StringComparer.OrdinalIgnoreCase);
    private bool _isInitialized = false;
    
    // Track DB operations for health monitoring
    private long _dbOperationsInFlight = 0;

    private sealed class EdgeRecord
    {
        public EdgeRecord(Edge edge, ContentState state)
        {
            Edge = edge;
            State = state;
        }

        public Edge Edge { get; private set; }
        public ContentState State { get; private set; }

        public void UpdateEdge(Edge edge) => Edge = edge;
        public void UpdateState(ContentState newState) => State = newState;
    }

    public NodeRegistry(IIceStorageBackend iceStorage, IWaterStorageBackend waterStorage, ICodexLogger logger)
    {
        _iceStorage = iceStorage;
        _waterStorage = waterStorage;
        _logger = logger;
        
        // Log registry construction with stack trace to identify multiple instances
        _logger.Info($"NodeRegistry constructed at {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
        _logger.Info($"NodeRegistry stack trace: {Environment.StackTrace}");
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        // Initialize storage backends outside of lock
        Interlocked.Increment(ref _dbOperationsInFlight);
        try
        {
            await _iceStorage.InitializeAsync();
            await _waterStorage.InitializeAsync();
        }
        finally
        {
            Interlocked.Decrement(ref _dbOperationsInFlight);
        }

        List<Node> iceNodes = new();
        List<Node> waterNodes = new();

        try
        {
            var loadedIceNodes = await _iceStorage.GetAllIceNodesAsync();
            if (loadedIceNodes != null)
            {
                iceNodes = loadedIceNodes.ToList();
                _logger.Info($"Loaded {iceNodes.Count} Ice nodes from storage backend");
            }
            else
            {
                _logger.Warn("Ice storage backend returned null nodes");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error hydrating Ice nodes: {ex.Message}", ex);
        }

        try
        {
            var loadedWaterNodes = await _waterStorage.GetAllWaterNodesAsync();
            if (loadedWaterNodes != null)
            {
                waterNodes = loadedWaterNodes.ToList();
                _logger.Info($"Loaded {waterNodes.Count} Water nodes from storage backend");
            }
            else
            {
                _logger.Warn("Water storage backend returned null nodes");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error hydrating Water nodes: {ex.Message}", ex);
        }

        var hydrationPerformed = false;
        var skippedWaterCount = 0;
        var hydratedIceCount = 0;
        var hydratedWaterCount = 0;

        _lock.EnterWriteLock();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            hydrationPerformed = true;

            foreach (var node in iceNodes)
            {
                if (node == null) continue; // Skip null nodes
                
                var normalizedNode = node.State == ContentState.Ice
                    ? node
                    : node with { State = ContentState.Ice };

                CacheNodeInMemory(normalizedNode);
            }

            foreach (var node in waterNodes)
            {
                if (node == null) continue; // Skip null nodes
                
                if (_iceNodes.ContainsKey(node.Id))
                {
                    skippedWaterCount++;
                    continue;
                }

                var normalizedNode = node.State == ContentState.Water
                    ? node
                    : node with { State = ContentState.Water };

                CacheNodeInMemory(normalizedNode);
            }

            hydratedIceCount = _iceNodes.Count;
            hydratedWaterCount = _waterNodes.Count;
            var totalHydratedCount = hydratedIceCount + hydratedWaterCount;

            _logger.Info($"NodeRegistry initialization complete - RAM collections: Ice={hydratedIceCount}, Water={hydratedWaterCount}, Total={totalHydratedCount}");
            _logger.Info($"Skipped {skippedWaterCount} Water nodes (already in Ice collection)");

            // Load edges from storage
            await LoadEdgesFromStorageAsync();

            _isInitialized = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        if (hydrationPerformed)
        {
            _logger.Info("UnifiedNodeRegistry initialized with Ice and Water storage backends");
            if (skippedWaterCount > 0)
            {
                _logger.Info($"Hydrated {hydratedIceCount} Ice nodes and {hydratedWaterCount} Water nodes into memory (skipped {skippedWaterCount} Water nodes shadowed by Ice)");
            }
            else
            {
                _logger.Info($"Hydrated {hydratedIceCount} Ice nodes and {hydratedWaterCount} Water nodes into memory");
            }
        }
    }

    private async Task LoadEdgesFromStorageAsync()
    {
        Interlocked.Increment(ref _dbOperationsInFlight);
        try
        {
            // Load edges from Ice storage
            var iceEdges = await _iceStorage.GetAllEdgesAsync();
            foreach (var edge in iceEdges)
            {
                if (edge != null)
                {
                    var edgeKey = BuildEdgeKey(edge.FromId, edge.ToId, edge.Role);
                    var record = new EdgeRecord(edge, ContentState.Ice);
                    _edgeRecords[edgeKey] = record;
                    IndexEdge(edgeKey, edge);
                }
            }

            // Load edges from Water storage
            var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
            foreach (var edge in waterEdges)
            {
                if (edge != null)
                {
                    var edgeKey = BuildEdgeKey(edge.FromId, edge.ToId, edge.Role);
                    // Only add if not already present from Ice storage (Ice takes precedence)
                    if (!_edgeRecords.ContainsKey(edgeKey))
                    {
                        var record = new EdgeRecord(edge, ContentState.Water);
                        _edgeRecords[edgeKey] = record;
                        IndexEdge(edgeKey, edge);
                    }
                }
            }

            _logger.Info($"Loaded {_edgeRecords.Count} edges from storage");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error loading edges from storage: {ex.Message}", ex);
        }
        finally
        {
            Interlocked.Decrement(ref _dbOperationsInFlight);
        }
    }

    public void Upsert(Node node)
    {
        _logger.Info($"NodeRegistry.Upsert called for node {node.Id} (State: {node.State})");
        _logger.Info($"Registry initialized: {_isInitialized}, Water nodes count: {_waterNodes.Count}");
        
        if (!_isInitialized)
        {
            _logger.Warn($"Registry not initialized, but storing node {node.Id} anyway");
        }

        // Determine if this is a new node before mutating collections
        var isNewNode = !_iceNodes.ContainsKey(node.Id) && !_waterNodes.ContainsKey(node.Id) && !_gasNodes.ContainsKey(node.Id);

        _lock.EnterWriteLock();
        try
        {
            CacheNodeInMemory(node);

            // Store in local collections immediately for synchronous access
            switch (node.State)
            {
                case ContentState.Ice:
                    _logger.Info($"Stored Ice node {node.Id} in local collection (total Ice nodes: {_iceNodes.Count})");
                    break;
                case ContentState.Water:
                    _logger.Info($"Stored Water node {node.Id} in local collection (total Water nodes: {_waterNodes.Count})");
                    break;
                case ContentState.Gas:
                    _logger.Info($"Stored Gas node {node.Id} in local collection (total Gas nodes: {_gasNodes.Count})");
                    break;
            }

            // Also store in persistent storage asynchronously
            _ = Task.Run(async () =>
            {
                Interlocked.Increment(ref _dbOperationsInFlight);
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
                            // Gas nodes are only in memory, no persistent storage needed
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error storing {node.State} node {node.Id}: {ex.Message}", ex);
                }
                finally
                {
                    Interlocked.Decrement(ref _dbOperationsInFlight);
                }
            });

        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Log after releasing the lock to avoid potential deadlocks
        _logger.Info($"Successfully cached {node.State} node {node.Id} in memory");

        // Enforce edges only for brand-new nodes
        if (isNewNode)
        {
            try
            {
                EnforceEdgesForNewNode(node);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Edge enforcement failed for new node {node.Id}: {ex.Message}");
            }
        }
    }

    private void EnforceEdgesForNewNode(Node node)
    {
        // 1) Instance-of edge to the node's declared typeId (types-as-nodes invariant)
        var metaTypeId = node.TypeId;
        if (!string.IsNullOrWhiteSpace(metaTypeId))
        {
            var instanceEdge = NodeHelpers.CreateEdge(
                node.Id,
                metaTypeId!,
                role: "instance-of",
                weight: 1.0,
                meta: new Dictionary<string, object> { ["relationship"] = "node-instance-of-type" },
                roleId: NodeHelpers.TryResolveRoleId(this, "instance-of")
            );
            Upsert(instanceEdge);
        }

        // 2) Derived relationship from parentNodeId -> node
        if (node.Meta != null && node.Meta.TryGetValue("parentNodeId", out var parentObj))
        {
            var parentId = parentObj?.ToString();
            if (!string.IsNullOrWhiteSpace(parentId))
            {
                var derivedEdge = NodeHelpers.CreateEdge(
                    parentId!,
                    node.Id,
                    role: "has_content",
                    weight: 1.0,
                    meta: new Dictionary<string, object> { ["relationship"] = "node-has-content" },
                    roleId: NodeHelpers.TryResolveRoleId(this, "has_content")
                );
                Upsert(derivedEdge);
            }
        }

        // 3) Content type relationship node -> contentType
        var mediaType = node.Content?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType))
        {
            string contentTypeId = mediaType switch
            {
                "text/plain" => "codex.meta/type/text",
                "text/markdown" => "codex.meta/type/markdown",
                "application/json" => "codex.meta/type/json",
                "text/html" => "codex.meta/type/html",
                "image/png" => "codex.meta/type/image",
                "image/jpeg" => "codex.meta/type/image",
                "image/svg+xml" => "codex.meta/type/svg",
                "video/mp4" => "codex.meta/type/video",
                "audio/mp3" => "codex.meta/type/audio",
                _ => "codex.meta/type/content"
            };

            var contentTypeEdge = NodeHelpers.CreateEdge(
                node.Id,
                contentTypeId,
                role: "has_content_type",
                weight: 1.0,
                meta: new Dictionary<string, object> { ["relationship"] = "node-has-content-type", ["mediaType"] = mediaType! },
                roleId: NodeHelpers.TryResolveRoleId(this, "has_content_type")
            );
            Upsert(contentTypeEdge);
        }
    }

    private void CacheNodeInMemory(Node node)
    {
        // Expecting caller to hold write lock
        _iceNodes.TryRemove(node.Id, out _);
        _waterNodes.TryRemove(node.Id, out _);
        _gasNodes.TryRemove(node.Id, out _);

        switch (node.State)
        {
            case ContentState.Ice:
                _iceNodes[node.Id] = node;
                break;
            case ContentState.Water:
                _waterNodes[node.Id] = node;
                break;
            case ContentState.Gas:
                _gasNodes[node.Id] = node;
                break;
            default:
                _logger.Warn($"Attempted to cache node {node.Id} with unrecognized state {node.State}");
                break;
        }

        // Ensure connected edges are re-evaluated when this node changes state
        ReevaluateEdgesForNode(node.Id);
    }

    public void Upsert(Edge edge)
    {
        _lock.EnterWriteLock();
        try
        {
            // Allow edge upsert even before full initialization.
            // Edges will be kept in memory and persisted once initialization completes.

            var edgeKey = BuildEdgeKey(edge.FromId, edge.ToId, edge.Role);
            if (_edgeRecords.TryGetValue(edgeKey, out var record))
            {
                var previousState = record.State;
                var previousEdge = record.Edge;

                if (!previousEdge.Equals(edge))
                {
                    // If endpoints changed we need to rebuild index
                    if (!string.Equals(previousEdge.FromId, edge.FromId, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(previousEdge.ToId, edge.ToId, StringComparison.OrdinalIgnoreCase))
                    {
                        RemoveEdgeFromIndex(edgeKey, previousEdge);
                        IndexEdge(edgeKey, edge);
                    }

                    record.UpdateEdge(edge);
                }

                var desiredState = DetermineDesiredEdgeState(edge.FromId, edge.ToId);
                if (desiredState != previousState)
                {
                    HandleEdgeStateTransition(edgeKey, record, previousState, desiredState);
                }
            }
            else
            {
                var desiredState = _isInitialized ? DetermineDesiredEdgeState(edge.FromId, edge.ToId) : ContentState.Gas;
                record = new EdgeRecord(edge, desiredState);
                _edgeRecords[edgeKey] = record;
                IndexEdge(edgeKey, edge);
                if (_isInitialized)
                {
                    HandleEdgeStateEntry(edgeKey, record, desiredState);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool TryGet(string id, out Node node)
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

        // Try to get node synchronously from memory first (all local collections)
        if (_iceNodes.TryGetValue(id, out node))
        {
            return true;
        }
        if (_waterNodes.TryGetValue(id, out node))
        {
            return true;
        }
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

    private static string BuildEdgeKey(string fromId, string toId, string role)
    {
        return $"{fromId}::{role}::{toId}".ToLowerInvariant();
    }

    private ContentState DetermineDesiredEdgeState(string fromId, string toId)
    {
        var fromState = TryGetNodeStateUnsafe(fromId);
        var toState = TryGetNodeStateUnsafe(toId);

        // If either endpoint is null (node not found), default to Gas
        if (fromState == null || toState == null)
        {
            return ContentState.Gas;
        }

        // Edge persists in the more fluid state (Gas > Water > Ice)
        // This ensures edges can only link from inside out, not outside in
        if (fromState == ContentState.Gas || toState == ContentState.Gas)
        {
            return ContentState.Gas;
        }

        if (fromState == ContentState.Water || toState == ContentState.Water)
        {
            return ContentState.Water;
        }

        // Both endpoints are Ice
        return ContentState.Ice;
    }

    private ContentState? TryGetNodeStateUnsafe(string nodeId)
    {
        if (_gasNodes.ContainsKey(nodeId))
        {
            return ContentState.Gas;
        }

        if (_waterNodes.ContainsKey(nodeId))
        {
            return ContentState.Water;
        }

        if (_iceNodes.ContainsKey(nodeId))
        {
            return ContentState.Ice;
        }

        return null;
    }

    private void HandleEdgeStateEntry(string edgeKey, EdgeRecord record, ContentState state)
    {
        record.UpdateState(state);

        if (state == ContentState.Ice)
        {
            PersistEdgeToIce(record.Edge);
        }
        else if (state == ContentState.Water)
        {
            PersistEdgeToWater(record.Edge);
        }
        // Gas edges remain in memory only
    }

    private void HandleEdgeStateTransition(string edgeKey, EdgeRecord record, ContentState previousState, ContentState newState)
    {
        if (previousState == newState)
        {
            return;
        }

        // Remove from previous storage
        if (previousState == ContentState.Ice && newState != ContentState.Ice)
        {
            RemoveEdgeFromIce(record.Edge);
        }
        else if (previousState == ContentState.Water && newState != ContentState.Water)
        {
            RemoveEdgeFromWater(record.Edge);
        }

        record.UpdateState(newState);

        // Store in new storage
        if (newState == ContentState.Ice)
        {
            PersistEdgeToIce(record.Edge);
        }
        else if (newState == ContentState.Water)
        {
            PersistEdgeToWater(record.Edge);
        }
        // Gas edges remain in memory only
    }

    private void PersistEdgeToIce(Edge edge)
    {
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

    private void RemoveEdgeFromIce(Edge edge)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _iceStorage.DeleteEdgeAsync(edge.FromId, edge.ToId, edge.Role);
                _logger.Debug($"Removed edge {edge.FromId}->{edge.ToId} from federated storage");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing edge {edge.FromId}->{edge.ToId}: {ex.Message}", ex);
            }
        });
    }

    private void PersistEdgeToWater(Edge edge)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _waterStorage.StoreWaterEdgeAsync(edge);
                _logger.Debug($"Stored edge {edge.FromId}->{edge.ToId} in Water storage");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error storing edge {edge.FromId}->{edge.ToId} in Water storage: {ex.Message}", ex);
            }
        });
    }

    private void RemoveEdgeFromWater(Edge edge)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _waterStorage.DeleteWaterEdgeAsync(edge.FromId, edge.ToId, edge.Role);
                _logger.Debug($"Removed edge {edge.FromId}->{edge.ToId} from Water storage");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing edge {edge.FromId}->{edge.ToId} from Water storage: {ex.Message}", ex);
            }
        });
    }

    private void IndexEdge(string edgeKey, Edge edge)
    {
        AddEdgeKeyForNode(edge.FromId, edgeKey);
        AddEdgeKeyForNode(edge.ToId, edgeKey);
    }

    private void RemoveEdgeFromIndex(string edgeKey, Edge edge)
    {
        RemoveEdgeKeyForNode(edge.FromId, edgeKey);
        RemoveEdgeKeyForNode(edge.ToId, edgeKey);
    }

    private void AddEdgeKeyForNode(string nodeId, string edgeKey)
    {
        if (!_nodeEdgeIndex.TryGetValue(nodeId, out var edges))
        {
            edges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _nodeEdgeIndex[nodeId] = edges;
        }
        edges.Add(edgeKey);
    }

    private void RemoveEdgeKeyForNode(string nodeId, string edgeKey)
    {
        if (_nodeEdgeIndex.TryGetValue(nodeId, out var edges))
        {
            edges.Remove(edgeKey);
            if (edges.Count == 0)
            {
                _nodeEdgeIndex.Remove(nodeId);
            }
        }
    }

    private void ReevaluateEdgesForNode(string nodeId)
    {
        if (!_nodeEdgeIndex.TryGetValue(nodeId, out var edgeKeys) || edgeKeys.Count == 0)
        {
            return;
        }

        // Create a copy to avoid modification during iteration
        var keysSnapshot = edgeKeys.ToList();
        foreach (var edgeKey in keysSnapshot)
        {
            if (_edgeRecords.TryGetValue(edgeKey, out var record))
            {
                var newState = DetermineDesiredEdgeState(record.Edge.FromId, record.Edge.ToId);
                if (newState != record.State)
                {
                    HandleEdgeStateTransition(edgeKey, record, record.State, newState);
                }
            }
        }
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

    public IEnumerable<Node> AllNodes()
    {
        _logger.Info($"NodeRegistry.AllNodes called - Ice: {_iceNodes.Count}, Water: {_waterNodes.Count}, Gas: {_gasNodes.Count}");
        
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                _logger.Warn("Registry not initialized, but returning nodes anyway");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Return all nodes from local collections
        var allNodes = new List<Node>();
        allNodes.AddRange(_iceNodes.Values);
        allNodes.AddRange(_waterNodes.Values);
        allNodes.AddRange(_gasNodes.Values);
        
        _logger.Info($"NodeRegistry.AllNodes returning {allNodes.Count} total nodes");
        return allNodes;
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

    public IEnumerable<Node> GetNodesByType(string typeId)
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

        // Log RAM collection counts for debugging
        var iceCount = _iceNodes.Count;
        var waterCount = _waterNodes.Count;
        var gasCount = _gasNodes.Count;
        var totalRamCount = iceCount + waterCount + gasCount;
        
        _logger.Info($"GetNodesByType('{typeId}') - RAM collections: Ice={iceCount}, Water={waterCount}, Gas={gasCount}, Total={totalRamCount}");

        // Return nodes from all local collections
        var allNodes = new List<Node>();
        var iceMatches = _iceNodes.Values.Where(n => n.TypeId == typeId).ToList();
        var waterMatches = _waterNodes.Values.Where(n => n.TypeId == typeId).ToList();
        var gasMatches = _gasNodes.Values.Where(n => n.TypeId == typeId).ToList();
        
        allNodes.AddRange(iceMatches);
        allNodes.AddRange(waterMatches);
        allNodes.AddRange(gasMatches);
        
        _logger.Info($"GetNodesByType('{typeId}') - Found in RAM: Ice={iceMatches.Count}, Water={waterMatches.Count}, Gas={gasMatches.Count}, Total={allNodes.Count}");
        
        return allNodes;
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

        var iceList = iceNodes.ToList();
        var waterList = waterNodes.ToList();
        var gasList = gasNodes.ToList();
        
        _logger.Info($"GetNodesByTypeAsync('{typeId}') - Storage backend counts: Ice={iceList.Count}, Water={waterList.Count}, Gas={gasList.Count}, Total={iceList.Count + waterList.Count + gasList.Count}");

        return iceList.Concat(waterList).Concat(gasList);
    }

    public IEnumerable<Node> GetNodesByTypePrefix(string typeIdPrefix)
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

        // Log RAM collection counts for debugging
        var iceCount = _iceNodes.Count;
        var waterCount = _waterNodes.Count;
        var gasCount = _gasNodes.Count;
        var totalRamCount = iceCount + waterCount + gasCount;
        
        _logger.Info($"GetNodesByTypePrefix('{typeIdPrefix}') - RAM collections: Ice={iceCount}, Water={waterCount}, Gas={gasCount}, Total={totalRamCount}");

        // Return nodes from all local collections matching the prefix
        var allNodes = new List<Node>();
        var iceMatches = _iceNodes.Values.Where(n => n.TypeId != null && n.TypeId.StartsWith(typeIdPrefix)).ToList();
        var waterMatches = _waterNodes.Values.Where(n => n.TypeId != null && n.TypeId.StartsWith(typeIdPrefix)).ToList();
        var gasMatches = _gasNodes.Values.Where(n => n.TypeId != null && n.TypeId.StartsWith(typeIdPrefix)).ToList();
        
        allNodes.AddRange(iceMatches);
        allNodes.AddRange(waterMatches);
        allNodes.AddRange(gasMatches);
        
        _logger.Info($"GetNodesByTypePrefix('{typeIdPrefix}') - Found in RAM: Ice={iceMatches.Count}, Water={waterMatches.Count}, Gas={gasMatches.Count}, Total={allNodes.Count}");
        
        return allNodes;
    }

    public IEnumerable<Node> GetNodesByState(ContentState state)
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

    public IEnumerable<Edge> AllEdges()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }

            return _edgeRecords.Values.Select(r => r.Edge).ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IEnumerable<Edge>> AllEdgesAsync()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }

            var snapshot = _edgeRecords.Values.Select(r => r.Edge).ToList().AsEnumerable();
            return Task.FromResult(snapshot);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Node? GetNode(string id)
    {
        if (TryGet(id, out var node))
        {
            return node;
        }
        return null;
    }

    public void RemoveNode(string nodeId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Registry not initialized. Call InitializeAsync() first.");
            }

            // Remove from local collections immediately
            _iceNodes.TryRemove(nodeId, out _);
            _waterNodes.TryRemove(nodeId, out _);
            _gasNodes.TryRemove(nodeId, out _);
            
            _logger.Debug($"Removed node {nodeId} from local collections");

            if (_nodeEdgeIndex.TryGetValue(nodeId, out var edgeKeys) && edgeKeys.Count > 0)
            {
                var keysSnapshot = edgeKeys.ToList();
                foreach (var edgeKey in keysSnapshot)
                {
                    if (_edgeRecords.TryGetValue(edgeKey, out var record))
                    {
                        if (record.State == ContentState.Ice)
                        {
                            RemoveEdgeFromIce(record.Edge);
                        }

                        RemoveEdgeFromIndex(edgeKey, record.Edge);
                        _edgeRecords.Remove(edgeKey);
                    }
                }

                _nodeEdgeIndex.Remove(nodeId);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        // Remove from storage backends asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                // Try to remove from all storages
                await _iceStorage.DeleteIceNodeAsync(nodeId);
                await _waterStorage.DeleteWaterNodeAsync(nodeId);
                
                _logger.Debug($"Removed node {nodeId} from storage backends");
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
            .Where(n => n != null && (n.Id == id || 
                       (n.Meta?.ContainsKey("generates") == true && 
                        n.Meta["generates"] is string generates && 
                        generates == id)))
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
            .Where(n => n != null && (n.Id == id || 
                       (n.Meta?.ContainsKey("generates") == true && 
                        n.Meta["generates"] is string generates && 
                        generates == id)))
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

    public async Task<UnifiedStorageStats> GetStatsAsync()
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

    public IEnumerable<Edge> GetEdgesFrom(string fromId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                return Enumerable.Empty<Edge>();
            }
            if (_nodeEdgeIndex.TryGetValue(fromId, out var edgeKeys))
            {
                return edgeKeys
                    .Select(key => _edgeRecords.TryGetValue(key, out var record) ? record.Edge : null)
                    .Where(edge => edge != null)!
                    .Cast<Edge>()
                    .ToList();
            }
            return Enumerable.Empty<Edge>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<Edge> GetEdgesTo(string toId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                return Enumerable.Empty<Edge>();
            }
            if (_nodeEdgeIndex.TryGetValue(toId, out var edgeKeys))
            {
                return edgeKeys
                    .Select(key => _edgeRecords.TryGetValue(key, out var record) ? record.Edge : null)
                    .Where(edge => edge != null)!
                    .Cast<Edge>()
                    .Where(edge => edge.ToId.Equals(toId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return Enumerable.Empty<Edge>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerable<Edge> GetEdges()
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                return Enumerable.Empty<Edge>();
            }
            return AllEdges();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Edge? GetEdge(string edgeId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                return null;
            }
            var allEdges = AllEdges();
            // Since Edge doesn't have an Id property, we'll need to use a different approach
            // For now, return null as this method needs to be redesigned
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Edge? GetEdge(string fromId, string toId)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_isInitialized)
            {
                return null;
            }
            // Access edges directly from local collection to avoid lock recursion
            if (_nodeEdgeIndex.TryGetValue(fromId, out var edgeKeys))
            {
                foreach (var edgeKey in edgeKeys)
                {
                    if (_edgeRecords.TryGetValue(edgeKey, out var record))
                    {
                        var edge = record.Edge;
                        if (edge.FromId.Equals(fromId, StringComparison.OrdinalIgnoreCase) &&
                            edge.ToId.Equals(toId, StringComparison.OrdinalIgnoreCase))
                        {
                            return edge;
                        }
                    }
                }
            }
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void RemoveEdge(string edgeId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_isInitialized)
            {
                return;
            }
            
            // Since Edge doesn't have an Id property, this method needs to be redesigned
            // For now, just log that it's not implemented
            _logger.Warn($"Edge removal not implemented - Edge record doesn't have Id property: {edgeId}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveEdge(string fromId, string toId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_isInitialized)
            {
                return;
            }
            
            // Since Edge doesn't have an Id property, this method needs to be redesigned
            // For now, just log that it's not implemented
            _logger.Warn($"Edge removal not implemented - Edge record doesn't have Id property: {fromId} -> {toId}");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all nodes and edges from the registry (for testing purposes)
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_isInitialized)
            {
                return;
            }
            
            _iceNodes.Clear();
            _waterNodes.Clear();
            _gasNodes.Clear();
            _edgeRecords.Clear();
            _nodeEdgeIndex.Clear();
            
            _logger.Debug("Registry cleared - all nodes and edges removed");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the current count of DB operations in flight (for health monitoring)
    /// </summary>
    public long GetDbOperationsInFlight()
    {
        return Interlocked.Read(ref _dbOperationsInFlight);
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
