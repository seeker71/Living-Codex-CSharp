using System.Collections.Concurrent;
using System.Diagnostics;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// AI Request Queue - Embodying the principle of mindful AI flow management
/// Manages AI request concurrency, prioritization, and graceful degradation
/// </summary>
public sealed class AIRequestQueue : IDisposable
{
    private readonly ICodexLogger _logger;
    private readonly ConcurrentQueue<AIQueueItem> _highPriorityQueue = new();
    private readonly ConcurrentQueue<AIQueueItem> _normalPriorityQueue = new();
    private readonly ConcurrentQueue<AIQueueItem> _lowPriorityQueue = new();
    private readonly ConcurrentDictionary<string, AIQueueItem> _activeRequests = new();
    private readonly ConcurrentDictionary<string, AIQueueItem> _completedRequests = new();
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly Timer _cleanupTimer;
    private readonly CancellationTokenSource _shutdownCts = new();
    
    // Configuration
    private readonly int _maxConcurrentRequests;
    private readonly int _maxQueueSize;
    private readonly TimeSpan _requestTimeout;
    private readonly TimeSpan _cleanupInterval;
    
    // Metrics
    private long _totalRequestsProcessed = 0;
    private long _totalRequestsRejected = 0;
    private long _totalRequestsTimeout = 0;
    private readonly object _metricsLock = new();

    public AIRequestQueue(
        ICodexLogger logger,
        int maxConcurrentRequests = 5,
        int maxQueueSize = 100,
        TimeSpan? requestTimeout = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _maxConcurrentRequests = maxConcurrentRequests;
        _maxQueueSize = maxQueueSize;
        _requestTimeout = requestTimeout ?? TimeSpan.FromMinutes(5);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(1);
        
        _concurrencyLimiter = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        
        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupExpiredRequests, null, _cleanupInterval, _cleanupInterval);
        
