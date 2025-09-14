using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Middleware;

namespace CodexBootstrap.Modules;

/// <summary>
/// Middleware module - handles cross-cutting concerns like performance monitoring
/// </summary>
[ApiModule(Name = "MiddlewareModule", Version = "0.1.0", Description = "Cross-cutting middleware concerns", Tags = new[] { "middleware", "performance", "monitoring" })]
public sealed class MiddlewareModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly PerformanceProfiler _profiler;
    private readonly CodexBootstrap.Core.ICodexLogger _logger;

    public MiddlewareModule(NodeRegistry registry, PerformanceProfiler profiler, CodexBootstrap.Core.ICodexLogger logger)
    {
        _registry = registry;
        _profiler = profiler;
        _logger = logger;
    }

    public string ModuleId => "codex.middleware";
    public string Name => "Middleware Module";
    public string Version => "0.1.0";
    public string Description => "Cross-cutting middleware concerns like performance monitoring.";

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: ModuleId,
            name: Name,
            version: Version,
            description: Description,
            capabilities: new[] { "performance-monitoring", "middleware", "cross-cutting" },
            tags: new[] { "middleware", "performance", "monitoring" },
            specReference: "codex.spec.middleware"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered via attribute-based routing
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Register security middleware first (highest priority)
        app.UseMiddleware<SecurityMiddleware>();
        
        // Register performance monitoring middleware
        app.UseMiddleware<PerformanceMiddleware>();
        
        _logger.Info("Middleware registered: SecurityMiddleware, PerformanceMiddleware");
    }
}
