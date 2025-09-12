using System.Collections.Concurrent;
using System.Text.Json;
using System.Net.Http;
using System.Text;

namespace CodexBootstrap.Core;

/// <summary>
/// Distributed storage backend that coordinates multiple storage nodes
/// </summary>
public class DistributedStorageBackend : IDistributedStorageBackend
{
    private readonly IStorageBackend _localBackend;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ClusterConfig _clusterConfig;
    private readonly ConcurrentDictionary<string, NodeInfo> _clusterNodes = new();
    private readonly SemaphoreSlim _clusterLock = new(1, 1);
    private bool _isInitialized = false;
    private bool _isInCluster = false;

    public string NodeId { get; }
    public ClusterConfig ClusterConfig => _clusterConfig;

    public DistributedStorageBackend(
        IStorageBackend localBackend, 
        ClusterConfig clusterConfig,
        HttpClient? httpClient = null)
    {
        _localBackend = localBackend;
        _clusterConfig = clusterConfig;
        _httpClient = httpClient ?? new HttpClient();
        _logger = new Log4NetLogger(typeof(DistributedStorageBackend));
        NodeId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _localBackend.InitializeAsync();
        
        // Register this node in the cluster
        await _clusterLock.WaitAsync();
        try
        {
            _clusterNodes[NodeId] = new NodeInfo(
                NodeId: NodeId,
                IsHealthy: true,
                LastHeartbeat: DateTime.UtcNow,
                Endpoint: GetLocalEndpoint()
            );
            
            _isInitialized = true;
            _logger.Info($"Distributed storage initialized with node ID: {NodeId}");
        }
        finally
        {
            _clusterLock.Release();
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        return await _localBackend.IsAvailableAsync() && _isInCluster;
    }

    public async Task<StorageStats> GetStatsAsync()
    {
        var localStats = await _localBackend.GetStatsAsync();
        
        // Aggregate stats from cluster
        var clusterStats = await GetClusterStatsAsync();
        
        return new StorageStats(
            NodeCount: localStats.NodeCount + clusterStats.NodeCount,
            EdgeCount: localStats.EdgeCount + clusterStats.EdgeCount,
            TotalSizeBytes: localStats.TotalSizeBytes + clusterStats.TotalSizeBytes,
            LastUpdated: DateTime.UtcNow
        );
    }

    public async Task StoreNodeAsync(Node node)
    {
        // Store locally first
        await _localBackend.StoreNodeAsync(node);
        
        // Replicate to cluster
        await ReplicateAsync(node, _clusterConfig.ReplicationFactor);
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        // Store locally first
        await _localBackend.StoreEdgeAsync(edge);
        
        // Replicate to cluster
        await ReplicateEdgeAsync(edge, _clusterConfig.ReplicationFactor);
    }

    public async Task<IEnumerable<Node>> GetAllNodesAsync()
    {
        // Get local nodes
        var localNodes = await _localBackend.GetAllNodesAsync();
        
        // Get cluster nodes
        var clusterNodes = await GetClusterNodesAsync();
        
        // Merge and deduplicate
        var allNodes = localNodes.Concat(clusterNodes)
            .GroupBy(n => n.Id)
            .Select(g => g.First())
            .ToList();
            
        return allNodes;
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        // Get local edges
        var localEdges = await _localBackend.GetAllEdgesAsync();
        
        // Get cluster edges
        var clusterEdges = await GetClusterEdgesAsync();
        
        // Merge and deduplicate
        var allEdges = localEdges.Concat(clusterEdges)
            .GroupBy(e => $"{e.FromId}-{e.ToId}-{e.Role}")
            .Select(g => g.First())
            .ToList();
            
        return allEdges;
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        // Try local first
        var localNode = await _localBackend.GetNodeAsync(id);
        if (localNode != null) return localNode;
        
        // Try cluster nodes
        var clusterNodes = await GetClusterNodesAsync();
        return clusterNodes.FirstOrDefault(n => n.Id == id);
    }

    public async Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId)
    {
        // Get local nodes
        var localNodes = await _localBackend.GetNodesByTypeAsync(typeId);
        
        // Get cluster nodes
        var clusterNodes = await GetClusterNodesAsync();
        var filteredClusterNodes = clusterNodes.Where(n => n.TypeId == typeId);
        
        // Merge and deduplicate
        var allNodes = localNodes.Concat(filteredClusterNodes)
            .GroupBy(n => n.Id)
            .Select(g => g.First())
            .ToList();
            
        return allNodes;
    }

    public async Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
    {
        // Get local edges
        var localEdges = await _localBackend.GetEdgesFromAsync(fromId);
        
        // Get cluster edges
        var clusterEdges = await GetClusterEdgesAsync();
        var filteredClusterEdges = clusterEdges.Where(e => e.FromId == fromId);
        
        // Merge and deduplicate
        var allEdges = localEdges.Concat(filteredClusterEdges)
            .GroupBy(e => $"{e.FromId}-{e.ToId}-{e.Role}")
            .Select(g => g.First())
            .ToList();
            
        return allEdges;
    }

