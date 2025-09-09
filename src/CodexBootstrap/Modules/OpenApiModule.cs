using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

// OpenAPI module specific response types
public record OpenApiGenerationResponse(string ModuleId, bool Success, string Message = "OpenAPI generated successfully", object? OpenApiSpec = null);

// OpenAPI data structures
public sealed record OpenApiInfo(
    string Title,
    string Version,
    string? Description = null
);

public sealed record OpenApiSchema(
    string Type,
    string? Format = null,
    string? Description = null,
    Dictionary<string, OpenApiSchema>? Properties = null,
    OpenApiSchema? Items = null,
    IReadOnlyList<string>? Enum = null,
    IReadOnlyList<object>? Example = null
);

public sealed record OpenApiParameter(
    string Name,
    string In,
    bool Required,
    OpenApiSchema Schema,
    string? Description = null
);

public sealed record OpenApiResponse(
    string Description,
    Dictionary<string, OpenApiSchema>? Content = null
);

public sealed record OpenApiOperation(
    string OperationId,
    IReadOnlyList<string>? Tags = null,
    string? Summary = null,
    string? Description = null,
    IReadOnlyList<OpenApiParameter>? Parameters = null,
    OpenApiSchema? RequestBody = null,
    Dictionary<string, OpenApiResponse>? Responses = null
);

public sealed record OpenApiPathItem(
    Dictionary<string, OpenApiOperation>? Operations = null
);

public sealed record OpenApiDocument(
    string OpenApi,
    OpenApiInfo Info,
    Dictionary<string, OpenApiSchema>? Components = null,
    Dictionary<string, OpenApiPathItem>? Paths = null
);

public sealed class OpenApiModule : IModule, IOpenApiProvider
{
    private readonly NodeRegistry _registry;

