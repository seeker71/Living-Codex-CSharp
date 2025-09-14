using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Performance monitoring module - provides performance metrics and optimization insights
/// </summary>
[ApiModule(Name = "PerformanceModule", Version = "0.1.0", Description = "Performance monitoring and optimization insights", Tags = new[] { "performance", "monitoring", "optimization" })]
public sealed class PerformanceModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly PerformanceProfiler _profiler;
    private readonly CodexBootstrap.Core.ICodexLogger _logger;

    public PerformanceModule(NodeRegistry registry, PerformanceProfiler profiler, CodexBootstrap.Core.ICodexLogger logger)
    {
        _registry = registry;
        _profiler = profiler;
        _logger = logger;
    }

    // Parameterless constructor for dynamic instantiation
    public PerformanceModule() : this(null!, null!, null!)
    {
        // This is a fallback constructor for dynamic instantiation
        // The actual dependencies should be injected by the DI container
    }

    public string ModuleId => "codex.performance";
    public string Name => "Performance Module";
    public string Version => "0.1.0";
    public string Description => "Performance monitoring and optimization insights.";

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: ModuleId,
            name: Name,
            version: Version,
            description: Description,
            capabilities: new[] { "performance-monitoring", "metrics", "optimization", "profiling" },
            tags: new[] { "performance", "monitoring", "optimization" },
            specReference: "codex.spec.performance"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered via attribute-based routing
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are now registered via attribute-based routing
    }

    /// <summary>
    /// Get performance metrics for all operations
    /// </summary>
    [ApiRoute("GET", "/performance/metrics", "get-metrics", "Get performance metrics for all operations", "codex.performance")]
    public async Task<object> GetMetricsAsync()
    {
        try
        {
            var allMetrics = _profiler.GetAllMetrics();
            return await Task.FromResult<object>(new
            {
                success = true,
                data = new
                {
                    totalOperations = allMetrics.Count,
                    metrics = allMetrics.Values.Select(m => new
                    {
                        operationName = m.OperationName,
                        callCount = m.CallCount,
                        averageDurationMs = Math.Round(m.AverageDurationMs, 2),
                        minDurationMs = m.MinDurationMs,
                        maxDurationMs = m.MaxDurationMs,
                        successCount = m.SuccessCount,
                        errorCount = m.ErrorCount,
                        errorRate = Math.Round(m.ErrorRate * 100, 2)
                    }).OrderByDescending(m => m.callCount)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting performance metrics: {ex.Message}", ex);
            return await Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get top slowest operations
    /// </summary>
    [ApiRoute("GET", "/performance/slowest", "get-slowest", "Get top slowest operations", "codex.performance")]
    public async Task<object> GetSlowestOperationsAsync([ApiParameter("count", "Number of operations to return", Required = false)] int count = 10)
    {
        try
        {
            var slowest = _profiler.GetTopSlowOperations(count);
            return await Task.FromResult<object>(new
            {
                success = true,
                data = new
                {
                    count = slowest.Count,
                    operations = slowest.Select(m => new
                    {
                        operationName = m.OperationName,
                        averageDurationMs = Math.Round(m.AverageDurationMs, 2),
                        callCount = m.CallCount,
                        errorRate = Math.Round(m.ErrorRate * 100, 2)
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting slowest operations: {ex.Message}", ex);
            return await Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get most frequent operations
    /// </summary>
    [ApiRoute("GET", "/performance/frequent", "get-frequent", "Get most frequent operations", "codex.performance")]
    public async Task<object> GetFrequentOperationsAsync([ApiParameter("count", "Number of operations to return", Required = false)] int count = 10)
    {
        try
        {
            var frequent = _profiler.GetTopFrequentOperations(count);
            return await Task.FromResult<object>(new
            {
                success = true,
                data = new
                {
                    count = frequent.Count,
                    operations = frequent.Select(m => new
                    {
                        operationName = m.OperationName,
                        callCount = m.CallCount,
                        averageDurationMs = Math.Round(m.AverageDurationMs, 2),
                        errorRate = Math.Round(m.ErrorRate * 100, 2)
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting frequent operations: {ex.Message}", ex);
            return await Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get operations with errors
    /// </summary>
    [ApiRoute("GET", "/performance/errors", "get-errors", "Get operations with errors", "codex.performance")]
    public async Task<object> GetOperationsWithErrorsAsync([ApiParameter("count", "Number of operations to return", Required = false)] int count = 10)
    {
        try
        {
            var withErrors = _profiler.GetOperationsWithErrors(count);
            return await Task.FromResult<object>(new
            {
                success = true,
                data = new
                {
                    count = withErrors.Count,
                    operations = withErrors.Select(m => new
                    {
                        operationName = m.OperationName,
                        errorCount = m.ErrorCount,
                        errorRate = Math.Round(m.ErrorRate * 100, 2),
                        callCount = m.CallCount,
                        averageDurationMs = Math.Round(m.AverageDurationMs, 2)
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting operations with errors: {ex.Message}", ex);
            return await Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance summary and recommendations
    /// </summary>
    [ApiRoute("GET", "/performance/summary", "get-summary", "Get performance summary and recommendations", "codex.performance")]
    public async Task<object> GetPerformanceSummaryAsync()
    {
        try
        {
            var allMetrics = _profiler.GetAllMetrics();
            var slowest = _profiler.GetTopSlowOperations(5);
            var frequent = _profiler.GetTopFrequentOperations(5);
            var withErrors = _profiler.GetOperationsWithErrors(5);

            var totalCalls = allMetrics.Values.Sum(m => m.CallCount);
            var totalErrors = allMetrics.Values.Sum(m => m.ErrorCount);
            var overallErrorRate = totalCalls > 0 ? (double)totalErrors / totalCalls : 0;

            var recommendations = new List<string>();

            // Generate recommendations based on metrics
            if (slowest.Any(s => s.AverageDurationMs > 1000))
            {
                recommendations.Add("Consider optimizing slow operations (>1s average)");
            }

            if (withErrors.Any(e => e.ErrorRate > 0.1))
            {
                recommendations.Add("High error rates detected - investigate error handling");
            }

            if (frequent.Any(f => f.CallCount > 1000))
            {
                recommendations.Add("High-frequency operations detected - consider caching");
            }

            if (overallErrorRate > 0.05)
            {
                recommendations.Add("Overall error rate is high - review system stability");
            }

            return await Task.FromResult<object>(new
            {
                success = true,
                data = new
                {
                    summary = new
                    {
                        totalOperations = allMetrics.Count,
                        totalCalls = totalCalls,
                        totalErrors = totalErrors,
                        overallErrorRate = Math.Round(overallErrorRate * 100, 2)
                    },
                    slowestOperations = slowest.Take(3).Select(m => new
                    {
                        operationName = m.OperationName,
                        averageDurationMs = Math.Round(m.AverageDurationMs, 2)
                    }),
                    frequentOperations = frequent.Take(3).Select(m => new
                    {
                        operationName = m.OperationName,
                        callCount = m.CallCount
                    }),
                    errorOperations = withErrors.Take(3).Select(m => new
                    {
                        operationName = m.OperationName,
                        errorRate = Math.Round(m.ErrorRate * 100, 2)
                    }),
                    recommendations = recommendations
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting performance summary: {ex.Message}", ex);
            return await Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }
}
