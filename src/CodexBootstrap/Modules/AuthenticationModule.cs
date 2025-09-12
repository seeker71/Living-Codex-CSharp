using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace CodexBootstrap.Modules;

/// <summary>
/// Authentication and authorization module
/// </summary>
public sealed class AuthenticationModule : IModule
{
    private readonly IAuthenticationService _authService;
    private readonly IAuthorizationService _authzService;
    private readonly Core.ILogger _logger;

    public AuthenticationModule(IServiceProvider serviceProvider)
    {
        // Create services directly since we can't modify the service collection at this point
        var userRepository = new InMemoryUserRepository();
        var roleRepository = new InMemoryRoleRepository();
        var permissionRepository = new InMemoryPermissionRepository();
        
        var jwtSettings = new JwtSettings(
            SecretKey: Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-key-that-is-at-least-32-characters-long",
            Issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "CodexBootstrap",
            Audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "CodexBootstrap",
            ExpirationMinutes: int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60")
        );
        
        _authService = new JwtAuthenticationService(userRepository, jwtSettings);
        _authzService = new RoleBasedAuthorizationService(userRepository, roleRepository, permissionRepository);
        _logger = new Log4NetLogger(typeof(AuthenticationModule));
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.auth",
            name: "Authentication and Authorization Module",
            version: "0.1.0",
            description: "Module for user authentication, authorization, and access control"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Handled by attribute discovery
    }

    [ApiRoute("POST", "/auth/login", "auth-login", "Authenticate user", "codex.auth")]
    public async Task<object> LoginAsync(LoginRequest request)
    {
        try
        {
            var result = await _authService.AuthenticateAsync(request.Username, request.Password);
            if (result.Success)
            {
                return new
                {
                    success = true,
                    user = new
                    {
                        id = result.User!.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        roles = result.User.Roles
                    },
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt
                };
            }
            else
            {
                return new ErrorResponse(result.Error ?? "Authentication failed");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Login failed: {ex.Message}", ex);
            return new ErrorResponse("Login failed");
        }
    }

    [ApiRoute("POST", "/auth/register", "auth-register", "Register new user", "codex.auth")]
    public async Task<object> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterUserAsync(request.Username, request.Email, request.Password, request.Roles);
            if (result.Success)
            {
                return new
                {
                    success = true,
                    user = new
                    {
                        id = result.User!.Id,
                        username = result.User.Username,
                        email = result.User.Email,
                        roles = result.User.Roles
                    },
                    message = "User registered successfully"
                };
            }
            else
            {
                return new ErrorResponse(result.Error ?? "Registration failed");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Registration failed: {ex.Message}", ex);
            return new ErrorResponse("Registration failed");
        }
    }

