using System.Collections.Concurrent;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// AI Pipeline Tracker - Simple integration point for AI request tracking
/// Embodying the principle of mindful AI flow - tracking AI requests with compassion
/// </summary>
public sealed class AIPipelineTracker
{
    private readonly ConcurrentDictionary<string, AIRequestInfo> _activeRequests = new();
    private readonly ConcurrentQueue<AIRequestInfo> _completedRequests = new();
    private readonly ConcurrentQueue<AIRequestInfo> _failedRequests = new();
    private readonly ICodexLogger _logger;
    
    private const int MAX_COMPLETED_REQUESTS = 1000;
    private const int SLOW_REQUEST_THRESHOLD_MS = 5000;

    public AIPipelineTracker(ICodexLogger logger)
    {
        _logger = logger;
        _logger.Info("[AIPipelineTracker] Initialized with compassionate AI tracking");
    }

    /// <summary>
    /// Start tracking an AI request - embodying the principle of mindful awareness
    /// </summary>
    public void StartRequest(string requestId, string requestType, string userId, string? model = null)
    {
        var requestInfo = new AIRequestInfo
        {
            RequestId = requestId,
            RequestType = requestType,
            UserId = userId,
            Model = model,
            StartTime = DateTime.UtcNow,
            Status = AIRequestStatus.Processing
        };
        
        _activeRequests[requestId] = requestInfo;
        _logger.Debug($"[AIPipelineTracker] Started tracking AI request: {requestType} for user {userId}");
    }

    /// <summary>
    /// Complete tracking an AI request - embodying the principle of completion awareness
    /// </summary>
    public void CompleteRequest(string requestId, bool success, long processingTimeMs, string? errorMessage = null)
    {
        if (_activeRequests.TryRemove(requestId, out var requestInfo))
        {
            requestInfo.EndTime = DateTime.UtcNow;
            requestInfo.ProcessingTime = processingTimeMs;
            requestInfo.Status = success ? AIRequestStatus.Completed : AIRequestStatus.Failed;
            requestInfo.ErrorMessage = errorMessage;
            
            if (success)
            {
                _completedRequests.Enqueue(requestInfo);
                _logger.Debug($"[AIPipelineTracker] Completed AI request: {requestInfo.RequestType} in {processingTimeMs}ms");
                
                // Log slow requests
                if (processingTimeMs > SLOW_REQUEST_THRESHOLD_MS)
                {
                    _logger.Warn($"[AIPipelineTracker] Slow AI request detected: {requestInfo.RequestType} took {processingTimeMs}ms");
                }
            }
            else
            {
                _failedRequests.Enqueue(requestInfo);
                _logger.Error($"[AIPipelineTracker] Failed AI request: {requestInfo.RequestType}, Error: {errorMessage}");
            }
            
            // Limit queue sizes
            while (_completedRequests.Count > MAX_COMPLETED_REQUESTS)
            {
                _completedRequests.TryDequeue(out _);
            }
            
            while (_failedRequests.Count > MAX_COMPLETED_REQUESTS)
            {
                _failedRequests.TryDequeue(out _);
            }
        }
        else
        {
            _logger.Warn($"[AIPipelineTracker] Attempted to complete unknown AI request: {requestId}");
        }
    }

    /// <summary>
    /// Get current AI pipeline metrics - embodying the principle of holistic awareness
    /// </summary>
    public object GetMetrics()
    {
        var activeRequests = _activeRequests.Values.ToList();
        var completedRequests = _completedRequests.ToList();
        var failedRequests = _failedRequests.ToList();
        
        var totalRequests = activeRequests.Count + completedRequests.Count + failedRequests.Count;
        var allProcessingTimes = completedRequests.Select(r => r.ProcessingTime).ToList();
        
        var averageProcessingTime = allProcessingTimes.Any() ? allProcessingTimes.Average() : 0;
        var p95ProcessingTime = allProcessingTimes.Any() ? CalculatePercentile(allProcessingTimes.ToArray(), 95) : 0;
        var p99ProcessingTime = allProcessingTimes.Any() ? CalculatePercentile(allProcessingTimes.ToArray(), 99) : 0;
        
