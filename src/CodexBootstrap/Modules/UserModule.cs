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
            tags: new[] { "user", "management", "auth", "profile" },
            specReference: "codex.spec.user"
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
        try
        {
            // Check if user already exists
            var existingUser = registry.GetNode($"user.{request.Username}");
            if (existingUser != null)
            {
                return Task.FromResult<object>(new UserCreateResponse(
                    Success: false,
                    UserId: null,
                    Message: "User already exists"
                ));
            }

            // Hash password
            var passwordHash = HashPassword(request.Password);

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
                        createdAt = DateTime.UtcNow,
                        isActive = true
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["username"] = request.Username,
                    ["email"] = request.Email,
                    ["displayName"] = request.DisplayName,
                    ["passwordHash"] = passwordHash,
                    ["createdAt"] = DateTime.UtcNow,
                    ["updatedAt"] = DateTime.UtcNow,
                    ["status"] = "active",
                    ["isActive"] = true
                }
            );

            // Store the user node in the registry
            registry.Upsert(userNode);

            // Create email index edge for fast lookup
            var emailIndexEdge = new Edge(
                FromId: "email-index",
                ToId: userNode.Id,
                Role: "email-index",
                Weight: 1.0,
                Meta: new Dictionary<string, object>
                {
                    ["email"] = request.Email,
                    ["username"] = request.Username
                }
            );
            registry.Upsert(emailIndexEdge);

            return Task.FromResult<object>(new UserCreateResponse(
                Success: true,
                UserId: userNode.Id,
                Message: "User created successfully"
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult<object>(new UserCreateResponse(
                Success: false,
                UserId: null,
                Message: $"Error creating user: {ex.Message}"
            ));
        }
    }

    private Task<object> AuthenticateUser(UserAuthRequest request)
    {
        try
        {
            // Get user by username
            var userNode = _registry.GetNode($"user.{request.Username}");
            if (userNode == null)
            {
                return Task.FromResult<object>(new UserAuthResponse(
                    Success: false,
                    Token: null,
                    Message: "Invalid credentials"
                ));
            }

            // Check if user is active
            if (!userNode.Meta.ContainsKey("isActive") || !(bool)userNode.Meta["isActive"])
            {
                return Task.FromResult<object>(new UserAuthResponse(
                    Success: false,
                    Token: null,
                    Message: "Account is disabled"
                ));
            }

            // Verify password
            var storedHash = userNode.Meta["passwordHash"]?.ToString();
            if (storedHash == null || !VerifyPassword(request.Password, storedHash))
            {
                return Task.FromResult<object>(new UserAuthResponse(
                    Success: false,
                    Token: null,
                    Message: "Invalid credentials"
                ));
            }

            // Generate token
            var token = GenerateJwtToken(request.Username, userNode.Meta["email"]?.ToString() ?? "");

            return Task.FromResult<object>(new UserAuthResponse(
                Success: true,
                Token: token,
                Message: "Authentication successful"
            ));
        }
        catch (Exception ex)
        {
            return Task.FromResult<object>(new UserAuthResponse(
                Success: false,
                Token: null,
                Message: $"Authentication error: {ex.Message}"
            ));
        }
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

    // Helper methods
    private string HashPassword(string password)
    {
        // In a real system, use proper password hashing like BCrypt or Argon2
        // For now, we'll use a simple hash for demonstration
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }

    private string GenerateJwtToken(string username, string email)
    {
        // In a real system, generate a proper JWT token
        // For now, we'll return a simple token for demonstration
        return $"jwt-{username}-{email}-{DateTime.UtcNow.Ticks}";
    }
}

// Request/Response DTOs for each API
[ResponseType("codex.user.create-request", "UserCreateRequest", "Request for user creation")]
public record UserCreateRequest(string Username, string Email, string DisplayName, string Password);

[ResponseType("codex.user.create-response", "UserCreateResponse", "Response for user creation")]
public record UserCreateResponse(bool Success, string UserId, string Message);

[ResponseType("codex.user.auth-request", "UserAuthRequest", "Request for user authentication")]
public record UserAuthRequest(string Username, string Password);

[ResponseType("codex.user.auth-response", "UserAuthResponse", "Response for user authentication")]
public record UserAuthResponse(bool Success, string? Token, string Message);

[ResponseType("codex.user.profile-request", "UserProfileRequest", "Request for user profile")]
public record UserProfileRequest(string UserId);

[ResponseType("codex.user.profile-response", "UserProfileResponse", "Response for user profile")]
public record UserProfileResponse(string UserId, string Username, string Email, string DisplayName, DateTime CreatedAt);

[ResponseType("codex.user.permissions-request", "UserPermissionsRequest", "Request for user permissions")]
public record UserPermissionsRequest(string UserId);

[ResponseType("codex.user.permissions-response", "UserPermissionsResponse", "Response for user permissions")]
public record UserPermissionsResponse(string UserId, string[] Permissions, string[] Roles);

[ResponseType("codex.user.sessions-request", "UserSessionsRequest", "Request for user sessions")]
public record UserSessionsRequest(string UserId);

[ResponseType("codex.user.sessions-response", "UserSessionsResponse", "Response for user sessions")]
public record UserSessionsResponse(string UserId, object[] Sessions);