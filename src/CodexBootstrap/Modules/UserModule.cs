using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// User Management Module (L7) - Modular Fractal API Design
/// Each API is self-contained with its own OpenAPI specification
/// </summary>
public class UserModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public UserModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public string ModuleId => "codex.user";
    public string Name => "User Management Module";
    public string Version => "1.0.0";
    public string Description => "User Management Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return ModuleHelpers.CreateModuleNode(ModuleId, Name, Version, Description);
    }

    public void Register(NodeRegistry registry)
    {
        // Module registration is now handled automatically by the attribute discovery system
        // This method can be used for additional module-specific setup if needed
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    /// <summary>
    /// Create a new user account
    /// </summary>
    [Post("/user/create", "user-create", "Create a new user account", "codex.user")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> CreateUser([ApiParameter("request", "User creation request", Required = true, Location = "body")] UserCreateRequest request)
    {
        try
        {
            // Create user node
            var userNode = new Node(
                Id: $"user.{request.Username}",
                TypeId: "codex.user",
                State: ContentState.Ice,
                Locale: "en",
                Title: request.Username,
                Description: $"User account for {request.Username}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        username = request.Username,
                        email = request.Email,
                        displayName = request.DisplayName,
                        createdAt = DateTime.UtcNow
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["username"] = request.Username,
                    ["email"] = request.Email,
                    ["displayName"] = request.DisplayName,
                    ["createdAt"] = DateTime.UtcNow,
                    ["status"] = "active"
                }
            );

            // Store the user in the registry
            _registry.Upsert(userNode);

            return new UserCreateResponse(
                Success: true,
                UserId: userNode.Id,
                Message: "User created successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to create user: {ex.Message}", "CREATE_ERROR");
        }
    }

    /// <summary>
    /// Authenticate user credentials
    /// </summary>
    [Post("/user/authenticate", "user-authenticate", "Authenticate user credentials", "codex.user")]
    [ApiResponse(200, "Success")]
    [ApiResponse(401, "Unauthorized")]
    public async Task<object> AuthenticateUser([ApiParameter("request", "User authentication request", Required = true, Location = "body")] UserAuthRequest request)
    {
        try
        {
            // Simple authentication logic (in real implementation, use proper auth)
            var isValid = !string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password);
            
            return new UserAuthResponse(
                Success: isValid,
                Token: isValid ? Guid.NewGuid().ToString() : null,
                Message: isValid ? "Authentication successful" : "Invalid credentials"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to authenticate user: {ex.Message}", "AUTH_ERROR");
        }
    }

    /// <summary>
    /// Get user profile
    /// </summary>
    [Get("/user/profile/{id}", "user-profile", "Get user profile", "codex.user")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetUserProfile([ApiParameter("id", "User ID", Required = true, Location = "path")] string id)
    {
        try
        {
            // In a real implementation, you would query the registry
            return new UserProfileResponse(
                UserId: id,
                Username: "sample_user",
                Email: "user@example.com",
                DisplayName: "Sample User",
                CreatedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get user profile: {ex.Message}", "PROFILE_ERROR");
        }
    }

    /// <summary>
    /// Get user permissions
    /// </summary>
    [Get("/user/permissions/{id}", "user-permissions", "Get user permissions", "codex.user")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetUserPermissions([ApiParameter("id", "User ID", Required = true, Location = "path")] string id)
    {
        try
        {
            return new UserPermissionsResponse(
                UserId: id,
                Permissions: new[] { "read", "write", "admin" },
                Roles: new[] { "user", "editor" }
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get user permissions: {ex.Message}", "PERMISSIONS_ERROR");
        }
    }

    /// <summary>
    /// Get user sessions
    /// </summary>
    [Get("/user/sessions/{id}", "user-sessions", "Get user sessions", "codex.user")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetUserSessions([ApiParameter("id", "User ID", Required = true, Location = "path")] string id)
    {
        try
        {
            return new UserSessionsResponse(
                UserId: id,
                Sessions: new[]
                {
                    new { id = "session1", createdAt = DateTime.UtcNow.AddHours(-1), isActive = true },
                    new { id = "session2", createdAt = DateTime.UtcNow.AddDays(-1), isActive = false }
                }
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get user sessions: {ex.Message}", "SESSIONS_ERROR");
        }
    }
}

// Request/Response DTOs for each API
public record UserCreateRequest(string Username, string Email, string DisplayName, string Password);
public record UserCreateResponse(bool Success, string UserId, string Message);

public record UserAuthRequest(string Username, string Password);
public record UserAuthResponse(bool Success, string? Token, string Message);

public record UserProfileRequest(string UserId);
public record UserProfileResponse(string UserId, string Username, string Email, string DisplayName, DateTime CreatedAt);

public record UserPermissionsRequest(string UserId);
public record UserPermissionsResponse(string UserId, string[] Permissions, string[] Roles);

public record UserSessionsRequest(string UserId);
public record UserSessionsResponse(string UserId, object[] Sessions);