using System.Text.Json;

namespace CodexBootstrap.Core;

// Core - only nodes and edges
public class NodeRegistry
{
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Edge> _edges = new();

    public virtual void Upsert(Node node) => _nodes[node.Id] = node;
    public virtual void Upsert(Edge edge) => _edges.Add(edge);
    public virtual bool TryGet(string id, out Node node) => _nodes.TryGetValue(id, out node!);
    public virtual IEnumerable<Edge> AllEdges() => _edges;
    public virtual IEnumerable<Node> AllNodes() => _nodes.Values.ToArray();
    
    public virtual IEnumerable<Node> GetNodesByType(string typeId) => 
        _nodes.Values.Where(n => n.TypeId == typeId).ToArray();
    
    public virtual IEnumerable<Edge> GetEdgesFrom(string fromId) => 
        _edges.Where(e => e.FromId == fromId);
    
    public virtual IEnumerable<Edge> GetEdgesTo(string toId) => 
        _edges.Where(e => e.ToId == toId);
    
    public virtual IEnumerable<Edge> GetEdges(string? fromId = null, string? toId = null, string? edgeType = null) =>
        _edges.Where(e => 
            (fromId == null || e.FromId == fromId) &&
            (toId == null || e.ToId == toId) &&
            (edgeType == null || e.Role == edgeType));
    
    public virtual void RemoveNode(string id) => _nodes.Remove(id);
    
    public virtual void RemoveEdge(string fromId, string toId) => 
        _edges.RemoveAll(e => e.FromId == fromId && e.ToId == toId);
    
    public virtual Node? GetNode(string id) => _nodes.TryGetValue(id, out var node) ? node : null;
    
    public virtual Edge? GetEdge(string fromId, string toId) => 
        _edges.FirstOrDefault(e => e.FromId == fromId && e.ToId == toId);
}


public sealed class ApiRouter : IApiRouter
{
    private readonly Dictionary<(string module, string api), Func<JsonElement?, Task<object>>> _handlers = new();
    private readonly Core.ILogger _logger;
    private readonly NodeRegistry _registry;
    
    public ApiRouter(NodeRegistry registry)
    {
        _logger = new Log4NetLogger(typeof(ApiRouter));
        _registry = registry;
    }
    
    public void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler)
    {
        // Registration is logged by ApiRouteDiscovery
        _handlers[(moduleId, api)] = handler;
    }
    
    public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler)
    {
        return _handlers.TryGetValue((moduleId, api), out handler!);
    }
    
    public NodeRegistry GetRegistry()
    {
        return _registry;
    }
}

// Node-based storage for everything
public static class NodeStorage
{
    public static Node CreateModuleNode(string id, string name, string version, string? description = null, string[]? capabilities = null, string[]? tags = null, string? specReference = null)
    {
        var content = new Dictionary<string, object>
        {
            ["id"] = id,
            ["name"] = name,
            ["version"] = version,
            ["description"] = description,
            ["capabilities"] = capabilities ?? new string[0],
            ["tags"] = tags ?? new string[0],
            ["createdAt"] = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(specReference))
        {
            content["specReference"] = specReference;
        }

        var meta = new Dictionary<string, object>
        {
            ["moduleId"] = id,
            ["version"] = version,
            ["name"] = name,
            ["description"] = description,
            ["capabilities"] = capabilities ?? new string[0],
            ["tags"] = tags ?? new string[0],
            ["createdAt"] = DateTime.UtcNow,
            ["type"] = "module"
        };

        if (!string.IsNullOrEmpty(specReference))
        {
            meta["specReference"] = specReference;
        }

        return new Node(
            Id: id,
            TypeId: "module",
            State: ContentState.Ice,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(content),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: meta
        );
    }

