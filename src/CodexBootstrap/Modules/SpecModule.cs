using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Spec module specific response types
public record SpecAtomsResponse(string ModuleId, bool Success, string Message = "Atoms processed successfully");
public record SpecComposeResponse(object Spec, bool Success, string Message = "Spec composed successfully");
public record SpecExportResponse(string ModuleId, object Atoms, bool Success, string Message = "Atoms exported successfully");
public record SpecImportResponse(string ModuleId, bool Success, string Message = "Atoms imported successfully");

public sealed class SpecModule : IModule
{
    private readonly NodeRegistry _registry;

    public SpecModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.spec",
            name: "Spec Management Module",
            version: "0.1.0",
            description: "Self-contained module for spec management operations (atoms, compose, export, import) using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var atomsApi = NodeStorage.CreateApiNode("codex.spec", "atoms", "/spec/atoms", "Submit module atoms");
        var composeApi = NodeStorage.CreateApiNode("codex.spec", "compose", "/spec/compose", "Compose spec from atoms");
        var exportApi = NodeStorage.CreateApiNode("codex.spec", "export", "/spec/export/{id}", "Export module atoms");
        var importApi = NodeStorage.CreateApiNode("codex.spec", "import", "/spec/import", "Import module atoms");
        var getAtomsApi = NodeStorage.CreateApiNode("codex.spec", "get-atoms", "/spec/atoms/{id}", "Get module atoms");
        var getSpecApi = NodeStorage.CreateApiNode("codex.spec", "get-spec", "/spec/{id}", "Get module spec");
        
