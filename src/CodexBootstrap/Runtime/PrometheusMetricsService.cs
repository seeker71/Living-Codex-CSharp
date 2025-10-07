using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Runtime;

/// <summary>
/// Prometheus metrics service for compassionate system monitoring
/// Embodies the principle of mindful observation - gathering insights with care and wisdom
/// </summary>
public sealed class PrometheusMetricsService
{
    private readonly ICodexLogger _logger;
    private readonly ConcurrentDictionary<string, double> _metrics = new();
    private readonly ConcurrentDictionary<string, DateTime> _metricTimestamps = new();
    
    // Metric names - embodying clear, meaningful naming
    private const string METRIC_ACTIVE_SESSIONS = "codex_active_sessions_total";
    private const string METRIC_REVOKED_TOKENS = "codex_revoked_tokens_total";
    private const string METRIC_MEMORY_USAGE_BYTES = "codex_memory_usage_bytes";
    private const string METRIC_MEMORY_PRESSURE_LEVEL = "codex_memory_pressure_level";
    private const string METRIC_CLEANUP_EVENTS = "codex_cleanup_events_total";
    private const string METRIC_SHUTDOWN_DURATION_SECONDS = "codex_shutdown_duration_seconds";
    private const string METRIC_MODULE_CLEANUP_COUNT = "codex_module_cleanup_count_total";
    private const string METRIC_HEALTH_SCORE = "codex_health_score";
    private const string METRIC_REQUEST_COUNT = "codex_requests_total";
    private const string METRIC_ACTIVE_REQUESTS = "codex_active_requests_current";
    
    // Labels for metric dimensions
    private const string LABEL_MODULE = "module";
    private const string LABEL_PRESSURE_LEVEL = "pressure_level";
    private const string LABEL_STATUS = "status";
    private const string LABEL_METHOD = "method";
    private const string LABEL_ENDPOINT = "endpoint";

    public PrometheusMetricsService(ICodexLogger logger)
    {
        _logger = logger;
        InitializeMetrics();
    }

    /// <summary>
    /// Initialize default metrics with compassionate baseline values
    /// </summary>
    private void InitializeMetrics()
    {
        SetMetric(METRIC_ACTIVE_SESSIONS, 0);
        SetMetric(METRIC_REVOKED_TOKENS, 0);
        SetMetric(METRIC_MEMORY_PRESSURE_LEVEL, 0); // 0 = Low pressure
        SetMetric(METRIC_CLEANUP_EVENTS, 0);
        SetMetric(METRIC_SHUTDOWN_DURATION_SECONDS, 0);
        SetMetric(METRIC_HEALTH_SCORE, 100); // Start with perfect health
        SetMetric(METRIC_REQUEST_COUNT, 0);
        SetMetric(METRIC_ACTIVE_REQUESTS, 0);
        
        _logger.Info("[PrometheusMetrics] Initialized with compassionate baseline metrics");
    }

    /// <summary>
    /// Update active sessions metric - embodying mindful session tracking
    /// </summary>
    public void UpdateActiveSessions(int count)
    {
        SetMetric(METRIC_ACTIVE_SESSIONS, count);
        _logger.Debug($"[PrometheusMetrics] Updated active sessions: {count}");
    }

    /// <summary>
    /// Update revoked tokens metric - embodying secure token management
    /// </summary>
    public void UpdateRevokedTokens(int count)
    {
        SetMetric(METRIC_REVOKED_TOKENS, count);
        _logger.Debug($"[PrometheusMetrics] Updated revoked tokens: {count}");
    }

    /// <summary>
    /// Update memory usage metrics - embodying compassionate resource monitoring
    /// </summary>
    public void UpdateMemoryMetrics(long memoryUsageBytes, MemoryPressureLevel pressureLevel)
    {
        SetMetric(METRIC_MEMORY_USAGE_BYTES, memoryUsageBytes);
        
        var pressureLevelValue = pressureLevel switch
        {
            MemoryPressureLevel.Low => 0,
            MemoryPressureLevel.Normal => 1,
            MemoryPressureLevel.Elevated => 2,
            MemoryPressureLevel.High => 3,
            MemoryPressureLevel.Critical => 4,
            _ => 0
        };
        
        SetMetric(METRIC_MEMORY_PRESSURE_LEVEL, pressureLevelValue);
        _logger.Debug($"[PrometheusMetrics] Updated memory metrics: {memoryUsageBytes} bytes, pressure: {pressureLevel}");
    }