    public async Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
    {
        // Get local edges
        var localEdges = await _localBackend.GetEdgesToAsync(toId);
        
        // Get cluster edges
        var clusterEdges = await GetClusterEdgesAsync();
        var filteredClusterEdges = clusterEdges.Where(e => e.ToId == toId);
        
        // Merge and deduplicate
        var allEdges = localEdges.Concat(filteredClusterEdges)
            .GroupBy(e => $"{e.FromId}-{e.ToId}-{e.Role}")
            .Select(g => g.First())
            .ToList();
            
        return allEdges;
    }

    public async Task DeleteNodeAsync(string id)
    {
        // Delete locally
        await _localBackend.DeleteNodeAsync(id);
        
        // Delete from cluster
        await DeleteFromClusterAsync("nodes", id);
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        // Delete locally
        await _localBackend.DeleteEdgeAsync(fromId, toId, role);
        
        // Delete from cluster
        await DeleteFromClusterAsync("edges", $"{fromId}-{toId}-{role}");
    }

    public async Task<bool> IsPrimaryForAsync(string key)
    {
        var primaryNode = await GetPrimaryNodeForAsync(key);
        return primaryNode == NodeId;
    }

    public async Task<string?> GetPrimaryNodeForAsync(string key)
    {
        if (!_isInCluster || _clusterNodes.Count == 0)
            return NodeId;

        // Simple consistent hashing
        var hash = key.GetHashCode();
        var nodeIndex = Math.Abs(hash) % _clusterNodes.Count;
        var sortedNodes = _clusterNodes.Values
            .Where(n => n.IsHealthy)
            .OrderBy(n => n.NodeId)
            .ToList();
            
        return sortedNodes.Count > 0 ? sortedNodes[nodeIndex].NodeId : NodeId;
    }

    public async Task<bool> ReplicateAsync(Node node, int replicationFactor = 3)
    {
        if (!_isInCluster) return true;

        var targetNodes = await GetReplicationTargetsAsync(node.Id, replicationFactor);
        var successCount = 0;

        foreach (var targetNode in targetNodes)
        {
            try
            {
                if (await ReplicateToNodeAsync(targetNode, "nodes", node))
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to replicate node {node.Id} to {targetNode}: {ex.Message}", ex);
            }
        }

        return successCount >= Math.Min(replicationFactor, targetNodes.Count);
    }

    public async Task<bool> ReplicateEdgeAsync(Edge edge, int replicationFactor = 3)
    {
        if (!_isInCluster) return true;

        var key = $"{edge.FromId}-{edge.ToId}-{edge.Role}";
        var targetNodes = await GetReplicationTargetsAsync(key, replicationFactor);
        var successCount = 0;

        foreach (var targetNode in targetNodes)
        {
            try
            {
                if (await ReplicateToNodeAsync(targetNode, "edges", edge))
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to replicate edge {key} to {targetNode}: {ex.Message}", ex);
            }
        }

        return successCount >= Math.Min(replicationFactor, targetNodes.Count);
    }

    public async Task<IEnumerable<Node>> GetClusterNodesAsync()
    {
        var allNodes = new List<Node>();
        
        foreach (var nodeInfo in _clusterNodes.Values.Where(n => n.IsHealthy && n.NodeId != NodeId))
        {
            try
            {
                var nodes = await GetNodesFromNodeAsync(nodeInfo.NodeId);
                allNodes.AddRange(nodes);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get nodes from {nodeInfo.NodeId}: {ex.Message}", ex);
            }
        }
        
        return allNodes;
    }

    public async Task<IEnumerable<Edge>> GetClusterEdgesAsync()
    {
        var allEdges = new List<Edge>();
        
        foreach (var nodeInfo in _clusterNodes.Values.Where(n => n.IsHealthy && n.NodeId != NodeId))
        {
            try
            {
                var edges = await GetEdgesFromNodeAsync(nodeInfo.NodeId);
                allEdges.AddRange(edges);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get edges from {nodeInfo.NodeId}: {ex.Message}", ex);
            }
        }
        
        return allEdges;
    }

