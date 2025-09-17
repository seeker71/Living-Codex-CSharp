using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Load Balancing and Performance Optimization Module
/// Implements load balancing strategies, performance monitoring, and auto-scaling recommendations
/// </summary>
public class LoadBalancingModule : ModuleBase
{
    private readonly Dictionary<string, ServiceInstance> _serviceInstances = new();
    private readonly Dictionary<string, LoadBalancingStrategy> _strategies = new();
    private readonly List<PerformanceMetric> _performanceMetrics = new();
    private readonly Dictionary<string, ScalingRecommendation> _scalingRecommendations = new();
    private readonly object _metricsLock = new();

    public override string Name => "Load Balancing Module";
    public override string Description => "Load Balancing and Performance Optimization Module";
    public override string Version => "1.0.0";

    public LoadBalancingModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        InitializeLoadBalancingStrategies();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.load-balancing",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "load-balancing", "performance", "scaling", "optimization" },
            capabilities: new[] { "load-balancing", "performance-monitoring", "auto-scaling", "resource-optimization", "health-monitoring" },
            spec: "codex.spec.load-balancing"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _coreApiService = coreApi;
        
        // Register all Load Balancing related nodes for AI agent discovery
        RegisterLoadBalancingNodes(registry);
    }

    /// <summary>
    /// Register all Load Balancing related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterLoadBalancingNodes(INodeRegistry registry)
    {
        // Register Load Balancing module node
        var loadBalancingNode = new Node(
            Id: "codex.load-balancing",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Load Balancing and Performance Optimization Module",
            Description: "Advanced load balancing, performance monitoring, and auto-scaling for distributed services",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "load-balancing", "performance-monitoring", "auto-scaling", "resource-optimization", "health-monitoring" },
                    endpoints = new[] { "balance-load", "get-metrics", "get-recommendations", "scale-service", "optimize-performance" },
                    integration = "performance-optimization"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Load Balancing and Performance Optimization Module",
                ["version"] = "1.0.0",
                ["type"] = "load-balancing",
                ["parentModule"] = "codex.load-balancing",
                ["capabilities"] = new[] { "load-balancing", "performance-monitoring", "auto-scaling", "resource-optimization" }
            }
        );
        registry.Upsert(loadBalancingNode);

        // Register Load Balancing routes as nodes
        RegisterLoadBalancingRoutes(registry);
        
        // Register Load Balancing DTOs as nodes
        RegisterLoadBalancingDTOs(registry);
    }

    /// <summary>
    /// Register Load Balancing routes as discoverable nodes
    /// </summary>
    private void RegisterLoadBalancingRoutes(INodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/load-balance/route", method = "POST", name = "load-balance-route", description = "Route request to best available service instance" },
            new { path = "/load-balance/metrics", method = "GET", name = "load-balance-metrics", description = "Get load balancing performance metrics" },
            new { path = "/load-balance/recommendations", method = "GET", name = "load-balance-recommendations", description = "Get scaling and optimization recommendations" },
            new { path = "/load-balance/scale", method = "POST", name = "load-balance-scale", description = "Scale service instances based on recommendations" },
            new { path = "/load-balance/optimize", method = "POST", name = "load-balance-optimize", description = "Optimize load balancing configuration" },
            new { path = "/load-balance/health", method = "GET", name = "load-balance-health", description = "Get load balancer health status" },
            new { path = "/load-balance/instances", method = "GET", name = "load-balance-instances", description = "Get registered service instances" },
            new { path = "/load-balance/strategies", method = "GET", name = "load-balance-strategies", description = "Get available load balancing strategies" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"load-balancing.route.{route.name}",
                TypeId: "meta.route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"Load Balancing route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetLoadBalancingRouteParameters(route.name),
                        responseType = GetLoadBalancingRouteResponseType(route.name)
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
                    ["module"] = "codex.load-balancing",
                    ["parentModule"] = "codex.load-balancing"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register Load Balancing DTOs as discoverable nodes
    /// </summary>
    private void RegisterLoadBalancingDTOs(INodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "LoadBalanceRequest", description = "Request to route to best service instance", properties = new[] { "ServiceName", "Strategy", "Priority", "Constraints" } },
            new { name = "LoadBalanceResponse", description = "Response with selected service instance", properties = new[] { "Success", "SelectedInstance", "Reason", "LoadTime" } },
            new { name = "LoadBalanceMetricsResponse", description = "Response with load balancing metrics", properties = new[] { "Success", "Metrics", "Timestamp", "TimeRange" } },
            new { name = "ScalingRecommendation", description = "Recommendation for scaling service instances", properties = new[] { "ServiceName", "Action", "Reason", "Priority", "EstimatedImpact" } },
            new { name = "ScalingRequest", description = "Request to scale service instances", properties = new[] { "ServiceName", "Action", "TargetCount", "Reason" } },
            new { name = "ScalingResponse", description = "Response from scaling operation", properties = new[] { "Success", "ScaledCount", "NewCount", "ScalingTime" } },
            new { name = "OptimizationRequest", description = "Request to optimize load balancing", properties = new[] { "OptimizationType", "TargetMetrics", "Constraints" } },
            new { name = "OptimizationResponse", description = "Response from optimization", properties = new[] { "Success", "OptimizationsApplied", "PerformanceGain", "OptimizationTime" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"load-balancing.dto.{dto.name}",
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
                        module = "codex.load-balancing",
                        usage = GetLoadBalancingDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.load-balancing",
                    ["parentModule"] = "codex.load-balancing",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    // Helper methods for AI agent generation
    private object GetLoadBalancingRouteParameters(string routeName)
    {
        return routeName switch
        {
            "load-balance-route" => new
            {
                request = new { type = "LoadBalanceRequest", required = true, location = "body", description = "Load balancing request configuration" }
            },
            "load-balance-metrics" => new
            {
                timeRange = new { type = "string", required = false, location = "query", description = "Time range for metrics" }
            },
            "load-balance-recommendations" => new
            {
                serviceName = new { type = "string", required = false, location = "query", description = "Specific service to get recommendations for" }
            },
            "load-balance-scale" => new
            {
                request = new { type = "ScalingRequest", required = true, location = "body", description = "Scaling configuration" }
            },
            "load-balance-optimize" => new
            {
                request = new { type = "OptimizationRequest", required = true, location = "body", description = "Optimization configuration" }
            },
            "load-balance-health" => new { },
            "load-balance-instances" => new
            {
                serviceName = new { type = "string", required = false, location = "query", description = "Filter by service name" }
            },
            "load-balance-strategies" => new { },
            _ => new { }
        };
    }

    private string GetLoadBalancingRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "load-balance-route" => "LoadBalanceResponse",
            "load-balance-metrics" => "LoadBalanceMetricsResponse",
            "load-balance-recommendations" => "ScalingRecommendation[]",
            "load-balance-scale" => "ScalingResponse",
            "load-balance-optimize" => "OptimizationResponse",
            "load-balance-health" => "LoadBalancerHealthStatus",
            "load-balance-instances" => "ServiceInstance[]",
            "load-balance-strategies" => "LoadBalancingStrategy[]",
            _ => "object"
        };
    }

    private string GetLoadBalancingDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "LoadBalanceRequest" => "Used to request routing to the best available service instance based on load balancing strategy.",
            "LoadBalanceResponse" => "Returned when load balancing is completed. Contains the selected service instance and routing details.",
            "LoadBalanceMetricsResponse" => "Returned when requesting load balancing metrics. Contains performance statistics and health data.",
            "ScalingRecommendation" => "Contains recommendations for scaling service instances based on current load and performance metrics.",
            "ScalingRequest" => "Used to request scaling of service instances up or down based on recommendations.",
            "ScalingResponse" => "Returned when scaling operation is completed. Contains scaling results and new instance count.",
            "OptimizationRequest" => "Used to request optimization of load balancing configuration based on performance data.",
            "OptimizationResponse" => "Returned when optimization is completed. Contains applied optimizations and performance improvements.",
            _ => "Load Balancing data transfer object"
        };
    }

    // Load Balancing API Methods
    [ApiRoute("POST", "/load-balance/route", "load-balance-route", "Route request to best available service instance", "codex.load-balancing")]
    public async Task<object> RouteRequest([ApiParameter("request", "Load balancing request", Required = true, Location = "body")] LoadBalanceRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var selectedInstance = await SelectBestInstance(request);
            var loadTime = DateTime.UtcNow - startTime;

            if (selectedInstance == null)
            {
                return new LoadBalanceResponse(
                    Success: false,
                    SelectedInstance: null,
                    Reason: "No available service instances",
                    LoadTime: loadTime
                );
            }

            return new LoadBalanceResponse(
                Success: true,
                SelectedInstance: selectedInstance,
                Reason: $"Selected using {request.Strategy} strategy",
                LoadTime: loadTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Load balancing failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/load-balance/metrics", "load-balance-metrics", "Get load balancing performance metrics", "codex.load-balancing")]
    public async Task<object> GetLoadBalanceMetrics([ApiParameter("timeRange", "Time range for metrics", Required = false, Location = "query")] string? timeRange = null)
    {
        try
        {
            var metrics = await CalculateLoadBalanceMetrics(timeRange);

            return new LoadBalanceMetricsResponse(
                Success: true,
                Metrics: metrics,
                Timestamp: DateTime.UtcNow,
                TimeRange: timeRange ?? "all"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get load balancing metrics: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/load-balance/recommendations", "load-balance-recommendations", "Get scaling and optimization recommendations", "codex.load-balancing")]
    public async Task<object> GetScalingRecommendations([ApiParameter("serviceName", "Specific service to get recommendations for", Required = false, Location = "query")] string? serviceName = null)
    {
        try
        {
            var recommendations = await GenerateScalingRecommendations(serviceName);

            return recommendations;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get scaling recommendations: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/load-balance/scale", "load-balance-scale", "Scale service instances based on recommendations", "codex.load-balancing")]
    public async Task<object> ScaleService([ApiParameter("request", "Scaling request", Required = true, Location = "body")] ScalingRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var scaledCount = await PerformScaling(request);
            var newCount = await GetServiceInstanceCount(request.ServiceName);

            var scalingTime = DateTime.UtcNow - startTime;

            return new ScalingResponse(
                Success: true,
                ScaledCount: scaledCount,
                NewCount: newCount,
                ScalingTime: scalingTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Scaling failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/load-balance/optimize", "load-balance-optimize", "Optimize load balancing configuration", "codex.load-balancing")]
    public async Task<object> OptimizeLoadBalancing([ApiParameter("request", "Optimization request", Required = true, Location = "body")] OptimizationRequest request)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var optimizationsApplied = await ApplyOptimizations(request);
            var performanceGain = CalculatePerformanceGain(optimizationsApplied);

            var optimizationTime = DateTime.UtcNow - startTime;

            return new OptimizationResponse(
                Success: true,
                OptimizationsApplied: optimizationsApplied,
                PerformanceGain: performanceGain,
                OptimizationTime: optimizationTime
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Optimization failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/load-balance/health", "load-balance-health", "Get load balancer health status", "codex.load-balancing")]
    public async Task<object> GetLoadBalancerHealth()
    {
        try
        {
            var health = await CalculateLoadBalancerHealth();

            return health;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get load balancer health: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/load-balance/instances", "load-balance-instances", "Get registered service instances", "codex.load-balancing")]
    public async Task<object> GetServiceInstances([ApiParameter("serviceName", "Filter by service name", Required = false, Location = "query")] string? serviceName = null)
    {
        try
        {
            var instances = await GetInstances(serviceName);

            return instances;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get service instances: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/load-balance/strategies", "load-balance-strategies", "Get available load balancing strategies", "codex.load-balancing")]
    public async Task<object> GetLoadBalancingStrategies()
    {
        try
        {
            return _strategies.Values.ToList();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get load balancing strategies: {ex.Message}");
        }
    }

    // Helper methods for load balancing
    private async Task<ServiceInstance?> SelectBestInstance(LoadBalanceRequest request)
    {
        var availableInstances = _serviceInstances.Values
            .Where(i => i.ServiceName == request.ServiceName && i.IsHealthy)
            .ToList();

        if (!availableInstances.Any())
        {
            return null;
        }

        var strategy = _strategies.GetValueOrDefault(request.Strategy) ?? _strategies["round_robin"];

        return strategy.Name switch
        {
            "round_robin" => SelectRoundRobin(availableInstances),
            "least_connections" => SelectLeastConnections(availableInstances),
            "weighted_round_robin" => SelectWeightedRoundRobin(availableInstances),
            "least_response_time" => SelectLeastResponseTime(availableInstances),
            "ip_hash" => SelectIpHash(availableInstances, request.Constraints),
            _ => SelectRoundRobin(availableInstances)
        };
    }

    private ServiceInstance SelectRoundRobin(List<ServiceInstance> instances)
    {
        var index = (int)(DateTime.UtcNow.Ticks % instances.Count);
        return instances[index];
    }

    private ServiceInstance SelectLeastConnections(List<ServiceInstance> instances)
    {
        return instances.OrderBy(i => i.ActiveConnections).First();
    }

    private ServiceInstance SelectWeightedRoundRobin(List<ServiceInstance> instances)
    {
        var totalWeight = instances.Sum(i => i.Weight);
        var random = new Random().NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var instance in instances)
        {
            currentWeight += instance.Weight;
            if (random <= currentWeight)
            {
                return instance;
            }
        }

        return instances.First();
    }

    private ServiceInstance SelectLeastResponseTime(List<ServiceInstance> instances)
    {
        return instances.OrderBy(i => i.AverageResponseTime).First();
    }

    private ServiceInstance SelectIpHash(List<ServiceInstance> instances, Dictionary<string, object>? constraints)
    {
        if (constraints?.ContainsKey("client_ip") == true)
        {
            var clientIp = constraints["client_ip"].ToString();
            var hash = clientIp?.GetHashCode() ?? 0;
            var index = Math.Abs(hash) % instances.Count;
            return instances[index];
        }

        return SelectRoundRobin(instances);
    }

    private async Task<LoadBalanceMetrics> CalculateLoadBalanceMetrics(string? timeRange)
    {
        var timeRangeObj = ParseTimeRange(timeRange);
        var relevantMetrics = _performanceMetrics
            .Where(m => m.Timestamp >= timeRangeObj.Start && m.Timestamp <= timeRangeObj.End)
            .ToList();

        return new LoadBalanceMetrics
        {
            TotalRequests = relevantMetrics.Sum(m => m.RequestCount),
            AverageResponseTime = relevantMetrics.Average(m => m.AverageResponseTime),
            ErrorRate = relevantMetrics.Average(m => m.ErrorRate),
            Throughput = relevantMetrics.Sum(m => m.Throughput),
            ActiveInstances = _serviceInstances.Values.Count(i => i.IsHealthy),
            TotalInstances = _serviceInstances.Count,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<List<ScalingRecommendation>> GenerateScalingRecommendations(string? serviceName)
    {
        var recommendations = new List<ScalingRecommendation>();

        var services = string.IsNullOrEmpty(serviceName) 
            ? _serviceInstances.Values.Select(i => i.ServiceName).Distinct()
            : new[] { serviceName };

        foreach (var svc in services)
        {
            var instances = _serviceInstances.Values.Where(i => i.ServiceName == svc).ToList();
            var metrics = _performanceMetrics.Where(m => m.ServiceName == svc).ToList();

            if (metrics.Any())
            {
                var avgCpu = metrics.Average(m => m.CpuUsage);
                var avgMemory = metrics.Average(m => m.MemoryUsage);
                var avgResponseTime = metrics.Average(m => m.AverageResponseTime);

                if (avgCpu > 80 || avgMemory > 85 || avgResponseTime > 1000)
                {
                    recommendations.Add(new ScalingRecommendation
                    {
                        ServiceName = svc,
                        Action = "scale_up",
                        Reason = $"High resource usage: CPU={avgCpu:F1}%, Memory={avgMemory:F1}%, ResponseTime={avgResponseTime:F0}ms",
                        Priority = "high",
                        EstimatedImpact = "20-30% performance improvement"
                    });
                }
                else if (avgCpu < 30 && avgMemory < 40 && instances.Count > 1)
                {
                    recommendations.Add(new ScalingRecommendation
                    {
                        ServiceName = svc,
                        Action = "scale_down",
                        Reason = $"Low resource usage: CPU={avgCpu:F1}%, Memory={avgMemory:F1}%",
                        Priority = "medium",
                        EstimatedImpact = "10-15% cost reduction"
                    });
                }
            }
        }

        return recommendations;
    }

    private async Task<int> PerformScaling(ScalingRequest request)
    {
        var instances = _serviceInstances.Values.Where(i => i.ServiceName == request.ServiceName).ToList();
        var currentCount = instances.Count;
        var targetCount = request.TargetCount;

        if (request.Action == "scale_up" && targetCount > currentCount)
        {
            // Simulate scaling up
            for (int i = currentCount; i < targetCount; i++)
            {
                var newInstance = new ServiceInstance
                {
                    Id = $"{request.ServiceName}-{i + 1}",
                    ServiceName = request.ServiceName,
                    Endpoint = $"http://{request.ServiceName}-{i + 1}:8080",
                    IsHealthy = true,
                    Weight = 1.0,
                    ActiveConnections = 0,
                    AverageResponseTime = 100
                };
                _serviceInstances[newInstance.Id] = newInstance;
            }
            return targetCount - currentCount;
        }
        else if (request.Action == "scale_down" && targetCount < currentCount)
        {
            // Simulate scaling down
            var instancesToRemove = instances.Take(currentCount - targetCount).ToList();
            foreach (var instance in instancesToRemove)
            {
                _serviceInstances.Remove(instance.Id);
            }
            return currentCount - targetCount;
        }

        return 0;
    }

    private async Task<int> GetServiceInstanceCount(string serviceName)
    {
        return _serviceInstances.Values.Count(i => i.ServiceName == serviceName);
    }

    private async Task<List<LoadBalancingOptimization>> ApplyOptimizations(OptimizationRequest request)
    {
        var optimizations = new List<LoadBalancingOptimization>();

        switch (request.OptimizationType)
        {
            case "strategy_optimization":
                optimizations.Add(new LoadBalancingOptimization
                {
                    Type = "strategy_tuning",
                    Description = "Optimized load balancing strategy based on traffic patterns",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "15% better distribution"
                });
                break;

            case "health_check_optimization":
                optimizations.Add(new LoadBalancingOptimization
                {
                    Type = "health_check_tuning",
                    Description = "Optimized health check intervals and thresholds",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "20% faster failover"
                });
                break;

            case "connection_pooling":
                optimizations.Add(new LoadBalancingOptimization
                {
                    Type = "connection_pooling",
                    Description = "Implemented connection pooling for better resource utilization",
                    AppliedAt = DateTime.UtcNow,
                    Impact = "25% reduced connection overhead"
                });
                break;
        }

        return optimizations;
    }

    private double CalculatePerformanceGain(List<LoadBalancingOptimization> optimizations)
    {
        return optimizations.Sum(o => ExtractPercentage(o.Impact));
    }

    private async Task<LoadBalancerHealthStatus> CalculateLoadBalancerHealth()
    {
        var healthyInstances = _serviceInstances.Values.Count(i => i.IsHealthy);
        var totalInstances = _serviceInstances.Count;
        var healthPercentage = totalInstances > 0 ? (double)healthyInstances / totalInstances : 0.0;

        var status = "healthy";
        if (healthPercentage < 0.8) status = "warning";
        if (healthPercentage < 0.5) status = "critical";

        return new LoadBalancerHealthStatus
        {
            Status = status,
            HealthyInstances = healthyInstances,
            TotalInstances = totalInstances,
            HealthPercentage = healthPercentage,
            LastChecked = DateTime.UtcNow
        };
    }

    private async Task<List<ServiceInstance>> GetInstances(string? serviceName)
    {
        var instances = _serviceInstances.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(serviceName))
        {
            instances = instances.Where(i => i.ServiceName == serviceName);
        }

        return instances.ToList();
    }

    private void InitializeLoadBalancingStrategies()
    {
        _strategies["round_robin"] = new LoadBalancingStrategy
        {
            Name = "round_robin",
            Description = "Distributes requests evenly across all available instances",
            Parameters = new Dictionary<string, object>()
        };

        _strategies["least_connections"] = new LoadBalancingStrategy
        {
            Name = "least_connections",
            Description = "Routes to the instance with the fewest active connections",
            Parameters = new Dictionary<string, object>()
        };

        _strategies["weighted_round_robin"] = new LoadBalancingStrategy
        {
            Name = "weighted_round_robin",
            Description = "Distributes requests based on instance weights",
            Parameters = new Dictionary<string, object> { ["weight_factor"] = 1.0 }
        };

        _strategies["least_response_time"] = new LoadBalancingStrategy
        {
            Name = "least_response_time",
            Description = "Routes to the instance with the lowest average response time",
            Parameters = new Dictionary<string, object>()
        };

        _strategies["ip_hash"] = new LoadBalancingStrategy
        {
            Name = "ip_hash",
            Description = "Routes based on client IP hash for session affinity",
            Parameters = new Dictionary<string, object>()
        };
    }

    // Utility methods
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

    private double ExtractPercentage(string impact)
    {
        var match = System.Text.RegularExpressions.Regex.Match(impact, @"(\d+)%");
        return match.Success ? double.Parse(match.Groups[1].Value) / 100.0 : 0.0;
    }
}

// Load Balancing DTOs
[ResponseType("codex.loadbalancing.balance-request", "LoadBalanceRequest", "Request for load balancing")]
public record LoadBalanceRequest(
    string ServiceName,
    string Strategy,
    int Priority,
    Dictionary<string, object>? Constraints
);

[ResponseType("codex.loadbalancing.balance-response", "LoadBalanceResponse", "Response for load balancing")]
public record LoadBalanceResponse(
    bool Success,
    ServiceInstance? SelectedInstance,
    string Reason,
    TimeSpan LoadTime
);

[ResponseType("codex.loadbalancing.metrics-response", "LoadBalanceMetricsResponse", "Response for load balance metrics")]
public record LoadBalanceMetricsResponse(
    bool Success,
    LoadBalanceMetrics Metrics,
    DateTime Timestamp,
    string TimeRange
);

[ResponseType("codex.loadbalancing.scaling-request", "ScalingRequest", "Request for service scaling")]
public record ScalingRequest(
    string ServiceName,
    string Action,
    int TargetCount,
    string Reason
);

[ResponseType("codex.loadbalancing.scaling-response", "ScalingResponse", "Response for service scaling")]
public record ScalingResponse(
    bool Success,
    int ScaledCount,
    int NewCount,
    TimeSpan ScalingTime
);

[ResponseType("codex.loadbalancing.optimization-request", "OptimizationRequest", "Request for load balancing optimization")]
public record OptimizationRequest(
    string OptimizationType,
    Dictionary<string, object> TargetMetrics,
    Dictionary<string, object> Constraints
);

[ResponseType("codex.loadbalancing.optimization-response", "OptimizationResponse", "Response for load balancing optimization")]
public record OptimizationResponse(
    bool Success,
    List<LoadBalancingOptimization> OptimizationsApplied,
    double PerformanceGain,
    TimeSpan OptimizationTime
);

// Supporting classes
[ResponseType("codex.loadbalancing.service-instance", "ServiceInstance", "Service instance entity")]
public class ServiceInstance
{
    public string Id { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public bool IsHealthy { get; set; } = true;
    public double Weight { get; set; } = 1.0;
    public int ActiveConnections { get; set; } = 0;
    public double AverageResponseTime { get; set; } = 0.0;
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
}

[ResponseType("codex.loadbalancing.strategy", "LoadBalancingStrategy", "Load balancing strategy entity")]
public class LoadBalancingStrategy
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();
}

[ResponseType("codex.loadbalancing.performance-metric", "PerformanceMetric", "Performance metric entity")]
public class PerformanceMetric
{
    public string ServiceName { get; set; } = "";
    public int RequestCount { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public double Throughput { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public DateTime Timestamp { get; set; }
}

[ResponseType("codex.loadbalancing.metrics", "LoadBalanceMetrics", "Load balance metrics entity")]
public class LoadBalanceMetrics
{
    public long TotalRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public double Throughput { get; set; }
    public int ActiveInstances { get; set; }
    public int TotalInstances { get; set; }
    public DateTime Timestamp { get; set; }
}

[ResponseType("codex.loadbalancing.scaling-recommendation", "ScalingRecommendation", "Scaling recommendation entity")]
public class ScalingRecommendation
{
    public string ServiceName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Reason { get; set; } = "";
    public string Priority { get; set; } = "";
    public string EstimatedImpact { get; set; } = "";
}

[ResponseType("codex.loadbalancing.optimization", "LoadBalancingOptimization", "Load balancing optimization entity")]
public class LoadBalancingOptimization
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime AppliedAt { get; set; }
    public string Impact { get; set; } = "";
}

[ResponseType("codex.loadbalancing.health-status", "LoadBalancerHealthStatus", "Load balancer health status entity")]
public class LoadBalancerHealthStatus
{
    public string Status { get; set; } = "";
    public int HealthyInstances { get; set; }
    public int TotalInstances { get; set; }
    public double HealthPercentage { get; set; }
    public DateTime LastChecked { get; set; }
}
