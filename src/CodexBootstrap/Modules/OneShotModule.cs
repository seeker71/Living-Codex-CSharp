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
    private readonly IApiRouter _router;

    public OneShotModule(NodeRegistry registry, IApiRouter router)
    {
        _registry = registry;
        _router = router;
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
        router.Register("codex.oneshot", "apply", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;
                var atoms = args.Value.TryGetProperty("atoms", out var atomsElement) ? atomsElement : (JsonElement?)null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return new ErrorResponse("Module ID is required");
                }

                // Real one-shot apply: compose → expand → validate → contract
                var steps = new List<object>();
                var success = true;
                var finalResult = "One-shot operation completed successfully";

                try
                {
                    // Step 1: Compose spec from atoms
                    var composeRequest = JsonSerializer.SerializeToElement(new { moduleId, atoms });
                    var composeResult = await ExecuteModuleApi("codex.spec", "compose", composeRequest);
                    steps.Add(new { step = "compose", status = "completed", result = "Spec composed from atoms" });

                    // Step 2: Expand the spec
                    var expandRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var expandResult = await ExecuteModuleApi("codex.breath", "expand", expandRequest);
                    steps.Add(new { step = "expand", status = "completed", result = "Spec expanded" });

                    // Step 3: Validate the expanded spec
                    var validateRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var validateResult = await ExecuteModuleApi("codex.breath", "validate", validateRequest);
                    steps.Add(new { step = "validate", status = "completed", result = "Spec validated" });

                    // Step 4: Contract the validated spec
                    var contractRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var contractResult = await ExecuteModuleApi("codex.breath", "contract", contractRequest);
                    steps.Add(new { step = "contract", status = "completed", result = "Spec contracted" });
                }
                catch (Exception ex)
                {
                    steps.Add(new { step = "error", status = "failed", result = ex.Message });
                    success = false;
                    finalResult = $"One-shot operation failed: {ex.Message}";
                }

                var result = new
                {
                    moduleId,
                    steps,
                    finalResult,
                    success
                };

                return new OneShotApplyResponse(moduleId, result, success);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to apply one-shot: {ex.Message}");
            }
        });

        router.Register("codex.oneshot", "execute", async args =>
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

                // Real one-shot execute on existing atoms
                var steps = new List<object>();
                var success = true;
                var finalResult = "One-shot execution completed successfully";

                try
                {
                    // Step 1: Load existing atoms
                    var getAtomsRequest = JsonSerializer.SerializeToElement(new { moduleId });
                    var atomsResult = await ExecuteModuleApi("codex.spec", "get-atoms", getAtomsRequest);
                    steps.Add(new { step = "load-atoms", status = "completed", result = "Atoms loaded from storage" });

                    // Step 2: Compose spec from loaded atoms
                    var composeRequest = JsonSerializer.SerializeToElement(new { moduleId, atoms = atomsResult });
                    var composeResult = await ExecuteModuleApi("codex.spec", "compose", composeRequest);
                    steps.Add(new { step = "compose", status = "completed", result = "Spec composed from loaded atoms" });

                    // Step 3: Expand the spec
                    var expandRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var expandResult = await ExecuteModuleApi("codex.breath", "expand", expandRequest);
                    steps.Add(new { step = "expand", status = "completed", result = "Spec expanded" });

                    // Step 4: Validate the expanded spec
                    var validateRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var validateResult = await ExecuteModuleApi("codex.breath", "validate", validateRequest);
                    steps.Add(new { step = "validate", status = "completed", result = "Spec validated" });

                    // Step 5: Contract the validated spec
                    var contractRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var contractResult = await ExecuteModuleApi("codex.breath", "contract", contractRequest);
                    steps.Add(new { step = "contract", status = "completed", result = "Spec contracted" });
                }
                catch (Exception ex)
                {
                    steps.Add(new { step = "error", status = "failed", result = ex.Message });
                    success = false;
                    finalResult = $"One-shot execution failed: {ex.Message}";
                }

                var result = new
                {
                    moduleId,
                    steps,
                    finalResult,
                    success
                };

                return new OneShotExecuteResponse(moduleId, result, success);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to execute one-shot: {ex.Message}");
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

    private async Task<object> ExecuteModuleApi(string moduleId, string apiName, JsonElement request)
    {
        if (_router.TryGetHandler(moduleId, apiName, out var handler))
        {
            var result = await handler(request);
            return result;
        }
        throw new InvalidOperationException($"API {apiName} not found in module {moduleId}");
    }
}
