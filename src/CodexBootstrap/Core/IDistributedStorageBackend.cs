using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for distributed storage backends that support replication and sharding
/// </summary>
public interface IDistributedStorageBackend : IStorageBackend
{
    /// <summary>
    /// Get the node ID of this storage instance
    /// </summary>
    string NodeId { get; }
    
    /// <summary>
    /// Get the cluster configuration
    /// </summary>
    ClusterConfig ClusterConfig { get; }
    
    /// <summary>
    /// Check if this node is the primary for a given key
    /// </summary>
    Task<bool> IsPrimaryForAsync(string key);
    
    /// <summary>
    /// Get the primary node for a given key
    /// </summary>
    Task<string?> GetPrimaryNodeForAsync(string key);
    
    /// <summary>
    /// Replicate data to other nodes in the cluster
    /// </summary>
    Task<bool> ReplicateAsync(Node node, int replicationFactor = 3);
    
    /// <summary>
    /// Replicate edge data to other nodes in the cluster
    /// </summary>
    Task<bool> ReplicateEdgeAsync(Edge edge, int replicationFactor = 3);
    
    /// <summary>
    /// Get nodes from the cluster (with eventual consistency)
    /// </summary>
    Task<IEnumerable<Node>> GetClusterNodesAsync();
    
    /// <summary>
    /// Get edges from the cluster (with eventual consistency)
    /// </summary>
    Task<IEnumerable<Edge>> GetClusterEdgesAsync();
    
    /// <summary>
    /// Join a cluster
    /// </summary>
    Task<bool> JoinClusterAsync(string clusterId, string[] seedNodes);
    
    /// <summary>
    /// Leave the cluster
    /// </summary>
    Task<bool> LeaveClusterAsync();
    
    /// <summary>
    /// Get cluster health status
    /// </summary>
    Task<ClusterHealth> GetClusterHealthAsync();
    
    /// <summary>
    /// Repair inconsistent data in the cluster
    /// </summary>
    Task<RepairResult> RepairClusterAsync();
}

/// <summary>
/// Cluster configuration
/// </summary>
public record ClusterConfig(
    string ClusterId,
    string[] SeedNodes,
    int ReplicationFactor = 3,
    int ReadConsistencyLevel = 1,
    int WriteConsistencyLevel = 1
)
{
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan FailureDetectionTimeout { get; init; } = TimeSpan.FromMinutes(2);
}

/// <summary>
/// Cluster health status
/// </summary>
public record ClusterHealth(
    bool IsHealthy,
    int TotalNodes,
    int HealthyNodes,
    int UnhealthyNodes,
    string[] UnhealthyNodeIds,
    double DataConsistencyScore,
    DateTime LastChecked
);

/// <summary>
/// Repair operation result
/// </summary>
public record RepairResult(
    bool Success,
    int NodesRepaired,
    int EdgesRepaired,
    int ConflictsResolved,
    TimeSpan Duration,
    string[] Errors
);
