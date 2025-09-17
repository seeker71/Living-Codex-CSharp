using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// Unified base class for modules that provides default implementations and enhanced attribute processing
/// All modules should inherit from this class to ensure proper architecture
/// </summary>
public abstract class ModuleBase : IModule
{
    protected readonly INodeRegistry _registry;
    protected readonly ICodexLogger _logger;
    protected readonly AttributeProcessor _attributeProcessor;
    protected readonly Dictionary<string, Node> _moduleNodes = new();
    protected IApiRouter? _apiRouter;
    protected CoreApiService? _coreApiService;
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Version { get; }
    
    protected ModuleBase(INodeRegistry registry, ICodexLogger logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _attributeProcessor = new AttributeProcessor(registry);
        
        // Auto-process this module's attributes
        ProcessModuleAttributes();
    }
    
    /// <summary>
    /// Process this module's attributes and generate nodes
    /// </summary>
    protected virtual void ProcessModuleAttributes()
    {
        _attributeProcessor.ProcessModule(GetType());
        
        // Store generated nodes for this module
        var generatedNodes = _attributeProcessor.GetGeneratedNodes();
        foreach (var node in generatedNodes.Values)
        {
            _moduleNodes[node.Id] = node;
        }
    }
    
    /// <summary>
    /// Create a module node with standard structure using the generic registration method
    /// </summary>
    protected virtual Node CreateModuleNode(string moduleId, string name, string version, string description, 
        string[]? tags = null, string[]? capabilities = null, string? spec = null)
    {
        return new Node(
            Id: moduleId,
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en-US",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = moduleId,
                    Name = name,
                    Version = version,
                    Description = description,
                    Tags = tags ?? Array.Empty<string>(),
                    Capabilities = capabilities ?? Array.Empty<string>(),
                    Spec = spec
                }),
                InlineBytes: null,
                ExternalUri: null,
                AuthRef: null,
                CacheKey: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["name"] = name,
                ["version"] = version,
                ["description"] = description,
                ["tags"] = tags ?? Array.Empty<string>(),
                ["capabilities"] = capabilities ?? Array.Empty<string>(),
                ["spec"] = spec,
                ["createdAt"] = DateTime.UtcNow
            }
        );
    }
    
    public abstract Node GetModuleNode();
    
    public virtual void Register(INodeRegistry registry)
    {
        var moduleNode = GetModuleNode();
        _moduleNodes[moduleNode.Id] = moduleNode;
        
        // Register all generated nodes (including the module node)
        foreach (var node in _moduleNodes.Values)
        {
            registry.Upsert(node);
        }
        
        _logger.Info($"Registered module: {Name} v{Version} with {_moduleNodes.Count} nodes");
    }
    
    public virtual void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // Store the router for use by modules
        _apiRouter = router;
        
        // Default implementation - modules can override to register API handlers
        _logger.Info($"API handlers registered for module: {Name}");
    }
    
    public virtual void RegisterHttpEndpoints(WebApplication app, INodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store the CoreApiService for use by modules
        _coreApiService = coreApi;
        
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
