using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;

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

public sealed class PhaseModule : IModule, IOpenApiProvider
{
    private NodeRegistry? _registry;

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.phase",
            name: "Phase Module",
            version: "0.1.0",
            description: "Module for managing node phase transitions (melt, refreeze) and resonance checking."
        );
    }

    public object GetOpenApiSpec()
    {
        if (_registry == null)
        {
            return new { error = "Registry not available" };
        }

        var moduleNode = GetModuleNode();
        return OpenApiHelper.GenerateOpenApiSpec("codex.phase", moduleNode, _registry);
    }

    public void Register(NodeRegistry registry)
    {
        _registry = registry; // Store registry reference for OpenAPI generation
        
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register PhaseChange and ResonanceProposal type definitions as nodes
        var phaseChangeType = new Node(
            Id: "codex.phase/phasechange",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "PhaseChange Type",
            Description: "Represents a phase transition between node states",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "PhaseChange",
                    fields = new[]
                    {
                        new { name = "nodeId", type = "string", required = true, description = "Node identifier" },
                        new { name = "fromState", type = "string", required = true, description = "Source state" },
                        new { name = "toState", type = "string", required = true, description = "Target state" },
                        new { name = "timestamp", type = "datetime", required = true, description = "When the change occurred" },
                        new { name = "reason", type = "string", required = true, description = "Reason for the change" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
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
            Title: "ResonanceProposal Type",
            Description: "Represents a proposal for structural changes that must pass resonance",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "ResonanceProposal",
                    fields = new[]
                    {
                        new { name = "nodeId", type = "string", required = true, description = "Node identifier" },
                        new { name = "anchors", type = "array", required = true, description = "Resonance anchor node IDs" },
                        new { name = "changes", type = "object", required = true, description = "Proposed changes" },
                        new { name = "justification", type = "string", required = true, description = "Justification for changes" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
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

        // Register API nodes
        var meltApiNode = new Node(
            Id: "codex.phase/melt-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Melt API",
            Description: "Convert an ice node to water state for editing",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "melt",
                    verb = "POST",
                    route = "/phase/melt/{id}",
                    parameters = new[]
                    {
                        new { name = "id", type = "string", required = true, description = "Node ID to melt" },
                        new { name = "reason", type = "string", required = false, description = "Reason for melting" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.phase",
                ["apiName"] = "melt"
            }
        );
        registry.Upsert(meltApiNode);

        var refreezeApiNode = new Node(
            Id: "codex.phase/refreeze-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Refreeze API",
            Description: "Convert a water node back to ice state after editing",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "refreeze",
                    verb = "POST",
                    route = "/phase/refreeze/{id}",
                    parameters = new[]
                    {
                        new { name = "id", type = "string", required = true, description = "Node ID to refreeze" },
                        new { name = "reason", type = "string", required = false, description = "Reason for refreezing" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.phase",
                ["apiName"] = "refreeze"
            }
        );
        registry.Upsert(refreezeApiNode);

        var resonanceApiNode = new Node(
            Id: "codex.phase/resonance-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Resonance Check API",
            Description: "Check if proposed changes resonate with anchor nodes",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "check",
                    verb = "POST",
                    route = "/resonance/check",
                    parameters = new[]
                    {
                        new { name = "proposal", type = "ResonanceProposal", required = true, description = "Resonance proposal to check" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.phase",
                ["apiName"] = "check"
            }
        );
        registry.Upsert(resonanceApiNode);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "melt"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "refreeze"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.phase", "check"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.phase", "melt", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var nodeId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var reason = args.Value.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : "Manual melt operation";

                if (string.IsNullOrEmpty(nodeId))
                {
                    return new ErrorResponse("Node ID is required");
                }

                if (!registry.TryGet(nodeId, out var node))
                {
                    return new ErrorResponse($"Node '{nodeId}' not found");
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
                        ["meltedAt"] = DateTime.UtcNow,
                        ["meltReason"] = reason ?? "Unknown",
                        ["originalState"] = "ice"
                    }
                };

                registry.Upsert(meltedNode);

                // Record the phase change
                await Task.Run(() => {
                var phaseChange = new PhaseChange(
                    NodeId: nodeId,
                    FromState: ContentState.Ice,
                    ToState: ContentState.Water,
                    Timestamp: DateTime.UtcNow,
                    Reason: reason ?? "Unknown"
                );

                var phaseChangeNode = new Node(
                    Id: $"phase-change-{Guid.NewGuid()}",
                    TypeId: "codex.phase/change",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: "Phase Change",
                    Description: $"Node {nodeId} melted from ice to water",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(phaseChange, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["nodeId"] = nodeId,
                        ["fromState"] = "ice",
                        ["toState"] = "water",
                        ["timestamp"] = DateTime.UtcNow
                    }
                );
                registry.Upsert(phaseChangeNode);
                });

                return new MeltResponse(NodeId: nodeId, Success: true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to melt node: {ex.Message}");
            }
        });

        router.Register("codex.phase", "refreeze", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var nodeId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var reason = args.Value.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() : "Manual refreeze operation";

                if (string.IsNullOrEmpty(nodeId))
                {
                    return new ErrorResponse("Node ID is required");
                }

                if (!registry.TryGet(nodeId, out var node))
                {
                    return new ErrorResponse($"Node '{nodeId}' not found");
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
                        ["refrozenAt"] = DateTime.UtcNow,
                        ["refreezeReason"] = reason ?? "Unknown",
                        ["originalState"] = "water"
                    }
                };

                registry.Upsert(refrozenNode);

                // Record the phase change
                var phaseChange = new PhaseChange(
                    NodeId: nodeId,
                    FromState: ContentState.Water,
                    ToState: ContentState.Ice,
                    Timestamp: DateTime.UtcNow,
                    Reason: reason ?? "Unknown"
                );

                var phaseChangeNode = new Node(
                    Id: $"phase-change-{Guid.NewGuid()}",
                    TypeId: "codex.phase/change",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: "Phase Change",
                    Description: $"Node {nodeId} refrozen from water to ice",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(phaseChange, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["nodeId"] = nodeId,
                        ["fromState"] = "water",
                        ["toState"] = "ice",
                        ["timestamp"] = DateTime.UtcNow
                    }
                );
                await Task.Run(() => registry.Upsert(phaseChangeNode));

                return new RefreezeResponse(NodeId: nodeId, Success: true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to refreeze node: {ex.Message}");
            }
        });

        router.Register("codex.phase", "check", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request body");
                }

                var proposalJson = args.Value.TryGetProperty("proposal", out var proposalElement) ? proposalElement.GetRawText() : null;

                if (string.IsNullOrEmpty(proposalJson))
                {
                    return new ErrorResponse("Resonance proposal is required");
                }

                var proposal = JsonSerializer.Deserialize<ResonanceProposal>(proposalJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (proposal == null)
                {
                    return new ErrorResponse("Invalid resonance proposal");
                }

                // Check if the target node exists
                if (!registry.TryGet(proposal.NodeId, out var targetNode))
                {
                    return new ResonanceCheckResponse(Ok: false, Message: $"Target node '{proposal.NodeId}' not found");
                }

                // Check resonance with anchor nodes
                var conflicts = await Task.Run(() => {
                var conflictsList = new List<string>();

                foreach (var anchorId in proposal.Anchors)
                {
                    if (!registry.TryGet(anchorId, out var anchorNode))
                    {
                        conflictsList.Add($"Anchor node '{anchorId}' not found");
                        continue;
                    }

                    // Simple resonance check: ensure the target node's type is compatible with anchor expectations
                    // This is a simplified implementation - in a real system, this would be more sophisticated
                    if (anchorNode.TypeId != targetNode.TypeId && !IsCompatibleType(anchorNode.TypeId, targetNode.TypeId))
                    {
                        conflictsList.Add($"Type mismatch: target node type '{targetNode.TypeId}' is not compatible with anchor node type '{anchorNode.TypeId}'");
                    }

                    // Check if the proposed changes would break structural integrity
                    if (proposal.Changes.ContainsKey("typeId") && proposal.Changes["typeId"]?.ToString() != anchorNode.TypeId)
                    {
                        conflictsList.Add($"Proposed type change would break compatibility with anchor node '{anchorId}'");
                    }
                }

                return conflictsList;
                });

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
        });
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
}
