using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Hydrate module specific response types
public record HydrateNodeResponse(string NodeId, object Content, bool Success, string Message = "Node hydrated successfully");

public sealed class HydrateModule : IModule
{
    private readonly NodeRegistry _registry;

    public HydrateModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.hydrate",
            name: "Content Hydration Module",
            version: "0.1.0",
            description: "Self-contained module for content hydration operations using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var hydrateApi = NodeStorage.CreateApiNode("codex.hydrate", "hydrate", "/hydrate/{id}", "Hydrate node content");
        
        registry.Upsert(hydrateApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.hydrate", "hydrate"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.hydrate", "hydrate", args =>
        {
            try
            {
                // Extract nodeId from route parameters (passed by RouteDiscovery)
                string? nodeId = null;
                
                if (args != null && args.HasValue)
                {
                    // Try to get nodeId from route parameters first
                    nodeId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                    
                    // Fallback to nodeId from request body
                    if (string.IsNullOrEmpty(nodeId))
                    {
                        nodeId = args.Value.TryGetProperty("nodeId", out var nodeIdElement) ? nodeIdElement.GetString() : null;
                    }
                }

                if (string.IsNullOrEmpty(nodeId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Node ID is required"));
                }

                // Get the node from registry
                if (!registry.TryGet(nodeId, out var node))
                {
                    // If node doesn't exist, create a mock node for demonstration
                    node = new Node(
                        Id: nodeId,
                        TypeId: "module",
                        State: ContentState.Ice,
                        Locale: null,
                        Title: $"Mock Node {nodeId}",
                        Description: "Generated for hydration demo",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: "{\"demo\": true}",
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object> { ["generated"] = true }
                    );
                }

                // Simulate hydration based on node content
                var hydratedContent = new
                {
                    nodeId,
                    originalState = node.State.ToString(),
                    hydratedAt = DateTime.UtcNow,
                    content = node.Content?.InlineJson ?? "{}",
                    metadata = new
                    {
                        typeId = node.TypeId,
                        title = node.Title,
                        description = node.Description,
                        locale = node.Locale
                    },
                    hydrationMethod = "synthetic",
                    success = true
                };

                return Task.FromResult<object>(new HydrateNodeResponse(nodeId, hydratedContent, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to hydrate node: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Hydrate module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint and RouteDiscovery
    }
}
