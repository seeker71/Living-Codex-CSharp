using System.Collections.Concurrent;
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
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
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
    private readonly string _jwtSecret;
    private readonly int _tokenExpirationHours;
    private readonly ConcurrentDictionary<string, UserSession> _activeSessions;
    private readonly ConcurrentDictionary<string, DateTime> _revokedTokens; // Track revocation time for cleanup
    private Timer? _cleanupTimer;
    
    // Memory management constants
    private const int MAX_ACTIVE_SESSIONS = 10000;
    private const int MAX_REVOKED_TOKENS = 50000;
    private const int CLEANUP_INTERVAL_MINUTES = 15;
    private const int REVOKED_TOKEN_TTL_HOURS = 48;

    public override string Name => "Identity Module";
    public override string Description => "Unified identity, authentication, and access management system";
    public override string Version => "2.0.0";

    public IdentityModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _providerRegistry = new IdentityProviderRegistry(logger);
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "default-jwt-secret-key-for-development-only";
        _tokenExpirationHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_HOURS"), out var hours) ? hours : 24;
        _activeSessions = new ConcurrentDictionary<string, UserSession>();
        _revokedTokens = new ConcurrentDictionary<string, DateTime>();
        
        // Start cleanup timer to prevent memory leaks
        _cleanupTimer = new Timer(
            CleanupExpiredData,
            null,
            TimeSpan.FromMinutes(5), // First cleanup after 5 minutes
            TimeSpan.FromMinutes(CLEANUP_INTERVAL_MINUTES)
        );
        
        _logger.Info($"IdentityModule initialized with cleanup every {CLEANUP_INTERVAL_MINUTES} minutes");
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
            version: "2.0.0",
            description: "Unified identity, authentication, and access management system with JWT tokens",
            tags: new[] { "identity", "auth", "access", "users", "security", "permissions", "jwt", "sessions" },
            capabilities: new[] { 
                "identity", "authentication", "authorization", "user-management", 
                "access-control", "session-management", "permissions", "providers",
                "user-registration", "user-login", "jwt-tokens", "password-management"
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

    [ApiRoute("POST", "/identity/validate", "Validate OAuth Token", "Validate an identity provider access token", "codex.identity")]
    public object ValidateAccessToken(
        [ApiParameter("request", "Identity validation request", Required = true, Location = "body")] IdentityValidationRequest request)
    {
        if (request == null)
        {
            return new { success = false, error = "Request body is required" };
        }

        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return new { success = false, error = "Provider and accessToken are required" };
        }

        var providerKey = request.Provider.Trim().ToLowerInvariant();
        var provider = _providerRegistry.GetProvider(providerKey);

        if (provider == null)
        {
            _logger.Warn($"Identity validation failed - provider not found: {request.Provider}");
            return new { success = false, error = $"Provider '{request.Provider}' not found" };
        }

        if (!provider.IsEnabled)
        {
            _logger.Warn($"Identity validation failed - provider disabled: {request.Provider}");
            return new { success = false, error = $"Provider '{request.Provider}' is disabled" };
        }

        // In lieu of real provider validation, echo back a successful mock response
        return new
        {
            success = true,
            provider = provider.ProviderName,
            isValid = true,
            userId = string.IsNullOrWhiteSpace(request.UserId) ? "anonymous" : request.UserId,
            issuedAt = DateTimeOffset.UtcNow,
            scopes = new[] { "profile", "email" }
        };
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
                Id: $"codex.user.{request.Username}.{Guid.NewGuid():N}",
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
            var emailIndexEdge = NodeHelpers.CreateEdge(
                fromId: "email-index",
                toId: userNode.Id,
                role: "email-index",
                weight: 1.0,
                meta: new Dictionary<string, object>
                {
                    ["email"] = request.Email,
                    ["username"] = request.Username
                },
                roleId: NodeHelpers.TryResolveRoleId(Registry, "email-index")
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
            // Get user by username - search for users with matching username in meta
            var userNodes = Registry.GetNodesByType("codex.user").ToList();
            var userNode = userNodes.FirstOrDefault(u => 
                u.Meta?.ContainsKey("username") == true && 
                u.Meta["username"]?.ToString() == request.Username);
            
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
                Id: $"codex.session.{sessionToken}.{Guid.NewGuid():N}",
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

    // ===============================
    // UNIFIED AUTHENTICATION ENDPOINTS
    // ===============================

    [ApiRoute("POST", "/auth/register", "RegisterUser", "Register a new user account", "codex.identity")]
    public async Task<IResult> RegisterUserAsync([ApiParameter("request", "User registration request")] AuthRegisterRequest request)
    {
        try
        {
            // Comprehensive validation
            var validationResult = ValidateRegistrationRequest(request);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(new AuthResponse(false, null, null, validationResult.ErrorMessage));
            }

            // Check if user already exists
            var existingUser = Registry.GetNode($"user.{request.Username}");
            if (existingUser != null)
            {
                return Results.Conflict(new AuthResponse(false, null, null, "Username already exists"));
            }

            // Check if email is already registered
            var emailExists = await CheckEmailExistsAsync(request.Email);
            if (emailExists)
            {
                return Results.Conflict(new AuthResponse(false, null, null, "Email already registered"));
            }

            // Create user
            var userId = $"user.{request.Username}";
            var passwordHash = HashPasswordSecure(request.Password);
            var userNode = CreateUserNode(userId, request.Username, request.Email, request.DisplayName, passwordHash);
            
            Registry.Upsert(userNode);
            
            // Create email index for fast lookup
            CreateEmailIndex(request.Email, userId, request.Username);

            // Generate welcome token
            var token = GenerateJwtTokenSecure(userId, request.Username, request.Email);
            var userProfile = CreateAuthUserProfile(userNode);

            _logger.Info($"User registered successfully: {request.Username} ({request.Email})");
            return Results.Json(new AuthResponse(true, token, userProfile, "Registration successful"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Registration error for {request.Username}: {ex.Message}", ex);
            return Results.Json(new AuthResponse(false, null, null, "Registration failed due to system error"), statusCode: 500);
        }
    }

    [ApiRoute("POST", "/auth/login", "LoginUser", "Authenticate user credentials", "codex.identity")]
    public async Task<IResult> LoginUserAsync([ApiParameter("request", "User login request")] AuthLoginRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.UsernameOrEmail) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new AuthResponse(false, null, null, "Username/email and password are required"));
            }

            // Find user by username or email
            var userNode = await FindUserByUsernameOrEmailAsync(request.UsernameOrEmail);
            if (userNode == null)
            {
                return Results.Json(new AuthResponse(false, null, null, "Invalid credentials"), statusCode: 401);
            }

            // Check if account is active
            if (!IsUserActive(userNode))
            {
                return Results.Json(new AuthResponse(false, null, null, "Account is disabled or inactive"), statusCode: 403);
            }

            // Verify password
            var storedHash = userNode.Meta["passwordHash"]?.ToString();
            if (storedHash == null || !VerifyPasswordSecure(request.Password, storedHash))
            {
                await RecordFailedLoginAttempt(userNode.Id);
                return Results.Json(new AuthResponse(false, null, null, "Invalid credentials"), statusCode: 401);
            }

            // Update last login
            await UpdateLastLoginAsync(userNode);

            // Generate token and create session
            var token = GenerateJwtTokenSecure(userNode.Id, userNode.Meta["username"]?.ToString() ?? "", userNode.Meta["email"]?.ToString() ?? "");
            var session = CreateUserSessionRecord(userNode.Id, token, request.RememberMe);
            _activeSessions[token] = session;

            var userProfile = CreateAuthUserProfile(userNode);

            _logger.Info($"User logged in successfully: {userNode.Meta["username"]}");
            return Results.Json(new AuthResponse(true, token, userProfile, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Login error: {ex.Message}", ex);
            return Results.Json(new AuthResponse(false, null, null, "Authentication failed due to system error"), statusCode: 500);
        }
    }

    [ApiRoute("POST", "/auth/logout", "LogoutUser", "Logout user and invalidate session", "codex.identity")]
    public async Task<IResult> LogoutUserAsync([ApiParameter("request", "Logout request")] AuthLogoutRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Results.BadRequest(new { success = false, message = "Token is required" });
            }

            // Revoke token (track revocation time for cleanup)
            _revokedTokens.TryAdd(request.Token, DateTime.UtcNow);
            _activeSessions.TryRemove(request.Token, out _);

            // Update user's last activity
            var claims = ValidateJwtTokenSecure(request.Token);
            if (claims != null)
            {
                var userId = claims.FindFirst("userId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    await UpdateLastActivityAsync(userId);
                }
            }

            return Results.Json(new { success = true, message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Logout error: {ex.Message}", ex);
            return Results.Json(new { success = false, message = "Logout failed" }, statusCode: 500);
        }
    }

    [ApiRoute("POST", "/auth/validate", "ValidateToken", "Validate JWT token", "codex.identity")]
    public async Task<IResult> ValidateTokenAsync([ApiParameter("request", "Token validation request")] AuthTokenValidationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Results.BadRequest(new AuthTokenValidationResponse(false, null, "Token is required"));
            }

            // Check if token is revoked
            if (_revokedTokens.ContainsKey(request.Token))
            {
                return Results.Json(new AuthTokenValidationResponse(false, null, "Token has been revoked"), statusCode: 401);
            }

            // Validate JWT token
            var claims = ValidateJwtTokenSecure(request.Token);
            if (claims == null)
            {
                return Results.Json(new AuthTokenValidationResponse(false, null, "Invalid or expired token"), statusCode: 401);
            }

            var userId = claims.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Json(new AuthTokenValidationResponse(false, null, "Invalid token claims"), statusCode: 401);
            }

            // Get user profile
            var userNode = Registry.GetNode(userId);
            if (userNode == null || !IsUserActive(userNode))
            {
                return Results.Json(new AuthTokenValidationResponse(false, null, "User not found or inactive"), statusCode: 401);
            }

            var userProfile = CreateAuthUserProfile(userNode);
            return Results.Json(new AuthTokenValidationResponse(true, userProfile, "Token is valid"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Token validation error: {ex.Message}", ex);
            return Results.Json(new AuthTokenValidationResponse(false, null, "Token validation failed"), statusCode: 500);
        }
    }

    [ApiRoute("GET", "/auth/profile/{userId}", "GetAuthUserProfile", "Get user profile by ID", "codex.identity")]
    public async Task<IResult> GetAuthUserProfileAsync([ApiParameter("userId", "User ID", Location = "path")] string userId)
    {
        try
        {
            var userNode = Registry.GetNode(userId);
            if (userNode == null)
            {
                return Results.NotFound(new { success = false, message = "User not found" });
            }

            var profile = CreateAuthUserProfile(userNode);
            return Results.Json(new { success = true, profile = profile });
        }
        catch (Exception ex)
        {
            _logger.Error($"Get profile error: {ex.Message}", ex);
            return Results.Json(new { success = false, message = "Failed to get user profile" }, statusCode: 500);
        }
    }

    [ApiRoute("PUT", "/auth/profile/{userId}", "UpdateAuthUserProfile", "Update user profile", "codex.identity")]
    public async Task<IResult> UpdateAuthUserProfileAsync(
        [ApiParameter("userId", "User ID", Location = "path")] string userId,
        [ApiParameter("body", "Profile update data")] AuthProfileUpdateRequest request)
    {
        try
        {
            var userNode = Registry.GetNode(userId);
            if (userNode == null)
            {
                return Results.NotFound(new { success = false, message = "User not found" });
            }

            // Update user node meta
            var updatedMeta = new Dictionary<string, object>(userNode.Meta);
            
            if (request.DisplayName != null)
                updatedMeta["displayName"] = request.DisplayName;
            
            if (request.Email != null)
                updatedMeta["email"] = request.Email;
            
            if (request.Bio != null)
                updatedMeta["bio"] = request.Bio;
            
            if (request.Location != null)
                updatedMeta["location"] = request.Location;
            
            if (request.AvatarUrl != null)
                updatedMeta["avatarUrl"] = request.AvatarUrl;
            
            if (request.CoverImageUrl != null)
                updatedMeta["coverImageUrl"] = request.CoverImageUrl;
            
            // Handle interests - convert array to List for storage
            if (request.Interests != null && request.Interests.Length > 0)
            {
                updatedMeta["interests"] = request.Interests.ToList();
            }
            
            updatedMeta["lastModified"] = DateTimeOffset.UtcNow;

            // Create updated node
            var updatedNode = userNode with { Meta = updatedMeta };
            
            // Save to registry
            Registry.Upsert(updatedNode);
            
            _logger.Info($"Updated auth profile for user {userId}");
            
            return Results.Json(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Update profile error: {ex.Message}", ex);
            return Results.Json(new { success = false, message = $"Failed to update profile: {ex.Message}" }, statusCode: 500);
        }
    }

    [ApiRoute("POST", "/auth/change-password", "ChangePassword", "Change user password", "codex.identity")]
    public async Task<IResult> ChangePasswordAsync([ApiParameter("request", "Change password request")] AuthChangePasswordRequest request)
    {
        try
        {
            var userNode = Registry.GetNode(request.UserId);
            if (userNode == null)
            {
                return Results.NotFound(new { success = false, message = "User not found" });
            }

            // Verify current password
            var storedHash = userNode.Meta["passwordHash"]?.ToString();
            if (storedHash == null || !VerifyPasswordSecure(request.CurrentPassword, storedHash))
            {
                return Results.Json(new { success = false, message = "Current password is incorrect" }, statusCode: 401);
            }

            // Validate new password
            var passwordValidation = ValidatePassword(request.NewPassword);
            if (!passwordValidation.IsValid)
            {
                return Results.BadRequest(new { success = false, message = passwordValidation.ErrorMessage });
            }

            // Update password
            var newPasswordHash = HashPasswordSecure(request.NewPassword);
            var updatedMeta = new Dictionary<string, object>(userNode.Meta)
            {
                ["passwordHash"] = newPasswordHash,
                ["passwordChangedAt"] = DateTime.UtcNow,
                ["updatedAt"] = DateTime.UtcNow
            };

            var updatedNode = userNode with { Meta = updatedMeta };
            Registry.Upsert(updatedNode);

            // Revoke all existing sessions for security
            await RevokeAllUserSessionsAsync(request.UserId);

            _logger.Info($"Password changed for user: {userNode.Meta["username"]}");
            return Results.Json(new { success = true, message = "Password changed successfully. Please login again." });
        }
        catch (Exception ex)
        {
            _logger.Error($"Change password error: {ex.Message}", ex);
            return Results.Json(new { success = false, message = "Failed to change password" }, statusCode: 500);
        }
    }

    // ===============================
    // UNIFIED AUTH HELPER METHODS
    // ===============================

    private ValidationResult ValidateRegistrationRequest(AuthRegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username))
            return new ValidationResult(false, "Username is required");

        if (request.Username.Length < 3 || request.Username.Length > 50)
            return new ValidationResult(false, "Username must be between 3 and 50 characters");

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_-]+$"))
            return new ValidationResult(false, "Username can only contain letters, numbers, underscores, and hyphens");

        if (string.IsNullOrEmpty(request.Email))
            return new ValidationResult(false, "Email is required");

        if (!IsValidEmail(request.Email))
            return new ValidationResult(false, "Invalid email format");

        var passwordValidation = ValidatePassword(request.Password);
        if (!passwordValidation.IsValid)
            return passwordValidation;

        return new ValidationResult(true, null);
    }

    private ValidationResult ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return new ValidationResult(false, "Password is required");

        if (password.Length < 8)
            return new ValidationResult(false, "Password must be at least 8 characters long");

        if (!password.Any(char.IsUpper))
            return new ValidationResult(false, "Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            return new ValidationResult(false, "Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            return new ValidationResult(false, "Password must contain at least one number");

        return new ValidationResult(true, null);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckEmailExistsAsync(string email)
    {
        var emailIndexEdges = Registry.AllEdges()
            .Where(e => e.Role == "email-index" && 
                       e.Meta.ContainsKey("email") && 
                       e.Meta["email"].ToString()?.Equals(email, StringComparison.OrdinalIgnoreCase) == true);
        
        return emailIndexEdges.Any();
    }

    private async Task<Node?> FindUserByUsernameOrEmailAsync(string usernameOrEmail)
    {
        // Try username first
        var userByUsername = Registry.GetNode($"user.{usernameOrEmail}");
        if (userByUsername != null)
            return userByUsername;

        // Try email lookup
        var emailEdge = Registry.AllEdges()
            .FirstOrDefault(e => e.Role == "email-index" && 
                                e.Meta.ContainsKey("email") && 
                                e.Meta["email"].ToString()?.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) == true);

        if (emailEdge != null)
        {
            return Registry.GetNode(emailEdge.ToId);
        }

        return null;
    }

    private Node CreateUserNode(string userId, string username, string email, string? displayName, string passwordHash)
    {
        return new Node(
            Id: userId,
            TypeId: "codex.user",
            State: ContentState.Ice,
            Locale: "en",
            Title: displayName ?? username,
            Description: $"User account for {username}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    username = username,
                    email = email,
                    displayName = displayName ?? username,
                    createdAt = DateTime.UtcNow,
                    isActive = true
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["username"] = username,
                ["email"] = email,
                ["displayName"] = displayName ?? username,
                ["passwordHash"] = passwordHash,
                ["createdAt"] = DateTime.UtcNow,
                ["updatedAt"] = DateTime.UtcNow,
                ["lastLoginAt"] = DateTime.UtcNow,
                ["status"] = "active",
                ["isActive"] = true,
                ["loginAttempts"] = 0,
                ["lastFailedLoginAt"] = (DateTime?)null
            }
        );
    }

    private void CreateEmailIndex(string email, string userId, string username)
    {
        var emailIndexEdge = NodeHelpers.CreateEdge(
            fromId: "email-index",
            toId: userId,
            role: "email-index",
            weight: 1.0,
            meta: new Dictionary<string, object>
            {
                ["email"] = email.ToLowerInvariant(),
                ["username"] = username,
                ["createdAt"] = DateTime.UtcNow
            },
            roleId: NodeHelpers.TryResolveRoleId(Registry, "email-index")
        );
        Registry.Upsert(emailIndexEdge);
    }

    private AuthUserProfile CreateAuthUserProfile(Node userNode)
    {
        // Parse interests from meta - could be List<string> or string
        List<string>? interests = null;
        if (userNode.Meta.ContainsKey("interests"))
        {
            var interestsValue = userNode.Meta["interests"];
            if (interestsValue is List<string> interestsList)
            {
                interests = interestsList;
            }
            else if (interestsValue is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                interests = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonElement.GetRawText());
            }
            else if (interestsValue is string interestsStr && !string.IsNullOrEmpty(interestsStr))
            {
                interests = interestsStr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
        }

        return new AuthUserProfile(
            Id: userNode.Id,
            Username: userNode.Meta["username"]?.ToString() ?? "",
            Email: userNode.Meta["email"]?.ToString() ?? "",
            DisplayName: userNode.Meta["displayName"]?.ToString() ?? "",
            CreatedAt: userNode.Meta.ContainsKey("createdAt") ? (DateTime)userNode.Meta["createdAt"] : DateTime.MinValue,
            LastLoginAt: userNode.Meta.ContainsKey("lastLoginAt") ? (DateTime?)userNode.Meta["lastLoginAt"] : null,
            IsActive: userNode.Meta.ContainsKey("isActive") ? (bool)userNode.Meta["isActive"] : false,
            Status: userNode.Meta["status"]?.ToString() ?? "unknown",
            Bio: userNode.Meta.ContainsKey("bio") ? userNode.Meta["bio"]?.ToString() : null,
            Location: userNode.Meta.ContainsKey("location") ? userNode.Meta["location"]?.ToString() : null,
            AvatarUrl: userNode.Meta.ContainsKey("avatarUrl") ? userNode.Meta["avatarUrl"]?.ToString() : null,
            CoverImageUrl: userNode.Meta.ContainsKey("coverImageUrl") ? userNode.Meta["coverImageUrl"]?.ToString() : null,
            Interests: interests
        );
    }

    private bool IsUserActive(Node userNode)
    {
        return userNode.Meta.ContainsKey("isActive") && (bool)userNode.Meta["isActive"] &&
               userNode.Meta.ContainsKey("status") && userNode.Meta["status"]?.ToString() == "active";
    }

    /// <summary>
    /// Hash password using BCrypt with automatic salt generation
    /// Work factor of 12 provides good security/performance balance
    /// </summary>
    private string HashPasswordSecure(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify password against BCrypt hash
    /// BCrypt automatically handles salt extraction
    /// </summary>
    private bool VerifyPasswordSecure(string password, string storedHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            // Invalid hash format
            return false;
        }
    }

    private string GenerateJwtTokenSecure(string userId, string username, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", userId),
                new Claim("username", username),
                new Claim("email", email),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            }),
            Expires = DateTime.UtcNow.AddHours(_tokenExpirationHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private ClaimsPrincipal? ValidateJwtTokenSecure(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
        }
        catch
        {
            return null;
        }
    }

    private UserSession CreateUserSessionRecord(string userId, string token, bool isPersistent)
    {
        return new UserSession(
            Token: token,
            UserId: userId,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddHours(isPersistent ? _tokenExpirationHours * 7 : _tokenExpirationHours), // 7x longer for persistent
            LastActivity: DateTime.UtcNow,
            IsPersistent: isPersistent
        );
    }

    private async Task UpdateLastLoginAsync(Node userNode)
    {
        var updatedMeta = new Dictionary<string, object>(userNode.Meta)
        {
            ["lastLoginAt"] = DateTime.UtcNow,
            ["loginAttempts"] = 0
        };
        var updatedNode = userNode with { Meta = updatedMeta };
        Registry.Upsert(updatedNode);
    }

    private async Task UpdateLastActivityAsync(string userId)
    {
        var userNode = Registry.GetNode(userId);
        if (userNode != null)
        {
            var updatedMeta = new Dictionary<string, object>(userNode.Meta)
            {
                ["lastActivityAt"] = DateTime.UtcNow
            };
            var updatedNode = userNode with { Meta = updatedMeta };
            Registry.Upsert(updatedNode);
        }
    }

    private async Task RecordFailedLoginAttempt(string userId)
    {
        var userNode = Registry.GetNode(userId);
        if (userNode != null)
        {
            var currentAttempts = userNode.Meta.ContainsKey("loginAttempts") ? (int)userNode.Meta["loginAttempts"] : 0;
            var updatedMeta = new Dictionary<string, object>(userNode.Meta)
            {
                ["loginAttempts"] = currentAttempts + 1,
                ["lastFailedLoginAt"] = DateTime.UtcNow
            };

            // Lock account after 5 failed attempts
            if (currentAttempts + 1 >= 5)
            {
                updatedMeta["isActive"] = false;
                updatedMeta["status"] = "locked";
                updatedMeta["lockedAt"] = DateTime.UtcNow;
            }

            var updatedNode = userNode with { Meta = updatedMeta };
            Registry.Upsert(updatedNode);
        }
    }

    private async Task RevokeAllUserSessionsAsync(string userId)
    {
        var userSessions = _activeSessions.Where(kvp => kvp.Value.UserId == userId).ToList();
        foreach (var session in userSessions)
        {
            _revokedTokens.TryAdd(session.Key, DateTime.UtcNow);
            _activeSessions.TryRemove(session.Key, out _);
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
        // Generate a proper JWT token with claims
        var tokenHandler = new JwtSecurityTokenHandler();
        
        // Use a secure key - in production this should be from configuration
        var key = Encoding.UTF8.GetBytes("your-256-bit-secret-key-here-must-be-at-least-32-chars-long!");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim("username", username),
                new Claim("email", email),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("jti", Guid.NewGuid().ToString()) // JWT ID for uniqueness
            }),
            Expires = DateTime.UtcNow.AddHours(24), // Token expires in 24 hours
            Issuer = "CodexBootstrap",
            Audience = "CodexBootstrap-Users",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateSessionToken(string userId)
    {
        return $"session-{userId}-{DateTime.UtcNow.Ticks}";
    }
    
    /// <summary>
    /// Cleanup expired sessions and revoked tokens to prevent memory leaks
    /// Runs every 15 minutes via timer
    /// </summary>
    private void CleanupExpiredData(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var cleaned = 0;
            
            // Cleanup expired sessions
            var expiredSessions = _activeSessions
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var sessionId in expiredSessions)
            {
                if (_activeSessions.TryRemove(sessionId, out _))
                {
                    cleaned++;
                }
            }
            
            // Cleanup old revoked tokens (older than 48 hours)
            var tokenCutoff = now.AddHours(-REVOKED_TOKEN_TTL_HOURS);
            var expiredTokens = _revokedTokens
                .Where(kvp => kvp.Value < tokenCutoff)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var token in expiredTokens)
            {
                if (_revokedTokens.TryRemove(token, out _))
                {
                    cleaned++;
                }
            }
            
            // Size-based eviction if collections too large (safety limit)
            if (_activeSessions.Count > MAX_ACTIVE_SESSIONS)
            {
                var toRemove = _activeSessions
                    .OrderBy(kvp => kvp.Value.LastActivity)
                    .Take(_activeSessions.Count - MAX_ACTIVE_SESSIONS)
                    .Select(kvp => kvp.Key)
                    .ToList();
                    
                foreach (var key in toRemove)
                {
                    if (_activeSessions.TryRemove(key, out _))
                    {
                        cleaned++;
                    }
                }
                
                _logger.Warn($"Session limit exceeded ({MAX_ACTIVE_SESSIONS}), evicted {toRemove.Count} oldest sessions");
            }
            
            if (_revokedTokens.Count > MAX_REVOKED_TOKENS)
            {
                var toRemove = _revokedTokens
                    .OrderBy(kvp => kvp.Value)
                    .Take(_revokedTokens.Count - MAX_REVOKED_TOKENS)
                    .Select(kvp => kvp.Key)
                    .ToList();
                    
                foreach (var key in toRemove)
                {
                    if (_revokedTokens.TryRemove(key, out _))
                    {
                        cleaned++;
                    }
                }
                
                _logger.Warn($"Revoked token limit exceeded ({MAX_REVOKED_TOKENS}), evicted {toRemove.Count} oldest tokens");
            }
            
            // Log cleanup statistics
            if (cleaned > 0 || _activeSessions.Count > 100 || _revokedTokens.Count > 100)
            {
                _logger.Info($"[MemoryCleanup] Removed {cleaned} expired entries. " +
                            $"Active sessions: {_activeSessions.Count}, " +
                            $"Revoked tokens: {_revokedTokens.Count}, " +
                            $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Cleanup error: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Dispose cleanup timer
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

public sealed record IdentityValidationRequest(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("userId")] string? UserId
);

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

// ===============================
// UNIFIED AUTH REQUEST/RESPONSE MODELS
// ===============================

[MetaNode(Id = "codex.auth.register-request", Name = "Auth Register Request", Description = "User registration request")]
public record AuthRegisterRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("displayName")] string? DisplayName = null
);

