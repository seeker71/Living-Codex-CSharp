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
        router.Register("codex.hydrate", "hydrate", async args =>
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
                    return new ErrorResponse("Node ID is required");
                }

                // Get the node from registry
                if (!registry.TryGet(nodeId, out var node))
                {
                    return new ErrorResponse($"Node '{nodeId}' not found");
                }

                // Real hydration: promote Gas/Ice → Water via adapters/synthesizer
                var hydratedContent = await HydrateNodeContent(node, registry);

                return new HydrateNodeResponse(nodeId, hydratedContent, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to hydrate node: {ex.Message}");
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Hydrate module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint and RouteDiscovery
    }

    private async Task<object> HydrateNodeContent(Node node, NodeRegistry registry)
    {
        try
        {
            // Check if node is in Ice or Gas state and needs hydration
            if (node.State != ContentState.Ice && node.State != ContentState.Gas)
            {
                return new
                {
                    nodeId = node.Id,
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
                    hydrationMethod = "already_hydrated",
                    success = true,
                    message = "Node is already in Water state"
                };
            }

            // Find adapters that can hydrate this node type
            var adapters = registry.GetNodesByType("codex.adapters/adapter")
                .Where(a => CanHydrateNodeType(a, node.TypeId))
                .ToList();

            if (!adapters.Any())
            {
                // No specific adapter found, use default synthesis
                return await SynthesizeContent(node);
            }

            // Try each adapter until one succeeds
            foreach (var adapter in adapters)
            {
                try
                {
                    var result = await TryAdapterHydration(node, adapter, registry);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    // Log adapter failure and try next one
                    Console.WriteLine($"Adapter {adapter.Id} failed: {ex.Message}");
                }
            }

            // Fallback to synthesis if all adapters failed
            return await SynthesizeContent(node);
        }
        catch (Exception ex)
        {
            return new
            {
                nodeId = node.Id,
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
                hydrationMethod = "error",
                success = false,
                error = ex.Message
            };
        }
    }

    private bool CanHydrateNodeType(Node adapter, string nodeTypeId)
    {
        // Check if adapter supports this node type
        var supportedTypes = adapter.Meta?.GetValueOrDefault("supportedTypes") as string[] ?? new string[0];
        return supportedTypes.Contains(nodeTypeId) || supportedTypes.Contains("*");
    }

    private Task<object?> TryAdapterHydration(Node node, Node adapter, NodeRegistry registry)
    {
        // This would call the adapter's hydration logic
        // For now, return null to indicate adapter couldn't handle this node
        return Task.FromResult<object?>(null);
    }

    private Task<object> SynthesizeContent(Node node)
    {
        // Default content synthesis when no adapter is available
        var synthesizedContent = new
        {
            id = node.Id,
            typeId = node.TypeId,
            title = node.Title,
            description = node.Description,
            state = ContentState.Water.ToString(),
            hydratedAt = DateTime.UtcNow,
            synthesized = true,
            originalContent = node.Content?.InlineJson ?? "{}"
        };

        return Task.FromResult<object>(new
        {
            nodeId = node.Id,
            originalState = node.State.ToString(),
            hydratedAt = DateTime.UtcNow,
            content = JsonSerializer.Serialize(synthesizedContent),
            metadata = new
            {
                typeId = node.TypeId,
                title = node.Title,
                description = node.Description,
                locale = node.Locale
            },
            hydrationMethod = "synthesis",
            success = true,
            synthesized = true
        });
    }
}
