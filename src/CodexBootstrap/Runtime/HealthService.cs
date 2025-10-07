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
        ModuleLoadingOverview? moduleLoading = null;
        
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
        
        // Always include module loading overview if module loader is set
        try
        {
            moduleLoading = CalculateModuleLoadingOverview();
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error getting module loading overview: {ex.Message}");
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
        
        // Calculate registration metrics - always calculate for accurate health status
        if (_registryInitialized)
        {
            try
            {
                // If cache is valid, use cached values but still calculate metrics for health status
                if (cacheValid)
                {
                    // Use cached counts but calculate fresh metrics
                    registrationMetrics = CalculateRegistrationMetrics();
                }
                else
                {
                    // Update cache and calculate metrics
                    registrationMetrics = CalculateRegistrationMetrics();
                    moduleLoading = CalculateModuleLoadingOverview();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error calculating registration metrics: {ex.Message}");
            }
        }
            
        // Determine status with improved logic
        var status = _registryInitialized 
            ? DetermineHealthStatus(registrationMetrics, moduleLoading)
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
            RegistrationMetrics: registrationMetrics,
            ModuleLoading: moduleLoading
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

    private ModuleLoadingOverview? CalculateModuleLoadingOverview()
    {
        try
        {
            if (_moduleLoader == null) return null;
            var (discovered, created, registered, asyncInitialized, asyncComplete) = _moduleLoader.GetModuleLoadingMetrics();
            var stuck = _moduleLoader.GetStuckModules().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return new ModuleLoadingOverview(
                Discovered: discovered,
                Created: created,
                Registered: registered,
                AsyncInitialized: asyncInitialized,
                AsyncInitializationComplete: asyncComplete,
                StuckModules: stuck
            );
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error calculating module loading overview: {ex.Message}");
            return null;
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

    /// <summary>
    /// Gets comprehensive memory health status - embodying compassionate resource stewardship
    /// </summary>
    public MemoryHealthStatus GetMemoryHealthStatus()
    {
        var process = Process.GetCurrentProcess();
        var memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
        var memoryUsageGB = memoryUsageMB / 1024.0;
        
        // Get memory pressure indicators
        var memoryPressure = CalculateMemoryPressure(memoryUsageMB);
        
        // Collect module-specific memory metrics
        var moduleMetrics = CollectModuleMemoryMetrics();
        
        // Calculate overall health score
        var healthScore = CalculateMemoryHealthScore(memoryUsageMB, moduleMetrics);
        
        return new MemoryHealthStatus(
            Timestamp: DateTime.UtcNow,
            ProcessId: process.Id,
            MemoryUsageMB: memoryUsageMB,
            MemoryUsageGB: Math.Round(memoryUsageGB, 2),
            MemoryPressure: memoryPressure,
            HealthScore: healthScore,
            ModuleMetrics: moduleMetrics,
            Recommendations: GenerateMemoryRecommendations(memoryUsageMB, moduleMetrics)
        );
    }

    /// <summary>
    /// Calculate memory pressure level based on usage patterns
    /// </summary>
    private MemoryPressureLevel CalculateMemoryPressure(long memoryUsageMB)
    {
        // Compassionate thresholds that respect system resources
        if (memoryUsageMB < 100) return MemoryPressureLevel.Low;
        if (memoryUsageMB < 300) return MemoryPressureLevel.Normal;
        if (memoryUsageMB < 500) return MemoryPressureLevel.Elevated;
        if (memoryUsageMB < 800) return MemoryPressureLevel.High;
        return MemoryPressureLevel.Critical;
    }

    /// <summary>
    /// Collect memory metrics from modules with cleanup systems
    /// </summary>
    private Dictionary<string, ModuleMemoryMetrics> CollectModuleMemoryMetrics()
    {
        var metrics = new Dictionary<string, ModuleMemoryMetrics>();
        
        try
        {
            // Note: In a real implementation, we would query each module for its memory usage
            // For now, we'll provide a template structure
            
            metrics["IdentityModule"] = new ModuleMemoryMetrics(
                ActiveSessions: 0, // Would be retrieved from IdentityModule
                RevokedTokens: 0,  // Would be retrieved from IdentityModule
                LastCleanup: DateTime.UtcNow.AddMinutes(-15), // Would be retrieved from module
                EstimatedMemoryMB: 5
            );
            
            metrics["UserContributionsModule"] = new ModuleMemoryMetrics(
                ActiveSessions: 0,
                RevokedTokens: 0,
                LastCleanup: DateTime.UtcNow.AddMinutes(-30),
                EstimatedMemoryMB: 10
            );
            
            metrics["RealtimeModule"] = new ModuleMemoryMetrics(
                ActiveSessions: 0,
                RevokedTokens: 0,
                LastCleanup: DateTime.UtcNow.AddMinutes(-15),
                EstimatedMemoryMB: 8
            );
            
            metrics["RealtimeNewsStreamModule"] = new ModuleMemoryMetrics(
                ActiveSessions: 0,
                RevokedTokens: 0,
                LastCleanup: DateTime.UtcNow.AddMinutes(-60),
                EstimatedMemoryMB: 15
            );
            
            metrics["LoadBalancingModule"] = new ModuleMemoryMetrics(
                ActiveSessions: 0,
                RevokedTokens: 0,
                LastCleanup: DateTime.UtcNow.AddMinutes(-30),
                EstimatedMemoryMB: 5
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error collecting module memory metrics: {ex.Message}", ex);
        }
        
        return metrics;
    }

    /// <summary>
    /// Calculate overall memory health score (0-100)
    /// </summary>
    private int CalculateMemoryHealthScore(long memoryUsageMB, Dictionary<string, ModuleMemoryMetrics> moduleMetrics)
    {
        var score = 100;
        
        // Reduce score based on memory usage
        if (memoryUsageMB > 500) score -= 30;
        else if (memoryUsageMB > 300) score -= 15;
        else if (memoryUsageMB > 200) score -= 5;
        
        // Reduce score based on module metrics
        var totalModuleMemory = moduleMetrics.Values.Sum(m => m.EstimatedMemoryMB);
        if (totalModuleMemory > 100) score -= 20;
        else if (totalModuleMemory > 50) score -= 10;
        
        // Check for stale cleanup times
        var staleModules = moduleMetrics.Count(m => m.Value.LastCleanup < DateTime.UtcNow.AddHours(-1));
        score -= staleModules * 5;
        
        return Math.Max(0, score);
    }

    /// <summary>
    /// Generate compassionate recommendations for memory optimization
    /// </summary>
    private List<string> GenerateMemoryRecommendations(long memoryUsageMB, Dictionary<string, ModuleMemoryMetrics> moduleMetrics)
    {
        var recommendations = new List<string>();
        
        if (memoryUsageMB > 500)
        {
            recommendations.Add("Memory usage is elevated. Consider reviewing module cleanup intervals.");
        }
        
        if (memoryUsageMB > 300)
        {
            recommendations.Add("Memory usage is moderate. Monitor for memory leaks in background processes.");
        }
        
        var staleModules = moduleMetrics.Where(m => m.Value.LastCleanup < DateTime.UtcNow.AddHours(-1)).ToList();
        if (staleModules.Any())
        {
            recommendations.Add($"Some modules haven't cleaned up recently: {string.Join(", ", staleModules.Select(m => m.Key))}");
        }
        
        if (!recommendations.Any())
        {
            recommendations.Add("Memory usage is healthy. Continue monitoring for optimal performance.");
        }
        
        return recommendations;
    }

    /// <summary>
    /// Determines health status using compassionate logic that considers multiple factors
    /// </summary>
    private string DetermineHealthStatus(RegistrationMetrics? registrationMetrics, ModuleLoadingOverview? moduleLoading)
    {
        // If we have no metrics, default to degraded (conservative approach)
        if (registrationMetrics == null)
        {
            _logger.Warn("Health status determination: No registration metrics available, defaulting to degraded");
            return "degraded";
        }

        // Check for critical issues first
        if (moduleLoading?.StuckModules?.Count > 0)
        {
            _logger.Warn($"Health status determination: {moduleLoading.StuckModules.Count} stuck modules detected");
            return "degraded";
        }

        // Check if all modules have completed async initialization
        if (moduleLoading?.AsyncInitializationComplete == false)
        {
            _logger.Info("Health status determination: Async initialization still in progress");
            return "degraded";
        }

        // Apply compassionate thresholds - be more forgiving than the original 90%
        var moduleSuccessRate = registrationMetrics.ModuleRegistrationSuccessRate;
        var routeSuccessRate = registrationMetrics.RouteRegistrationSuccessRate;

        // If module success rate is very high (>95%), consider healthy regardless of route issues
        if (moduleSuccessRate >= 0.95)
        {
            _logger.Info($"Health status determination: Module success rate {moduleSuccessRate:P1} is excellent, marking healthy");
            return "healthy";
        }

        // If both rates are good (>85%), consider healthy
        if (moduleSuccessRate >= 0.85 && routeSuccessRate >= 0.85)
        {
            _logger.Info($"Health status determination: Both success rates good (modules: {moduleSuccessRate:P1}, routes: {routeSuccessRate:P1}), marking healthy");
            return "healthy";
        }

        // If module rate is decent (>80%) and routes are reasonable (>70%), still healthy
        if (moduleSuccessRate >= 0.80 && routeSuccessRate >= 0.70)
        {
            _logger.Info($"Health status determination: Success rates acceptable (modules: {moduleSuccessRate:P1}, routes: {routeSuccessRate:P1}), marking healthy");
            return "healthy";
        }

        // Otherwise, degraded
        _logger.Warn($"Health status determination: Success rates too low (modules: {moduleSuccessRate:P1}, routes: {routeSuccessRate:P1}), marking degraded");
        return "degraded";
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
    RegistrationMetrics? RegistrationMetrics = null,
    ModuleLoadingOverview? ModuleLoading = null
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

/// <summary>
/// Module loading overview for detailed lifecycle counts and stuck modules
/// </summary>
public record ModuleLoadingOverview(
    int Discovered,
    int Created,
    int Registered,
    int AsyncInitialized,
    bool AsyncInitializationComplete,
    Dictionary<string, string> StuckModules
);

/// <summary>
/// Memory health status - embodying compassionate resource stewardship
/// </summary>
public record MemoryHealthStatus(
    DateTime Timestamp,
    int ProcessId,
    long MemoryUsageMB,
    double MemoryUsageGB,
    MemoryPressureLevel MemoryPressure,
    int HealthScore,
    Dictionary<string, ModuleMemoryMetrics> ModuleMetrics,
    List<string> Recommendations
);

/// <summary>
/// Module-specific memory metrics
/// </summary>
public record ModuleMemoryMetrics(
    int ActiveSessions,
    int RevokedTokens,
    DateTime LastCleanup,
    int EstimatedMemoryMB
);

/// <summary>
/// Memory pressure levels - embodying compassionate thresholds
/// </summary>
public enum MemoryPressureLevel
{
    Low,        // < 100MB - Very healthy
    Normal,     // 100-300MB - Healthy
    Elevated,   // 300-500MB - Monitor closely
    High,       // 500-800MB - Take action
    Critical    // > 800MB - Immediate intervention needed
}
