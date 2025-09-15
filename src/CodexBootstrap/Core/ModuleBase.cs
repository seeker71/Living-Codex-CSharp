using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// Base class for modules that provides default implementations
/// All modules should inherit from this class to ensure proper architecture
/// </summary>
public abstract class ModuleBase : IModule
{
    protected readonly NodeRegistry _registry;
    protected readonly ICodexLogger _logger;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Version { get; }
    
    protected ModuleBase(NodeRegistry registry, ICodexLogger logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public abstract Node GetModuleNode();
    
    public virtual void Register(NodeRegistry registry)
    {
        var moduleNode = GetModuleNode();
        registry.Upsert(moduleNode);
        _logger.Info($"Registered module: {Name} v{Version}");
    }
    
    public virtual void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Default implementation - modules can override to register API handlers
        _logger.Info($"API handlers registered for module: {Name}");
    }
    
    public virtual void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Default implementation - modules can override to register HTTP endpoints
        _logger.Info($"HTTP endpoints registered for module: {Name}");
    }
    
    public virtual void SetupInterModuleCommunication(IServiceProvider services)
    {
        // Default implementation - modules can override to setup inter-module communication
        _logger.Info($"Inter-module communication setup for module: {Name}");
    }
    
    public virtual void Unregister()
    {
        // Default implementation - modules can override to clean up resources
        _logger.Info($"Unregistered module: {Name}");
    }
}
