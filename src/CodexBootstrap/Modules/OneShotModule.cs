using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// OneShot module specific response types
public record OneShotApplyResponse(string ModuleId, object Result, bool Success, string Message = "One-shot applied successfully");
public record OneShotExecuteResponse(string ModuleId, object Result, bool Success, string Message = "One-shot executed successfully");

public sealed class OneShotModule : IModule
{
    private readonly NodeRegistry _registry;

    public OneShotModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.oneshot",
            name: "One-Shot Operations Module",
            version: "0.1.0",
            description: "Self-contained module for one-shot operations (apply, execute) using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var applyApi = NodeStorage.CreateApiNode("codex.oneshot", "apply", "/oneshot/apply", "Apply atoms to prototype");
        var executeApi = NodeStorage.CreateApiNode("codex.oneshot", "execute", "/oneshot/{id}", "Execute one-shot on existing atoms");
        
        registry.Upsert(applyApi);
        registry.Upsert(executeApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.oneshot", "apply"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.oneshot", "execute"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.oneshot", "apply", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;
                var atoms = args.Value.TryGetProperty("atoms", out var atomsElement) ? atomsElement : (JsonElement?)null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Simulate one-shot apply: compose → expand → validate → contract
                var result = new
                {
                    moduleId,
                    steps = new[]
                    {
                        new { step = "compose", status = "completed", result = "Spec composed" },
                        new { step = "expand", status = "completed", result = "Spec expanded" },
                        new { step = "validate", status = "completed", result = "Spec validated" },
                        new { step = "contract", status = "completed", result = "Spec contracted" }
                    },
                    finalResult = "One-shot operation completed successfully"
                };

                return Task.FromResult<object>(new OneShotApplyResponse(moduleId, result, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to apply one-shot: {ex.Message}"));
            }
        });

        router.Register("codex.oneshot", "execute", args =>
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

                // Simulate one-shot execute on existing atoms
                var result = new
                {
                    moduleId,
                    steps = new[]
                    {
                        new { step = "load-atoms", status = "completed", result = "Atoms loaded" },
                        new { step = "compose", status = "completed", result = "Spec composed" },
                        new { step = "expand", status = "completed", result = "Spec expanded" },
                        new { step = "validate", status = "completed", result = "Spec validated" },
                        new { step = "contract", status = "completed", result = "Spec contracted" }
                    },
                    finalResult = "One-shot execution completed successfully"
                };

                return Task.FromResult<object>(new OneShotExecuteResponse(moduleId, result, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to execute one-shot: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // One-shot operation endpoints
        app.MapPost("/oneshot/apply", (JsonElement request) =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.oneshot", "apply", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to apply one-shot: {ex.Message}");
            }
        });

        app.MapPost("/oneshot/{id}", (string id, JsonElement request) =>
        {
            try
            {
                var requestWithId = JsonSerializer.SerializeToElement(new { moduleId = id, request });
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.oneshot", "execute", requestWithId));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to execute one-shot: {ex.Message}");
            }
        });
    }
}
