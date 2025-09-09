using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class CoreApiService
{
    private readonly NodeRegistry _registry;
    private readonly IApiRouter _router;

    public CoreApiService(NodeRegistry registry, IApiRouter router)
    {
        _registry = registry;
        _router = router;
    }

    public List<ModuleInfo> GetModules()
    {
        var moduleNodes = _registry.GetNodesByType("module");
        return moduleNodes.Select(ExtractModuleInfo).ToList();
    }

    public ModuleInfo? GetModule(string id)
    {
        if (_registry.TryGet(id, out var node) && node.TypeId == "module")
        {
            return ExtractModuleInfo(node);
        }
        return null;
    }

    public object ExecuteDynamicCall(DynamicCall call)
    {
        if (!_router.TryGetHandler(call.ModuleId, call.Api, out var handler))
            throw new InvalidOperationException($"API {call.Api} not found in module {call.ModuleId}");
        
        var task = handler(call.Args);
        task.Wait();
        return task.Result ?? new ErrorResponse("Handler returned null");
    }

    public List<Node> GetNodes() => _registry.AllNodes().ToList();
    public List<Edge> GetEdges() => _registry.AllEdges().ToList();
    
    public Node? GetNode(string id) => 
        _registry.TryGet(id, out var node) ? node : null;

    public List<Node> GetNodesByType(string typeId) => 
        _registry.GetNodesByType(typeId).ToList();

    public List<Edge> GetEdgesFrom(string fromId) => 
        _registry.GetEdgesFrom(fromId).ToList();

    public List<Edge> GetEdgesTo(string toId) => 
        _registry.GetEdgesTo(toId).ToList();

    public Node UpsertNode(Node node)
    {
        _registry.Upsert(node);
        return node;
    }

    public Edge UpsertEdge(Edge edge)
    {
        _registry.Upsert(edge);
        return edge;
    }

    private ModuleInfo ExtractModuleInfo(Node moduleNode)
    {
        return new ModuleInfo(
            Id: moduleNode.Meta?.GetValueOrDefault("moduleId", moduleNode.Id)?.ToString() ?? moduleNode.Id,
            Name: moduleNode.Meta?.GetValueOrDefault("name", moduleNode.Title)?.ToString() ?? moduleNode.Title ?? "Unknown",
            Version: moduleNode.Meta?.GetValueOrDefault("version", "0.1.0")?.ToString() ?? "0.1.0",
            Description: moduleNode.Description,
            Title: moduleNode.Title ?? "Unknown"
        );
    }
}