        _logger.Info($"[AIRequestQueue] Initialized with max concurrent: {maxConcurrentRequests}, max queue size: {maxQueueSize}");
    }

    /// <summary>
    /// Enqueue an AI request with priority - embodying the principle of mindful prioritization
    /// </summary>
    public async Task<AIQueueResult> EnqueueRequestAsync(
        string requestId,
        string requestType,
        string userId,
        Func<CancellationToken, Task<object>> requestProcessor,
        AIRequestPriority priority = AIRequestPriority.Normal,
        string? model = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? _requestTimeout;
        var queueItem = new AIQueueItem
        {
            RequestId = requestId,
            RequestType = requestType,
            UserId = userId,
            Model = model,
            Priority = priority,
            RequestProcessor = requestProcessor,
            Timeout = effectiveTimeout,
            CreatedAt = DateTime.UtcNow,
            Status = AIRequestStatus.Queued
        };

        // Check if queue is full
        var currentQueueSize = GetCurrentQueueSize();
        if (currentQueueSize >= _maxQueueSize)
        {
            Interlocked.Increment(ref _totalRequestsRejected);
            _logger.Warn($"[AIRequestQueue] Queue full ({currentQueueSize}/{_maxQueueSize}), rejecting request: {requestType}");
            
            return new AIQueueResult
            {
                Success = false,
                Status = AIRequestStatus.Rejected,
                ErrorMessage = "AI request queue is full. Please try again later.",
                EstimatedWaitTime = TimeSpan.FromMinutes(5), // Conservative estimate
                RequestId = requestId
            };
        }

        // Add to appropriate priority queue
        switch (priority)
        {
            case AIRequestPriority.High:
                _highPriorityQueue.Enqueue(queueItem);
                break;
            case AIRequestPriority.Normal:
                _normalPriorityQueue.Enqueue(queueItem);
                break;
            case AIRequestPriority.Low:
                _lowPriorityQueue.Enqueue(queueItem);
                break;
        }

        _logger.Debug($"[AIRequestQueue] Enqueued {priority} priority request: {requestType} for user {userId}");

        // Start processing if we have available slots
        _ = Task.Run(async () => await ProcessQueueAsync(cancellationToken), cancellationToken);

        // For synchronous requests, wait for completion
        if (queueItem.Status == AIRequestStatus.Queued)
        {
            return await WaitForCompletionAsync(queueItem, cancellationToken);
        }

        // For asynchronous requests, return immediately
        return new AIQueueResult
        {
            Success = true,
            Status = AIRequestStatus.Queued,
            RequestId = requestId,
            EstimatedWaitTime = EstimateWaitTime(priority)
        };
    }

    /// <summary>
    /// Process the request queue - embodying the principle of mindful processing
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_shutdownCts.Token.IsCancellationRequested)
        {
            var queueItem = GetNextRequest();
            if (queueItem == null)
            {
                // No requests in queue, wait a bit
                await Task.Delay(100, cancellationToken);
                continue;
            }

            // Try to acquire concurrency slot
            if (!await _concurrencyLimiter.WaitAsync(100, cancellationToken))
            {
                // No available slots, put request back at front of queue
                RequeueRequest(queueItem);
                await Task.Delay(50, cancellationToken);
                continue;
            }

            // Process the request
            _ = Task.Run(async () => await ProcessRequestAsync(queueItem, cancellationToken), cancellationToken);
        }
    }

    /// <summary>
    /// Get the next request from priority queues
    /// </summary>
    private AIQueueItem? GetNextRequest()
    {
        // High priority first
        if (_highPriorityQueue.TryDequeue(out var highPriorityItem))
            return highPriorityItem;
        
        // Normal priority second
        if (_normalPriorityQueue.TryDequeue(out var normalPriorityItem))
            return normalPriorityItem;
        
        // Low priority last
        if (_lowPriorityQueue.TryDequeue(out var lowPriorityItem))
            return lowPriorityItem;

        return null;
    }

    /// <summary>
    /// Put a request back at the front of its priority queue
    /// </summary>
    private void RequeueRequest(AIQueueItem queueItem)
    {
        // For simplicity, we'll create a new queue with this item first
        // In a production system, you might want a more sophisticated approach
        switch (queueItem.Priority)
        {
            case AIRequestPriority.High:
                var highItems = _highPriorityQueue.ToArray();
                _highPriorityQueue.Clear();
                _highPriorityQueue.Enqueue(queueItem);
                foreach (var item in highItems)
                    _highPriorityQueue.Enqueue(item);
                break;
            case AIRequestPriority.Normal:
                var normalItems = _normalPriorityQueue.ToArray();
                _normalPriorityQueue.Clear();
                _normalPriorityQueue.Enqueue(queueItem);
                foreach (var item in normalItems)
                    _normalPriorityQueue.Enqueue(item);
                break;
            case AIRequestPriority.Low:
                var lowItems = _lowPriorityQueue.ToArray();
                _lowPriorityQueue.Clear();
                _lowPriorityQueue.Enqueue(queueItem);
                foreach (var item in lowItems)
                    _lowPriorityQueue.Enqueue(item);
                break;
        }
    }

    /// <summary>
    /// Process a single request - embodying the principle of mindful execution
    /// </summary>
    private async Task ProcessRequestAsync(AIQueueItem queueItem, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        queueItem.Status = AIRequestStatus.Processing;
        queueItem.StartedAt = DateTime.UtcNow;
        _activeRequests[queueItem.RequestId] = queueItem;

        try
        {
            _logger.Debug($"[AIRequestQueue] Processing request: {queueItem.RequestType} for user {queueItem.UserId}");

            // Create timeout cancellation token
            using var timeoutCts = new CancellationTokenSource(queueItem.Timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token, _shutdownCts.Token);

            // Execute the request
            var result = await queueItem.RequestProcessor(combinedCts.Token);
            
            stopwatch.Stop();
            queueItem.Status = AIRequestStatus.Completed;
            queueItem.Result = result;
            queueItem.ProcessingTime = stopwatch.ElapsedMilliseconds;
            queueItem.CompletedAt = DateTime.UtcNow;

            Interlocked.Increment(ref _totalRequestsProcessed);
            _logger.Info($"[AIRequestQueue] Completed request: {queueItem.RequestType} in {queueItem.ProcessingTime}ms");

            // Move to completed requests
            _activeRequests.TryRemove(queueItem.RequestId, out _);
            _completedRequests[queueItem.RequestId] = queueItem;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _shutdownCts.Token.IsCancellationRequested)
        {
            stopwatch.Stop();
            queueItem.Status = AIRequestStatus.Cancelled;
            queueItem.ErrorMessage = "Request was cancelled";
            queueItem.ProcessingTime = stopwatch.ElapsedMilliseconds;
            
            _logger.Info($"[AIRequestQueue] Cancelled request: {queueItem.RequestType}");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            queueItem.Status = AIRequestStatus.Timeout;
            queueItem.ErrorMessage = $"Request timed out after {queueItem.Timeout.TotalSeconds} seconds";
            queueItem.ProcessingTime = stopwatch.ElapsedMilliseconds;
            
            Interlocked.Increment(ref _totalRequestsTimeout);
            _logger.Warn($"[AIRequestQueue] Request timed out: {queueItem.RequestType}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            queueItem.Status = AIRequestStatus.Failed;
            queueItem.ErrorMessage = ex.Message;
            queueItem.ProcessingTime = stopwatch.ElapsedMilliseconds;
            queueItem.CompletedAt = DateTime.UtcNow;
            
            _logger.Error($"[AIRequestQueue] Request failed: {queueItem.RequestType}, Error: {ex.Message}", ex);
        }
        finally
        {
            _activeRequests.TryRemove(queueItem.RequestId, out _);
            _concurrencyLimiter.Release();
            
            // Keep completed requests for a while for status checking
            if (queueItem.Status != AIRequestStatus.Processing)
            {
                _completedRequests[queueItem.RequestId] = queueItem;
            }
        }
    }

    /// <summary>
    /// Wait for a request to complete (for synchronous requests)
    /// </summary>
    private async Task<AIQueueResult> WaitForCompletionAsync(AIQueueItem queueItem, CancellationToken cancellationToken)
    {
        var timeoutCts = new CancellationTokenSource(queueItem.Timeout);
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Wait for the request to be processed
            while (queueItem.Status == AIRequestStatus.Queued || queueItem.Status == AIRequestStatus.Processing)
            {
                if (combinedCts.Token.IsCancellationRequested)
                    break;
                
                await Task.Delay(100, combinedCts.Token);
            }

            return new AIQueueResult
            {
                Success = queueItem.Status == AIRequestStatus.Completed,
                Status = queueItem.Status,
                Result = queueItem.Result,
                ErrorMessage = queueItem.ErrorMessage,
                ProcessingTime = queueItem.ProcessingTime,
                RequestId = queueItem.RequestId
            };
        }
        catch (OperationCanceledException)
        {
            return new AIQueueResult
            {
                Success = false,
                Status = AIRequestStatus.Timeout,
                ErrorMessage = "Request timed out while waiting for completion",
                RequestId = queueItem.RequestId
            };
        }
    }

    /// <summary>
    /// Get request status - embodying the principle of transparent awareness
    /// </summary>
    public AIQueueResult? GetRequestStatus(string requestId)
    {
        if (_activeRequests.TryGetValue(requestId, out var activeRequest))
        {
            return new AIQueueResult
            {
                Success = false,
                Status = activeRequest.Status,
                RequestId = requestId,
                EstimatedWaitTime = EstimateWaitTime(activeRequest.Priority)
            };
        }

        if (_completedRequests.TryGetValue(requestId, out var completedRequest))
        {
            return new AIQueueResult
            {
                Success = completedRequest.Status == AIRequestStatus.Completed,
                Status = completedRequest.Status,
                Result = completedRequest.Result,
                ErrorMessage = completedRequest.ErrorMessage,
                ProcessingTime = completedRequest.ProcessingTime,
                RequestId = requestId
            };
        }

        return null; // Request not found
    }

    /// <summary>
    /// Get queue metrics - embodying the principle of holistic awareness
    /// </summary>
    public AIQueueMetrics GetMetrics()
    {
        lock (_metricsLock)
        {
            var activeCount = _activeRequests.Count;
            var queueSize = GetCurrentQueueSize();
            var completedCount = _completedRequests.Count;

            return new AIQueueMetrics
            {
                Timestamp = DateTime.UtcNow,
                MaxConcurrentRequests = _maxConcurrentRequests,
                MaxQueueSize = _maxQueueSize,
                ActiveRequests = activeCount,
                QueuedRequests = queueSize,
                CompletedRequests = completedCount,
                TotalRequestsProcessed = _totalRequestsProcessed,
                TotalRequestsRejected = _totalRequestsRejected,
                TotalRequestsTimeout = _totalRequestsTimeout,
                QueueUtilization = (double)activeCount / _maxConcurrentRequests,
                QueueFullness = (double)queueSize / _maxQueueSize,
                HealthScore = CalculateHealthScore(),
                Status = GetQueueStatus()
            };
        }
    }

    /// <summary>
    /// Cleanup expired requests
    /// </summary>
    private void CleanupExpiredRequests(object? state)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-1); // Keep completed requests for 1 hour
            
            var expiredKeys = _completedRequests
                .Where(kvp => kvp.Value.CompletedAt < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _completedRequests.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.Debug($"[AIRequestQueue] Cleaned up {expiredKeys.Count} expired requests");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[AIRequestQueue] Error during cleanup: {ex.Message}", ex);
        }
    }

    private int GetCurrentQueueSize()
    {
        return _highPriorityQueue.Count + _normalPriorityQueue.Count + _lowPriorityQueue.Count;
    }

    private TimeSpan EstimateWaitTime(AIRequestPriority priority)
    {
        var queueSize = GetCurrentQueueSize();
        var activeCount = _activeRequests.Count;
        
        // Estimate based on current load and priority
        var baseWaitTime = activeCount * TimeSpan.FromSeconds(10); // Assume 10s per active request
        var queueWaitTime = queueSize * TimeSpan.FromSeconds(2); // Assume 2s per queued request
        
        // Priority adjustment
        var priorityMultiplier = priority switch
        {
            AIRequestPriority.High => 0.5,
            AIRequestPriority.Normal => 1.0,
            AIRequestPriority.Low => 2.0,
            _ => 1.0
        };

        return TimeSpan.FromMilliseconds((baseWaitTime.TotalMilliseconds + queueWaitTime.TotalMilliseconds) * priorityMultiplier);
    }

    private int CalculateHealthScore()
    {
        var score = 100;
        
        // Deduct for high queue utilization
        var queueUtilization = (double)_activeRequests.Count / _maxConcurrentRequests;
        if (queueUtilization > 0.9) score -= 30;
        else if (queueUtilization > 0.7) score -= 20;
        else if (queueUtilization > 0.5) score -= 10;
        
        // Deduct for queue fullness
        var queueFullness = (double)GetCurrentQueueSize() / _maxQueueSize;
        if (queueFullness > 0.9) score -= 25;
        else if (queueFullness > 0.7) score -= 15;
        else if (queueFullness > 0.5) score -= 10;
        
        // Deduct for high rejection rate
        var totalRequests = _totalRequestsProcessed + _totalRequestsRejected + _totalRequestsTimeout;
        if (totalRequests > 0)
        {
            var rejectionRate = (double)_totalRequestsRejected / totalRequests;
            if (rejectionRate > 0.1) score -= 20;
            else if (rejectionRate > 0.05) score -= 10;
        }
        
        return Math.Max(0, score);
    }

    private string GetQueueStatus()
    {
        var healthScore = CalculateHealthScore();
        var queueUtilization = (double)_activeRequests.Count / _maxConcurrentRequests;
        
        if (healthScore >= 90 && queueUtilization < 0.5) return "Excellent";
        if (healthScore >= 80 && queueUtilization < 0.7) return "Good";
        if (healthScore >= 70 && queueUtilization < 0.9) return "Fair";
        if (healthScore >= 60) return "Poor";
        return "Critical";
    }

    public void Dispose()
    {
        _shutdownCts.Cancel();
        _cleanupTimer?.Dispose();
        _concurrencyLimiter?.Dispose();
        _shutdownCts?.Dispose();
        _logger.Info("[AIRequestQueue] Disposed gracefully");
    }
}

