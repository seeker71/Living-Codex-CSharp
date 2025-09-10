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
    
    public string ModuleId => "codex.user";
    public string Version => "1.0.0";
    public string Description => "User Management Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return new Node(
            Id: ModuleId,
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "User Management Module",
            Description: Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    moduleId = ModuleId,
                    version = Version,
                    description = Description,
                    apis = new[]
                    {
                        new { name = "create", spec = "/user/create/spec" },
                        new { name = "authenticate", spec = "/user/authenticate/spec" },
                        new { name = "profile", spec = "/user/profile/spec" },
                        new { name = "permissions", spec = "/user/permissions/spec" },
                        new { name = "sessions", spec = "/user/sessions/spec" }
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = ModuleId,
                ["version"] = Version,
                ["type"] = "user-management"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register module node
        registry.Upsert(GetModuleNode());

        // Register API nodes for RouteDiscovery
        var createApi = NodeStorage.CreateApiNode("codex.user", "create", "/user/create", "Create a new user account");
        var authenticateApi = NodeStorage.CreateApiNode("codex.user", "authenticate", "/user/authenticate", "Authenticate user credentials");
        var profileApi = NodeStorage.CreateApiNode("codex.user", "profile", "/user/profile/{id}", "Get or update user profile");
        var permissionsApi = NodeStorage.CreateApiNode("codex.user", "permissions", "/user/permissions/{id}", "Manage user permissions");
        var sessionsApi = NodeStorage.CreateApiNode("codex.user", "sessions", "/user/sessions/{id}", "Manage user sessions");

        registry.Upsert(createApi);
        registry.Upsert(authenticateApi);
        registry.Upsert(profileApi);
        registry.Upsert(permissionsApi);
        registry.Upsert(sessionsApi);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.user", "create"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.user", "authenticate"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.user", "profile"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.user", "permissions"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.user", "sessions"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // User Creation API
        router.Register("codex.user", "create", async (args) =>
        {
            var logger = new Log4NetLogger(typeof(UserModule));
            logger.Debug($"DEBUG: args is null = {args == null}");
            if (args != null)
            {
                var jsonString = JsonSerializer.Serialize(args);
                logger.Debug($"DEBUG: JSON string = {jsonString}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                var request = JsonSerializer.Deserialize<UserCreateRequest>(jsonString, options);
                logger.Debug($"DEBUG: Deserialized request = {request?.Username}");
                return await CreateUser(request ?? new UserCreateRequest("", "", "", ""), router.GetRegistry());
            }
            else
            {
                logger.Debug("DEBUG: args is null, using default request");
                return await CreateUser(new UserCreateRequest("", "", "", ""), router.GetRegistry());
            }
        });

        // User Authentication API
        router.Register("codex.user", "authenticate", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserAuthRequest>(JsonSerializer.Serialize(args));
            return await AuthenticateUser(request ?? new UserAuthRequest("", ""));
        });

        // User Profile API
        router.Register("codex.user", "profile", async (args) =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var request = JsonSerializer.Deserialize<UserProfileRequest>(JsonSerializer.Serialize(args), options);
            return await GetUserProfile(request ?? new UserProfileRequest(""), router.GetRegistry());
        });

        // User Permissions API
        router.Register("codex.user", "permissions", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserPermissionsRequest>(JsonSerializer.Serialize(args));
            return await GetUserPermissions(request ?? new UserPermissionsRequest(""));
        });

        // User Sessions API
        router.Register("codex.user", "sessions", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserSessionsRequest>(JsonSerializer.Serialize(args));
            return await GetUserSessions(request ?? new UserSessionsRequest(""));
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Each API endpoint is self-contained with its own OpenAPI spec
        app.MapPost("/user/create", async (UserCreateRequest request) =>
        {
            var result = await CreateUser(request, registry);
            return Results.Ok(result);
        })
        .WithName("CreateUser");

        app.MapPost("/user/authenticate", async (UserAuthRequest request) =>
        {
            var result = await AuthenticateUser(request);
            return Results.Ok(result);
        })
        .WithName("AuthenticateUser");

        app.MapGet("/user/profile/{id}", async (string id) =>
        {
            var request = new UserProfileRequest(id);
            var result = await GetUserProfile(request, registry);
            return Results.Ok(result);
        })
        .WithName("GetUserProfile");

        app.MapGet("/user/permissions/{id}", async (string id) =>
        {
            var request = new UserPermissionsRequest(id);
            var result = await GetUserPermissions(request);
            return Results.Ok(result);
        })
        .WithName("GetUserPermissions");

        app.MapGet("/user/sessions/{id}", async (string id) =>
        {
            var request = new UserSessionsRequest(id);
            var result = await GetUserSessions(request);
            return Results.Ok(result);
        })
        .WithName("GetUserSessions");
    }

    // API Implementation Methods
    private Task<object> CreateUser(UserCreateRequest request, NodeRegistry registry)
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

        // Store the user node in the registry
        registry.Upsert(userNode);

        return Task.FromResult<object>(new UserCreateResponse(
            Success: true,
            UserId: userNode.Id,
            Message: "User created successfully"
        ));
    }

    private Task<object> AuthenticateUser(UserAuthRequest request)
    {
        // Simple authentication logic (in real implementation, use proper auth)
        var isValid = !string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password);
        
        return Task.FromResult<object>(new UserAuthResponse(
            Success: isValid,
            Token: isValid ? Guid.NewGuid().ToString() : null,
            Message: isValid ? "Authentication successful" : "Invalid credentials"
        ));
    }

    private Task<object> GetUserProfile(UserProfileRequest request, NodeRegistry registry)
    {
        // Query the registry for the user node
        if (!registry.TryGet(request.UserId, out var userNode))
        {
            return Task.FromResult<object>(new UserProfileResponse(
                UserId: request.UserId,
                Username: "not_found",
                Email: "not_found",
                DisplayName: "User Not Found",
                CreatedAt: DateTime.MinValue
            ));
        }

        // Extract data from the user node
        var username = userNode.Meta?.TryGetValue("username", out var usernameValue) == true ? usernameValue.ToString() ?? "unknown" : "unknown";
        var email = userNode.Meta?.TryGetValue("email", out var emailValue) == true ? emailValue.ToString() ?? "unknown" : "unknown";
        var displayName = userNode.Meta?.TryGetValue("displayName", out var displayNameValue) == true ? displayNameValue.ToString() ?? "unknown" : "unknown";
        var createdAt = userNode.Meta?.TryGetValue("createdAt", out var createdAtValue) == true && createdAtValue is DateTime dateTime ? dateTime : DateTime.UtcNow;

        return Task.FromResult<object>(new UserProfileResponse(
            UserId: userNode.Id,
            Username: username,
            Email: email,
            DisplayName: displayName,
            CreatedAt: createdAt
        ));
    }

    private Task<object> GetUserPermissions(UserPermissionsRequest request)
    {
        return Task.FromResult<object>(new UserPermissionsResponse(
            UserId: request.UserId,
            Permissions: new[] { "read", "write", "admin" },
            Roles: new[] { "user", "editor" }
        ));
    }

    private Task<object> GetUserSessions(UserSessionsRequest request)
    {
        return Task.FromResult<object>(new UserSessionsResponse(
            UserId: request.UserId,
            Sessions: new[]
            {
                new { id = "session1", createdAt = DateTime.UtcNow.AddHours(-1), isActive = true },
                new { id = "session2", createdAt = DateTime.UtcNow.AddDays(-1), isActive = false }
            }
        ));
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