    [ApiRoute("POST", "/auth/refresh", "auth-refresh", "Refresh token", "codex.auth")]
    public async Task<object> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var newToken = await _authService.RefreshTokenAsync(request.RefreshToken);
            return new
            {
                success = true,
                token = newToken
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Token refresh failed: {ex.Message}", ex);
            return new ErrorResponse("Token refresh failed");
        }
    }

    [ApiRoute("POST", "/auth/logout", "auth-logout", "Logout user", "codex.auth")]
    public async Task<object> LogoutAsync(LogoutRequest request)
    {
        try
        {
            var success = await _authService.RevokeTokenAsync(request.Token);
            return new
            {
                success = success,
                message = success ? "Logged out successfully" : "Logout failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Logout failed: {ex.Message}", ex);
            return new ErrorResponse("Logout failed");
        }
    }

    [ApiRoute("GET", "/auth/profile", "auth-profile", "Get user profile", "codex.auth")]
    public async Task<object> GetProfileAsync(string token)
    {
        try
        {
            var user = await _authService.GetUserFromTokenAsync(token);
            if (user == null)
            {
                return new ErrorResponse("Invalid token");
            }

            return new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    roles = user.Roles,
                    isActive = user.IsActive,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Get profile failed: {ex.Message}", ex);
            return new ErrorResponse("Get profile failed");
        }
    }

    [ApiRoute("POST", "/auth/change-password", "auth-change-password", "Change password", "codex.auth")]
    public async Task<object> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var success = await _authService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword);
            return new
            {
                success = success,
                message = success ? "Password changed successfully" : "Password change failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Change password failed: {ex.Message}", ex);
            return new ErrorResponse("Change password failed");
        }
    }

    [ApiRoute("POST", "/auth/reset-password", "auth-reset-password", "Reset password", "codex.auth")]
    public async Task<object> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var success = await _authService.ResetPasswordAsync(request.Email);
            return new
            {
                success = success,
                message = "Password reset email sent" // Don't reveal if email exists
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Reset password failed: {ex.Message}", ex);
            return new ErrorResponse("Reset password failed");
        }
    }

    [ApiRoute("GET", "/auth/permissions", "auth-permissions", "Get user permissions", "codex.auth")]
    public async Task<object> GetPermissionsAsync(string userId)
    {
        try
        {
            var permissions = await _authzService.GetUserPermissionsAsync(userId);
            return new
            {
                success = true,
                permissions = permissions.Select(p => new
                {
                    id = p.Id,
                    resource = p.Resource,
                    action = p.Action,
                    description = p.Description
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Get permissions failed: {ex.Message}", ex);
            return new ErrorResponse("Get permissions failed");
        }
    }

    [ApiRoute("GET", "/auth/roles", "auth-roles", "Get user roles", "codex.auth")]
    public async Task<object> GetRolesAsync(string userId)
    {
        try
        {
            var roles = await _authzService.GetUserRolesAsync(userId);
            return new
            {
                success = true,
                roles = roles.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    description = r.Description,
                    permissions = r.Permissions
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Get roles failed: {ex.Message}", ex);
            return new ErrorResponse("Get roles failed");
        }
    }

    [ApiRoute("POST", "/auth/grant-permission", "auth-grant-permission", "Grant permission", "codex.auth")]
    public async Task<object> GrantPermissionAsync(GrantPermissionRequest request)
    {
        try
        {
            var success = await _authzService.GrantPermissionAsync(request.UserId, request.Resource, request.Action);
            return new
            {
                success = success,
                message = success ? "Permission granted successfully" : "Permission grant failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Grant permission failed: {ex.Message}", ex);
            return new ErrorResponse("Grant permission failed");
        }
    }

    [ApiRoute("POST", "/auth/revoke-permission", "auth-revoke-permission", "Revoke permission", "codex.auth")]
    public async Task<object> RevokePermissionAsync(RevokePermissionRequest request)
    {
        try
        {
            var success = await _authzService.RevokePermissionAsync(request.UserId, request.Resource, request.Action);
            return new
            {
                success = success,
                message = success ? "Permission revoked successfully" : "Permission revoke failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Revoke permission failed: {ex.Message}", ex);
            return new ErrorResponse("Revoke permission failed");
        }
    }

    [ApiRoute("POST", "/auth/assign-role", "auth-assign-role", "Assign role", "codex.auth")]
    public async Task<object> AssignRoleAsync(AssignRoleRequest request)
    {
        try
        {
            var success = await _authzService.AssignRoleAsync(request.UserId, request.Role);
            return new
            {
                success = success,
                message = success ? "Role assigned successfully" : "Role assignment failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Assign role failed: {ex.Message}", ex);
            return new ErrorResponse("Assign role failed");
        }
    }

    [ApiRoute("POST", "/auth/remove-role", "auth-remove-role", "Remove role", "codex.auth")]
    public async Task<object> RemoveRoleAsync(RemoveRoleRequest request)
    {
        try
        {
            var success = await _authzService.RemoveRoleAsync(request.UserId, request.Role);
            return new
            {
                success = success,
                message = success ? "Role removed successfully" : "Role removal failed"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Remove role failed: {ex.Message}", ex);
            return new ErrorResponse("Remove role failed");
        }
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Discovery is handled globally
    }
}

// Request/Response DTOs
[ResponseType("codex.auth.login-request", "LoginRequest", "Request for user login")]
public record LoginRequest(string Username, string Password);

[ResponseType("codex.auth.register-request", "RegisterRequest", "Request for user registration")]
public record RegisterRequest(string Username, string Email, string Password, string[]? Roles = null);

[ResponseType("codex.auth.refresh-token-request", "RefreshTokenRequest", "Request for token refresh")]
public record RefreshTokenRequest(string RefreshToken);

[ResponseType("codex.auth.logout-request", "LogoutRequest", "Request for user logout")]
public record LogoutRequest(string Token);

[ResponseType("codex.auth.change-password-request", "ChangePasswordRequest", "Request for password change")]
public record ChangePasswordRequest(string UserId, string CurrentPassword, string NewPassword);

[ResponseType("codex.auth.reset-password-request", "ResetPasswordRequest", "Request for password reset")]
public record ResetPasswordRequest(string Email);

[ResponseType("codex.auth.grant-permission-request", "GrantPermissionRequest", "Request for permission grant")]
public record GrantPermissionRequest(string UserId, string Resource, string Action);

[ResponseType("codex.auth.revoke-permission-request", "RevokePermissionRequest", "Request for permission revocation")]
public record RevokePermissionRequest(string UserId, string Resource, string Action);

[ResponseType("codex.auth.assign-role-request", "AssignRoleRequest", "Request for role assignment")]
public record AssignRoleRequest(string UserId, string Role);

[ResponseType("codex.auth.remove-role-request", "RemoveRoleRequest", "Request for role removal")]
public record RemoveRoleRequest(string UserId, string Role);