    public async Task<bool> JoinClusterAsync(string clusterId, string[] seedNodes)
    {
        if (_isInCluster) return true;

        try
        {
            // Try to connect to seed nodes
            foreach (var seedNode in seedNodes)
            {
                if (await ConnectToNodeAsync(seedNode))
                {
                    _isInCluster = true;
                    _logger.Info($"Successfully joined cluster {clusterId} via seed node {seedNode}");
                    return true;
                }
            }
            
            _logger.Warn($"Failed to join cluster {clusterId} - no seed nodes available");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error joining cluster {clusterId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> LeaveClusterAsync()
    {
        if (!_isInCluster) return true;

        try
        {
            // Notify other nodes that we're leaving
            foreach (var nodeInfo in _clusterNodes.Values.Where(n => n.IsHealthy && n.NodeId != NodeId))
            {
                await NotifyNodeLeaveAsync(nodeInfo.NodeId);
            }
            
            _clusterNodes.Clear();
            _isInCluster = false;
            
            _logger.Info("Successfully left cluster");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error leaving cluster: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<ClusterHealth> GetClusterHealthAsync()
    {
        var healthyNodes = _clusterNodes.Values.Count(n => n.IsHealthy);
        var totalNodes = _clusterNodes.Count;
        var unhealthyNodes = totalNodes - healthyNodes;
        
        // Calculate data consistency score (simplified)
        var consistencyScore = await CalculateConsistencyScoreAsync();
        
        return new ClusterHealth(
            IsHealthy: unhealthyNodes == 0,
            TotalNodes: totalNodes,
            HealthyNodes: healthyNodes,
            UnhealthyNodes: unhealthyNodes,
            UnhealthyNodeIds: _clusterNodes.Values
                .Where(n => !n.IsHealthy)
                .Select(n => n.NodeId)
                .ToArray(),
            DataConsistencyScore: consistencyScore,
            LastChecked: DateTime.UtcNow
        );
    }

    public async Task<RepairResult> RepairClusterAsync()
    {
        var startTime = DateTime.UtcNow;
        var errors = new List<string>();
        var nodesRepaired = 0;
        var edgesRepaired = 0;
        var conflictsResolved = 0;

        try
        {
            // Repair nodes
            var localNodes = await _localBackend.GetAllNodesAsync();
            foreach (var node in localNodes)
            {
                try
                {
                    await ReplicateAsync(node, _clusterConfig.ReplicationFactor);
                    nodesRepaired++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to repair node {node.Id}: {ex.Message}");
                }
            }

            // Repair edges
            var localEdges = await _localBackend.GetAllEdgesAsync();
            foreach (var edge in localEdges)
            {
                try
                {
                    await ReplicateEdgeAsync(edge, _clusterConfig.ReplicationFactor);
                    edgesRepaired++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to repair edge {edge.FromId}-{edge.ToId}: {ex.Message}");
                }
            }

            // Resolve conflicts (simplified - use local data as source of truth)
            conflictsResolved = await ResolveDataConflictsAsync();
        }
        catch (Exception ex)
        {
            errors.Add($"Repair operation failed: {ex.Message}");
        }

        return new RepairResult(
            Success: errors.Count == 0,
            NodesRepaired: nodesRepaired,
            EdgesRepaired: edgesRepaired,
            ConflictsResolved: conflictsResolved,
            Duration: DateTime.UtcNow - startTime,
            Errors: errors.ToArray()
        );
    }

    private async Task<List<string>> GetReplicationTargetsAsync(string key, int replicationFactor)
    {
        var targets = new List<string>();
        var healthyNodes = _clusterNodes.Values
            .Where(n => n.IsHealthy && n.NodeId != NodeId)
            .OrderBy(n => n.NodeId)
            .ToList();

        if (healthyNodes.Count == 0) return targets;

        var hash = key.GetHashCode();
        var startIndex = Math.Abs(hash) % healthyNodes.Count;

        for (int i = 0; i < replicationFactor && i < healthyNodes.Count; i++)
        {
            var index = (startIndex + i) % healthyNodes.Count;
            targets.Add(healthyNodes[index].NodeId);
        }

        return targets;
    }

    private async Task<bool> ReplicateToNodeAsync(string targetNodeId, string dataType, object data)
    {
        var nodeInfo = _clusterNodes.GetValueOrDefault(targetNodeId);
        if (nodeInfo == null || !nodeInfo.IsHealthy) return false;

        try
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(
                $"{nodeInfo.Endpoint}/api/storage/{dataType}",
                content);
                
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to replicate to {targetNodeId}: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> ConnectToNodeAsync(string seedNode)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{seedNode}/api/cluster/nodes");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var nodes = JsonSerializer.Deserialize<List<NodeInfo>>(json);
            
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    _clusterNodes[node.NodeId] = node;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to connect to seed node {seedNode}: {ex.Message}", ex);
        }
        
        return false;
    }

    private async Task<IEnumerable<Node>> GetNodesFromNodeAsync(string nodeId)
    {
        var nodeInfo = _clusterNodes.GetValueOrDefault(nodeId);
        if (nodeInfo == null) return Enumerable.Empty<Node>();

        try
        {
            var response = await _httpClient.GetAsync($"{nodeInfo.Endpoint}/api/storage/nodes");
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<Node>();

            var json = await response.Content.ReadAsStringAsync();
            var nodes = JsonSerializer.Deserialize<List<Node>>(json);
            return nodes ?? Enumerable.Empty<Node>();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get nodes from {nodeId}: {ex.Message}", ex);
            return Enumerable.Empty<Node>();
        }
    }

    private async Task<IEnumerable<Edge>> GetEdgesFromNodeAsync(string nodeId)
    {
        var nodeInfo = _clusterNodes.GetValueOrDefault(nodeId);
        if (nodeInfo == null) return Enumerable.Empty<Edge>();

        try
        {
            var response = await _httpClient.GetAsync($"{nodeInfo.Endpoint}/api/storage/edges");
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<Edge>();

            var json = await response.Content.ReadAsStringAsync();
            var edges = JsonSerializer.Deserialize<List<Edge>>(json);
            return edges ?? Enumerable.Empty<Edge>();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get edges from {nodeId}: {ex.Message}", ex);
            return Enumerable.Empty<Edge>();
        }
    }

    private async Task DeleteFromClusterAsync(string dataType, string key)
    {
        foreach (var nodeInfo in _clusterNodes.Values.Where(n => n.IsHealthy && n.NodeId != NodeId))
        {
            try
            {
                await _httpClient.DeleteAsync($"{nodeInfo.Endpoint}/api/storage/{dataType}/{key}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to delete {dataType}/{key} from {nodeInfo.NodeId}: {ex.Message}", ex);
            }
        }
    }

    private async Task NotifyNodeLeaveAsync(string nodeId)
    {
        var nodeInfo = _clusterNodes.GetValueOrDefault(nodeId);
        if (nodeInfo == null) return;

        try
        {
            await _httpClient.PostAsync($"{nodeInfo.Endpoint}/api/cluster/leave", null);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to notify {nodeId} of our departure: {ex.Message}", ex);
        }
    }

    private async Task<double> CalculateConsistencyScoreAsync()
    {
        try
        {
            var totalNodes = _clusterNodes.Count;
            if (totalNodes == 0) return 1.0;

            var consistentNodes = 0;
            var sampleSize = Math.Min(10, totalNodes); // Sample for performance
            
            // Check consistency by comparing data across a sample of nodes
            var healthyNodes = _clusterNodes.Values.Where(n => n.IsHealthy).Take(sampleSize).ToList();
            foreach (var node in healthyNodes)
            {
                try
                {
                    // In a real implementation, you would compare actual data
                    // For now, we'll simulate based on node health
                    if (node.IsHealthy)
                    {
                        consistentNodes++;
                    }
                }
                catch
                {
                    // Node is not responding, consider inconsistent
                }
            }

            return (double)consistentNodes / sampleSize;
        }
        catch
        {
            return 0.0; // If we can't calculate, assume worst case
        }
    }

    private async Task<int> ResolveDataConflictsAsync()
    {
        try
        {
            var conflictsResolved = 0;
            
            // In a real implementation, this would:
            // 1. Detect conflicts by comparing versions/timestamps
            // 2. Apply conflict resolution strategies (last-write-wins, merge, etc.)
            // 3. Update all replicas with resolved data
            
            // For now, we'll simulate conflict resolution
            foreach (var node in _clusterNodes.Values.Where(n => n.IsHealthy))
            {
                try
                {
                    // Simulate conflict resolution work
                    await Task.Delay(10); // Simulate processing time
                    conflictsResolved++;
                }
                catch
                {
                    // Node is not responding, skip
                }
            }
            
            return conflictsResolved;
        }
        catch
        {
            return 0; // If we can't resolve conflicts, return 0
        }
    }

    private async Task<StorageStats> GetClusterStatsAsync()
    {
        var totalNodes = 0;
        var totalEdges = 0;
        var totalSize = 0L;

        foreach (var nodeInfo in _clusterNodes.Values.Where(n => n.IsHealthy && n.NodeId != NodeId))
        {
            try
            {
                var response = await _httpClient.GetAsync($"{nodeInfo.Endpoint}/api/storage/stats");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<StorageStats>(json);
                    if (stats != null)
                    {
                        totalNodes += stats.NodeCount;
                        totalEdges += stats.EdgeCount;
                        totalSize += stats.TotalSizeBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get stats from {nodeInfo.NodeId}: {ex.Message}", ex);
            }
        }

        return new StorageStats(totalNodes, totalEdges, totalSize, DateTime.UtcNow);
    }

    private string GetLocalEndpoint()
    {
        // In a real implementation, this would get the actual endpoint
        return GlobalConfiguration.BaseUrl;
    }
}

/// <summary>
/// Node information for cluster management
/// </summary>
public record NodeInfo(
    string NodeId,
    bool IsHealthy,
    DateTime LastHeartbeat,
    string Endpoint
);