[MetaNode(Id = "codex.auth.login-request", Name = "Auth Login Request", Description = "User login request")]
public record AuthLoginRequest(
    [property: JsonPropertyName("usernameOrEmail")] string UsernameOrEmail,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("rememberMe")] bool RememberMe = false
);

[MetaNode(Id = "codex.auth.logout-request", Name = "Auth Logout Request", Description = "User logout request")]
public record AuthLogoutRequest(
    [property: JsonPropertyName("token")] string Token
);

[MetaNode(Id = "codex.auth.token-validation-request", Name = "Auth Token Validation Request", Description = "JWT token validation request")]
public record AuthTokenValidationRequest(
    [property: JsonPropertyName("token")] string Token
);

[MetaNode(Id = "codex.auth.change-password-request", Name = "Auth Change Password Request", Description = "User password change request")]
public record AuthChangePasswordRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("currentPassword")] string CurrentPassword,
    [property: JsonPropertyName("newPassword")] string NewPassword
);

[MetaNode(Id = "codex.auth.profile-update-request", Name = "Auth Profile Update Request", Description = "User profile update request")]
public record AuthProfileUpdateRequest(
    [property: JsonPropertyName("displayName")] string? DisplayName = null,
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("bio")] string? Bio = null,
    [property: JsonPropertyName("location")] string? Location = null,
    [property: JsonPropertyName("interests")] string[]? Interests = null,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl = null,
    [property: JsonPropertyName("coverImageUrl")] string? CoverImageUrl = null
);

