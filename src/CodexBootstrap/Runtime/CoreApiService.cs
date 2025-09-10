using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class CoreApiService
{
    private readonly NodeRegistry _registry;
    private readonly IApiRouter _router;
    private readonly Core.ILogger _logger;

    public CoreApiService(NodeRegistry registry, IApiRouter router)
    {
        _registry = registry;
        _router = router;
        _logger = new Log4NetLogger(typeof(CoreApiService));
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

    public async Task<object> ExecuteDynamicCall(DynamicCall call)
    {
        _logger.Debug($"DynamicCall: ModuleId='{call.ModuleId}', Api='{call.Api}', Args={call.Args}");
        
        if (string.IsNullOrEmpty(call.Api))
        {
            var error = $"API name is empty for module {call.ModuleId}. Available APIs: {string.Join(", ", GetAvailableApisForModule(call.ModuleId))}";
            _logger.Warn(error);
            return new ErrorResponse(error);
        }
        
        if (!_router.TryGetHandler(call.ModuleId, call.Api, out var handler))
        {
            var error = $"API '{call.Api}' not found in module '{call.ModuleId}'. Available APIs: {string.Join(", ", GetAvailableApisForModule(call.ModuleId))}";
            _logger.Warn(error);
            return new ErrorResponse(error);
        }
        
        var result = await handler(call.Args);
        return result ?? new ErrorResponse("Handler returned null");
    }
    
    private IEnumerable<string> GetAvailableApisForModule(string moduleId)
    {
        // This is a simplified version - in a real implementation, you'd want to track this more efficiently
        return new[] { "No APIs available" };
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
            Name: moduleNode.Meta?.GetValueOrDefault("name", moduleNode.Title ?? "Unknown")?.ToString() ?? moduleNode.Title ?? "Unknown",
            Version: moduleNode.Meta?.GetValueOrDefault("version", "0.1.0")?.ToString() ?? "0.1.0",
            Description: moduleNode.Description,
            Title: moduleNode.Title ?? "Unknown"
        );
    }
}