/// <summary>
/// AI Queue Item - Embodying the principle of mindful request tracking
/// </summary>
public class AIQueueItem
{
    public string RequestId { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Model { get; set; }
    public AIRequestPriority Priority { get; set; }
    public Func<CancellationToken, Task<object>> RequestProcessor { get; set; } = null!;
    public TimeSpan Timeout { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long ProcessingTime { get; set; }
    public AIRequestStatus Status { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// AI Queue Result - Embodying the principle of clear response awareness
/// </summary>
public class AIQueueResult
{
    public bool Success { get; set; }
    public AIRequestStatus Status { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public long ProcessingTime { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public TimeSpan? EstimatedWaitTime { get; set; }
}

/// <summary>
/// AI Queue Metrics - Embodying the principle of comprehensive awareness
/// </summary>
public class AIQueueMetrics
{
    public DateTime Timestamp { get; set; }
    public int MaxConcurrentRequests { get; set; }
    public int MaxQueueSize { get; set; }
    public int ActiveRequests { get; set; }
    public int QueuedRequests { get; set; }
    public int CompletedRequests { get; set; }
    public long TotalRequestsProcessed { get; set; }
    public long TotalRequestsRejected { get; set; }
    public long TotalRequestsTimeout { get; set; }
    public double QueueUtilization { get; set; }
    public double QueueFullness { get; set; }
    public int HealthScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// AI Request Priority - Embodying the principle of mindful prioritization
/// </summary>
public enum AIRequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

// AIRequestStatus enum is defined in AIPipelineTracker.cs
