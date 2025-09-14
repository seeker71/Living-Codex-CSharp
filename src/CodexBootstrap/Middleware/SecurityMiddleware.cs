using System;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace CodexBootstrap.Middleware
{
    /// <summary>
    /// Middleware for handling security concerns including authentication and input validation
    /// </summary>
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CodexBootstrap.Core.ICodexLogger _logger;
        private readonly IInputValidator _inputValidator;
        private readonly CodexBootstrap.Core.Security.IAuthenticationService _authenticationService;

        // Endpoints that don't require authentication
        private readonly string[] _publicEndpoints = {
            "/health",
            "/hello",
            "/api/discovery",
            "/spec/routes/all",
            "/auth/login",
            "/auth/register"
        };

        public SecurityMiddleware(RequestDelegate next, CodexBootstrap.Core.ICodexLogger logger, IInputValidator inputValidator, CodexBootstrap.Core.Security.IAuthenticationService authenticationService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Log the incoming request
                _logger.Info($"Incoming request: {context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}");

                // Validate request size
                if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
                {
                    await WriteErrorResponse(context, 413, "Request too large");
                    return;
                }

                // Check for suspicious patterns in the request
                if (IsSuspiciousRequest(context))
                {
                    _logger.Warn($"Suspicious request detected from {context.Connection.RemoteIpAddress}: {context.Request.Method} {context.Request.Path}");
                    await WriteErrorResponse(context, 400, "Invalid request");
                    return;
                }

                // Validate input parameters
                var validationResult = await ValidateRequestInput(context);
                if (!validationResult.IsValid)
                {
                    await WriteErrorResponse(context, 400, validationResult.ErrorMessage);
                    return;
                }

                // Check authentication for protected endpoints
                if (RequiresAuthentication(context.Request.Path))
                {
                    var authResult = await ValidateAuthentication(context);
                    if (!authResult.IsSuccess)
                    {
                        await WriteErrorResponse(context, 401, authResult.ErrorMessage);
                        return;
                    }

                    // Add user information to context
                    context.Items["User"] = authResult.User;
                }

                // Add security headers
                AddSecurityHeaders(context);

                // Continue to the next middleware
                await _next(context);

                // Log the response
                _logger.Info($"Request completed: {context.Request.Method} {context.Request.Path} - Status: {context.Response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Security middleware error: {ex.Message}", ex);
                await WriteErrorResponse(context, 500, "Internal server error");
            }
        }

        /// <summary>
        /// Checks if the request requires authentication
        /// </summary>
        private bool RequiresAuthentication(string path)
        {
            return !_publicEndpoints.Any(endpoint => path.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates authentication for protected endpoints
        /// </summary>
        private async Task<CodexBootstrap.Core.Security.AuthenticationResult> ValidateAuthentication(HttpContext context)
        {
            // Check for Authorization header
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                return CodexBootstrap.Core.Security.AuthenticationResult.Failure("Authorization header is required");
            }

            var authValue = authHeader.FirstOrDefault();
            if (string.IsNullOrEmpty(authValue))
            {
                return CodexBootstrap.Core.Security.AuthenticationResult.Failure("Authorization header cannot be empty");
            }

            // Extract token from "Bearer <token>" format
            if (!authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return CodexBootstrap.Core.Security.AuthenticationResult.Failure("Invalid authorization format. Expected 'Bearer <token>'");
            }

            var token = authValue.Substring(7).Trim();
            if (string.IsNullOrEmpty(token))
            {
                return CodexBootstrap.Core.Security.AuthenticationResult.Failure("Token cannot be empty");
            }

            return await _authenticationService.ValidateTokenAsync(token);
        }

        /// <summary>
        /// Validates input parameters in the request
        /// </summary>
        private async Task<CodexBootstrap.Core.Security.ValidationResult> ValidateRequestInput(HttpContext context)
        {
            var validationResults = new List<CodexBootstrap.Core.Security.ValidationResult>();

            // Validate query parameters
            foreach (var param in context.Request.Query)
            {
                var result = _inputValidator.ValidateString(param.Value.ToString(), param.Key, 1000);
                if (!result.IsValid)
                {
                    validationResults.Add(result);
                }
            }

            // Validate path parameters
            var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments != null)
            {
                foreach (var segment in pathSegments)
                {
                    var result = _inputValidator.ValidateString(segment, "PathSegment", 200);
                    if (!result.IsValid)
                    {
                        validationResults.Add(result);
                    }
                }
            }

            // Validate request body for POST/PUT requests
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                if (context.Request.ContentType?.Contains("application/json") == true)
                {
                    // Read and validate JSON body
                    context.Request.EnableBuffering();
                    var body = await new System.IO.StreamReader(context.Request.Body).ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrEmpty(body))
                    {
                        var jsonResult = _inputValidator.ValidateJson(body, "Request body");
                        if (!jsonResult.IsValid)
                        {
                            validationResults.Add(jsonResult);
                        }
                    }
                }
            }

            return validationResults.Any() 
                ? CodexBootstrap.Core.Security.ValidationResult.Error(string.Join("; ", validationResults.Select(r => r.ErrorMessage)))
                : CodexBootstrap.Core.Security.ValidationResult.Success;
        }

        /// <summary>
        /// Checks for suspicious patterns in the request
        /// </summary>
        private bool IsSuspiciousRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var query = context.Request.QueryString.Value?.ToLowerInvariant() ?? "";

            // Check for common attack patterns
            var suspiciousPatterns = new[]
            {
                "..", "script", "javascript", "vbscript", "onload", "onerror",
                "union select", "drop table", "delete from", "insert into",
                "exec(", "xp_", "sp_", "waitfor delay", "benchmark("
            };

            var fullRequest = $"{path} {query}";
            return suspiciousPatterns.Any(pattern => fullRequest.Contains(pattern));
        }

        /// <summary>
        /// Adds security headers to the response
        /// </summary>
        private void AddSecurityHeaders(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Prevent clickjacking
            headers["X-Frame-Options"] = "DENY";

            // Prevent MIME type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Enable XSS protection
            headers["X-XSS-Protection"] = "1; mode=block";

            // Strict Transport Security (HTTPS only)
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }

            // Content Security Policy
            headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; frame-ancestors 'none';";

            // Referrer Policy
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions Policy
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Remove server header
            headers.Remove("Server");
        }

        /// <summary>
        /// Writes an error response to the client
        /// </summary>
        private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                success = false,
                error = message,
                statusCode = statusCode,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
