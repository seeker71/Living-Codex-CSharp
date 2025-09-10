using System.Text.Json;

namespace CodexBootstrap.Core;

// Core - only nodes and edges
public sealed class NodeRegistry
{
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Edge> _edges = new();

    public void Upsert(Node node) => _nodes[node.Id] = node;
    public void Upsert(Edge edge) => _edges.Add(edge);
    public bool TryGet(string id, out Node node) => _nodes.TryGetValue(id, out node!);
    public IEnumerable<Edge> AllEdges() => _edges;
    public IEnumerable<Node> AllNodes() => _nodes.Values;
    
    public IEnumerable<Node> GetNodesByType(string typeId) => 
        _nodes.Values.Where(n => n.TypeId == typeId);
    
    public IEnumerable<Edge> GetEdgesFrom(string fromId) => 
        _edges.Where(e => e.FromId == fromId);
    
    public IEnumerable<Edge> GetEdgesTo(string toId) => 
        _edges.Where(e => e.ToId == toId);
}


public sealed class ApiRouter : IApiRouter
{
    private readonly Dictionary<(string module, string api), Func<JsonElement?, Task<object>>> _handlers = new();
    private readonly Core.ILogger _logger;
    
    public ApiRouter()
    {
        _logger = new Log4NetLogger(typeof(ApiRouter));
    }
    
    public void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler)
    {
        _logger.Debug($"ApiRouter: Registering {moduleId}.{api}");
        _handlers[(moduleId, api)] = handler;
    }
    
    public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler)
    {
        var found = _handlers.TryGetValue((moduleId, api), out handler!);
        _logger.Debug($"ApiRouter: TryGetHandler({moduleId}, {api}) = {found}");
        if (!found)
        {
            _logger.Debug($"ApiRouter: Available handlers: {string.Join(", ", _handlers.Keys.Select(k => $"{k.module}.{k.api}"))}");
        }
        return found;
    }
}

// Node-based storage for everything
public static class NodeStorage
{
    public static Node CreateModuleNode(string id, string name, string version, string? description = null)
    {
        return new Node(
            Id: id,
            TypeId: "module",
            State: ContentState.Ice,
            Locale: "en",
            Title: name,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new { id, name, version, description }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = id,
                ["version"] = version,
                ["name"] = name
            }
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
}
