using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexBootstrap.Core;

/// <summary>
/// Helper class for generating OpenAPI 3.0 specifications from module metadata
/// </summary>
public static class OpenApiHelper
{
    public static object GenerateOpenApiSpec(string moduleId, Node moduleNode, NodeRegistry registry)
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

        // Get all API nodes for this module
        var apiNodes = registry.GetNodesByType("codex.meta/api")
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

    private static ApiSpec? ParseApiSpec(Node apiNode)
    {
        try
        {
            if (apiNode.Content?.InlineJson == null) return null;

            var apiContent = JsonSerializer.Deserialize<JsonElement>(apiNode.Content.InlineJson);
            var name = apiContent.GetProperty("name").GetString() ?? "unknown";
            var verb = apiContent.GetProperty("verb").GetString() ?? "GET";
            var route = apiContent.GetProperty("route").GetString() ?? "/";
            var description = apiContent.TryGetProperty("description", out var desc) ? desc.GetString() : null;

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

            return new ApiSpec(name, verb, route, description, parameters.Any() ? parameters : null);
        }
        catch
        {
            return null;
        }
    }

    private static object? GenerateOpenApiOperation(ApiSpec apiSpec, Dictionary<string, object> components)
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

    private static KeyValuePair<string, object>? GenerateOpenApiPath(ApiSpec apiSpec, object operation)
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

    private static object GenerateOpenApiSchema(string type, Dictionary<string, object> components)
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
}
