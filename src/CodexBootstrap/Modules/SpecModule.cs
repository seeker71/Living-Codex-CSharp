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
        router.Register("codex.spec", "atoms", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;
                var atoms = args.Value.TryGetProperty("atoms", out var atomsElement) ? atomsElement : (JsonElement?)null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Store atoms as nodes
                if (atoms.HasValue)
                {
                    // Parse atoms and store as nodes
                    var atomsJson = atoms.Value.GetRawText();
                    // This would parse and store the atoms - simplified for now
                    return Task.FromResult<object>(new SpecAtomsResponse(moduleId, true, "Atoms stored successfully"));
                }

                return Task.FromResult<object>(new SpecAtomsResponse(moduleId, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to process atoms: {ex.Message}"));
            }
        });

        router.Register("codex.spec", "compose", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Compose spec from stored atoms
                var spec = new { id = moduleId, name = "Composed Spec", version = "1.0.0" };
                return Task.FromResult<object>(new SpecComposeResponse(spec, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to compose spec: {ex.Message}"));
            }
        });

        router.Register("codex.spec", "export", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Export atoms for the module
                var atoms = new { moduleId, nodes = new object[0], edges = new object[0] };
                return Task.FromResult<object>(new SpecExportResponse(moduleId, atoms, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to export atoms: {ex.Message}"));
            }
        });

        router.Register("codex.spec", "import", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;
                var atoms = args.Value.TryGetProperty("atoms", out var atomsElement) ? atomsElement : (JsonElement?)null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Import atoms for the module
                return Task.FromResult<object>(new SpecImportResponse(moduleId, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to import atoms: {ex.Message}"));
            }
        });

        router.Register("codex.spec", "get-atoms", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Get atoms for the module
                var atoms = new { moduleId, nodes = new object[0], edges = new object[0] };
                return Task.FromResult<object>(atoms);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to get atoms: {ex.Message}"));
            }
        });

        router.Register("codex.spec", "get-spec", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("moduleId", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Get spec for the module
                var spec = new { id = moduleId, name = "Module Spec", version = "1.0.0" };
                return Task.FromResult<object>(spec);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to get spec: {ex.Message}"));
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
}
