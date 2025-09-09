using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

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

public sealed class OpenApiModule : IModule
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

    public object GetOpenApiSpec(string moduleId)
    {
        // Get the module node to find the module instance
        if (!_registry.TryGet(moduleId, out var moduleNode))
        {
            return new { error = $"Module '{moduleId}' not found" };
        }

        // Check if OpenAPI spec already exists as a node
        var existingSpecId = $"openapi-spec-{moduleId}";
        if (_registry.TryGet(existingSpecId, out var existingSpec))
        {
            // Return the existing spec from the node
            return JsonSerializer.Deserialize<object>(existingSpec.Content?.InlineJson ?? "{}") ?? new { error = "Failed to deserialize existing spec" };
        }

        // Generate and store the OpenAPI spec as a node
        var openApiSpec = GenerateOpenApiSpec(moduleId, moduleNode);
        StoreOpenApiSpecAsNode(moduleId, openApiSpec);
        
        return openApiSpec;
    }

    private void StoreOpenApiSpecAsNode(string moduleId, object openApiSpec)
    {
        var specId = $"openapi-spec-{moduleId}";
        var specNode = new Node(
            Id: specId,
            TypeId: "codex.openapi/spec",
            State: ContentState.Ice,
            Locale: "en",
            Title: $"OpenAPI Specification for {moduleId}",
            Description: $"Generated OpenAPI 3.0.3 specification for module {moduleId}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(openApiSpec, new JsonSerializerOptions { WriteIndented = true }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["specType"] = "openapi",
                ["version"] = "3.0.3",
                ["generatedAt"] = DateTime.UtcNow.ToString("O")
            }
        );

        _registry.Upsert(specNode);

        // Create edge from module to spec
        var edge = new Edge(
            FromId: moduleId,
            ToId: specId,
            Role: "hasOpenApiSpec",
            Weight: 1.0,
            Meta: new Dictionary<string, object>
            {
                ["specType"] = "openapi",
                ["version"] = "3.0.3"
            }
        );
        _registry.Upsert(edge);
    }

    private void CreateApiOperationNode(string moduleId, ApiSpec apiSpec, object operation, string path)
    {
        var operationId = $"openapi-operation-{moduleId}-{apiSpec.Name}";
        var operationNode = new Node(
            Id: operationId,
            TypeId: "codex.openapi/operation",
            State: ContentState.Ice,
            Locale: "en",
            Title: $"{apiSpec.Verb.ToUpper()} {path}",
            Description: apiSpec.Description ?? $"API operation {apiSpec.Name}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = true }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = moduleId,
                ["apiName"] = apiSpec.Name,
                ["verb"] = apiSpec.Verb,
                ["path"] = path,
                ["operationId"] = apiSpec.Name
            }
        );

        _registry.Upsert(operationNode);

        // Create edge from module to operation
        var edge = new Edge(
            FromId: moduleId,
            ToId: operationId,
            Role: "hasOperation",
            Weight: 1.0,
            Meta: new Dictionary<string, object>
            {
                ["verb"] = apiSpec.Verb,
                ["path"] = path,
                ["operationId"] = apiSpec.Name
            }
        );
        _registry.Upsert(edge);
    }

    private object GenerateOpenApiSpec(string moduleId, Node moduleNode)
    {
        var moduleName = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title ?? moduleId;
        var moduleVersion = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0";
        var moduleDescription = moduleNode.Description ?? $"API for {moduleName}";

        var info = new
        {
            title = moduleName,
            version = moduleVersion,
            description = moduleDescription
        };

        var components = new Dictionary<string, object>();
        var paths = new Dictionary<string, object>();

        // Get all API nodes for this module (both "api" and "codex.meta/api" types)
        var apiNodes = _registry.GetNodesByType("codex.meta/api")
            .Concat(_registry.GetNodesByType("api"))
            .Where(n => n.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
            .ToList();

        foreach (var apiNode in apiNodes)
        {
            var apiName = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString();
            if (string.IsNullOrEmpty(apiName)) continue;

            // Parse API specification from content
            var apiSpec = ParseApiSpec(apiNode);
            if (apiSpec == null) continue;

            // Generate OpenAPI operation
            var operation = GenerateOpenApiOperation(apiSpec, components);
            if (operation == null) continue;

            // Generate path
            var path = GenerateOpenApiPath(apiSpec, operation);
            if (path.HasValue)
            {
                paths[path.Value.Key] = path.Value.Value;
                
                // Create a node for this API operation
                CreateApiOperationNode(moduleId, apiSpec, operation, path.Value.Key);
            }
        }

        return new
        {
            openapi = "3.0.3",
            info = info,
            components = components.Any() ? components : null,
            paths = paths.Any() ? paths : null
        };
    }

    private ApiSpec? ParseApiSpec(Node apiNode)
    {
        try
        {
            if (apiNode.Content?.InlineJson == null) return null;

            var apiContent = JsonSerializer.Deserialize<JsonElement>(apiNode.Content.InlineJson);
            
            // Handle two different formats:
            // Format 1: {"name": "apiName", "verb": "GET", "route": "/path", "parameters": [...]}
            // Format 2: {"moduleId": "module", "apiName": "apiName", "route": "/path", "description": "..."}
            
            string name;
            string verb;
            string route;
            string? description;
            List<ParameterSpec> parameters = new();

            if (apiContent.TryGetProperty("name", out var nameElement))
            {
                // Format 1: Full API spec format
                name = nameElement.GetString() ?? "unknown";
                verb = apiContent.TryGetProperty("verb", out var verbElement) ? verbElement.GetString() ?? "GET" : "GET";
                route = apiContent.TryGetProperty("route", out var routeElement) ? routeElement.GetString() ?? "/" : "/";
                description = apiContent.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;

                // Parse parameters if they exist
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
            }
            else if (apiContent.TryGetProperty("apiName", out var apiNameElement))
            {
                // Format 2: Simple format
                name = apiNameElement.GetString() ?? "unknown";
                verb = "POST"; // Default to POST for simple format
                route = apiContent.TryGetProperty("route", out var routeElement) ? routeElement.GetString() ?? "/" : "/";
                description = apiContent.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
            }
            else
            {
                return null;
            }

            return new ApiSpec(name, verb, route, description, parameters.Any() ? parameters : null);
        }
        catch
        {
            return null;
        }
    }

    private object? GenerateOpenApiOperation(ApiSpec apiSpec, Dictionary<string, object> components)
    {
        var operationId = $"{apiSpec.Name}";
        var summary = apiSpec.Description ?? apiSpec.Name;
        var description = apiSpec.Description;

        var parameters = new List<object>();
        if (apiSpec.Parameters != null)
        {
            foreach (var param in apiSpec.Parameters)
            {
                var paramSchema = GenerateOpenApiSchema(param.Type, components);
                parameters.Add(new
                {
                    name = param.Name,
                    @in = "query", // Default to query parameter
                    required = param.Required,
                    description = param.Description,
                    schema = paramSchema
                });
            }
        }

        var responses = new Dictionary<string, object>
        {
            ["200"] = new
            {
                description = "Successful response",
                content = new Dictionary<string, object>
                {
                    ["application/json"] = new
                    {
                        schema = new
                        {
                            type = "object",
                            description = "Response object"
                        }
                    }
                }
            }
        };

        return new
        {
            operationId = operationId,
            summary = summary,
            description = description,
            parameters = parameters.Any() ? parameters : null,
            responses = responses
        };
    }

    private KeyValuePair<string, object>? GenerateOpenApiPath(ApiSpec apiSpec, object operation)
    {
        // Convert route to OpenAPI path format
        var path = apiSpec.Route;
        if (path.StartsWith("/"))
        {
            path = path.Substring(1);
        }

        // Convert {id} to {id} format for OpenAPI
        path = path.Replace("{", "{").Replace("}", "}");

        var pathItem = new Dictionary<string, object>
        {
            [apiSpec.Verb.ToLower()] = operation
        };

        return new KeyValuePair<string, object>($"/{path}", pathItem);
    }

    private object GenerateOpenApiSchema(string type, Dictionary<string, object> components)
    {
        return type.ToLower() switch
        {
            "string" => new { type = "string" },
            "int" or "integer" => new { type = "integer" },
            "bool" or "boolean" => new { type = "boolean" },
            "double" or "float" or "number" => new { type = "number" },
            "object" => new { type = "object" },
            "array" => new { type = "array", items = new { type = "string" } },
            _ => new { type = "string", description = $"Type: {type}" }
        };
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

        // Register API nodes
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

        var listSpecsApiNode = new Node(
            Id: "codex.openapi/list-specs-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "List OpenAPI Specs API",
            Description: "List all OpenAPI specifications as nodes",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "list-specs",
                    verb = "GET",
                    route = "/openapi/specs",
                    description = "List all OpenAPI specifications stored as nodes"
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.openapi",
                ["apiName"] = "list-specs"
            }
        );
        registry.Upsert(listSpecsApiNode);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.openapi", "generate"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.openapi", "list-specs"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.openapi", "generate", args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request parameters"));
                }

                var moduleId = args.Value.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

                if (string.IsNullOrEmpty(moduleId))
                {
                    return Task.FromResult<object>(new ErrorResponse("Module ID is required"));
                }

                // Get the module node
                if (!registry.TryGet(moduleId, out var moduleNode))
                {
                    return Task.FromResult<object>(new ErrorResponse($"Module '{moduleId}' not found"));
                }

                // Generate OpenAPI specification
                var openApiSpec = GetOpenApiSpec(moduleId);

                return Task.FromResult<object>(new OpenApiGenerationResponse(ModuleId: moduleId, Success: true, OpenApiSpec: openApiSpec));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to generate OpenAPI: {ex.Message}"));
            }
        });

        router.Register("codex.openapi", "list-specs", args =>
        {
            try
            {
                // Get all OpenAPI spec nodes
                var specNodes = _registry.GetNodesByType("codex.openapi/spec")
                    .Select(node => new
                    {
                        id = node.Id,
                        moduleId = node.Meta?.GetValueOrDefault("moduleId")?.ToString(),
                        title = node.Title,
                        description = node.Description,
                        generatedAt = node.Meta?.GetValueOrDefault("generatedAt")?.ToString(),
                        version = node.Meta?.GetValueOrDefault("version")?.ToString(),
                        specType = node.Meta?.GetValueOrDefault("specType")?.ToString()
                    })
                    .ToList();

                return Task.FromResult<object>(new
                {
                    specs = specNodes,
                    count = specNodes.Count
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to list OpenAPI specs: {ex.Message}"));
            }
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // OpenAPI specifications for modules
        app.MapGet("/openapi/{moduleId}", (string moduleId) =>
        {
            try
            {
                // Get the module node to find the module instance
                var moduleNode = coreApi.GetModule(moduleId);
                if (moduleNode == null)
                {
                    return Results.NotFound($"Module '{moduleId}' not found");
                }

                var openApiSpec = GetOpenApiSpec(moduleId);
                return Results.Ok(openApiSpec);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get OpenAPI spec for module '{moduleId}': {ex.Message}");
            }
        });

        // List all OpenAPI specifications as nodes
        app.MapGet("/openapi-specs", () =>
        {
            try
            {
                // Use the list-specs API
                var result = coreApi.ExecuteDynamicCall(new DynamicCall("codex.openapi", "list-specs", null));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to list OpenAPI specs: {ex.Message}");
            }
        });
    }

}