    public static Node CreateApiNode(string moduleId, string apiName, string route, string? description = null)
    {
        return new Node(
            Id: $"{moduleId}.{apiName}",
            TypeId: "api",
            State: ContentState.Ice,
            Locale: "en",
            Title: apiName,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new { moduleId, apiName, route, description }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["apiName"] = apiName,
                ["route"] = route
            }
        );
    }

    /// <summary>
    /// Register a module with all its records as meta nodes
    /// </summary>
    public static void RegisterModuleWithMetaNodes(NodeRegistry registry, IModule module, string[]? recordTypes = null)
    {
        // Register the main module node
        var moduleNode = module.GetModuleNode();
        registry.Upsert(moduleNode);
        
        // Call the module's Register method to initialize any module-specific data
        try
        {
            module.Register(registry);
        }
        catch (Exception ex)
        {
            // Log but don't fail the registration
            Console.WriteLine($"Warning: Module {module.GetType().Name} Register method failed: {ex.Message}");
        }
        
        // Create spec-to-module tracking if spec reference exists
        if (moduleNode.Meta?.ContainsKey("specReference") == true)
        {
            var specReference = moduleNode.Meta["specReference"]?.ToString();
            if (!string.IsNullOrEmpty(specReference))
            {
                // Create edge from spec to module
                var specModuleEdge = CreateSpecModuleEdge(specReference, moduleNode.Id);
                registry.Upsert(specModuleEdge);
                
                // Create edge from module to spec
                var moduleSpecEdge = CreateModuleSpecEdge(moduleNode.Id, specReference);
                registry.Upsert(moduleSpecEdge);
            }
        }
        
        // Register record types as meta nodes if provided
        if (recordTypes != null)
        {
            foreach (var recordType in recordTypes)
            {
                var metaNode = new Node(
                    Id: $"{moduleNode.Id}.meta.{recordType.ToLower()}",
                    TypeId: "codex.meta/type",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: $"{recordType} Record Type",
                    Description: $"Meta-node definition for {recordType} record type used by {moduleNode.Title}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new
                        {
                            recordType = recordType,
                            moduleId = moduleNode.Id,
                            moduleName = moduleNode.Title,
                            definedAt = DateTime.UtcNow
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["recordType"] = recordType,
                        ["moduleId"] = moduleNode.Id,
                        ["moduleName"] = moduleNode.Title,
                        ["parentModule"] = moduleNode.Id,
                        ["definedAt"] = DateTime.UtcNow
                    }
                );
                registry.Upsert(metaNode);
                
                // Create edge from module to meta node
                var edge = new Edge(
                    FromId: moduleNode.Id,
                    ToId: metaNode.Id,
                    Role: "defines",
                    Weight: 1.0,
                    Meta: new Dictionary<string, object>
                    {
                        ["relationship"] = "module-defines-record-type",
                        ["recordType"] = recordType
                    }
                );
                registry.Upsert(edge);
            }
        }
    }

    public static Node CreateSpecNode(string id, string name, string version, object spec)
    {
        return new Node(
            Id: id,
            TypeId: "codex.meta/spec",
            State: ContentState.Ice,
            Locale: "en",
            Title: name,
            Description: $"Specification for {name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(spec),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["specId"] = id,
                ["name"] = name,
                ["version"] = version
            }
        );
    }

    public static Edge CreateModuleApiEdge(string moduleId, string apiName)
    {
        return new Edge(
            FromId: moduleId,
            ToId: $"{moduleId}.{apiName}",
            Role: "exposes",
            Weight: 1.0,
            Meta: new Dictionary<string, object>
            {
                ["relationship"] = "module-exposes-api"
            }
        );
    }

    public static Edge CreateModuleSpecEdge(string moduleId, string specId)
    {
        return new Edge(
            FromId: moduleId,
            ToId: specId,
            Role: "implements",
            Weight: 1.0,
            Meta: new Dictionary<string, object>
            {
                ["relationship"] = "module-implements-spec"
            }
        );
    }

    public static Edge CreateSpecModuleEdge(string specId, string moduleId)
    {
        return new Edge(
            FromId: specId,
            ToId: moduleId,
            Role: "has-implementation",
            Weight: 1.0,
            Meta: new Dictionary<string, object>
            {
                ["relationship"] = "spec-has-module-implementation"
            }
        );
    }
}
