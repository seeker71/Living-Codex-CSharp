using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Spec module specific response types
[ResponseType("codex.spec.atoms-response", "SpecAtomsResponse", "Response for spec atoms processing")]
public record SpecAtomsResponse(string ModuleId, bool Success, string Message = "Atoms processed successfully");

[ResponseType("codex.spec.compose-response", "SpecComposeResponse", "Response for spec composition")]
public record SpecComposeResponse(object Spec, bool Success, string Message = "Spec composed successfully");

[ResponseType("codex.spec.export-response", "SpecExportResponse", "Response for spec export")]
public record SpecExportResponse(string ModuleId, object Atoms, bool Success, string Message = "Atoms exported successfully");

[ResponseType("codex.spec.import-response", "SpecImportResponse", "Response for spec import")]
public record SpecImportResponse(string ModuleId, bool Success, string Message = "Atoms imported successfully");

[MetaNodeAttribute("codex.spec.module", "codex.meta/module", "SpecModule", "Specification management module")]
public sealed class SpecModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly Core.ILogger _logger;

    public SpecModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _logger = new Log4NetLogger(typeof(SpecModule));
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.spec",
            name: "Spec Management Module",
            version: "0.1.0",
            description: "Self-contained module for spec management operations (atoms, compose, export, import) using node-based storage",
            capabilities: new[] { "spec-management", "atoms", "compose", "export", "import" },
            tags: new[] { "spec", "management", "atoms", "compose" },
            specReference: "codex.spec.spec-management"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register API nodes
        var atomsApi = NodeStorage.CreateApiNode("codex.spec", "atoms", "/spec/atoms", "Submit module atoms");
        var composeApi = NodeStorage.CreateApiNode("codex.spec", "compose", "/spec/compose", "Compose spec from atoms");
        var exportApi = NodeStorage.CreateApiNode("codex.spec", "export", "/spec/export/{id}", "Export module atoms");
        var importApi = NodeStorage.CreateApiNode("codex.spec", "import", "/spec/import", "Import module atoms");
        var getAtomsApi = NodeStorage.CreateApiNode("codex.spec", "get-atoms", "/spec/atoms/{id}", "Get module atoms");
        var getSpecApi = NodeStorage.CreateApiNode("codex.spec", "get-spec", "/spec/{id}", "Get module spec");
        var getAllModulesApi = NodeStorage.CreateApiNode("codex.spec", "get-all-modules", "/spec/modules/all", "Get all modules catalog");
        var getAllRoutesApi = NodeStorage.CreateApiNode("codex.spec", "get-all-routes", "/spec/routes/all", "Get all routes catalog");
        var getFeaturesMapApi = NodeStorage.CreateApiNode("codex.spec", "get-features-map", "/spec/features/map", "Get modules mapped to features");
        var getStatusOverviewApi = NodeStorage.CreateApiNode("codex.spec", "get-status-overview", "/spec/status/overview", "Get comprehensive system status overview");
        
        registry.Upsert(atomsApi);
        registry.Upsert(composeApi);
        registry.Upsert(exportApi);
        registry.Upsert(importApi);
        registry.Upsert(getAtomsApi);
        registry.Upsert(getSpecApi);
        registry.Upsert(getAllModulesApi);
        registry.Upsert(getAllRoutesApi);
        registry.Upsert(getFeaturesMapApi);
        registry.Upsert(getStatusOverviewApi);
        
        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "compose"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "export"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "import"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-atoms"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-spec"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-all-modules"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-all-routes"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-features-map"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.spec", "get-status-overview"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // This method is now handled by attribute-based discovery
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // This method is now handled by attribute-based discovery
    }

    [ApiRoute("POST", "/spec/atoms", "spec-atoms", "Submit module atoms", "codex.spec")]
    public async Task<object> SubmitAtoms([ApiParameter("request", "Atoms submission request", Required = true, Location = "body")] SpecAtomsRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Store atoms as nodes
            if (request.Atoms.HasValue)
            {
                await StoreAtomsAsNodes(request.ModuleId, request.Atoms.Value);
                return new SpecAtomsResponse(request.ModuleId, true, "Atoms stored successfully");
            }

            return new SpecAtomsResponse(request.ModuleId, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to process atoms: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/spec/compose", "spec-compose", "Compose spec from atoms", "codex.spec")]
    public async Task<object> ComposeSpec([ApiParameter("request", "Spec composition request", Required = true, Location = "body")] SpecComposeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Compose spec from stored atoms
            var spec = await ComposeSpecFromAtoms(request.ModuleId);
            return new SpecComposeResponse(spec, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to compose spec: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/export/{id}", "spec-export", "Export module atoms", "codex.spec")]
    public async Task<object> ExportAtoms([ApiParameter("id", "Module ID to export", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Export atoms for the module
            var atoms = await ExportModuleAtoms(id);
            return new SpecExportResponse(id, atoms, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to export atoms: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/spec/import", "spec-import", "Import module atoms", "codex.spec")]
    public async Task<object> ImportAtoms([ApiParameter("request", "Atoms import request", Required = true, Location = "body")] SpecImportRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ModuleId))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Import atoms for the module
            if (request.Atoms != null && request.Atoms.Value.ValueKind != JsonValueKind.Null)
            {
                await StoreAtomsAsNodes(request.ModuleId, request.Atoms.Value);
            }
            else
            {
                return new ErrorResponse("Atoms data is required for import");
            }
            return new SpecImportResponse(request.ModuleId, true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to import atoms: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/atoms/{id}", "spec-get-atoms", "Get module atoms", "codex.spec")]
    public async Task<object> GetAtoms([ApiParameter("id", "Module ID to get atoms for", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Get atoms for the module
            var atoms = await ExportModuleAtoms(id);
            return atoms;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get atoms: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/{id}", "spec-get-spec", "Get module spec", "codex.spec")]
    public async Task<object> GetSpec([ApiParameter("id", "Module ID to get spec for", Required = true, Location = "path")] string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return new ErrorResponse("Module ID is required");
            }

            // Get spec for the module
            var spec = await ComposeSpecFromAtoms(id);
            return spec;
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get spec: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/modules/all", "spec-get-all-modules", "Get all modules catalog", "codex.spec")]
    public async Task<object> GetAllModules()
    {
        try
        {
            var modules = await DiscoverAllModules();
            return new
            {
                success = true,
                message = "All modules discovered successfully",
                timestamp = DateTime.UtcNow,
                totalModules = modules.Count,
                modules = modules
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to discover modules: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/routes/all", "spec-get-all-routes", "Get all routes catalog", "codex.spec")]
    public async Task<object> GetAllRoutes()
    {
        try
        {
            var routes = await DiscoverAllRoutes();
            return new
            {
                success = true,
                message = "All routes discovered successfully",
                timestamp = DateTime.UtcNow,
                totalRoutes = routes.Count,
                routes = routes
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to discover routes: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/debug/registry", "spec-debug-registry", "Debug registry contents", "codex.spec")]
    public object DebugRegistry()
    {
        try
        {
            var allNodes = _registry.AllNodes().ToList();
            var nodeTypes = allNodes.GroupBy(n => n.TypeId)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var apiNodes = allNodes
                .Where(node => node.TypeId == "api" || node.TypeId == "codex.meta/api")
                .ToList();

            return new
            {
                success = true,
                totalNodes = allNodes.Count,
                nodeTypes = nodeTypes,
                apiNodesCount = apiNodes.Count,
                apiNodeIds = apiNodes.Take(10).Select(n => new { n.Id, n.TypeId, n.Title }).ToList(),
                sampleApiNode = apiNodes.FirstOrDefault()?.Meta
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Debug failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/features/map", "spec-get-features-map", "Get modules mapped to features", "codex.spec")]
    public async Task<object> GetFeaturesMap()
    {
        try
        {
            var featuresMap = await MapModulesToFeatures();
            return new
            {
                success = true,
                message = "Features map generated successfully",
                timestamp = DateTime.UtcNow,
                totalFeatures = featuresMap.Count,
                features = featuresMap
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate features map: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/status/overview", "spec-get-status-overview", "Get comprehensive system status overview", "codex.spec")]
    public async Task<object> GetStatusOverview()
    {
        try
        {
            var modules = await DiscoverAllModules();
            var routes = await DiscoverAllRoutes();
            var featuresMap = await MapModulesToFeatures();
            
            return new
            {
                success = true,
                message = "System status overview generated successfully",
                timestamp = DateTime.UtcNow,
                system = new
                {
                    totalModules = modules.Count,
                    totalRoutes = routes.Count,
                    totalFeatures = featuresMap.Count,
                    specDrivenModules = modules.Count(m => m.IsSpecDriven),
                    hotReloadableModules = modules.Count(m => m.IsHotReloadable),
                    stableModules = modules.Count(m => m.IsStable)
                },
                modules = modules,
                routes = routes,
                features = featuresMap
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate status overview: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/modules/with-specs", "get-modules-with-specs", "Get all modules with their spec references", "codex.spec")]
    public async Task<object> GetModulesWithSpecsAsync()
    {
        try
        {
            var modules = _registry.GetNodesByType("module")
                .Concat(_registry.GetNodesByType("codex.meta/module"))
                .Where(m => m.Meta?.ContainsKey("specReference") == true)
                .Select(m => new
                {
                    moduleId = m.Id,
                    name = m.Title,
                    version = m.Meta?.GetValueOrDefault("version")?.ToString(),
                    description = m.Description,
                    specReference = m.Meta?.GetValueOrDefault("specReference")?.ToString(),
                    capabilities = m.Meta?.GetValueOrDefault("capabilities") as string[] ?? new string[0],
                    tags = m.Meta?.GetValueOrDefault("tags") as string[] ?? new string[0]
                })
                .ToList();

            return new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                totalModules = modules.Count,
                modules = modules
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting modules with specs: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get modules with specs: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/spec/relationships/spec-to-modules", "get-spec-to-module-relationships", "Get all spec-to-module relationships", "codex.spec")]
    public async Task<object> GetSpecToModuleRelationshipsAsync()
    {
        try
        {
            var specModuleEdges = _registry.AllEdges()
                .Where(e => e.Role == "has-implementation" && e.Meta?.GetValueOrDefault("relationship")?.ToString() == "spec-has-module-implementation")
                .Select(e => new
                {
                    specId = e.FromId,
                    moduleId = e.ToId,
                    relationship = e.Role,
                    weight = e.Weight,
                    createdAt = e.Meta?.GetValueOrDefault("createdAt")?.ToString()
                })
                .ToList();

            var moduleSpecEdges = _registry.AllEdges()
                .Where(e => e.Role == "implements" && e.Meta?.GetValueOrDefault("relationship")?.ToString() == "module-implements-spec")
                .Select(e => new
                {
                    moduleId = e.FromId,
                    specId = e.ToId,
                    relationship = e.Role,
                    weight = e.Weight,
                    createdAt = e.Meta?.GetValueOrDefault("createdAt")?.ToString()
                })
                .ToList();

            return new
            {
                success = true,
                timestamp = DateTime.UtcNow,
                specToModuleEdges = specModuleEdges,
                moduleToSpecEdges = moduleSpecEdges,
                totalSpecToModule = specModuleEdges.Count,
                totalModuleToSpec = moduleSpecEdges.Count
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting spec-to-module relationships: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get spec-to-module relationships: {ex.Message}");
        }
    }

    private Task StoreAtomsAsNodes(string moduleId, JsonElement atoms)
    {
        try
        {
            _logger.Info($"StoreAtomsAsNodes called for moduleId: {moduleId}");
            _logger.Debug($"Atoms JSON: {atoms.GetRawText()}");
            
            // Parse atoms JSON
            AtomsData atomsData;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                atomsData = JsonSerializer.Deserialize<AtomsData>(atoms.GetRawText(), jsonOptions) ?? new AtomsData();
                if (atomsData == null) 
            {
                _logger.Warn("AtomsData is null after deserialization");
                return Task.CompletedTask;
            }
            
            _logger.Info($"Parsed {atomsData.Nodes?.Count ?? 0} nodes and {atomsData.Edges?.Count ?? 0} edges");
                
                if (atomsData.Nodes == null || atomsData.Nodes.Count == 0)
                {
                    _logger.Warn("No nodes found in atoms data");
                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to deserialize atoms JSON: {ex.Message}");
                _logger.Error($"JSON content: {atoms.GetRawText()}");
                return Task.CompletedTask;
            }

            // Store nodes
            foreach (var nodeData in atomsData.Nodes ?? new List<NodeData>())
            {
                var node = new Node(
                    Id: nodeData.Id ?? Guid.NewGuid().ToString(),
                    TypeId: nodeData.TypeId ?? "unknown",
                    State: Enum.TryParse<ContentState>(nodeData.State, out var state) ? state : ContentState.Ice,
                    Locale: nodeData.Locale,
                    Title: nodeData.Title,
                    Description: nodeData.Description,
                    Content: nodeData.Content != null ? new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(nodeData.Content),
                        InlineBytes: null,
                        ExternalUri: null
                    ) : null,
                    Meta: new Dictionary<string, object>
                    {
                        ["moduleId"] = moduleId,
                        ["storedAt"] = DateTime.UtcNow.ToString("O")
                    }
                );
                _registry.Upsert(node);
            }

            // Store edges
            foreach (var edgeData in atomsData.Edges ?? new List<EdgeData>())
            {
                var edge = new Edge(
                    FromId: edgeData.FromId ?? "",
                    ToId: edgeData.ToId ?? "",
                    Role: edgeData.Role ?? "related",
                    Weight: null,
                    Meta: null
                );
                _registry.Upsert(edge);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to store atoms: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    private Task<object> ComposeSpecFromAtoms(string moduleId)
    {
        try
        {
            // Get module node
            if (!_registry.TryGet(moduleId, out var moduleNode))
            {
                throw new InvalidOperationException($"Module '{moduleId}' not found");
            }

            // Get module's API nodes
            var apiNodes = _registry.GetNodesByType("api")
                .Where(api => api.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get module's edges
            var moduleEdges = _registry.AllEdges()
                .Where(edge => edge.FromId == moduleId || edge.ToId == moduleId)
                .ToList();

            // Compose spec from actual module data
            var spec = new
            {
                id = moduleId,
                name = moduleNode.Title ?? moduleId,
                version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0",
                description = moduleNode.Description ?? $"Spec for {moduleId}",
                state = moduleNode.State.ToString(),
                apis = apiNodes.Select(api => new
                {
                    name = api.Meta?.GetValueOrDefault("apiName")?.ToString() ?? "unknown",
                    route = api.Meta?.GetValueOrDefault("route")?.ToString(),
                    description = api.Description
                }).ToList(),
                dependencies = moduleEdges
                    .Where(e => e.FromId == moduleId)
                    .Select(e => e.ToId)
                    .ToList(),
                dependents = moduleEdges
                    .Where(e => e.ToId == moduleId)
                    .Select(e => e.FromId)
                    .ToList(),
                composedAt = DateTime.UtcNow.ToString("O")
            };

            return Task.FromResult<object>(spec);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to compose spec: {ex.Message}");
        }
    }

    private Task<object> ExportModuleAtoms(string moduleId)
    {
        try
        {
            // Get all nodes related to this module
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
                .ToList();

            // Get all edges related to this module
            var moduleEdges = _registry.AllEdges()
                .Where(edge => moduleNodes.Any(n => n.Id == edge.FromId || n.Id == edge.ToId))
                .ToList();

            var atoms = new
            {
                moduleId,
                exportedAt = DateTime.UtcNow.ToString("O"),
                nodes = moduleNodes.Select(node => new
                {
                    id = node.Id,
                    typeId = node.TypeId,
                    state = node.State.ToString(),
                    locale = node.Locale,
                    title = node.Title,
                    description = node.Description,
                    content = node.Content?.InlineJson
                }).ToList(),
                edges = moduleEdges.Select(edge => new
                {
                    fromId = edge.FromId,
                    toId = edge.ToId,
                    role = edge.Role
                }).ToList()
            };

            return Task.FromResult<object>(atoms);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to export atoms: {ex.Message}");
        }
    }

    // Data classes for atoms parsing
    private class AtomsData
    {
        public List<NodeData>? Nodes { get; set; }
        public List<EdgeData>? Edges { get; set; }
    }

    private class NodeData
    {
        public string? Id { get; set; }
        public string? TypeId { get; set; }
        public string? State { get; set; }
        public string? Locale { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public object? Content { get; set; }
    }

    private class EdgeData
    {
        public string? FromId { get; set; }
        public string? ToId { get; set; }
        public string? Role { get; set; }
    }

    // Discovery methods for comprehensive module and route tracking
    private async Task<List<ModuleInfo>> DiscoverAllModules()
    {
        var modules = new List<ModuleInfo>();
        
        try
        {
            // Get all module nodes from registry (both standard and meta module types)
            var moduleNodes = _registry.AllNodes()
                .Where(node => node.TypeId == "module" || node.TypeId == "codex.meta/module")
                .ToList();

            foreach (var moduleNode in moduleNodes)
            {
                var moduleInfo = new ModuleInfo
                {
                    Id = moduleNode.Id,
                    Name = moduleNode.Title ?? moduleNode.Id,
                    Version = moduleNode.Meta?.GetValueOrDefault("version")?.ToString() ?? "1.0.0",
                    Description = moduleNode.Description ?? "No description available",
                    State = moduleNode.State.ToString(),
                    IsSpecDriven = moduleNode.Meta?.GetValueOrDefault("spec-driven")?.ToString() == "true",
                    IsHotReloadable = IsModuleHotReloadable(moduleNode),
                    IsStable = IsModuleStable(moduleNode),
                    Features = ExtractModuleFeatures(moduleNode),
                    Dependencies = GetModuleDependencies(moduleNode.Id),
                    Routes = await GetModuleRoutes(moduleNode.Id),
                    LastUpdated = moduleNode.Meta?.GetValueOrDefault("lastUpdated")?.ToString() ?? DateTime.UtcNow.ToString("O")
                };
                
                modules.Add(moduleInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering modules: {ex.Message}");
        }
        
        return modules;
    }

    private async Task<List<RouteInfo>> DiscoverAllRoutes()
    {
        var routes = new List<RouteInfo>();
        
        try
        {
            // Get all API nodes from registry
            var allNodes = _registry.AllNodes().ToList();
            var apiNodes = allNodes
                .Where(node => node.TypeId == "api" || node.TypeId == "codex.meta/api")
                .ToList();
            
            _logger.Info($"Total nodes in registry: {allNodes.Count}");
            _logger.Info($"API nodes found: {apiNodes.Count}");
            _logger.Info($"API node TypeIds: {string.Join(", ", apiNodes.Select(n => n.TypeId).Distinct())}");
            _logger.Info($"All TypeIds in registry: {string.Join(", ", allNodes.Select(n => n.TypeId).Distinct().Take(20))}");

            foreach (var apiNode in apiNodes)
            {
                var routeInfo = new RouteInfo
                {
                    Id = apiNode.Id,
                    Name = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString() ?? "unknown",
                    Path = apiNode.Meta?.GetValueOrDefault("route")?.ToString() ?? "",
                    Method = ExtractHttpMethod(apiNode),
                    Description = apiNode.Description ?? "No description available",
                    ModuleId = apiNode.Meta?.GetValueOrDefault("moduleId")?.ToString() ?? "unknown",
                    Tags = ExtractRouteTags(apiNode),
                    Parameters = ExtractRouteParameters(apiNode),
                    ResponseTypes = ExtractResponseTypes(apiNode),
                    IsSpecDriven = apiNode.Meta?.GetValueOrDefault("spec-driven")?.ToString() == "true",
                    Status = ExtractRouteStatus(apiNode),
                    LastUpdated = apiNode.Meta?.GetValueOrDefault("lastUpdated")?.ToString() ?? DateTime.UtcNow.ToString("O")
                };
                
                routes.Add(routeInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error discovering routes: {ex.Message}");
        }
        
        return routes;
    }

    private async Task<List<FeatureInfo>> MapModulesToFeatures()
    {
        var features = new List<FeatureInfo>();
        
        try
        {
            // Define feature categories based on the Living Codex specification
            var featureCategories = new Dictionary<string, List<string>>
            {
                ["Core Framework"] = new() { "Core", "Spec", "Storage", "ModuleLoader", "NodeRegistry" },
                ["Abundance & Amplification"] = new() { "UserContributions", "Abundance", "Amplification", "Rewards" },
                ["Future Knowledge"] = new() { "FutureKnowledge", "PatternDiscovery", "Prediction", "LLM" },
                ["Resonance Engine"] = new() { "Resonance", "Joy", "Frequency", "U-CORE", "Sacred" },
                ["Translation & Communication"] = new() { "Translation", "Language", "Communication" },
                ["Real-time Systems"] = new() { "Realtime", "News", "Streaming", "Events" },
                ["Graph & Query"] = new() { "Graph", "Query", "MetaNode", "Exploration" },
                ["Security & Access"] = new() { "Security", "Authentication", "Authorization", "Access" },
                ["Monitoring & Health"] = new() { "Health", "Metrics", "Monitoring", "Status" },
                ["AI & Machine Learning"] = new() { "AI", "ML", "Concept", "Ontology", "Intelligence" }
            };

            var modules = await DiscoverAllModules();
            
            foreach (var category in featureCategories)
            {
                var categoryModules = modules.Where(m => 
                    category.Value.Any(keyword => 
                        m.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        m.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        m.Features.Any(f => f.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    )).ToList();

                if (categoryModules.Any())
                {
                    features.Add(new FeatureInfo
                    {
                        Category = category.Key,
                        ModuleCount = categoryModules.Count,
                        Modules = categoryModules.Select(m => new ModuleReference
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Version = m.Version,
                            IsSpecDriven = m.IsSpecDriven,
                            IsHotReloadable = m.IsHotReloadable
                        }).ToList(),
                        Routes = categoryModules.SelectMany(m => m.Routes).ToList(),
                        Priority = CalculateFeaturePriority(category.Key, categoryModules)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error mapping modules to features: {ex.Message}");
        }
        
        return features;
    }

    // Helper methods
    private bool IsModuleHotReloadable(Node moduleNode)
    {
        var moduleName = moduleNode.Meta?.GetValueOrDefault("name")?.ToString() ?? "";
        var hotReloadableModules = new[] { "TestDynamicModule", "ExampleModule", "HelloModule" };
        return hotReloadableModules.Any(name => moduleName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsModuleStable(Node moduleNode)
    {
        var moduleId = moduleNode.Id;
        var stableModules = new[] { "codex.core", "codex.spec", "codex.storage" };
        return stableModules.Contains(moduleId);
    }

    private List<string> ExtractModuleFeatures(Node moduleNode)
    {
        var features = new List<string>();
        
        // Extract from description
        var description = moduleNode.Description ?? "";
        if (description.Contains("AI", StringComparison.OrdinalIgnoreCase)) features.Add("AI");
        if (description.Contains("LLM", StringComparison.OrdinalIgnoreCase)) features.Add("LLM");
        if (description.Contains("Real-time", StringComparison.OrdinalIgnoreCase)) features.Add("Real-time");
        if (description.Contains("Translation", StringComparison.OrdinalIgnoreCase)) features.Add("Translation");
        if (description.Contains("Security", StringComparison.OrdinalIgnoreCase)) features.Add("Security");
        if (description.Contains("Graph", StringComparison.OrdinalIgnoreCase)) features.Add("Graph");
        if (description.Contains("Resonance", StringComparison.OrdinalIgnoreCase)) features.Add("Resonance");
        if (description.Contains("Future", StringComparison.OrdinalIgnoreCase)) features.Add("Future Knowledge");
        
        // Extract from tags
        if (moduleNode.Meta?.ContainsKey("tags") == true)
        {
            var tags = moduleNode.Meta["tags"];
            if (tags is string[] tagArray)
            {
                features.AddRange(tagArray);
            }
        }
        
        return features.Distinct().ToList();
    }

    private List<string> GetModuleDependencies(string moduleId)
    {
        return _registry.AllEdges()
            .Where(edge => edge.FromId == moduleId)
            .Select(edge => edge.ToId)
            .ToList();
    }

    private async Task<List<RouteInfo>> GetModuleRoutes(string moduleId)
    {
        return _registry.AllNodes()
            .Where(node => node.TypeId == "api" && 
                          node.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId)
            .Select(apiNode => new RouteInfo
            {
                Id = apiNode.Id,
                Name = apiNode.Meta?.GetValueOrDefault("apiName")?.ToString() ?? "unknown",
                Path = apiNode.Meta?.GetValueOrDefault("route")?.ToString() ?? "",
                Method = ExtractHttpMethod(apiNode),
                Description = apiNode.Description ?? "No description available",
                ModuleId = moduleId,
                Status = ExtractRouteStatus(apiNode)
            })
            .ToList();
    }

    private string ExtractHttpMethod(Node apiNode)
    {
        var verb = apiNode.Meta?.GetValueOrDefault("verb")?.ToString() ?? "";
        if (!string.IsNullOrEmpty(verb)) return verb.ToUpperInvariant();
        return "GET"; // Default
    }

    private List<string> ExtractRouteTags(Node apiNode)
    {
        var tags = new List<string>();
        var description = apiNode.Description ?? "";
        
        if (description.Contains("health", StringComparison.OrdinalIgnoreCase)) tags.Add("health");
        if (description.Contains("metrics", StringComparison.OrdinalIgnoreCase)) tags.Add("metrics");
        if (description.Contains("status", StringComparison.OrdinalIgnoreCase)) tags.Add("status");
        if (description.Contains("test", StringComparison.OrdinalIgnoreCase)) tags.Add("test");
        if (description.Contains("admin", StringComparison.OrdinalIgnoreCase)) tags.Add("admin");
        
        return tags;
    }

    private List<string> ExtractRouteParameters(Node apiNode)
    {
        // This would need to be enhanced to parse actual parameter information
        // For now, return empty list
        return new List<string>();
    }

    private List<string> ExtractResponseTypes(Node apiNode)
    {
        // This would need to be enhanced to parse actual response type information
        // For now, return empty list
        return new List<string>();
    }

    private RouteStatus ExtractRouteStatus(Node apiNode)
    {
        // Get status from meta data, default to Untested if not specified
        var statusString = apiNode.Meta?.GetValueOrDefault("status")?.ToString();
        
        if (string.IsNullOrEmpty(statusString))
        {
            // Try to infer status from description or other metadata
            var description = apiNode.Description ?? "";
            var route = apiNode.Meta?.GetValueOrDefault("route")?.ToString() ?? "";
            
            if (description.Contains("stub", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.Stub;
            
            if (description.Contains("simulated", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("mock", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.Simulated;
            
            if (description.Contains("fallback", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("backup", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.Fallback;
            
            if (description.Contains("ai", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("llm", StringComparison.OrdinalIgnoreCase) ||
                description.Contains("artificial intelligence", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.AiEnabled;
            
            if (description.Contains("external", StringComparison.OrdinalIgnoreCase) || 
                description.Contains("api", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.ExternalInfo;
            
            if (description.Contains("test", StringComparison.OrdinalIgnoreCase) || 
                route.Contains("test", StringComparison.OrdinalIgnoreCase))
                return RouteStatus.Simple;
            
            return RouteStatus.Untested;
        }
        
        // Parse the status string
        if (Enum.TryParse<RouteStatus>(statusString, true, out var status))
            return status;
        
        return RouteStatus.Untested;
    }

    private int CalculateFeaturePriority(string category, List<ModuleInfo> modules)
    {
        // Priority based on category importance and module count
        var categoryPriority = category switch
        {
            "Core Framework" => 10,
            "Security & Access" => 9,
            "AI & Machine Learning" => 8,
            "Future Knowledge" => 7,
            "Resonance Engine" => 6,
            "Abundance & Amplification" => 5,
            "Real-time Systems" => 4,
            "Graph & Query" => 3,
            "Translation & Communication" => 2,
            "Monitoring & Health" => 1,
            _ => 0
        };
        
        var moduleCountBonus = Math.Min(modules.Count * 2, 10);
        return categoryPriority + moduleCountBonus;
    }
}

// Data classes for comprehensive tracking
public class ModuleInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string State { get; set; } = "";
    public bool IsSpecDriven { get; set; }
    public bool IsHotReloadable { get; set; }
    public bool IsStable { get; set; }
    public List<string> Features { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<RouteInfo> Routes { get; set; } = new();
    public string LastUpdated { get; set; } = "";
}

public class RouteInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Method { get; set; } = "";
    public string Description { get; set; } = "";
    public string ModuleId { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> Parameters { get; set; } = new();
    public List<string> ResponseTypes { get; set; } = new();
    public bool IsSpecDriven { get; set; }
    public RouteStatus Status { get; set; } = RouteStatus.Untested;
    public string LastUpdated { get; set; } = "";
}

public class FeatureInfo
{
    public string Category { get; set; } = "";
    public int ModuleCount { get; set; }
    public List<ModuleReference> Modules { get; set; } = new();
    public List<RouteInfo> Routes { get; set; } = new();
    public int Priority { get; set; }
}

public class ModuleReference
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsSpecDriven { get; set; }
    public bool IsHotReloadable { get; set; }
}

// Request types for the spec module
[RequestType("codex.spec.atoms-request", "SpecAtomsRequest", "Spec atoms request")]
public sealed record SpecAtomsRequest(string ModuleId, JsonElement? Atoms = null);

[RequestType("codex.spec.compose-request", "SpecComposeRequest", "Spec compose request")]
public sealed record SpecComposeRequest(string ModuleId);

[RequestType("codex.spec.import-request", "SpecImportRequest", "Spec import request")]
public sealed record SpecImportRequest(string ModuleId, JsonElement? Atoms = null);
