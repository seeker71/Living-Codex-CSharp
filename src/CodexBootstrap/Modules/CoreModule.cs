using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Core module specific response types
[ResponseType("codex.core.atoms-response", "CoreAtomsResponse", "Core atoms response")]
public record CoreAtomsResponse(object Atoms, bool Success, string Message = "Core atoms retrieved successfully");

[ResponseType("codex.core.spec-response", "CoreSpecResponse", "Core spec response")]
public record CoreSpecResponse(object Spec, bool Success, string Message = "Core spec retrieved successfully");

public sealed class CoreModule : ModuleBase
{

    public override string Name => "Core System Module";
    public override string Description => "Core system functionality and atoms management";
    public override string Version => "0.1.0";

    public CoreModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.core",
            name: "Core System Module",
            version: "0.1.0",
            description: "Self-contained module for core system operations (atoms, spec) using node-based storage",
            tags: new[] { "get_atoms", "get_spec", "core_operations" },
            capabilities: new[] { "core", "atoms", "spec", "storage", "system" },
            spec: "codex.spec.core"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // This method is now handled by attribute-based discovery
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // This method is now handled by attribute-based discovery
    }

    [ApiRoute("GET", "/core/atoms", "core-atoms", "Get core atoms", "codex.core")]
    public async Task<object> GetAtoms()
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

            return new CoreAtomsResponse(atoms, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get core atoms: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/core/spec", "core-spec", "Get core spec", "codex.core")]
    public async Task<object> GetSpec()
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

            return new CoreSpecResponse(spec, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get core spec: {ex.Message}");
        }
    }
}
