using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// AI Queue Module - Embodying the principle of mindful AI request orchestration
/// Provides queued AI endpoints with graceful degradation and async job tracking
/// </summary>
[MetaNode(Id = "ai-queue", Name = "AI Queue Module", Description = "Manages AI request queuing, concurrency control, and async job tracking")]
public class AIQueueModule : ModuleBase
{
    private readonly AIRequestQueue _requestQueue;
    private AIPipelineTracker? _pipelineTracker;

    public override string Name => "AI Queue Module";
    public override string Description => "Manages AI request queuing, concurrency control, and async job tracking";
    public override string Version => "1.0.0";

    public AIQueueModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        // Get AIPipelineTracker from service provider during initialization
        _pipelineTracker = null!; // Will be set in RegisterHttpEndpoints
        
        // Configure queue with compassionate limits
        _requestQueue = new AIRequestQueue(
            logger: logger,
            maxConcurrentRequests: GetMaxConcurrentRequests(),
            maxQueueSize: GetMaxQueueSize(),
            requestTimeout: TimeSpan.FromMinutes(5),
            cleanupInterval: TimeSpan.FromMinutes(1)
        );
        
        _logger.Info("[AIQueueModule] Initialized with mindful AI request management");
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "ai-queue",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "ai", "queue", "concurrency", "async", "jobs" },
            capabilities: new[] { 
                "request-queuing", "concurrency-control", "async-processing", 
                "job-tracking", "graceful-degradation", "priority-handling" 
            },
            spec: "codex.spec.ai-queue"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // Get AIPipelineTracker from service provider
        _pipelineTracker = base._serviceProvider?.GetService(typeof(AIPipelineTracker)) as AIPipelineTracker;
        if (_pipelineTracker == null)
        {
            _logger.Warn("[AIQueueModule] AIPipelineTracker not available from service provider");
        }
        
        _logger.Info("[AIQueueModule] Registering API handlers for AI request queue");
        base.RegisterApiHandlers(router, registry);
    }

    /// <summary>
    /// Queue an AI request with priority - embodying the principle of mindful processing
    /// </summary>
    [ApiRoute("POST", "/ai/queue/request", "ai-queue-request", "Queue an AI request for processing", "ai-queue")]
    public async Task<object> QueueAIRequestAsync([ApiParameter("request", "AI request details", Required = true, Location = "body")] AIQueueRequest request)
    {
        try
        {
            var requestId = request.RequestId ?? Guid.NewGuid().ToString("N");
            var userId = request.UserId ?? "anonymous";
            
            // Determine if this should be async based on expected processing time
            var shouldBeAsync = ShouldProcessAsync(request.RequestType, request.EstimatedDuration);
            
            // Create the request processor
            var requestProcessor = CreateRequestProcessor(request);
            
            // Determine priority
            var priority = DeterminePriority(request.Priority, request.UserId);
            
            // Enqueue the request
            var result = await _requestQueue.EnqueueRequestAsync(
                requestId: requestId,
                requestType: request.RequestType,
                userId: userId,
                requestProcessor: requestProcessor,
                priority: priority,
                model: request.Model,
                timeout: request.Timeout,
                cancellationToken: CancellationToken.None
            );

            if (result.Success)
            {
                _logger.Info($"[AIQueueModule] Successfully queued {request.RequestType} request: {requestId}");
                
                if (shouldBeAsync && result.Status == AIRequestStatus.Queued)
                {
                    // Return async job information
                    return new
                    {
                        success = true,
                        requestId = requestId,
                        status = "queued",
                        estimatedWaitTime = result.EstimatedWaitTime?.TotalSeconds,
                        checkStatusUrl = $"/ai/queue/status/{requestId}",
                        message = "Request queued for processing. Use the status endpoint to check progress."
                    };
                }
                else
                {
                    // Return synchronous result
                    return new
                    {
                        success = result.Success,
                        requestId = requestId,
                        status = result.Status.ToString().ToLower(),
                        result = result.Result,
                        processingTime = result.ProcessingTime,
                        errorMessage = result.ErrorMessage
                    };
                }
            }
            else
            {
                _logger.Warn($"[AIQueueModule] Failed to queue {request.RequestType} request: {result.ErrorMessage}");
                
                return new
                {
                    success = false,
                    requestId = requestId,
                    status = result.Status.ToString().ToLower(),
                    errorMessage = result.ErrorMessage,
                    estimatedWaitTime = result.EstimatedWaitTime?.TotalSeconds,
                    retryAfter = GetRetryAfter(result.Status)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[AIQueueModule] Error queuing AI request: {ex.Message}", ex);
            return new ErrorResponse($"Failed to queue AI request: {ex.Message}");
        }
    }

    /// <summary>
    /// Get request status - embodying the principle of transparent awareness
    /// </summary>
    [ApiRoute("GET", "/ai/queue/status/{requestId}", "ai-queue-status", "Get status of a queued AI request", "ai-queue")]
    public async Task<object> GetRequestStatusAsync([ApiParameter("requestId", "Request ID", Required = true, Location = "path")] string requestId)
    {
        try
        {
            var status = _requestQueue.GetRequestStatus(requestId);
            
            if (status == null)
            {
                return new ErrorResponse($"Request {requestId} not found", "NOT_FOUND");
            }

            return new
            {
                success = status.Success,
                requestId = requestId,
                status = status.Status.ToString().ToLower(),
                result = status.Result,
                processingTime = status.ProcessingTime,
                errorMessage = status.ErrorMessage,
                estimatedWaitTime = status.EstimatedWaitTime?.TotalSeconds,
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"[AIQueueModule] Error getting request status: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get request status: {ex.Message}");
        }
    }

    /// <summary>
    /// Get queue metrics - embodying the principle of holistic awareness
    /// </summary>
    [ApiRoute("GET", "/ai/queue/metrics", "ai-queue-metrics", "Get AI request queue metrics", "ai-queue")]
    public async Task<object> GetQueueMetricsAsync()
    {
        try
        {
            var metrics = _requestQueue.GetMetrics();
            
            return new
            {
                success = true,
                metrics = new
                {
                    timestamp = metrics.Timestamp,
                    configuration = new
                    {
                        maxConcurrentRequests = metrics.MaxConcurrentRequests,
                        maxQueueSize = metrics.MaxQueueSize
                    },
                    current = new
                    {
                        activeRequests = metrics.ActiveRequests,
                        queuedRequests = metrics.QueuedRequests,
                        completedRequests = metrics.CompletedRequests
                    },
                    totals = new
                    {
                        processed = metrics.TotalRequestsProcessed,
                        rejected = metrics.TotalRequestsRejected,
                        timeout = metrics.TotalRequestsTimeout
                    },
                    utilization = new
                    {
                        queueUtilization = Math.Round(metrics.QueueUtilization * 100, 2),
                        queueFullness = Math.Round(metrics.QueueFullness * 100, 2)
                    },
                    health = new
                    {
                        score = metrics.HealthScore,
                        status = metrics.Status
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"[AIQueueModule] Error getting queue metrics: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get queue metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Additional API endpoint for the AI Dashboard
    /// </summary>
    [ApiRoute("GET", "/api/ai-queue-metrics", "api-ai-queue-metrics", "Get AI queue metrics for dashboard", "ai-queue")]
    public async Task<object> GetApiQueueMetricsAsync()
    {
        return await GetQueueMetricsAsync();
    }

    /// <summary>
    /// Cancel a queued request - embodying the principle of graceful cancellation
    /// </summary>
    [ApiRoute("POST", "/ai/queue/cancel/{requestId}", "ai-queue-cancel", "Cancel a queued AI request", "ai-queue")]
    public async Task<object> CancelRequestAsync([ApiParameter("requestId", "Request ID", Required = true, Location = "path")] string requestId)
    {
        try
        {
            // Note: The current implementation doesn't support cancellation of queued requests
            // This would require additional infrastructure in AIRequestQueue
            
            _logger.Info($"[AIQueueModule] Cancel request called for: {requestId}");
            
            return new
            {
                success = false,
                requestId = requestId,
                message = "Request cancellation not yet implemented. Request will complete or timeout naturally.",
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"[AIQueueModule] Error canceling request: {ex.Message}", ex);
            return new ErrorResponse($"Failed to cancel request: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a request processor for the given request - embodying the principle of mindful delegation
    /// </summary>
    private Func<CancellationToken, Task<object>> CreateRequestProcessor(AIQueueRequest request)
    {
        return async (cancellationToken) =>
        {
            var requestId = request.RequestId ?? Guid.NewGuid().ToString("N");
            
            // Start tracking in pipeline tracker if available
            _pipelineTracker?.StartRequest(
                requestId: requestId,
                requestType: request.RequestType,
                userId: request.UserId ?? "anonymous",
                model: request.Model
            );

            try
            {
                // Route to appropriate AI module based on request type
                var result = await RouteToAIModuleAsync(request, cancellationToken);
                
                // Complete tracking if available
                _pipelineTracker?.CompleteRequest(
                    requestId: requestId,
                    success: true,
                    processingTimeMs: 0, // Will be calculated by the queue
                    errorMessage: null
                );

                return result;
            }
            catch (Exception ex)
            {
                // Complete tracking with error if available
                _pipelineTracker?.CompleteRequest(
                    requestId: requestId,
                    success: false,
                    processingTimeMs: 0,
                    errorMessage: ex.Message
                );
                
                throw;
            }
        };
    }

    /// <summary>
    /// Route request to appropriate AI module - embodying the principle of intelligent delegation
    /// </summary>
    private async Task<object> RouteToAIModuleAsync(AIQueueRequest request, CancellationToken cancellationToken)
    {
        // For now, simulate AI processing based on request type
        // In a real implementation, this would call the appropriate AI module
        
        var processingTime = GetEstimatedProcessingTime(request.RequestType);
        
        // Simulate processing time
        await Task.Delay(processingTime, cancellationToken);
        
        // Return mock result based on request type
        return request.RequestType.ToLower() switch
        {
            "concept-extraction" => new
            {
                concepts = new[] { "example-concept-1", "example-concept-2" },
                confidence = 0.85,
                processingTime = processingTime
            },
            "analysis" => new
            {
                analysis = "Mock analysis result",
                score = 0.92,
                processingTime = processingTime
            },
            "transformation" => new
            {
                transformed = "Mock transformed result",
                processingTime = processingTime
            },
            _ => new
            {
                result = $"Mock result for {request.RequestType}",
                processingTime = processingTime
            }
        };
    }

    private bool ShouldProcessAsync(string requestType, TimeSpan? estimatedDuration)
    {
        // Process async if estimated duration > 2 seconds or if explicitly requested
        return estimatedDuration?.TotalSeconds > 2 || IsLongRunningRequest(requestType);
    }

    private bool IsLongRunningRequest(string requestType)
    {
        var longRunningTypes = new[] { "transformation", "analysis", "generation" };
        return longRunningTypes.Any(t => requestType.ToLower().Contains(t));
    }

    private AIRequestPriority DeterminePriority(string? priority, string? userId)
    {
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<AIRequestPriority>(priority, true, out var parsedPriority))
        {
            return parsedPriority;
        }

        // Default priority based on user (authenticated users get higher priority)
        return string.IsNullOrEmpty(userId) || userId == "anonymous" ? AIRequestPriority.Low : AIRequestPriority.Normal;
    }

    private TimeSpan GetEstimatedProcessingTime(string requestType)
    {
        return requestType.ToLower() switch
        {
            "concept-extraction" => TimeSpan.FromSeconds(1),
            "analysis" => TimeSpan.FromSeconds(3),
            "transformation" => TimeSpan.FromSeconds(5),
            "generation" => TimeSpan.FromSeconds(8),
            _ => TimeSpan.FromSeconds(2)
        };
    }

    private int GetMaxConcurrentRequests()
    {
        var envValue = Environment.GetEnvironmentVariable("AI_MAX_CONCURRENT_REQUESTS");
        return int.TryParse(envValue, out var value) && value > 0 ? value : 5;
    }

    private int GetMaxQueueSize()
    {
        var envValue = Environment.GetEnvironmentVariable("AI_MAX_QUEUE_SIZE");
        return int.TryParse(envValue, out var value) && value > 0 ? value : 100;
    }

    private int GetRetryAfter(AIRequestStatus status)
    {
        return status switch
        {
            AIRequestStatus.Rejected => 60, // 1 minute
            AIRequestStatus.Timeout => 30,  // 30 seconds
            _ => 10 // 10 seconds
        };
    }

    public void Dispose()
    {
        _requestQueue?.Dispose();
    }
}

/// <summary>
/// AI Queue Request - Embodying the principle of clear request specification
/// </summary>
public class AIQueueRequest
{
    public string? RequestId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Model { get; set; }
    public string? Priority { get; set; }
    public TimeSpan? Timeout { get; set; }
    public int? EstimatedDurationSeconds { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    public TimeSpan? EstimatedDuration => EstimatedDurationSeconds.HasValue 
        ? TimeSpan.FromSeconds(EstimatedDurationSeconds.Value) 
        : null;
}
