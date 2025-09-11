using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Phase module specific response types
public record MeltResponse(string NodeId, bool Success, string Message = "Node melted to water state");
public record RefreezeResponse(string NodeId, bool Success, string Message = "Node refrozen to ice state");
public record ResonanceCheckResponse(bool Ok, string Message, IReadOnlyList<string>? Conflicts = null);

// Phase data structures
public sealed record PhaseChange(
    string NodeId,
    ContentState FromState,
    ContentState ToState,
    DateTime Timestamp,
    string Reason
);

public sealed record ResonanceProposal(
    string NodeId,
    IReadOnlyList<string> Anchors,
    Dictionary<string, object> Changes,
    string Justification
);

public sealed class PhaseModule : IModule
{
    private readonly NodeRegistry _registry;

    public PhaseModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.phase",
            name: "Phase Module",
            version: "0.1.0",
            description: "Module for managing node phase transitions (melt, refreeze) and resonance checking."
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register PhaseChange and ResonanceProposal type definitions as nodes
        var phaseChangeType = new Node(
            Id: "codex.phase/phasechange",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "PhaseChange",
            Description: "Represents a phase transition between node states",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "NodeId", type = "string", required = true },
                        new { name = "FromState", type = "string", required = true },
                        new { name = "ToState", type = "string", required = true },
                        new { name = "Timestamp", type = "datetime", required = true },
                        new { name = "Reason", type = "string", required = true }
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.phase",
                ["typeName"] = "PhaseChange"
            }
        );
        registry.Upsert(phaseChangeType);

        var resonanceProposalType = new Node(
            Id: "codex.phase/resonanceproposal",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "ResonanceProposal",
            Description: "Represents a proposal for changes that need resonance checking",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    fields = new[]
                    {
                        new { name = "NodeId", type = "string", required = true },
                        new { name = "Anchors", type = "array", required = true },
                        new { name = "Changes", type = "object", required = true },
                        new { name = "Justification", type = "string", required = true }
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.phase",
                ["typeName"] = "ResonanceProposal"
            }
        );
        registry.Upsert(resonanceProposalType);

        // Register API nodes for RouteDiscovery
        var meltApi = NodeStorage.CreateApiNode("codex.phase", "melt", "/phase/melt", "Convert an ice node to water state for editing");
        var refreezeApi = NodeStorage.CreateApiNode("codex.phase", "refreeze", "/phase/refreeze", "Convert a water node back to ice state after editing");
        var resonanceApi = NodeStorage.CreateApiNode("codex.phase", "resonance", "/phase/resonance", "Check if proposed changes resonate with anchor nodes");
        
        registry.Upsert(meltApi);
        registry.Upsert(refreezeApi);
        registry.Upsert(resonanceApi);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "melt"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "refreeze"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "resonance"));
    }

    [ApiRoute("POST", "/phase/melt/{id}", "phase-melt", "Convert an ice node to water state for editing", "codex.phase")]
    public async Task<object> MeltNode([ApiParameter("id", "Node ID to melt", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Node ID is required");
            }

            if (!_registry.TryGet(id, out var node))
            {
                return new ErrorResponse($"Node '{id}' not found");
            }

            // Only allow melting ice nodes
            if (node.State != ContentState.Ice)
            {
                return new ErrorResponse($"Cannot melt node in {node.State} state. Only ice nodes can be melted.");
            }

            // Create melted version (ice -> water)
            var meltedNode = node with 
            { 
                State = ContentState.Water,
                Meta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                {
                    ["meltedAt"] = DateTime.UtcNow.ToString("O"),
                    ["meltReason"] = "Manual melt operation",
                    ["originalState"] = "ice"
                }
            };

            _registry.Upsert(meltedNode);

            // Record the phase change
            var phaseChange = new PhaseChange(
                NodeId: id,
                FromState: ContentState.Ice,
                ToState: ContentState.Water,
                Timestamp: DateTime.UtcNow,
                Reason: "Manual melt operation"
            );

            var phaseChangeNode = new Node(
                Id: $"phase-change-{Guid.NewGuid()}",
                TypeId: "codex.phase/change",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Phase Change",
                Description: $"Node {id} melted from ice to water",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(phaseChange, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["nodeId"] = id,
                    ["fromState"] = "ice",
                    ["toState"] = "water",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            );
            _registry.Upsert(phaseChangeNode);

            return new MeltResponse(NodeId: id, Success: true, Message: $"Node {id} successfully melted from ice to water state");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to melt node: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/phase/refreeze/{id}", "phase-refreeze", "Convert a water node back to ice state after editing", "codex.phase")]
    public async Task<object> RefreezeNode([ApiParameter("id", "Node ID to refreeze", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Node ID is required");
            }

            if (!_registry.TryGet(id, out var node))
            {
                return new ErrorResponse($"Node '{id}' not found");
            }

            // Only allow refreezing water nodes
            if (node.State != ContentState.Water)
            {
                return new ErrorResponse($"Cannot refreeze node in {node.State} state. Only water nodes can be refrozen.");
            }

            // Create refrozen version (water -> ice)
            var refrozenNode = node with 
            { 
                State = ContentState.Ice,
                Meta = new Dictionary<string, object>(node.Meta ?? new Dictionary<string, object>())
                {
                    ["refrozenAt"] = DateTime.UtcNow.ToString("O"),
                    ["refreezeReason"] = "Manual refreeze operation",
                    ["originalState"] = "water"
                }
            };

            _registry.Upsert(refrozenNode);

            // Record the phase change
            var phaseChange = new PhaseChange(
                NodeId: id,
                FromState: ContentState.Water,
                ToState: ContentState.Ice,
                Timestamp: DateTime.UtcNow,
                Reason: "Manual refreeze operation"
            );

            var phaseChangeNode = new Node(
                Id: $"phase-change-{Guid.NewGuid()}",
                TypeId: "codex.phase/change",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Phase Change",
                Description: $"Node {id} refrozen from water to ice",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(phaseChange, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["nodeId"] = id,
                    ["fromState"] = "water",
                    ["toState"] = "ice",
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }
            );
            _registry.Upsert(phaseChangeNode);

            return new RefreezeResponse(NodeId: id, Success: true, Message: $"Node {id} successfully refrozen from water to ice state");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to refreeze node: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/resonance/check", "resonance-check", "Check if proposed changes resonate with anchor nodes", "codex.phase")]
    public async Task<object> CheckResonance([ApiParameter("proposal", "Resonance proposal to check", Required = true, Location = "body")] ResonanceProposal proposal)
    {
        try
        {
            if (proposal == null)
            {
                return new ErrorResponse("Resonance proposal is required");
            }

            // Check if the target node exists
            if (!_registry.TryGet(proposal.NodeId, out var targetNode))
            {
                return new ResonanceCheckResponse(Ok: false, Message: $"Target node '{proposal.NodeId}' not found");
            }

            // Check resonance with anchor nodes
            var conflicts = new List<string>();

            foreach (var anchorId in proposal.Anchors)
            {
                if (!_registry.TryGet(anchorId, out var anchorNode))
                {
                    conflicts.Add($"Anchor node '{anchorId}' not found");
                    continue;
                }

                // Simple resonance check: ensure the target node's type is compatible with anchor expectations
                // This is a simplified implementation - in a real system, this would be more sophisticated
                if (anchorNode.TypeId != targetNode.TypeId && !IsCompatibleType(anchorNode.TypeId, targetNode.TypeId))
                {
                    conflicts.Add($"Type mismatch: target node type '{targetNode.TypeId}' is not compatible with anchor node type '{anchorNode.TypeId}'");
                }

                // Check if the proposed changes would break structural integrity
                if (proposal.Changes.ContainsKey("typeId") && proposal.Changes["typeId"]?.ToString() != anchorNode.TypeId)
                {
                    conflicts.Add($"Proposed type change would break compatibility with anchor node '{anchorId}'");
                }
            }

            var isResonant = conflicts.Count == 0;
            var message = isResonant 
                ? "Proposal resonates with all anchor nodes" 
                : $"Proposal conflicts with {conflicts.Count} anchor nodes";

            return new ResonanceCheckResponse(
                Ok: isResonant, 
                Message: message, 
                Conflicts: conflicts
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to check resonance: {ex.Message}");
        }
    }

    private static bool IsCompatibleType(string anchorType, string targetType)
    {
        // Simple compatibility check - in a real system, this would be more sophisticated
        // For now, we'll consider types compatible if they have the same base type or if one is a subtype
        if (anchorType == targetType) return true;
        
        // Check if target is a subtype of anchor (e.g., codex.meta/type is compatible with codex.meta/*)
        if (anchorType.EndsWith("/*") && targetType.StartsWith(anchorType.Replace("/*", "")))
            return true;
            
        // Check if anchor is a subtype of target
        if (targetType.EndsWith("/*") && anchorType.StartsWith(targetType.Replace("/*", "")))
            return true;
            
        return false;
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Phase module uses ApiRoute attributes for endpoint registration
        // No additional API handlers needed
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Phase module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the ApiRoute attributes
    }
}