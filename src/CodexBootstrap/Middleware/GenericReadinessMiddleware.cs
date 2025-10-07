using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Middleware
{
    /// <summary>
    /// Generic middleware that provides universal readiness checking for all API endpoints
    /// This is the most generic readiness handler that automatically applies to all API calls
    /// </summary>
    public class GenericReadinessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ReadinessTracker _readinessTracker;
        private readonly EndpointReadinessTracker _endpointTracker;
        private readonly ICodexLogger _logger;

        public GenericReadinessMiddleware(
            RequestDelegate next, 
            ReadinessTracker readinessTracker,
            EndpointReadinessTracker endpointTracker,
            ICodexLogger logger)
        {
            _next = next;
            _readinessTracker = readinessTracker;
            _endpointTracker = endpointTracker;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip readiness checks for system endpoints
            if (IsSystemEndpoint(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Get the endpoint metadata
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            // Check for AlwaysAvailable attribute - skip all readiness checks
            if (HasAttribute<AlwaysAvailableAttribute>(endpoint))
            {
                await _next(context);
                return;
            }

            // Apply universal readiness checking to all API endpoints
            var readinessResult = await ApplyUniversalReadinessCheck(context, endpoint);
            if (readinessResult != null)
            {
                context.Response.StatusCode = readinessResult.Value.StatusCode ?? 503;
                await context.Response.WriteAsJsonAsync(readinessResult.Value.Value);
                return;
            }

            // Apply degraded mode headers if applicable
            await ApplyDegradedModeHeaders(context, endpoint);

            await _next(context);
        }

        /// <summary>
        /// Check if the path is a system endpoint that should skip readiness checks
        /// </summary>
        private static bool IsSystemEndpoint(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? "";
            return pathValue.StartsWith("/readiness") ||
                   pathValue.StartsWith("/health") ||
                   pathValue.StartsWith("/swagger") ||
                   pathValue.StartsWith("/docs") ||
                   pathValue.StartsWith("/favicon.ico");
        }

        /// <summary>
        /// Apply universal readiness checking to all API endpoints
        /// This is the core generic readiness handler
        /// </summary>
        private async Task<(int? StatusCode, object Value)?> ApplyUniversalReadinessCheck(HttpContext context, Microsoft.AspNetCore.Http.Endpoint endpoint)
        {
            // Check for explicit ReadinessRequired attribute first
            var readinessAttr = GetAttribute<ReadinessRequiredAttribute>(endpoint);
            if (readinessAttr != null)
            {
                return await CheckReadinessRequirement(context, readinessAttr);
            }

            // For all other API endpoints, apply automatic readiness checking
            var moduleName = InferModuleFromPath(context.Request.Path);
            if (!string.IsNullOrEmpty(moduleName))
            {
                var moduleState = _readinessTracker.GetComponentState(moduleName);
                if (moduleState != ReadinessState.Ready)
                {
                    _logger.Debug($"API endpoint {context.Request.Path} blocked - module {moduleName} is {moduleState}");
                    return CreateNotReadyResponse(moduleState, $"Module '{moduleName}' is not ready", moduleName);
                }
            }

            return null;
        }

        /// <summary>
        /// Apply degraded mode headers to endpoints that support it
        /// </summary>
        private async Task ApplyDegradedModeHeaders(HttpContext context, Microsoft.AspNetCore.Http.Endpoint endpoint)
        {
            var degradedAttr = GetAttribute<DegradedWhenNotReadyAttribute>(endpoint);
            if (degradedAttr != null)
            {
                await HandleDegradedMode(context, degradedAttr);
            }
            else
            {
                // For endpoints without explicit degraded mode, add basic readiness headers
                var moduleName = InferModuleFromPath(context.Request.Path);
                if (!string.IsNullOrEmpty(moduleName))
                {
                    var moduleState = _readinessTracker.GetComponentState(moduleName);
                    if (moduleState != ReadinessState.Ready)
                    {
                        context.Response.Headers.Add("X-Readiness-State", moduleState.ToString());
                        context.Response.Headers.Add("X-Readiness-Module", moduleName);
                    }
                }
            }
        }

        private async Task<(int? StatusCode, object Value)?> CheckReadinessRequirement(HttpContext context, ReadinessRequiredAttribute attr)
        {
            var requiredModule = attr.RequiredModule;
            
            // If no module specified, try to infer from path
            if (string.IsNullOrEmpty(requiredModule))
            {
                requiredModule = InferModuleFromPath(context.Request.Path);
            }

            if (string.IsNullOrEmpty(requiredModule))
            {
                _logger.Warn($"ReadinessRequired attribute found but no module specified for {context.Request.Path}");
                return null;
            }

            var moduleState = _readinessTracker.GetComponentState(requiredModule);
            
            if (moduleState != ReadinessState.Ready)
            {
                if (attr.WaitForReady)
                {
                    try
                    {
                        var timeout = TimeSpan.FromSeconds(attr.TimeoutSeconds);
                        await _readinessTracker.WaitForComponentAsync(requiredModule, timeout);
                    }
                    catch (TimeoutException)
                    {
                        return CreateNotReadyResponse(moduleState, attr.Message, requiredModule);
                    }
                }
                else
                {
                    return CreateNotReadyResponse(moduleState, attr.Message, requiredModule);
                }
            }

            return null;
        }

        private async Task HandleDegradedMode(HttpContext context, DegradedWhenNotReadyAttribute attr)
        {
            var module = attr.Module;
            
            // If no module specified, try to infer from path
            if (string.IsNullOrEmpty(module))
            {
                module = InferModuleFromPath(context.Request.Path);
            }

            if (!string.IsNullOrEmpty(module))
            {
                var moduleState = _readinessTracker.GetComponentState(module);
                if (moduleState != ReadinessState.Ready)
                {
                    // Add degraded mode headers
                    context.Response.Headers.Add("X-Degraded-Mode", "true");
                    context.Response.Headers.Add("X-Degraded-Reason", attr.DegradedMessage);
                    context.Response.Headers.Add("X-Degraded-Module", module);
                }
            }
        }

        private (int? StatusCode, object Value) CreateNotReadyResponse(ReadinessState moduleState, string customMessage, string moduleName)
        {
            var message = !string.IsNullOrEmpty(customMessage) 
                ? customMessage 
                : $"Module '{moduleName}' is not ready (state: {moduleState})";

            return (503, new
            {
                error = "Service Unavailable",
                message = message,
                module = moduleName,
                state = moduleState.ToString(),
                retryAfter = 5,
                eventsEndpoint = "/readiness/events"
            });
        }

        /// <summary>
        /// Infer the module name from the request path using comprehensive mapping
        /// This is the most generic way to map API endpoints to modules
        /// </summary>
        private string InferModuleFromPath(PathString path)
        {
            var pathValue = path.Value?.ToLowerInvariant() ?? "";
            
            // Handle API endpoints
            if (pathValue.StartsWith("/api/"))
            {
                var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2)
                {
                    var apiPrefix = segments[1];
                    
                    // Map common API prefixes to module names
                    return apiPrefix switch
                    {
                        "news" => "RealtimeNewsStreamModule",
                        "files" => "FileSystemModule",
                        "ai" => "AIModule",
                        "ontology" => "OntologyModule",
                        "concept" => "ConceptModule",
                        "gallery" => "GalleryModule",
                        "user" => "UserConceptModule",
                        "storage" => "StorageModule",
                        "performance" => "PerformanceModule",
                        "health" => "CoreModule",
                        "readiness" => "CoreModule",
                        "spec" => "CoreModule",
                        "swagger" => "CoreModule",
                        "auth" => "IdentityModule",
                        "identity" => "IdentityModule",
                        _ => $"{char.ToUpper(apiPrefix[0])}{apiPrefix[1..]}Module"
                    };
                }
            }
            
            // Handle other common patterns
            if (pathValue.StartsWith("/swagger") || pathValue.StartsWith("/docs"))
                return "CoreModule";
            if (pathValue.StartsWith("/health"))
                return "CoreModule";
            if (pathValue.StartsWith("/readiness"))
                return "CoreModule";
            if (pathValue.StartsWith("/spec"))
                return "CoreModule";
            if (pathValue.StartsWith("/auth"))
                return "IdentityModule";
            if (pathValue.StartsWith("/identity"))
                return "IdentityModule";
            
            return string.Empty;
        }

        private bool HasAttribute<T>(Microsoft.AspNetCore.Http.Endpoint endpoint) where T : Attribute
        {
            return GetAttribute<T>(endpoint) != null;
        }

        private T? GetAttribute<T>(Microsoft.AspNetCore.Http.Endpoint endpoint) where T : Attribute
        {
            // Check method attributes
            if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is ControllerActionDescriptor actionDescriptor)
            {
                var methodInfo = actionDescriptor.MethodInfo;
                var attr = methodInfo.GetCustomAttribute<T>();
                if (attr != null) return attr;

                // Check class attributes
                var controllerType = actionDescriptor.ControllerTypeInfo;
                return controllerType.GetCustomAttribute<T>();
            }

            return null;
        }
    }
}
