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
    private readonly NodeRegistry _registry;

    public UserModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public string ModuleId => "codex.user";
    public string Version => "1.0.0";
    public string Description => "User Management Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: ModuleId,
            name: "User Management Module",
            version: Version,
            description: Description,
            capabilities: new[] { "user-creation", "authentication", "profile-management", "permissions", "session-management" },
            tags: new[] { "user", "management", "auth", "profile" }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // User Creation API
        router.Register("codex.user", "create", (args) =>
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
                return CreateUser(request ?? new UserCreateRequest("", "", "", ""), router.GetRegistry());
            }
            else
            {
                logger.Debug("DEBUG: args is null, using default request");
                return CreateUser(new UserCreateRequest("", "", "", ""), router.GetRegistry());
            }
        });

        // User Authentication API
        router.Register("codex.user", "authenticate", (args) =>
        {
            var request = JsonSerializer.Deserialize<UserAuthRequest>(JsonSerializer.Serialize(args));
            return AuthenticateUser(request ?? new UserAuthRequest("", ""));
        });

        // User Profile API
        router.Register("codex.user", "profile", (args) =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var request = JsonSerializer.Deserialize<UserProfileRequest>(JsonSerializer.Serialize(args), options);
            return GetUserProfile(request ?? new UserProfileRequest(""), router.GetRegistry());
        });

        // User Permissions API
        router.Register("codex.user", "permissions", (args) =>
        {
            var request = JsonSerializer.Deserialize<UserPermissionsRequest>(JsonSerializer.Serialize(args));
            return GetUserPermissions(request ?? new UserPermissionsRequest(""));
        });

        // User Sessions API
        router.Register("codex.user", "sessions", (args) =>
        {
            var request = JsonSerializer.Deserialize<UserSessionsRequest>(JsonSerializer.Serialize(args));
            return GetUserSessions(request ?? new UserSessionsRequest(""));
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Each API endpoint is self-contained with its own OpenAPI spec
        app.MapPost("/user/create", (UserCreateRequest request) =>
        {
            var result = CreateUser(request, registry);
            return Results.Ok(result);
        })
        .WithName("CreateUser");

        app.MapPost("/user/authenticate", (UserAuthRequest request) =>
        {
            var result = AuthenticateUser(request);
            return Results.Ok(result);
        })
        .WithName("AuthenticateUser");

        app.MapGet("/user/profile/{id}", (string id) =>
        {
            var request = new UserProfileRequest(id);
            var result = GetUserProfile(request, registry);
            return Results.Ok(result);
        })
        .WithName("GetUserProfile");

        app.MapGet("/user/permissions/{id}", (string id) =>
        {
            var request = new UserPermissionsRequest(id);
            var result = GetUserPermissions(request);
            return Results.Ok(result);
        })
        .WithName("GetUserPermissions");

        app.MapGet("/user/sessions/{id}", (string id) =>
        {
            var request = new UserSessionsRequest(id);
            var result = GetUserSessions(request);
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

    /// <summary>
    /// Create a new user account
    /// </summary>
    [ApiRoute("POST", "/user/create", "create", "Create a new user account", "codex.user")]
    public async Task<object> CreateUserAsync([ApiParameter("body", "User creation request")] UserCreateRequest request)
    {
        return await Task.FromResult(CreateUser(request, _registry));
    }

    /// <summary>
    /// Authenticate user credentials
    /// </summary>
    [ApiRoute("POST", "/user/authenticate", "authenticate", "Authenticate user credentials", "codex.user")]
    public async Task<object> AuthenticateUserAsync([ApiParameter("body", "User authentication request")] UserAuthRequest request)
    {
        return await Task.FromResult(AuthenticateUser(request));
    }

    /// <summary>
    /// Get or update user profile
    /// </summary>
    [ApiRoute("GET", "/user/profile/{id}", "profile", "Get or update user profile", "codex.user")]
    public async Task<object> GetUserProfileAsync([ApiParameter("path", "User ID")] string id)
    {
        var request = new UserProfileRequest(id);
        return await Task.FromResult(GetUserProfile(request, _registry));
    }

    /// <summary>
    /// Manage user permissions
    /// </summary>
    [ApiRoute("GET", "/user/permissions/{id}", "permissions", "Manage user permissions", "codex.user")]
    public async Task<object> GetUserPermissionsAsync([ApiParameter("path", "User ID")] string id)
    {
        var request = new UserPermissionsRequest(id);
        return await Task.FromResult(GetUserPermissions(request));
    }

    /// <summary>
    /// Manage user sessions
    /// </summary>
    [ApiRoute("GET", "/user/sessions/{id}", "sessions", "Manage user sessions", "codex.user")]
    public async Task<object> GetUserSessionsAsync([ApiParameter("path", "User ID")] string id)
    {
        var request = new UserSessionsRequest(id);
        return await Task.FromResult(GetUserSessions(request));
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