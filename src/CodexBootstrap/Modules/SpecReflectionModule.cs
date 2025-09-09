using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

// Spec Reflection module specific response types
public record ReflectResponse(List<Node> MetaNodes, string SpecId);

public record IngestResponse(ModuleSpec Spec, bool Success, string Message);

public sealed class SpecReflectionModule : IModule
{
    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.reflect",
            name: "Spec Reflection Module",
            version: "0.1.0",
            description: "Converts specs to meta-nodes and back, enabling self-describing system architecture"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register API nodes for reflection endpoints
        RegisterApiNodes(registry);
    }

    private static void RegisterApiNodes(NodeRegistry registry)
    {
        // Reflect spec endpoint
        var reflectApiNode = new Node(
            Id: Guid.NewGuid().ToString(),
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Reflect Spec API",
            Description: "Converts a module spec to meta-nodes",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    verb = "GET",
                    route = "/reflect/spec/{id}",
                    description = "Reflects a module spec to meta-nodes",
                    parameters = new[] { new { name = "id", type = "string", required = true } }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.reflect",
                ["apiName"] = "reflect",
                ["route"] = "/reflect/spec/{id}",
                ["verb"] = "GET"
            }
        );
        registry.Upsert(reflectApiNode);

        // Ingest spec endpoint
        var ingestApiNode = new Node(
            Id: Guid.NewGuid().ToString(),
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Ingest Spec API",
            Description: "Builds a module spec from meta-nodes",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    verb = "POST",
                    route = "/ingest/spec",
                    description = "Ingests meta-nodes to build a module spec",
                    parameters = new[] { new { name = "metaNodes", type = "array", required = true } }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.reflect",
                ["apiName"] = "ingest",
                ["route"] = "/ingest/spec",
                ["verb"] = "POST"
            }
        );
        registry.Upsert(ingestApiNode);
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.reflect", "reflect", async args =>
        {
            try
            {
                var specId = args?.GetProperty("id").GetString();
                if (string.IsNullOrEmpty(specId))
                {
                    return new ErrorResponse("Spec ID is required");
                }

                // Find the spec node
                var specNode = registry.GetNodesByType("codex.meta/spec")
                    .FirstOrDefault(n => n.Id == specId || n.Meta?.GetValueOrDefault("specId")?.ToString() == specId);

                if (specNode == null)
                {
                    return new ErrorResponse($"Spec with ID '{specId}' not found");
                }

                // Convert spec to meta-nodes
                var metaNodes = ReflectSpecToMetaNodes(specNode, registry);
                
                return new ReflectResponse(metaNodes, specId);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to reflect spec: {ex.Message}");
            }
        });

        router.Register("codex.reflect", "ingest", async args =>
        {
            try
            {
                if (args == null)
                {
                    return new ErrorResponse("Meta nodes are required");
                }

                var metaNodesJson = args?.GetProperty("metaNodes").GetRawText();
                if (string.IsNullOrEmpty(metaNodesJson))
                {
                    return new ErrorResponse("Meta nodes JSON is required");
                }
                
                var metaNodes = JsonSerializer.Deserialize<List<Node>>(metaNodesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                if (metaNodes == null || !metaNodes.Any())
                {
                    return new ErrorResponse("No meta nodes provided");
                }

                // Convert meta-nodes back to spec
                var spec = IngestMetaNodesToSpec(metaNodes);
                
                return new IngestResponse(spec, true, "Successfully ingested meta-nodes to spec");
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to ingest meta-nodes: {ex.Message}");
            }
        });
    }

    private static List<Node> ReflectSpecToMetaNodes(Node specNode, NodeRegistry registry)
    {
        var metaNodes = new List<Node>();

        try
        {
            // Parse the spec content
            var specContent = specNode.Content?.InlineJson;
            if (string.IsNullOrEmpty(specContent))
            {
                return metaNodes;
            }

            var spec = JsonSerializer.Deserialize<ModuleSpec>(specContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (spec == null)
            {
                return metaNodes;
            }

            // Create meta-module node
            var metaModuleNode = new Node(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.meta/module",
                State: ContentState.Ice,
                Locale: "en",
                Title: $"Meta Module: {spec.Name}",
                Description: $"Meta-node representation of module {spec.Name}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        id = spec.Id,
                        name = spec.Name,
                        version = spec.Version,
                        description = spec.Description,
                        title = spec.Title
                    }, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["specId"] = specNode.Id,
                    ["moduleId"] = spec.Id,
                    ["name"] = spec.Name,
                    ["version"] = spec.Version
                }
            );
            metaNodes.Add(metaModuleNode);

            // Create meta-type nodes for each type
            if (spec.Types != null)
            {
                foreach (var type in spec.Types)
                {
                    var metaTypeNode = new Node(
                        Id: Guid.NewGuid().ToString(),
                        TypeId: "codex.meta/type",
                        State: ContentState.Ice,
                        Locale: "en",
                        Title: $"Meta Type: {type.Name}",
                        Description: $"Meta-node representation of type {type.Name}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new
                            {
                                name = type.Name,
                                description = type.Description,
                                fields = type.Fields
                            }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["specId"] = specNode.Id,
                            ["moduleId"] = spec.Id,
                            ["typeName"] = type.Name,
                            ["parentModule"] = spec.Id
                        }
                    );
                    metaNodes.Add(metaTypeNode);

                    // Create meta-property nodes for each field
                    if (type.Fields != null)
                    {
                        foreach (var field in type.Fields)
                        {
                            var metaPropertyNode = new Node(
                                Id: Guid.NewGuid().ToString(),
                                TypeId: "codex.meta/property",
                                State: ContentState.Ice,
                                Locale: "en",
                                Title: $"Meta Property: {field.Name}",
                                Description: $"Meta-node representation of property {field.Name}",
                                Content: new ContentRef(
                                    MediaType: "application/json",
                                    InlineJson: JsonSerializer.Serialize(new
                                    {
                                        name = field.Name,
                                        type = field.Type,
                                        required = field.Required,
                                        description = field.Description
                                    }),
                                    InlineBytes: null,
                                    ExternalUri: null
                                ),
                                Meta: new Dictionary<string, object>
                                {
                                    ["specId"] = specNode.Id,
                                    ["moduleId"] = spec.Id,
                                    ["typeName"] = type.Name,
                                    ["propertyName"] = field.Name,
                                    ["parentType"] = type.Name
                                }
                            );
                            metaNodes.Add(metaPropertyNode);
                        }
                    }
                }
            }

            // Create meta-api nodes for each API
            if (spec.Apis != null)
            {
                foreach (var api in spec.Apis)
                {
                    var metaApiNode = new Node(
                        Id: Guid.NewGuid().ToString(),
                        TypeId: "codex.meta/api",
                        State: ContentState.Ice,
                        Locale: "en",
                        Title: $"Meta API: {api.Name}",
                        Description: $"Meta-node representation of API {api.Name}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new
                            {
                                name = api.Name,
                                verb = api.Verb,
                                route = api.Route,
                                description = api.Description,
                                parameters = api.Parameters
                            }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["specId"] = specNode.Id,
                            ["moduleId"] = spec.Id,
                            ["apiName"] = api.Name,
                            ["parentModule"] = spec.Id
                        }
                    );
                    metaNodes.Add(metaApiNode);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but return partial results
            Console.WriteLine($"Error reflecting spec to meta-nodes: {ex.Message}");
        }

        return metaNodes;
    }

    private static ModuleSpec IngestMetaNodesToSpec(List<Node> metaNodes)
    {
        try
        {
            // Find the meta-module node
            var metaModuleNode = metaNodes.FirstOrDefault(n => n.TypeId == "codex.meta/module");
            if (metaModuleNode == null)
            {
                throw new InvalidOperationException("No meta-module node found");
            }

            // Parse module info
            var moduleContent = JsonSerializer.Deserialize<JsonElement>(metaModuleNode.Content?.InlineJson ?? "{}");
            var moduleId = moduleContent.GetProperty("id").GetString() ?? "unknown";
            var moduleName = moduleContent.GetProperty("name").GetString() ?? "Unknown";
            var moduleVersion = moduleContent.GetProperty("version").GetString() ?? "0.1.0";
            var moduleDescription = moduleContent.TryGetProperty("description", out var desc) ? desc.GetString() : null;
            var moduleTitle = moduleContent.TryGetProperty("title", out var title) ? title.GetString() : null;

            // Find meta-type nodes
            var metaTypeNodes = metaNodes.Where(n => n.TypeId == "codex.meta/type").ToList();
            var types = new List<TypeSpec>();

            foreach (var metaTypeNode in metaTypeNodes)
            {
                var typeContent = JsonSerializer.Deserialize<JsonElement>(metaTypeNode.Content?.InlineJson ?? "{}");
                var typeName = typeContent.GetProperty("name").GetString() ?? "Unknown";
                var typeDescription = typeContent.TryGetProperty("description", out var typeDesc) ? typeDesc.GetString() : null;

                // Find properties for this type
                var propertyNodes = metaNodes.Where(n => 
                    n.TypeId == "codex.meta/property" && 
                    n.Meta?.GetValueOrDefault("typeName")?.ToString() == typeName).ToList();

                var fields = new List<FieldSpec>();
                foreach (var propNode in propertyNodes)
                {
                    var propContent = JsonSerializer.Deserialize<JsonElement>(propNode.Content?.InlineJson ?? "{}");
                    var fieldName = propContent.GetProperty("name").GetString() ?? "unknown";
                    var fieldType = propContent.GetProperty("type").GetString() ?? "string";
                    var fieldRequired = propContent.TryGetProperty("required", out var req) && req.GetBoolean();
                    var fieldDescription = propContent.TryGetProperty("description", out var fieldDesc) ? fieldDesc.GetString() : null;

                    fields.Add(new FieldSpec(fieldName, fieldType, fieldRequired, fieldDescription));
                }

                types.Add(new TypeSpec(typeName, typeDescription, fields));
            }

            // Find meta-api nodes
            var metaApiNodes = metaNodes.Where(n => n.TypeId == "codex.meta/api").ToList();
            var apis = new List<ApiSpec>();

            foreach (var metaApiNode in metaApiNodes)
            {
                var apiContent = JsonSerializer.Deserialize<JsonElement>(metaApiNode.Content?.InlineJson ?? "{}");
                var apiName = apiContent.GetProperty("name").GetString() ?? "unknown";
                var apiVerb = apiContent.GetProperty("verb").GetString() ?? "GET";
                var apiRoute = apiContent.GetProperty("route").GetString() ?? "/";
                var apiDescription = apiContent.TryGetProperty("description", out var apiDesc) ? apiDesc.GetString() : null;

                // Parse parameters if they exist
                var parameters = new List<ParameterSpec>();
                if (apiContent.TryGetProperty("parameters", out var paramsElement))
                {
                    foreach (var paramElement in paramsElement.EnumerateArray())
                    {
                        var paramName = paramElement.GetProperty("name").GetString() ?? "param";
                        var paramType = paramElement.GetProperty("type").GetString() ?? "string";
                        var paramRequired = paramElement.TryGetProperty("required", out var paramReq) && paramReq.GetBoolean();
                        var paramDescription = paramElement.TryGetProperty("description", out var paramDesc) ? paramDesc.GetString() : null;

                        parameters.Add(new ParameterSpec(paramName, paramType, paramRequired, paramDescription));
                    }
                }

                apis.Add(new ApiSpec(apiName, apiVerb, apiRoute, apiDescription, parameters));
            }

            return new ModuleSpec(
                Id: moduleId,
                Name: moduleName,
                Version: moduleVersion,
                Description: moduleDescription,
                Title: moduleTitle,
                Dependencies: new List<ModuleRef>(), // Could be extracted from meta-nodes if needed
                Types: types,
                Apis: apis
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to ingest meta-nodes to spec: {ex.Message}", ex);
        }
    }
}
