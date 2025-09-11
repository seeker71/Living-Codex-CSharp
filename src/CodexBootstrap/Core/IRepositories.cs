namespace CodexBootstrap.Core;

/// <summary>
/// User repository interface
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}

/// <summary>
/// Role repository interface
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(string id);
    Task<Role?> GetByNameAsync(string name);
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role> CreateAsync(Role role);
    Task<Role> UpdateAsync(Role role);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
}

/// <summary>
/// Permission repository interface
/// </summary>
public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(string id);
    Task<Permission?> GetByResourceAndActionAsync(string resource, string action);
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId);
    Task<Permission> CreateAsync(Permission permission);
    Task<Permission> UpdateAsync(Permission permission);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task GrantToUserAsync(string userId, string permissionId);
    Task RevokeFromUserAsync(string userId, string permissionId);
}

/// <summary>
/// In-memory user repository implementation
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new();
    private readonly Dictionary<string, string> _usernameIndex = new();
    private readonly Dictionary<string, string> _emailIndex = new();
    private readonly ILogger _logger;

    public InMemoryUserRepository()
    {
        _logger = new Log4NetLogger(typeof(InMemoryUserRepository));
    }

    public Task<User?> GetByIdAsync(string id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUsernameAsync(string username)
    {
        if (_usernameIndex.TryGetValue(username, out var userId))
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }
        return Task.FromResult<User?>(null);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        if (_emailIndex.TryGetValue(email, out var userId))
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }
        return Task.FromResult<User?>(null);
    }

    public Task<IEnumerable<User>> GetAllAsync()
    {
        return Task.FromResult(_users.Values.AsEnumerable());
    }

    public Task<User> CreateAsync(User user)
    {
        _users[user.Id] = user;
        _usernameIndex[user.Username] = user.Id;
        _emailIndex[user.Email] = user.Id;
        
        _logger.Info($"User {user.Username} created with ID {user.Id}");
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        if (_users.TryGetValue(user.Id, out var existingUser))
        {
            // Update indexes if username or email changed
            if (existingUser.Username != user.Username)
            {
                _usernameIndex.Remove(existingUser.Username);
                _usernameIndex[user.Username] = user.Id;
            }
            
            if (existingUser.Email != user.Email)
            {
                _emailIndex.Remove(existingUser.Email);
                _emailIndex[user.Email] = user.Id;
            }
        }

        _users[user.Id] = user;
        _logger.Info($"User {user.Username} updated");
        return Task.FromResult(user);
    }

    public Task<bool> DeleteAsync(string id)
    {
        if (_users.TryGetValue(id, out var user))
        {
            _users.Remove(id);
            _usernameIndex.Remove(user.Username);
            _emailIndex.Remove(user.Email);
            
            _logger.Info($"User {user.Username} deleted");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_users.ContainsKey(id));
    }
}

/// <summary>
/// In-memory role repository implementation
/// </summary>
public class InMemoryRoleRepository : IRoleRepository
{
    private readonly Dictionary<string, Role> _roles = new();
    private readonly Dictionary<string, string> _nameIndex = new();
    private readonly ILogger _logger;

    public InMemoryRoleRepository()
    {
        _logger = new Log4NetLogger(typeof(InMemoryRoleRepository));
    }

    public Task<Role?> GetByIdAsync(string id)
    {
        _roles.TryGetValue(id, out var role);
        return Task.FromResult(role);
    }

    public Task<Role?> GetByNameAsync(string name)
    {
        if (_nameIndex.TryGetValue(name, out var roleId))
        {
            _roles.TryGetValue(roleId, out var role);
            return Task.FromResult(role);
        }
        return Task.FromResult<Role?>(null);
    }

    public Task<IEnumerable<Role>> GetAllAsync()
    {
        return Task.FromResult(_roles.Values.AsEnumerable());
    }

    public Task<Role> CreateAsync(Role role)
    {
        _roles[role.Id] = role;
        _nameIndex[role.Name] = role.Id;
        
        _logger.Info($"Role {role.Name} created with ID {role.Id}");
        return Task.FromResult(role);
    }

    public Task<Role> UpdateAsync(Role role)
    {
        if (_roles.TryGetValue(role.Id, out var existingRole))
        {
            if (existingRole.Name != role.Name)
            {
                _nameIndex.Remove(existingRole.Name);
                _nameIndex[role.Name] = role.Id;
            }
        }

        _roles[role.Id] = role;
        _logger.Info($"Role {role.Name} updated");
        return Task.FromResult(role);
    }