    /// <summary>
    /// Record cleanup event - embodying the principle of regular maintenance
    /// </summary>
    public void RecordCleanupEvent(string moduleName, int itemsCleaned)
    {
        var metricName = $"{METRIC_MODULE_CLEANUP_COUNT}{{module=\"{moduleName}\"}}";
        IncrementMetric(metricName, itemsCleaned);
        IncrementMetric(METRIC_CLEANUP_EVENTS);
        
        _logger.Info($"[PrometheusMetrics] Recorded cleanup event: {moduleName} cleaned {itemsCleaned} items");
    }

    /// <summary>
    /// Record shutdown duration - embodying the principle of graceful completion
    /// </summary>
    public void RecordShutdownDuration(TimeSpan duration)
    {
        SetMetric(METRIC_SHUTDOWN_DURATION_SECONDS, duration.TotalSeconds);
        _logger.Info($"[PrometheusMetrics] Recorded shutdown duration: {duration.TotalSeconds:F2} seconds");
    }

    /// <summary>
    /// Update health score - embodying holistic system wellness
    /// </summary>
    public void UpdateHealthScore(int score)
    {
        SetMetric(METRIC_HEALTH_SCORE, score);
        _logger.Debug($"[PrometheusMetrics] Updated health score: {score}");
    }

    /// <summary>
    /// Update request metrics - embodying mindful traffic monitoring
    /// </summary>
    public void UpdateRequestMetrics(long totalRequests, long activeRequests)
    {
        SetMetric(METRIC_REQUEST_COUNT, totalRequests);
        SetMetric(METRIC_ACTIVE_REQUESTS, activeRequests);
        _logger.Debug($"[PrometheusMetrics] Updated request metrics: {totalRequests} total, {activeRequests} active");
    }

    /// <summary>
    /// Get all metrics in Prometheus format - embodying clear, accessible data
    /// </summary>
    public string GetPrometheusFormat()
    {
        var output = new System.Text.StringBuilder();
        
        // Add help comments - embodying compassionate documentation
        output.AppendLine("# HELP codex_active_sessions_total Current number of active user sessions");
        output.AppendLine("# TYPE codex_active_sessions_total gauge");
        output.AppendLine($"{METRIC_ACTIVE_SESSIONS} {GetMetricValue(METRIC_ACTIVE_SESSIONS)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_revoked_tokens_total Current number of revoked authentication tokens");
        output.AppendLine("# TYPE codex_revoked_tokens_total gauge");
        output.AppendLine($"{METRIC_REVOKED_TOKENS} {GetMetricValue(METRIC_REVOKED_TOKENS)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_memory_usage_bytes Current memory usage in bytes");
        output.AppendLine("# TYPE codex_memory_usage_bytes gauge");
        output.AppendLine($"{METRIC_MEMORY_USAGE_BYTES} {GetMetricValue(METRIC_MEMORY_USAGE_BYTES)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_memory_pressure_level Memory pressure level (0=Low, 1=Normal, 2=Elevated, 3=High, 4=Critical)");
        output.AppendLine("# TYPE codex_memory_pressure_level gauge");
        output.AppendLine($"{METRIC_MEMORY_PRESSURE_LEVEL} {GetMetricValue(METRIC_MEMORY_PRESSURE_LEVEL)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_cleanup_events_total Total number of cleanup events performed");
        output.AppendLine("# TYPE codex_cleanup_events_total counter");
        output.AppendLine($"{METRIC_CLEANUP_EVENTS} {GetMetricValue(METRIC_CLEANUP_EVENTS)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_shutdown_duration_seconds Duration of last graceful shutdown in seconds");
        output.AppendLine("# TYPE codex_shutdown_duration_seconds gauge");
        output.AppendLine($"{METRIC_SHUTDOWN_DURATION_SECONDS} {GetMetricValue(METRIC_SHUTDOWN_DURATION_SECONDS)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_health_score Overall system health score (0-100)");
        output.AppendLine("# TYPE codex_health_score gauge");
        output.AppendLine($"{METRIC_HEALTH_SCORE} {GetMetricValue(METRIC_HEALTH_SCORE)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_requests_total Total number of HTTP requests processed");
        output.AppendLine("# TYPE codex_requests_total counter");
        output.AppendLine($"{METRIC_REQUEST_COUNT} {GetMetricValue(METRIC_REQUEST_COUNT)}");
        output.AppendLine();
        
        output.AppendLine("# HELP codex_active_requests_current Current number of active HTTP requests");
        output.AppendLine("# TYPE codex_active_requests_current gauge");
        output.AppendLine($"{METRIC_ACTIVE_REQUESTS} {GetMetricValue(METRIC_ACTIVE_REQUESTS)}");
        output.AppendLine();
        
        // Add module-specific cleanup metrics
        var moduleMetrics = _metrics.Where(kv => kv.Key.StartsWith(METRIC_MODULE_CLEANUP_COUNT))
                                   .OrderBy(kv => kv.Key);
        
        if (moduleMetrics.Any())
        {
            output.AppendLine("# HELP codex_module_cleanup_count_total Number of items cleaned by each module");
            output.AppendLine("# TYPE codex_module_cleanup_count_total counter");
            
            foreach (var metric in moduleMetrics)
            {
                output.AppendLine($"{metric.Key} {metric.Value}");
            }
            output.AppendLine();
        }
        
        return output.ToString();
    }

