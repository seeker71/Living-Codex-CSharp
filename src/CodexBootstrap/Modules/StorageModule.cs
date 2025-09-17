using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Storage management module that configures its own storage backend
/// </summary>
public sealed class StorageModule : ModuleBase
{
    private readonly IStorageBackend? _storage;

    public override string Name => "Storage Module";
    public override string Description => "Storage management module that configures its own storage backend";
    public override string Version => "1.0.0";

    public StorageModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, IStorageBackend? storage = null) 
        : base(registry, logger)
    {
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
                return new DistributedStorageBackend(baseBackend, clusterConfig, _logger);
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

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.storage",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "get_stats", "storage_operations", "data_management" },
            capabilities: new[] { "storage", "management", "nodes", "edges", "statistics" },
            spec: "codex.spec.storage"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
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

            // Since we unified the registry architecture, all registries now support storage sync
            await _registry.InitializeAsync();
            return new
            {
                success = true,
                message = "Cache synchronized with storage"
            };
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
            var stats = await _registry.GetStatsAsync();
            return new
            {
                success = true,
                cache = new
                {
                    iceNodeCount = stats.IceStats.IceNodeCount,
                    waterNodeCount = stats.WaterStats.WaterNodeCount,
                    gasNodeCount = stats.GasNodeCount,
                    edgeCount = stats.IceStats.EdgeCount,
                    totalMemoryUsage = stats.TotalMemoryUsage,
                    lastUpdated = stats.LastUpdated
                }
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cache stats: {ex.Message}");
        }
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Discovery is handled globally
    }
}
