# Node Registry Architecture Redesign

## Current Issues Identified

### 1. **Multiple Registry Instances**
- Modules are creating local `NodeRegistry` instances
- Empty constructors bypass proper dependency injection
- Registry isolation prevents data sharing between modules
- Violates the "single source of truth" principle

### 2. **Inconsistent Node State Management**
- No clear distinction between Ice/Water/Gas node states
- Missing persistence strategy for different node types
- No proper lifecycle management for transient nodes

### 3. **Module Construction Anti-Patterns**
- Empty constructors create modules with local registries
- No proper dependency injection for core services
- Inter-module communication setup is ad-hoc

## Architectural Principles

### 1. **Single Global Registry**
- **One Registry Instance**: Only one `NodeRegistry` instance in the entire system
- **No Local Registries**: All modules must use the global registry
- **Registry Injection**: All modules receive the registry via constructor injection

### 2. **Node State Lifecycle Management**
- **Ice (Frozen)**: Persistent, immutable nodes (specs, schemas, core data)
- **Water (Fluid)**: Semi-persistent nodes (cache, generated code, derived data)
- **Gas (Transient)**: In-memory only nodes (temporary data, sessions, calculations)

### 3. **Module Construction Pattern**
- **Required Dependencies**: All modules must receive `NodeRegistry` and `ILogger` via constructor
- **No Empty Constructors**: Eliminate parameterless constructors entirely
- **Two-Phase Initialization**: Construction + Inter-module setup phase

## Redesign Plan

### Phase 1: Registry Architecture Cleanup

#### 1.1 Remove Local Registry Pattern
```csharp
// ❌ Current Anti-Pattern
public class SomeModule : IModule
{
    private readonly NodeRegistry _localRegistry;
    
    public SomeModule() : this(new NodeRegistry(), new Log4NetLogger()) { }
}

// ✅ New Pattern
public class SomeModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly ILogger _logger;
    
    public SomeModule(NodeRegistry registry, ILogger logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

#### 1.2 Registry State Management
```csharp
public enum NodeState
{
    Ice,    // Persistent, immutable
    Water,  // Semi-persistent, mutable
    Gas     // Transient, in-memory only
}

public class NodeRegistry
{
    // Ice nodes - persisted to storage
    private readonly Dictionary<string, Node> _iceNodes;
    
    // Water nodes - cached, can be regenerated
    private readonly Dictionary<string, Node> _waterNodes;
    
    // Gas nodes - transient, not persisted
    private readonly Dictionary<string, Node> _gasNodes;
    
    public void Upsert(Node node)
    {
        switch (node.State)
        {
            case NodeState.Ice:
                _iceNodes[node.Id] = node;
                PersistNode(node); // Save to storage
                break;
            case NodeState.Water:
                _waterNodes[node.Id] = node;
                CacheNode(node); // Cache for performance
                break;
            case NodeState.Gas:
                _gasNodes[node.Id] = node;
                // No persistence
                break;
        }
    }
}
```

### Phase 2: Module Construction Redesign

#### 2.1 Mandatory Constructor Injection
```csharp
public interface IModule
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    
    // Required dependencies - no empty constructors allowed
    void Initialize(NodeRegistry registry, ILogger logger);
    
    // Optional inter-module setup
    void SetupInterModuleCommunication(IServiceProvider services);
    
    Node GetModuleNode();
    void Register(NodeRegistry registry);
    void RegisterApiHandlers(IApiRouter router, NodeRegistry registry);
    void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader);
    void Unregister();
}
```

#### 2.2 Module Loader Redesign
```csharp
public class ModuleLoader
{
    private readonly NodeRegistry _globalRegistry;
    private readonly IServiceProvider _serviceProvider;
    
    public void LoadModules()
    {
        // Phase 1: Create all modules with required dependencies
        var modules = DiscoverModules()
            .Select(type => CreateModule(type, _globalRegistry))
            .ToList();
            
        // Phase 2: Setup inter-module communication
        foreach (var module in modules)
        {
            module.SetupInterModuleCommunication(_serviceProvider);
        }
        
        // Phase 3: Register modules
        foreach (var module in modules)
        {
            module.Register(_globalRegistry);
        }
    }
    
    private IModule CreateModule(Type moduleType, NodeRegistry registry)
    {
        // All modules must have constructor with registry and logger
        var constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(ILogger) });
        if (constructor == null)
        {
            throw new InvalidOperationException($"Module {moduleType.Name} must have constructor(NodeRegistry, ILogger)");
        }
        
        var logger = _serviceProvider.GetRequiredService<ILogger>();
        return (IModule)constructor.Invoke(new object[] { registry, logger });
    }
}
```

### Phase 3: Portal Module Architecture

#### 3.1 Portal Module Concept
```csharp
public class PortalModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IPortal> _portals;
    
    public PortalModule(NodeRegistry registry, ILogger logger)
    {
        _registry = registry;
        _logger = logger;
        _portals = new Dictionary<string, IPortal>();
    }
    
    public void SetupInterModuleCommunication(IServiceProvider services)
    {
        // Register different portal types
        _portals["news"] = new NewsPortal(_registry, _logger);
        _portals["search"] = new SearchPortal(_registry, _logger);
        _portals["external-api"] = new ExternalApiPortal(_registry, _logger);
        _portals["world-sensors"] = new WorldSensorsPortal(_registry, _logger);
    }
}

