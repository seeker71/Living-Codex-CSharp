using System.Collections.Concurrent;
using System.Text.Json;

namespace CodexBootstrap.Core;

public class ApiRouter : IApiRouter
{
    private readonly ConcurrentDictionary<string, Func<JsonElement?, Task<object>>> _handlers;
    private readonly INodeRegistry _registry;
    private readonly ICodexLogger _logger;

    public ApiRouter(INodeRegistry registry, ICodexLogger logger)
    {
        _handlers = new ConcurrentDictionary<string, Func<JsonElement?, Task<object>>>();
        _registry = registry;
        _logger = logger;
    }

    public void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler)
    {
        var key = $"{moduleId}:{api}";
        _handlers[key] = handler;
        _logger.Debug($"ApiRouter: Registered handler for {key}");
    }

    public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler)
    {
        var key = $"{moduleId}:{api}";
        var result = _handlers.TryGetValue(key, out handler!);
        if (!result)
        {
            _logger.Debug($"ApiRouter: No handler found for {key}");
        }
        return result;
    }

    public INodeRegistry GetRegistry()
    {
        return _registry;
    }
}


