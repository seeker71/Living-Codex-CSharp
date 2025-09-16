using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Delta module specific response types
[ResponseType("codex.delta.diff-response", "DiffResponse", "Response for diff operations")]
public record DiffResponse(string SourceId, string TargetId, PatchDoc Patch);

[ResponseType("codex.delta.patch-response", "PatchResponse", "Response for patch operations")]
public record PatchResponse(string TargetId, bool Success, string Message = "Patch applied successfully");

// Delta data structures
[ResponseType("codex.delta.patch-op", "PatchOp", "Patch operation structure")]
public sealed record PatchOp(
    string Op,        // "add", "remove", "replace", "move", "copy", "test"
    string Path,      // JSON pointer path
    object? Value,    // Value for add/replace operations
    string? From      // Source path for move/copy operations
);

[ResponseType("codex.delta.patch-doc", "PatchDoc", "Patch document structure")]
public sealed record PatchDoc(
    string TargetId,
    IReadOnlyList<PatchOp> Ops
);

public sealed class DeltaModule : ModuleBase
{
    public override string Name => "Delta Module";
    public override string Description => "Delta operations and patch management";
    public override string Version => "1.0.0";

    public DeltaModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.delta",
            name: "Delta Module",
            version: "0.1.0",
            description: "Module for git-like patches and diffs on nodes and edges.",
            tags: new[] { "delta", "patch", "diff", "version", "change" },
            capabilities: new[] { "delta", "patch", "diff", "versioning", "git-like" },
            spec: "codex.spec.delta"
        );
    }



    [ApiRoute("GET", "/diff/{id}", "delta-diff", "Generate diff between two nodes", "codex.delta")]
    public async Task<object> DiffNodes([ApiParameter("id", "Source node ID", Required = true, Location = "path")] string id, [ApiParameter("against", "Base node ID to compare against", Required = true, Location = "query")] string against)
    {
        try
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(against))
            {
                return new ErrorResponse("Both 'id' and 'against' parameters are required");
            }

            if (!_registry.TryGet(id, out var sourceNode))
            {
                return new ErrorResponse($"Source node '{id}' not found");
            }

            if (!_registry.TryGet(against, out var againstNode))
            {
                return new ErrorResponse($"Base node '{against}' not found");
            }

            // Generate patch operations by comparing the two nodes
            var ops = await Task.Run(() => GeneratePatchOps(againstNode, sourceNode));

            var patch = new PatchDoc(TargetId: against, Ops: ops);

            return new DiffResponse(SourceId: id, TargetId: against, Patch: patch);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate diff: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/patch/{targetId}", "delta-patch", "Apply patch to target node", "codex.delta")]
    public async Task<object> PatchNode([ApiParameter("targetId", "Target node ID", Required = true, Location = "path")] string targetId, [ApiParameter("patch", "Patch document to apply", Required = true, Location = "body")] PatchDoc patch)
    {
        try
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return new ErrorResponse("Target ID is required");
            }

            if (patch == null)
            {
                return new ErrorResponse("Patch document is required");
            }

            if (!_registry.TryGet(targetId, out var targetNode))
            {
                return new ErrorResponse($"Target node '{targetId}' not found");
            }

            // Apply the patch operations
            var patchedNode = await Task.Run(() => ApplyPatch(targetNode, patch));
            _registry.Upsert(patchedNode);

            return new PatchResponse(TargetId: targetId, Success: true, Message: "Patch applied successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to apply patch: {ex.Message}");
        }
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // Delta module uses ApiRoute attributes for endpoint registration
        // No additional API handlers needed
    }

    private static IReadOnlyList<PatchOp> GeneratePatchOps(Node fromNode, Node toNode)
    {
        var ops = new List<PatchOp>();

        // Compare basic properties
        if (fromNode.TypeId != toNode.TypeId)
        {
            ops.Add(new PatchOp("replace", "/typeId", toNode.TypeId, null));
        }

        if (fromNode.State != toNode.State)
        {
            ops.Add(new PatchOp("replace", "/state", toNode.State.ToString().ToLowerInvariant(), null));
        }

        if (fromNode.Locale != toNode.Locale)
        {
            ops.Add(new PatchOp("replace", "/locale", toNode.Locale, null));
        }

        if (fromNode.Title != toNode.Title)
        {
            ops.Add(new PatchOp("replace", "/title", toNode.Title, null));
        }

        if (fromNode.Description != toNode.Description)
        {
            ops.Add(new PatchOp("replace", "/description", toNode.Description, null));
        }

        // Compare content
        if (!ContentRefEquals(fromNode.Content, toNode.Content))
        {
            ops.Add(new PatchOp("replace", "/content", toNode.Content, null));
        }

        // Compare meta (simplified - just replace the whole meta object)
        if (!MetaEquals(fromNode.Meta, toNode.Meta))
        {
            ops.Add(new PatchOp("replace", "/meta", toNode.Meta, null));
        }

        return ops;
    }

    private static Node ApplyPatch(Node targetNode, PatchDoc patch)
    {
        var patchedNode = targetNode;

        foreach (var op in patch.Ops)
        {
            patchedNode = ApplyPatchOp(patchedNode, op);
        }

        return patchedNode;
    }

    private static Node ApplyPatchOp(Node node, PatchOp op)
    {
        return op.Op switch
        {
            "replace" => ApplyReplaceOp(node, op),
            "add" => ApplyAddOp(node, op),
            "remove" => ApplyRemoveOp(node, op),
            _ => throw new NotSupportedException($"Patch operation '{op.Op}' is not supported")
        };
    }

    private static Node ApplyReplaceOp(Node node, PatchOp op)
    {
        return op.Path switch
        {
            "/typeId" => node with { TypeId = op.Value?.ToString() ?? "" },
            "/state" => node with { State = Enum.Parse<ContentState>(op.Value?.ToString() ?? "Ice", true) },
            "/locale" => node with { Locale = op.Value?.ToString() },
            "/title" => node with { Title = op.Value?.ToString() },
            "/description" => node with { Description = op.Value?.ToString() },
            "/content" => node with { Content = op.Value as ContentRef },
            "/meta" => node with { Meta = op.Value as Dictionary<string, object> },
            _ => throw new NotSupportedException($"Path '{op.Path}' is not supported for replace operation")
        };
    }

    private static Node ApplyAddOp(Node node, PatchOp op)
    {
        // For now, treat add as replace for simplicity
        return ApplyReplaceOp(node, op);
    }

    private static Node ApplyRemoveOp(Node node, PatchOp op)
    {
        return op.Path switch
        {
            "/locale" => node with { Locale = null },
            "/title" => node with { Title = null },
            "/description" => node with { Description = null },
            "/content" => node with { Content = null },
            "/meta" => node with { Meta = null },
            _ => throw new NotSupportedException($"Path '{op.Path}' is not supported for remove operation")
        };
    }

    private static bool ContentRefEquals(ContentRef? a, ContentRef? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        return a.MediaType == b.MediaType &&
               a.InlineJson == b.InlineJson &&
               a.InlineBytes == b.InlineBytes &&
               a.ExternalUri == b.ExternalUri;
    }

    private static bool MetaEquals(Dictionary<string, object>? a, Dictionary<string, object>? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        if (a.Count != b.Count) return false;

        foreach (var kvp in a)
        {
            if (!b.TryGetValue(kvp.Key, out var value) || !Equals(kvp.Value, value))
                return false;
        }

        return true;
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Delta module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint
    }
}
