using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// AI Test Module - Demonstrates AI pipeline monitoring integration
/// Embodying the principle of mindful testing - testing AI monitoring with compassion
/// </summary>
[ApiModule(Name = "AITestModule", Version = "1.0.0", Description = "AI pipeline monitoring demonstration and testing", Tags = new[] { "ai", "test", "monitoring", "pipeline" })]
[MetaNode(Id = "codex.ai.test", Name = "AI Test Module", Description = "AI pipeline monitoring demonstration and testing")]
public sealed class AITestModule : ModuleBase
{
    private readonly HttpClient _httpClient;

    public override string Name => "AI Test Module";
    public override string Description => "AI pipeline monitoring demonstration and testing";
    public override string Version => "1.0.0";

    public AITestModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _httpClient = httpClient;
        
        logger.Info("[AITest] Module initialized with compassionate AI testing");
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.ai.test",
            name: "AI Test Module",
            version: "1.0.0",
            description: "AI pipeline monitoring demonstration and testing",
            tags: new[] { "ai", "test", "monitoring", "demo", "pipeline" },
            capabilities: new[] { 
                "ai-request-simulation", "pipeline-monitoring-demo", "performance-testing",
                "ai-metrics-demonstration", "monitoring-integration-test"
            },
            spec: "codex.spec.ai.test"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        base._logger.Info("AI Test Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store service provider for dependency injection
        base._serviceProvider = app.Services;
        
        base._logger.Info("AI Test Module HTTP endpoints registered");
    }
    
    /// <summary>
    /// Get the AI pipeline tracker from the service provider
    /// </summary>
    private AIPipelineTracker? GetAIPipelineTracker()
    {
        if (_serviceProvider == null)
        {
            base._logger.Error("[AITest] Service provider is null!");
            return null;
        }
        
        var tracker = _serviceProvider.GetService<AIPipelineTracker>();
        base._logger.Info($"[AITest] AIPipelineTracker from service provider: {tracker != null}");
        return tracker;
    }

