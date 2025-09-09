using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Core module specific response types
public record CoreAtomsResponse(object Atoms, bool Success, string Message = "Core atoms retrieved successfully");
public record CoreSpecResponse(object Spec, bool Success, string Message = "Core spec retrieved successfully");

public sealed class CoreModule : IModule
{
    private readonly NodeRegistry _registry;

    public CoreModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.core",
            name: "Core System Module",
            version: "0.1.0",
            description: "Self-contained module for core system operations (atoms, spec) using node-based storage"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var atomsApi = NodeStorage.CreateApiNode("codex.core", "atoms", "/core/atoms", "Get core atoms");
        var specApi = NodeStorage.CreateApiNode("codex.core", "spec", "/core/spec", "Get core spec");
        
        registry.Upsert(atomsApi);
        registry.Upsert(specApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.core", "atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.core", "spec"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.core", "atoms", args =>
        {
            try
            {
                // Return core atoms definition
                var atoms = new
                {
                    id = "codex.core",
                    version = "0.1.0",
                    name = "Core",
                    resources = new[]
                    {
                        new
                        {
                            name = "Node",
                            fields = new[]
                            {
                                new { name = "id", type = "string", required = true },
                                new { name = "typeId", type = "string", required = true },
                                new { name = "state", type = "string", required = true }
                            }
                        },
                        new
                        {
                            name = "Edge",
                            fields = new[]
                            {
                                new { name = "fromId", type = "string", required = true },
                                new { name = "toId", type = "string", required = true },
                                new { name = "role", type = "string", required = true }
                            }
                        }
                    },
                    operations = new[]
                    {
                        new { name = "SubmitAtoms", verb = "POST", route = "/spec/atoms" },
                        new { name = "Compose", verb = "POST", route = "/spec/compose" },
                        new { name = "Expand", verb = "POST", route = "/breath/expand/{id}" },
                        new { name = "Validate", verb = "POST", route = "/breath/validate/{id}" },
                        new { name = "Contract", verb = "POST", route = "/breath/contract/{id}" }
                    }
                };

                return Task.FromResult<object>(new CoreAtomsResponse(atoms, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to get core atoms: {ex.Message}"));
            }
        });

        router.Register("codex.core", "spec", args =>
        {
            try
            {
                // Return core spec definition
                var spec = new
                {
                    id = "codex.core",
                    name = "Core System",
                    version = "0.1.0",
                    description = "Core system specification",
                    types = new[]
                    {
                        new
                        {
                            name = "Node",
                            description = "Core node entity",
                            fields = new[]
                            {
                                new { name = "id", type = "string", required = true },
                                new { name = "typeId", type = "string", required = true },
                                new { name = "state", type = "string", required = true }
                            }
                        },
                        new
                        {
                            name = "Edge",
                            description = "Core edge entity",
                            fields = new[]
                            {
                                new { name = "fromId", type = "string", required = true },
                                new { name = "toId", type = "string", required = true },
                                new { name = "role", type = "string", required = true }
                            }
                        }
                    },
                    apis = new[]
                    {
                        new { name = "GetNodes", verb = "GET", route = "/nodes" },
                        new { name = "GetNode", verb = "GET", route = "/nodes/{id}" },
                        new { name = "UpsertNode", verb = "POST", route = "/nodes" },
                        new { name = "GetEdges", verb = "GET", route = "/edges" },
                        new { name = "UpsertEdge", verb = "POST", route = "/edges" }
                    }
                };

                return Task.FromResult<object>(new CoreSpecResponse(spec, true));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to get core spec: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Core system endpoints
        app.MapGet("/core/atoms", () =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.core", "atoms", null));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get core atoms: {ex.Message}");
            }
        });

        app.MapGet("/core/spec", () =>
        {
            try
            {
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.core", "spec", null));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get core spec: {ex.Message}");
            }
        });
    }
}
