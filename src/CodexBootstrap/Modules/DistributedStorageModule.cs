using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Distributed storage management module
/// </summary>
public sealed class DistributedStorageModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly IDistributedStorageBackend? _distributedStorage;
    private readonly DistributedCacheManager? _distributedCacheManager;
    private readonly Core.ILogger _logger;

    public DistributedStorageModule(NodeRegistry registry, IDistributedStorageBackend? distributedStorage = null)
    {
        _registry = registry;
        _distributedStorage = distributedStorage;
        _distributedCacheManager = distributedStorage != null ? 
            new DistributedCacheManager(distributedStorage, new NodeCacheManager(distributedStorage)) : null;
        _logger = new Log4NetLogger(typeof(DistributedStorageModule));
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.distributed-storage",
            name: "Distributed Storage Management Module",
            version: "0.1.0",
            description: "Module for managing distributed storage clusters and replication",
            capabilities: new[] { "distributed-storage", "clusters", "replication", "management" },
            tags: new[] { "distributed", "storage", "cluster", "replication" },
            specReference: "codex.spec.distributed-storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var clusterHealthApi = NodeStorage.CreateApiNode("codex.distributed-storage", "cluster-health", "/cluster/health", "Get cluster health status");
        var clusterNodesApi = NodeStorage.CreateApiNode("codex.distributed-storage", "cluster-nodes", "/cluster/nodes", "Get cluster nodes");
        var joinClusterApi = NodeStorage.CreateApiNode("codex.distributed-storage", "join-cluster", "/cluster/join", "Join a cluster");
        var leaveClusterApi = NodeStorage.CreateApiNode("codex.distributed-storage", "leave-cluster", "/cluster/leave", "Leave the cluster");
        var repairClusterApi = NodeStorage.CreateApiNode("codex.distributed-storage", "repair-cluster", "/cluster/repair", "Repair cluster data");
        var syncClusterApi = NodeStorage.CreateApiNode("codex.distributed-storage", "sync-cluster", "/cluster/sync", "Sync with cluster");
        
        registry.Upsert(clusterHealthApi);
        registry.Upsert(clusterNodesApi);
        registry.Upsert(joinClusterApi);
        registry.Upsert(leaveClusterApi);
        registry.Upsert(repairClusterApi);
        registry.Upsert(syncClusterApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "cluster-health"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "cluster-nodes"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "join-cluster"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "leave-cluster"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "repair-cluster"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.distributed-storage", "sync-cluster"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Handled by attribute discovery
    }

    [ApiRoute("GET", "/cluster/health", "cluster-health", "Get cluster health status", "codex.distributed-storage")]
    public async Task<object> GetClusterHealthAsync()
    {
        try
        {
            if (_distributedStorage == null)
            {
                return new ErrorResponse("Distributed storage not configured");
            }

            var health = await _distributedStorage.GetClusterHealthAsync();
            return new
            {
                success = true,
                health = new
                {
                    isHealthy = health.IsHealthy,
                    totalNodes = health.TotalNodes,
                    healthyNodes = health.HealthyNodes,
                    unhealthyNodes = health.UnhealthyNodes,
                    unhealthyNodeIds = health.UnhealthyNodeIds,
                    dataConsistencyScore = health.DataConsistencyScore,
                    lastChecked = health.LastChecked
                }
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cluster health: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/cluster/nodes", "cluster-nodes", "Get cluster nodes", "codex.distributed-storage")]
    public async Task<object> GetClusterNodesAsync()
    {
        try
        {
            if (_distributedStorage == null)
            {
                return new ErrorResponse("Distributed storage not configured");
            }

            var nodes = await _distributedStorage.GetClusterNodesAsync();
            return new
            {
                success = true,
                nodes = nodes.Select(n => new
                {
                    id = n.Id,
                    typeId = n.TypeId,
                    state = n.State.ToString(),
                    title = n.Title,
                    description = n.Description
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cluster nodes: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cluster/join", "join-cluster", "Join a cluster", "codex.distributed-storage")]
    public async Task<object> JoinClusterAsync(JoinClusterRequest request)
    {
        try
        {
            if (_distributedStorage == null)
            {
                return new ErrorResponse("Distributed storage not configured");
            }

            var success = await _distributedStorage.JoinClusterAsync(request.ClusterId, request.SeedNodes);
            return new
            {
                success = success,
                message = success ? "Successfully joined cluster" : "Failed to join cluster",
                clusterId = request.ClusterId,
                seedNodes = request.SeedNodes
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to join cluster: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cluster/leave", "leave-cluster", "Leave the cluster", "codex.distributed-storage")]
    public async Task<object> LeaveClusterAsync()
    {
        try
        {
            if (_distributedStorage == null)
            {
                return new ErrorResponse("Distributed storage not configured");
            }

            var success = await _distributedStorage.LeaveClusterAsync();
            return new
            {
                success = success,
                message = success ? "Successfully left cluster" : "Failed to leave cluster"
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to leave cluster: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cluster/repair", "repair-cluster", "Repair cluster data", "codex.distributed-storage")]
    public async Task<object> RepairClusterAsync()
    {
        try
        {
            if (_distributedStorage == null)
            {
                return new ErrorResponse("Distributed storage not configured");
            }

            var result = await _distributedStorage.RepairClusterAsync();
            return new
            {
                success = result.Success,
                message = result.Success ? "Cluster repair completed successfully" : "Cluster repair failed",
                repair = new
                {
                    nodesRepaired = result.NodesRepaired,
                    edgesRepaired = result.EdgesRepaired,
                    conflictsResolved = result.ConflictsResolved,
                    duration = result.Duration.TotalSeconds,
                    errors = result.Errors
                }
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to repair cluster: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cluster/sync", "sync-cluster", "Sync with cluster", "codex.distributed-storage")]
    public async Task<object> SyncClusterAsync()
    {
        try
        {
            if (_distributedCacheManager == null)
            {
                return new ErrorResponse("Distributed cache manager not configured");
            }

            await _distributedCacheManager.SyncWithDistributedStorageAsync();
            return new
            {
                success = true,
                message = "Successfully synced with cluster"
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to sync with cluster: {ex.Message}");
        }
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Discovery is handled globally
    }
}

/// <summary>
/// Request to join a cluster
/// </summary>
public record JoinClusterRequest(
    string ClusterId,
    string[] SeedNodes
);
