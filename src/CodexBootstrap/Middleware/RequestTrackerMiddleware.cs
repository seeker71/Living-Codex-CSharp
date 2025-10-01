using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Middleware;

/// <summary>
/// Middleware to track all concurrent requests and log slow requests
/// </summary>
public class RequestTrackerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICodexLogger _logger;
    private static readonly ConcurrentDictionary<string, RequestInfo> _activeRequests = new();
    private static readonly string _logFilePath = Path.Combine("logs", "request-tracker.log");
    private static long _requestIdCounter = 0;

    public RequestTrackerMiddleware(RequestDelegate next, ICodexLogger logger)
    {
        _next = next;
        _logger = logger;
        
        // Ensure logs directory exists
        Directory.CreateDirectory("logs");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Interlocked.Increment(ref _requestIdCounter).ToString();
        var startTime = Stopwatch.GetTimestamp();
        var requestInfo = new RequestInfo
        {
            RequestId = requestId,
            Method = context.Request.Method,
            Path = context.Request.Path,
            StartTime = DateTime.UtcNow,
            StartTimestamp = startTime
        };

        _activeRequests[requestId] = requestInfo;
        
        // Log request start
        await LogRequestAsync($"START [{requestId}] {context.Request.Method} {context.Request.Path} at {requestInfo.StartTime:HH:mm:ss.fff}");

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = GetElapsedMilliseconds(startTime);
            _activeRequests.TryRemove(requestId, out _);
            
            // Log completion
            var statusCode = context.Response.StatusCode;
            await LogRequestAsync($"END   [{requestId}] {context.Request.Method} {context.Request.Path} - {statusCode} - {elapsed:F2}ms");
            
            // Log slow requests to console
            if (elapsed > 1000)
            {
                _logger.Warn($"SLOW REQUEST: [{requestId}] {context.Request.Method} {context.Request.Path} took {elapsed:F2}ms");
            }
        }
    }

    private static double GetElapsedMilliseconds(long startTimestamp)
    {
        var elapsed = Stopwatch.GetTimestamp() - startTimestamp;
        return (elapsed * 1000.0) / Stopwatch.Frequency;
    }

    private static async Task LogRequestAsync(string message)
    {
        try
        {
            var logMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} - {message}\n";
            await File.AppendAllTextAsync(_logFilePath, logMessage);
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public static IEnumerable<RequestInfo> GetActiveRequests()
    {
        return _activeRequests.Values.ToList();
    }

    public static string GetActiveRequestsSummary()
    {
        var active = _activeRequests.Values.OrderBy(r => r.StartTime).ToList();
        if (active.Count == 0)
        {
            return "No active requests";
        }

        var now = DateTime.UtcNow;
        var summary = new List<string>();
        foreach (var req in active)
        {
            var duration = (now - req.StartTime).TotalMilliseconds;
            var status = duration > 5000 ? "🔴 STUCK" : duration > 1000 ? "⚠️ SLOW" : "✅ OK";
            summary.Add($"{status} [{req.RequestId}] {req.Method} {req.Path} - {duration:F0}ms");
        }

        return string.Join("\n", summary);
    }
}

public class RequestInfo
{
    public string RequestId { get; set; } = "";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime StartTime { get; set; }
    public long StartTimestamp { get; set; }
}

