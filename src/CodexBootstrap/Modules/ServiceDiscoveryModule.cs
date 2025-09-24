using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Service Discovery Module - Modular Fractal API Design
/// Manages service registration, discovery, and health monitoring
/// </summary>
public class ServiceDiscoveryModule : ModuleBase
{
    private readonly Dictionary<string, ServiceInfo> _registeredServices = new();
    private readonly Dictionary<string, ServiceHealth> _serviceHealth = new();

    public override string Name => "Service Discovery Module";
    public override string Description => "Modular Fractal API Design - Manages service registration, discovery, and health monitoring";
    public override string Version => "1.0.0";

    public ServiceDiscoveryModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        // _apiRouter will be set via RegisterApiHandlers in ModuleBase
    }


    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.service.discovery",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "service-discovery", "microservices", "registration" },
            capabilities: new[] { "service-registration", "service-discovery", "health-monitoring" },
            spec: "codex.spec.service-discovery"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store CoreApiService reference for inter-module communication
        _coreApiService = coreApi;
        
        // Register all API Gateway related nodes for AI agent discovery
        RegisterApiGatewayNodes(registry);
        
        // HTTP endpoints are now registered automatically by the attribute discovery system
    }

    /// <summary>
    /// Register all API Gateway related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterApiGatewayNodes(INodeRegistry registry)
    {
        // Register API Gateway module node using CreateModuleNode
        var apiGatewayNode = CreateModuleNode(
            moduleId: "codex.api-gateway",
            name: "API Gateway Module",
            version: "1.0.0",
            description: "Integrated API Gateway functionality within Service Discovery Module - provides routing, load balancing, and service discovery",
            tags: new[] { "api-gateway", "routing", "load-balancing", "service-discovery" },
            capabilities: new[] { "service-discovery", "load-balancing", "routing", "health-monitoring", "circuit-breaker" },
            spec: "codex.spec.api-gateway"
        );
        registry.Upsert(apiGatewayNode);

        // Register API Gateway routes as nodes
        RegisterApiGatewayRoutes(registry);
        
        // Register API Gateway DTOs as nodes
        RegisterApiGatewayDTOs(registry);
        
        // Register API Gateway classes as nodes
        RegisterApiGatewayClasses(registry);
    }

    /// <summary>
    /// Register API Gateway routes as discoverable nodes
    /// </summary>
    private void RegisterApiGatewayRoutes(INodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/gateway/route", method = "POST", name = "gateway-route", description = "Route request to appropriate service" },
            new { path = "/gateway/load-balance/{serviceType}", method = "GET", name = "gateway-load-balance", description = "Get load-balanced service" },
            new { path = "/gateway/health", method = "GET", name = "gateway-health", description = "Get gateway health status" },
            new { path = "/gateway/discover/route/{path}", method = "GET", name = "gateway-discover-route", description = "Discover route for path" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"codex.service-discovery.route.{route.name}.{Guid.NewGuid():N}",
                TypeId: "codex.meta/route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"API Gateway route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetRouteParameters(route.name),
                        responseType = GetRouteResponseType(route.name),
                        example = GetRouteExample(route.name)
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
                    ["module"] = "codex.api-gateway",
                    ["parentModule"] = "codex.service.discovery"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register API Gateway DTOs as discoverable nodes
    /// </summary>
    private void RegisterApiGatewayDTOs(INodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "GatewayRouteRequest", description = "Request for routing to a service", properties = new[] { "ServiceType", "Path", "Endpoint", "Method", "Payload" } },
            new { name = "GatewayRouteResponse", description = "Response from routing request", properties = new[] { "Success", "ServiceId", "ServiceUrl", "Endpoint", "Method", "Timestamp", "Message" } },
            new { name = "GatewayHealthResponse", description = "Gateway health status response", properties = new[] { "Status", "TotalServices", "HealthyServices", "UnhealthyServices", "Services", "Message" } },
            new { name = "RouteDiscoveryResponse", description = "Response from route discovery", properties = new[] { "Path", "MatchingServices", "Count", "Message" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"codex.service-discovery.dto.{dto.name}.{Guid.NewGuid():N}",
                TypeId: "codex.meta/type",
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
                        module = "codex.api-gateway",
                        usage = GetDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.api-gateway",
                    ["parentModule"] = "codex.service.discovery",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    /// <summary>
    /// Register API Gateway classes as discoverable nodes
    /// </summary>
    private void RegisterApiGatewayClasses(INodeRegistry registry)
    {
        var classes = new[]
        {
            new { name = "GatewayRouteResult", description = "Result of routing a request to a service", properties = new[] { "ServiceId", "ServiceUrl", "Endpoint", "Method", "Timestamp", "Success" } }
        };

        foreach (var cls in classes)
        {
            var classNode = new Node(
                Id: $"codex.service-discovery.class.{cls.name}.{Guid.NewGuid():N}",
                TypeId: "codex.meta/type",
                State: ContentState.Ice,
                Locale: "en",
                Title: cls.name,
                Description: cls.description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = cls.name,
                        description = cls.description,
                        properties = cls.properties,
                        type = "class",
                        module = "codex.api-gateway",
                        usage = GetClassUsage(cls.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = cls.name,
                    ["description"] = cls.description,
                    ["type"] = "class",
                    ["module"] = "codex.api-gateway",
                    ["parentModule"] = "codex.service.discovery",
                    ["properties"] = cls.properties
                }
            );
            registry.Upsert(classNode);
        }
    }

    /// <summary>
    /// Get route parameters for AI agent generation
    /// </summary>
    private object GetRouteParameters(string routeName)
    {
        return routeName switch
        {
            "gateway-route" => new
            {
                request = new { type = "GatewayRouteRequest", required = true, location = "body", description = "Route request details" }
            },
            "gateway-load-balance" => new
            {
                serviceType = new { type = "string", required = true, location = "path", description = "Type of service to load balance" }
            },
            "gateway-health" => new { },
            "gateway-discover-route" => new
            {
                path = new { type = "string", required = true, location = "path", description = "Path to discover routes for" }
            },
            _ => new { }
        };
    }

    /// <summary>
    /// Get route response type for AI agent generation
    /// </summary>
    private string GetRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "gateway-route" => "GatewayRouteResponse",
            "gateway-load-balance" => "ServiceInfoResponse",
            "gateway-health" => "GatewayHealthResponse",
            "gateway-discover-route" => "RouteDiscoveryResponse",
            _ => "object"
        };
    }

    /// <summary>
    /// Get route example for AI agent generation
    /// </summary>
    private object GetRouteExample(string routeName)
    {
        return routeName switch
        {
            "gateway-route" => new
            {
                request = new
                {
                    serviceType = "concept-translation",
                    path = "/translate",
                    endpoint = "translate",
                    method = "POST",
                    payload = new { concept = "unity", targetLanguage = "spanish" }
                },
                response = new
                {
                    success = true,
                    serviceId = "translation-service-1",
                    serviceUrl = "http://translation-service-1:5000/translate",
                    endpoint = "translate",
                    method = "POST",
                    timestamp = "2024-01-01T00:00:00Z",
                    message = "Request routed successfully"
                }
            },
            "gateway-load-balance" => new
            {
                response = new
                {
                    success = true,
                    service = new
                    {
                        serviceId = "translation-service-1",
                        serviceType = "concept-translation",
                        baseUrl = "http://translation-service-1:5000",
                        capabilities = new { routes = "/translate,/analyze" },
                        health = new { status = "Healthy", lastCheck = "2024-01-01T00:00:00Z" },
                        lastSeen = "2024-01-01T00:00:00Z"
                    },
                    message = "Load-balanced service found"
                }
            },
            "gateway-health" => new
            {
                status = "healthy",
                totalServices = 5,
                healthyServices = 5,
                unhealthyServices = 0,
                services = new[] { new { status = "Healthy", lastCheck = "2024-01-01T00:00:00Z" } },
                message = "Gateway status: healthy"
            },
            "gateway-discover-route" => new
            {
                path = "/translate",
                matchingServices = new[]
                {
                    new
                    {
                        serviceId = "translation-service-1",
                        serviceType = "concept-translation",
                        baseUrl = "http://translation-service-1:5000",
                        capabilities = new { routes = "/translate,/analyze" }
                    }
                },
                count = 1,
                message = "Found 1 services matching path '/translate'"
            },
            _ => new { }
        };
    }

    /// <summary>
    /// Get DTO usage information for AI agent generation
    /// </summary>
    private string GetDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "GatewayRouteRequest" => "Used to request routing to a specific service. Contains service type, path, endpoint, method, and payload.",
            "GatewayRouteResponse" => "Returned when a request is successfully routed to a service. Contains routing details and service information.",
            "GatewayHealthResponse" => "Provides overall gateway health status including service counts and individual service health.",
            "RouteDiscoveryResponse" => "Returned when discovering which services can handle a specific path.",
            _ => "API Gateway data transfer object"
        };
    }

    /// <summary>
    /// Get class usage information for AI agent generation
    /// </summary>
    private string GetClassUsage(string className)
    {
        return className switch
        {
            "GatewayRouteResult" => "Internal class used to represent the result of routing a request to a service. Contains service details and routing information.",
            _ => "API Gateway class"
        };
    }

    /// <summary>
    /// Register a service with the discovery system
    /// </summary>
    [ApiRoute("POST", "/service/register", "service-register", "Register a new service", "codex.service.discovery")]
    public async Task<object> RegisterService([ApiParameter("request", "Service registration request", Required = true, Location = "body")] ServiceRegistrationRequest request)
    {
        try
        {
            var serviceInfo = new ServiceInfo(
                ServiceId: request.ServiceId,
                ServiceType: request.ServiceType,
                BaseUrl: request.BaseUrl,
                Capabilities: request.Capabilities,
                Health: new ServiceHealth("Healthy", DateTime.UtcNow, null),
                LastSeen: DateTime.UtcNow
            );

            _registeredServices[request.ServiceId] = serviceInfo;
            _serviceHealth[request.ServiceId] = serviceInfo.Health;

            // Store as node in registry for persistence
            var serviceNode = new Node(
                Id: $"codex.service.{request.ServiceId}.{Guid.NewGuid():N}",
                TypeId: "service",
                State: ContentState.Water,
                Locale: "en",
                Title: serviceInfo.ServiceId,
                Description: $"Service {serviceInfo.ServiceType}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(serviceInfo),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["serviceId"] = serviceInfo.ServiceId,
                    ["serviceType"] = serviceInfo.ServiceType,
                    ["baseUrl"] = serviceInfo.BaseUrl,
                    ["capabilities"] = serviceInfo.Capabilities,
                    ["lastSeen"] = serviceInfo.LastSeen
                }
            );
            _registry.Upsert(serviceNode);

            return new ServiceRegistrationResponse(
                Success: true,
                ServiceId: request.ServiceId,
                Message: "Service registered successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to register service: {ex.Message}", "SERVICE_REGISTRATION_ERROR");
        }
    }

    /// <summary>
    /// Discover services by type
    /// </summary>
    [ApiRoute("GET", "/service/discover/{serviceType}", "service-discover", "Discover services by type", "codex.service.discovery")]
    public async Task<object> DiscoverServices([ApiParameter("serviceType", "Type of service to discover", Required = true, Location = "path")] string serviceType)
    {
        try
        {
            var services = _registeredServices.Values
                .Where(s => s.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase))
                .Where(s => IsServiceHealthy(s.ServiceId))
                .ToList();

            return new ServiceDiscoveryResponse(
                Success: true,
                Services: services,
                Count: services.Count,
                Message: $"Found {services.Count} services of type '{serviceType}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to discover services: {ex.Message}", "SERVICE_DISCOVERY_ERROR");
        }
    }

    /// <summary>
    /// Get all registered services
    /// </summary>
    [ApiRoute("GET", "/service/list", "service-list", "Get all registered services", "codex.service.discovery")]
    public async Task<object> ListServices()
    {
        try
        {
            var services = _registeredServices.Values.ToList();
            return new ServiceListResponse(
                Success: true,
                Services: services,
                Count: services.Count,
                Message: $"Found {services.Count} registered services"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to list services: {ex.Message}", "SERVICE_LIST_ERROR");
        }
    }

    /// <summary>
    /// Get service information by ID
    /// </summary>
    [ApiRoute("GET", "/service/{serviceId}", "service-get", "Get service information by ID", "codex.service.discovery")]
    public async Task<object> GetService([ApiParameter("serviceId", "Service ID", Required = true, Location = "path")] string serviceId)
    {
        try
        {
            if (!_registeredServices.TryGetValue(serviceId, out var service))
            {
                return ResponseHelpers.CreateErrorResponse($"Service '{serviceId}' not found", "SERVICE_NOT_FOUND");
            }

            return new ServiceInfoResponse(
                Success: true,
                Service: service,
                Message: "Service information retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get service: {ex.Message}", "SERVICE_GET_ERROR");
        }
    }

    /// <summary>
    /// Update service health status
    /// </summary>
    [ApiRoute("POST", "/service/health", "service-health-update", "Update service health status", "codex.service.discovery")]
    public async Task<object> UpdateServiceHealth([ApiParameter("request", "Health update request", Required = true, Location = "body")] ServiceHealthUpdateRequest request)
    {
        try
        {
            if (!_registeredServices.TryGetValue(request.ServiceId, out var service))
            {
                return ResponseHelpers.CreateErrorResponse($"Service '{request.ServiceId}' not found", "SERVICE_NOT_FOUND");
            }

            var health = new ServiceHealth(
                Status: request.Status,
                LastCheck: DateTime.UtcNow,
                Error: request.Error
            );

            _serviceHealth[request.ServiceId] = health;

            // Update service info
            var updatedService = service with { Health = health, LastSeen = DateTime.UtcNow };
            _registeredServices[request.ServiceId] = updatedService;

            // Update node in registry
            var serviceNode = new Node(
                Id: $"codex.service.{request.ServiceId}.{Guid.NewGuid():N}",
                TypeId: "service",
                State: ContentState.Water,
                Locale: "en",
                Title: updatedService.ServiceId,
                Description: $"Service {updatedService.ServiceType}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(updatedService),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["serviceId"] = updatedService.ServiceId,
                    ["serviceType"] = updatedService.ServiceType,
                    ["baseUrl"] = updatedService.BaseUrl,
                    ["capabilities"] = updatedService.Capabilities,
                    ["lastSeen"] = updatedService.LastSeen,
                    ["healthStatus"] = health.Status,
                    ["lastHealthCheck"] = health.LastCheck
                }
            );
            _registry.Upsert(serviceNode);

            return new ServiceHealthUpdateResponse(
                Success: true,
                ServiceId: request.ServiceId,
                Health: health,
                Message: "Service health updated successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to update service health: {ex.Message}", "SERVICE_HEALTH_UPDATE_ERROR");
        }
    }

    /// <summary>
    /// Check if a service is healthy
    /// </summary>
    [ApiRoute("GET", "/service/{serviceId}/health", "service-health-check", "Check service health", "codex.service.discovery")]
    public async Task<object> CheckServiceHealth([ApiParameter("serviceId", "Service ID", Required = true, Location = "path")] string serviceId)
    {
        try
        {
            if (!_registeredServices.TryGetValue(serviceId, out var service))
            {
                return ResponseHelpers.CreateErrorResponse($"Service '{serviceId}' not found", "SERVICE_NOT_FOUND");
            }

            var isHealthy = IsServiceHealthy(serviceId);
            var health = _serviceHealth.GetValueOrDefault(serviceId, new ServiceHealth("Unknown", DateTime.MinValue, null));

            return new ServiceHealthCheckResponse(
                Success: true,
                ServiceId: serviceId,
                IsHealthy: isHealthy,
                Health: health,
                Message: isHealthy ? "Service is healthy" : "Service is unhealthy"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to check service health: {ex.Message}", "SERVICE_HEALTH_CHECK_ERROR");
        }
    }

    /// <summary>
    /// Unregister a service
    /// </summary>
    [ApiRoute("DELETE", "/service/{serviceId}", "service-unregister", "Unregister a service", "codex.service.discovery")]
    public async Task<object> UnregisterService([ApiParameter("serviceId", "Service ID", Required = true, Location = "path")] string serviceId)
    {
        try
        {
            if (!_registeredServices.ContainsKey(serviceId))
            {
                return ResponseHelpers.CreateErrorResponse($"Service '{serviceId}' not found", "SERVICE_NOT_FOUND");
            }

            _registeredServices.Remove(serviceId);
            _serviceHealth.Remove(serviceId);

            // Remove from registry - try to get and remove the node
            if (_registry.TryGet($"service:{serviceId}", out var nodeToRemove))
            {
                // Mark as deleted by updating its state or remove from storage
                var deletedNode = nodeToRemove with { State = ContentState.Gas };
                _registry.Upsert(deletedNode);
            }

            return new ServiceUnregistrationResponse(
                Success: true,
                ServiceId: serviceId,
                Message: "Service unregistered successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to unregister service: {ex.Message}", "SERVICE_UNREGISTRATION_ERROR");
        }
    }

    /// <summary>
    /// Get services by capability
    /// </summary>
    [ApiRoute("GET", "/service/capability/{capability}", "service-by-capability", "Get services by capability", "codex.service.discovery")]
    public async Task<object> GetServicesByCapability([ApiParameter("capability", "Capability to search for", Required = true, Location = "path")] string capability)
    {
        try
        {
            var services = _registeredServices.Values
                .Where(s => s.Capabilities.ContainsKey(capability))
                .Where(s => IsServiceHealthy(s.ServiceId))
                .ToList();

            return new ServiceCapabilityResponse(
                Success: true,
                Capability: capability,
                Services: services,
                Count: services.Count,
                Message: $"Found {services.Count} services with capability '{capability}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get services by capability: {ex.Message}", "SERVICE_CAPABILITY_ERROR");
        }
    }

    /// <summary>
    /// Route request to appropriate service (API Gateway functionality)
    /// </summary>
    [ApiRoute("POST", "/gateway/route", "gateway-route", "Route request to appropriate service", "codex.service.discovery")]
    public async Task<object> RouteRequest([ApiParameter("request", "Route request", Required = true, Location = "body")] GatewayRouteRequest request)
    {
        try
        {
            // Find the best service for this request
            var service = await FindBestServiceAsync(request.ServiceType, request.Path);
            if (service == null)
            {
                return ResponseHelpers.CreateErrorResponse("No suitable service found", "SERVICE_NOT_FOUND");
            }

            // Route the request
            var result = await RouteToServiceInternalAsync(service, request);
            return new GatewayRouteResponse(
                Success: true,
                ServiceId: service.ServiceId,
                ServiceUrl: result.ServiceUrl,
                Endpoint: result.Endpoint,
                Method: result.Method,
                Timestamp: result.Timestamp,
                Message: "Request routed successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Routing failed: {ex.Message}", "ROUTING_ERROR");
        }
    }

    /// <summary>
    /// Get load-balanced service for a specific type
    /// </summary>
    [ApiRoute("GET", "/gateway/load-balance/{serviceType}", "gateway-load-balance", "Get load-balanced service", "codex.service.discovery")]
    public async Task<object> GetLoadBalancedService([ApiParameter("serviceType", "Service type", Required = true, Location = "path")] string serviceType)
    {
        try
        {
            var service = await FindBestServiceAsync(serviceType, null);
            if (service == null)
            {
                return ResponseHelpers.CreateErrorResponse("No healthy services found", "SERVICE_NOT_FOUND");
            }

            return new ServiceInfoResponse(
                Success: true,
                Service: service,
                Message: "Load-balanced service found"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Load balancing failed: {ex.Message}", "LOAD_BALANCE_ERROR");
        }
    }

    /// <summary>
    /// Get gateway health status
    /// </summary>
    [ApiRoute("GET", "/gateway/health", "gateway-health", "Get gateway health status", "codex.service.discovery")]
    public async Task<object> GetGatewayHealth()
    {
        try
        {
            var totalServices = _registeredServices.Count;
            var healthyServices = _registeredServices.Values.Count(s => IsServiceHealthy(s.ServiceId));
            var unhealthyServices = totalServices - healthyServices;

            return new GatewayHealthResponse(
                Status: unhealthyServices == 0 ? "healthy" : "degraded",
                TotalServices: totalServices,
                HealthyServices: healthyServices,
                UnhealthyServices: unhealthyServices,
                Services: _serviceHealth.Values.ToList(),
                Message: $"Gateway status: {(unhealthyServices == 0 ? "healthy" : "degraded")}"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get gateway health: {ex.Message}", "GATEWAY_HEALTH_ERROR");
        }
    }

    /// <summary>
    /// Discover route for a specific path
    /// </summary>
    [ApiRoute("GET", "/gateway/discover/route/{path}", "gateway-discover-route", "Discover route for path", "codex.service.discovery")]
    public async Task<object> DiscoverRoute([ApiParameter("path", "Path to discover", Required = true, Location = "path")] string path)
    {
        try
        {
            var matchingServices = new List<ServiceInfo>();

            foreach (var service in _registeredServices.Values)
            {
                if (service.Capabilities.ContainsKey("routes"))
                {
                    var routes = service.Capabilities["routes"].Split(',');
                    if (routes.Any(route => path.StartsWith(route.TrimEnd('*'))))
                    {
                        matchingServices.Add(service);
                    }
                }
            }

            return new RouteDiscoveryResponse(
                Path: path,
                MatchingServices: matchingServices,
                Count: matchingServices.Count,
                Message: $"Found {matchingServices.Count} services matching path '{path}'"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Route discovery failed: {ex.Message}", "ROUTE_DISCOVERY_ERROR");
        }
    }

    /// <summary>
    /// Check if a service is healthy based on last seen time and health status
    /// </summary>
    private bool IsServiceHealthy(string serviceId)
    {
        if (!_registeredServices.TryGetValue(serviceId, out var service))
            return false;

        if (!_serviceHealth.TryGetValue(serviceId, out var health))
            return false;

        // Service is healthy if status is "Healthy" and last seen within 5 minutes
        var timeSinceLastSeen = DateTime.UtcNow - service.LastSeen;
        return health.Status == "Healthy" && timeSinceLastSeen.TotalMinutes < 5;
    }

    /// <summary>
    /// Find the best service for a request based on type and path
    /// </summary>
    private async Task<ServiceInfo?> FindBestServiceAsync(string? serviceType, string? path)
    {
        var candidates = _registeredServices.Values
            .Where(s => IsServiceHealthy(s.ServiceId));

        if (!string.IsNullOrEmpty(serviceType))
        {
            candidates = candidates.Where(s => s.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(path))
        {
            candidates = candidates.Where(s => 
                s.Capabilities.ContainsKey("routes") && 
                s.Capabilities["routes"].Split(',').Any(route => path.StartsWith(route.TrimEnd('*'))));
        }

        // Return the first healthy service (simple load balancing)
        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// Route request to a specific service
    /// </summary>
    private async Task<GatewayRouteResult> RouteToServiceInternalAsync(ServiceInfo service, GatewayRouteRequest request)
    {
        // This would typically make an HTTP request to the target service
        // For now, we'll simulate the routing
        var endpoint = request.Endpoint ?? "default";
        var url = $"{service.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        return new GatewayRouteResult
        {
            ServiceId = service.ServiceId,
            ServiceUrl = url,
            Endpoint = endpoint,
            Method = request.Method ?? "POST",
            Timestamp = DateTime.UtcNow,
            Success = true
        };
    }
}

// Request/Response DTOs for each API
public record ServiceRegistrationRequest(
    string ServiceId,
    string ServiceType,
    string BaseUrl,
    Dictionary<string, string> Capabilities
);

public record ServiceRegistrationResponse(
    bool Success,
    string ServiceId,
    string Message
);

public record ServiceDiscoveryResponse(
    bool Success,
    List<ServiceInfo> Services,
    int Count,
    string Message
);

public record ServiceListResponse(
    bool Success,
    List<ServiceInfo> Services,
    int Count,
    string Message
);

public record ServiceInfoResponse(
    bool Success,
    ServiceInfo Service,
    string Message
);

public record ServiceHealthUpdateRequest(
    string ServiceId,
    string Status,
    string? Error = null
);

public record ServiceHealthUpdateResponse(
    bool Success,
    string ServiceId,
    ServiceHealth Health,
    string Message
);

public record ServiceHealthCheckResponse(
    bool Success,
    string ServiceId,
    bool IsHealthy,
    ServiceHealth Health,
    string Message
);

public record ServiceUnregistrationResponse(
    bool Success,
    string ServiceId,
    string Message
);

public record ServiceCapabilityResponse(
    bool Success,
    string Capability,
    List<ServiceInfo> Services,
    int Count,
    string Message
);

public record ServiceInfo(
    string ServiceId,
    string ServiceType,
    string BaseUrl,
    Dictionary<string, string> Capabilities,
    ServiceHealth Health,
    DateTime LastSeen
);

public record ServiceHealth(
    string Status,
    DateTime LastCheck,
    string? Error
);

// Additional DTOs for API Gateway functionality
public record GatewayRouteRequest(
    string? ServiceType = null,
    string? Path = null,
    string? Endpoint = null,
    string? Method = null,
    object? Payload = null
);

public record GatewayRouteResponse(
    bool Success,
    string ServiceId,
    string ServiceUrl,
    string Endpoint,
    string Method,
    DateTime Timestamp,
    string Message
);

public record GatewayHealthResponse(
    string Status,
    int TotalServices,
    int HealthyServices,
    int UnhealthyServices,
    List<ServiceHealth> Services,
    string Message
);

public record RouteDiscoveryResponse(
    string Path,
    List<ServiceInfo> MatchingServices,
    int Count,
    string Message
);

public class GatewayRouteResult
{
    public string ServiceId { get; set; } = "";
    public string ServiceUrl { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Method { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

