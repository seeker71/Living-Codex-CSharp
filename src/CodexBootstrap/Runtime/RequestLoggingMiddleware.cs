using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICodexLogger logger, HealthService health, ErrorMetrics errors)
    {
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.HasValue ? context.Request.Path.Value ?? "/" : "/";
        var routeKey = method + " " + path;

        health.IncrementRequestCount();

        try
        {
            await _next(context);
            sw.Stop();
            logger.Info($"request {{ method: '{method}', path: '{path}', status: {context.Response.StatusCode}, ms: {sw.ElapsedMilliseconds} }}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            errors.Increment(routeKey);
            logger.Error($"request_error {{ method: '{method}', path: '{path}', ms: {sw.ElapsedMilliseconds}, error: '{ex.Message}' }}", ex);
            throw;
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
