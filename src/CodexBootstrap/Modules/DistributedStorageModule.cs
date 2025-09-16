using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Distributed storage management module
/// </summary>
public sealed class DistributedStorageModule : ModuleBase
{
    private readonly IDistributedStorageBackend? _distributedStorage;
    private readonly DistributedCacheManager? _distributedCacheManager;

    public override string Name => "Distributed Storage Management Module";
    public override string Description => "Distributed storage management module";
    public override string Version => "0.1.0";

    public DistributedStorageModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, IDistributedStorageBackend? distributedStorage = null) 
        : base(registry, logger)
    {
        _distributedStorage = distributedStorage;
        _distributedCacheManager = distributedStorage != null ? 
            new DistributedCacheManager(distributedStorage, new NodeCacheManager(distributedStorage, logger), logger) : null;
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.distributed-storage",
            name: "Distributed Storage Management Module",
            version: "0.1.0",
            description: "Module for managing distributed storage clusters and replication",
            tags: new[] { "distributed", "storage", "cluster", "replication" },
            capabilities: new[] { "distributed-storage", "clusters", "replication", "management" },
            spec: "codex.spec.distributed-storage"
        );
    }


    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
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

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
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
