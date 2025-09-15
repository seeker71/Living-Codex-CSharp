using System.Collections.Concurrent;

namespace CodexBootstrap.Runtime;

public sealed class ErrorMetrics
{
    private long _totalErrors;
    private readonly ConcurrentDictionary<string, long> _errorsByRoute = new();

    public void Increment(string routeKey)
    {
        System.Threading.Interlocked.Increment(ref _totalErrors);
        _errorsByRoute.AddOrUpdate(routeKey, 1, (_, current) => current + 1);
    }

    public ErrorMetricsSnapshot GetSnapshot()
    {
        return new ErrorMetricsSnapshot(_totalErrors, _errorsByRoute.ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}

public record ErrorMetricsSnapshot(long TotalErrors, Dictionary<string, long> ErrorsByRoute);
