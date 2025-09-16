using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class RouteDiscovery
{
    private readonly IApiRouter _router;
    private readonly INodeRegistry _registry;
    private readonly Core.ICodexLogger _logger;

    public RouteDiscovery(IApiRouter router, INodeRegistry registry)
    {
        _router = router;
        _registry = registry;
        _logger = new Log4NetLogger(typeof(RouteDiscovery));
    }

    public void DiscoverAndRegisterRoutes(WebApplication app)
    {
        // Find all API nodes and register their routes
        var apiNodes = _registry.GetNodesByType("api")
            .Concat(_registry.GetNodesByType("codex.meta/api"));
        
        _logger.Info($"RouteDiscovery: Found {apiNodes.Count()} API nodes");
        
        foreach (var apiNode in apiNodes)
        {
            _logger.Debug($"RouteDiscovery: Processing API node {apiNode.Id}");
            RegisterApiRoute(app, apiNode);
        }
    }

    private void RegisterApiRoute(WebApplication app, Node apiNode)
    {
        try
        {
        var moduleId = apiNode.Meta?.GetValueOrDefault("moduleId")?.ToString();
        var apiName = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString();
        var route = apiNode.Meta?.GetValueOrDefault("route")?.ToString();

        _logger.Debug($"RouteDiscovery: Registering {apiNode.Id} - moduleId: {moduleId}, apiName: {apiName}, route: {route}");

        if (string.IsNullOrEmpty(moduleId) || string.IsNullOrEmpty(apiName) || string.IsNullOrEmpty(route))
        {
            _logger.Warn($"RouteDiscovery: Skipping {apiNode.Id} - missing required properties");
            return;
        }

            // Create route pattern
            var routePattern = CreateRoutePattern(route);
            
            // Register the route
            app.MapPost(routePattern, async (HttpContext context) =>
            {
                try
                {
                    // Read the request body
                    using var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    
                    JsonElement? args = null;
                    if (!string.IsNullOrEmpty(body))
                    {
                        args = JsonSerializer.Deserialize<JsonElement>(body);
                    }
                    
                    // Extract route parameters and merge with request body
                    var routeParams = new Dictionary<string, object>();
                    foreach (var param in context.Request.RouteValues)
                    {
                        if (param.Key != null && param.Value != null)
                        {
                            routeParams[param.Key] = param.Value;
                        }
                    }
                    
                    // Merge route parameters with request body
                    if (routeParams.Any())
                    {
                        var mergedArgs = new Dictionary<string, object>();
                        if (args.HasValue)
                        {
                            foreach (var prop in args.Value.EnumerateObject())
                            {
                                mergedArgs[prop.Name] = prop.Value;
                            }
                        }
                        foreach (var param in routeParams)
                        {
                            mergedArgs[param.Key] = param.Value;
                        }
                        args = JsonSerializer.SerializeToElement(mergedArgs);
                    }
                    
                    // Call the module's API handler
                    if (_router.TryGetHandler(moduleId, apiName, out var handler))
                    {
                        var result = await handler(args);
                        return Results.Ok(result);
                    }
                    
                    return Results.NotFound($"API {apiName} not found in module {moduleId}");
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error executing {apiName}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to register route for API {apiNode.Id}: {ex.Message}", ex);
        }
    }

    private string CreateRoutePattern(string route)
    {
        // Convert API route to ASP.NET Core route pattern
        var pattern = route;
        
        // If the route doesn't start with /, add it
        if (!pattern.StartsWith("/"))
        {
            pattern = "/" + pattern;
        }
        
        return pattern;
    }
}