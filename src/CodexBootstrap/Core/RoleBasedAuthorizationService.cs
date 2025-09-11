using System.Security.Claims;

namespace CodexBootstrap.Core;

/// <summary>
/// Role-based authorization service
/// </summary>
public class RoleBasedAuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger _logger;

    public RoleBasedAuthorizationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _logger = new Log4NetLogger(typeof(RoleBasedAuthorizationService));
    }

    public async Task<bool> HasPermissionAsync(string userId, string resource, string action)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            // Check direct user permissions
            var userPermissions = await _permissionRepository.GetUserPermissionsAsync(userId);
            if (userPermissions.Any(p => p.Resource == resource && p.Action == action))
            {
                return true;
            }

            // Check role-based permissions
            foreach (var roleName in user.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName);
                if (role != null && role.Permissions.Contains($"{resource}:{action}"))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Permission check failed for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> HasRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null && user.IsActive && user.Roles.Contains(role);
        }
        catch (Exception ex)
        {
            _logger.Error($"Role check failed for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Enumerable.Empty<Permission>();
            }

            var permissions = new List<Permission>();

            // Get direct user permissions
            var userPermissions = await _permissionRepository.GetUserPermissionsAsync(userId);
            permissions.AddRange(userPermissions);

            // Get role-based permissions
            foreach (var roleName in user.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName);
                if (role != null)
                {
                    foreach (var permissionString in role.Permissions)
                    {
                        var parts = permissionString.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var permission = await _permissionRepository.GetByResourceAndActionAsync(parts[0], parts[1]);
                            if (permission != null && !permissions.Any(p => p.Id == permission.Id))
                            {
                                permissions.Add(permission);
                            }
                        }
                    }
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get permissions for user {userId}: {ex.Message}", ex);
            return Enumerable.Empty<Permission>();
        }
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Enumerable.Empty<Role>();
            }

            var roles = new List<Role>();
            foreach (var roleName in user.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName);
                if (role != null)
                {
                    roles.Add(role);
                }
            }

            return roles;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get roles for user {userId}: {ex.Message}", ex);
            return Enumerable.Empty<Role>();
        }
    }

    public async Task<bool> GrantPermissionAsync(string userId, string resource, string action)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            // Check if permission exists
            var permission = await _permissionRepository.GetByResourceAndActionAsync(resource, action);
            if (permission == null)
            {
                // Create permission if it doesn't exist
                permission = new Permission(
                    Id: Guid.NewGuid().ToString(),
                    Resource: resource,
                    Action: action,
                    Description: $"Permission for {action} on {resource}",
                    CreatedAt: DateTime.UtcNow
                );
                await _permissionRepository.CreateAsync(permission);
            }

            // Grant permission to user
            await _permissionRepository.GrantToUserAsync(userId, permission.Id);

            _logger.Info($"Permission {resource}:{action} granted to user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to grant permission to user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> RevokePermissionAsync(string userId, string resource, string action)
    {
        try
        {
            var permission = await _permissionRepository.GetByResourceAndActionAsync(resource, action);
            if (permission == null)
            {
                return false;
            }

            await _permissionRepository.RevokeFromUserAsync(userId, permission.Id);

            _logger.Info($"Permission {resource}:{action} revoked from user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to revoke permission from user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> AssignRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            var roleEntity = await _roleRepository.GetByNameAsync(role);
            if (roleEntity == null)
            {
                return false;
            }

            if (user.Roles.Contains(role))
            {
                return true; // Already has the role
            }

            var updatedRoles = user.Roles.Concat(new[] { role }).ToArray();
            var updatedUser = user with { Roles = updatedRoles };
            await _userRepository.UpdateAsync(updatedUser);

            _logger.Info($"Role {role} assigned to user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to assign role to user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(string userId, string role)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            if (!user.Roles.Contains(role))
            {
                return true; // Already doesn't have the role
            }

            var updatedRoles = user.Roles.Where(r => r != role).ToArray();
            var updatedUser = user with { Roles = updatedRoles };
            await _userRepository.UpdateAsync(updatedUser);

            _logger.Info($"Role {role} removed from user {userId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to remove role from user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<Role> CreateRoleAsync(string name, string description, string[]? permissions = null)
    {
        try
        {
            var role = new Role(
                Id: Guid.NewGuid().ToString(),
                Name: name,
                Description: description,
                Permissions: permissions ?? Array.Empty<string>(),
                CreatedAt: DateTime.UtcNow
            );

            await _roleRepository.CreateAsync(role);
            _logger.Info($"Role {name} created successfully");

            return role;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create role {name}: {ex.Message}", ex);
            throw;
        }
    }

    public async Task<Permission> CreatePermissionAsync(string resource, string action, string description)
    {
        try
        {
            var permission = new Permission(
                Id: Guid.NewGuid().ToString(),
                Resource: resource,
                Action: action,
                Description: description,
                CreatedAt: DateTime.UtcNow
            );

            await _permissionRepository.CreateAsync(permission);
            _logger.Info($"Permission {resource}:{action} created successfully");

            return permission;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create permission {resource}:{action}: {ex.Message}", ex);
            throw;
        }
    }
}
