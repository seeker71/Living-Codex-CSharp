using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Spec module specific response types
[ResponseType("codex.spec.atoms-response", "SpecAtomsResponse", "Spec atoms response")]
public record SpecAtomsResponse(string ModuleId, bool Success, string Message = "Atoms processed successfully");

[ResponseType("codex.spec.compose-response", "SpecComposeResponse", "Spec compose response")]
public record SpecComposeResponse(object Spec, bool Success, string Message = "Spec composed successfully");

[ResponseType("codex.spec.export-response", "SpecExportResponse", "Spec export response")]
public record SpecExportResponse(string ModuleId, object Atoms, bool Success, string Message = "Atoms exported successfully");

[ResponseType("codex.spec.import-response", "SpecImportResponse", "Spec import response")]
public record SpecImportResponse(string ModuleId, bool Success, string Message = "Atoms imported successfully");

public sealed class SpecModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly Core.ILogger _logger;

    public SpecModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _logger = new Log4NetLogger(typeof(SpecModule));
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
        // This method is now handled by attribute-based discovery
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // This method is now handled by attribute-based discovery
    }

    [ApiRoute("POST", "/spec/atoms", "spec-atoms", "Submit module atoms", "codex.spec")]
    public async Task<object> SubmitAtoms([ApiParameter("request", "Atoms submission request", Required = true, Location = "body")] SpecAtomsRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Store atoms as nodes
            if (request.Atoms.HasValue)
            {
                await StoreAtomsAsNodes(request.ModuleId, request.Atoms.Value);
                return new SpecAtomsResponse(request.ModuleId, true, "Atoms stored successfully");
            }

            return new SpecAtomsResponse(request.ModuleId, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to process atoms: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/spec/compose", "spec-compose", "Compose spec from atoms", "codex.spec")]
    public async Task<object> ComposeSpec([ApiParameter("request", "Spec composition request", Required = true, Location = "body")] SpecComposeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Compose spec from stored atoms
            var spec = await ComposeSpecFromAtoms(request.ModuleId);
            return new SpecComposeResponse(spec, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to compose spec: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/export/{id}", "spec-export", "Export module atoms", "codex.spec")]
    public async Task<object> ExportAtoms([ApiParameter("id", "Module ID to export", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Export atoms for the module
            var atoms = await ExportModuleAtoms(id);
            return new SpecExportResponse(id, atoms, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to export atoms: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/spec/import", "spec-import", "Import module atoms", "codex.spec")]
    public async Task<object> ImportAtoms([ApiParameter("request", "Atoms import request", Required = true, Location = "body")] SpecImportRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Import atoms for the module
            if (request.Atoms != null && request.Atoms.Value.ValueKind != JsonValueKind.Null)
            {
                await StoreAtomsAsNodes(request.ModuleId, request.Atoms.Value);
            }
            else
            {
                return new ErrorResponse("Atoms data is required for import");
            }
            return new SpecImportResponse(request.ModuleId, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to import atoms: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/atoms/{id}", "spec-get-atoms", "Get module atoms", "codex.spec")]
    public async Task<object> GetAtoms([ApiParameter("id", "Module ID to get atoms for", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Get atoms for the module
            var atoms = await ExportModuleAtoms(id);
            return atoms;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get atoms: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/{id}", "spec-get-spec", "Get module spec", "codex.spec")]
    public async Task<object> GetSpec([ApiParameter("id", "Module ID to get spec for", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Get spec for the module
            var spec = await ComposeSpecFromAtoms(id);
            return spec;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get spec: {ex.Message}");
        }
    }

    private Task StoreAtomsAsNodes(string moduleId, JsonElement atoms)
    {
        try
        {
            _logger.Info($"StoreAtomsAsNodes called for moduleId: {moduleId}");
            _logger.Debug($"Atoms JSON: {atoms.GetRawText()}");
            
            // Parse atoms JSON
            var atomsData = JsonSerializer.Deserialize<AtomsData>(atoms.GetRawText());
            if (atomsData == null) 
            {
                _logger.Warn("AtomsData is null after deserialization");
                return Task.CompletedTask;
            }
            
            _logger.Info($"Parsed {atomsData.Nodes?.Count ?? 0} nodes and {atomsData.Edges?.Count ?? 0} edges");

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
                _registry.Upsert(node);
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
                _registry.Upsert(edge);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to store atoms: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    private Task<object> ComposeSpecFromAtoms(string moduleId)
    {
        try
        {
            // Get module node
            if (!_registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Get module's API nodes
            var apiNodes = _registry.GetNodesByType("api")
                .Where(api => api.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get module's edges
            var moduleEdges = _registry.AllEdges()
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

            return Task.FromResult<object>(spec);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compose spec: {ex.Message}");
        }
    }

    private Task<object> ExportModuleAtoms(string moduleId)
    {
        try
        {
            // Get all nodes related to this module
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = _registry.AllEdges()
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

            return Task.FromResult<object>(atoms);
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

// Request types for the spec module
[RequestType("codex.spec.atoms-request", "SpecAtomsRequest", "Spec atoms request")]
public sealed record SpecAtomsRequest(string ModuleId, JsonElement? Atoms = null);

[RequestType("codex.spec.compose-request", "SpecComposeRequest", "Spec compose request")]
public sealed record SpecComposeRequest(string ModuleId);

[RequestType("codex.spec.import-request", "SpecImportRequest", "Spec import request")]
public sealed record SpecImportRequest(string ModuleId, JsonElement? Atoms = null);