    public OpenApiModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.openapi",
            name: "OpenAPI Module",
            version: "0.1.0",
            description: "Module for generating deterministic OpenAPI specifications from module types and APIs."
        );
    }

    public object GetOpenApiSpec()
    {
        var moduleNode = GetModuleNode();
        return OpenApiHelper.GenerateOpenApiSpec("codex.openapi", moduleNode, _registry);
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register OpenApiDocument type definition as node
        var openApiType = new Node(
            Id: "codex.openapi/document",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "OpenApiDocument Type",
            Description: "Represents an OpenAPI 3.0 specification document",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "OpenApiDocument",
                    fields = new[]
                    {
                        new { name = "openApi", type = "string", required = true, description = "OpenAPI version" },
                        new { name = "info", type = "OpenApiInfo", required = true, description = "API information" },
                        new { name = "components", type = "object", required = false, description = "Reusable components" },
                        new { name = "paths", type = "object", required = false, description = "API paths and operations" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.openapi",
                ["typeName"] = "OpenApiDocument"
            }
        );
        registry.Upsert(openApiType);

        // Register API node
        var generateApiNode = new Node(
            Id: "codex.openapi/generate-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Generate OpenAPI API",
            Description: "Generate OpenAPI specification for a module",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "generate",
                    verb = "GET",
                    route = "/openapi/{id}",
                    parameters = new[]
                    {
                        new { name = "id", type = "string", required = true, description = "Module ID to generate OpenAPI for" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.openapi",
                ["apiName"] = "generate"
            }
        );
        registry.Upsert(generateApiNode);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.openapi", "generate"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.openapi", "generate", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return new ErrorResponse("Missing request parameters");
                }

                var moduleId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return new ErrorResponse("Module ID is required");
                }

                // Get the module node
                if (!registry.TryGet(moduleId, out var moduleNode))
                {
                    return new ErrorResponse($"Module '{moduleId}' not found");
                }

                // Generate OpenAPI specification
                var openApiSpec = await GenerateOpenApiSpec(moduleId, moduleNode, registry);

                return new OpenApiGenerationResponse(ModuleId: moduleId, Success: true, OpenApiSpec: openApiSpec);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to generate OpenAPI: {ex.Message}");
            }
        });
    }

    private async Task<OpenApiDocument> GenerateOpenApiSpec(string moduleId, Node moduleNode, NodeRegistry registry)
    {
        var moduleName = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title ?? moduleId;
        var moduleVersion = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0";
        var moduleDescription = moduleNode.Description ?? $"API for {moduleName}";

        var info = new OpenApiInfo(
            Title: moduleName,
            Version: moduleVersion,
            Description: moduleDescription
        );

        var components = new Dictionary<string, OpenApiSchema>();
        var paths = new Dictionary<string, OpenApiPathItem>();

        // Get all API nodes for this module
        var apiNodes = registry.GetNodesByType("codex.meta/api")
            .Where(n => n.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
            .ToList();

        foreach (var apiNode in apiNodes)
        {
            var apiName = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString();
            if (string.IsNullOrEmpty(apiName)) continue;

            // Parse API specification from content
            var apiSpec = await ParseApiSpec(apiNode);
            if (apiSpec == null) continue;

            // Generate OpenAPI operation
            var operation = GenerateOpenApiOperation(apiSpec, components);
            if (operation == null) continue;

            // Generate path
            var path = GenerateOpenApiPath(apiSpec, operation);
            if (path.HasValue)
            {
                paths[path.Value.Key] = path.Value.Value;
            }
        }

        return new OpenApiDocument(
            OpenApi: "3.0.3",
            Info: info,
            Components: components.Any() ? components : null,
            Paths: paths.Any() ? paths : null
        );
    }

    private Task<ApiSpec?> ParseApiSpec(Node apiNode)
    {
        try
        {
            if (apiNode.Content?.InlineJson == null) return Task.FromResult<ApiSpec?>(null);

            var apiData = JsonSerializer.Deserialize<JsonElement>(apiNode.Content.InlineJson);
            
            var name = apiData.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var verb = apiData.TryGetProperty("verb", out var verbElement) ? verbElement.GetString() : null;
            var route = apiData.TryGetProperty("route", out var routeElement) ? routeElement.GetString() : null;
            var description = apiData.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(verb) || string.IsNullOrEmpty(route))
                return Task.FromResult<ApiSpec?>(null);

            // Parse parameters
            var parameters = new List<ParameterSpec>();
            if (apiData.TryGetProperty("parameters", out var paramsElement) && paramsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var paramElement in paramsElement.EnumerateArray())
                {
                    var paramName = paramElement.TryGetProperty("name", out var pn) ? pn.GetString() : null;
                    var paramType = paramElement.TryGetProperty("type", out var pt) ? pt.GetString() : null;
                    var paramRequired = paramElement.TryGetProperty("required", out var pr) ? pr.GetBoolean() : false;
                    var paramDescription = paramElement.TryGetProperty("description", out var pd) ? pd.GetString() : null;

                    if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(paramType))
                    {
                        parameters.Add(new ParameterSpec(
                            Name: paramName,
                            Type: paramType,
                            Required: paramRequired,
                            Description: paramDescription
                        ));
                    }
                }
            }

            return Task.FromResult<ApiSpec?>(new ApiSpec(
                Name: name,
                Verb: verb,
                Route: route,
                Description: description,
                Parameters: parameters.Any() ? parameters : null
            ));
        }
        catch
        {
            return Task.FromResult<ApiSpec?>(null);
        }
    }

    private OpenApiOperation? GenerateOpenApiOperation(ApiSpec apiSpec, Dictionary<string, OpenApiSchema> components)
    {
        var operationId = apiSpec.Name;
        var summary = apiSpec.Description ?? apiSpec.Name;
        var description = apiSpec.Description;

        var parameters = new List<OpenApiParameter>();
        if (apiSpec.Parameters != null)
        {
            foreach (var param in apiSpec.Parameters)
            {
                var schema = GenerateOpenApiSchema(param.Type, components);
                parameters.Add(new OpenApiParameter(
                    Name: param.Name,
                    In: "path", // Assume path parameters for now
                    Required: param.Required,
                    Schema: schema,
                    Description: param.Description
                ));
            }
        }

        var responses = new Dictionary<string, OpenApiResponse>
        {
            ["200"] = new OpenApiResponse("Success", new Dictionary<string, OpenApiSchema>
            {
                ["application/json"] = new OpenApiSchema("object", Description: "Response data")
            })
        };

        return new OpenApiOperation(
            OperationId: operationId,
            Summary: summary,
            Description: description,
            Parameters: parameters.Any() ? parameters : null,
            Responses: responses
        );
    }

    private KeyValuePair<string, OpenApiPathItem>? GenerateOpenApiPath(ApiSpec apiSpec, OpenApiOperation operation)
    {
        // Extract path from route (e.g., "/openapi/{id}" -> "/openapi/{id}")
        var route = apiSpec.Name; // This would need to be extracted from the actual route
        if (string.IsNullOrEmpty(route)) return null;

        var pathItem = new OpenApiPathItem(
            Operations: new Dictionary<string, OpenApiOperation>
            {
                ["get"] = operation // Assume GET for now
            }
        );

        return new KeyValuePair<string, OpenApiPathItem>(route, pathItem);
    }

    private OpenApiSchema GenerateOpenApiSchema(string typeName, Dictionary<string, OpenApiSchema> components)
    {
        // Map primitive types
        switch (typeName.ToLower())
        {
            case "string":
                return new OpenApiSchema("string");
            case "int":
            case "integer":
                return new OpenApiSchema("integer", "int32");
            case "long":
                return new OpenApiSchema("integer", "int64");
            case "float":
            case "double":
                return new OpenApiSchema("number", "double");
            case "bool":
            case "boolean":
                return new OpenApiSchema("boolean");
            case "datetime":
                return new OpenApiSchema("string", "date-time");
            default:
                // For complex types, create a reference
                var refKey = $"#/components/schemas/{typeName}";
                if (!components.ContainsKey(typeName))
                {
                    components[typeName] = new OpenApiSchema("object", Description: $"Schema for {typeName}");
                }
                return new OpenApiSchema("object", Description: $"Reference to {typeName}");
        }
    }
}
