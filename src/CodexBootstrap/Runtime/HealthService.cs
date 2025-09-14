using System.Diagnostics;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Health monitoring service that tracks system metrics
/// </summary>
public sealed class HealthService
{
    private readonly NodeRegistry _registry;
    private readonly Core.ICodexLogger _logger;
    private readonly DateTime _startTime;
    private long _requestCount;
    private readonly object _lock = new object();
    private ModuleLoader? _moduleLoader;

    public HealthService(NodeRegistry registry)
    {
        _registry = registry;
        _logger = new Log4NetLogger(typeof(HealthService));
        _startTime = DateTime.UtcNow;
        _requestCount = 0;
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
            var nodeCount = _registry.AllNodes().Count();
            var edgeCount = _registry.AllEdges().Count();
            
            // Calculate registration metrics
            var registrationMetrics = CalculateRegistrationMetrics();
            
            // Use actual loaded module count from ModuleLoader instead of NodeRegistry count
            var moduleCount = _moduleLoader?.GetLoadedModules().Count ?? 
                             (_registry.GetNodesByType("module").Count() + 
                              _registry.GetNodesByType("codex.module").Count());

            return new HealthStatus(
                Status: registrationMetrics.ModuleRegistrationSuccessRate >= 0.9 ? "healthy" : "degraded",
                Uptime: uptime,
                RequestCount: _requestCount,
                NodeCount: nodeCount,
                EdgeCount: edgeCount,
                ModuleCount: moduleCount,
                Timestamp: DateTime.UtcNow,
                Version: GetVersion(),
                RegistrationMetrics: registrationMetrics
            );
        }
    }

    private RegistrationMetrics CalculateRegistrationMetrics()
    {
        try
        {
            var totalModulesLoaded = _moduleLoader?.GetLoadedModules().Count ?? 0;
            var modulesInNodeRegistry = _registry.GetNodesByType("module").Count() + 
                                      _registry.GetNodesByType("codex.meta/module").Count();
            var modulesWithFailedRegistration = Math.Max(0, totalModulesLoaded - modulesInNodeRegistry);
            
            // Count routes by looking at API nodes (both standard and meta API types)
            var totalRoutesRegistered = _registry.GetNodesByType("api").Count() + 
                                      _registry.GetNodesByType("codex.meta/api").Count();
            var routesWithFailedRegistration = 0; // This would need to be tracked separately
            
            var moduleSuccessRate = totalModulesLoaded > 0 ? (double)modulesInNodeRegistry / totalModulesLoaded : 1.0;
            var routeSuccessRate = 1.0; // This would need to be calculated based on actual route registration tracking
            
            return new RegistrationMetrics(
                TotalModulesLoaded: totalModulesLoaded,
                ModulesRegisteredInNodeRegistry: modulesInNodeRegistry,
                ModulesWithFailedRegistration: modulesWithFailedRegistration,
                TotalRoutesRegistered: totalRoutesRegistered,
                RoutesWithFailedRegistration: routesWithFailedRegistration,
                ModuleRegistrationSuccessRate: moduleSuccessRate,
                RouteRegistrationSuccessRate: routeSuccessRate,
                FailedModuleRegistrations: new List<string>(), // Would be populated from actual failure tracking
                FailedRouteRegistrations: new List<string>()   // Would be populated from actual failure tracking
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
    int NodeCount,
    int EdgeCount,
    int ModuleCount,
    DateTime Timestamp,
    string Version,
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
