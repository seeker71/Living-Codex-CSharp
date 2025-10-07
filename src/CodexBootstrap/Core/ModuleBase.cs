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
    protected object? _realtimeModule; // Will be set during inter-module communication setup
    protected IServiceProvider? _serviceProvider; // Will be set during inter-module communication setup
    
    // Readiness tracking
    private ReadinessState _currentReadinessState = ReadinessState.NotStarted;
    private ReadinessResult _lastReadinessResult = new();
    private readonly List<string> _providedEndpoints = new();
    
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Version { get; }
    
    // IModule readiness implementation
    public ReadinessState CurrentReadinessState => _currentReadinessState;
    public event EventHandler<ReadinessChangedEventArgs>? ReadinessChanged;
    
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
        if (router == null)
        {
            throw new ArgumentNullException(nameof(router));
        }
        // Store the router for use by modules
        _apiRouter = router;
        
        // Default implementation - modules can override to register API handlers
        _logger.Info($"ModuleBase.RegisterApiHandlers called for module: {Name} (type: {GetType().Name})");
    }
    
    public virtual void RegisterHttpEndpoints(WebApplication app, INodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store the CoreApiService for use by modules
        _coreApiService = coreApi;
        
        // Default implementation - modules can override to register HTTP endpoints
        _logger.Info($"HTTP endpoints registered for module: {Name}");
        
        // Auto-register common endpoints for this module
        AutoRegisterCommonEndpoints();
    }

    /// <summary>
    /// Auto-register common endpoints for this module
    /// Override this method to register specific endpoints
    /// </summary>
    protected virtual void AutoRegisterCommonEndpoints()
    {
        // Register common API patterns for this module
        var moduleName = Name.ToLowerInvariant().Replace("module", "");
        
        // Common endpoint patterns
        AddProvidedEndpoint($"/api/{moduleName}");
        AddProvidedEndpoint($"/api/{moduleName}/health");
        AddProvidedEndpoint($"/api/{moduleName}/status");
        
        // Add module-specific endpoints if any
        RegisterModuleSpecificEndpoints();
    }

    /// <summary>
    /// Override this method to register module-specific endpoints
    /// </summary>
    protected virtual void RegisterModuleSpecificEndpoints()
    {
        // Default implementation - modules can override to add specific endpoints
    }
    
    public virtual async Task InitializeAsync()
    {
        // Default implementation - modules can override to perform async initialization
        _logger.Info($"Async initialization for module: {Name}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Wait for registry initialization to complete before proceeding
    /// Use this in modules that need to access nodes during async initialization
    /// </summary>
    protected async Task WaitForRegistryInitializationAsync()
    {
        _logger.Info($"Module {Name} waiting for registry initialization to complete...");
        await _registry.WaitForInitializationAsync();
        _logger.Info($"Module {Name} registry initialization complete, proceeding with async initialization");
    }

    /// <summary>
    /// Update the module's readiness state
    /// </summary>
    protected void UpdateReadinessState(ReadinessResult result)
    {
        var previousState = _currentReadinessState;
        _currentReadinessState = result.State;
        _lastReadinessResult = result;

        _logger.Info($"Module {Name} readiness state changed: {previousState} -> {result.State} ({result.Message})");

        ReadinessChanged?.Invoke(this, new ReadinessChangedEventArgs
        {
            ComponentId = Name,
            PreviousState = previousState,
            CurrentState = result.State,
            Result = result
        });
    }

    /// <summary>
    /// Mark the module as initializing
    /// </summary>
    protected void MarkAsInitializing(string message = "Initializing")
    {
        UpdateReadinessState(ReadinessResult.Initializing(message));
    }

    /// <summary>
    /// Mark the module as ready
    /// </summary>
    protected void MarkAsReady(string message = "Ready")
    {
        UpdateReadinessState(ReadinessResult.Success(message));
    }

    /// <summary>
    /// Mark the module as failed
    /// </summary>
    protected void MarkAsFailed(string message, Exception? exception = null)
    {
        UpdateReadinessState(ReadinessResult.Failed(message, exception));
    }

    /// <summary>
    /// Mark the module as degraded
    /// </summary>
    protected void MarkAsDegraded(string message)
    {
        UpdateReadinessState(ReadinessResult.Degraded(message));
    }

    /// <summary>
    /// Add an endpoint provided by this module
    /// </summary>
    protected void AddProvidedEndpoint(string endpoint)
    {
        if (!_providedEndpoints.Contains(endpoint))
        {
            _providedEndpoints.Add(endpoint);
        }
    }

    // IModule interface implementation
    public ReadinessResult GetReadinessResult()
    {
        return _lastReadinessResult;
    }

    public IEnumerable<string> GetProvidedEndpoints()
    {
        return _providedEndpoints.AsReadOnly();
    }

    public virtual void SetupInterModuleCommunication(IServiceProvider services)
    {
        // Store the service provider for modules to access services
        _serviceProvider = services;
        
        // Try to get RealtimeModule from services for modules that need it
        _realtimeModule = services.GetService(Type.GetType("CodexBootstrap.Modules.RealtimeModule"));
        
        // Default implementation - modules can override to setup inter-module communication
        _logger.Info($"Inter-module communication setup for module: {Name}");
    }
    
    public virtual void Unregister()
    {
        // Default implementation - modules can override to clean up resources
        _logger.Info($"Unregistered module: {Name}");
    }
}
