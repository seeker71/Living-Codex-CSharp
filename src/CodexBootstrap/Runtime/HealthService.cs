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
        lock (_lock)
        {
            var uptime = DateTime.UtcNow - _startTime;
            int nodeCount = 0;
            int edgeCount = 0;
            int moduleCount = 0;
            long dbOps = 0;
            
            // Only query registry if initialized to avoid blocking
            try
            {
                if (_registryInitialized)
                {
                    nodeCount = _registry.AllNodes().Count();
                    edgeCount = _registry.AllEdges().Count();
                    moduleCount = _moduleLoader?.GetLoadedModules().Count ?? 
                                 (_registry.GetNodesByType("module").Count() + 
                                  _registry.GetNodesByType("codex.meta/module").Count());
                }
                
                // Get DB operations count from registry
                if (_registry is NodeRegistry nr)
                {
                    dbOps = nr.GetDbOperationsInFlight();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error querying registry in health check: {ex.Message}");
            }
            
            // Calculate registration metrics only if registry is initialized
            var registrationMetrics = _registryInitialized ? CalculateRegistrationMetrics() : null;
            
            // Determine status
            var status = _registryInitialized 
                ? (registrationMetrics?.ModuleRegistrationSuccessRate >= 0.9 ? "healthy" : "degraded")
                : "initializing";

            return new HealthStatus(
                Status: status,
                Uptime: uptime,
                RequestCount: _requestCount,
                ActiveRequests: _activeRequests,
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
