using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Security Module - JWT authentication, data encryption, and access control
/// Implements comprehensive security features for the Living Codex system
/// </summary>
public class SecurityModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, UserSession> _activeSessions = new();
    private readonly Dictionary<string, SecurityRole> _roles = new();
    private readonly Dictionary<string, SecurityPolicy> _policies = new();
    private readonly List<SecurityAuditLog> _auditLogs = new();
    private readonly string _jwtSecret;
    private readonly string _encryptionKey;
    private CoreApiService? _coreApiService;
    private readonly object _securityLock = new();

    public SecurityModule(NodeRegistry registry)
    {
        _registry = registry;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-jwt-key-that-should-be-32-characters-long";
        _encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? "your-32-character-encryption-key-here";
        InitializeSecurityRoles();
        InitializeSecurityPolicies();
    }

    public SecurityModule() : this(new NodeRegistry()) { }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.security",
            name: "Security Module",
            version: "1.0.0",
            description: "JWT authentication, data encryption, and comprehensive access control for Living Codex",
            capabilities: new[] { "jwt-authentication", "data-encryption", "role-based-access", "audit-logging", "security-policies" },
            tags: new[] { "security", "authentication", "encryption", "access-control" }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _coreApiService = coreApi;
        
        // Register all Security related nodes for AI agent discovery
        RegisterSecurityNodes(registry);
    }

    /// <summary>
    /// Register all Security related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterSecurityNodes(NodeRegistry registry)
    {
        // Register Security module node
        var securityNode = new Node(
            Id: "codex.security",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Security Module",
            Description: "JWT authentication, data encryption, and comprehensive access control for Living Codex",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "jwt-authentication", "data-encryption", "role-based-access", "audit-logging", "security-policies" },
                    endpoints = new[] { "authenticate", "authorize", "encrypt", "decrypt", "audit-log", "manage-roles" },
                    integration = "security"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Security Module",
                ["version"] = "1.0.0",
                ["type"] = "security",
                ["parentModule"] = "codex.security",
                ["capabilities"] = new[] { "jwt-authentication", "data-encryption", "role-based-access", "audit-logging" }
            }
        );
        registry.Upsert(securityNode);

        // Register Security routes as nodes
        RegisterSecurityRoutes(registry);
        
        // Register Security DTOs as nodes
        RegisterSecurityDTOs(registry);
    }

    /// <summary>
    /// Register Security routes as discoverable nodes
    /// </summary>
    private void RegisterSecurityRoutes(NodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/security/authenticate", method = "POST", name = "security-authenticate", description = "Authenticate user and generate JWT token" },
            new { path = "/security/authorize", method = "POST", name = "security-authorize", description = "Authorize user for specific action or resource" },
            new { path = "/security/encrypt", method = "POST", name = "security-encrypt", description = "Encrypt sensitive data" },
            new { path = "/security/decrypt", method = "POST", name = "security-decrypt", description = "Decrypt encrypted data" },
            new { path = "/security/audit-log", method = "GET", name = "security-audit-log", description = "Get security audit logs" },
            new { path = "/security/roles", method = "GET", name = "security-roles", description = "Get available security roles" },
            new { path = "/security/policies", method = "GET", name = "security-policies", description = "Get security policies" },
            new { path = "/security/validate-token", method = "POST", name = "security-validate-token", description = "Validate JWT token" },
            new { path = "/security/refresh-token", method = "POST", name = "security-refresh-token", description = "Refresh JWT token" },
            new { path = "/security/logout", method = "POST", name = "security-logout", description = "Logout user and invalidate session" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"security.route.{route.name}",
                TypeId: "meta.route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"Security route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetSecurityRouteParameters(route.name),
                        responseType = GetSecurityRouteResponseType(route.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = route.name,
                    ["path"] = route.path,
                    ["method"] = route.method,
                    ["description"] = route.description,
                    ["module"] = "codex.security",
                    ["parentModule"] = "codex.security"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register Security DTOs as discoverable nodes
    /// </summary>
    private void RegisterSecurityDTOs(NodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "AuthenticationRequest", description = "Request to authenticate user", properties = new[] { "Username", "Password", "RememberMe" } },
            new { name = "AuthenticationResponse", description = "Response from authentication", properties = new[] { "Success", "Token", "RefreshToken", "ExpiresAt", "User" } },
            new { name = "AuthorizationRequest", description = "Request to authorize user action", properties = new[] { "Token", "Action", "Resource", "Context" } },
            new { name = "AuthorizationResponse", description = "Response from authorization", properties = new[] { "Success", "Authorized", "Reason", "RequiredRole" } },
            new { name = "EncryptionRequest", description = "Request to encrypt data", properties = new[] { "Data", "Algorithm", "KeyId" } },
            new { name = "EncryptionResponse", description = "Response from encryption", properties = new[] { "Success", "EncryptedData", "KeyId", "Algorithm" } },
            new { name = "DecryptionRequest", description = "Request to decrypt data", properties = new[] { "EncryptedData", "KeyId", "Algorithm" } },
            new { name = "DecryptionResponse", description = "Response from decryption", properties = new[] { "Success", "DecryptedData", "KeyId" } },
            new { name = "AuditLogResponse", description = "Response with audit logs", properties = new[] { "Success", "Logs", "TotalCount", "Page", "PageSize" } },
            new { name = "TokenValidationRequest", description = "Request to validate JWT token", properties = new[] { "Token" } },
            new { name = "TokenValidationResponse", description = "Response from token validation", properties = new[] { "Success", "Valid", "Claims", "ExpiresAt" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"security.dto.{dto.name}",
                TypeId: "meta.type",
                State: ContentState.Ice,
                Locale: "en",
                Title: dto.name,
                Description: dto.description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = dto.name,
                        description = dto.description,
                        properties = dto.properties,
                        type = "record",
                        module = "codex.security",
                        usage = GetSecurityDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.security",
                    ["parentModule"] = "codex.security",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    // Helper methods for AI agent generation
    private object GetSecurityRouteParameters(string routeName)
    {
        return routeName switch
        {
            "security-authenticate" => new
            {
                request = new { type = "AuthenticationRequest", required = true, location = "body", description = "Authentication credentials" }
            },
            "security-authorize" => new
            {
                request = new { type = "AuthorizationRequest", required = true, location = "body", description = "Authorization request details" }
            },
            "security-encrypt" => new
            {
                request = new { type = "EncryptionRequest", required = true, location = "body", description = "Data to encrypt" }
            },
            "security-decrypt" => new
            {
                request = new { type = "DecryptionRequest", required = true, location = "body", description = "Data to decrypt" }
            },
            "security-audit-log" => new
            {
                page = new { type = "int", required = false, location = "query", description = "Page number" },
                pageSize = new { type = "int", required = false, location = "query", description = "Page size" },
                action = new { type = "string", required = false, location = "query", description = "Filter by action" }
            },
            "security-roles" => new { },
            "security-policies" => new { },
            "security-validate-token" => new
            {
                request = new { type = "TokenValidationRequest", required = true, location = "body", description = "Token to validate" }
            },
            "security-refresh-token" => new
            {
                request = new { type = "TokenValidationRequest", required = true, location = "body", description = "Refresh token request" }
            },
            "security-logout" => new
            {
                token = new { type = "string", required = true, location = "body", description = "JWT token to invalidate" }
            },
            _ => new { }
        };
    }

    private string GetSecurityRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "security-authenticate" => "AuthenticationResponse",
            "security-authorize" => "AuthorizationResponse",
            "security-encrypt" => "EncryptionResponse",
            "security-decrypt" => "DecryptionResponse",
            "security-audit-log" => "AuditLogResponse",
            "security-roles" => "SecurityRole[]",
            "security-policies" => "SecurityPolicy[]",
            "security-validate-token" => "TokenValidationResponse",
            "security-refresh-token" => "AuthenticationResponse",
            "security-logout" => "LogoutResponse",
            _ => "object"
        };
    }

    private string GetSecurityDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "AuthenticationRequest" => "Used to request user authentication with username and password credentials.",
            "AuthenticationResponse" => "Returned when authentication is successful. Contains JWT token and user information.",
            "AuthorizationRequest" => "Used to request authorization for specific actions or resources.",
            "AuthorizationResponse" => "Returned when authorization is checked. Contains authorization result and required role.",
            "EncryptionRequest" => "Used to request encryption of sensitive data using specified algorithm and key.",
            "EncryptionResponse" => "Returned when encryption is completed. Contains encrypted data and encryption details.",
            "DecryptionRequest" => "Used to request decryption of encrypted data using specified key and algorithm.",
            "DecryptionResponse" => "Returned when decryption is completed. Contains decrypted data and key information.",
            "AuditLogResponse" => "Returned when requesting security audit logs. Contains paginated log entries.",
            "TokenValidationRequest" => "Used to request validation of JWT token for authentication verification.",
            "TokenValidationResponse" => "Returned when token validation is completed. Contains validation result and claims.",
            _ => "Security data transfer object"
        };
    }

    // Security API Methods
    [ApiRoute("POST", "/security/authenticate", "security-authenticate", "Authenticate user and generate JWT token", "codex.security")]
    public async Task<object> Authenticate([ApiParameter("request", "Authentication request", Required = true, Location = "body")] AuthenticationRequest request)
    {
        try
        {
            // Simulate user authentication (in real implementation, validate against user store)
            var user = await ValidateUserCredentials(request.Username, request.Password);
            
            if (user == null)
            {
                LogSecurityEvent("authentication_failed", $"Failed login attempt for user: {request.Username}");
                return new AuthenticationResponse(
                    Success: false,
                    Token: null,
                    RefreshToken: null,
                    ExpiresAt: null,
                    User: null
                );
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Store session
            lock (_securityLock)
            {
                _activeSessions[user.Id] = new UserSession
                {
                    UserId = user.Id,
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTime.UtcNow
                };
            }

            LogSecurityEvent("authentication_success", $"User {user.Username} authenticated successfully");

            return new AuthenticationResponse(
                Success: true,
                Token: token,
                RefreshToken: refreshToken,
                ExpiresAt: expiresAt,
                User: user
            );
        }
        catch (Exception ex)
        {
            LogSecurityEvent("authentication_error", $"Authentication error: {ex.Message}");
            return new ErrorResponse($"Authentication failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/authorize", "security-authorize", "Authorize user for specific action or resource", "codex.security")]
    public async Task<object> Authorize([ApiParameter("request", "Authorization request", Required = true, Location = "body")] AuthorizationRequest request)
    {
        try
        {
            var claims = ValidateJwtToken(request.Token);
            if (claims == null)
            {
                return new AuthorizationResponse(
                    Success: false,
                    Authorized: false,
                    Reason: "Invalid or expired token",
                    RequiredRole: null
                );
            }

            var userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = claims.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return new AuthorizationResponse(
                    Success: false,
                    Authorized: false,
                    Reason: "Invalid user claims",
                    RequiredRole: null
                );
            }

            var isAuthorized = await CheckAuthorization(userId, userRole, request.Action, request.Resource, request.Context);

            LogSecurityEvent("authorization_check", $"Authorization check for user {userId}, action: {request.Action}, authorized: {isAuthorized}");

            return new AuthorizationResponse(
                Success: true,
                Authorized: isAuthorized,
                Reason: isAuthorized ? "Authorized" : "Insufficient permissions",
                RequiredRole: isAuthorized ? null : GetRequiredRole(request.Action, request.Resource)
            );
        }
        catch (Exception ex)
        {
            LogSecurityEvent("authorization_error", $"Authorization error: {ex.Message}");
            return new ErrorResponse($"Authorization failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/encrypt", "security-encrypt", "Encrypt sensitive data", "codex.security")]
    public async Task<object> EncryptData([ApiParameter("request", "Encryption request", Required = true, Location = "body")] EncryptionRequest request)
    {
        try
        {
            var encryptedData = await EncryptString(request.Data, request.Algorithm, request.KeyId);
            
            LogSecurityEvent("data_encrypted", $"Data encrypted using {request.Algorithm}");

            return new EncryptionResponse(
                Success: true,
                EncryptedData: encryptedData,
                KeyId: request.KeyId,
                Algorithm: request.Algorithm
            );
        }
        catch (Exception ex)
        {
            LogSecurityEvent("encryption_error", $"Encryption error: {ex.Message}");
            return new ErrorResponse($"Encryption failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/decrypt", "security-decrypt", "Decrypt encrypted data", "codex.security")]
    public async Task<object> DecryptData([ApiParameter("request", "Decryption request", Required = true, Location = "body")] DecryptionRequest request)
    {
        try
        {
            var decryptedData = await DecryptString(request.EncryptedData, request.KeyId, request.Algorithm);
            
            LogSecurityEvent("data_decrypted", $"Data decrypted using {request.Algorithm}");

            return new DecryptionResponse(
                Success: true,
                DecryptedData: decryptedData,
                KeyId: request.KeyId
            );
        }
        catch (Exception ex)
        {
            LogSecurityEvent("decryption_error", $"Decryption error: {ex.Message}");
            return new ErrorResponse($"Decryption failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/security/audit-log", "security-audit-log", "Get security audit logs", "codex.security")]
    public async Task<object> GetAuditLogs([ApiParameter("page", "Page number", Required = false, Location = "query")] int? page = null, [ApiParameter("pageSize", "Page size", Required = false, Location = "query")] int? pageSize = null, [ApiParameter("action", "Filter by action", Required = false, Location = "query")] string? action = null)
    {
        try
        {
            var logs = await GetFilteredAuditLogs(page ?? 1, pageSize ?? 50, action);

            return new AuditLogResponse(
                Success: true,
                Logs: logs,
                TotalCount: _auditLogs.Count,
                Page: page ?? 1,
                PageSize: pageSize ?? 50
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get audit logs: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/security/roles", "security-roles", "Get available security roles", "codex.security")]
    public async Task<object> GetSecurityRoles()
    {
        try
        {
            return _roles.Values.ToList();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get security roles: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/security/policies", "security-policies", "Get security policies", "codex.security")]
    public async Task<object> GetSecurityPolicies()
    {
        try
        {
            return _policies.Values.ToList();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get security policies: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/validate-token", "security-validate-token", "Validate JWT token", "codex.security")]
    public async Task<object> ValidateToken([ApiParameter("request", "Token validation request", Required = true, Location = "body")] TokenValidationRequest request)
    {
        try
        {
            var claims = ValidateJwtToken(request.Token);
            var isValid = claims != null;
            var expiresAt = claims?.FindFirst("exp")?.Value != null 
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(claims.FindFirst("exp").Value)).DateTime 
                : (DateTime?)null;

            var claimsDict = claims?.Claims?.ToDictionary(c => c.Type, c => c.Value) ?? new Dictionary<string, string>();

            return new TokenValidationResponse(
                Success: true,
                Valid: isValid,
                Claims: claimsDict,
                ExpiresAt: expiresAt
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Token validation failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/refresh-token", "security-refresh-token", "Refresh JWT token", "codex.security")]
    public async Task<object> RefreshToken([ApiParameter("request", "Refresh token request", Required = true, Location = "body")] TokenValidationRequest request)
    {
        try
        {
            var claims = ValidateJwtToken(request.Token);
            if (claims == null)
            {
                return new ErrorResponse("Invalid refresh token");
            }

            var userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("Invalid user in refresh token");
            }

            // Generate new token
            var user = new SecurityUser { Id = userId, Username = claims.FindFirst(ClaimTypes.Name)?.Value ?? "unknown" };
            var newToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddHours(24);

            return new AuthenticationResponse(
                Success: true,
                Token: newToken,
                RefreshToken: refreshToken,
                ExpiresAt: expiresAt,
                User: user
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Token refresh failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/security/logout", "security-logout", "Logout user and invalidate session", "codex.security")]
    public async Task<object> Logout([ApiParameter("token", "JWT token to invalidate", Required = true, Location = "body")] string token)
    {
        try
        {
            var claims = ValidateJwtToken(token);
            if (claims != null)
            {
                var userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    lock (_securityLock)
                    {
                        _activeSessions.Remove(userId);
                    }
                    LogSecurityEvent("user_logout", $"User {userId} logged out");
                }
            }

            return new LogoutResponse(Success: true, Message: "Logged out successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Logout failed: {ex.Message}");
        }
    }

    // Helper methods for security operations
    private async Task<SecurityUser?> ValidateUserCredentials(string username, string password)
    {
        // Simulate user validation (in real implementation, validate against user store)
        await Task.Delay(10);
        
        if (username == "admin" && password == "admin123")
        {
            return new SecurityUser
            {
                Id = "user-1",
                Username = username,
                Email = "admin@example.com",
                Role = "admin",
                IsActive = true
            };
        }
        
        if (username == "user" && password == "user123")
        {
            return new SecurityUser
            {
                Id = "user-2",
                Username = username,
                Email = "user@example.com",
                Role = "user",
                IsActive = true
            };
        }

        return null;
    }

    private string GenerateJwtToken(SecurityUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private ClaimsPrincipal? ValidateJwtToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> CheckAuthorization(string userId, string userRole, string action, string resource, Dictionary<string, object>? context)
    {
        // Simulate authorization check
        await Task.Delay(5);

        var role = _roles.GetValueOrDefault(userRole);
        if (role == null) return false;

        return role.Permissions.Any(p => p.Action == action && p.Resource == resource);
    }

    private string? GetRequiredRole(string action, string resource)
    {
        // Simulate role requirement lookup
        return action switch
        {
            "read" => "user",
            "write" => "editor",
            "admin" => "admin",
            _ => "user"
        };
    }

    private async Task<string> EncryptString(string plainText, string algorithm, string keyId)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16]; // In production, use a proper IV

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        
        await swEncrypt.WriteAsync(plainText);
        await swEncrypt.FlushAsync();
        csEncrypt.FlushFinalBlock();
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    private async Task<string> DecryptString(string cipherText, string keyId, string algorithm)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
        aes.IV = new byte[16]; // In production, use a proper IV

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return await srDecrypt.ReadToEndAsync();
    }

    private async Task<List<SecurityAuditLog>> GetFilteredAuditLogs(int page, int pageSize, string? action)
    {
        await Task.Delay(5);

        var logs = _auditLogs.AsEnumerable();

        if (!string.IsNullOrEmpty(action))
        {
            logs = logs.Where(l => l.Action == action);
        }

        return logs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private void LogSecurityEvent(string action, string description)
    {
        var log = new SecurityAuditLog
        {
            Id = Guid.NewGuid().ToString(),
            Action = action,
            Description = description,
            UserId = "system",
            Timestamp = DateTime.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "Living-Codex-Security"
        };

        lock (_securityLock)
        {
            _auditLogs.Add(log);
        }
    }

    private void InitializeSecurityRoles()
    {
        _roles["admin"] = new SecurityRole
        {
            Name = "admin",
            Description = "Administrator with full system access",
            Permissions = new List<SecurityPermission>
            {
                new() { Action = "read", Resource = "*" },
                new() { Action = "write", Resource = "*" },
                new() { Action = "admin", Resource = "*" }
            }
        };

        _roles["editor"] = new SecurityRole
        {
            Name = "editor",
            Description = "Editor with read and write access",
            Permissions = new List<SecurityPermission>
            {
                new() { Action = "read", Resource = "*" },
                new() { Action = "write", Resource = "concepts" },
                new() { Action = "write", Resource = "translations" }
            }
        };

        _roles["user"] = new SecurityRole
        {
            Name = "user",
            Description = "Regular user with read access",
            Permissions = new List<SecurityPermission>
            {
                new() { Action = "read", Resource = "concepts" },
                new() { Action = "read", Resource = "translations" }
            }
        };
    }

    private void InitializeSecurityPolicies()
    {
        _policies["password_policy"] = new SecurityPolicy
        {
            Name = "password_policy",
            Description = "Password complexity requirements",
            Rules = new Dictionary<string, object>
            {
                ["min_length"] = 8,
                ["require_uppercase"] = true,
                ["require_lowercase"] = true,
                ["require_numbers"] = true,
                ["require_special_chars"] = true
            }
        };

        _policies["session_policy"] = new SecurityPolicy
        {
            Name = "session_policy",
            Description = "Session management rules",
            Rules = new Dictionary<string, object>
            {
                ["max_session_duration"] = 24, // hours
                ["idle_timeout"] = 2, // hours
                ["max_concurrent_sessions"] = 3
            }
        };
    }
}

// Security DTOs
[ResponseType("codex.security.auth-request", "AuthenticationRequest", "Request for user authentication")]
public record AuthenticationRequest(
    string Username,
    string Password,
    bool RememberMe
);

[ResponseType("codex.security.auth-response", "AuthenticationResponse", "Response for user authentication")]
public record AuthenticationResponse(
    bool Success,
    string? Token,
    string? RefreshToken,
    DateTime? ExpiresAt,
    SecurityUser? User
);

[ResponseType("codex.security.authorization-request", "AuthorizationRequest", "Request for authorization check")]
public record AuthorizationRequest(
    string Token,
    string Action,
    string Resource,
    Dictionary<string, object>? Context
);

[ResponseType("codex.security.authorization-response", "AuthorizationResponse", "Response for authorization check")]
public record AuthorizationResponse(
    bool Success,
    bool Authorized,
    string Reason,
    string? RequiredRole
);

[ResponseType("codex.security.encryption-request", "EncryptionRequest", "Request for data encryption")]
public record EncryptionRequest(
    string Data,
    string Algorithm,
    string KeyId
);

[ResponseType("codex.security.encryption-response", "EncryptionResponse", "Response for data encryption")]
public record EncryptionResponse(
    bool Success,
    string EncryptedData,
    string KeyId,
    string Algorithm
);

[ResponseType("codex.security.decryption-request", "DecryptionRequest", "Request for data decryption")]
public record DecryptionRequest(
    string EncryptedData,
    string KeyId,
    string Algorithm
);

[ResponseType("codex.security.decryption-response", "DecryptionResponse", "Response for data decryption")]
public record DecryptionResponse(
    bool Success,
    string DecryptedData,
    string KeyId
);

[ResponseType("codex.security.audit-log-response", "AuditLogResponse", "Response for audit log retrieval")]
public record AuditLogResponse(
    bool Success,
    List<SecurityAuditLog> Logs,
    int TotalCount,
    int Page,
    int PageSize
);

[ResponseType("codex.security.token-validation-request", "TokenValidationRequest", "Request for token validation")]
public record TokenValidationRequest(
    string Token
);

[ResponseType("codex.security.token-validation-response", "TokenValidationResponse", "Response for token validation")]
public record TokenValidationResponse(
    bool Success,
    bool Valid,
    Dictionary<string, string>? Claims,
    DateTime? ExpiresAt
);

[ResponseType("codex.security.logout-response", "LogoutResponse", "Response for user logout")]
public record LogoutResponse(
    bool Success,
    string Message
);

// Supporting classes
[ResponseType("codex.security.user", "SecurityUser", "Security user entity")]
public class SecurityUser
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

[ResponseType("codex.security.user-session", "UserSession", "User session entity")]
public class UserSession
{
    public string UserId { get; set; } = "";
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

[ResponseType("codex.security.role", "SecurityRole", "Security role entity")]
public class SecurityRole
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SecurityPermission> Permissions { get; set; } = new();
}

[ResponseType("codex.security.permission", "SecurityPermission", "Security permission entity")]
public class SecurityPermission
{
    public string Action { get; set; } = "";
    public string Resource { get; set; } = "";
}

[ResponseType("codex.security.policy", "SecurityPolicy", "Security policy entity")]
public class SecurityPolicy
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, object> Rules { get; set; } = new();
}

[ResponseType("codex.security.audit-log", "SecurityAuditLog", "Security audit log entity")]
public class SecurityAuditLog
{
    public string Id { get; set; } = "";
    public string Action { get; set; } = "";
    public string Description { get; set; } = "";
    public string UserId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = "";
    public string UserAgent { get; set; } = "";
}
