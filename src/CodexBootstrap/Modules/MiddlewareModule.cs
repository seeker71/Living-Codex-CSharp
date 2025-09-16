using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Middleware;

namespace CodexBootstrap.Modules;

/// <summary>
/// Middleware module - handles cross-cutting concerns like performance monitoring
/// </summary>
[ApiModule(Name = "MiddlewareModule", Version = "0.1.0", Description = "Cross-cutting middleware concerns", Tags = new[] { "middleware", "performance", "monitoring" })]
public sealed class MiddlewareModule : ModuleBase
{
    private readonly PerformanceProfiler _profiler;

    public override string Name => "Middleware Module";
    public override string Description => "Cross-cutting middleware concerns like performance monitoring";
    public override string Version => "0.1.0";

    public MiddlewareModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, PerformanceProfiler? profiler = null) 
        : base(registry, logger)
    {
        _profiler = profiler ?? new PerformanceProfiler(logger);
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.middleware",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "middleware", "performance", "monitoring" },
            capabilities: new[] { "performance-monitoring", "middleware", "cross-cutting" },
            spec: "codex.spec.middleware"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are now registered via attribute-based routing
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Register security middleware first (highest priority)
        // TODO: Fix security middleware SQL injection detection for legitimate endpoints
        // app.UseMiddleware<SecurityMiddleware>();
        
        // Register performance monitoring middleware
        app.UseMiddleware<PerformanceMiddleware>();
        
        _logger.Info("Middleware registered: PerformanceMiddleware (SecurityMiddleware temporarily disabled)");
    }
}
