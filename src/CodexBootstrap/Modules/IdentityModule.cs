using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Identity Module - Unified identity, authentication, and access management
/// Provides generic identity services including authentication, authorization, user management, and access control
/// </summary>
[MetaNode(Id = "codex.identity", Name = "Identity Module", Description = "Unified identity, authentication, and access management system")]
public sealed class IdentityModule : ModuleBase
{
    private readonly IdentityProviderRegistry _providerRegistry;

    public override string Name => "Identity Module";
    public override string Description => "Unified identity, authentication, and access management system";
    public override string Version => "1.0.0";

    public IdentityModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _providerRegistry = new IdentityProviderRegistry(logger);
    }

    /// <summary>
    /// Gets the registry to use - now always the unified registry
    /// </summary>
    private INodeRegistry Registry => _registry;

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.identity",
            name: "Identity Module",
            version: "1.0.0",
            description: "Unified identity, authentication, and access management system",
            tags: new[] { "identity", "auth", "access", "users", "security", "permissions" },
            capabilities: new[] { 
                "identity", "authentication", "authorization", "user-management", 
                "access-control", "session-management", "permissions", "providers" 
            },
            spec: "codex.spec.identity"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Identity Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attribute-based routing
        _logger.Info("Identity Module HTTP endpoints registered");
    }

    // Generic Identity Endpoints
    [ApiRoute("GET", "/identity/providers", "Get Identity Providers", "Get available identity providers", "codex.identity")]
    public async Task<object> GetIdentityProvidersAsync()
    {
        try
        {
            var allProviders = _providerRegistry.GetAllProviders();
            var providers = allProviders.Select(p => new IdentityProviderInfo(
                p.Key, 
                p.Value.ProviderName, 
                $"{p.Key}-client-id", // This would come from configuration
                p.Value.IsEnabled
            )).ToList();

            return new IdentityProvidersResponse(providers);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting identity providers: {ex.Message}", ex);
            return new IdentityProvidersResponse(new List<IdentityProviderInfo>());
        }
    }

    [ApiRoute("GET", "/identity/login/{provider}", "Initiate Login", "Initiate login with specified identity provider", "codex.identity")]
    public async Task<object> InitiateLoginAsync(
        [ApiParameter("provider", "Identity provider name", Location = "path")] string provider,
        [ApiParameter("returnUrl", "URL to return to after login", Required = false, Location = "query")] string? returnUrl = null)
    {
        try
        {
            // Simple debug output to console
            Console.WriteLine($"[DEBUG] InitiateLoginAsync called with provider: {provider}, returnUrl: {returnUrl}");
            
            var identityProvider = _providerRegistry.GetProvider(provider);
            if (identityProvider == null)
            {
                Console.WriteLine($"[DEBUG] Identity provider not found: {provider}");
                return new { success = false, error = $"Provider '{provider}' not found" };
            }

            if (!identityProvider.IsEnabled)
            {
                Console.WriteLine($"[DEBUG] Identity provider is disabled: {provider}");
                return new { success = false, error = $"Provider '{provider}' is disabled" };
            }

            Console.WriteLine($"[DEBUG] Calling InitiateLogin on provider: {provider}");
            var result = await identityProvider.InitiateLogin(returnUrl);
            // Avoid serializing arbitrary objects for logging to prevent exceptions
            _logger.Info($"Initiated login for provider: {provider}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"Identity login initiation error: {ex.Message}", ex);
            return new { success = false, error = "Failed to initiate login" };
        }
    }

    [ApiRoute("GET", "/identity/callback/{provider}", "Handle Callback", "Handle identity provider callback", "codex.identity")]
    public async Task<object> HandleCallbackAsync(
        [ApiParameter("provider", "Identity provider name", Location = "path")] string provider,
        [ApiParameter("code", "Authorization code from provider", Required = true, Location = "query")] string code,
        [ApiParameter("state", "State parameter for validation", Required = true, Location = "query")] string state,
        [ApiParameter("returnUrl", "URL to return to after login", Required = false, Location = "query")] string? returnUrl = null)
    {
        try
        {
            var identityProvider = _providerRegistry.GetProvider(provider);
            if (identityProvider == null)
            {
                _logger.Warn($"Identity provider not found: {provider}");
                return new IdentityCallbackResponse(provider, false, Error: $"Provider '{provider}' not found");
            }

            if (!identityProvider.IsEnabled)
            {
                _logger.Warn($"Identity provider is disabled: {provider}");
                return new IdentityCallbackResponse(provider, false, Error: $"Provider '{provider}' is disabled");
            }

            return await identityProvider.HandleCallbackAsync(code, state, returnUrl);
        }
        catch (Exception ex)
        {
            _logger.Error($"Identity callback error: {ex.Message}", ex);
            return new IdentityCallbackResponse(provider, false, Error: "Identity callback failed");
        }
    }

    [ApiRoute("GET", "/identity/userinfo/{provider}", "Get User Info", "Get user information from identity provider", "codex.identity")]
    public async Task<object> GetUserInfoAsync(
        [ApiParameter("provider", "Identity provider name", Location = "path")] string provider,
        [ApiParameter("accessToken", "Access token from provider", Required = true, Location = "query")] string accessToken)
    {
        try
        {
            var identityProvider = _providerRegistry.GetProvider(provider);
            if (identityProvider == null)
            {
                _logger.Warn($"Identity provider not found: {provider}");
                return new { success = false, error = $"Provider '{provider}' not found" };
            }

            if (!identityProvider.IsEnabled)
            {
                _logger.Warn($"Identity provider is disabled: {provider}");
                return new { success = false, error = $"Provider '{provider}' is disabled" };
            }

            var userInfo = await identityProvider.GetUserInfoAsync(accessToken);
            return new { success = true, userInfo };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user info: {ex.Message}", ex);
            return new { success = false, error = "Failed to get user info" };
        }
    }

    // User Management Endpoints
    [ApiRoute("POST", "/identity/users", "Create User", "Create a new user identity", "codex.identity")]
    public async Task<object> CreateUserAsync([ApiParameter("body", "User creation request")] UserCreateRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = Registry.GetNode($"user.{request.Username}");
            if (existingUser != null)
            {
                return new UserCreateResponse(false, null, "User already exists");
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
                Description: $"User identity for {request.Username}",
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
            Registry.Upsert(userNode);

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
            Registry.Upsert(emailIndexEdge);

            return new UserCreateResponse(true, userNode.Id, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating user: {ex.Message}", ex);
            return new UserCreateResponse(false, null, $"Error creating user: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/identity/authenticate", "Authenticate User", "Authenticate user credentials", "codex.identity")]
    public async Task<object> AuthenticateUserAsync([ApiParameter("body", "User authentication request")] UserAuthRequest request)
    {
        try
        {
            // Get user by username
            var userNode = Registry.GetNode($"user.{request.Username}");
            if (userNode == null)
            {
                return new UserAuthResponse(false, null, "Invalid credentials");
            }

            // Check if user is active
            if (!userNode.Meta.ContainsKey("isActive") || !(bool)userNode.Meta["isActive"])
            {
                return new UserAuthResponse(false, null, "Account is disabled");
            }

            // Verify password
            var storedHash = userNode.Meta["passwordHash"]?.ToString();
            if (storedHash == null || !VerifyPassword(request.Password, storedHash))
            {
                return new UserAuthResponse(false, null, "Invalid credentials");
            }

            // Generate token
            var token = GenerateJwtToken(request.Username, userNode.Meta["email"]?.ToString() ?? "");

            return new UserAuthResponse(true, token, "Authentication successful");
        }
        catch (Exception ex)
        {
            _logger.Error($"Authentication error: {ex.Message}", ex);
            return new UserAuthResponse(false, null, $"Authentication error: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/identity/users/{id}", "Get User Profile", "Get user identity information", "codex.identity")]
    public async Task<object> GetUserProfileAsync([ApiParameter("id", "User ID", Location = "path")] string id)
    {
        try
        {
            // Query the registry for the user node
            if (!Registry.TryGet(id, out var userNode))
            {
                return new UserProfileResponse(id, "not_found", "not_found", "User Not Found", DateTime.MinValue);
            }

            // Extract data from the user node
            var username = userNode.Meta?.TryGetValue("username", out var usernameValue) == true ? usernameValue.ToString() ?? "unknown" : "unknown";
            var email = userNode.Meta?.TryGetValue("email", out var emailValue) == true ? emailValue.ToString() ?? "unknown" : "unknown";
            var displayName = userNode.Meta?.TryGetValue("displayName", out var displayNameValue) == true ? displayNameValue.ToString() ?? "unknown" : "unknown";
            var createdAt = userNode.Meta?.TryGetValue("createdAt", out var createdAtValue) == true && createdAtValue is DateTime dateTime ? dateTime : DateTime.UtcNow;

            return new UserProfileResponse(userNode.Id, username, email, displayName, createdAt);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user profile: {ex.Message}", ex);
            return new UserProfileResponse(id, "error", "error", "Error retrieving profile", DateTime.MinValue);
        }
    }

    // Session Management Endpoints
    [ApiRoute("POST", "/identity/sessions", "Create Session", "Create a new user session", "codex.identity")]
    public async Task<object> CreateSessionAsync([ApiParameter("body", "Session creation request")] SessionCreateRequest request)
    {
        try
        {
            // Generate session token
            var sessionToken = GenerateSessionToken(request.UserId);
            
            // Create session node
            var sessionNode = new Node(
                Id: $"session.{sessionToken}",
                TypeId: "codex.session",
                State: ContentState.Ice,
                Locale: "en",
                Title: $"Session for {request.UserId}",
                Description: $"User session created at {DateTime.UtcNow}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        userId = request.UserId,
                        createdAt = DateTime.UtcNow,
                        expiresAt = DateTime.UtcNow.AddHours(24),
                        isActive = true
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["userId"] = request.UserId,
                    ["sessionToken"] = sessionToken,
                    ["createdAt"] = DateTime.UtcNow,
                    ["expiresAt"] = DateTime.UtcNow.AddHours(24),
                    ["isActive"] = true
                }
            );

            Registry.Upsert(sessionNode);

            return new SessionCreateResponse(true, sessionToken, "Session created successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating session: {ex.Message}", ex);
            return new SessionCreateResponse(false, null, $"Error creating session: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/identity/sessions/{token}", "End Session", "End a user session", "codex.identity")]
    public async Task<object> EndSessionAsync([ApiParameter("token", "Session token", Location = "path")] string token)
    {
        try
        {
            var sessionId = $"session.{token}";
            var sessionNode = Registry.GetNode(sessionId);
            
            if (sessionNode == null)
            {
                return new SessionEndResponse(false, "Session not found");
            }

            // Mark session as inactive
            sessionNode.Meta["isActive"] = false;
            sessionNode.Meta["endedAt"] = DateTime.UtcNow;
            Registry.Upsert(sessionNode);

            return new SessionEndResponse(true, "Session ended successfully");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ending session: {ex.Message}", ex);
            return new SessionEndResponse(false, $"Error ending session: {ex.Message}");
        }
    }

    // Helper methods
    private string HashPassword(string password)
    {
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
        return $"jwt-{username}-{email}-{DateTime.UtcNow.Ticks}";
    }

    private string GenerateSessionToken(string userId)
    {
        return $"session-{userId}-{DateTime.UtcNow.Ticks}";
    }
}

// Identity Request/Response Types
[ResponseType("codex.identity.provider-info", "IdentityProviderInfo", "Information about an identity provider")]
public record IdentityProviderInfo(string Provider, string DisplayName, string ClientId, bool IsEnabled);

[ResponseType("codex.identity.providers-response", "IdentityProvidersResponse", "Response containing available identity providers")]
public record IdentityProvidersResponse(List<IdentityProviderInfo> providers);

[ResponseType("codex.identity.callback-response", "IdentityCallbackResponse", "Response for identity provider callback")]
public record IdentityCallbackResponse(string Provider, bool Success, string? Token = null, string? Error = null);

[ResponseType("codex.identity.user-create-request", "UserCreateRequest", "Request for user creation")]
public record UserCreateRequest(string Username, string Email, string DisplayName, string Password);

[ResponseType("codex.identity.user-create-response", "UserCreateResponse", "Response for user creation")]
public record UserCreateResponse(bool Success, string? UserId, string Message);

[ResponseType("codex.identity.user-auth-request", "UserAuthRequest", "Request for user authentication")]
public record UserAuthRequest(string Username, string Password);

[ResponseType("codex.identity.user-auth-response", "UserAuthResponse", "Response for user authentication")]
public record UserAuthResponse(bool Success, string? Token, string Message);

[ResponseType("codex.identity.user-profile-response", "UserProfileResponse", "Response for user profile")]
public record UserProfileResponse(string UserId, string Username, string Email, string DisplayName, DateTime CreatedAt);

[ResponseType("codex.identity.session-create-request", "SessionCreateRequest", "Request for session creation")]
public record SessionCreateRequest(string UserId);

[ResponseType("codex.identity.session-create-response", "SessionCreateResponse", "Response for session creation")]
public record SessionCreateResponse(bool Success, string? SessionToken, string Message);

[ResponseType("codex.identity.session-end-response", "SessionEndResponse", "Response for session ending")]
public record SessionEndResponse(bool Success, string Message);
