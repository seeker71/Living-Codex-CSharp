using System.Diagnostics;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Health monitoring service that tracks system metrics
/// </summary>
public sealed class HealthService
{
    private readonly NodeRegistry _registry;
    private readonly Core.ILogger _logger;
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
            // Use actual loaded module count from ModuleLoader instead of NodeRegistry count
            var moduleCount = _moduleLoader?.GetLoadedModules().Count ?? 
                             (_registry.GetNodesByType("module").Count() + 
                              _registry.GetNodesByType("codex.module").Count());

            return new HealthStatus(
                Status: "healthy",
                Uptime: uptime,
                RequestCount: _requestCount,
                NodeCount: nodeCount,
                EdgeCount: edgeCount,
                ModuleCount: moduleCount,
                Timestamp: DateTime.UtcNow,
                Version: GetVersion()
            );
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
    string Version
);
