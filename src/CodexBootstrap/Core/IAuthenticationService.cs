using System.Security.Claims;

namespace CodexBootstrap.Core;

/// <summary>
/// Interface for authentication services
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate a user with credentials
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    
    /// <summary>
    /// Authenticate a user with a token
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(string token);
    
    /// <summary>
    /// Generate a JWT token for a user
    /// </summary>
    Task<string> GenerateTokenAsync(User user);
    
    /// <summary>
    /// Refresh an existing token
    /// </summary>
    Task<string> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revoke a token
    /// </summary>
    Task<bool> RevokeTokenAsync(string token);
    
    /// <summary>
    /// Validate a token
    /// </summary>
    Task<TokenValidationResult> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Get user from token
    /// </summary>
    Task<User?> GetUserFromTokenAsync(string token);
    
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<RegistrationResult> RegisterUserAsync(string username, string email, string password, string[]? roles = null);
    
    /// <summary>
    /// Change user password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    
    /// <summary>
    /// Reset user password
    /// </summary>
    Task<bool> ResetPasswordAsync(string email);
}

/// <summary>
/// Interface for authorization services
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if user has permission for a resource
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string resource, string action);
    
    /// <summary>
    /// Check if user has role
    /// </summary>
    Task<bool> HasRoleAsync(string userId, string role);
    
    /// <summary>
    /// Get user permissions
    /// </summary>
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);
    
    /// <summary>
    /// Get user roles
    /// </summary>
    Task<IEnumerable<Role>> GetUserRolesAsync(string userId);
    
    /// <summary>
    /// Grant permission to user
    /// </summary>
    Task<bool> GrantPermissionAsync(string userId, string resource, string action);
    
    /// <summary>
    /// Revoke permission from user
    /// </summary>
    Task<bool> RevokePermissionAsync(string userId, string resource, string action);
    
    /// <summary>
    /// Assign role to user
    /// </summary>
    Task<bool> AssignRoleAsync(string userId, string role);
    
    /// <summary>
    /// Remove role from user
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, string role);
    
    /// <summary>
    /// Create a new role
    /// </summary>
    Task<Role> CreateRoleAsync(string name, string description, string[]? permissions = null);
    
    /// <summary>
    /// Create a new permission
    /// </summary>
    Task<Permission> CreatePermissionAsync(string resource, string action, string description);
}

/// <summary>
/// User entity
/// </summary>
public record User(
    string Id,
    string Username,
    string Email,
    string PasswordHash,
    string[] Roles,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Role entity
/// </summary>
public record Role(
    string Id,
    string Name,
    string Description,
    string[] Permissions,
    DateTime CreatedAt,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Permission entity
/// </summary>
public record Permission(
    string Id,
    string Resource,
    string Action,
    string Description,
    DateTime CreatedAt,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Authentication result
/// </summary>
public record AuthenticationResult(
    bool Success,
    User? User = null,
    string? Token = null,
    string? RefreshToken = null,
    string? Error = null,
    DateTime? ExpiresAt = null
);

/// <summary>
/// Token validation result
/// </summary>
public record TokenValidationResult(
    bool IsValid,
    User? User = null,
    ClaimsPrincipal? Principal = null,
    string? Error = null,
    DateTime? ExpiresAt = null
);

/// <summary>
/// Registration result
/// </summary>
public record RegistrationResult(
    bool Success,
    User? User = null,
    string? Error = null
);
