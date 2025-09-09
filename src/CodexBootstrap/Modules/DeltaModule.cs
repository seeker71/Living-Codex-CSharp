using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

// Delta module specific response types
public record DiffResponse(string SourceId, string TargetId, PatchDoc Patch);
public record PatchResponse(string TargetId, bool Success, string Message = "Patch applied successfully");

// Delta data structures
public sealed record PatchOp(
    string Op,        // "add", "remove", "replace", "move", "copy", "test"
    string Path,      // JSON pointer path
    object? Value,    // Value for add/replace operations
    string? From      // Source path for move/copy operations
);

public sealed record PatchDoc(
    string TargetId,
    IReadOnlyList<PatchOp> Ops
);

public sealed class DeltaModule : IModule
{
    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.delta",
            name: "Delta Module",
            version: "0.1.0",
            description: "Module for git-like patches and diffs on nodes and edges."
        );
    }


    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register PatchOp and PatchDoc type definitions as nodes
        var patchOpType = new Node(
            Id: "codex.delta/patchop",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "PatchOp Type",
            Description: "Represents a single patch operation",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "PatchOp",
                    fields = new[]
                    {
                        new { name = "op", type = "string", required = true, description = "Operation type" },
                        new { name = "path", type = "string", required = true, description = "JSON pointer path" },
                        new { name = "value", type = "object", required = false, description = "Value for add/replace" },
                        new { name = "from", type = "string", required = false, description = "Source path for move/copy" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.delta",
                ["typeName"] = "PatchOp"
            }
        );
        registry.Upsert(patchOpType);

        var patchDocType = new Node(
            Id: "codex.delta/patchdoc",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "PatchDoc Type",
            Description: "Represents a complete patch document",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "PatchDoc",
                    fields = new[]
                    {
                        new { name = "targetId", type = "string", required = true, description = "Target node ID" },
                        new { name = "ops", type = "array", required = true, description = "List of patch operations" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.delta",
                ["typeName"] = "PatchDoc"
            }
        );
        registry.Upsert(patchDocType);

        // Register API nodes
        var diffApiNode = new Node(
            Id: "codex.delta/diff-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Diff API",
            Description: "Compare two nodes and generate a patch",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "diff",
                    verb = "GET",
                    route = "/diff/{id}",
                    parameters = new[]
                    {
                        new { name = "id", type = "string", required = true, description = "Source node ID" },
                        new { name = "against", type = "string", required = true, description = "Base node ID to compare against" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.delta",
                ["apiName"] = "diff"
            }
        );
        registry.Upsert(diffApiNode);

        var patchApiNode = new Node(
            Id: "codex.delta/patch-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Patch API",
            Description: "Apply a patch to a target node",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "patch",
                    verb = "POST",
                    route = "/patch/{targetId}",
                    parameters = new[]
                    {
                        new { name = "targetId", type = "string", required = true, description = "Target node ID" },
                        new { name = "patch", type = "PatchDoc", required = true, description = "Patch document to apply" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.delta",
                ["apiName"] = "patch"
            }
        );
        registry.Upsert(patchApiNode);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.delta", "diff"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.delta", "patch"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.delta", "diff", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var sourceId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var againstId = args.Value.TryGetProperty("against", out var againstElement) ? againstElement.GetString() : null;

                if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(againstId))
                {
                    return new ErrorResponse("Both 'id' and 'against' parameters are required");
                }

                if (!registry.TryGet(sourceId, out var sourceNode))
                {
                    return new ErrorResponse($"Source node '{sourceId}' not found");
                }

                if (!registry.TryGet(againstId, out var againstNode))
                {
                    return new ErrorResponse($"Base node '{againstId}' not found");
                }

                // Generate patch operations by comparing the two nodes
                var ops = await Task.Run(() => GeneratePatchOps(againstNode, sourceNode));

                var patch = new PatchDoc(TargetId: againstId, Ops: ops);

                return new DiffResponse(SourceId: sourceId, TargetId: againstId, Patch: patch);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to generate diff: {ex.Message}");
            }
        });

        router.Register("codex.delta", "patch", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request body");
                }

                var targetId = args.Value.TryGetProperty("targetId", out var targetElement) ? targetElement.GetString() : null;
                var patchJson = args.Value.TryGetProperty("patch", out var patchElement) ? patchElement.GetRawText() : null;

                if (string.IsNullOrEmpty(targetId) || string.IsNullOrEmpty(patchJson))
                {
                    return new ErrorResponse("Both 'targetId' and 'patch' are required");
                }

                var patch = JsonSerializer.Deserialize<PatchDoc>(patchJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (patch == null)
                {
                    return new ErrorResponse("Invalid patch document");
                }

                if (!registry.TryGet(targetId, out var targetNode))
                {
                    return new ErrorResponse($"Target node '{targetId}' not found");
                }

                // Apply the patch operations
                var patchedNode = await Task.Run(() => ApplyPatch(targetNode, patch));
                registry.Upsert(patchedNode);

                return new PatchResponse(TargetId: targetId, Success: true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to apply patch: {ex.Message}");
            }
        });
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
}
