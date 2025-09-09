using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Composer module specific response types
public record ComposeResponse(object Spec);

public sealed class ComposerModule : IModule
{
    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.composer",
            name: "Spec Composer Module",
            version: "0.1.0",
            description: "Self-contained module for composing module specifications from atoms using node-based storage"
        );
    }


    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var composeApi = NodeStorage.CreateApiNode("codex.composer", "compose", "/compose", "Compose module specification from atoms");
        var registerAtomsApi = NodeStorage.CreateApiNode("codex.composer", "register-atoms", "/register-atoms", "Register atoms in the core system");
        var registerSpecApi = NodeStorage.CreateApiNode("codex.composer", "register-spec", "/register-spec", "Register composed spec in the core system");
        
        registry.Upsert(composeApi);
        registry.Upsert(registerAtomsApi);
        registry.Upsert(registerSpecApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.composer", "compose"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.composer", "register-atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.composer", "register-spec"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.composer", "compose", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var atoms = JsonSerializer.Deserialize<ModuleAtoms>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (atoms == null) return Task.FromResult<object>(new ErrorResponse("Invalid atoms data"));

                // Store atoms as nodes
                foreach (var node in atoms.Nodes)
                {
                    registry.Upsert(node);
                }
                foreach (var edge in atoms.Edges)
                {
                    registry.Upsert(edge);
                }

                // Create a proper ModuleSpec
                var spec = new ModuleSpec(
                    Id: atoms.Id,
                    Name: $"Module {atoms.Id}",
                    Version: "0.1.0",
                    Description: $"Composed from {atoms.Nodes.Count} nodes",
                    Title: $"Module {atoms.Id}",
                    Dependencies: new List<ModuleRef>(),
                    Types: new List<TypeSpec>(),
                    Apis: new List<ApiSpec>()
                );

                // Store spec as a node
                var specNode = NodeStorage.CreateSpecNode(atoms.Id, $"Module {atoms.Id}", "0.1.0", spec);
                registry.Upsert(specNode);

                return Task.FromResult<object>(new ComposeResponse(spec));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Composition failed: {ex.Message}"));
            }
        });

        router.Register("codex.composer", "register-atoms", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var atoms = JsonSerializer.Deserialize<ModuleAtoms>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (atoms == null) return Task.FromResult<object>(new ErrorResponse("Invalid atoms data"));

                // Store atoms as nodes
                foreach (var node in atoms.Nodes)
                {
                    registry.Upsert(node);
                }
                foreach (var edge in atoms.Edges)
                {
                    registry.Upsert(edge);
                }

                return Task.FromResult<object>(atoms);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Registration failed: {ex.Message}"));
            }
        });

        router.Register("codex.composer", "register-spec", args =>
        {
            if (args is null) return Task.FromResult<object>(new ErrorResponse("Missing request body"));

            try
            {
                var spec = JsonSerializer.Deserialize<object>(args.Value, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (spec == null) return Task.FromResult<object>(new ErrorResponse("Invalid spec data"));

                // Store spec as a node
                var specNode = NodeStorage.CreateSpecNode("spec-" + Guid.NewGuid().ToString("N")[..8], "Spec", "0.1.0", spec);
                registry.Upsert(specNode);

                return Task.FromResult<object>(spec);
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Registration failed: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Composer module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint
    }
}
