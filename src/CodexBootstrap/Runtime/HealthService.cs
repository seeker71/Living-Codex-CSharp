using System.Diagnostics;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Health monitoring service that tracks system metrics
/// </summary>
public sealed class HealthService
{
    private readonly INodeRegistry _registry;
    private readonly Core.ICodexLogger _logger;
    private readonly DateTime _startTime;
    private long _requestCount;
    private long _activeRequests;
    private long _dbOperationsInFlight;
    private readonly object _lock = new object();
    private ModuleLoader? _moduleLoader;
    private bool _registryInitialized = false;
    
    // Cached counts to avoid expensive queries on every health check
    private int _cachedNodeCount = 0;
    private int _cachedEdgeCount = 0;
    private int _cachedModuleCount = 0;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(5);

    public HealthService(INodeRegistry registry)
    {
        _registry = registry;
        _logger = new Log4NetLogger(typeof(HealthService));
        _startTime = DateTime.UtcNow;
        _requestCount = 0;
        _activeRequests = 0;
        _dbOperationsInFlight = 0;
    }

    public void SetModuleLoader(ModuleLoader moduleLoader)
    {
        _moduleLoader = moduleLoader;
    }

    /// <summary>
    /// Gets the current system health status
    /// </summary>
    public HealthStatus GetHealthStatus()
    {
        var uptime = DateTime.UtcNow - _startTime;
        int nodeCount = 0;
        int edgeCount = 0;
        int moduleCount = 0;
        long dbOps = 0;
        RegistrationMetrics? registrationMetrics = null;
        
        // Use cached counts if available and not expired
        var now = DateTime.UtcNow;
        var cacheValid = (now - _lastCacheUpdate) < _cacheExpiry;
        
        if (cacheValid && _registryInitialized)
        {
            // Use cached values for fast response
            nodeCount = _cachedNodeCount;
            edgeCount = _cachedEdgeCount;
            moduleCount = _cachedModuleCount;
        }
        else if (_registryInitialized)
        {
            // Update cache (only one thread at a time)
            if (Monitor.TryEnter(_lock, 0))
            {
                try
                {
                    nodeCount = _registry.AllNodes().Count();
                    edgeCount = _registry.AllEdges().Count();
                    moduleCount = _moduleLoader?.GetLoadedModules().Count ?? 
                                 (_registry.GetNodesByType("module").Count() + 
                                  _registry.GetNodesByType("codex.meta/module").Count());
                    
                    // Update cache
                    _cachedNodeCount = nodeCount;
                    _cachedEdgeCount = edgeCount;
                    _cachedModuleCount = moduleCount;
                    _lastCacheUpdate = now;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error updating health cache: {ex.Message}");
                    // Use old cached values
                    nodeCount = _cachedNodeCount;
                    edgeCount = _cachedEdgeCount;
                    moduleCount = _cachedModuleCount;
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
            else
            {
                // Another thread is updating cache, use old values
                nodeCount = _cachedNodeCount;
                edgeCount = _cachedEdgeCount;
                moduleCount = _cachedModuleCount;
            }
        }
        
        // Get DB operations count from registry (fast operation)
        try
        {
            if (_registry is NodeRegistry nr)
            {
                dbOps = nr.GetDbOperationsInFlight();
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error querying DB operations: {ex.Message}");
        }
        
        // Calculate registration metrics only if cache needs update
        if (_registryInitialized && !cacheValid)
        {
            try
            {
                registrationMetrics = CalculateRegistrationMetrics();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error calculating registration metrics: {ex.Message}");
            }
        }
            
        // Determine status
        var status = _registryInitialized 
            ? (registrationMetrics?.ModuleRegistrationSuccessRate >= 0.9 ? "healthy" : "degraded")
            : "initializing";

        return new HealthStatus(
            Status: status,
            Uptime: uptime,
            RequestCount: Interlocked.Read(ref _requestCount),
            ActiveRequests: Interlocked.Read(ref _activeRequests),
            DbOperationsInFlight: dbOps,
            NodeCount: nodeCount,
            EdgeCount: edgeCount,
            ModuleCount: moduleCount,
            Timestamp: DateTime.UtcNow,
            Version: GetVersion(),
            RegistryInitialized: _registryInitialized,
            MemoryUsageMB: GC.GetTotalMemory(false) / 1024 / 1024,
            ThreadCount: System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
            RegistrationMetrics: registrationMetrics
        );
    }

    private RegistrationMetrics CalculateRegistrationMetrics()
    {
        try
        {
            var totalModulesLoaded = _moduleLoader?.GetLoadedModules().Count ?? 0;
            
            // Get actual failure lists from ModuleLoader
            var failedModules = _moduleLoader?.GetFailedModuleLoads().ToList() ?? new List<string>();
            var failedRoutes = _moduleLoader?.GetFailedRouteRegistrations().ToList() ?? new List<string>();
            
            var modulesInNodeRegistry = _registry.GetNodesByType("module").Count() + 
                                      _registry.GetNodesByType("codex.meta/module").Count();
            var modulesWithFailedRegistration = failedModules.Count;
            
            // Count routes by looking at API nodes (both standard and meta API types)
            var totalRoutesRegistered = _registry.GetNodesByType("api").Count() + 
                                      _registry.GetNodesByType("codex.meta/api").Count();
            var routesWithFailedRegistration = failedRoutes.Count;
            
            var moduleSuccessRate = (totalModulesLoaded + modulesWithFailedRegistration) > 0 
                ? (double)totalModulesLoaded / (totalModulesLoaded + modulesWithFailedRegistration) 
                : 1.0;
            
            var routeSuccessRate = (totalRoutesRegistered + routesWithFailedRegistration) > 0 
                ? (double)totalRoutesRegistered / (totalRoutesRegistered + routesWithFailedRegistration) 
                : 1.0;
            
            return new RegistrationMetrics(
                TotalModulesLoaded: totalModulesLoaded,
                ModulesRegisteredInNodeRegistry: modulesInNodeRegistry,
                ModulesWithFailedRegistration: modulesWithFailedRegistration,
                TotalRoutesRegistered: totalRoutesRegistered,
                RoutesWithFailedRegistration: routesWithFailedRegistration,
                ModuleRegistrationSuccessRate: moduleSuccessRate,
                RouteRegistrationSuccessRate: routeSuccessRate,
                FailedModuleRegistrations: failedModules,
                FailedRouteRegistrations: failedRoutes
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calculating registration metrics: {ex.Message}", ex);
            return new RegistrationMetrics(0, 0, 0, 0, 0, 0.0, 0.0, new List<string>(), new List<string>());
        }
    }

    /// <summary>
    /// Increments the request counter
    /// </summary>
    public void IncrementRequestCount()
    {
        lock (_lock)
        {
            _requestCount++;
        }
    }

    /// <summary>
    /// Tracks active request count
    /// </summary>
    public void BeginRequest()
    {
        Interlocked.Increment(ref _activeRequests);
    }

    public void EndRequest()
    {
        Interlocked.Decrement(ref _activeRequests);
    }

    /// <summary>
    /// Tracks DB operations in flight
    /// </summary>
    public void BeginDbOperation()
    {
        Interlocked.Increment(ref _dbOperationsInFlight);
    }

    public void EndDbOperation()
    {
        Interlocked.Decrement(ref _dbOperationsInFlight);
    }

    /// <summary>
    /// Marks registry as initialized
    /// </summary>
    public void MarkRegistryInitialized()
    {
        lock (_lock)
        {
            _registryInitialized = true;
            _logger.Info("Registry marked as initialized in HealthService");
        }
    }

    private string GetVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }
}

/// <summary>
/// Health status response model
/// </summary>
public record HealthStatus(
    string Status,
    TimeSpan Uptime,
    long RequestCount,
    long ActiveRequests,
    long DbOperationsInFlight,
    int NodeCount,
    int EdgeCount,
    int ModuleCount,
    DateTime Timestamp,
    string Version,
    bool RegistryInitialized,
    long MemoryUsageMB,
    int ThreadCount,
    RegistrationMetrics? RegistrationMetrics = null
);

/// <summary>
/// Registration metrics for tracking module and route registration success/failure
/// </summary>
public record RegistrationMetrics(
    int TotalModulesLoaded,
    int ModulesRegisteredInNodeRegistry,
    int ModulesWithFailedRegistration,
    int TotalRoutesRegistered,
    int RoutesWithFailedRegistration,
    double ModuleRegistrationSuccessRate,
    double RouteRegistrationSuccessRate,
    List<string> FailedModuleRegistrations,
    List<string> FailedRouteRegistrations
);
