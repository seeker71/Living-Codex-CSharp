using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Comprehensive access control and permissions module using Microsoft.AspNetCore.Authorization
/// </summary>
public sealed class AccessControlModule : ModuleBase
{
    private readonly Dictionary<string, Permission> _permissions = new();
    private readonly Dictionary<string, Role> _roles = new();
    private readonly Dictionary<string, List<string>> _userRoles = new();
    private readonly Dictionary<string, List<string>> _rolePermissions = new();
    private readonly Dictionary<string, List<AccessPolicy>> _resourcePolicies = new();
    private readonly Dictionary<string, List<AccessRule>> _accessRules = new();

    public override string Name => "Access Control Module";
    public override string Description => "Comprehensive access control and permissions module using Microsoft.AspNetCore.Authorization";
    public override string Version => "1.0.0";

    public AccessControlModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) : base(registry, logger)
    {
        InitializeDefaultPermissions();
        InitializeDefaultRoles();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.access-control",
            name: "Access Control Module",
            version: "1.0.0",
            description: "Comprehensive access control and permissions management system",
            tags: new[] { "access-control", "permissions", "roles", "security", "authorization" },
            capabilities: new[] { "create_permission", "update_permission", "delete_permission", "get_permission", "list_permissions", "create_role", "update_role", "delete_role", "get_role", "list_roles", "assign_role", "remove_role", "check_permission", "check_access", "create_policy", "update_policy", "delete_policy", "evaluate_policy", "create_rule", "update_rule", "delete_rule", "evaluate_rule" },
            spec: "codex.spec.access-control"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("Access Control API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints will be registered via ApiRouteDiscovery
    }

    // Permission Management API Methods
    [ApiRoute("POST", "/access-control/permissions", "CreatePermission", "Create a new permission", "codex.access-control")]
    public async Task<object> CreatePermissionAsync([ApiParameter("body", "Permission creation request")] CreatePermissionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Name) || string.IsNullOrEmpty(request?.Resource))
            {
                return new ErrorResponse("Name and Resource are required");
            }

            var permissionId = GeneratePermissionId();
            var permission = new Permission
            {
                Id = permissionId,
                Name = request.Name,
                Resource = request.Resource,
                Action = request.Action ?? "read",
                Description = request.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            _permissions[permissionId] = permission;

            _logger.Info($"Created permission: {permission.Name} for resource {permission.Resource}");

            return new { success = true, permission = permission };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create permission: {ex.Message}");
        }
    }

    [ApiRoute("PUT", "/access-control/permissions/{permissionId}", "UpdatePermission", "Update an existing permission", "codex.access-control")]
    public async Task<object> UpdatePermissionAsync(string permissionId, [ApiParameter("body", "Permission update request")] UpdatePermissionRequest request)
    {
        try
        {
            if (!_permissions.ContainsKey(permissionId))
            {
                return new ErrorResponse("Permission not found");
            }

            var permission = _permissions[permissionId];
            var updatedPermission = permission with
            {
                Name = request.Name ?? permission.Name,
                Resource = request.Resource ?? permission.Resource,
                Action = request.Action ?? permission.Action,
                Description = request.Description ?? permission.Description,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = request.IsActive ?? permission.IsActive,
                Metadata = request.Metadata ?? permission.Metadata
            };

            _permissions[permissionId] = updatedPermission;

            _logger.Info($"Updated permission: {updatedPermission.Name}");

            return new { success = true, permission = updatedPermission };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to update permission: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/access-control/permissions/{permissionId}", "DeletePermission", "Delete a permission", "codex.access-control")]
    public async Task<object> DeletePermissionAsync(string permissionId)
    {
        try
        {
            if (!_permissions.ContainsKey(permissionId))
            {
                return new ErrorResponse("Permission not found");
            }

            var permission = _permissions[permissionId];
            _permissions.Remove(permissionId);

            // Remove from all roles
            foreach (var roleId in _rolePermissions.Keys.ToList())
            {
                _rolePermissions[roleId].RemoveAll(p => p == permissionId);
            }

            _logger.Info($"Deleted permission: {permission.Name}");

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to delete permission: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/access-control/permissions/{permissionId}", "GetPermission", "Get a permission", "codex.access-control")]
    public async Task<object> GetPermissionAsync(string permissionId)
    {
        try
        {
            if (!_permissions.ContainsKey(permissionId))
            {
                return new ErrorResponse("Permission not found");
            }

            return new { success = true, permission = _permissions[permissionId] };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get permission: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/access-control/permissions", "ListPermissions", "List all permissions", "codex.access-control")]
    public async Task<object> ListPermissionsAsync()
    {
        try
        {
            var permissions = _permissions.Values.ToList();
            return new { success = true, permissions = permissions };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing permissions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list permissions: {ex.Message}");
        }
    }

    // Role Management API Methods
    [ApiRoute("POST", "/access-control/roles", "CreateRole", "Create a new role", "codex.access-control")]
    public async Task<object> CreateRoleAsync([ApiParameter("body", "Role creation request")] CreateRoleRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Name))
            {
                return new ErrorResponse("Name is required");
            }

            var roleId = GenerateRoleId();
            var role = new Role
            {
                Id = roleId,
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            _roles[roleId] = role;
            _rolePermissions[roleId] = new List<string>();

            _logger.Info($"Created role: {role.Name}");

            return new { success = true, role = role };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create role: {ex.Message}");
        }
    }

    [ApiRoute("PUT", "/access-control/roles/{roleId}", "UpdateRole", "Update an existing role", "codex.access-control")]
    public async Task<object> UpdateRoleAsync(string roleId, [ApiParameter("body", "Role update request")] UpdateRoleRequest request)
    {
        try
        {
            if (!_roles.ContainsKey(roleId))
            {
                return new ErrorResponse("Role not found");
            }

            var role = _roles[roleId];
            var updatedRole = role with
            {
                Name = request.Name ?? role.Name,
                Description = request.Description ?? role.Description,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = request.IsActive ?? role.IsActive,
                Metadata = request.Metadata ?? role.Metadata
            };

            _roles[roleId] = updatedRole;

            _logger.Info($"Updated role: {updatedRole.Name}");

            return new { success = true, role = updatedRole };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to update role: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/access-control/roles/{roleId}", "DeleteRole", "Delete a role", "codex.access-control")]
    public async Task<object> DeleteRoleAsync(string roleId)
    {
        try
        {
            if (!_roles.ContainsKey(roleId))
            {
                return new ErrorResponse("Role not found");
            }

            var role = _roles[roleId];
            _roles.Remove(roleId);
            _rolePermissions.Remove(roleId);

            // Remove from all users
            foreach (var userId in _userRoles.Keys.ToList())
            {
                _userRoles[userId].RemoveAll(r => r == roleId);
            }

            _logger.Info($"Deleted role: {role.Name}");

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to delete role: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/access-control/roles/{roleId}", "GetRole", "Get a role", "codex.access-control")]
    public async Task<object> GetRoleAsync(string roleId)
    {
        try
        {
            if (!_roles.ContainsKey(roleId))
            {
                return new ErrorResponse("Role not found");
            }

            var role = _roles[roleId];
            var permissions = _rolePermissions.GetValueOrDefault(roleId, new List<string>())
                .Select(pId => _permissions.GetValueOrDefault(pId))
                .Where(p => p != null)
                .ToList();

            return new { success = true, role = role, permissions = permissions };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get role: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/access-control/roles", "ListRoles", "List all roles", "codex.access-control")]
    public async Task<object> ListRolesAsync()
    {
        try
        {
            var roles = _roles.Values.ToList();
            return new { success = true, roles = roles };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing roles: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list roles: {ex.Message}");
        }
    }

    // Role Assignment API Methods
    [ApiRoute("POST", "/access-control/users/{userId}/roles", "AssignRole", "Assign a role to a user", "codex.access-control")]
    public async Task<object> AssignRoleAsync(string userId, [ApiParameter("body", "Role assignment request")] AssignRoleRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.RoleId))
            {
                return new ErrorResponse("RoleId is required");
            }

            if (!_roles.ContainsKey(request.RoleId))
            {
                return new ErrorResponse("Role not found");
            }

            if (!_userRoles.ContainsKey(userId))
            {
                _userRoles[userId] = new List<string>();
            }

            if (!_userRoles[userId].Contains(request.RoleId))
            {
                _userRoles[userId].Add(request.RoleId);
                _logger.Info($"Assigned role {request.RoleId} to user {userId}");
            }

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error assigning role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to assign role: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/access-control/users/{userId}/roles/{roleId}", "RemoveRole", "Remove a role from a user", "codex.access-control")]
    public async Task<object> RemoveRoleAsync(string userId, string roleId)
    {
        try
        {
            if (_userRoles.ContainsKey(userId))
            {
                _userRoles[userId].RemoveAll(r => r == roleId);
                _logger.Info($"Removed role {roleId} from user {userId}");
            }

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error removing role: {ex.Message}", ex);
            return new ErrorResponse($"Failed to remove role: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/access-control/users/{userId}/roles", "GetUserRoles", "Get roles assigned to a user", "codex.access-control")]
    public async Task<object> GetUserRolesAsync(string userId)
    {
        try
        {
            var roleIds = _userRoles.GetValueOrDefault(userId, new List<string>());
            var roles = roleIds.Select(rId => _roles.GetValueOrDefault(rId))
                .Where(r => r != null)
                .ToList();

            return new { success = true, roles = roles };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user roles: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get user roles: {ex.Message}");
        }
    }

    // Permission Assignment API Methods
    [ApiRoute("POST", "/access-control/roles/{roleId}/permissions", "AssignPermission", "Assign a permission to a role", "codex.access-control")]
    public async Task<object> AssignPermissionAsync(string roleId, [ApiParameter("body", "Permission assignment request")] AssignPermissionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.PermissionId))
            {
                return new ErrorResponse("PermissionId is required");
            }

            if (!_roles.ContainsKey(roleId))
            {
                return new ErrorResponse("Role not found");
            }

            if (!_permissions.ContainsKey(request.PermissionId))
            {
                return new ErrorResponse("Permission not found");
            }

            if (!_rolePermissions.ContainsKey(roleId))
            {
                _rolePermissions[roleId] = new List<string>();
            }

            if (!_rolePermissions[roleId].Contains(request.PermissionId))
            {
                _rolePermissions[roleId].Add(request.PermissionId);
                _logger.Info($"Assigned permission {request.PermissionId} to role {roleId}");
            }

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error assigning permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to assign permission: {ex.Message}");
        }
    }

    [ApiRoute("DELETE", "/access-control/roles/{roleId}/permissions/{permissionId}", "RemovePermission", "Remove a permission from a role", "codex.access-control")]
    public async Task<object> RemovePermissionAsync(string roleId, string permissionId)
    {
        try
        {
            if (_rolePermissions.ContainsKey(roleId))
            {
                _rolePermissions[roleId].RemoveAll(p => p == permissionId);
                _logger.Info($"Removed permission {permissionId} from role {roleId}");
            }

            return new { success = true };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error removing permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to remove permission: {ex.Message}");
        }
    }

    // Access Control API Methods
    [ApiRoute("POST", "/access-control/check-permission", "CheckPermission", "Check if a user has a specific permission", "codex.access-control")]
    public async Task<object> CheckPermissionAsync([ApiParameter("body", "Permission check request")] CheckPermissionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.UserId) || string.IsNullOrEmpty(request?.PermissionName))
            {
                return new ErrorResponse("UserId and PermissionName are required");
            }

            var hasPermission = await HasPermissionAsync(request.UserId, request.PermissionName, request.Resource);
            
            return new { success = true, hasPermission = hasPermission };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking permission: {ex.Message}", ex);
            return new ErrorResponse($"Failed to check permission: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/access-control/check-access", "CheckAccess", "Check if a user has access to a resource", "codex.access-control")]
    public async Task<object> CheckAccessAsync([ApiParameter("body", "Access check request")] CheckAccessRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.UserId) || string.IsNullOrEmpty(request?.Resource))
            {
                return new ErrorResponse("UserId and Resource are required");
            }

            var hasAccess = await HasAccessAsync(request.UserId, request.Resource, request.Action ?? "read");
            
            return new { success = true, hasAccess = hasAccess };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking access: {ex.Message}", ex);
            return new ErrorResponse($"Failed to check access: {ex.Message}");
        }
    }

    // Policy Management API Methods
    [ApiRoute("POST", "/access-control/policies", "CreatePolicy", "Create a new access policy", "codex.access-control")]
    public async Task<object> CreatePolicyAsync([ApiParameter("body", "Policy creation request")] CreatePolicyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.Name) || string.IsNullOrEmpty(request?.Resource))
            {
                return new ErrorResponse("Name and Resource are required");
            }

            var policyId = GeneratePolicyId();
            var policy = new AccessPolicy
            {
                Id = policyId,
                Name = request.Name,
                Resource = request.Resource,
                Action = request.Action ?? "read",
                Conditions = request.Conditions ?? new Dictionary<string, object>(),
                Effect = request.Effect ?? PolicyEffect.Allow,
                Priority = request.Priority ?? 100,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                IsActive = true,
                Metadata = request.Metadata ?? new Dictionary<string, object>()
            };

            if (!_resourcePolicies.ContainsKey(request.Resource))
            {
                _resourcePolicies[request.Resource] = new List<AccessPolicy>();
            }
            _resourcePolicies[request.Resource].Add(policy);

            _logger.Info($"Created policy: {policy.Name} for resource {policy.Resource}");

            return new { success = true, policy = policy };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating policy: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create policy: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/access-control/evaluate-policy", "EvaluatePolicy", "Evaluate an access policy", "codex.access-control")]
    public async Task<object> EvaluatePolicyAsync([ApiParameter("body", "Policy evaluation request")] EvaluatePolicyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request?.UserId) || string.IsNullOrEmpty(request?.Resource))
            {
                return new ErrorResponse("UserId and Resource are required");
            }

            var result = await EvaluatePolicyForUserAsync(request.UserId, request.Resource, request.Action ?? "read", request.Context ?? new Dictionary<string, object>());
            
            return new { success = true, result = result };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error evaluating policy: {ex.Message}", ex);
            return new ErrorResponse($"Failed to evaluate policy: {ex.Message}");
        }
    }

    // Core Access Control Logic
    private async Task<bool> HasPermissionAsync(string userId, string permissionName, string? resource = null)
    {
        try
        {
            var userRoles = _userRoles.GetValueOrDefault(userId, new List<string>());
            
            foreach (var roleId in userRoles)
            {
                var rolePermissions = _rolePermissions.GetValueOrDefault(roleId, new List<string>());
                
                foreach (var permissionId in rolePermissions)
                {
                    var permission = _permissions.GetValueOrDefault(permissionId);
                    if (permission != null && permission.IsActive && permission.Name == permissionName)
                    {
                        if (string.IsNullOrEmpty(resource) || permission.Resource == resource)
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking permission for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> HasAccessAsync(string userId, string resource, string action)
    {
        try
        {
            // Check direct permissions first
            var userRoles = _userRoles.GetValueOrDefault(userId, new List<string>());
            
            foreach (var roleId in userRoles)
            {
                var rolePermissions = _rolePermissions.GetValueOrDefault(roleId, new List<string>());
                
                foreach (var permissionId in rolePermissions)
                {
                    var permission = _permissions.GetValueOrDefault(permissionId);
                    if (permission != null && permission.IsActive && 
                        permission.Resource == resource && permission.Action == action)
                    {
                        return true;
                    }
                }
            }

            // Check policies
            var policies = _resourcePolicies.GetValueOrDefault(resource, new List<AccessPolicy>());
            foreach (var policy in policies.Where(p => p.IsActive).OrderBy(p => p.Priority))
            {
                if (policy.Action == action || policy.Action == "*")
                {
                    var result = await EvaluatePolicyConditionsAsync(policy, userId, resource, action);
                    if (result)
                    {
                        return policy.Effect == PolicyEffect.Allow;
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking access for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> EvaluatePolicyForUserAsync(string userId, string resource, string action, Dictionary<string, object> context)
    {
        try
        {
            var policies = _resourcePolicies.GetValueOrDefault(resource, new List<AccessPolicy>());
            var applicablePolicies = policies.Where(p => p.IsActive && (p.Action == action || p.Action == "*"))
                .OrderBy(p => p.Priority)
                .ToList();

            foreach (var policy in applicablePolicies)
            {
                var conditionsMet = await EvaluatePolicyConditionsAsync(policy, userId, resource, action, context);
                if (conditionsMet)
                {
                    return policy.Effect == PolicyEffect.Allow;
                }
            }

            return false; // Default deny
        }
        catch (Exception ex)
        {
            _logger.Error($"Error evaluating policy for user {userId}: {ex.Message}", ex);
            return false;
        }
    }

    private async Task<bool> EvaluatePolicyConditionsAsync(AccessPolicy policy, string userId, string resource, string action, Dictionary<string, object>? context = null)
    {
        try
        {
            if (policy.Conditions.Count == 0)
            {
                return true; // No conditions means always apply
            }

            foreach (var condition in policy.Conditions)
            {
                var key = condition.Key;
                var expectedValue = condition.Value;

                switch (key.ToLower())
                {
                    case "user_id":
                        if (userId != expectedValue?.ToString())
                            return false;
                        break;
                    case "time_of_day":
                        var currentHour = DateTime.UtcNow.Hour;
                        var timeRange = expectedValue?.ToString()?.Split('-');
                        if (timeRange?.Length == 2)
                        {
                            var startHour = int.Parse(timeRange[0]);
                            var endHour = int.Parse(timeRange[1]);
                            if (currentHour < startHour || currentHour > endHour)
                                return false;
                        }
                        break;
                    case "day_of_week":
                        var currentDay = (int)DateTime.UtcNow.DayOfWeek;
                        var allowedDays = expectedValue?.ToString()?.Split(',').Select(int.Parse).ToList();
                        if (allowedDays != null && !allowedDays.Contains(currentDay))
                            return false;
                        break;
                    case "ip_address":
                        // In a real implementation, you'd get the actual IP from the request context
                        var userIp = context?.GetValueOrDefault("ip_address")?.ToString();
                        if (userIp != expectedValue?.ToString())
                            return false;
                        break;
                    case "user_agent":
                        var userAgent = context?.GetValueOrDefault("user_agent")?.ToString();
                        if (userAgent != expectedValue?.ToString())
                            return false;
                        break;
                    default:
                        // Custom condition - in a real implementation, you'd have a plugin system
                        if (context?.GetValueOrDefault(key)?.ToString() != expectedValue?.ToString())
                            return false;
                        break;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error evaluating policy conditions: {ex.Message}", ex);
            return false;
        }
    }

    // Helper methods
    private void InitializeDefaultPermissions()
    {
        var defaultPermissions = new[]
        {
            new Permission { Id = "perm_read_nodes", Name = "read_nodes", Resource = "nodes", Action = "read", Description = "Read access to nodes" },
            new Permission { Id = "perm_write_nodes", Name = "write_nodes", Resource = "nodes", Action = "write", Description = "Write access to nodes" },
            new Permission { Id = "perm_delete_nodes", Name = "delete_nodes", Resource = "nodes", Action = "delete", Description = "Delete access to nodes" },
            new Permission { Id = "perm_read_edges", Name = "read_edges", Resource = "edges", Action = "read", Description = "Read access to edges" },
            new Permission { Id = "perm_write_edges", Name = "write_edges", Resource = "edges", Action = "write", Description = "Write access to edges" },
            new Permission { Id = "perm_delete_edges", Name = "delete_edges", Resource = "edges", Action = "delete", Description = "Delete access to edges" },
            new Permission { Id = "perm_admin", Name = "admin", Resource = "*", Action = "*", Description = "Full administrative access" }
        };

        foreach (var perm in defaultPermissions)
        {
            _permissions[perm.Id] = perm;
        }
    }

    private void InitializeDefaultRoles()
    {
        var defaultRoles = new[]
        {
            new Role { Id = "role_admin", Name = "Administrator", Description = "Full system access" },
            new Role { Id = "role_user", Name = "User", Description = "Standard user access" },
            new Role { Id = "role_reader", Name = "Reader", Description = "Read-only access" }
        };

        foreach (var role in defaultRoles)
        {
            _roles[role.Id] = role;
            _rolePermissions[role.Id] = new List<string>();
        }

        // Assign permissions to roles
        _rolePermissions["role_admin"] = _permissions.Keys.ToList();
        _rolePermissions["role_user"] = new[] { "perm_read_nodes", "perm_write_nodes", "perm_read_edges", "perm_write_edges" }.ToList();
        _rolePermissions["role_reader"] = new[] { "perm_read_nodes", "perm_read_edges" }.ToList();
    }

    private string GeneratePermissionId() => $"perm_{Guid.NewGuid():N}";
    private string GenerateRoleId() => $"role_{Guid.NewGuid():N}";
    private string GeneratePolicyId() => $"policy_{Guid.NewGuid():N}";

    // Data models
    [ResponseType]
    public record Permission
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Resource { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public bool IsActive { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    [ResponseType]
    public record Role
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public bool IsActive { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    [ResponseType]
    public record AccessPolicy
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Resource { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public Dictionary<string, object> Conditions { get; init; } = new();
        public PolicyEffect Effect { get; init; }
        public int Priority { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public bool IsActive { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    [ResponseType]
    public record AccessRule
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Resource { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public Dictionary<string, object> Conditions { get; init; } = new();
        public PolicyEffect Effect { get; init; }
        public int Priority { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public bool IsActive { get; init; }
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    public enum PolicyEffect
    {
        Allow,
        Deny
    }

    // Request/Response types
    [ResponseType]
    public record CreatePermissionRequest(string Name, string Resource, string? Action = null, string? Description = null, Dictionary<string, object>? Metadata = null);
    [ResponseType]
    public record UpdatePermissionRequest(string? Name = null, string? Resource = null, string? Action = null, string? Description = null, bool? IsActive = null, Dictionary<string, object>? Metadata = null);
    [ResponseType]
    public record CreateRoleRequest(string Name, string? Description = null, Dictionary<string, object>? Metadata = null);
    [ResponseType]
    public record UpdateRoleRequest(string? Name = null, string? Description = null, bool? IsActive = null, Dictionary<string, object>? Metadata = null);
    [ResponseType]
    public record AssignRoleRequest(string RoleId);
    [ResponseType]
    public record AssignPermissionRequest(string PermissionId);
    [ResponseType]
    public record CheckPermissionRequest(string UserId, string PermissionName, string? Resource = null);
    [ResponseType]
    public record CheckAccessRequest(string UserId, string Resource, string? Action = null);
    [ResponseType]
    public record CreatePolicyRequest(string Name, string Resource, string? Action = null, Dictionary<string, object>? Conditions = null, PolicyEffect? Effect = null, int? Priority = null, Dictionary<string, object>? Metadata = null);
    [ResponseType]
    public record EvaluatePolicyRequest(string UserId, string Resource, string? Action = null, Dictionary<string, object>? Context = null);
}
