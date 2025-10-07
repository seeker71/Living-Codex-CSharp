using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Middleware;

public sealed class PerformanceProfilingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICodexLogger _logger;
    private readonly PerformanceMetrics _metrics;
    private readonly Timer _metricsReportingTimer;

    public PerformanceProfilingMiddleware(RequestDelegate next, ICodexLogger logger)
    {
        _next = next;
        _logger = logger;
        _metrics = new PerformanceMetrics();

        _metricsReportingTimer = new Timer(
            ReportMetrics,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5)
        );

        _logger.Info("[PerformanceProfiling] Middleware initialized");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var endpointKey = $"{method} {path}";

        if (ShouldSkipProfiling(path))
        {
            await _next(context);
            return;
        }

        try
        {
            _metrics.IncrementRequestCount(endpointKey);
            await _next(context);
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            _metrics.RecordResponseTime(endpointKey, duration);
            _metrics.RecordStatusCode(endpointKey, context.Response.StatusCode);

            if (duration > 2000)
            {
                _logger.Warn($"[PerformanceProfiling] Slow request: {endpointKey} took {duration}ms");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.RecordError(endpointKey, ex);
            _logger.Error($"[PerformanceProfiling] Error in request: {endpointKey}", ex);
            throw;
        }
    }

    private bool ShouldSkipProfiling(string path)
    {
        var skipPatterns = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico",
            "/robots.txt"
        };

        return skipPatterns.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private void ReportMetrics(object? state)
    {
        try
        {
            var metrics = _metrics.GetMetricsSummary();
            if (metrics.TotalRequests > 0)
            {
                _logger.Info($"[PerformanceProfiling] Total Requests: {metrics.TotalRequests}, Avg: {metrics.AverageResponseTime:F1}ms, P95: {metrics.P95ResponseTime}ms, ErrorRate: {metrics.ErrorRate:P2}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("[PerformanceProfiling] Error reporting metrics", ex);
        }
    }

    public void Dispose()
    {
        _metricsReportingTimer?.Dispose();
        _logger.Info("[PerformanceProfiling] Middleware disposed");
    }
}

public sealed class PerformanceMetrics
{
    private readonly ConcurrentDictionary<string, EndpointMetrics> _endpointMetrics = new();
    private readonly ConcurrentDictionary<string, int> _requestCounts = new();
    private readonly ConcurrentDictionary<string, int> _errorCounts = new();
    private readonly ConcurrentDictionary<string, List<long>> _responseTimes = new();
    private readonly ConcurrentDictionary<string, Dictionary<int, int>> _statusCodes = new();

    private const int MAX_RESPONSE_TIME_SAMPLES = 1000;

    public void IncrementRequestCount(string endpoint)
    {
        _requestCounts.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
    }

    public void RecordResponseTime(string endpoint, long milliseconds)
    {
        _responseTimes.AddOrUpdate(
            endpoint,
            new List<long> { milliseconds },
            (_, times) =>
            {
                lock (times)
                {
                    times.Add(milliseconds);
                    if (times.Count > MAX_RESPONSE_TIME_SAMPLES)
                    {
                        times.RemoveAt(0);
                    }
                }
                return times;
            });
    }

    public void RecordStatusCode(string endpoint, int statusCode)
    {
        _statusCodes.AddOrUpdate(
            endpoint,
            new Dictionary<int, int> { [statusCode] = 1 },
            (_, codes) =>
            {
                codes[statusCode] = codes.TryGetValue(statusCode, out var c) ? c + 1 : 1;
                return codes;
            });
    }

    public void RecordError(string endpoint, Exception exception)
    {
        _errorCounts.AddOrUpdate(endpoint, 1, (_, count) => count + 1);
        _statusCodes.AddOrUpdate(
            endpoint,
            new Dictionary<int, int> { [500] = 1 },
            (_, codes) =>
            {
                codes[500] = codes.TryGetValue(500, out var c) ? c + 1 : 1;
                return codes;
            });
    }

    public PerformanceMetricsSummary GetMetricsSummary()
    {
        var endpointMetrics = new List<EndpointMetricsSummary>();
        var totalRequests = 0;
        var totalErrors = 0;
        var allResponseTimes = new List<long>();

        foreach (var endpoint in _requestCounts.Keys)
        {
            var requestCount = _requestCounts.GetValueOrDefault(endpoint, 0);
            var errorCount = _errorCounts.GetValueOrDefault(endpoint, 0);

            totalRequests += requestCount;
            totalErrors += errorCount;

            if (_responseTimes.TryGetValue(endpoint, out var responseTimes))
            {
                lock (responseTimes)
                {
                    if (responseTimes.Count > 0)
                    {
                        var times = responseTimes.ToArray();
                        allResponseTimes.AddRange(times);

                        var avgResponseTime = times.Average();
                        var p95ResponseTime = CalculatePercentile(times, 95);
                        var p99ResponseTime = CalculatePercentile(times, 99);

                        endpointMetrics.Add(new EndpointMetricsSummary(
                            endpoint,
                            requestCount,
                            errorCount,
                            avgResponseTime,
                            p95ResponseTime,
                            p99ResponseTime,
                            requestCount > 0 ? (double)errorCount / requestCount : 0
                        ));
                    }
                }
            }
        }

        var averageResponseTime = allResponseTimes.Count > 0 ? allResponseTimes.Average() : 0;
        var overallP95 = allResponseTimes.Count > 0 ? CalculatePercentile(allResponseTimes.ToArray(), 95) : 0;
        var overallP99 = allResponseTimes.Count > 0 ? CalculatePercentile(allResponseTimes.ToArray(), 99) : 0;
        var errorRate = totalRequests > 0 ? (double)totalErrors / totalRequests : 0;

        return new PerformanceMetricsSummary(
            DateTime.UtcNow,
            totalRequests,
            totalErrors,
            errorRate,
            averageResponseTime,
            overallP95,
            overallP99,
            endpointMetrics
        );
    }

    private static long CalculatePercentile(long[] values, int percentile)
    {
        if (values.Length == 0) return 0;
        Array.Sort(values);
        var index = (int)Math.Ceiling((percentile / 100.0) * values.Length) - 1;
        return values[Math.Max(0, Math.Min(index, values.Length - 1))];
    }
}

public record PerformanceMetricsSummary(
    DateTime Timestamp,
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    double AverageResponseTime,
    long P95ResponseTime,
    long P99ResponseTime,
    List<EndpointMetricsSummary> EndpointMetrics
);

public record EndpointMetricsSummary(
    string Endpoint,
    long RequestCount,
    long ErrorCount,
    double AverageResponseTime,
    long P95ResponseTime,
    long P99ResponseTime,
    double ErrorRate
);

public class EndpointMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public long RequestCount { get; set; }
    public long ErrorCount { get; set; }
    public List<long> ResponseTimes { get; set; } = new();
    public Dictionary<int, int> StatusCodes { get; set; } = new();
}

