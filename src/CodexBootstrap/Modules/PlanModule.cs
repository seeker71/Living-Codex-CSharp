using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Plan module specific response types
public record PlanResponse(string ModuleId, object Plan, bool Success, string Message = "Plan generated successfully");

public sealed class PlanModule : IModule
{
    private readonly NodeRegistry _registry;

    public PlanModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.plan",
            name: "Plan Generation Module",
            version: "0.1.0",
            description: "Self-contained module for generating topology plans using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var planApi = NodeStorage.CreateApiNode("codex.plan", "get-plan", "/plan/{id}", "Get topology plan for module");
        
        registry.Upsert(planApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.plan", "get-plan"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.plan", "get-plan", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return new ErrorResponse("Module ID is required");
                }

                // Generate real topology plan for the module
                var plan = await GenerateTopologyPlan(moduleId, registry);

                return new PlanResponse(moduleId, plan, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to generate plan: {ex.Message}");
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Plan generation endpoint
        app.MapGet("/plan/{id}", (string id) =>
        {
            try
            {
                var request = JsonSerializer.SerializeToElement(new { moduleId = id });
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.plan", "get-plan", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to generate plan: {ex.Message}");
            }
        });
    }

    private async Task<object> GenerateTopologyPlan(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get the module node to understand its structure
            if (!registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Analyze module dependencies and APIs
            var moduleApis = registry.GetNodesByType("api")
                .Where(api => api.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            var moduleEdges = registry.AllEdges()
                .Where(edge => edge.FromId == moduleId || edge.ToId == moduleId)
                .ToList();

            // Build topology based on actual module structure
            var topologyNodes = new List<object>();
            var topologyEdges = new List<object>();
            var executionOrder = new List<string>();

            // Core module node
            topologyNodes.Add(new
            {
                id = $"{moduleId}-core",
                type = "core",
                dependencies = new string[0],
                state = moduleNode.State.ToString(),
                description = "Core module functionality"
            });
            executionOrder.Add("core");

            // API nodes based on actual APIs
            foreach (var api in moduleApis)
            {
                var apiName = api.Meta?.GetValueOrDefault("apiName")?.ToString() ?? "unknown";
                topologyNodes.Add(new
                {
                    id = $"{moduleId}-{apiName}",
                    type = "api",
                    dependencies = new[] { $"{moduleId}-core" },
                    route = api.Meta?.GetValueOrDefault("route")?.ToString(),
                    description = api.Description
                });
                topologyEdges.Add(new
                {
                    from = $"{moduleId}-{apiName}",
                    to = $"{moduleId}-core",
                    role = "depends_on"
                });
            }

            // Storage node if module has persistent data
            if (moduleNode.Content != null || moduleEdges.Any())
            {
                topologyNodes.Add(new
                {
                    id = $"{moduleId}-storage",
                    type = "storage",
                    dependencies = new[] { $"{moduleId}-core" },
                    description = "Module data storage"
                });
                topologyEdges.Add(new
                {
                    from = $"{moduleId}-storage",
                    to = $"{moduleId}-core",
                    role = "depends_on"
                });
                executionOrder.Add("storage");
            }

            // Add API execution after storage
            if (moduleApis.Any())
            {
                executionOrder.Add("api");
            }

            // Calculate complexity based on actual structure
            var complexity = CalculateComplexity(topologyNodes.Count, topologyEdges.Count, moduleApis.Count);

            // Determine required resources
            var requiredResources = DetermineRequiredResources(moduleNode, moduleApis);

            return new
            {
                moduleId,
                topology = new
                {
                    nodes = topologyNodes,
                    edges = topologyEdges
                },
                executionOrder,
                estimatedComplexity = complexity,
                requiredResources,
                analysis = new
                {
                    apiCount = moduleApis.Count,
                    edgeCount = moduleEdges.Count,
                    hasContent = moduleNode.Content != null,
                    moduleState = moduleNode.State.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate topology plan: {ex.Message}");
        }
    }

    private string CalculateComplexity(int nodeCount, int edgeCount, int apiCount)
    {
        var score = nodeCount + edgeCount + apiCount;
        return score switch
        {
            <= 3 => "low",
            <= 6 => "medium",
            <= 10 => "high",
            _ => "very_high"
        };
    }

    private string[] DetermineRequiredResources(Node moduleNode, List<Node> moduleApis)
    {
        var resources = new List<string> { "memory" };

        if (moduleNode.Content != null)
        {
            resources.Add("storage");
        }

        if (moduleApis.Any())
        {
            resources.Add("network");
        }

        if (moduleNode.State == ContentState.Water)
        {
            resources.Add("processing");
        }

        return resources.ToArray();
    }
}
