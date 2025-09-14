using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// System for discovering and registering API routes from C# attributes
/// </summary>
public static class ApiRouteDiscovery
{
    private static readonly ICodexLogger _logger = new Log4NetLogger(typeof(ApiRouteDiscovery));
    private static readonly Dictionary<Type, object> _moduleInstances = new();
    private static IServiceProvider? ServiceProvider;
    /// <summary>
    /// Discovers and registers all API routes from attributes in the current assembly
    /// </summary>
    public static void DiscoverAndRegisterRoutes(WebApplication app, IApiRouter router, NodeRegistry registry)
    {
        ServiceProvider = app.Services;
        var assembly = Assembly.GetExecutingAssembly();
        var routeMethods = GetRouteMethods(assembly);
        var registeredRoutes = new HashSet<string>();

        foreach (var (method, attribute) in routeMethods)
        {
            var routeKey = $"{attribute.Verb.ToUpperInvariant()}:{attribute.Route}";
            
            if (registeredRoutes.Contains(routeKey))
            {
                throw new InvalidOperationException(
                    $"Duplicate route registration detected! " +
                    $"Route '{attribute.Verb} {attribute.Route}' is already registered. " +
                    $"This route is being registered by method '{method.DeclaringType?.Name}.{method.Name}' " +
                    $"in module '{attribute.ModuleId}'. " +
                    $"Please check for duplicate ApiRoute attributes or conflicting route patterns.");
            }
            
            registeredRoutes.Add(routeKey);
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
            _logger.Info($"Registering route: {attribute.Verb} {attribute.Route} from {method.DeclaringType?.Name}.{method.Name}");
            
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
            
            _logger.Info($"Successfully registered route: {attribute.Verb} {attribute.Route}");
        }
        catch (Exception ex)
        {
            // Log error with detailed information
            var errorMessage = $"Error registering route {attribute.Verb} {attribute.Route} from {method.DeclaringType?.Name}.{method.Name}: {ex.Message}";
            _logger.Error(errorMessage, ex);
            throw new InvalidOperationException(errorMessage, ex);
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
            status = attribute.Status.ToString(),
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
            ["status"] = attribute.Status.ToString(),
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
        // Convert Func<object?, Task<object>> to Func<JsonElement?, Task<object>>
        Func<JsonElement?, Task<object>> jsonHandler = async (JsonElement? request) =>
        {
            return await handler(request);
        };
        ModuleHelpers.RegisterApiHandler(router, attribute.ModuleId, attribute.Name, jsonHandler);
    }

    /// <summary>
    /// Creates a handler delegate from the method
    /// </summary>
    private static Func<object?, Task<object>> CreateHandlerDelegate(MethodInfo method, IApiRouter router, NodeRegistry registry)
    {
        return async (object? request) =>
        {
            try
            {
                var instance = CreateModuleInstance(method.DeclaringType!, router, registry);
                var parameters = method.GetParameters();
                var args = await BindParametersFromObjectAsync(parameters, request);
                var result = method.Invoke(instance, args);
                return await UnwrapResultAsync(result);
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
        // Check if we already have an instance of this module type
        if (_moduleInstances.TryGetValue(moduleType, out var existingInstance))
        {
            return existingInstance;
        }

        object instance;

        // Try to find a constructor that takes IApiRouter and NodeRegistry
        var constructor = moduleType.GetConstructor(new[] { typeof(IApiRouter), typeof(NodeRegistry) });
        if (constructor != null)
        {
            instance = Activator.CreateInstance(moduleType, router, registry)!;
        }
        // Try to find a constructor that takes just IApiRouter
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(IApiRouter) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, router)!;
        }
        // Try to find a constructor that takes just NodeRegistry
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional RealtimeModule
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(RealtimeModule) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, (RealtimeModule?)null)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional string
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(string) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, (string?)null)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional IStorageBackend
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(IStorageBackend) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, (IStorageBackend?)null)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional ICacheManager
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(ICacheManager) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, (ICacheManager?)null)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional HttpClient
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(HttpClient) })) != null)
        {
            // Get HttpClient from service provider
            var httpClient = ServiceProvider?.GetService<HttpClient>() ?? new HttpClient();
            instance = Activator.CreateInstance(moduleType, registry, httpClient)!;
        }
        // Try to find a constructor that takes NodeRegistry and optional IDistributedStorageBackend
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(IDistributedStorageBackend) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, (IDistributedStorageBackend?)null)!;
        }
        // Try to find a constructor that takes NodeRegistry and IApiRouter (different order)
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(NodeRegistry), typeof(IApiRouter) })) != null)
        {
            instance = Activator.CreateInstance(moduleType, registry, router)!;
        }
        // Try to find a constructor that takes IServiceProvider
        else if ((constructor = moduleType.GetConstructor(new[] { typeof(IServiceProvider) })) != null)
        {
            // Create a minimal service provider
            var services = new ServiceCollection();
            services.AddSingleton(router);
            services.AddSingleton(registry);
            var serviceProvider = services.BuildServiceProvider();
            instance = Activator.CreateInstance(moduleType, serviceProvider)!;
        }
        // Fallback to parameterless constructor
        else
        {
            instance = Activator.CreateInstance(moduleType)!;
        }

        // Cache the instance for reuse
        _moduleInstances[moduleType] = instance;
        return instance;
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
        var httpVerb = attribute.Verb.ToUpperInvariant();
        app.MapMethods(attribute.Route, new[] { httpVerb }, async (HttpContext context) =>
        {
            try
            {
                var instance = CreateModuleInstance(method.DeclaringType!, router, registry);
                var args = await BindParametersFromHttpContextAsync(method, context);
                var result = method.Invoke(instance, args);
                return Results.Ok(await UnwrapResultAsync(result));
            }
            catch (ApiBindingException bex)
            {
                return Results.Ok(new ErrorResponse(bex.Message));
            }
            catch (JsonException jex)
            {
                return Results.Ok(new ErrorResponse($"Invalid JSON: {jex.Message}"));
            }
            catch (Exception ex)
            {
                return Results.Ok(new ErrorResponse($"Error executing {method.Name}: {ex.Message}"));
            }
        }).WithName(attribute.Name);
    }

    /// <summary>
    /// Wraps a JsonElement handler to work with object handlers
    /// </summary>
    private static Func<object?, Task<object>> WrapHandler(Func<object?, Task<object>> handler)
    {
        return handler;
    }

    private static async Task<object[]> BindParametersFromObjectAsync(ParameterInfo[] parameters, object? source)
    {
        if (parameters.Length == 0)
        {
            return Array.Empty<object>();
        }

        if (parameters.Length == 1)
        {
            var p = parameters[0];
            var value = await DeserializeToTypeAsync(source, p.ParameterType);
            if (value == null)
            {
                if (p.HasDefaultValue) return new[] { p.DefaultValue! };
                if (IsNullable(p.ParameterType)) return new object?[] { null! } as object[];
                throw new InvalidOperationException($"Missing value for parameter '{p.Name}'");
            }
            return new[] { value };
        }

        // Multiple parameters require object-like source; try JSON object mapping
        var args = new object?[parameters.Length];
        JsonElement? json = source as JsonElement?;
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            var targetName = p.GetCustomAttribute<ApiParameterAttribute>()?.Name ?? p.Name ?? $"arg{i}";
            object? value = null;

            if (json.HasValue && json.Value.ValueKind == JsonValueKind.Object)
            {
                if (json.Value.TryGetProperty(targetName, out var prop))
                {
                    value = JsonSerializer.Deserialize(prop.GetRawText(), p.ParameterType);
                }
                else
                {
                    foreach (var propEnum in json.Value.EnumerateObject())
                    {
                        if (string.Equals(propEnum.Name, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            value = JsonSerializer.Deserialize(propEnum.Value.GetRawText(), p.ParameterType);
                            break;
                        }
                    }
                }
            }
            else if (source != null && p.ParameterType.IsInstanceOfType(source))
            {
                value = source;
            }

            if (value == null)
            {
                if (p.HasDefaultValue)
                {
                    value = p.DefaultValue;
                }
                else if (IsNullable(p.ParameterType))
                {
                    value = null;
                }
                else
                {
                    throw new InvalidOperationException($"Missing value for parameter '{targetName}'");
                }
            }
            args[i] = value;
        }
        return args!;
    }

    private static async Task<object?> DeserializeToTypeAsync(object? source, Type targetType)
    {
        if (source == null) return null;
        var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (nonNullType.IsInstanceOfType(source)) return source;
        if (source is JsonElement je)
        {
            return JsonSerializer.Deserialize(je.GetRawText(), nonNullType);
        }
        if (source is string s)
        {
            try { return JsonSerializer.Deserialize(s, nonNullType); } catch { /* ignore */ }
        }
        return source;
    }

    private static async Task<object[]> BindParametersFromHttpContextAsync(MethodInfo method, HttpContext context)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0) return Array.Empty<object>();

        // Identify body parameter
        ParameterInfo? bodyParam = null;
        foreach (var p in parameters)
        {
            var attr = p.GetCustomAttribute<ApiParameterAttribute>();
            var loc = attr?.Location?.ToLowerInvariant();
            if (loc == "body") { bodyParam = p; break; }
        }
        if (bodyParam == null)
        {
            foreach (var p in parameters)
            {
                if (!IsSimpleType(p.ParameterType)) { bodyParam = p; break; }
            }
        }

        object? bodyValue = null;
        var methodName = context.Request.Method;
        if (bodyParam != null && BodyAllowedForVerb(methodName))
        {
            bodyValue = await context.Request.ReadFromJsonAsync(bodyParam.ParameterType);
        }

        var args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p == bodyParam)
            {
                args[i] = bodyValue;
                continue;
            }

            var paramAttr = p.GetCustomAttribute<ApiParameterAttribute>();
            var name = paramAttr?.Name ?? p.Name ?? $"arg{i}";
            var location = (paramAttr?.Location ?? GuessLocationForParameter(p, methodName)).ToLowerInvariant();

            object? raw = null;
            switch (location)
            {
                case "path":
                    if (context.Request.RouteValues.TryGetValue(name, out var rv)) raw = rv?.ToString();
                    break;
                case "query":
                    if (context.Request.Query.TryGetValue(name, out var qv)) raw = qv.FirstOrDefault();
                    break;
                case "header":
                    if (context.Request.Headers.TryGetValue(name, out var hv)) raw = hv.FirstOrDefault();
                    break;
                case "body":
                    throw new ApiBindingException($"Multiple body parameters are not supported (parameter '{name}')");
                default:
                    if (context.Request.RouteValues.TryGetValue(name, out var rv2)) raw = rv2?.ToString();
                    if (raw == null && context.Request.Query.TryGetValue(name, out var qv2)) raw = qv2.FirstOrDefault();
                    break;
            }

            if (raw == null)
            {
                if (p.HasDefaultValue) { args[i] = p.DefaultValue; continue; }
                if (IsNullable(p.ParameterType)) { args[i] = null; continue; }
                throw new ApiBindingException($"Missing required parameter '{name}'");
            }

            args[i] = ConvertToType(raw, p.ParameterType);
        }

        return args!;
    }

    private static bool BodyAllowedForVerb(string httpMethod)
    {
        var m = httpMethod.ToUpperInvariant();
        return m == "POST" || m == "PUT" || m == "PATCH";
    }

    private static string GuessLocationForParameter(ParameterInfo p, string httpMethod)
    {
        var m = httpMethod.ToUpperInvariant();
        var paramName = p.Name ?? string.Empty;
        var declaring = p.Member as MethodInfo;
        var route = declaring?.GetCustomAttribute<ApiRouteAttribute>()?.Route ?? string.Empty;
        var inPath = !string.IsNullOrEmpty(paramName) && route.Contains("{" + paramName + "}", StringComparison.OrdinalIgnoreCase);

        if (IsSimpleType(p.ParameterType))
        {
            if (inPath) return "path";
            // Default simple types to query for safe GET/DELETE; query first for others as well
            return "query";
        }

        // Complex types prefer body on write verbs
        return BodyAllowedForVerb(m) ? "body" : (inPath ? "path" : "query");
    }

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid);
    }

    private static object? ConvertToType(object? value, Type targetType)
    {
        if (value == null) return null;
        var nonNullType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (nonNullType == typeof(string)) return value.ToString();
        if (nonNullType.IsEnum) return Enum.Parse(nonNullType, value.ToString()!, true);
        try
        {
            if (nonNullType == typeof(int)) return int.Parse(value.ToString()!);
            if (nonNullType == typeof(long)) return long.Parse(value.ToString()!);
            if (nonNullType == typeof(bool)) return bool.Parse(value.ToString()!);
            if (nonNullType == typeof(double)) return double.Parse(value.ToString()!);
            if (nonNullType == typeof(float)) return float.Parse(value.ToString()!);
            if (nonNullType == typeof(decimal)) return decimal.Parse(value.ToString()!);
            if (nonNullType == typeof(DateTime)) return DateTime.Parse(value.ToString()!);
            if (nonNullType == typeof(Guid)) return Guid.Parse(value.ToString()!);
        }
        catch (Exception ex)
        {
            throw new ApiBindingException($"Invalid value '{value}' for type {nonNullType.Name}: {ex.Message}");
        }
        // Fallback: try JSON deserialize when value looks like JSON
        var s = value as string;
        if (!string.IsNullOrWhiteSpace(s))
        {
            try { return JsonSerializer.Deserialize(s, nonNullType); } catch { }
        }
        return value;
    }

    private static bool IsNullable(Type t) => !t.IsValueType || Nullable.GetUnderlyingType(t) != null;

    private static async Task<object> UnwrapResultAsync(object? result)
    {
        if (result is Task<object> to) return await to;
        if (result is Task t)
        {
            await t;
            return new { success = true };
        }
        return result ?? new { success = true };
    }

    private sealed class ApiBindingException : Exception
    {
        public ApiBindingException(string message) : base(message) { }
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

}