    /// <summary>
    /// Get metrics summary for health monitoring - embodying holistic awareness
    /// </summary>
    public MetricsSummary GetMetricsSummary()
    {
        var now = DateTime.UtcNow;
        
        return new MetricsSummary(
            Timestamp: now,
            ActiveSessions: (int)GetMetricValue(METRIC_ACTIVE_SESSIONS),
            RevokedTokens: (int)GetMetricValue(METRIC_REVOKED_TOKENS),
            MemoryUsageBytes: (long)GetMetricValue(METRIC_MEMORY_USAGE_BYTES),
            MemoryPressureLevel: (int)GetMetricValue(METRIC_MEMORY_PRESSURE_LEVEL),
            CleanupEvents: (int)GetMetricValue(METRIC_CLEANUP_EVENTS),
            ShutdownDurationSeconds: GetMetricValue(METRIC_SHUTDOWN_DURATION_SECONDS),
            HealthScore: (int)GetMetricValue(METRIC_HEALTH_SCORE),
            RequestCount: (long)GetMetricValue(METRIC_REQUEST_COUNT),
            ActiveRequests: (long)GetMetricValue(METRIC_ACTIVE_REQUESTS),
            ModuleCleanupCounts: _metrics.Where(kv => kv.Key.StartsWith(METRIC_MODULE_CLEANUP_COUNT))
                                         .ToDictionary(kv => kv.Key, kv => kv.Value)
        );
    }

    /// <summary>
    /// Set a metric value - embodying precise, mindful measurement
    /// </summary>
    private void SetMetric(string name, double value)
    {
        _metrics[name] = value;
        _metricTimestamps[name] = DateTime.UtcNow;
    }

    /// <summary>
    /// Increment a metric value - embodying cumulative awareness
    /// </summary>
    private void IncrementMetric(string name, double increment = 1.0)
    {
        _metrics.AddOrUpdate(name, increment, (key, existing) => existing + increment);
        _metricTimestamps[name] = DateTime.UtcNow;
    }

    /// <summary>
    /// Get a metric value - embodying reliable data access
    /// </summary>
    private double GetMetricValue(string name)
    {
        return _metrics.TryGetValue(name, out var value) ? value : 0.0;
    }
}

/// <summary>
/// Metrics summary for compassionate monitoring dashboard
/// </summary>
public record MetricsSummary(
    DateTime Timestamp,
    int ActiveSessions,
    int RevokedTokens,
    long MemoryUsageBytes,
    int MemoryPressureLevel,
    int CleanupEvents,
    double ShutdownDurationSeconds,
    int HealthScore,
    long RequestCount,
    long ActiveRequests,
    Dictionary<string, double> ModuleCleanupCounts
);
