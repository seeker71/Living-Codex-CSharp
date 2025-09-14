using CodexBootstrap.Core;

namespace CodexBootstrap.Middleware;

/// <summary>
/// Middleware for performance monitoring and request profiling
/// </summary>
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PerformanceProfiler _profiler;
    private readonly CodexBootstrap.Core.ICodexLogger _logger;

    public PerformanceMiddleware(RequestDelegate next, PerformanceProfiler profiler, CodexBootstrap.Core.ICodexLogger logger)
    {
        _next = next;
        _profiler = profiler;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var operationName = GetOperationName(context);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var success = context.Response.StatusCode < 400;
            _profiler.RecordOperation(operationName, stopwatch.ElapsedMilliseconds, success);

            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000) // More than 1 second
            {
                _logger.Warn($"Slow request detected: {operationName} took {stopwatch.ElapsedMilliseconds}ms");
            }

            // Log very slow requests
            if (stopwatch.ElapsedMilliseconds > 5000) // More than 5 seconds
            {
                _logger.Error($"Very slow request detected: {operationName} took {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }

    private static string GetOperationName(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // Simplify path for better grouping
        var simplifiedPath = SimplifyPath(path);
        
        return $"{method} {simplifiedPath}";
    }

    private static string SimplifyPath(string path)
    {
        // Replace GUIDs and IDs with placeholders for better grouping
        var simplified = System.Text.RegularExpressions.Regex.Replace(path, @"/[0-9a-fA-F-]{36}", "/{id}");
        simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"/\d+", "/{id}");
        
        return simplified;
    }
}
