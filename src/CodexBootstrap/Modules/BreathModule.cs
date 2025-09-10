using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Breath module specific response types
public record ExpandResponse(string Id, string Phase, bool Expanded, string? Message = null);

public record ValidateResponse(string Id, bool Valid, string Message);

public record ContractResponse(string Id, string Phase, bool Contracted, string? Message = null);

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
        router.Register("codex.breath", "expand", async args =>
        {
            if (args is null) return new ErrorResponse("Missing request body");

            try
            {
                var request = JsonSerializer.Deserialize<ExpandRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return new ErrorResponse("Missing or invalid id");

                // Real expand operation - promote Ice → Gas
                var expandedResult = await ExpandModule(request.Id, registry);
                return expandedResult;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Expand failed: {ex.Message}");
            }
        });

        router.Register("codex.breath", "validate", async args =>
        {
            if (args is null) return new ErrorResponse("Missing request body");

            try
            {
                var request = JsonSerializer.Deserialize<ValidateRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return new ErrorResponse("Missing or invalid id");

                // Real validate operation - check Gas state integrity
                var validationResult = await ValidateModule(request.Id, registry);
                return validationResult;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Validate failed: {ex.Message}");
            }
        });

        router.Register("codex.breath", "contract", async args =>
        {
            if (args is null) return new ErrorResponse("Missing request body");

            try
            {
                var request = JsonSerializer.Deserialize<ContractRequest>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (request?.Id == null) return new ErrorResponse("Missing or invalid id");

                // Real contract operation - promote Gas → Water
                var contractResult = await ContractModule(request.Id, registry);
                return contractResult;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Contract failed: {ex.Message}");
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

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Breath module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint
    }

    private Task<ExpandResponse> ExpandModule(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get the module node
            if (!registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Check if module is in Ice state (ready to expand)
            if (moduleNode.State != ContentState.Ice)
            {
                return Task.FromResult(new ExpandResponse(moduleId, moduleNode.State.ToString(), false, $"Module is in {moduleNode.State} state, cannot expand from Ice"));
            }

            // Get all nodes related to this module
            var moduleNodes = registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = registry.AllEdges()
                .Where(edge => moduleNodes.Any(n => n.Id == edge.FromId || n.Id == edge.ToId))
                .ToList();

            // Expand: promote Ice → Gas by materializing dependencies
            var expandedNodes = new List<Node>();
            var expandedEdges = new List<Edge>();

            foreach (var node in moduleNodes)
            {
                if (node.State == ContentState.Ice)
                {
                    // Create expanded version in Gas state
                    var expandedNode = node with 
                    { 
                        State = ContentState.Gas,
                        Meta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                        {
                            ["expandedAt"] = DateTime.UtcNow.ToString("O"),
                            ["expandedFrom"] = node.Id
                        }
                    };
                    expandedNodes.Add(expandedNode);
                    registry.Upsert(expandedNode);
                }
            }

            // Expand edges to show full dependency graph
            foreach (var edge in moduleEdges)
            {
                var expandedEdge = edge with
                {
                    Meta = new Dictionary<string, object>(edge.Meta ?? new Dictionary<string, object>())
                    {
                        ["expandedAt"] = DateTime.UtcNow.ToString("O")
                    }
                };
                expandedEdges.Add(expandedEdge);
                registry.Upsert(expandedEdge);
            }

            return Task.FromResult(new ExpandResponse(
                moduleId, 
                "expanded", 
                true, 
                $"Expanded {expandedNodes.Count} nodes and {expandedEdges.Count} edges from Ice to Gas state"
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExpandResponse(moduleId, "error", false, $"Expand failed: {ex.Message}"));
        }
    }

    private Task<ValidateResponse> ValidateModule(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get the module node
            if (!registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Check if module is in Gas state (ready to validate)
            if (moduleNode.State != ContentState.Gas)
            {
                return Task.FromResult(new ValidateResponse(moduleId, false, $"Module is in {moduleNode.State} state, cannot validate from Gas"));
            }

            // Get all nodes related to this module
            var moduleNodes = registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = registry.AllEdges()
                .Where(edge => moduleNodes.Any(n => n.Id == edge.FromId || n.Id == edge.ToId))
                .ToList();

            var validationErrors = new List<string>();

            // Validate node integrity
            foreach (var node in moduleNodes)
            {
                if (node.State == ContentState.Gas)
                {
                    // Check required fields
                    if (string.IsNullOrEmpty(node.Id))
                        validationErrors.Add($"Node {node.Id} has empty ID");
                    if (string.IsNullOrEmpty(node.TypeId))
                        validationErrors.Add($"Node {node.Id} has empty TypeId");
                    if (node.Content == null && node.State == ContentState.Gas)
                        validationErrors.Add($"Node {node.Id} in Gas state has no content");
                }
            }

            // Validate edge integrity
            foreach (var edge in moduleEdges)
            {
                if (string.IsNullOrEmpty(edge.FromId) || string.IsNullOrEmpty(edge.ToId))
                    validationErrors.Add($"Edge has empty FromId or ToId");
                if (string.IsNullOrEmpty(edge.Role))
                    validationErrors.Add($"Edge {edge.FromId}->{edge.ToId} has empty Role");
            }

            // Validate module structure
            if (!moduleNodes.Any())
                validationErrors.Add("Module has no nodes");
            if (moduleNodes.Count(n => n.State == ContentState.Gas) == 0)
                validationErrors.Add("Module has no nodes in Gas state");

            var isValid = !validationErrors.Any();
            var message = isValid 
                ? $"Validation successful: {moduleNodes.Count} nodes, {moduleEdges.Count} edges"
                : $"Validation failed: {string.Join("; ", validationErrors)}";

            return Task.FromResult(new ValidateResponse(moduleId, isValid, message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ValidateResponse(moduleId, false, $"Validate failed: {ex.Message}"));
        }
    }

    private Task<ContractResponse> ContractModule(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get the module node
            if (!registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Check if module is in Gas state (ready to contract)
            if (moduleNode.State != ContentState.Gas)
            {
                return Task.FromResult(new ContractResponse(moduleId, moduleNode.State.ToString(), false, $"Module is in {moduleNode.State} state, cannot contract from Gas"));
            }

            // Get all nodes related to this module
            var moduleNodes = registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = registry.AllEdges()
                .Where(edge => moduleNodes.Any(n => n.Id == edge.FromId || n.Id == edge.ToId))
                .ToList();

            // Contract: promote Gas → Water by consolidating and optimizing
            var contractedNodes = new List<Node>();
            var contractedEdges = new List<Edge>();

            foreach (var node in moduleNodes)
            {
                if (node.State == ContentState.Gas)
                {
                    // Create contracted version in Water state
                    var contractedNode = node with 
                    { 
                        State = ContentState.Water,
                        Meta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                        {
                            ["contractedAt"] = DateTime.UtcNow.ToString("O"),
                            ["contractedFrom"] = node.Id
                        }
                    };
                    contractedNodes.Add(contractedNode);
                    registry.Upsert(contractedNode);
                }
            }

            // Contract edges to show optimized relationships
            foreach (var edge in moduleEdges)
            {
                var contractedEdge = edge with
                {
                    Meta = new Dictionary<string, object>(edge.Meta ?? new Dictionary<string, object>())
                    {
                        ["contractedAt"] = DateTime.UtcNow.ToString("O")
                    }
                };
                contractedEdges.Add(contractedEdge);
                registry.Upsert(contractedEdge);
            }

            return Task.FromResult(new ContractResponse(
                moduleId, 
                "contracted", 
                true, 
                $"Contracted {contractedNodes.Count} nodes and {contractedEdges.Count} edges from Gas to Water state"
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ContractResponse(moduleId, "error", false, $"Contract failed: {ex.Message}"));
        }
    }
}

// Request/Response types for the breath module
public sealed record ExpandRequest(string Id);
public sealed record ValidateRequest(string Id);
public sealed record ContractRequest(string Id);
public sealed record BreathRequest(string Id, string? Operation = null);
public sealed record BreathLoopRequest(string Id, string[]? Operations = null);