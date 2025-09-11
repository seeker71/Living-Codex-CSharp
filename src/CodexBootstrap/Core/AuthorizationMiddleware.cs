using System.Security.Claims;

namespace CodexBootstrap.Core;

/// <summary>
/// Authorization middleware for protecting endpoints
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public AuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
        _logger = new Log4NetLogger(typeof(AuthorizationMiddleware));
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationService authService, IAuthorizationService authzService)
    {
        // Skip authorization for certain paths
        if (ShouldSkipAuthorization(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check for authorization attributes
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequireAuthAttribute>() != null)
        {
            var authResult = await CheckAuthentication(context, authService);
            if (!authResult.IsAuthenticated)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // Check authorization if specified
            var requirePermission = endpoint.Metadata.GetMetadata<RequirePermissionAttribute>();
            if (requirePermission != null)
            {
                var hasPermission = await authzService.HasPermissionAsync(
                    authResult.UserId!, 
                    requirePermission.Resource, 
                    requirePermission.Action);
                
                if (!hasPermission)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }

            var requireRole = endpoint.Metadata.GetMetadata<RequireRoleAttribute>();
            if (requireRole != null)
            {
                var hasRole = await authzService.HasRoleAsync(authResult.UserId!, requireRole.Role);
                if (!hasRole)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }

            // Add user info to context
            context.Items["UserId"] = authResult.UserId;
            context.Items["User"] = authResult.User;
        }

        await _next(context);
    }

    private bool ShouldSkipAuthorization(PathString path)
    {
        var skipPaths = new[]
        {
            "/auth/login",
            "/auth/register",
            "/auth/reset-password",
            "/health",
            "/swagger",
            "/openapi"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private async Task<AuthenticationCheckResult> CheckAuthentication(HttpContext context, IAuthenticationService authService)
    {
        try
        {
            // Try to get token from Authorization header
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var user = await authService.GetUserFromTokenAsync(token);
                if (user != null)
                {
                    return new AuthenticationCheckResult(true, user.Id, user);
                }
            }

            // Try to get token from query parameter
            var tokenParam = context.Request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tokenParam))
            {
                var user = await authService.GetUserFromTokenAsync(tokenParam);
                if (user != null)
                {
                    return new AuthenticationCheckResult(true, user.Id, user);
                }
            }

            return new AuthenticationCheckResult(false);
        }
        catch (Exception ex)
        {
            _logger.Error($"Authentication check failed: {ex.Message}", ex);
            return new AuthenticationCheckResult(false);
        }
    }
}

/// <summary>
/// Attribute to require authentication
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAuthAttribute : Attribute
{
}

/// <summary>
/// Attribute to require specific permission
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute
{
    public string Resource { get; }
    public string Action { get; }

    public RequirePermissionAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }
}

/// <summary>
/// Attribute to require specific role
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireRoleAttribute : Attribute
{
    public string Role { get; }

    public RequireRoleAttribute(string role)
    {
        Role = role;
    }
}

/// <summary>
/// Authentication check result
/// </summary>
public record AuthenticationCheckResult(
    bool IsAuthenticated,
    string? UserId = null,
    User? User = null
);