    /// <summary>
    /// Simulate an AI request - embodying the principle of mindful testing
    /// </summary>
    [Post("/ai/test/simulate", "Simulate AI Request", "Simulate an AI request for monitoring demonstration", "codex.ai.test")]
    public async Task<IResult> SimulateAIRequestAsync(JsonElement? request)
    {
        try
        {
            base._logger.Info("[AITest] SimulateAIRequestAsync called");
            
            // For now, skip AI pipeline tracking until service provider issue is resolved
            // var aiTracker = GetAIPipelineTracker();
            // if (aiTracker == null)
            // {
            //     base._logger.Error("[AITest] AIPipelineTracker is null!");
            //     return Results.Json(new { success = false, error = "AIPipelineTracker not initialized" }, statusCode: 503);
            // }
            
            if (request == null)
            {
                base._logger.Error("[AITest] Request is null!");
                return Results.BadRequest(new { success = false, error = "Request is null" });
            }
            
            var requestType = request.Value.GetProperty("requestType").GetString() ?? "text-generation";
            var userId = request.Value.GetProperty("userId").GetString() ?? "test-user";
            var model = request.Value.GetProperty("model").GetString() ?? "gpt-3.5-turbo";
            var processingTime = request.Value.TryGetProperty("processingTime", out var timeProp) ? timeProp.GetInt32() : 2000;
            
            base._logger.Info($"[AITest] Parsed request - Type: {requestType}, User: {userId}, Model: {model}, Time: {processingTime}");
            
            var requestId = Guid.NewGuid().ToString();
            
            base._logger.Info($"[AITest] Simulating AI request: {requestType} for user {userId}");
            
            // Simulate processing time (AI tracking temporarily disabled)
            await Task.Delay(processingTime);
            base._logger.Info($"[AITest] Completed processing delay: {processingTime}ms");
            
            return Results.Ok(new
            {
                success = true,
                message = "AI request simulated successfully",
                requestId = requestId,
                requestType = requestType,
                userId = userId,
                model = model,
                processingTime = processingTime,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            base._logger.Error($"Error simulating AI request: {ex.Message}", ex);
            return Results.Json(new { success = false, error = $"Failed to simulate AI request: {ex.Message}" }, statusCode: 500);
        }
    }

    /// <summary>
    /// Simulate a slow AI request - embodying the principle of mindful testing
    /// </summary>
    [Post("/ai/test/slow", "Simulate Slow AI Request", "Simulate a slow AI request for monitoring demonstration", "codex.ai.test")]
    public async Task<IResult> SimulateSlowAIRequestAsync(JsonElement request)
    {
        try
        {
            var aiTracker = GetAIPipelineTracker();
            if (aiTracker == null)
            {
                base._logger.Error("[AITest] AIPipelineTracker is null!");
                return Results.Json(new { success = false, error = "AIPipelineTracker not initialized" }, statusCode: 503);
            }
            
            var requestType = request.GetProperty("requestType").GetString() ?? "complex-analysis";
            var userId = request.GetProperty("userId").GetString() ?? "test-user";
            var model = request.GetProperty("model").GetString() ?? "gpt-4";
            var processingTime = request.TryGetProperty("processingTime", out var timeProp) ? timeProp.GetInt32() : 8000;
            
            var requestId = Guid.NewGuid().ToString();
            
            base._logger.Info($"[AITest] Simulating slow AI request: {requestType} for user {userId}");
            
            // Start tracking the AI request
            aiTracker.StartRequest(requestId, requestType, userId, model);
            
            // Simulate slow processing time
            await Task.Delay(processingTime);
            
            // Complete the request (simulate success)
            aiTracker.CompleteRequest(requestId, true, processingTime);
            
            return Results.Ok(new
            {
                success = true,
                message = "Slow AI request simulated successfully",
                requestId = requestId,
                requestType = requestType,
                userId = userId,
                model = model,
                processingTime = processingTime,
                timestamp = DateTime.UtcNow,
                note = "This request was intentionally slow for monitoring demonstration"
            });
        }
        catch (Exception ex)
        {
            base._logger.Error("Error simulating slow AI request", ex);
            return Results.Json(new { success = false, error = "Failed to simulate slow AI request" }, statusCode: 500);
        }
    }

    /// <summary>
    /// Simulate a failed AI request - embodying the principle of mindful testing
    /// </summary>
    [Post("/ai/test/fail", "Simulate Failed AI Request", "Simulate a failed AI request for monitoring demonstration", "codex.ai.test")]
    public async Task<IResult> SimulateFailedAIRequestAsync(JsonElement request)
    {
        try
        {
            var aiTracker = GetAIPipelineTracker();
            if (aiTracker == null)
            {
                base._logger.Error("[AITest] AIPipelineTracker is null!");
                return Results.Json(new { success = false, error = "AIPipelineTracker not initialized" }, statusCode: 503);
            }
            
            var requestType = request.GetProperty("requestType").GetString() ?? "error-prone-task";
            var userId = request.GetProperty("userId").GetString() ?? "test-user";
            var model = request.GetProperty("model").GetString() ?? "unstable-model";
            var processingTime = request.TryGetProperty("processingTime", out var timeProp) ? timeProp.GetInt32() : 1500;
            var errorMessage = request.GetProperty("errorMessage").GetString() ?? "Simulated AI processing error";
            
            var requestId = Guid.NewGuid().ToString();
            
            base._logger.Info($"[AITest] Simulating failed AI request: {requestType} for user {userId}");
            
            // Start tracking the AI request
            aiTracker.StartRequest(requestId, requestType, userId, model);
            
            // Simulate processing time before failure
            await Task.Delay(processingTime);
            
            // Complete the request (simulate failure)
            aiTracker.CompleteRequest(requestId, false, processingTime, errorMessage);
            
            return Results.Json(new
            {
                success = false,
                message = "AI request failed as simulated",
                requestId = requestId,
                requestType = requestType,
                userId = userId,
                model = model,
                processingTime = processingTime,
                errorMessage = errorMessage,
                timestamp = DateTime.UtcNow,
                note = "This request was intentionally failed for monitoring demonstration"
            }, statusCode: 500);
        }
        catch (Exception ex)
        {
            base._logger.Error("Error simulating failed AI request", ex);
            return Results.Json(new { success = false, error = "Failed to simulate failed AI request" }, statusCode: 500);
        }
    }

    /// <summary>
    /// Get AI pipeline test metrics - embodying the principle of holistic testing awareness
    /// </summary>
    [Get("/ai/test/metrics", "Get AI Test Metrics", "Get current AI pipeline test metrics", "codex.ai.test")]
    public async Task<IResult> GetAITestMetricsAsync()
    {
        try
        {
            var aiTracker = GetAIPipelineTracker();
            if (aiTracker == null)
            {
                base._logger.Error("[AITest] AIPipelineTracker is null!");
                return Results.Json(new { success = false, error = "AIPipelineTracker not initialized" }, statusCode: 503);
            }
            
            var metrics = aiTracker.GetMetrics();
            var activeRequests = aiTracker.GetActiveRequests();
            var recentRequests = aiTracker.GetRecentRequests();
            
            return Results.Ok(new
            {
                success = true,
                message = "AI test metrics retrieved successfully",
                timestamp = DateTime.UtcNow,
                metrics = metrics,
                activeRequests = activeRequests,
                recentRequests = recentRequests,
                testInfo = new
                {
                    availableTests = new[]
                    {
                        "POST /ai/test/simulate - Normal AI request simulation",
                        "POST /ai/test/slow - Slow AI request simulation", 
                        "POST /ai/test/fail - Failed AI request simulation"
                    },
                    exampleRequest = new
                    {
                        requestType = "text-generation",
                        userId = "test-user",
                        model = "gpt-3.5-turbo",
                        processingTime = 2000
                    }
                }
            });
        }
        catch (Exception ex)
        {
            base._logger.Error("Error getting AI test metrics", ex);
            return Results.Json(new { success = false, error = "Failed to get AI test metrics" }, statusCode: 500);
        }
    }
}
