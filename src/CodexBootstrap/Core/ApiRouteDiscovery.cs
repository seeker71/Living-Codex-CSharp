using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// System for discovering and registering API routes from C# attributes
/// </summary>
public static class ApiRouteDiscovery
{
    /// <summary>
    /// Discovers and registers all API routes from attributes in the current assembly
    /// </summary>
    public static void DiscoverAndRegisterRoutes(WebApplication app, IApiRouter router, NodeRegistry registry)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var routeMethods = GetRouteMethods(assembly);

        foreach (var (method, attribute) in routeMethods)
        {
            RegisterRoute(app, router, registry, method, attribute);
        }
    }

    /// <summary>
    /// Gets all methods decorated with API route attributes
    /// </summary>
    private static IEnumerable<(MethodInfo method, ApiRouteAttribute attribute)> GetRouteMethods(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttribute<ApiRouteAttribute>() != null);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ApiRouteAttribute>();
                if (attribute != null)
                {
                    yield return (method, attribute);
                }
            }
        }
    }

    /// <summary>
    /// Registers a single route
    /// </summary>
    private static void RegisterRoute(WebApplication app, IApiRouter router, NodeRegistry registry, 
        MethodInfo method, ApiRouteAttribute attribute)
    {
        try
        {
            // Create API node
            var apiNode = CreateApiNode(attribute, method);
            registry.Upsert(apiNode);

            // Create module-API edge
            var edge = NodeHelpers.CreateModuleApiEdge(attribute.ModuleId, apiNode.Id);
            registry.Upsert(edge);

            // Register with API router
            RegisterApiHandler(router, registry, attribute, method);

            // Map HTTP endpoint
            MapHttpEndpoint(app, attribute, method, router, registry);
        }
        catch (Exception ex)
        {
            // Log error but continue with other routes
            Console.WriteLine($"Error registering route {attribute.Route}: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an API node from the attribute and method
    /// </summary>
    private static Node CreateApiNode(ApiRouteAttribute attribute, MethodInfo method)
    {
        var parameters = GetParameterSpecs(method);
        var content = new
        {
            moduleId = attribute.ModuleId,
            apiName = attribute.Name,
            route = attribute.Route,
            description = attribute.Description,
            verb = attribute.Verb,
            requiresAuth = attribute.RequiresAuth,
            requiredPermissions = attribute.RequiredPermissions,
            requestType = attribute.RequestType?.Name,
            responseType = attribute.ResponseType?.Name,
            tags = attribute.Tags,
            summary = attribute.Summary,
            operationId = attribute.OperationId,
            deprecated = attribute.Deprecated,
            version = attribute.Version,
            parameters = parameters.Select(p => new
            {
                name = p.Name,
                type = p.Type,
                required = p.Required,
                description = p.Description
            }).ToArray()
        };

        var meta = new Dictionary<string, object>
        {
            ["moduleId"] = attribute.ModuleId,
            ["apiName"] = attribute.Name,
            ["route"] = attribute.Route,
            ["verb"] = attribute.Verb,
            ["isApi"] = true,
            ["requiresAuth"] = attribute.RequiresAuth,
            ["deprecated"] = attribute.Deprecated,
            ["version"] = attribute.Version
        };

        if (attribute.RequiredPermissions != null)
            meta["requiredPermissions"] = attribute.RequiredPermissions;

        if (attribute.RequestType != null)
            meta["requestType"] = attribute.RequestType.Name;

        if (attribute.ResponseType != null)
            meta["responseType"] = attribute.ResponseType.Name;

        if (attribute.Tags != null)
            meta["tags"] = attribute.Tags;

        if (attribute.Summary != null)
            meta["summary"] = attribute.Summary;

        if (attribute.OperationId != null)
            meta["operationId"] = attribute.OperationId;

        return NodeHelpers.CreateNode(
            id: $"{attribute.ModuleId}.{attribute.Name}",
            typeId: "codex.meta/api",
            state: ContentState.Ice,
            title: attribute.Name,
            description: attribute.Description,
            content: NodeHelpers.CreateJsonContent(content),
            additionalMeta: meta
        );
    }

    /// <summary>
    /// Gets parameter specifications from method parameters
    /// </summary>
    private static List<ParameterSpec> GetParameterSpecs(MethodInfo method)
    {
        var parameters = new List<ParameterSpec>();

        foreach (var param in method.GetParameters())
        {
            var paramAttribute = param.GetCustomAttribute<ApiParameterAttribute>();
            if (paramAttribute != null)
            {
                parameters.Add(new ParameterSpec(
                    Name: paramAttribute.Name,
                    Type: paramAttribute.Type,
                    Required: paramAttribute.Required,
                    Description: paramAttribute.Description
                ));
            }
            else
            {
                // Auto-generate parameter spec from parameter info
                parameters.Add(new ParameterSpec(
                    Name: param.Name ?? "unknown",
                    Type: GetTypeName(param.ParameterType),
                    Required: !param.HasDefaultValue,
                    Description: $"Parameter {param.Name}"
                ));
            }
        }

        return parameters;
    }

    /// <summary>
    /// Gets a simplified type name for parameter types
    /// </summary>
    private static string GetTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "integer";
        if (type == typeof(long)) return "integer";
        if (type == typeof(double)) return "number";
        if (type == typeof(float)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "string";
        if (type.IsArray) return "array";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return GetTypeName(Nullable.GetUnderlyingType(type)!);
        
        return type.Name.ToLowerInvariant();
    }

    /// <summary>
    /// Registers the API handler with the router
    /// </summary>
    private static void RegisterApiHandler(IApiRouter router, NodeRegistry registry, ApiRouteAttribute attribute, MethodInfo method)
    {
        var handler = CreateHandlerDelegate(method, router, registry);
        ModuleHelpers.RegisterApiHandler(router, attribute.ModuleId, attribute.Name, handler);
    }

    /// <summary>
    /// Creates a handler delegate from the method
    /// </summary>
    private static Func<JsonElement?, Task<object>> CreateHandlerDelegate(MethodInfo method, IApiRouter router, NodeRegistry registry)
    {
        return async (JsonElement? request) =>
        {
            try
            {
                // Create instance with proper dependency injection
                var instance = CreateModuleInstance(method.DeclaringType!, router, registry);
                
                // Get method parameters
                var parameters = method.GetParameters();
                object[] args;
                
                if (parameters.Length == 0)
                {
                    // No parameters
                    args = Array.Empty<object>();
                }
                else if (parameters.Length == 1 && request != null)
                {
                    // Single parameter - use the request
                    args = new[] { (object)request };
                }
                else
                {
                    // Multiple parameters - try to match by type or use defaults
                    args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (i == 0 && request != null && param.ParameterType.IsAssignableFrom(request.GetType()))
                        {
                            args[i] = request;
                        }
                        else if (param.HasDefaultValue)
                        {
                            args[i] = param.DefaultValue!;
                        }
                        else
                        {
                            args[i] = GetDefaultValue(param.ParameterType);
                        }
                    }
                }
                
                var result = method.Invoke(instance, args);
                
                if (result is Task<object> task)
                {
                    return await task;
                }
                else if (result is Task taskResult)
                {
                    await taskResult;
                    return new { success = true };
                }
                else
                {
                    return result ?? new { success = true };
                }
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Error executing {method.Name}: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// Creates a module instance with proper dependency injection
    /// </summary>
    private static object CreateModuleInstance(Type moduleType, IApiRouter router, NodeRegistry registry)
    {
        // Try to find a constructor that takes IApiRouter and NodeRegistry
        var constructor = moduleType.GetConstructor(new[] { typeof(IApiRouter), typeof(NodeRegistry) });
        if (constructor != null)
        {
            return Activator.CreateInstance(moduleType, router, registry)!;
        }

        // Try to find a constructor that takes just IApiRouter
        constructor = moduleType.GetConstructor(new[] { typeof(IApiRouter) });
        if (constructor != null)
        {
            return Activator.CreateInstance(moduleType, router)!;
        }

        // Try to find a constructor that takes just NodeRegistry
        constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry) });
        if (constructor != null)
        {
            return Activator.CreateInstance(moduleType, registry)!;
        }

        // Fallback to parameterless constructor
        return Activator.CreateInstance(moduleType)!;
    }

    /// <summary>
    /// Gets the default value for a type
    /// </summary>
    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    /// <summary>
    /// Maps the HTTP endpoint
    /// </summary>
    private static void MapHttpEndpoint(WebApplication app, ApiRouteAttribute attribute, MethodInfo method, IApiRouter router, NodeRegistry registry)
    {
        var handler = CreateHandlerDelegate(method, router, registry);
        
        // Auto-detect request type from method parameters
        var requestType = attribute.RequestType ?? GetRequestTypeFromMethod(method);
        
        // Handle different HTTP verbs appropriately
        switch (attribute.Verb.ToUpperInvariant())
        {
            case "GET":
            case "DELETE":
                // GET and DELETE methods don't support request bodies
                MapHttpEndpointWithoutBody(app, attribute, WrapHandler(handler));
                break;
            case "POST":
            case "PUT":
            case "PATCH":
                // POST, PUT, and PATCH methods can have request bodies
                if (requestType != null)
                {
                    MapHttpEndpointWithBody(app, attribute, WrapHandler(handler), requestType);
                }
                else
                {
                    MapHttpEndpointWithoutBody(app, attribute, WrapHandler(handler));
                }
                break;
            default:
                // Fallback to no body for unknown verbs
                MapHttpEndpointWithoutBody(app, attribute, WrapHandler(handler));
                break;
        }
    }

    /// <summary>
    /// Wraps a JsonElement handler to work with object handlers
    /// </summary>
    private static Func<object?, Task<object>> WrapHandler(Func<JsonElement?, Task<object>> handler)
    {
        return async (object? request) =>
        {
            JsonElement? jsonRequest = request as JsonElement?;
            return await handler(jsonRequest);
        };
    }

    /// <summary>
    /// Gets the request type from method parameters
    /// </summary>
    private static Type? GetRequestTypeFromMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        
        // Look for a parameter with ApiParameterAttribute that has Location = "body"
        foreach (var param in parameters)
        {
            var paramAttribute = param.GetCustomAttribute<ApiParameterAttribute>();
            if (paramAttribute != null && paramAttribute.Location == "body")
            {
                return param.ParameterType;
            }
        }
        
        // If no body parameter found, look for the first complex type parameter
        foreach (var param in parameters)
        {
            if (param.ParameterType != typeof(string) && 
                param.ParameterType != typeof(int) && 
                param.ParameterType != typeof(long) && 
                param.ParameterType != typeof(double) && 
                param.ParameterType != typeof(float) && 
                param.ParameterType != typeof(bool) && 
                param.ParameterType != typeof(DateTime) &&
                !param.ParameterType.IsPrimitive)
            {
                return param.ParameterType;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Maps HTTP endpoint without request body
    /// </summary>
    private static void MapHttpEndpointWithoutBody(WebApplication app, ApiRouteAttribute attribute, Func<object?, Task<object>> handler)
    {
        switch (attribute.Verb.ToUpperInvariant())
        {
            case "GET":
                app.MapGet(attribute.Route, async () =>
                {
                    var result = await handler(null);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "POST":
                app.MapPost(attribute.Route, async () =>
                {
                    var result = await handler(null);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "PUT":
                app.MapPut(attribute.Route, async () =>
                {
                    var result = await handler(null);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "DELETE":
                app.MapDelete(attribute.Route, async () =>
                {
                    var result = await handler(null);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "PATCH":
                app.MapMethods(attribute.Route, new[] { "PATCH" }, async () =>
                {
                    var result = await handler(null);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
        }
    }

    /// <summary>
    /// Maps HTTP endpoint with request body
    /// </summary>
    private static void MapHttpEndpointWithBody(WebApplication app, ApiRouteAttribute attribute, Func<object?, Task<object>> handler, Type requestType)
    {
        switch (attribute.Verb.ToUpperInvariant())
        {
            case "POST":
                app.MapPost(attribute.Route, async (HttpContext context) =>
                {
                    var request = await context.Request.ReadFromJsonAsync(requestType);
                    var result = await handler(request);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "PUT":
                app.MapPut(attribute.Route, async (HttpContext context) =>
                {
                    var request = await context.Request.ReadFromJsonAsync(requestType);
                    var result = await handler(request);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
            case "PATCH":
                app.MapMethods(attribute.Route, new[] { "PATCH" }, async (HttpContext context) =>
                {
                    var request = await context.Request.ReadFromJsonAsync(requestType);
                    var result = await handler(request);
                    return Results.Ok(result);
                }).WithName(attribute.Name);
                break;
        }
    }
}
