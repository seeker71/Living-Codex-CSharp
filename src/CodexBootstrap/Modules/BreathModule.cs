using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

// Breath module specific response types
public record ExpandResponse(string Id, string Phase, bool Expanded);

public record ValidateResponse(string Id, bool Valid, string Message);

public record ContractResponse(string Id, string Phase, bool Contracted);

public record BreathLoopResult(string Operation, object Result);

public record BreathLoopResponse(string Id, List<BreathLoopResult> Results, bool Success);

public record OneshotResult(
    ExpandResponse Expand,
    ValidateResponse Validate, 
    ContractResponse Contract
);

public record OneshotResponse(
    string Id, 
    string Operation, 
    OneshotResult Result, 
    bool Success, 
    string Message
);

public sealed class BreathModule : IModule
{
    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.breath",
            name: "Breath Engine Module",
            version: "0.1.0",
            description: "Self-contained module for breath loop operations (expand, validate, contract) using node-based storage"
        );
    }


    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var expandApi = NodeStorage.CreateApiNode("codex.breath", "expand", "/breath/expand/{id}", "Expand a module specification");
        var validateApi = NodeStorage.CreateApiNode("codex.breath", "validate", "/breath/validate/{id}", "Validate a module specification");
        var contractApi = NodeStorage.CreateApiNode("codex.breath", "contract", "/breath/contract/{id}", "Contract a module specification");
        var breathLoopApi = NodeStorage.CreateApiNode("codex.breath", "breath-loop", "/breath/loop/{id}", "Execute full breath loop");
        var oneshotApi = NodeStorage.CreateApiNode("codex.breath", "oneshot", "/breath/oneshot/{id}", "One-shot breath loop execution");
        
        registry.Upsert(expandApi);
        registry.Upsert(validateApi);
        registry.Upsert(contractApi);
        registry.Upsert(breathLoopApi);
        registry.Upsert(oneshotApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.breath", "expand"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.breath", "validate"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.breath", "contract"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.breath", "breath-loop"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.breath", "oneshot"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.breath", "expand", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var request = JsonSerializer.Deserialize<ExpandRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return Task.FromResult<object>(new ErrorResponse("Missing or invalid id"));

                // Simple expand operation
                var result = new ExpandResponse(request.Id, "expanded", true);
                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Expand failed: {ex.Message}"));
            }
        });

        router.Register("codex.breath", "validate", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var request = JsonSerializer.Deserialize<ValidateRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return Task.FromResult<object>(new ErrorResponse("Missing or invalid id"));

                // Simple validate operation
                var result = new ValidateResponse(request.Id, true, "Validation successful");
                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Validate failed: {ex.Message}"));
            }
        });

        router.Register("codex.breath", "contract", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var request = JsonSerializer.Deserialize<ContractRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return Task.FromResult<object>(new ErrorResponse("Missing or invalid id"));

                // Simple contract operation
                var result = new ContractResponse(request.Id, "contracted", true);
                return Task.FromResult<object>(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Contract failed: {ex.Message}"));
            }
        });

        router.Register("codex.breath", "breath-loop", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var request = JsonSerializer.Deserialize<BreathLoopRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return Task.FromResult<object>(new ErrorResponse("Missing or invalid id"));

                var results = new List<BreathLoopResult>();
                var success = true;

                foreach (var operation in request.Operations ?? new[] { "expand", "validate", "contract" })
                {
                    try
                    {
                        object result = operation.ToLowerInvariant() switch
                        {
                            "expand" => new ExpandResponse(request.Id, "expanded", true),
                            "validate" => new ValidateResponse(request.Id, true, "Validation successful"),
                            "contract" => new ContractResponse(request.Id, "contracted", true),
                            _ => new ErrorResponse($"Unknown operation: {operation}")
                        };
                        results.Add(new BreathLoopResult(operation, result));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new BreathLoopResult(operation, new ErrorResponse(ex.Message)));
                        success = false;
                    }
                }

                var response = new BreathLoopResponse(request.Id, results, success);
                return Task.FromResult<object>(response);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Breath loop failed: {ex.Message}"));
            }
        });

        router.Register("codex.breath", "oneshot", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var request = JsonSerializer.Deserialize<BreathRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return Task.FromResult<object>(new ErrorResponse("Missing or invalid id"));

                // Execute the breath loop: expand, validate, contract
                var expandResult = new ExpandResponse(request.Id, "expanded", true);
                var validateResult = new ValidateResponse(request.Id, true, "Validation successful");
                var contractResult = new ContractResponse(request.Id, "contracted", true);

                var result = new OneshotResult(expandResult, validateResult, contractResult);
                var response = new OneshotResponse(request.Id, "oneshot", result, true, "Oneshot completed");
                return Task.FromResult<object>(response);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Oneshot failed: {ex.Message}"));
            }
        });
    }
}

// Request/Response types for the breath module
public sealed record ExpandRequest(string Id);
public sealed record ValidateRequest(string Id);
public sealed record ContractRequest(string Id);
public sealed record BreathRequest(string Id, string? Operation = null);
public sealed record BreathLoopRequest(string Id, string[]? Operations = null);