[MetaNode(Id = "codex.auth.response", Name = "Auth Response", Description = "Generic authentication response")]
public record AuthResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("user")] AuthUserProfile? User,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp = default
)
{
    public AuthResponse(bool success, string? token, AuthUserProfile? user, string message) 
        : this(success, token, user, message, DateTime.UtcNow) { }
}

[MetaNode(Id = "codex.auth.token-validation-response", Name = "Auth Token Validation Response", Description = "JWT token validation response")]
public record AuthTokenValidationResponse(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("user")] AuthUserProfile? User,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp = default
)
{
    public AuthTokenValidationResponse(bool isValid, AuthUserProfile? user, string message) 
        : this(isValid, user, message, DateTime.UtcNow) { }
}

[MetaNode(Id = "codex.auth.user-profile", Name = "Auth User Profile", Description = "Authentication user profile information")]
public record AuthUserProfile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("lastLoginAt")] DateTime? LastLoginAt,
    [property: JsonPropertyName("isActive")] bool IsActive,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("bio")] string? Bio = null,
    [property: JsonPropertyName("location")] string? Location = null,
    [property: JsonPropertyName("interests")] List<string>? Interests = null,
    [property: JsonPropertyName("avatarUrl")] string? AvatarUrl = null,
    [property: JsonPropertyName("coverImageUrl")] string? CoverImageUrl = null
);

[MetaNode(Id = "codex.auth.user-session", Name = "User Session", Description = "User session information")]
public record UserSession(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt,
    [property: JsonPropertyName("lastActivity")] DateTime LastActivity,
    [property: JsonPropertyName("isPersistent")] bool IsPersistent
);

[MetaNode(Id = "codex.auth.validation-result", Name = "Validation Result", Description = "Validation result")]
public record ValidationResult(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage
);
