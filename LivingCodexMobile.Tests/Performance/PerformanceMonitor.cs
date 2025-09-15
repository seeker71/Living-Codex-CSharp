using System.Diagnostics;

namespace LivingCodexMobile.Tests.Performance;

/// <summary>
/// Monitors test performance and provides metrics
/// </summary>
public class PerformanceMonitor
{
    private readonly Stopwatch _stopwatch;
    private readonly List<PerformanceMetric> _metrics;

    public PerformanceMonitor()
    {
        _stopwatch = new Stopwatch();
        _metrics = new List<PerformanceMetric>();
    }

    /// <summary>
    /// Starts monitoring a test
    /// </summary>
    public void StartTest(string testName)
    {
        _stopwatch.Restart();
        Console.WriteLine($"🧪 Starting test: {testName}");
    }

    /// <summary>
    /// Stops monitoring a test and records the metric
    /// </summary>
    public void StopTest(string testName, bool passed = true)
    {
        _stopwatch.Stop();
        var metric = new PerformanceMetric
        {
            TestName = testName,
            Duration = _stopwatch.Elapsed,
            Passed = passed,
            Timestamp = DateTime.UtcNow
        };
        _metrics.Add(metric);

        var status = passed ? "✅" : "❌";
        var duration = _stopwatch.Elapsed.TotalMilliseconds;
        Console.WriteLine($"{status} Test completed: {testName} ({duration:F2}ms)");
    }

    /// <summary>
    /// Gets performance summary
    /// </summary>
    public PerformanceSummary GetSummary()
    {
        if (!_metrics.Any())
            return new PerformanceSummary();

        var totalTests = _metrics.Count;
        var passedTests = _metrics.Count(m => m.Passed);
        var failedTests = totalTests - passedTests;
        var totalDuration = _metrics.Sum(m => m.Duration.TotalMilliseconds);
        var averageDuration = totalDuration / totalTests;
        var slowestTest = _metrics.OrderByDescending(m => m.Duration).First();
        var fastestTest = _metrics.OrderBy(m => m.Duration).First();

        return new PerformanceSummary
        {
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            SuccessRate = (double)passedTests / totalTests * 100,
            TotalDuration = TimeSpan.FromMilliseconds(totalDuration),
            AverageDuration = TimeSpan.FromMilliseconds(averageDuration),
            SlowestTest = slowestTest,
            FastestTest = fastestTest
        };
    }

    /// <summary>
    /// Prints performance summary to console
    /// </summary>
    public void PrintSummary()
    {
        var summary = GetSummary();
        
        Console.WriteLine();
        Console.WriteLine("📊 Performance Summary");
        Console.WriteLine("=====================");
        Console.WriteLine($"Total Tests: {summary.TotalTests}");
        Console.WriteLine($"Passed: {summary.PassedTests} ({summary.SuccessRate:F1}%)");
        Console.WriteLine($"Failed: {summary.FailedTests}");
        Console.WriteLine($"Total Duration: {summary.TotalDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Average Duration: {summary.AverageDuration.TotalMilliseconds:F2}ms");
        
        if (summary.SlowestTest != null)
        {
            Console.WriteLine($"Slowest Test: {summary.SlowestTest.TestName} ({summary.SlowestTest.Duration.TotalMilliseconds:F2}ms)");
        }
        
        if (summary.FastestTest != null)
        {
            Console.WriteLine($"Fastest Test: {summary.FastestTest.TestName} ({summary.FastestTest.Duration.TotalMilliseconds:F2}ms)");
        }
        
        Console.WriteLine();
    }

    /// <summary>
    /// Gets slow tests (above threshold)
    /// </summary>
    public List<PerformanceMetric> GetSlowTests(double thresholdMs = 1000)
    {
        return _metrics.Where(m => m.Duration.TotalMilliseconds > thresholdMs).ToList();
    }

    /// <summary>
    /// Gets failed tests
    /// </summary>
    public List<PerformanceMetric> GetFailedTests()
    {
        return _metrics.Where(m => !m.Passed).ToList();
    }
}

/// <summary>
/// Represents a performance metric for a single test
/// </summary>
public class PerformanceMetric
{
    public string TestName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Passed { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a performance summary for all tests
/// </summary>
public class PerformanceSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public PerformanceMetric? SlowestTest { get; set; }
    public PerformanceMetric? FastestTest { get; set; }
}