        registry.Upsert(atomsApi);
        registry.Upsert(composeApi);
        registry.Upsert(exportApi);
        registry.Upsert(importApi);
        registry.Upsert(getAtomsApi);
        registry.Upsert(getSpecApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "compose"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "export"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "import"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-spec"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.spec", "atoms", async args =>
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

                // Store atoms as nodes
                if (atoms.HasValue)
                {
                    await StoreAtomsAsNodes(moduleId, atoms.Value, registry);
                    return new SpecAtomsResponse(moduleId, true, "Atoms stored successfully");
                }

                return new SpecAtomsResponse(moduleId, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to process atoms: {ex.Message}");
            }
        });

        router.Register("codex.spec", "compose", async args =>
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

                // Compose spec from stored atoms
                var spec = await ComposeSpecFromAtoms(moduleId, registry);
                return new SpecComposeResponse(spec, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to compose spec: {ex.Message}");
            }
        });

        router.Register("codex.spec", "export", async args =>
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

                // Export atoms for the module
                var atoms = await ExportModuleAtoms(moduleId, registry);
                return new SpecExportResponse(moduleId, atoms, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to export atoms: {ex.Message}");
            }
        });

        router.Register("codex.spec", "import", async args =>
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

                // Import atoms for the module
                if (atoms.HasValue)
                {
                    await StoreAtomsAsNodes(moduleId, atoms.Value, registry);
                }
                return new SpecImportResponse(moduleId, true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to import atoms: {ex.Message}");
            }
        });

        router.Register("codex.spec", "get-atoms", async args =>
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

                // Get atoms for the module
                var atoms = await ExportModuleAtoms(moduleId, registry);
                return atoms;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to get atoms: {ex.Message}");
            }
        });

        router.Register("codex.spec", "get-spec", async args =>
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

                // Get spec for the module
                var spec = await ComposeSpecFromAtoms(moduleId, registry);
                return spec;
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to get spec: {ex.Message}");
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Spec management endpoints
        app.MapPost("/spec/atoms", (JsonElement request) =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "atoms", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to process atoms: {ex.Message}");
            }
        });

        app.MapPost("/spec/compose", (JsonElement request) =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "compose", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to compose spec: {ex.Message}");
            }
        });

        app.MapGet("/spec/export/{id}", (string id) =>
        {
            try
            {
                var request = JsonSerializer.SerializeToElement(new { moduleId = id });
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "export", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to export atoms: {ex.Message}");
            }
        });

        app.MapPost("/spec/import", (JsonElement request) =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "import", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to import atoms: {ex.Message}");
            }
        });

        app.MapGet("/spec/atoms/{id}", (string id) =>
        {
            try
            {
                var request = JsonSerializer.SerializeToElement(new { moduleId = id });
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "get-atoms", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get atoms: {ex.Message}");
            }
        });

        app.MapGet("/spec/{id}", (string id) =>
        {
            try
            {
                var request = JsonSerializer.SerializeToElement(new { moduleId = id });
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.spec", "get-spec", request));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get spec: {ex.Message}");
            }
        });
    }

    private async Task StoreAtomsAsNodes(string moduleId, JsonElement atoms, NodeRegistry registry)
    {
        try
        {
            // Parse atoms JSON
            var atomsData = JsonSerializer.Deserialize<AtomsData>(atoms.GetRawText());
            if (atomsData == null) return;

            // Store nodes
            foreach (var nodeData in atomsData.Nodes ?? new List<NodeData>())
            {
                var node = new Node(
                    Id: nodeData.Id ?? Guid.NewGuid().ToString(),
                    TypeId: nodeData.TypeId ?? "unknown",
                    State: Enum.TryParse<ContentState>(nodeData.State, out var state) ? state : ContentState.Ice,
                    Locale: nodeData.Locale,
                    Title: nodeData.Title,
                    Description: nodeData.Description,
                    Content: nodeData.Content != null ? new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(nodeData.Content),
                        InlineBytes: null,
                        ExternalUri: null
                    ) : null,
                    Meta: new Dictionary<string, object>
                    {
                        ["moduleId"] = moduleId,
                        ["storedAt"] = DateTime.UtcNow.ToString("O")
                    }
                );
                registry.Upsert(node);
            }

            // Store edges
            foreach (var edgeData in atomsData.Edges ?? new List<EdgeData>())
            {
                var edge = new Edge(
                    FromId: edgeData.FromId ?? "",
                    ToId: edgeData.ToId ?? "",
                    Role: edgeData.Role ?? "related",
                    Weight: null,
                    Meta: null
                );
                registry.Upsert(edge);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to store atoms: {ex.Message}");
        }
    }

    private async Task<object> ComposeSpecFromAtoms(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get module node
            if (!registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Get module's API nodes
            var apiNodes = registry.GetNodesByType("api")
                .Where(api => api.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get module's edges
            var moduleEdges = registry.AllEdges()
                .Where(edge => edge.FromId == moduleId || edge.ToId == moduleId)
                .ToList();

            // Compose spec from actual module data
            var spec = new
            {
                id = moduleId,
                name = moduleNode.Title ?? moduleId,
                version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0",
                description = moduleNode.Description ?? $"Spec for {moduleId}",
                state = moduleNode.State.ToString(),
                apis = apiNodes.Select(api => new
                {
                    name = api.Meta?.GetValueOrDefault("apiName")?.ToString() ?? "unknown",
                    route = api.Meta?.GetValueOrDefault("route")?.ToString(),
                    description = api.Description
                }).ToList(),
                dependencies = moduleEdges
                    .Where(e => e.FromId == moduleId)
                    .Select(e => e.ToId)
                    .ToList(),
                dependents = moduleEdges
                    .Where(e => e.ToId == moduleId)
                    .Select(e => e.FromId)
                    .ToList(),
                composedAt = DateTime.UtcNow.ToString("O")
            };

            return spec;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compose spec: {ex.Message}");
        }
    }

    private async Task<object> ExportModuleAtoms(string moduleId, NodeRegistry registry)
    {
        try
        {
            // Get all nodes related to this module
            var moduleNodes = registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = registry.AllEdges()
                .Where(edge => moduleNodes.Any(n => n.Id == edge.FromId || n.Id == edge.ToId))
                .ToList();

            var atoms = new
            {
                moduleId,
                exportedAt = DateTime.UtcNow.ToString("O"),
                nodes = moduleNodes.Select(node => new
                {
                    id = node.Id,
                    typeId = node.TypeId,
                    state = node.State.ToString(),
                    locale = node.Locale,
                    title = node.Title,
                    description = node.Description,
                    content = node.Content?.InlineJson
                }).ToList(),
                edges = moduleEdges.Select(edge => new
                {
                    fromId = edge.FromId,
                    toId = edge.ToId,
                    role = edge.Role
                }).ToList()
            };

            return atoms;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export atoms: {ex.Message}");
        }
    }

    // Data classes for atoms parsing
    private class AtomsData
    {
        public List<NodeData>? Nodes { get; set; }
        public List<EdgeData>? Edges { get; set; }
    }

    private class NodeData
    {
        public string? Id { get; set; }
        public string? TypeId { get; set; }
        public string? State { get; set; }
        public string? Locale { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public object? Content { get; set; }
    }

    private class EdgeData
    {
        public string? FromId { get; set; }
        public string? ToId { get; set; }
        public string? Role { get; set; }
    }
}