        // Calculate throughput (requests per minute) based on recent activity
        var recentRequests = completedRequests
            .Where(r => r.EndTime > DateTime.UtcNow.AddMinutes(-1))
            .Count();
        
        return new
        {
            timestamp = DateTime.UtcNow,
            totalRequests = totalRequests,
            activeRequests = activeRequests.Count,
            completedRequests = completedRequests.Count,
            failedRequests = failedRequests.Count,
            averageProcessingTime = Math.Round(averageProcessingTime, 2),
            p95ProcessingTime = p95ProcessingTime,
            p99ProcessingTime = p99ProcessingTime,
            throughputPerMinute = recentRequests,
            slowRequests = completedRequests.Count(r => r.ProcessingTime > SLOW_REQUEST_THRESHOLD_MS),
            healthScore = CalculateHealthScore(activeRequests.Count, failedRequests.Count, totalRequests, p95ProcessingTime),
            status = GetHealthStatus(activeRequests.Count, failedRequests.Count, totalRequests, p95ProcessingTime)
        };
    }

    /// <summary>
    /// Get active AI requests - embodying the principle of current awareness
    /// </summary>
    public List<object> GetActiveRequests()
    {
        return _activeRequests.Values.Select(r => new
        {
            requestId = r.RequestId,
            requestType = r.RequestType,
            userId = r.UserId,
            model = r.Model,
            startTime = r.StartTime,
            duration = DateTime.UtcNow - r.StartTime
        }).ToList<object>();
    }

    /// <summary>
    /// Get recent AI requests - embodying the principle of historical awareness
    /// </summary>
    public List<object> GetRecentRequests(int count = 20)
    {
        var recentRequests = _completedRequests
            .OrderByDescending(r => r.EndTime)
            .Take(count)
            .Select(r => new
            {
                requestId = r.RequestId,
                requestType = r.RequestType,
                userId = r.UserId,
                model = r.Model,
                processingTime = r.ProcessingTime,
                endTime = r.EndTime,
                status = r.Status.ToString()
            })
            .ToList<object>();
        
        return recentRequests;
    }

    private static long CalculatePercentile(long[] values, int percentile)
    {
        if (values.Length == 0) return 0;
        
        Array.Sort(values);
        var index = (int)Math.Ceiling((percentile / 100.0) * values.Length) - 1;
        return values[Math.Max(0, Math.Min(index, values.Length - 1))];
    }

    private int CalculateHealthScore(int activeRequests, int failedRequests, long totalRequests, long p95ProcessingTime)
    {
        var score = 100;
        
        // Deduct points for high failure rates
        var failureRate = totalRequests > 0 ? (double)failedRequests / totalRequests : 0;
        if (failureRate > 0.05) score -= 25; // 5% failure rate
        else if (failureRate > 0.01) score -= 15; // 1% failure rate
        
        // Deduct points for slow processing times
        if (p95ProcessingTime > 30000) score -= 25; // 30 seconds
        else if (p95ProcessingTime > 10000) score -= 15; // 10 seconds
        else if (p95ProcessingTime > 5000) score -= 10; // 5 seconds
        
        // Deduct points for high concurrent requests
        if (activeRequests > 50) score -= 15;
        else if (activeRequests > 20) score -= 10;
        
        return Math.Max(0, score);
    }

    private string GetHealthStatus(int activeRequests, int failedRequests, long totalRequests, long p95ProcessingTime)
    {
        var healthScore = CalculateHealthScore(activeRequests, failedRequests, totalRequests, p95ProcessingTime);
        
        return healthScore switch
        {
            >= 90 => "Excellent",
            >= 80 => "Good",
            >= 70 => "Fair",
            >= 60 => "Poor",
            _ => "Critical"
        };
    }
}

/// <summary>
/// AI Request Info - Embodying the principle of mindful data tracking
/// </summary>
public class AIRequestInfo
{
    public string RequestId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Model { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long ProcessingTime { get; set; }
    public AIRequestStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// AI Request Status - Embodying the principle of clear state awareness
/// </summary>
public enum AIRequestStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Timeout = 4,
    Cancelled = 5,
    Rejected = 6
}
