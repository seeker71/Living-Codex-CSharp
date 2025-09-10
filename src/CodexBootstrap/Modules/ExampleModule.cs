using System;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Example module demonstrating attribute-based API route registration
/// </summary>
public class ExampleModule : IModule
{
    public string ModuleId => "codex.example";
    public string Name => "Example Module";
    public string Version => "1.0.0";
    public string Description => "Example module demonstrating attribute-based API routes";

    public void Register(NodeRegistry registry)
    {
        // Module registration is now handled automatically by the attribute discovery system
        // This method can be used for additional module-specific setup if needed
    }

    public Node GetModuleNode()
    {
        return ModuleHelpers.CreateModuleNode(ModuleId, Name, Version, Description);
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApiService, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    /// <summary>
    /// Simple GET endpoint example
    /// </summary>
    [Get("/example/hello", "hello", "Simple hello world endpoint", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(500, "Internal server error")]
    public Task<object> HelloWorld()
    {
        return Task.FromResult<object>(new
        {
            message = "Hello from attribute-based API!",
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// GET endpoint with path parameter
    /// </summary>
    [Get("/example/greeting/{name}", "greeting", "Personalized greeting endpoint", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public Task<object> GetGreeting([ApiParameter("name", "Name to greet", Required = true, Location = "path")] string name)
    {
        return Task.FromResult<object>(new
        {
            greeting = $"Hello, {name}!",
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// POST endpoint with request body
    /// </summary>
    [Post("/example/echo", "echo", "Echo back the request data", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public Task<object> Echo([ApiParameter("data", "Data to echo back", Required = true, Location = "body")] EchoRequest request)
    {
        return Task.FromResult<object>(new
        {
            echo = request.Message,
            originalData = request,
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// PUT endpoint with authentication requirement
    /// </summary>
    [Put("/example/update/{id}", "update", "Update resource endpoint", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(401, "Unauthorized")]
    [ApiResponse(404, "Not found")]
    public Task<object> UpdateResource(
        [ApiParameter("id", "Resource ID", Required = true, Location = "path")] string id,
        [ApiParameter("data", "Update data", Required = true, Location = "body")] UpdateRequest request)
    {
        return Task.FromResult<object>(new
        {
            id = id,
            updated = request,
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// DELETE endpoint with query parameters
    /// </summary>
    [Delete("/example/delete/{id}", "delete", "Delete resource endpoint", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public Task<object> DeleteResource(
        [ApiParameter("id", "Resource ID", Required = true, Location = "path")] string id,
        [ApiParameter("confirm", "Confirmation flag", Required = false, Location = "query")] bool confirm = false)
    {
        if (!confirm)
        {
            return Task.FromResult<object>(new ErrorResponse("Deletion requires confirmation"));
        }

        return Task.FromResult<object>(new
        {
            deleted = id,
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// PATCH endpoint with complex request
    /// </summary>
    [Patch("/example/patch/{id}", "patch", "Patch resource endpoint", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    [ApiResponse(404, "Not found")]
    public Task<object> PatchResource(
        [ApiParameter("id", "Resource ID", Required = true, Location = "path")] string id,
        [ApiParameter("patch", "Patch data", Required = true, Location = "body")] PatchRequest request)
    {
        return Task.FromResult<object>(new
        {
            id = id,
            patched = request,
            timestamp = DateTime.UtcNow,
            module = "codex.example"
        });
    }

    /// <summary>
    /// Advanced endpoint with all features
    /// </summary>
    [ApiRoute("POST", "/example/advanced", "advanced", "Advanced endpoint with all features", "codex.example")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    [ApiResponse(401, "Unauthorized")]
    [ApiResponse(500, "Internal server error")]
    public Task<object> AdvancedEndpoint(
        [ApiParameter("data", "Request data", Required = true, Location = "body")] AdvancedRequest request)
    {
        return Task.FromResult<object>(new
        {
            processed = request,
            timestamp = DateTime.UtcNow,
            module = "codex.example",
            features = new[]
            {
                "attribute-based routing",
                "automatic parameter binding",
                "response type inference",
                "OpenAPI documentation"
            }
        });
    }
}

/// <summary>
/// Echo request model
/// </summary>
public class EchoRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Sender { get; set; }
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// Update request model
/// </summary>
public class UpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}

/// <summary>
/// Patch request model
/// </summary>
public class PatchRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? Active { get; set; }
}

/// <summary>
/// Advanced request model
/// </summary>
public class AdvancedRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
