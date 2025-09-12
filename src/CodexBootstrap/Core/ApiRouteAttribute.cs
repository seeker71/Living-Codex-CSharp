using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Represents the integration status of an API route
/// </summary>
public enum RouteStatus
{
    /// <summary>
    /// Route is a placeholder stub with no implementation
    /// </summary>
    Stub,
    
    /// <summary>
    /// Route has basic implementation but limited functionality
    /// </summary>
    Simple,
    
    /// <summary>
    /// Route uses simulated/mocked data instead of real implementation
    /// </summary>
    Simulated,
    
    /// <summary>
    /// Route is a fallback implementation when primary service is unavailable
    /// </summary>
    Fallback,
    
    /// <summary>
    /// Route is enhanced with AI capabilities
    /// </summary>
    AiEnabled,
    
    /// <summary>
    /// Route depends on external information/services
    /// </summary>
    ExternalInfo,
    
    /// <summary>
    /// Route has not been tested yet
    /// </summary>
    Untested,
    
    /// <summary>
    /// Route has been partially tested
    /// </summary>
    PartiallyTested,
    
    /// <summary>
    /// Route has been fully tested and verified
    /// </summary>
    FullyTested
}

/// <summary>
/// Attribute to declare an API endpoint for automatic route registration
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ApiRouteAttribute : Attribute
{
    /// <summary>
    /// The HTTP verb for this endpoint (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string Verb { get; }

    /// <summary>
    /// The route pattern for this endpoint (e.g., "/concept/create", "/user/{id}")
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// The name of this API endpoint
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of this API endpoint
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The module ID that owns this endpoint
    /// </summary>
    public string ModuleId { get; }

    /// <summary>
    /// Whether this endpoint requires authentication
    /// </summary>
    public bool RequiresAuth { get; set; } = false;

    /// <summary>
    /// The required permissions for this endpoint
    /// </summary>
    public string[]? RequiredPermissions { get; set; }

    /// <summary>
    /// The request type for this endpoint
    /// </summary>
    public Type? RequestType { get; set; }

    /// <summary>
    /// The response type for this endpoint
    /// </summary>
    public Type? ResponseType { get; set; }

    /// <summary>
    /// The tags for this endpoint (for OpenAPI documentation)
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// The summary for this endpoint (for OpenAPI documentation)
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// The operation ID for this endpoint (for OpenAPI documentation)
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Whether this endpoint is deprecated
    /// </summary>
    public bool Deprecated { get; set; } = false;

    /// <summary>
    /// The version of this endpoint
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The integration status of this endpoint
    /// </summary>
    public RouteStatus Status { get; set; } = RouteStatus.Untested;

    /// <summary>
    /// Initializes a new instance of the ApiRouteAttribute
    /// </summary>
    /// <param name="verb">The HTTP verb for this endpoint</param>
    /// <param name="route">The route pattern for this endpoint</param>
    /// <param name="name">The name of this API endpoint</param>
    /// <param name="description">The description of this API endpoint</param>
    /// <param name="moduleId">The module ID that owns this endpoint</param>
    public ApiRouteAttribute(string verb, string route, string name, string description, string moduleId)
    {
        Verb = verb ?? throw new ArgumentNullException(nameof(verb));
        Route = route ?? throw new ArgumentNullException(nameof(route));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ModuleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
    }
}

/// <summary>
/// Attribute to declare a GET endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class GetAttribute : ApiRouteAttribute
{
    public GetAttribute(string route, string name, string description, string moduleId) 
        : base("GET", route, name, description, moduleId) { }
}

/// <summary>
/// Attribute to declare a POST endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PostAttribute : ApiRouteAttribute
{
    public PostAttribute(string route, string name, string description, string moduleId) 
        : base("POST", route, name, description, moduleId) { }
}

/// <summary>
/// Attribute to declare a PUT endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PutAttribute : ApiRouteAttribute
{
    public PutAttribute(string route, string name, string description, string moduleId) 
        : base("PUT", route, name, description, moduleId) { }
}

/// <summary>
/// Attribute to declare a DELETE endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class DeleteAttribute : ApiRouteAttribute
{
    public DeleteAttribute(string route, string name, string description, string moduleId) 
        : base("DELETE", route, name, description, moduleId) { }
}

/// <summary>
/// Attribute to declare a PATCH endpoint
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PatchAttribute : ApiRouteAttribute
{
    public PatchAttribute(string route, string name, string description, string moduleId) 
        : base("PATCH", route, name, description, moduleId) { }
}

/// <summary>
/// Attribute to declare parameter information for API endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ApiParameterAttribute : Attribute
{
    /// <summary>
    /// The name of the parameter
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of the parameter
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// The type of the parameter
    /// </summary>
    public string Type { get; set; } = "string";

    /// <summary>
    /// The location of the parameter (query, path, header, body)
    /// </summary>
    public string Location { get; set; } = "query";

    /// <summary>
    /// The example value for this parameter
    /// </summary>
    public object? Example { get; set; }

    /// <summary>
    /// Initializes a new instance of the ApiParameterAttribute
    /// </summary>
    /// <param name="name">The name of the parameter</param>
    /// <param name="description">The description of the parameter</param>
    public ApiParameterAttribute(string name, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Attribute to declare response information for API endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ApiResponseAttribute : Attribute
{
    /// <summary>
    /// The HTTP status code for this response
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The description of this response
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The type of this response
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Initializes a new instance of the ApiResponseAttribute
    /// </summary>
    /// <param name="statusCode">The HTTP status code for this response</param>
    /// <param name="description">The description of this response</param>
    public ApiResponseAttribute(int statusCode, string description)
    {
        StatusCode = statusCode;
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
