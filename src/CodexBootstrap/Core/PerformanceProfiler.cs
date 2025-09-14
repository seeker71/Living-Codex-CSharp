using System.Diagnostics;
using System.Collections.Concurrent;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Performance profiler for monitoring critical paths and identifying bottlenecks
/// </summary>
public class PerformanceProfiler
{
    private readonly ICodexLogger _logger;
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;
    private readonly ConcurrentDictionary<string, List<long>> _responseTimes;
    private readonly Timer _reportingTimer;

    public PerformanceProfiler(ICodexLogger logger)
    {
        _logger = logger;
        _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
        _responseTimes = new ConcurrentDictionary<string, List<long>>();
        
        // Report performance metrics every 30 seconds
        _reportingTimer = new Timer(ReportMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public IDisposable StartProfiling(string operationName)
    {
        return new ProfilingScope(this, operationName);
    }

    public void RecordOperation(string operationName, long durationMs, bool success = true)
    {
        _metrics.AddOrUpdate(operationName, 
            new PerformanceMetrics { OperationName = operationName, CallCount = 1, TotalDurationMs = durationMs, SuccessCount = success ? 1 : 0 },
            (key, existing) => new PerformanceMetrics
            {
                OperationName = operationName,
                CallCount = existing.CallCount + 1,
                TotalDurationMs = existing.TotalDurationMs + durationMs,
                SuccessCount = existing.SuccessCount + (success ? 1 : 0),
                MinDurationMs = Math.Min(existing.MinDurationMs, durationMs),
                MaxDurationMs = Math.Max(existing.MaxDurationMs, durationMs)
            });

        // Track response times for percentile calculations
        _responseTimes.AddOrUpdate(operationName,
            new List<long> { durationMs },
            (key, existing) =>
            {
                lock (existing)
                {
                    existing.Add(durationMs);
                    // Keep only last 1000 measurements to prevent memory growth
                    if (existing.Count > 1000)
                    {
                        existing.RemoveRange(0, existing.Count - 1000);
                    }
                }
                return existing;
            });
    }

    public PerformanceMetrics GetMetrics(string operationName)
    {
        return _metrics.TryGetValue(operationName, out var metrics) ? metrics : new PerformanceMetrics { OperationName = operationName };
    }

    public Dictionary<string, PerformanceMetrics> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public List<PerformanceMetrics> GetTopSlowOperations(int count = 10)
    {
        return _metrics.Values
            .OrderByDescending(m => m.AverageDurationMs)
            .Take(count)
            .ToList();
    }

    public List<PerformanceMetrics> GetTopFrequentOperations(int count = 10)
    {
        return _metrics.Values
            .OrderByDescending(m => m.CallCount)
            .Take(count)
            .ToList();
    }

    public List<PerformanceMetrics> GetOperationsWithErrors(int count = 10)
    {
        return _metrics.Values
            .Where(m => m.ErrorRate > 0)
            .OrderByDescending(m => m.ErrorRate)
            .Take(count)
            .ToList();
    }

    private void ReportMetrics(object? state)
    {
        try
        {
            var allMetrics = GetAllMetrics();
            if (!allMetrics.Any()) return;

            _logger.Info("=== Performance Metrics Report ===");
            
            var topSlow = GetTopSlowOperations(5);
            _logger.Info("Top 5 Slowest Operations:");
            foreach (var metric in topSlow)
            {
                _logger.Info($"  {metric.OperationName}: {metric.AverageDurationMs:F2}ms avg, {metric.CallCount} calls, {metric.ErrorRate:P1} error rate");
            }

            var topFrequent = GetTopFrequentOperations(5);
            _logger.Info("Top 5 Most Frequent Operations:");
            foreach (var metric in topFrequent)
            {
                _logger.Info($"  {metric.OperationName}: {metric.CallCount} calls, {metric.AverageDurationMs:F2}ms avg");
            }

            var withErrors = GetOperationsWithErrors(5);
            if (withErrors.Any())
            {
                _logger.Warn("Operations with Errors:");
                foreach (var metric in withErrors)
                {
                    _logger.Warn($"  {metric.OperationName}: {metric.ErrorRate:P1} error rate ({metric.CallCount - metric.SuccessCount}/{metric.CallCount})");
                }
            }

            _logger.Info($"Total Operations Monitored: {allMetrics.Count}");
            _logger.Info("=== End Performance Report ===");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error reporting performance metrics: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _reportingTimer?.Dispose();
    }

    private class ProfilingScope : IDisposable
    {
        private readonly PerformanceProfiler _profiler;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public ProfilingScope(PerformanceProfiler profiler, string operationName)
        {
            _profiler = profiler;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _profiler.RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
                _disposed = true;
            }
        }
    }
}

public class PerformanceMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public long CallCount { get; set; }
    public long TotalDurationMs { get; set; }
    public long SuccessCount { get; set; }
    public long MinDurationMs { get; set; } = long.MaxValue;
    public long MaxDurationMs { get; set; }
    
    public double AverageDurationMs => CallCount > 0 ? (double)TotalDurationMs / CallCount : 0;
    public double ErrorRate => CallCount > 0 ? (double)(CallCount - SuccessCount) / CallCount : 0;
    public long ErrorCount => CallCount - SuccessCount;
}