public interface IPortal
{
    string PortalType { get; }
    Task<PortalResponse> QueryAsync(PortalQuery query);
    Task<bool> IsHealthyAsync();
}
```

#### 3.2 News Portal Implementation
```csharp
public class NewsPortal : IPortal
{
    private readonly NodeRegistry _registry;
    private readonly ILogger _logger;
    
    public string PortalType => "news";
    
    public async Task<PortalResponse> QueryAsync(PortalQuery query)
    {
        // News-specific query logic
        var newsNodes = _registry.GetNodesByType("codex.news.item")
            .Where(n => n.State == NodeState.Water) // Only semi-persistent news
            .ToList();
            
        return new PortalResponse
        {
            Data = newsNodes,
            Metadata = new { count = newsNodes.Count, source = "news-portal" }
        };
    }
}
```

### Phase 4: Implementation Steps

#### 4.1 Registry Cleanup (Week 1)
- [ ] Remove all local registry instances
- [ ] Update all modules to use constructor injection
- [ ] Implement proper node state management
- [ ] Add persistence strategy for Ice nodes
- [ ] Add caching strategy for Water nodes

#### 4.2 Module Construction Redesign (Week 2)
- [ ] Update IModule interface
- [ ] Remove all empty constructors
- [ ] Implement two-phase initialization
- [ ] Update ModuleLoader
- [ ] Add inter-module communication setup

#### 4.3 Portal Module Implementation (Week 3)
- [ ] Create PortalModule base class
- [ ] Implement NewsPortal
- [ ] Implement SearchPortal
- [ ] Implement ExternalApiPortal
- [ ] Implement WorldSensorsPortal

#### 4.4 News Module Consolidation (Week 4)
- [ ] Merge RealtimeNewsStreamModule into NewsPortal
- [ ] Merge NewsFeedModule into NewsPortal
- [ ] Consolidate all news endpoints
- [x] Update mobile app to use current NewsFeedModule endpoints (temporary mapping)
- [x] Add tests for NewsFeed endpoints (trending, search, read/unread, feed)

### Phase 5: Testing and Validation

#### 5.1 Registry Tests
```csharp
[Test]
public void Registry_ShouldMaintainSingleInstance()
{
    var registry1 = new NodeRegistry();
    var registry2 = new NodeRegistry();
    
    // Should be the same instance or properly shared
    Assert.AreSame(registry1, registry2);
}

[Test]
public void NodeStates_ShouldBeHandledCorrectly()
{
    var iceNode = new Node { State = NodeState.Ice };
    var waterNode = new Node { State = NodeState.Water };
    var gasNode = new Node { State = NodeState.Gas };
    
    registry.Upsert(iceNode);
    registry.Upsert(waterNode);
    registry.Upsert(gasNode);
    
    // Ice nodes should be persisted
    Assert.IsTrue(IsPersisted(iceNode.Id));
    
    // Water nodes should be cached
    Assert.IsTrue(IsCached(waterNode.Id));
    
    // Gas nodes should be in-memory only
    Assert.IsTrue(IsInMemory(gasNode.Id));
}
```

#### 5.2 Module Construction Tests
```csharp
[Test]
public void Module_ShouldRequireConstructorInjection()
{
    var moduleType = typeof(SomeModule);
    var constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(ILogger) });
    
    Assert.IsNotNull(constructor, "Module must have constructor(NodeRegistry, ILogger)");
}

[Test]
public void Module_ShouldNotHaveEmptyConstructor()
{
    var moduleType = typeof(SomeModule);
    var emptyConstructor = moduleType.GetConstructor(Type.EmptyTypes);
    
    Assert.IsNull(emptyConstructor, "Module should not have empty constructor");
}
```

## Expected Outcomes

### Before Redesign:
- ❌ Multiple registry instances causing data isolation
- ❌ Inconsistent node state management
- ❌ Empty constructors bypassing dependency injection
- ❌ Ad-hoc inter-module communication
- ❌ Fragmented news functionality across modules

### After Redesign:
- ✅ Single global registry with proper state management
- ✅ Consistent Ice/Water/Gas node lifecycle
- ✅ Mandatory constructor injection for all modules
- ✅ Proper inter-module communication setup
- ✅ Unified portal architecture for external data access
- ✅ Consolidated news functionality in NewsPortal

## Success Metrics

1. **Registry Consistency**: All modules use the same registry instance
2. **Node State Management**: Proper Ice/Water/Gas lifecycle
3. **Module Construction**: No empty constructors, proper dependency injection
4. **Portal Architecture**: Unified external data access
5. **News Consolidation**: Single news portal with all functionality
6. **Test Coverage**: >95% coverage for registry and module architecture

This redesign addresses the fundamental architectural issues and provides a solid foundation for the Living Codex system that aligns with the core principles of node-based architecture and proper dependency management.