    public Task<bool> DeleteAsync(string id)
    {
        if (_roles.TryGetValue(id, out var role))
        {
            _roles.Remove(id);
            _nameIndex.Remove(role.Name);
            
            _logger.Info($"Role {role.Name} deleted");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_roles.ContainsKey(id));
    }
}

/// <summary>
/// In-memory permission repository implementation
/// </summary>
public class InMemoryPermissionRepository : IPermissionRepository
{
    private readonly Dictionary<string, Permission> _permissions = new();
    private readonly Dictionary<string, string> _resourceActionIndex = new();
    private readonly Dictionary<string, HashSet<string>> _userPermissions = new();
    private readonly ILogger _logger;

    public InMemoryPermissionRepository()
    {
        _logger = new Log4NetLogger(typeof(InMemoryPermissionRepository));
    }

    public Task<Permission?> GetByIdAsync(string id)
    {
        _permissions.TryGetValue(id, out var permission);
        return Task.FromResult(permission);
    }

    public Task<Permission?> GetByResourceAndActionAsync(string resource, string action)
    {
        var key = $"{resource}:{action}";
        if (_resourceActionIndex.TryGetValue(key, out var permissionId))
        {
            _permissions.TryGetValue(permissionId, out var permission);
            return Task.FromResult(permission);
        }
        return Task.FromResult<Permission?>(null);
    }

    public Task<IEnumerable<Permission>> GetAllAsync()
    {
        return Task.FromResult(_permissions.Values.AsEnumerable());
    }

    public Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId)
    {
        if (_userPermissions.TryGetValue(userId, out var permissionIds))
        {
            var permissions = permissionIds
                .Select(id => _permissions.TryGetValue(id, out var permission) ? permission : null)
                .Where(p => p != null)
                .Cast<Permission>();
            return Task.FromResult(permissions);
        }
        return Task.FromResult(Enumerable.Empty<Permission>());
    }

    public Task<Permission> CreateAsync(Permission permission)
    {
        _permissions[permission.Id] = permission;
        var key = $"{permission.Resource}:{permission.Action}";
        _resourceActionIndex[key] = permission.Id;
        
        _logger.Info($"Permission {permission.Resource}:{permission.Action} created with ID {permission.Id}");
        return Task.FromResult(permission);
    }

    public Task<Permission> UpdateAsync(Permission permission)
    {
        if (_permissions.TryGetValue(permission.Id, out var existingPermission))
        {
            var oldKey = $"{existingPermission.Resource}:{existingPermission.Action}";
            var newKey = $"{permission.Resource}:{permission.Action}";
            
            if (oldKey != newKey)
            {
                _resourceActionIndex.Remove(oldKey);
                _resourceActionIndex[newKey] = permission.Id;
            }
        }

        _permissions[permission.Id] = permission;
        _logger.Info($"Permission {permission.Resource}:{permission.Action} updated");
        return Task.FromResult(permission);
    }

    public Task<bool> DeleteAsync(string id)
    {
        if (_permissions.TryGetValue(id, out var permission))
        {
            _permissions.Remove(id);
            var key = $"{permission.Resource}:{permission.Action}";
            _resourceActionIndex.Remove(key);
            
            // Remove from all user permissions
            foreach (var userId in _userPermissions.Keys.ToList())
            {
                _userPermissions[userId].Remove(id);
            }
            
            _logger.Info($"Permission {permission.Resource}:{permission.Action} deleted");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return Task.FromResult(_permissions.ContainsKey(id));
    }

    public Task GrantToUserAsync(string userId, string permissionId)
    {
        if (!_userPermissions.ContainsKey(userId))
        {
            _userPermissions[userId] = new HashSet<string>();
        }
        
        _userPermissions[userId].Add(permissionId);
        _logger.Info($"Permission {permissionId} granted to user {userId}");
        return Task.CompletedTask;
    }

    public Task RevokeFromUserAsync(string userId, string permissionId)
    {
        if (_userPermissions.TryGetValue(userId, out var permissions))
        {
            permissions.Remove(permissionId);
            _logger.Info($"Permission {permissionId} revoked from user {userId}");
        }
        return Task.CompletedTask;
    }
}
