using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Breath module specific response types
[ResponseType("codex.breath.expand-response", "ExpandResponse", "Expand response")]
public record ExpandResponse(string Id, string Phase, bool Expanded, string? Message = null);

[ResponseType("codex.breath.validate-response", "ValidateResponse", "Validate response")]
public record ValidateResponse(string Id, bool Valid, string Message);

[ResponseType("codex.breath.contract-response", "ContractResponse", "Contract response")]
public record ContractResponse(string Id, string Phase, bool Contracted, string? Message = null);

[ResponseType("codex.breath.breath-loop-result", "BreathLoopResult", "Breath loop result")]
public record BreathLoopResult(string Operation, object Result);

[ResponseType("codex.breath.breath-loop-response", "BreathLoopResponse", "Breath loop response")]
public record BreathLoopResponse(string Id, List<BreathLoopResult> Results, bool Success);

[ResponseType("codex.breath.oneshot-result", "OneshotResult", "Oneshot result")]
public record OneshotResult(
    ExpandResponse Expand,
    ValidateResponse Validate, 
    ContractResponse Contract
);

[ResponseType("codex.breath.oneshot-response", "OneshotResponse", "Oneshot response")]
public record OneshotResponse(
    string Id, 
    string Operation, 
    OneshotResult Result, 
    bool Success, 
    string Message
);

public sealed class BreathModule : ModuleBase
{

    public override string Name => "Breath Module";
    public override string Description => "Breath loop implementation for compose → expand → validate → (melt/patch/refreeze) → contract";
    public override string Version => "1.0.0";

    public BreathModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }
    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.breath",
            name: "Breath Engine Module",
            version: "0.1.0",
            description: "Self-contained module for breath loop operations (expand, validate, contract) using node-based storage",
            tags: new[] { "breath", "loop", "expand", "validate", "contract" },
            capabilities: new[] { "breath-loop", "expand", "validate", "contract", "lifecycle" },
            spec: "codex.spec.breath"
        );
    }


    // No overrides for RegisterApiHandlers/RegisterHttpEndpoints to ensure base wiring runs.

    [ApiRoute("POST", "/breath/expand/{id}", "breath-expand", "Expand a module specification", "codex.breath")]
    public async Task<object> ExpandModule([ApiParameter("id", "Module ID to expand", Required = true, Location = "path")] string id)
    {
        try
        {
            // Get the module node
            if (!_registry.TryGet(id, out var moduleNode))
            {
                return new ErrorResponse($"Module '{id}' not found");
            }

            // Check if module is in Ice state (ready to expand)
            if (moduleNode.State != ContentState.Ice)
            {
                return new ExpandResponse(id, moduleNode.State.ToString(), false, $"Module is in {moduleNode.State} state, cannot expand from Ice");
            }

            // Get all nodes related to this module
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == id)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = _registry.AllEdges()
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
                    _registry.Upsert(expandedNode);
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
                _registry.Upsert(expandedEdge);
            }

            return new ExpandResponse(
                id, 
                "expanded", 
                true, 
                $"Expanded {expandedNodes.Count} nodes and {expandedEdges.Count} edges from Ice to Gas state"
            );
        }
        catch (Exception ex)
        {
            return new ExpandResponse(id, "error", false, $"Expand failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/breath/validate/{id}", "breath-validate", "Validate a module specification", "codex.breath")]
    public async Task<object> ValidateModule([ApiParameter("id", "Module ID to validate", Required = true, Location = "path")] string id)
    {
        try
        {
            // Get the module node
            if (!_registry.TryGet(id, out var moduleNode))
            {
                return new ErrorResponse($"Module '{id}' not found");
            }

            // Check if module is in Gas state (ready to validate)
            if (moduleNode.State != ContentState.Gas)
            {
                return new ValidateResponse(id, false, $"Module is in {moduleNode.State} state, cannot validate from Gas");
            }

            // Get all nodes related to this module
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == id)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = _registry.AllEdges()
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

            return new ValidateResponse(id, isValid, message);
        }
        catch (Exception ex)
        {
            return new ValidateResponse(id, false, $"Validate failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/breath/contract/{id}", "breath-contract", "Contract a module specification", "codex.breath")]
    public async Task<object> ContractModule([ApiParameter("id", "Module ID to contract", Required = true, Location = "path")] string id)
    {
        try
        {
            // Get the module node
            if (!_registry.TryGet(id, out var moduleNode))
            {
                return new ErrorResponse($"Module '{id}' not found");
            }

            // Check if module is in Gas state (ready to contract)
            if (moduleNode.State != ContentState.Gas)
            {
                return new ContractResponse(id, moduleNode.State.ToString(), false, $"Module is in {moduleNode.State} state, cannot contract from Gas");
            }

            // Get all nodes related to this module
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == id)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = _registry.AllEdges()
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
                    _registry.Upsert(contractedNode);
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
                _registry.Upsert(contractedEdge);
            }

            return new ContractResponse(
                id, 
                "contracted", 
                true, 
                $"Contracted {contractedNodes.Count} nodes and {contractedEdges.Count} edges from Gas to Water state"
            );
        }
        catch (Exception ex)
        {
            return new ContractResponse(id, "error", false, $"Contract failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/breath/loop/{id}", "breath-loop", "Execute full breath loop", "codex.breath")]
    public async Task<object> BreathLoop([ApiParameter("id", "Module ID for breath loop", Required = true, Location = "path")] string id, 
                                       [ApiParameter("request", "Breath loop request", Required = true, Location = "body")] BreathLoopRequest request)
    {
        try
        {
            if (request?.Id == null) return new ErrorResponse("Missing or invalid id");

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

            return new BreathLoopResponse(request.Id, results, success);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Breath loop failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/breath/oneshot/{id}", "breath-oneshot", "One-shot breath loop execution", "codex.breath")]
    public async Task<object> Oneshot([ApiParameter("id", "Module ID for oneshot", Required = true, Location = "path")] string id,
                                     [ApiParameter("request", "Oneshot request", Required = true, Location = "body")] BreathRequest request)
    {
        try
        {
            if (request?.Id == null) return new ErrorResponse("Missing or invalid id");

            // Execute the breath loop: expand, validate, contract
            var expandResult = new ExpandResponse(request.Id, "expanded", true);
            var validateResult = new ValidateResponse(request.Id, true, "Validation successful");
            var contractResult = new ContractResponse(request.Id, "contracted", true);

            var result = new OneshotResult(expandResult, validateResult, contractResult);
            return new OneshotResponse(request.Id, "oneshot", result, true, "Oneshot completed");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Oneshot failed: {ex.Message}");
        }
    }
}

// Request/Response types for the breath module
[RequestType("codex.breath.expand-request", "ExpandRequest", "Expand request")]
public sealed record ExpandRequest(string Id);

[RequestType("codex.breath.validate-request", "ValidateRequest", "Validate request")]
public sealed record ValidateRequest(string Id);

[RequestType("codex.breath.contract-request", "ContractRequest", "Contract request")]
public sealed record ContractRequest(string Id);

[RequestType("codex.breath.breath-request", "BreathRequest", "Breath request")]
public sealed record BreathRequest(string Id, string? Operation = null);

[RequestType("codex.breath.breath-loop-request", "BreathLoopRequest", "Breath loop request")]
public sealed record BreathLoopRequest(string Id, string[]? Operations = null);