using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Intelligent Caching Module - Advanced caching with predictive pre-loading
/// Implements smart caching strategies, usage pattern analysis, and performance optimization
/// </summary>
public class IntelligentCachingModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private readonly Dictionary<string, UsagePattern> _usagePatterns = new();
    private readonly Dictionary<string, PreloadPrediction> _predictions = new();
    private readonly List<CacheMetrics> _cacheMetrics = new();
    private CoreApiService? _coreApiService;
    private readonly object _cacheLock = new();

    public IntelligentCachingModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public IntelligentCachingModule() : this(new NodeRegistry()) { }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.intelligent-caching",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Intelligent Caching Module",
            Description: "Advanced caching system with predictive pre-loading and performance optimization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "predictive-preloading", "usage-pattern-analysis", "cache-optimization", "performance-monitoring", "smart-invalidation" },
                    endpoints = new[] { "preload-concepts", "analyze-patterns", "optimize-cache", "get-metrics", "invalidate-cache" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Intelligent Caching Module",
                ["version"] = "1.0.0",
                ["type"] = "caching",
                ["capabilities"] = new[] { "predictive-preloading", "usage-pattern-analysis", "cache-optimization", "performance-monitoring" }
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _coreApiService = coreApi;
        
        // Register all Intelligent Caching related nodes for AI agent discovery
        RegisterIntelligentCachingNodes(registry);
    }

    /// <summary>
    /// Register all Intelligent Caching related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterIntelligentCachingNodes(NodeRegistry registry)
    {
        // Register Intelligent Caching module node
        var intelligentCachingNode = new Node(
            Id: "codex.intelligent-caching",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Intelligent Caching Module",
            Description: "Advanced caching system with predictive pre-loading and performance optimization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "predictive-preloading", "usage-pattern-analysis", "cache-optimization", "performance-monitoring", "smart-invalidation" },
                    endpoints = new[] { "preload-concepts", "analyze-patterns", "optimize-cache", "get-metrics", "invalidate-cache" },
                    integration = "performance-optimization"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Intelligent Caching Module",
                ["version"] = "1.0.0",
                ["type"] = "caching",
                ["parentModule"] = "codex.intelligent-caching",
                ["capabilities"] = new[] { "predictive-preloading", "usage-pattern-analysis", "cache-optimization", "performance-monitoring" }
            }
        );
        registry.Upsert(intelligentCachingNode);

        // Register Intelligent Caching routes as nodes
        RegisterIntelligentCachingRoutes(registry);
        
        // Register Intelligent Caching DTOs as nodes
        RegisterIntelligentCachingDTOs(registry);
    }

    /// <summary>
    /// Register Intelligent Caching routes as discoverable nodes
    /// </summary>
    private void RegisterIntelligentCachingRoutes(NodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/cache/preload", method = "POST", name = "cache-preload", description = "Preload concepts based on predictions" },
            new { path = "/cache/patterns/analyze", method = "POST", name = "cache-patterns-analyze", description = "Analyze usage patterns for optimization" },
            new { path = "/cache/optimize", method = "POST", name = "cache-optimize", description = "Optimize cache based on patterns and metrics" },
            new { path = "/cache/metrics", method = "GET", name = "cache-metrics", description = "Get cache performance metrics" },
            new { path = "/cache/invalidate", method = "POST", name = "cache-invalidate", description = "Invalidate cache entries" },
            new { path = "/cache/predictions", method = "GET", name = "cache-predictions", description = "Get preload predictions" },
            new { path = "/cache/patterns", method = "GET", name = "cache-patterns", description = "Get usage patterns" },
            new { path = "/cache/health", method = "GET", name = "cache-health", description = "Get cache health status" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"intelligent-caching.route.{route.name}",
                TypeId: "meta.route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"Intelligent Caching route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetCachingRouteParameters(route.name),
                        responseType = GetCachingRouteResponseType(route.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = route.name,
                    ["path"] = route.path,
                    ["method"] = route.method,
                    ["description"] = route.description,
                    ["module"] = "codex.intelligent-caching",
                    ["parentModule"] = "codex.intelligent-caching"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register Intelligent Caching DTOs as discoverable nodes
    /// </summary>
    private void RegisterIntelligentCachingDTOs(NodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "PreloadRequest", description = "Request to preload concepts", properties = new[] { "ConceptIds", "PreloadStrategy", "Priority", "MaxItems" } },
            new { name = "PreloadResponse", description = "Response from preload operation", properties = new[] { "Success", "PreloadedCount", "SkippedCount", "Errors", "PreloadTime" } },
            new { name = "CachePatternAnalysisRequest", description = "Request to analyze usage patterns", properties = new[] { "TimeRange", "PatternTypes", "MinFrequency", "MaxResults" } },
            new { name = "CachePatternAnalysisResponse", description = "Response from pattern analysis", properties = new[] { "Success", "Patterns", "Insights", "Recommendations", "AnalysisTime" } },
            new { name = "CacheOptimizationRequest", description = "Request to optimize cache", properties = new[] { "OptimizationStrategy", "TargetMetrics", "Constraints" } },
            new { name = "CacheOptimizationResponse", description = "Response from cache optimization", properties = new[] { "Success", "OptimizationsApplied", "PerformanceGain", "OptimizationTime" } },
            new { name = "CacheMetricsResponse", description = "Response with cache metrics", properties = new[] { "Success", "Metrics", "Timestamp", "TimeRange" } },
            new { name = "CacheInvalidationRequest", description = "Request to invalidate cache", properties = new[] { "Keys", "Pattern", "Reason", "Force" } },
            new { name = "CacheInvalidationResponse", description = "Response from cache invalidation", properties = new[] { "Success", "InvalidatedCount", "InvalidationTime" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"intelligent-caching.dto.{dto.name}",
                TypeId: "meta.type",
                State: ContentState.Ice,
                Locale: "en",
                Title: dto.name,
                Description: dto.description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = dto.name,
                        description = dto.description,
                        properties = dto.properties,
                        type = "record",
                        module = "codex.intelligent-caching",
                        usage = GetCachingDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.intelligent-caching",
                    ["parentModule"] = "codex.intelligent-caching",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    // Helper methods for AI agent generation
    private object GetCachingRouteParameters(string routeName)
    {
        return routeName switch
        {
            "cache-preload" => new
            {
                request = new { type = "PreloadRequest", required = true, location = "body", description = "Preload configuration" }
            },
            "cache-patterns-analyze" => new
            {
                request = new { type = "PatternAnalysisRequest", required = true, location = "body", description = "Pattern analysis configuration" }
            },
            "cache-optimize" => new
            {
                request = new { type = "CacheOptimizationRequest", required = true, location = "body", description = "Cache optimization configuration" }
            },
            "cache-metrics" => new
            {
                timeRange = new { type = "string", required = false, location = "query", description = "Time range for metrics" }
            },
            "cache-invalidate" => new
            {
                request = new { type = "CacheInvalidationRequest", required = true, location = "body", description = "Cache invalidation configuration" }
            },
            "cache-predictions" => new
            {
                limit = new { type = "int", required = false, location = "query", description = "Maximum number of predictions" }
            },
            "cache-patterns" => new
            {
                patternType = new { type = "string", required = false, location = "query", description = "Type of patterns to retrieve" }
            },
            "cache-health" => new { },
            _ => new { }
        };
    }

    private string GetCachingRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "cache-preload" => "PreloadResponse",
            "cache-patterns-analyze" => "PatternAnalysisResponse",
            "cache-optimize" => "CacheOptimizationResponse",
            "cache-metrics" => "CacheMetricsResponse",
            "cache-invalidate" => "CacheInvalidationResponse",
            "cache-predictions" => "PreloadPrediction[]",
            "cache-patterns" => "UsagePattern[]",
            "cache-health" => "CacheHealthStatus",
            _ => "object"
        };
    }

    private string GetCachingDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "PreloadRequest" => "Used to request preloading of concepts based on predictions and patterns.",
            "PreloadResponse" => "Returned when preload operation is completed. Contains statistics about preloaded items.",
            "CachePatternAnalysisRequest" => "Used to request analysis of usage patterns for cache optimization.",
            "CachePatternAnalysisResponse" => "Returned when pattern analysis is completed. Contains discovered patterns and insights.",
            "CacheOptimizationRequest" => "Used to request cache optimization based on patterns and metrics.",
            "CacheOptimizationResponse" => "Returned when cache optimization is completed. Contains optimization results.",
            "CacheMetricsResponse" => "Returned when requesting cache performance metrics. Contains detailed statistics.",
            "CacheInvalidationRequest" => "Used to request invalidation of specific cache entries or patterns.",
            "CacheInvalidationResponse" => "Returned when cache invalidation is completed. Contains invalidation statistics.",
            _ => "Intelligent Caching data transfer object"
        };
    }

    // Intelligent Caching API Methods
    [ApiRoute("POST", "/cache/preload", "cache-preload", "Preload concepts based on predictions", "codex.intelligent-caching")]
    public async Task<object> PreloadConcepts([ApiParameter("request", "Preload request", Required = true, Location = "body")] PreloadRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var preloadedCount = 0;
            var skippedCount = 0;
            var errors = new List<string>();

            // Get predictions if no specific concepts provided
            var conceptsToPreload = request.ConceptIds?.ToList() ?? await GetPredictedConcepts(request.MaxItems);

            foreach (var conceptId in conceptsToPreload)
            {
                try
                {
                    if (await PreloadConcept(conceptId, request.PreloadStrategy, request.Priority))
                    {
                        preloadedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to preload {conceptId}: {ex.Message}");
                }
            }

            var preloadTime = DateTime.UtcNow - startTime;

            return new PreloadResponse(
                Success: true,
                PreloadedCount: preloadedCount,
                SkippedCount: skippedCount,
                Errors: errors,
                PreloadTime: preloadTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Preload operation failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cache/patterns/analyze", "cache-patterns-analyze", "Analyze usage patterns for optimization", "codex.intelligent-caching")]
    public async Task<object> AnalyzePatterns([ApiParameter("request", "Pattern analysis request", Required = true, Location = "body")] CachePatternAnalysisRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var patterns = await AnalyzeUsagePatterns(request);
            var insights = GeneratePatternInsights(patterns);
            var recommendations = GenerateOptimizationRecommendations(patterns, insights);

            var analysisTime = DateTime.UtcNow - startTime;

            return new CachePatternAnalysisResponse(
                Success: true,
                Patterns: patterns,
                Insights: insights,
                Recommendations: recommendations,
                AnalysisTime: analysisTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Pattern analysis failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cache/optimize", "cache-optimize", "Optimize cache based on patterns and metrics", "codex.intelligent-caching")]
    public async Task<object> OptimizeCache([ApiParameter("request", "Cache optimization request", Required = true, Location = "body")] CacheOptimizationRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var optimizationsApplied = await ApplyCacheOptimizations(request);
            var performanceGain = CalculatePerformanceGain(optimizationsApplied);

            var optimizationTime = DateTime.UtcNow - startTime;

            return new CacheOptimizationResponse(
                Success: true,
                OptimizationsApplied: optimizationsApplied,
                PerformanceGain: performanceGain,
                OptimizationTime: optimizationTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Cache optimization failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/cache/metrics", "cache-metrics", "Get cache performance metrics", "codex.intelligent-caching")]
    public async Task<object> GetCacheMetrics([ApiParameter("timeRange", "Time range for metrics", Required = false, Location = "query")] string? timeRange = null)
    {
        try
        {
            var metrics = await CalculateCacheMetrics(timeRange);

            return new CacheMetricsResponse(
                Success: true,
                Metrics: metrics,
                Timestamp: DateTime.UtcNow,
                TimeRange: timeRange ?? "all"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cache metrics: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/cache/invalidate", "cache-invalidate", "Invalidate cache entries", "codex.intelligent-caching")]
    public async Task<object> InvalidateCache([ApiParameter("request", "Cache invalidation request", Required = true, Location = "body")] CacheInvalidationRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var invalidatedCount = await PerformCacheInvalidation(request);

            var invalidationTime = DateTime.UtcNow - startTime;

            return new CacheInvalidationResponse(
                Success: true,
                InvalidatedCount: invalidatedCount,
                InvalidationTime: invalidationTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Cache invalidation failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/cache/predictions", "cache-predictions", "Get preload predictions", "codex.intelligent-caching")]
    public async Task<object> GetPredictions([ApiParameter("limit", "Maximum number of predictions", Required = false, Location = "query")] int? limit = null)
    {
        try
        {
            var predictions = await GetPreloadPredictions(limit ?? 50);

            return predictions;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get predictions: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/cache/patterns", "cache-patterns", "Get usage patterns", "codex.intelligent-caching")]
    public async Task<object> GetPatterns([ApiParameter("patternType", "Type of patterns to retrieve", Required = false, Location = "query")] string? patternType = null)
    {
        try
        {
            var patterns = await GetUsagePatterns(patternType);

            return patterns;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get patterns: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/cache/health", "cache-health", "Get cache health status", "codex.intelligent-caching")]
    public async Task<object> GetCacheHealth()
    {
        try
        {
            var health = await CalculateCacheHealth();

            return health;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get cache health: {ex.Message}");
        }
    }

    // Helper methods for intelligent caching
    private async Task<bool> PreloadConcept(string conceptId, string strategy, int priority)
    {
        lock (_cacheLock)
        {
            if (_cache.ContainsKey(conceptId))
            {
                return false; // Already cached
            }
        }

        try
        {
            // Simulate preloading concept data
            var conceptData = await LoadConceptData(conceptId);
            
            lock (_cacheLock)
            {
                _cache[conceptId] = new CacheEntry
                {
                    Key = conceptId,
                    Data = conceptData,
                    CreatedAt = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow,
                    AccessCount = 0,
                    Priority = priority,
                    Strategy = strategy
                };
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<string>> GetPredictedConcepts(int maxItems)
    {
        // Simulate prediction based on usage patterns
        var predictions = _predictions.Values
            .OrderByDescending(p => p.Confidence)
            .ThenByDescending(p => p.Frequency)
            .Take(maxItems)
            .Select(p => p.ConceptId)
            .ToList();

        return predictions;
    }

    private async Task<List<UsagePattern>> AnalyzeUsagePatterns(CachePatternAnalysisRequest request)
    {
        var patterns = new List<UsagePattern>();

        // Simulate pattern analysis
        var timeRange = ParseTimeRange(request.TimeRange);
        var relevantPatterns = _usagePatterns.Values
            .Where(p => p.LastSeen >= timeRange.Start && p.LastSeen <= timeRange.End)
            .Where(p => p.Frequency >= request.MinFrequency)
            .Take(request.MaxResults)
            .ToList();

        foreach (var pattern in relevantPatterns)
        {
            patterns.Add(pattern);
        }

        return patterns;
    }

    private List<CachePatternInsight> GeneratePatternInsights(List<UsagePattern> patterns)
    {
        var insights = new List<CachePatternInsight>();

        // Generate insights based on patterns
        if (patterns.Any())
        {
            var avgFrequency = patterns.Average(p => p.Frequency);
            var mostCommonType = patterns.GroupBy(p => p.Type).OrderByDescending(g => g.Count()).First().Key;

            insights.Add(new CachePatternInsight
            {
                Type = "frequency_analysis",
                Description = $"Average access frequency: {avgFrequency:F2}",
                Confidence = 0.8,
                Impact = "high"
            });

            insights.Add(new CachePatternInsight
            {
                Type = "pattern_type",
                Description = $"Most common pattern type: {mostCommonType}",
                Confidence = 0.9,
                Impact = "medium"
            });
        }

        return insights;
    }

    private List<OptimizationRecommendation> GenerateOptimizationRecommendations(List<UsagePattern> patterns, List<CachePatternInsight> insights)
    {
        var recommendations = new List<OptimizationRecommendation>();

        // Generate recommendations based on patterns and insights
        if (patterns.Count > 10)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Type = "cache_size",
                Description = "Consider increasing cache size due to high pattern diversity",
                Priority = "medium",
                EstimatedImpact = "15% performance improvement"
            });
        }

        if (insights.Any(i => i.Type == "frequency_analysis" && i.Impact == "high"))
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Type = "preload_strategy",
                Description = "Implement aggressive preloading for high-frequency concepts",
                Priority = "high",
                EstimatedImpact = "25% performance improvement"
            });
        }

        return recommendations;
    }

    private async Task<List<CacheOptimization>> ApplyCacheOptimizations(CacheOptimizationRequest request)
    {
        var optimizations = new List<CacheOptimization>();

        // Simulate applying optimizations
        switch (request.OptimizationStrategy)
        {
            case "memory_optimization":
                optimizations.Add(new CacheOptimization
                {
                    Type = "memory_cleanup",
                    Description = "Cleaned up unused cache entries",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "10% memory reduction"
                });
                break;

            case "preload_optimization":
                optimizations.Add(new CacheOptimization
                {
                    Type = "preload_tuning",
                    Description = "Tuned preload parameters based on patterns",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "20% faster preloading"
                });
                break;

            case "invalidation_optimization":
                optimizations.Add(new CacheOptimization
                {
                    Type = "smart_invalidation",
                    Description = "Implemented smart cache invalidation",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "15% fewer unnecessary invalidations"
                });
                break;
        }

        return optimizations;
    }

    private double CalculatePerformanceGain(List<CacheOptimization> optimizations)
    {
        // Simulate calculating performance gain
        return optimizations.Sum(o => ExtractPercentage(o.Impact));
    }

    private async Task<CacheMetrics> CalculateCacheMetrics(string? timeRange)
    {
        var timeRangeObj = ParseTimeRange(timeRange);
        var relevantMetrics = _cacheMetrics
            .Where(m => m.Timestamp >= timeRangeObj.Start && m.Timestamp <= timeRangeObj.End)
            .ToList();

        return new CacheMetrics
        {
            HitRate = relevantMetrics.Average(m => m.HitRate),
            MissRate = relevantMetrics.Average(m => m.MissRate),
            AverageResponseTime = relevantMetrics.Average(m => m.AverageResponseTime),
            CacheSize = _cache.Count,
            MemoryUsage = CalculateMemoryUsage(),
            PreloadAccuracy = CalculatePreloadAccuracy(),
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<int> PerformCacheInvalidation(CacheInvalidationRequest request)
    {
        var invalidatedCount = 0;

        lock (_cacheLock)
        {
            if (request.Keys != null && request.Keys.Length > 0)
            {
                // Invalidate specific keys
                foreach (var key in request.Keys)
                {
                    if (_cache.Remove(key))
                    {
                        invalidatedCount++;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(request.Pattern))
            {
                // Invalidate by pattern
                var keysToRemove = _cache.Keys.Where(k => k.Contains(request.Pattern)).ToList();
                foreach (var key in keysToRemove)
                {
                    if (_cache.Remove(key))
                    {
                        invalidatedCount++;
                    }
                }
            }
        }

        return invalidatedCount;
    }

    private async Task<List<PreloadPrediction>> GetPreloadPredictions(int limit)
    {
        return _predictions.Values
            .OrderByDescending(p => p.Confidence)
            .Take(limit)
            .ToList();
    }

    private async Task<List<UsagePattern>> GetUsagePatterns(string? patternType)
    {
        var patterns = _usagePatterns.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(patternType))
        {
            patterns = patterns.Where(p => p.Type == patternType);
        }

        return patterns.ToList();
    }

    private async Task<CacheHealthStatus> CalculateCacheHealth()
    {
        var hitRate = _cacheMetrics.Any() ? _cacheMetrics.Average(m => m.HitRate) : 0.0;
        var memoryUsage = CalculateMemoryUsage();
        var cacheSize = _cache.Count;

        var status = "healthy";
        if (hitRate < 0.7) status = "warning";
        if (hitRate < 0.5 || memoryUsage > 0.9) status = "critical";

        return new CacheHealthStatus
        {
            Status = status,
            HitRate = hitRate,
            MemoryUsage = memoryUsage,
            CacheSize = cacheSize,
            LastChecked = DateTime.UtcNow
        };
    }

    // Utility methods
    private async Task<object> LoadConceptData(string conceptId)
    {
        // Simulate loading concept data
        await Task.Delay(10); // Simulate network delay
        return new { id = conceptId, data = $"Concept data for {conceptId}", loadedAt = DateTime.UtcNow };
    }

    private (DateTime Start, DateTime End) ParseTimeRange(string? timeRange)
    {
        var now = DateTime.UtcNow;
        return timeRange switch
        {
            "1h" => (now.AddHours(-1), now),
            "24h" => (now.AddDays(-1), now),
            "7d" => (now.AddDays(-7), now),
            "30d" => (now.AddDays(-30), now),
            _ => (now.AddDays(-1), now)
        };
    }

    private double CalculateMemoryUsage()
    {
        // Simulate memory usage calculation
        return Math.Min(_cache.Count * 0.01, 1.0);
    }

    private double CalculatePreloadAccuracy()
    {
        // Simulate preload accuracy calculation
        return 0.85; // 85% accuracy
    }

    private double ExtractPercentage(string impact)
    {
        var match = System.Text.RegularExpressions.Regex.Match(impact, @"(\d+)%");
        return match.Success ? double.Parse(match.Groups[1].Value) / 100.0 : 0.0;
    }
}

// Intelligent Caching DTOs
public record PreloadRequest(
    string[]? ConceptIds,
    string PreloadStrategy,
    int Priority,
    int MaxItems
);

public record PreloadResponse(
    bool Success,
    int PreloadedCount,
    int SkippedCount,
    List<string> Errors,
    TimeSpan PreloadTime
);

public record CachePatternAnalysisRequest(
    string TimeRange,
    string[] PatternTypes,
    int MinFrequency,
    int MaxResults
);

public record CachePatternAnalysisResponse(
    bool Success,
    List<UsagePattern> Patterns,
    List<CachePatternInsight> Insights,
    List<OptimizationRecommendation> Recommendations,
    TimeSpan AnalysisTime
);

public record CacheOptimizationRequest(
    string OptimizationStrategy,
    Dictionary<string, object> TargetMetrics,
    Dictionary<string, object> Constraints
);

public record CacheOptimizationResponse(
    bool Success,
    List<CacheOptimization> OptimizationsApplied,
    double PerformanceGain,
    TimeSpan OptimizationTime
);

public record CacheMetricsResponse(
    bool Success,
    CacheMetrics Metrics,
    DateTime Timestamp,
    string TimeRange
);

public record CacheInvalidationRequest(
    string[]? Keys,
    string? Pattern,
    string Reason,
    bool Force
);

public record CacheInvalidationResponse(
    bool Success,
    int InvalidatedCount,
    TimeSpan InvalidationTime
);

// Supporting classes
public class CacheEntry
{
    public string Key { get; set; } = "";
    public object Data { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public int Priority { get; set; }
    public string Strategy { get; set; } = "";
}

public class UsagePattern
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string ConceptId { get; set; } = "";
    public int Frequency { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PreloadPrediction
{
    public string ConceptId { get; set; } = "";
    public double Confidence { get; set; }
    public int Frequency { get; set; }
    public string Reason { get; set; } = "";
    public DateTime PredictedAt { get; set; }
}

public class CacheMetrics
{
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public double AverageResponseTime { get; set; }
    public int CacheSize { get; set; }
    public double MemoryUsage { get; set; }
    public double PreloadAccuracy { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CachePatternInsight
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public double Confidence { get; set; }
    public string Impact { get; set; } = "";
}

public class OptimizationRecommendation
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "";
    public string EstimatedImpact { get; set; } = "";
}

public class CacheOptimization
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime AppliedAt { get; set; }
    public string Impact { get; set; } = "";
}

public class CacheHealthStatus
{
    public string Status { get; set; } = "";
    public double HitRate { get; set; }
    public double MemoryUsage { get; set; }
    public int CacheSize { get; set; }
    public DateTime LastChecked { get; set; }
}
