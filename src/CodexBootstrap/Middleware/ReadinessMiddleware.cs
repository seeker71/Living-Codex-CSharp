using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Middleware
{
    /// <summary>
    /// Middleware that checks endpoint readiness before processing requests
    /// </summary>
    public class ReadinessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ReadinessTracker _readinessTracker;
        private readonly ICodexLogger _logger;

        public ReadinessMiddleware(RequestDelegate next, ReadinessTracker readinessTracker, ICodexLogger logger)
        {
            _next = next;
            _readinessTracker = readinessTracker;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip readiness checks for readiness endpoints themselves
            if (context.Request.Path.StartsWithSegments("/readiness"))
            {
                await _next(context);
                return;
            }

            // Skip readiness checks for health endpoints
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            // Skip readiness checks for swagger/docs
            if (context.Request.Path.StartsWithSegments("/swagger") || 
                context.Request.Path.StartsWithSegments("/docs"))
            {
                await _next(context);
                return;
            }

            // Check if the endpoint is ready (if endpoint tracking is enabled)
            var endpoint = GetEndpointFromPath(context.Request.Path);
            var endpointState = _readinessTracker.GetComponentState(endpoint);

            // Only block if endpoint is explicitly registered and not ready
            // NotStarted means endpoint tracking is not enabled, so skip this check
            if (endpointState != ReadinessState.Ready && endpointState != ReadinessState.NotStarted)
            {
                await HandleNotReadyEndpoint(context, endpoint, endpointState);
                return;
            }

            // Check if the module providing this endpoint is ready
            var module = GetModuleFromPath(context.Request.Path);
            if (!string.IsNullOrEmpty(module))
            {
                var moduleState = _readinessTracker.GetComponentState(module);
                if (moduleState != ReadinessState.Ready)
                {
                    await HandleNotReadyModule(context, module, moduleState);
                    return;
                }
            }

            await _next(context);
        }

        private async Task HandleNotReadyEndpoint(HttpContext context, string endpoint, ReadinessState state)
        {
            var component = _readinessTracker.GetComponentReadiness(endpoint);
            var message = GetNotReadyMessage(state, component?.LastResult?.Message);

            context.Response.StatusCode = 503; // Service Unavailable
            context.Response.Headers.Add("Retry-After", "5"); // Retry after 5 seconds
            context.Response.Headers.Add("X-Readiness-State", state.ToString());
            context.Response.Headers.Add("X-Readiness-Message", message);
            context.Response.Headers.Add("X-Readiness-Endpoint", "/readiness/events");

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Service Unavailable",
                message = message,
                endpoint = endpoint,
                state = state.ToString(),
                retryAfter = 5,
                eventsEndpoint = "/readiness/events"
            }));
        }

        private async Task HandleNotReadyModule(HttpContext context, string module, ReadinessState state)
        {
            var component = _readinessTracker.GetComponentReadiness(module);
            var message = $"Module '{module}' is not ready: {GetNotReadyMessage(state, component?.LastResult?.Message)}";

            context.Response.StatusCode = 503; // Service Unavailable
            context.Response.Headers.Add("Retry-After", "5"); // Retry after 5 seconds
            context.Response.Headers.Add("X-Readiness-State", state.ToString());
            context.Response.Headers.Add("X-Readiness-Message", message);
            context.Response.Headers.Add("X-Readiness-Module", module);
            context.Response.Headers.Add("X-Readiness-Endpoint", "/readiness/events");

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Service Unavailable",
                message = message,
                module = module,
                state = state.ToString(),
                retryAfter = 5,
                eventsEndpoint = "/readiness/events"
            }));
        }

        private string GetNotReadyMessage(ReadinessState state, string? componentMessage)
        {
            return state switch
            {
                ReadinessState.NotStarted => "Component has not started initialization",
                ReadinessState.Initializing => componentMessage ?? "Component is currently initializing",
                ReadinessState.Failed => componentMessage ?? "Component initialization failed",
                ReadinessState.Degraded => componentMessage ?? "Component is in degraded state",
                _ => "Component is not ready"
            };
        }

        private string GetEndpointFromPath(PathString path)
        {
            // Convert path to endpoint identifier
            // e.g., "/api/news/latest" -> "api/news/latest"
            var pathValue = path.Value?.TrimStart('/') ?? "";
            
            // Handle root path
            if (string.IsNullOrEmpty(pathValue))
            {
                return "root";
            }

            return pathValue;
        }

        private string? GetModuleFromPath(PathString path)
        {
            // Generic endpoint-to-module mapping based on path patterns
            // This can be extended or made configurable
            var pathValue = path.Value?.ToLowerInvariant() ?? "";
            
            // Check for common API patterns
            if (pathValue.StartsWith("/api/"))
            {
                var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2)
                {
                    var apiPrefix = segments[1]; // e.g., "news", "files", "ai"
                    
                    // Map API prefixes to likely module names
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
                        _ => $"Api{char.ToUpper(apiPrefix[0])}{apiPrefix[1..]}Module" // Generic fallback
                    };
                }
            }
            
            // Check for other patterns
            if (pathValue.StartsWith("/swagger") || pathValue.StartsWith("/docs"))
                return "OpenApiModule";
            if (pathValue.StartsWith("/health"))
                return "CoreModule";
            if (pathValue.StartsWith("/readiness"))
                return "CoreModule";
            if (pathValue.StartsWith("/auth"))
                return "IdentityModule";
            if (pathValue.StartsWith("/identity"))
                return "IdentityModule";
            
            return null; // Unknown module
        }
    }
}
