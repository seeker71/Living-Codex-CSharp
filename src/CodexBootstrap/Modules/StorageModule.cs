using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Storage management module that configures its own storage backend
/// </summary>
public sealed class StorageModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly IStorageBackend? _storage;
    private readonly Core.ILogger _logger;

    public StorageModule(NodeRegistry registry, IStorageBackend? storage = null)
    {
        _registry = registry;
        _logger = new Log4NetLogger(typeof(StorageModule));
        _storage = storage ?? ConfigureDefaultStorageBackend();
    }

    /// <summary>
    /// Get the configured storage backend
    /// </summary>
    public IStorageBackend? GetStorageBackend() => _storage;

    /// <summary>
    /// Configure the default storage backend based on environment or configuration
    /// </summary>
    private IStorageBackend ConfigureDefaultStorageBackend()
    {
        try
        {
            // Check for environment variables or configuration
            var storageType = Environment.GetEnvironmentVariable("STORAGE_TYPE")?.ToLower() ?? "jsonfile";
            var storagePath = Environment.GetEnvironmentVariable("STORAGE_PATH") ?? "data";
            var clusterId = Environment.GetEnvironmentVariable("CLUSTER_ID");
            var seedNodes = Environment.GetEnvironmentVariable("SEED_NODES")?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            
            _logger.Info($"Configuring storage backend: {storageType} at {storagePath}");

            IStorageBackend baseBackend = storageType switch
            {
                "sqlite" => new SqliteStorageBackend(storagePath),
                "jsonfile" => new JsonFileStorageBackend(storagePath),
                _ => new JsonFileStorageBackend(storagePath)
            };

            // If cluster configuration is provided, wrap with distributed storage
            if (!string.IsNullOrEmpty(clusterId) && seedNodes.Length > 0)
            {
                var clusterConfig = new ClusterConfig(
                    ClusterId: clusterId,
                    SeedNodes: seedNodes,
                    ReplicationFactor: int.Parse(Environment.GetEnvironmentVariable("REPLICATION_FACTOR") ?? "3"),
                    ReadConsistencyLevel: int.Parse(Environment.GetEnvironmentVariable("READ_CONSISTENCY") ?? "1"),
                    WriteConsistencyLevel: int.Parse(Environment.GetEnvironmentVariable("WRITE_CONSISTENCY") ?? "1")
                );
                
                _logger.Info($"Configuring distributed storage with cluster ID: {clusterId}, seed nodes: {string.Join(", ", seedNodes)}");
                return new DistributedStorageBackend(baseBackend, clusterConfig);
            }

            return baseBackend;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to configure storage backend: {ex.Message}", ex);
            // Return a null storage backend - the module will handle this gracefully
            return null!;
        }
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.storage",
            name: "Storage Management Module",
            version: "0.1.0",
            description: "Self-contained module for storage management operations using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Removed manual registrations; now using attributes
    }

    [ApiRoute("GET", "/storage/stats", "storage-stats", "Get storage statistics", "codex.storage")]
    public async Task<object> GetStatsAsync()
    {
        try
        {
            if (_storage == null)
            {
                return new ErrorResponse("No storage backend configured");
            }

            var stats = await _storage.GetStatsAsync();
            return new
            {
                success = true,
                stats = new
                {
                    nodeCount = stats.NodeCount,
                    edgeCount = stats.EdgeCount,
                    totalSizeBytes = stats.TotalSizeBytes,
                    lastUpdated = stats.LastUpdated
                }
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get storage stats: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/storage/sync", "storage-sync", "Sync cache with storage", "codex.storage")]
    public async Task<object> SyncAsync()
    {
        try
        {
            if (_storage == null)
            {
                return new ErrorResponse("No storage backend configured");
            }

            if (_registry is PersistentNodeRegistry persistentRegistry)
            {
                await persistentRegistry.SyncWithStorageAsync();
                return new
                {
                    success = true,
                    message = "Cache synchronized with storage"
                };
            }
            else
            {
                return new ErrorResponse("Registry is not persistent");
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to sync storage: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/storage/health", "storage-health", "Check storage health", "codex.storage")]
    public async Task<object> GetHealthAsync()
    {
        try
        {
            if (_storage == null)
            {
                return new
                {
                    success = false,
                    message = "No storage backend configured",
                    available = false
                };
            }

            var isAvailable = await _storage.IsAvailableAsync();
            var stats = isAvailable ? await _storage.GetStatsAsync() : null;

            return new
            {
                success = true,
                available = isAvailable,
                stats = stats != null ? new
                {
                    nodeCount = stats.NodeCount,
                    edgeCount = stats.EdgeCount,
                    totalSizeBytes = stats.TotalSizeBytes,
                    lastUpdated = stats.LastUpdated
                } : null
            };
        }
        catch (Exception ex)
        {
            return new
            {
                success = false,
                message = $"Storage health check failed: {ex.Message}",
                available = false
            };
        }
    }

    [ApiRoute("GET", "/storage/cache", "storage-cache", "Get cache statistics", "codex.storage")]
    public async Task<object> GetCacheStatsAsync()
    {
        try
        {
            if (_registry is PersistentNodeRegistry persistentRegistry)
            {
                var cacheStats = await persistentRegistry.GetCacheStatsAsync();
                return new
                {
                    success = true,
                    cache = new
                    {
                        iceNodeCount = cacheStats.IceNodeCount,
                        waterNodeCount = cacheStats.WaterNodeCount,
                        gasNodeCount = cacheStats.GasNodeCount,
                        edgeCount = cacheStats.EdgeCount,
                        totalMemoryUsage = cacheStats.TotalMemoryUsage,
                        lastUpdated = cacheStats.LastUpdated
                    }
                };
            }
            else
            {
                return new ErrorResponse("Registry is not persistent");
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cache stats: {ex.Message}");
        }
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Discovery is handled globally
    }
}
