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
        router.Register("codex.plan", "get-plan", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Generate topology plan for the module
                var plan = new
                {
                    moduleId,
                    topology = new
                    {
                        nodes = new[]
                        {
                            new { id = $"{moduleId}-core", type = "core", dependencies = new string[0] },
                            new { id = $"{moduleId}-api", type = "api", dependencies = new[] { $"{moduleId}-core" } },
                            new { id = $"{moduleId}-storage", type = "storage", dependencies = new[] { $"{moduleId}-core" } }
                        },
                        edges = new[]
                        {
                            new { from = $"{moduleId}-api", to = $"{moduleId}-core", role = "depends_on" },
                            new { from = $"{moduleId}-storage", to = $"{moduleId}-core", role = "depends_on" }
                        }
                    },
                    executionOrder = new[] { "core", "storage", "api" },
                    estimatedComplexity = "low",
                    requiredResources = new[] { "memory", "storage" }
                };

                return Task.FromResult<object>(new PlanResponse(moduleId, plan, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to generate plan: {ex.Message}"));
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
}
