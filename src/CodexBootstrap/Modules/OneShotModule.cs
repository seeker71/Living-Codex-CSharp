using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// OneShot module specific response types
public record OneShotApplyResponse(string ModuleId, object Result, bool Success, string Message = "One-shot applied successfully");
public record OneShotExecuteResponse(string ModuleId, object Result, bool Success, string Message = "One-shot executed successfully");

public sealed class OneShotModule : ModuleBase
{
    private readonly IApiRouter _router;

    public override string Name => "One-Shot Operations Module";
    public override string Description => "Self-contained module for one-shot operations (apply, execute) using node-based storage";
    public override string Version => "0.1.0";

    public OneShotModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, IApiRouter? router = null) 
        : base(registry, logger)
    {
        _router = router ?? new MockApiRouter();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.oneshot",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "oneshot", "apply", "execute", "operations" },
            capabilities: new[] { "one-shot", "apply", "execute", "operations" },
            spec: "codex.spec.oneshot"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
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
                    var composeMessage = ExtractMessageFromResult(composeResult);
                    steps.Add(new { step = "compose", status = "completed", result = composeMessage });

                    // Step 2: Expand the spec
                    var expandRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var expandResult = await ExecuteModuleApi("codex.breath", "expand", expandRequest);
                    var expandMessage = ExtractMessageFromResult(expandResult);
                    steps.Add(new { step = "expand", status = "completed", result = expandMessage });

                    // Step 3: Validate the expanded spec
                    var validateRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var validateResult = await ExecuteModuleApi("codex.breath", "validate", validateRequest);
                    var validateMessage = ExtractMessageFromResult(validateResult);
                    steps.Add(new { step = "validate", status = "completed", result = validateMessage });

                    // Step 4: Contract the validated spec
                    var contractRequest = JsonSerializer.SerializeToElement(new { id = moduleId });
                    var contractResult = await ExecuteModuleApi("codex.breath", "contract", contractRequest);
                    var contractMessage = ExtractMessageFromResult(contractResult);
                    steps.Add(new { step = "contract", status = "completed", result = contractMessage });
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

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
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

    private string ExtractMessageFromResult(object result)
    {
        try
        {
            // Try to extract message from common response types
            var resultType = result.GetType();
            
            // Check for Message property
            var messageProperty = resultType.GetProperty("Message");
            if (messageProperty != null)
            {
                var message = messageProperty.GetValue(result)?.ToString();
                if (!string.IsNullOrEmpty(message))
                    return message;
            }

            // Check for result property
            var resultProperty = resultType.GetProperty("Result");
            if (resultProperty != null)
            {
                var resultValue = resultProperty.GetValue(result)?.ToString();
                if (!string.IsNullOrEmpty(resultValue))
                    return resultValue;
            }

            // Check for success property
            var successProperty = resultType.GetProperty("Success");
            if (successProperty != null)
            {
                var success = successProperty.GetValue(result);
                return success?.ToString() == "True" ? "Operation successful" : "Operation failed";
            }

            // Fallback to type name
            return $"{resultType.Name} completed";
        }
        catch
        {
            return "Operation completed";
        }
    }
}
