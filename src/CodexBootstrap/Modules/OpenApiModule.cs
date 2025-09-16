using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Simplified OpenAPI module response types
public record OpenApiGenerationResponse(string ModuleId, bool Success, string Message = "OpenAPI generated successfully", object? OpenApiSpec = null);

public record OpenApiSpecResponse(
    string OpenApi = "3.0.3",
    OpenApiInfo? Info = null,
    Dictionary<string, object>? Paths = null
);

public record OpenApiInfo(
    string Title,
    string Version,
    string Description
);

public record OpenApiSpecsListResponse(
    List<OpenApiSpecInfo> Specs,
    int Count
);

public record OpenApiSpecInfo(
    string Id,
    string? ModuleId,
    string? Title,
    string? Description,
    string? GeneratedAt,
    string? Version,
    string? SpecType
);

[MetaNode(Id = "codex.openapi", Name = "OpenAPI Module", Description = "Module for generating comprehensive OpenAPI 3.0 specifications from meta-node attributes")]
public sealed class OpenApiModule : ModuleBase
{
    public override string Name => "OpenAPI Module";
    public override string Description => "Module for generating comprehensive OpenAPI 3.0 specifications from meta-node attributes";
    public override string Version => "1.0.0";

    public OpenApiModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.openapi",
            name: "OpenAPI Module",
            version: "0.1.0",
            description: "Module for generating deterministic OpenAPI specifications from module types and APIs.",
            tags: new[] { "openapi", "spec", "generation", "documentation" },
            capabilities: new[] { "openapi", "specification", "generation", "documentation" },
            spec: "codex.spec.openapi"
        );
    }

    public object GetOpenApiSpec(string moduleId)
    {
        // Get the module node to find the module instance
        if (!_registry.TryGet(moduleId, out var moduleNode))
        {
            return new ErrorResponse($"Module '{moduleId}' not found");
        }

        // Check if OpenAPI spec already exists as a node
        var existingSpecId = $"openapi-spec-{moduleId}";
        if (_registry.TryGet(existingSpecId, out var existingSpec))
        {
            // Return the existing spec from the node
            try
            {
                var deserialized = JsonSerializer.Deserialize<OpenApiSpecResponse>(existingSpec.Content?.InlineJson ?? "{}");
                if (deserialized == null)
                {
                    return new ErrorResponse("Failed to deserialize existing spec");
                }
                return deserialized;
            }
            catch
            {
                return new ErrorResponse("Failed to deserialize existing spec");
            }
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

    // Simplified - using built-in ASP.NET Core OpenAPI support

    private object GenerateOpenApiSpec(string moduleId, Node moduleNode)
    {
        var moduleName = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? moduleNode.Title ?? moduleId;
        var moduleVersion = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0";
        var moduleDescription = moduleNode.Description ?? $"API for {moduleName}";

        // Get all API nodes for this module
        var apiNodes = _registry.GetNodesByType("codex.meta/api")
            .Where(n => n.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
            .ToList();

        var paths = new Dictionary<string, object>();

        foreach (var apiNode in apiNodes)
        {
            var apiName = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString();
            var verb = apiNode.Meta?.GetValueOrDefault("verb")?.ToString()?.ToLower() ?? "get";
            var route = apiNode.Meta?.GetValueOrDefault("route")?.ToString() ?? "/";
            var description = apiNode.Description ?? apiName ?? "API endpoint";

            if (string.IsNullOrEmpty(apiName)) continue;

            // Generate simple OpenAPI path item
            var pathItem = new Dictionary<string, object>
            {
                [verb] = new
                {
                    operationId = apiName,
                    summary = description,
                    description = description,
                    responses = new Dictionary<string, object>
                    {
                        ["200"] = new
                        {
                            description = "Successful response",
                            content = new Dictionary<string, object>
                            {
                                ["application/json"] = new
                                {
                                    schema = new { type = "object" }
                                }
                            }
                        }
                    }
                }
            };

            paths[route] = pathItem;
        }

        return new OpenApiSpecResponse(
            OpenApi: "3.0.3",
            Info: new OpenApiInfo(
                Title: moduleName,
                Version: moduleVersion,
                Description: moduleDescription
            ),
            Paths: paths.Any() ? paths : null
        );
    }

    // Simplified - using built-in ASP.NET Core OpenAPI support


    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
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
                var specNodes = registry.GetNodesByType("codex.openapi/spec")
                    .Select(node => new OpenApiSpecInfo(
                        Id: node.Id,
                        ModuleId: node.Meta?.GetValueOrDefault("moduleId")?.ToString(),
                        Title: node.Title,
                        Description: node.Description,
                        GeneratedAt: node.Meta?.GetValueOrDefault("generatedAt")?.ToString(),
                        Version: node.Meta?.GetValueOrDefault("version")?.ToString(),
                        SpecType: node.Meta?.GetValueOrDefault("specType")?.ToString()
                    ))
                    .ToList();

                return Task.FromResult<object>(new OpenApiSpecsListResponse(
                    Specs: specNodes,
                    Count: specNodes.Count
                ));
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to list OpenAPI specs: {ex.Message}"));
            }
        });
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Swagger UI is automatically configured in Program.cs
        // This module provides additional OpenAPI endpoints for module-specific specs
        
        // Get OpenAPI spec for a specific module
        app.MapGet("/openapi/module/{moduleId}", (string moduleId) =>
        {
            try
            {
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

        // List all available OpenAPI specifications
        app.MapGet("/openapi/specs", async () =>
        {
            try
            {
                var result = await coreApi.ExecuteDynamicCall(new DynamicCall("codex.openapi", "list-specs", null));
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to list OpenAPI specs: {ex.Message}");
            }
        });
    }

}
