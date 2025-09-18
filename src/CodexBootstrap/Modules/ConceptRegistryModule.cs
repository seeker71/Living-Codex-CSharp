using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Registry Module - Central registry for all concepts across services
/// Manages concept registration, discovery, versioning, and cross-service synchronization
/// </summary>
public class ConceptRegistryModule : ModuleBase
{
    private readonly Dictionary<string, ConceptRegistryEntry> _conceptRegistry = new();
    private readonly Dictionary<string, List<string>> _serviceConcepts = new();
    private readonly Dictionary<string, ConceptVersion> _conceptVersions = new();
    private readonly Dictionary<string, List<ConceptRelationship>> _conceptRelationships = new();

    public override string Name => "Concept Registry Module";
    public override string Description => "Central registry for all concepts across services";
    public override string Version => "1.0.0";

    public ConceptRegistryModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.concept-registry",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "concept-registry", "ontology", "discovery", "quality-assessment", "u-core" },
            capabilities: new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "quality-assessment", "ontology-integration" },
            spec: "codex.spec.concept-registry"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _coreApiService = coreApi;
        
        // Register all Concept Registry related nodes for AI agent discovery
        RegisterConceptRegistryNodes(registry);
    }

    /// <summary>
    /// Register all Concept Registry related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterConceptRegistryNodes(INodeRegistry registry)
    {
        // Register Concept Registry module node
        var conceptRegistryNode = CreateModuleNode(
            moduleId: "codex.concept-registry",
            name: "Concept Registry Module",
            version: "1.0.0",
            description: "Central registry for all concepts across services with version management and cross-service synchronization",
            tags: new[] { "concept", "registry", "cross-service", "version-management", "quality-assessment" },
            capabilities: new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "relationship-management", "quality-assessment" },
            spec: "codex.spec.concept-registry"
        );
        registry.Upsert(conceptRegistryNode);

        // Register Quality Assessment routes as nodes
        RegisterQualityAssessmentRoutes(registry);
        
        // Register Quality Assessment DTOs as nodes
        RegisterQualityAssessmentDTOs(registry);
    }

    /// <summary>
    /// Register Quality Assessment routes as discoverable nodes
    /// </summary>
    private void RegisterQualityAssessmentRoutes(INodeRegistry registry)
    {
        var routes = new[]
        {
            new { path = "/concepts/quality/assess", method = "POST", name = "concept-quality-assess", description = "Assess the quality of a concept" },
            new { path = "/concepts/quality/batch-assess", method = "POST", name = "concept-quality-batch-assess", description = "Assess quality of multiple concepts" },
            new { path = "/concepts/quality/standards", method = "GET", name = "concept-quality-standards", description = "Get available quality assessment standards" },
            new { path = "/concepts/quality/compare", method = "POST", name = "concept-quality-compare", description = "Compare quality of multiple concepts" }
        };

        foreach (var route in routes)
        {
            var routeNode = new Node(
                Id: $"concept-registry.quality.route.{route.name}",
                TypeId: "codex.meta/route",
                State: ContentState.Ice,
                Locale: "en",
                Title: route.description,
                Description: $"Quality Assessment route: {route.method} {route.path}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        path = route.path,
                        method = route.method,
                        name = route.name,
                        description = route.description,
                        parameters = GetQualityRouteParameters(route.name),
                        responseType = GetQualityRouteResponseType(route.name)
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
                    ["module"] = "codex.concept-registry",
                    ["parentModule"] = "codex.concept-registry"
                }
            );
            registry.Upsert(routeNode);
        }
    }

    /// <summary>
    /// Register Quality Assessment DTOs as discoverable nodes
    /// </summary>
    private void RegisterQualityAssessmentDTOs(INodeRegistry registry)
    {
        var dtos = new[]
        {
            new { name = "QualityAssessmentRequest", description = "Request to assess concept quality", properties = new[] { "ConceptId", "AssessmentCriteria", "ReportOptions" } },
            new { name = "QualityAssessmentResponse", description = "Response from quality assessment", properties = new[] { "Success", "ConceptId", "QualityMetrics", "QualityReport", "AssessedAt", "Message" } },
            new { name = "BatchQualityAssessmentRequest", description = "Request to assess multiple concepts", properties = new[] { "ConceptIds", "AssessmentCriteria", "ReportOptions" } },
            new { name = "BatchQualityAssessmentResponse", description = "Response from batch quality assessment", properties = new[] { "Success", "Results", "TotalAssessed", "AssessedAt", "Message" } },
            new { name = "QualityStandardsResponse", description = "Response with quality standards", properties = new[] { "Success", "Standards", "Category", "Count", "RetrievedAt", "Message" } },
            new { name = "QualityComparisonRequest", description = "Request to compare concept quality", properties = new[] { "ConceptIds", "AssessmentCriteria" } },
            new { name = "QualityComparisonResponse", description = "Response from quality comparison", properties = new[] { "Success", "Comparisons", "ComparedAt", "Message" } }
        };

        foreach (var dto in dtos)
        {
            var dtoNode = new Node(
                Id: $"concept-registry.quality.dto.{dto.name}",
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
                        module = "codex.concept-registry",
                        usage = GetQualityDTOUsage(dto.name)
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = dto.name,
                    ["description"] = dto.description,
                    ["type"] = "record",
                    ["module"] = "codex.concept-registry",
                    ["parentModule"] = "codex.concept-registry",
                    ["properties"] = dto.properties
                }
            );
            registry.Upsert(dtoNode);
        }
    }

    // Helper methods for AI agent generation
    private object GetQualityRouteParameters(string routeName)
    {
        return routeName switch
        {
            "concept-quality-assess" => new
            {
                request = new { type = "QualityAssessmentRequest", required = true, location = "body", description = "Quality assessment details" }
            },
            "concept-quality-batch-assess" => new
            {
                request = new { type = "BatchQualityAssessmentRequest", required = true, location = "body", description = "Batch quality assessment details" }
            },
            "concept-quality-standards" => new
            {
                category = new { type = "string", required = false, location = "query", description = "Quality standard category" }
            },
            "concept-quality-compare" => new
            {
                request = new { type = "QualityComparisonRequest", required = true, location = "body", description = "Quality comparison details" }
            },
            _ => new { }
        };
    }

    private string GetQualityRouteResponseType(string routeName)
    {
        return routeName switch
        {
            "concept-quality-assess" => "QualityAssessmentResponse",
            "concept-quality-batch-assess" => "BatchQualityAssessmentResponse",
            "concept-quality-standards" => "QualityStandardsResponse",
            "concept-quality-compare" => "QualityComparisonResponse",
            _ => "object"
        };
    }

    private string GetQualityDTOUsage(string dtoName)
    {
        return dtoName switch
        {
            "QualityAssessmentRequest" => "Used to request quality assessment of a concept. Contains concept ID and assessment criteria.",
            "QualityAssessmentResponse" => "Returned when quality assessment is completed. Contains quality metrics and report.",
            "BatchQualityAssessmentRequest" => "Used to request quality assessment of multiple concepts in batch.",
            "BatchQualityAssessmentResponse" => "Returned when batch quality assessment is completed. Contains results for all concepts.",
            "QualityStandardsResponse" => "Returned when requesting available quality standards. Contains list of standards.",
            "QualityComparisonRequest" => "Used to request quality comparison between multiple concepts.",
            "QualityComparisonResponse" => "Returned when quality comparison is completed. Contains comparison results.",
            _ => "Quality Assessment data transfer object"
        };
    }

    // Concept Discovery & Ontology Integration API Methods
    [ApiRoute("POST", "/concept/discover", "concept-discover", "Discover and register concepts from content", "codex.concept-registry")]
    public async Task<object> DiscoverConcepts([ApiParameter("request", "Concept discovery request", Required = true, Location = "body")] ConceptDiscoveryRequest request)
    {
        try
        {
            // Use AI module to extract concepts via HTTP call
            try
            {
                using var httpClient = new HttpClient();
                var extractionRequest = new
                {
                    title = request.Title,
                    content = request.Content,
                    categories = new[] { request.Domain },
                    source = "concept-discovery",
                    url = ""
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(extractionRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(GlobalConfiguration.GetUrl("/ai/extract-concepts"), jsonContent);
                
                if (!response.IsSuccessStatusCode)
                {
                    return new ErrorResponse($"AI module returned error: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var extractionResult = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Parse extracted concepts and register them
                var discoveredConcepts = new List<DiscoveredConcept>();
                var registeredConcepts = new List<string>();

                // Handle both old format (concept objects) and new format (concept strings)
                if (extractionResult.TryGetProperty("concepts", out var conceptsArray))
                {
                    foreach (var conceptElement in conceptsArray.EnumerateArray())
                    {
                        string conceptNameStr;
                        double score = 0.8; // Default confidence
                        string description;
                        string conceptType = "consciousness";

                        if (conceptElement.ValueKind == JsonValueKind.String)
                        {
                            // New format: simple string array
                            conceptNameStr = conceptElement.GetString() ?? "unknown";
                            description = $"Discovered concept: {conceptNameStr}";
                        }
                        else
                        {
                            // Old format: concept objects
                            conceptNameStr = conceptElement.TryGetProperty("concept", out var conceptName) 
                                ? conceptName.GetString() ?? "unknown" 
                                : "unknown";
                            score = conceptElement.TryGetProperty("score", out var scoreProp) 
                                ? scoreProp.GetDouble() 
                                : 0.8;
                            description = conceptElement.TryGetProperty("description", out var desc) 
                                ? desc.GetString() ?? $"Discovered concept: {conceptNameStr}" 
                                : $"Discovered concept: {conceptNameStr}";
                            conceptType = conceptElement.TryGetProperty("category", out var cat) 
                                ? cat.GetString() ?? "consciousness" 
                                : "consciousness";
                        }

                        var conceptId = $"discovered-{conceptNameStr.ToLower().Replace(" ", "-").Replace("'", "")}";
                        
                        discoveredConcepts.Add(new DiscoveredConcept(
                            conceptId,
                            conceptNameStr,
                            description,
                            conceptType,
                            score,
                            new Dictionary<string, object>
                            {
                                ["discoveredAt"] = DateTime.UtcNow,
                                ["source"] = "AI extraction",
                                ["confidence"] = score
                            }
                        ));

                        // Register concept in U-CORE ontology
                        try
                        {
                            var ontologyRequest = new
                            {
                                conceptId = conceptId,
                                name = conceptNameStr,
                                description = description,
                                conceptType = conceptType,
                                properties = new Dictionary<string, object>
                                {
                                    ["discoveredAt"] = DateTime.UtcNow,
                                    ["source"] = "AI extraction",
                                    ["confidence"] = score
                                }
                            };

                            var ontologyJson = new StringContent(
                                JsonSerializer.Serialize(ontologyRequest),
                                System.Text.Encoding.UTF8,
                                "application/json"
                            );

                            var ontologyResponse = await httpClient.PostAsync(GlobalConfiguration.GetUrl("/concept/ontology/register"), ontologyJson);
                            if (ontologyResponse.IsSuccessStatusCode)
                            {
                                registeredConcepts.Add(conceptId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error($"Failed to register concept {conceptId}: {ex.Message}");
                        }
                    }
                }

                return new ConceptDiscoveryResponse(
                    Success: true,
                    DiscoveredConcepts: discoveredConcepts,
                    RegisteredConcepts: registeredConcepts,
                    TotalDiscovered: discoveredConcepts.Count,
                    TotalRegistered: registeredConcepts.Count,
                    Message: "Concept discovery completed successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"AI module integration failed: {ex.Message}");
                return new ErrorResponse($"AI module integration failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Concept discovery failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/concept/ontology/register", "concept-ontology-register", "Register concept in U-CORE ontology", "codex.concept-registry")]
    public async Task<object> RegisterConceptInOntology([ApiParameter("request", "Ontology registration request", Required = true, Location = "body")] OntologyRegistrationRequest request)
    {
        try
        {
            // Create U-CORE concept from discovered concept
            var ucoreConcept = new UCoreConcept
            {
                Id = request.ConceptId,
                Name = request.Name,
                Description = request.Description,
                Type = MapToConceptType(request.ConceptType),
                Frequency = CalculateSacredFrequency(request.Name, request.Description),
                Resonance = CalculateResonance(request.Name, request.Description),
                Properties = request.Properties ?? new Dictionary<string, object>()
            };

            // Register in U-CORE ontology
            var ontology = new UCoreOntology();
            ontology.Concepts[request.ConceptId] = ucoreConcept;

            // Store as node in registry
            var conceptNode = new Node(
                Id: $"ucore.concept.{request.ConceptId}",
                TypeId: "ucore.concept",
                State: ContentState.Ice,
                Locale: "en",
                Title: request.Name,
                Description: request.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(ucoreConcept),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["conceptId"] = request.ConceptId,
                    ["name"] = request.Name,
                    ["type"] = request.ConceptType,
                    ["frequency"] = ucoreConcept.Frequency,
                    ["resonance"] = ucoreConcept.Resonance,
                    ["registeredAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(conceptNode);

            return new OntologyRegistrationResponse(
                Success: true,
                ConceptId: request.ConceptId,
                UCoreConcept: ucoreConcept,
                Frequency: ucoreConcept.Frequency,
                Resonance: ucoreConcept.Resonance,
                Message: "Concept registered in U-CORE ontology successfully"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Ontology registration failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/concept/ontology/relate", "concept-ontology-relate", "Create relationships between concepts", "codex.concept-registry")]
    public async Task<object> CreateConceptRelationships([ApiParameter("request", "Relationship creation request", Required = true, Location = "body")] ConceptRelationshipRequest request)
    {
        try
        {
            var relationships = new List<UCoreRelationship>();
            var relationshipIds = new List<string>();

            foreach (var relationship in request.Relationships)
            {
                var relationshipId = $"rel.{relationship.SourceConceptId}.{relationship.TargetConceptId}.{relationship.Type}";
                
                var ucoreRelationship = new UCoreRelationship
                {
                    Id = relationshipId,
                    Source = relationship.SourceConceptId,
                    Target = relationship.TargetConceptId,
                    Type = MapToRelationshipType(relationship.Type),
                    Strength = relationship.Strength,
                    Description = relationship.Description,
                    Properties = relationship.Properties ?? new Dictionary<string, object>()
                };

                relationships.Add(ucoreRelationship);
                relationshipIds.Add(relationshipId);

                // Store relationship as edge in registry
                var relationshipEdge = new Edge(
                    FromId: $"ucore.concept.{relationship.SourceConceptId}",
                    ToId: $"ucore.concept.{relationship.TargetConceptId}",
                    Role: "ucore.relationship",
                    Weight: relationship.Strength,
                    Meta: new Dictionary<string, object>
                    {
                        ["relationshipId"] = relationshipId,
                        ["type"] = relationship.Type,
                        ["strength"] = relationship.Strength,
                        ["description"] = relationship.Description,
                        ["createdAt"] = DateTime.UtcNow
                    }
                );

                _registry.Upsert(relationshipEdge);
            }

            return new ConceptRelationshipResponse(
                Success: true,
                Relationships: relationships,
                RelationshipIds: relationshipIds,
                TotalCreated: relationships.Count,
                Message: $"Created {relationships.Count} concept relationships successfully"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Relationship creation failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/concept/ontology/explore/{id}", "concept-ontology-explore", "Explore concept relationships", "codex.concept-registry")]
    public async Task<object> ExploreConceptRelationships([ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id)
    {
        try
        {
            var conceptId = $"ucore.concept.{id}";
            var conceptNode = _registry.GetNode(conceptId);
            
            if (conceptNode == null)
            {
                return new ErrorResponse("Concept not found in ontology");
            }

            // Get all relationships for this concept
            var outgoingEdges = _registry.GetEdgesFrom(conceptId)
                .Where(e => e.Role == "ucore.relationship")
                .ToList();

            var incomingEdges = _registry.GetEdgesTo(conceptId)
                .Where(e => e.Role == "ucore.relationship")
                .ToList();

            var relationships = new List<ConceptRelationshipInfo>();
            
            foreach (var edge in outgoingEdges)
            {
                relationships.Add(new ConceptRelationshipInfo(
                    edge.ToId.Replace("ucore.concept.", ""),
                    edge.Meta?.GetValueOrDefault("type")?.ToString() ?? "unknown",
                    Convert.ToDouble(edge.Meta?.GetValueOrDefault("strength") ?? 0.0),
                    edge.Meta?.GetValueOrDefault("description")?.ToString() ?? "",
                    "outgoing"
                ));
            }

            foreach (var edge in incomingEdges)
            {
                relationships.Add(new ConceptRelationshipInfo(
                    edge.FromId.Replace("ucore.concept.", ""),
                    edge.Meta?.GetValueOrDefault("type")?.ToString() ?? "unknown",
                    Convert.ToDouble(edge.Meta?.GetValueOrDefault("strength") ?? 0.0),
                    edge.Meta?.GetValueOrDefault("description")?.ToString() ?? "",
                    "incoming"
                ));
            }

            return new ConceptExplorationResponse(
                Success: true,
                ConceptId: id,
                ConceptName: conceptNode.Title,
                Relationships: relationships,
                TotalRelationships: relationships.Count,
                OutgoingCount: outgoingEdges.Count,
                IncomingCount: incomingEdges.Count,
                Message: "Concept exploration completed successfully"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Concept exploration failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/concept/ontology/amplify", "concept-ontology-amplify", "Amplify concept resonance", "codex.concept-registry")]
    public async Task<object> AmplifyConceptResonance([ApiParameter("request", "Amplification request", Required = true, Location = "body")] ConceptAmplificationRequest request)
    {
        try
        {
            var conceptId = $"ucore.concept.{request.ConceptId}";
            var conceptNode = _registry.GetNode(conceptId);
            
            if (conceptNode == null)
            {
                return new ErrorResponse("Concept not found in ontology");
            }

            // Calculate amplification based on resonance and frequency
            var currentResonance = Convert.ToDouble(conceptNode.Meta?.GetValueOrDefault("resonance") ?? 0.0);
            var currentFrequency = Convert.ToDouble(conceptNode.Meta?.GetValueOrDefault("frequency") ?? 432.0);
            
            var amplificationFactor = CalculateAmplificationFactor(request.AmplificationType, currentResonance, currentFrequency);
            var newResonance = Math.Min(currentResonance * amplificationFactor, 1.0);
            var newFrequency = currentFrequency * amplificationFactor;

            // Update concept with amplified values
            conceptNode.Meta["resonance"] = newResonance;
            conceptNode.Meta["frequency"] = newFrequency;
            conceptNode.Meta["amplifiedAt"] = DateTime.UtcNow;
            conceptNode.Meta["amplificationType"] = request.AmplificationType;

            _registry.Upsert(conceptNode);

            return new ConceptAmplificationResponse(
                Success: true,
                ConceptId: request.ConceptId,
                OriginalResonance: currentResonance,
                AmplifiedResonance: newResonance,
                OriginalFrequency: currentFrequency,
                AmplifiedFrequency: newFrequency,
                AmplificationFactor: amplificationFactor,
                AmplificationType: request.AmplificationType,
                Message: "Concept resonance amplified successfully"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Concept amplification failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/concept/ontology/frequencies", "concept-ontology-frequencies", "Get concept frequency mappings", "codex.concept-registry")]
    public async Task<object> GetConceptFrequencies([ApiParameter("conceptType", "Concept type filter", Required = false, Location = "query")] string? conceptType = null)
    {
        try
        {
            var frequencyMappings = new List<ConceptFrequencyMapping>();
            
            // Get all U-CORE concepts from registry
            var conceptNodes = _registry.GetNodesByType("ucore.concept");
            
            foreach (var node in conceptNodes)
            {
                if (conceptType != null && !node.Meta?.GetValueOrDefault("type")?.ToString()?.Equals(conceptType, StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                var conceptId = node.Meta?.GetValueOrDefault("conceptId")?.ToString() ?? node.Id;
                var name = node.Title;
                var frequency = Convert.ToDouble(node.Meta?.GetValueOrDefault("frequency") ?? 432.0);
                var resonance = Convert.ToDouble(node.Meta?.GetValueOrDefault("resonance") ?? 0.0);
                var type = node.Meta?.GetValueOrDefault("type")?.ToString() ?? "unknown";

                frequencyMappings.Add(new ConceptFrequencyMapping(
                    conceptId,
                    name,
                    frequency,
                    resonance,
                    type,
                    GetSacredFrequencyName(frequency)
                ));
            }

            return new ConceptFrequencyResponse(
                Success: true,
                FrequencyMappings: frequencyMappings,
                TotalConcepts: frequencyMappings.Count,
                FilteredByType: conceptType,
                Message: $"Retrieved frequency mappings for {frequencyMappings.Count} concepts"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to retrieve concept frequencies: {ex.Message}");
        }
    }

    // Helper methods for concept discovery and ontology integration
    private ConceptType MapToConceptType(string conceptType)
    {
        return conceptType.ToLower() switch
        {
            "emotion" or "feeling" => ConceptType.Emotion,
            "transformation" or "change" => ConceptType.Transformation,
            "consciousness" or "awareness" => ConceptType.Consciousness,
            "energy" or "power" => ConceptType.Energy,
            "frequency" or "vibration" => ConceptType.Frequency,
            _ => ConceptType.Core
        };
    }

    private RelationshipType MapToRelationshipType(string relationshipType)
    {
        return relationshipType.ToLower() switch
        {
            "amplifies" or "enhances" => RelationshipType.Amplifies,
            "transforms" or "changes" => RelationshipType.Transforms,
            "unifies" or "connects" => RelationshipType.Unifies,
            "resonates" or "harmonizes" => RelationshipType.Resonates,
            "harmonizes" or "balances" => RelationshipType.Harmonizes,
            "integrates" or "combines" => RelationshipType.Integrates,
            _ => RelationshipType.Resonates
        };
    }

    private double CalculateSacredFrequency(string name, string description)
    {
        var text = $"{name} {description}".ToLower();
        
        // Map to sacred frequencies based on content
        if (text.Contains("love") || text.Contains("heart") || text.Contains("compassion"))
            return 528.0; // Love frequency
        if (text.Contains("healing") || text.Contains("harmony") || text.Contains("balance"))
            return 432.0; // Natural frequency
        if (text.Contains("consciousness") || text.Contains("intuition") || text.Contains("wisdom"))
            return 741.0; // Expression frequency
        if (text.Contains("pain") || text.Contains("suffering") || text.Contains("transformation"))
            return 174.0; // Pain transformation frequency
        
        // Default to 432Hz
        return 432.0;
    }

    private double CalculateResonance(string name, string description)
    {
        var text = $"{name} {description}".ToLower();
        var resonance = 0.5; // Base resonance
        
        // Increase resonance based on positive keywords
        if (text.Contains("love") || text.Contains("joy") || text.Contains("peace"))
            resonance += 0.3;
        if (text.Contains("consciousness") || text.Contains("awareness") || text.Contains("wisdom"))
            resonance += 0.2;
        if (text.Contains("healing") || text.Contains("harmony") || text.Contains("balance"))
            resonance += 0.2;
        
        return Math.Min(resonance, 1.0);
    }

    private double CalculateAmplificationFactor(string amplificationType, double currentResonance, double currentFrequency)
    {
        return amplificationType.ToLower() switch
        {
            "gentle" => 1.1,
            "moderate" => 1.3,
            "strong" => 1.6,
            "intense" => 2.0,
            "sacred" => 2.5,
            _ => 1.2
        };
    }

    private string GetSacredFrequencyName(double frequency)
    {
        return frequency switch
        {
            174.0 => "174 Hz - Pain Transformation",
            432.0 => "432 Hz - Natural Harmony",
            528.0 => "528 Hz - Love Frequency",
            741.0 => "741 Hz - Expression Frequency",
            _ => $"{frequency} Hz - Custom Frequency"
        };
    }

    // Quality Assessment API Methods
    [ApiRoute("POST", "/concepts/quality/assess", "concept-quality-assess", "Assess the quality of a concept", "codex.concept-registry")]
    public async Task<object> AssessConceptQuality([ApiParameter("request", "Quality assessment request", Required = true, Location = "body")] QualityAssessmentRequest request)
    {
        try
        {
            // Get the concept to assess
            if (!_conceptRegistry.TryGetValue(request.ConceptId, out var conceptEntry))
            {
                return new ErrorResponse("Concept not found");
            }

            // Perform quality assessment
            var qualityMetrics = await PerformQualityAssessment(conceptEntry, request.AssessmentCriteria);
            
            // Generate quality report
            var qualityReport = await GenerateQualityReport(conceptEntry, qualityMetrics, request.ReportOptions);

            return new QualityAssessmentResponse(
                Success: true,
                ConceptId: request.ConceptId,
                QualityMetrics: qualityMetrics,
                QualityReport: qualityReport,
                AssessedAt: DateTime.UtcNow,
                Message: "Quality assessment completed successfully"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Quality assessment failed: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/concepts/quality/batch-assess", "concept-quality-batch-assess", "Assess quality of multiple concepts", "codex.concept-registry")]
    public async Task<object> BatchAssessConceptQuality([ApiParameter("request", "Batch quality assessment request", Required = true, Location = "body")] BatchQualityAssessmentRequest request)
    {
        try
        {
            var assessmentResults = new List<ConceptQualityResult>();
            
            foreach (var conceptId in request.ConceptIds)
            {
                if (_conceptRegistry.TryGetValue(conceptId, out var conceptEntry))
                {
                    var qualityMetrics = await PerformQualityAssessment(conceptEntry, request.AssessmentCriteria);
                    var qualityReport = await GenerateQualityReport(conceptEntry, qualityMetrics, request.ReportOptions);
                    
                    assessmentResults.Add(new ConceptQualityResult
                    {
                        ConceptId = conceptId,
                        QualityMetrics = qualityMetrics,
                        QualityReport = qualityReport,
                        AssessedAt = DateTime.UtcNow
                    });
                }
            }

            return new BatchQualityAssessmentResponse(
                Success: true,
                Results: assessmentResults,
                TotalAssessed: assessmentResults.Count,
                AssessedAt: DateTime.UtcNow,
                Message: $"Assessed quality for {assessmentResults.Count} concepts"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Batch quality assessment failed: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/concepts/quality/standards", "concept-quality-standards", "Get available quality assessment standards", "codex.concept-registry")]
    public async Task<object> GetQualityStandards([ApiParameter("category", "Quality standard category", Required = false, Location = "query")] string? category = null)
    {
        try
        {
            var standards = await GetAvailableQualityStandards(category);
            
            return new QualityStandardsResponse(
                Success: true,
                Standards: standards,
                Category: category ?? "all",
                Count: standards.Count,
                RetrievedAt: DateTime.UtcNow,
                Message: $"Retrieved {standards.Count} quality standards"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to retrieve quality standards: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/concepts/quality/compare", "concept-quality-compare", "Compare quality of multiple concepts", "codex.concept-registry")]
    public async Task<object> CompareConceptQuality([ApiParameter("request", "Quality comparison request", Required = true, Location = "body")] QualityComparisonRequest request)
    {
        try
        {
            var comparisonResults = new List<ConceptQualityComparison>();
            
            // Assess each concept
            var conceptAssessments = new Dictionary<string, ConceptQualityMetrics>();
            foreach (var conceptId in request.ConceptIds)
            {
                if (_conceptRegistry.TryGetValue(conceptId, out var conceptEntry))
                {
                    var qualityMetrics = await PerformQualityAssessment(conceptEntry, request.AssessmentCriteria);
                    conceptAssessments[conceptId] = qualityMetrics;
                }
            }

            // Perform comparisons
            for (int i = 0; i < request.ConceptIds.Length; i++)
            {
                for (int j = i + 1; j < request.ConceptIds.Length; j++)
                {
                    var concept1Id = request.ConceptIds[i];
                    var concept2Id = request.ConceptIds[j];
                    
                    if (conceptAssessments.TryGetValue(concept1Id, out var metrics1) && 
                        conceptAssessments.TryGetValue(concept2Id, out var metrics2))
                    {
                        var comparison = await CompareQualityMetrics(concept1Id, metrics1, concept2Id, metrics2);
                        comparisonResults.Add(comparison);
                    }
                }
            }

            return new QualityComparisonResponse(
                Success: true,
                Comparisons: comparisonResults,
                ComparedAt: DateTime.UtcNow,
                Message: $"Compared quality of {request.ConceptIds.Length} concepts"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Quality comparison failed: {ex.Message}");
        }
    }

    // Helper methods for quality assessment
    private async Task<ConceptQualityMetrics> PerformQualityAssessment(ConceptRegistryEntry concept, Dictionary<string, object> criteria)
    {
        var metrics = new ConceptQualityMetrics
        {
            ConceptId = concept.ConceptId,
            OverallScore = 0.0,
            ClarityScore = 0.0,
            CompletenessScore = 0.0,
            ConsistencyScore = 0.0,
            AccuracyScore = 0.0,
            RelevanceScore = 0.0,
            InnovationScore = 0.0,
            UsabilityScore = 0.0,
            MaintainabilityScore = 0.0,
            AssessedAt = DateTime.UtcNow
        };

        // Calculate individual quality scores
        metrics.ClarityScore = CalculateClarityScore(concept);
        metrics.CompletenessScore = CalculateCompletenessScore(concept);
        metrics.ConsistencyScore = CalculateConsistencyScore(concept);
        metrics.AccuracyScore = CalculateAccuracyScore(concept);
        metrics.RelevanceScore = CalculateRelevanceScore(concept);
        metrics.InnovationScore = CalculateInnovationScore(concept);
        metrics.UsabilityScore = CalculateUsabilityScore(concept);
        metrics.MaintainabilityScore = CalculateMaintainabilityScore(concept);

        // Calculate overall score as weighted average
        metrics.OverallScore = CalculateOverallQualityScore(metrics);

        return metrics;
    }

    private double CalculateClarityScore(ConceptRegistryEntry concept)
    {
        var score = 0.5; // Base score
        
        // Check description quality
        if (!string.IsNullOrEmpty(concept.Concept.Description))
        {
            score += 0.2;
            if (concept.Concept.Description.Length > 50) score += 0.1;
            if (concept.Concept.Description.Length > 100) score += 0.1;
        }
        
        // Check metadata completeness
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Length > 0)
        {
            score += 0.1;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateCompletenessScore(ConceptRegistryEntry concept)
    {
        var score = 0.3; // Base score
        
        // Check required fields
        if (!string.IsNullOrEmpty(concept.Concept.Title)) score += 0.2;
        if (!string.IsNullOrEmpty(concept.Concept.Description)) score += 0.2;
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Length > 0) score += 0.2;
        if (concept.Version != null && concept.Version != "1.0.0") score += 0.1;
        
        return Math.Min(score, 1.0);
    }

    private double CalculateConsistencyScore(ConceptRegistryEntry concept)
    {
        var score = 0.7; // Base score for consistency
        
        // Check naming consistency
        if (!string.IsNullOrEmpty(concept.Concept.Title) && concept.Concept.Title.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
        {
            score += 0.1;
        }
        
        // Check metadata consistency
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Length > 0)
        {
            score += 0.1;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateAccuracyScore(ConceptRegistryEntry concept)
    {
        var score = 0.6; // Base score
        
        // Check for valid data
        if (!string.IsNullOrEmpty(concept.Concept.Title) && concept.Concept.Title.Length > 0)
        {
            score += 0.2;
        }
        
        if (!string.IsNullOrEmpty(concept.Concept.Description) && concept.Concept.Description.Length > 10)
        {
            score += 0.2;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateRelevanceScore(ConceptRegistryEntry concept)
    {
        var score = 0.5; // Base score
        
        // Check if concept has relationships (indicates relevance)
        if (_conceptRelationships.TryGetValue(concept.ConceptId, out var relationships) && relationships.Count > 0)
        {
            score += 0.3;
        }
        
        // Check if concept is used by multiple services
        var serviceCount = _serviceConcepts.Count(kvp => kvp.Value.Contains(concept.ConceptId));
        if (serviceCount > 1)
        {
            score += 0.2;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateInnovationScore(ConceptRegistryEntry concept)
    {
        var score = 0.4; // Base score
        
        // Check for unique characteristics
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Contains("innovation"))
        {
            score += 0.3;
        }
        
        // Check version history for evolution
        if (concept.Version != null && concept.Version != "1.0.0")
        {
            score += 0.2;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateUsabilityScore(ConceptRegistryEntry concept)
    {
        var score = 0.5; // Base score
        
        // Check for clear naming
        if (!string.IsNullOrEmpty(concept.Concept.Title) && concept.Concept.Title.Length <= 50)
        {
            score += 0.2;
        }
        
        // Check for good description
        if (!string.IsNullOrEmpty(concept.Concept.Description) && concept.Concept.Description.Length >= 20)
        {
            score += 0.2;
        }
        
        // Check for examples or usage info
        if (concept.Metadata != null && concept.Metadata.Tags != null && (concept.Metadata.Tags.Contains("example") || concept.Metadata.Tags.Contains("usage")))
        {
            score += 0.1;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateMaintainabilityScore(ConceptRegistryEntry concept)
    {
        var score = 0.6; // Base score
        
        // Check for version management
        if (concept.Version != null && concept.Version != "1.0.0")
        {
            score += 0.2;
        }
        
        // Check for metadata that aids maintenance
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Contains("maintainer"))
        {
            score += 0.1;
        }
        
        if (concept.Metadata != null && concept.Metadata.Tags != null && concept.Metadata.Tags.Contains("lastUpdated"))
        {
            score += 0.1;
        }
        
        return Math.Min(score, 1.0);
    }

    private double CalculateOverallQualityScore(ConceptQualityMetrics metrics)
    {
        // Weighted average of all quality scores
        var weights = new Dictionary<string, double>
        {
            ["clarity"] = 0.15,
            ["completeness"] = 0.15,
            ["consistency"] = 0.10,
            ["accuracy"] = 0.15,
            ["relevance"] = 0.15,
            ["innovation"] = 0.10,
            ["usability"] = 0.10,
            ["maintainability"] = 0.10
        };
        
        var weightedSum = metrics.ClarityScore * weights["clarity"] +
                         metrics.CompletenessScore * weights["completeness"] +
                         metrics.ConsistencyScore * weights["consistency"] +
                         metrics.AccuracyScore * weights["accuracy"] +
                         metrics.RelevanceScore * weights["relevance"] +
                         metrics.InnovationScore * weights["innovation"] +
                         metrics.UsabilityScore * weights["usability"] +
                         metrics.MaintainabilityScore * weights["maintainability"];
        
        return Math.Min(weightedSum, 1.0);
    }

    private async Task<QualityReport> GenerateQualityReport(ConceptRegistryEntry concept, ConceptQualityMetrics metrics, Dictionary<string, object> options)
    {
        var report = new QualityReport
        {
            ConceptId = concept.ConceptId,
            OverallGrade = GetQualityGrade(metrics.OverallScore),
            Strengths = GetQualityStrengths(metrics),
            Weaknesses = GetQualityWeaknesses(metrics),
            Recommendations = GenerateQualityRecommendations(metrics),
            DetailedAnalysis = GenerateDetailedAnalysis(concept, metrics),
            GeneratedAt = DateTime.UtcNow
        };
        
        return report;
    }

    private string GetQualityGrade(double score)
    {
        if (score >= 0.9) return "A+";
        if (score >= 0.8) return "A";
        if (score >= 0.7) return "B+";
        if (score >= 0.6) return "B";
        if (score >= 0.5) return "C+";
        if (score >= 0.4) return "C";
        if (score >= 0.3) return "D";
        return "F";
    }

    private List<string> GetQualityStrengths(ConceptQualityMetrics metrics)
    {
        var strengths = new List<string>();
        
        if (metrics.ClarityScore >= 0.8) strengths.Add("High clarity and readability");
        if (metrics.CompletenessScore >= 0.8) strengths.Add("Comprehensive and complete");
        if (metrics.ConsistencyScore >= 0.8) strengths.Add("Consistent naming and structure");
        if (metrics.AccuracyScore >= 0.8) strengths.Add("Accurate and reliable");
        if (metrics.RelevanceScore >= 0.8) strengths.Add("Highly relevant and useful");
        if (metrics.InnovationScore >= 0.8) strengths.Add("Innovative and forward-thinking");
        if (metrics.UsabilityScore >= 0.8) strengths.Add("User-friendly and accessible");
        if (metrics.MaintainabilityScore >= 0.8) strengths.Add("Well-maintained and documented");
        
        return strengths;
    }

    private List<string> GetQualityWeaknesses(ConceptQualityMetrics metrics)
    {
        var weaknesses = new List<string>();
        
        if (metrics.ClarityScore < 0.6) weaknesses.Add("Needs improvement in clarity");
        if (metrics.CompletenessScore < 0.6) weaknesses.Add("Incomplete or missing information");
        if (metrics.ConsistencyScore < 0.6) weaknesses.Add("Inconsistent naming or structure");
        if (metrics.AccuracyScore < 0.6) weaknesses.Add("Accuracy concerns identified");
        if (metrics.RelevanceScore < 0.6) weaknesses.Add("Limited relevance or usefulness");
        if (metrics.InnovationScore < 0.6) weaknesses.Add("Lacks innovation or uniqueness");
        if (metrics.UsabilityScore < 0.6) weaknesses.Add("Poor usability or accessibility");
        if (metrics.MaintainabilityScore < 0.6) weaknesses.Add("Difficult to maintain or update");
        
        return weaknesses;
    }

    private List<string> GenerateQualityRecommendations(ConceptQualityMetrics metrics)
    {
        var recommendations = new List<string>();
        
        if (metrics.ClarityScore < 0.7) recommendations.Add("Improve concept description and documentation");
        if (metrics.CompletenessScore < 0.7) recommendations.Add("Add missing metadata and version information");
        if (metrics.ConsistencyScore < 0.7) recommendations.Add("Standardize naming conventions and structure");
        if (metrics.AccuracyScore < 0.7) recommendations.Add("Review and validate concept data");
        if (metrics.RelevanceScore < 0.7) recommendations.Add("Establish relationships with other concepts");
        if (metrics.InnovationScore < 0.7) recommendations.Add("Add unique or innovative characteristics");
        if (metrics.UsabilityScore < 0.7) recommendations.Add("Improve user experience and accessibility");
        if (metrics.MaintainabilityScore < 0.7) recommendations.Add("Enhance maintenance documentation and processes");
        
        return recommendations;
    }

    private string GenerateDetailedAnalysis(ConceptRegistryEntry concept, ConceptQualityMetrics metrics)
    {
        var analysis = $"Quality Analysis for Concept '{concept.Concept.Title}':\n\n";
        analysis += $"Overall Score: {metrics.OverallScore:F2} ({GetQualityGrade(metrics.OverallScore)})\n\n";
        analysis += "Detailed Scores:\n";
        analysis += $"- Clarity: {metrics.ClarityScore:F2}\n";
        analysis += $"- Completeness: {metrics.CompletenessScore:F2}\n";
        analysis += $"- Consistency: {metrics.ConsistencyScore:F2}\n";
        analysis += $"- Accuracy: {metrics.AccuracyScore:F2}\n";
        analysis += $"- Relevance: {metrics.RelevanceScore:F2}\n";
        analysis += $"- Innovation: {metrics.InnovationScore:F2}\n";
        analysis += $"- Usability: {metrics.UsabilityScore:F2}\n";
        analysis += $"- Maintainability: {metrics.MaintainabilityScore:F2}\n";
        
        return analysis;
    }

    private async Task<List<QualityStandard>> GetAvailableQualityStandards(string? category)
    {
        var standards = new List<QualityStandard>
        {
            new QualityStandard("ISO-25010", "ISO/IEC 25010 Software Quality Model", "Comprehensive software quality assessment", "international"),
            new QualityStandard("IEEE-1061", "IEEE 1061 Software Quality Metrics", "Software quality measurement standards", "international"),
            new QualityStandard("CMMI", "Capability Maturity Model Integration", "Process improvement framework", "process"),
            new QualityStandard("ISO-9001", "ISO 9001 Quality Management", "Quality management system standards", "management"),
            new QualityStandard("Custom", "Custom Quality Standards", "Project-specific quality criteria", "custom")
        };
        
        if (!string.IsNullOrEmpty(category))
        {
            standards = standards.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return standards;
    }

    private async Task<ConceptQualityComparison> CompareQualityMetrics(string concept1Id, ConceptQualityMetrics metrics1, string concept2Id, ConceptQualityMetrics metrics2)
    {
        var comparison = new ConceptQualityComparison
        {
            Concept1Id = concept1Id,
            Concept2Id = concept2Id,
            OverallScoreDifference = metrics1.OverallScore - metrics2.OverallScore,
            BetterConceptId = metrics1.OverallScore > metrics2.OverallScore ? concept1Id : concept2Id,
            DetailedComparison = new Dictionary<string, double>
            {
                ["clarity"] = metrics1.ClarityScore - metrics2.ClarityScore,
                ["completeness"] = metrics1.CompletenessScore - metrics2.CompletenessScore,
                ["consistency"] = metrics1.ConsistencyScore - metrics2.ConsistencyScore,
                ["accuracy"] = metrics1.AccuracyScore - metrics2.AccuracyScore,
                ["relevance"] = metrics1.RelevanceScore - metrics2.RelevanceScore,
                ["innovation"] = metrics1.InnovationScore - metrics2.InnovationScore,
                ["usability"] = metrics1.UsabilityScore - metrics2.UsabilityScore,
                ["maintainability"] = metrics1.MaintainabilityScore - metrics2.MaintainabilityScore
            },
            ComparedAt = DateTime.UtcNow
        };
        
        return comparison;
    }
}

// Quality Assessment DTOs
public record QualityAssessmentRequest(
    string ConceptId,
    Dictionary<string, object> AssessmentCriteria,
    Dictionary<string, object> ReportOptions
);

public record QualityAssessmentResponse(
    bool Success,
    string ConceptId,
    ConceptQualityMetrics QualityMetrics,
    QualityReport QualityReport,
    DateTime AssessedAt,
    string Message
);

public record BatchQualityAssessmentRequest(
    string[] ConceptIds,
    Dictionary<string, object> AssessmentCriteria,
    Dictionary<string, object> ReportOptions
);

public record BatchQualityAssessmentResponse(
    bool Success,
    List<ConceptQualityResult> Results,
    int TotalAssessed,
    DateTime AssessedAt,
    string Message
);

public record QualityStandardsResponse(
    bool Success,
    List<QualityStandard> Standards,
    string Category,
    int Count,
    DateTime RetrievedAt,
    string Message
);

public record QualityComparisonRequest(
    string[] ConceptIds,
    Dictionary<string, object> AssessmentCriteria
);

public record QualityComparisonResponse(
    bool Success,
    List<ConceptQualityComparison> Comparisons,
    DateTime ComparedAt,
    string Message
);

public class ConceptQualityMetrics
{
    public string ConceptId { get; set; } = "";
    public double OverallScore { get; set; }
    public double ClarityScore { get; set; }
    public double CompletenessScore { get; set; }
    public double ConsistencyScore { get; set; }
    public double AccuracyScore { get; set; }
    public double RelevanceScore { get; set; }
    public double InnovationScore { get; set; }
    public double UsabilityScore { get; set; }
    public double MaintainabilityScore { get; set; }
    public DateTime AssessedAt { get; set; }
}

public class QualityReport
{
    public string ConceptId { get; set; } = "";
    public string OverallGrade { get; set; } = "";
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string DetailedAnalysis { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}

public class ConceptQualityResult
{
    public string ConceptId { get; set; } = "";
    public ConceptQualityMetrics QualityMetrics { get; set; } = new();
    public QualityReport QualityReport { get; set; } = new();
    public DateTime AssessedAt { get; set; }
}

public class QualityStandard
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";

    public QualityStandard(string id, string name, string description, string category)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
    }
}

public class ConceptQualityComparison
{
    public string Concept1Id { get; set; } = "";
    public string Concept2Id { get; set; } = "";
    public double OverallScoreDifference { get; set; }
    public string BetterConceptId { get; set; } = "";
    public Dictionary<string, double> DetailedComparison { get; set; } = new();
    public DateTime ComparedAt { get; set; }
}

// Basic Concept Registry DTOs
public record ConceptRegistrationRequest(
    string ConceptId,
    string ServiceId,
    Node Concept,
    ConceptMetadata? Metadata,
    string[]? Tags
);

public record ConceptRegistrationResponse(
    bool Success,
    string ConceptId,
    string Version,
    string Message
);

public class ConceptRegistryEntry
{
    public string ConceptId { get; set; } = "";
    public string ServiceId { get; set; } = "";
    public Node Concept { get; set; } = new("", "", ContentState.Ice, null, "", "", null, null);
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Status { get; set; } = "active";
    public ConceptMetadata? Metadata { get; set; }
}

public class ConceptVersion
{
    public string Version { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Changes { get; set; } = "";
    public string Author { get; set; } = "";
    public string Reason { get; set; } = "";
}

public class ConceptRelationship
{
    public string RelationshipId { get; set; } = "";
    public string SourceConceptId { get; set; } = "";
    public string TargetConceptId { get; set; } = "";
    public string Type { get; set; } = "";
    public double Strength { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConceptMetadata
{
    public string[]? Tags { get; set; }
    public string[]? Categories { get; set; }
    public string? Language { get; set; }
    public string? Culture { get; set; }
    public string? Complexity { get; set; }
}

// Concept Discovery & Ontology Integration DTOs
public record ConceptDiscoveryRequest(
    string Title,
    string Content,
    string Domain,
    string ExtractionLevel,
    Dictionary<string, object>? Options
);

public record ConceptDiscoveryResponse(
    bool Success,
    List<DiscoveredConcept> DiscoveredConcepts,
    List<string> RegisteredConcepts,
    int TotalDiscovered,
    int TotalRegistered,
    string Message
);

public record DiscoveredConcept(
    string ConceptId,
    string Name,
    string Description,
    string Type,
    double Confidence,
    Dictionary<string, object> Properties
);

public record OntologyRegistrationRequest(
    string ConceptId,
    string Name,
    string Description,
    string ConceptType,
    Dictionary<string, object>? Properties
);

public record OntologyRegistrationResponse(
    bool Success,
    string ConceptId,
    UCoreConcept UCoreConcept,
    double Frequency,
    double Resonance,
    string Message
);

public record ConceptRelationshipRequest(
    List<ConceptRelationshipData> Relationships
);

public record ConceptRelationshipData(
    string SourceConceptId,
    string TargetConceptId,
    string Type,
    double Strength,
    string Description,
    Dictionary<string, object>? Properties
);

public record ConceptRelationshipResponse(
    bool Success,
    List<UCoreRelationship> Relationships,
    List<string> RelationshipIds,
    int TotalCreated,
    string Message
);

public record ConceptExplorationResponse(
    bool Success,
    string ConceptId,
    string ConceptName,
    List<ConceptRelationshipInfo> Relationships,
    int TotalRelationships,
    int OutgoingCount,
    int IncomingCount,
    string Message
);

public record ConceptRelationshipInfo(
    string TargetConceptId,
    string Type,
    double Strength,
    string Description,
    string Direction
);

public record ConceptAmplificationRequest(
    string ConceptId,
    string AmplificationType
);

public record ConceptAmplificationResponse(
    bool Success,
    string ConceptId,
    double OriginalResonance,
    double AmplifiedResonance,
    double OriginalFrequency,
    double AmplifiedFrequency,
    double AmplificationFactor,
    string AmplificationType,
    string Message
);

public record ConceptFrequencyResponse(
    bool Success,
    List<ConceptFrequencyMapping> FrequencyMappings,
    int TotalConcepts,
    string? FilteredByType,
    string Message
);

public record ConceptFrequencyMapping(
    string ConceptId,
    string Name,
    double Frequency,
    double Resonance,
    string Type,
    string SacredFrequency
);
