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

/// <summary>
/// Manages node state transitions between Ice, Water, and Gas states.
/// </summary>
/// <remarks>
/// Does not move edges itself; relies on NodeRegistry edge re-evaluation when nodes settle into new buckets.
/// </remarks>
public sealed class PhaseModule : ModuleBase
{
    public override string Name => "Phase Module";
    public override string Description => "Manages node state transitions between Ice, Water, and Gas states";
    public override string Version => "1.0.0";

    public PhaseModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.phase",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "phase", "transition", "melt", "refreeze", "resonance" },
            capabilities: new[] { "phase-transitions", "melt", "refreeze", "resonance" },
            spec: "codex.spec.phase"
        );
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
                Id: $"codex.phase.change.{Guid.NewGuid():N}",
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
                Id: $"codex.phase.change.{Guid.NewGuid():N}",
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

                // Sophisticated resonance check: multi-dimensional compatibility analysis
                var resonanceScore = CalculateNodeResonance(anchorNode, targetNode, proposal);
                if (resonanceScore < 0.3) // Threshold for acceptable resonance
                {
                    conflicts.Add($"Low resonance ({resonanceScore:F2}) between anchor '{anchorId}' and target '{proposal.NodeId}'. " +
                                $"Anchor type: {anchorNode.TypeId}, Target type: {targetNode.TypeId}");
                }
                
                // Check semantic compatibility beyond just type matching
                if (!IsSemanticCompatible(anchorNode, targetNode))
                {
                    conflicts.Add($"Semantic incompatibility: anchor concept '{GetNodeConcept(anchorNode)}' " +
                                $"conflicts with target concept '{GetNodeConcept(targetNode)}'");
                }
                
                // Validate temporal consistency (if both nodes have temporal metadata)
                if (!IsTemporallyConsistent(anchorNode, targetNode))
                {
                    conflicts.Add($"Temporal inconsistency detected between anchor and target nodes");
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
        // Sophisticated type compatibility checking with hierarchical analysis
        if (anchorType == targetType) return true;

        // Parse type hierarchies
        var anchorParts = anchorType.Split('/');
        var targetParts = targetType.Split('/');
        
        // Check for wildcard compatibility
        if (anchorType.EndsWith("/*") && targetType.StartsWith(anchorType.Replace("/*", "")))
            return true;
        if (targetType.EndsWith("/*") && anchorType.StartsWith(targetType.Replace("/*", "")))
            return true;
            
        // Check for semantic type compatibility
        var compatibilityMap = new Dictionary<string, string[]>
        {
            ["codex.content"] = new[] { "codex.media", "codex.document", "codex.data" },
            ["codex.concept"] = new[] { "codex.idea", "codex.knowledge", "codex.insight" },
            ["codex.user"] = new[] { "codex.person", "codex.agent", "codex.identity" },
            ["codex.system"] = new[] { "codex.process", "codex.workflow", "codex.automation" },
            ["codex.meta"] = new[] { "codex.schema", "codex.template", "codex.structure" }
        };
        
        // Check if types are in the same compatibility group
        foreach (var (baseType, compatibleTypes) in compatibilityMap)
        {
            var anchorBase = GetBaseType(anchorType);
            var targetBase = GetBaseType(targetType);
            
            if ((anchorBase == baseType && compatibleTypes.Contains(targetBase)) ||
                (targetBase == baseType && compatibleTypes.Contains(anchorBase)) ||
                (compatibleTypes.Contains(anchorBase) && compatibleTypes.Contains(targetBase)))
            {
                return true;
            }
        }
        
        // Check hierarchical distance - types are compatible if they share significant common path
        var commonPrefixLength = 0;
        var maxLength = Math.Min(anchorParts.Length, targetParts.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            if (anchorParts[i] == targetParts[i])
                commonPrefixLength++;
            else
                break;
        }
        
        // Compatible if they share at least 2 levels of hierarchy
        return commonPrefixLength >= 2;
    }
    
    private static string GetBaseType(string typeId)
    {
        var parts = typeId.Split('/');
        return parts.Length >= 2 ? $"{parts[0]}/{parts[1]}" : typeId;
    }

    private double CalculateNodeResonance(Node anchorNode, Node targetNode, ResonanceProposal proposal)
    {
        // Multi-dimensional resonance calculation
        var typeResonance = CalculateTypeResonance(anchorNode.TypeId, targetNode.TypeId);
        var semanticResonance = CalculateSemanticResonance(anchorNode, targetNode);
        var structuralResonance = CalculateStructuralResonance(anchorNode, targetNode);
        var temporalResonance = CalculateTemporalResonance(anchorNode, targetNode);
        var axisResonance = CalculateAxisResonance(anchorNode, targetNode);
        
        // Weight different aspects of resonance
        var totalResonance = 
            (typeResonance * 0.3) +        // Type compatibility is important
            (semanticResonance * 0.25) +   // Semantic meaning alignment
            (structuralResonance * 0.2) +  // Structural consistency
            (temporalResonance * 0.15) +   // Temporal consistency
            (axisResonance * 0.1);         // Axis alignment
            
        return Math.Max(0.0, Math.Min(1.0, totalResonance));
    }

    private double CalculateTypeResonance(string anchorType, string targetType)
    {
        if (anchorType == targetType) return 1.0;
        if (IsCompatibleType(anchorType, targetType)) return 0.8;
        
        // Check type hierarchy similarity
        var anchorParts = anchorType.Split('/');
        var targetParts = targetType.Split('/');
        
        var commonPrefixLength = 0;
        var maxLength = Math.Min(anchorParts.Length, targetParts.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            if (anchorParts[i] == targetParts[i])
                commonPrefixLength++;
            else
                break;
        }
        
        return commonPrefixLength > 0 ? (double)commonPrefixLength / Math.Max(anchorParts.Length, targetParts.Length) : 0.1;
    }

    private double CalculateSemanticResonance(Node anchorNode, Node targetNode)
    {
        var anchorConcept = GetNodeConcept(anchorNode);
        var targetConcept = GetNodeConcept(targetNode);
        
        // Simple semantic similarity based on concept overlap
        if (anchorConcept == targetConcept) return 1.0;
        
        // Check for concept intersection
        var anchorWords = anchorConcept.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var targetWords = targetConcept.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var intersection = anchorWords.Intersect(targetWords).Count();
        var union = anchorWords.Union(targetWords).Count();
        
        return union > 0 ? (double)intersection / union : 0.0;
    }

    private double CalculateStructuralResonance(Node anchorNode, Node targetNode)
    {
        // Compare structural properties like content presence, meta complexity
        var anchorComplexity = CalculateNodeComplexity(anchorNode);
        var targetComplexity = CalculateNodeComplexity(targetNode);
        
        // Nodes with similar structural complexity have higher resonance
        var complexityDiff = Math.Abs(anchorComplexity - targetComplexity);
        return Math.Max(0.0, 1.0 - (complexityDiff / 10.0)); // Scale complexity difference
    }

    private double CalculateTemporalResonance(Node anchorNode, Node targetNode)
    {
        if (!IsTemporallyConsistent(anchorNode, targetNode)) return 0.0;
        
        // If both nodes have temporal metadata, check temporal proximity
        var anchorTime = GetNodeTimestamp(anchorNode);
        var targetTime = GetNodeTimestamp(targetNode);
        
        if (anchorTime.HasValue && targetTime.HasValue)
        {
            var timeDiff = Math.Abs((anchorTime.Value - targetTime.Value).TotalDays);
            return Math.Max(0.0, 1.0 - (timeDiff / 365.0)); // Year-based temporal decay
        }
        
        return 0.5; // Neutral if temporal data is missing
    }

    private double CalculateAxisResonance(Node anchorNode, Node targetNode)
    {
        var anchorAxes = GetNodeAxes(anchorNode);
        var targetAxes = GetNodeAxes(targetNode);
        
        if (anchorAxes.Length == 0 && targetAxes.Length == 0) return 0.5;
        if (anchorAxes.Length == 0 || targetAxes.Length == 0) return 0.3;
        
        var intersection = anchorAxes.Intersect(targetAxes).Count();
        var union = anchorAxes.Union(targetAxes).Count();
        
        return union > 0 ? (double)intersection / union : 0.0;
    }

    private bool IsSemanticCompatible(Node anchorNode, Node targetNode)
    {
        // Check for explicit semantic conflicts
        var anchorConcept = GetNodeConcept(anchorNode).ToLower();
        var targetConcept = GetNodeConcept(targetNode).ToLower();
        
        // Define conflicting concept pairs
        var conflicts = new[]
        {
            ("positive", "negative"),
            ("abstract", "concrete"),
            ("static", "dynamic"),
            ("individual", "collective"),
            ("internal", "external")
        };
        
        foreach (var (concept1, concept2) in conflicts)
        {
            if ((anchorConcept.Contains(concept1) && targetConcept.Contains(concept2)) ||
                (anchorConcept.Contains(concept2) && targetConcept.Contains(concept1)))
            {
                return false;
            }
        }
        
        return true;
    }

    private bool IsTemporallyConsistent(Node anchorNode, Node targetNode)
    {
        var anchorTime = GetNodeTimestamp(anchorNode);
        var targetTime = GetNodeTimestamp(targetNode);
        
        // If both have timestamps, check for temporal paradoxes
        if (anchorTime.HasValue && targetTime.HasValue)
        {
            // Check if there are any temporal ordering requirements
            var anchorRequiresAfter = anchorNode.Meta?.ContainsKey("requiresAfter") == true;
            var targetRequiresBefore = targetNode.Meta?.ContainsKey("requiresBefore") == true;
            
            if (anchorRequiresAfter && anchorTime > targetTime) return false;
            if (targetRequiresBefore && targetTime < anchorTime) return false;
        }
        
        return true;
    }

    private string GetNodeConcept(Node node)
    {
        // Extract conceptual information from node
        if (node.Meta?.ContainsKey("concept") == true)
            return node.Meta["concept"].ToString() ?? "";
            
        // Fallback to title or description
        return node.Title ?? node.Description ?? node.TypeId;
    }

    private DateTime? GetNodeTimestamp(Node node)
    {
        if (node.Meta?.ContainsKey("timestamp") == true)
        {
            if (DateTime.TryParse(node.Meta["timestamp"].ToString(), out var timestamp))
                return timestamp;
        }
        
        if (node.Meta?.ContainsKey("createdAt") == true)
        {
            if (DateTime.TryParse(node.Meta["createdAt"].ToString(), out var createdAt))
                return createdAt;
        }
        
        return null;
    }

    private string[] GetNodeAxes(Node node)
    {
        if (node.Meta?.ContainsKey("axes") == true)
        {
            if (node.Meta["axes"] is string[] axes)
                return axes;
            if (node.Meta["axes"] is string axesString)
                return axesString.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
        
        return Array.Empty<string>();
    }

    private double CalculateNodeComplexity(Node node)
    {
        var complexity = 0.0;
        
        // Content complexity
        if (node.Content != null)
        {
            complexity += !string.IsNullOrEmpty(node.Content.InlineJson) ? 2.0 : 0.0;
            complexity += node.Content.InlineBytes?.Length > 0 ? 3.0 : 0.0;
            complexity += node.Content.ExternalUri != null ? 1.0 : 0.0;
        }
        
        // Metadata complexity
        complexity += (node.Meta?.Count ?? 0) * 0.5;
        
        // Text content complexity
        complexity += (node.Title?.Length ?? 0) / 100.0;
        complexity += (node.Description?.Length ?? 0) / 200.0;
        
        return complexity;
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // Phase module uses ApiRoute attributes for endpoint registration
        // No additional API handlers needed
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Phase module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the ApiRoute attributes
    }
}
