using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Registry Module - Central registry for all concepts across services
/// Manages concept registration, discovery, versioning, and cross-service synchronization
/// </summary>
public class ConceptRegistryModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, ConceptRegistryEntry> _conceptRegistry = new();
    private readonly Dictionary<string, List<string>> _serviceConcepts = new();
    private readonly Dictionary<string, ConceptVersion> _conceptVersions = new();
    private readonly Dictionary<string, List<ConceptRelationship>> _conceptRelationships = new();
    private CoreApiService? _coreApiService;

    public ConceptRegistryModule(NodeRegistry registry)
    {
        _registry = registry;
    }

    public ConceptRegistryModule() : this(new NodeRegistry()) { }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.concept-registry",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Registry Module",
            Description: "Central registry for all concepts across services with version management and cross-service synchronization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "relationship-management", "quality-assessment" },
                    endpoints = new[] { "register-concept", "discover-concepts", "get-concept", "update-concept", "sync-concepts", "assess-quality" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Concept Registry Module",
                ["version"] = "1.0.0",
                ["type"] = "registry",
                ["capabilities"] = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "quality-assessment" }
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
        
        // Register all Concept Registry related nodes for AI agent discovery
        RegisterConceptRegistryNodes(registry);
    }

    /// <summary>
    /// Register all Concept Registry related nodes for AI agent discovery and module generation
    /// </summary>
    private void RegisterConceptRegistryNodes(NodeRegistry registry)
    {
        // Register Concept Registry module node
        var conceptRegistryNode = new Node(
            Id: "codex.concept-registry",
            TypeId: "codex.module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Registry Module",
            Description: "Central registry for all concepts across services with version management and cross-service synchronization",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    version = "1.0.0",
                    capabilities = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "relationship-management", "quality-assessment" },
                    endpoints = new[] { "register-concept", "discover-concepts", "get-concept", "update-concept", "sync-concepts", "assess-quality" },
                    integration = "cross-service-concept-management"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["name"] = "Concept Registry Module",
                ["version"] = "1.0.0",
                ["type"] = "registry",
                ["parentModule"] = "codex.concept-registry",
                ["capabilities"] = new[] { "concept-registration", "concept-discovery", "version-management", "cross-service-sync", "quality-assessment" }
            }
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
    private void RegisterQualityAssessmentRoutes(NodeRegistry registry)
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
                TypeId: "meta.route",
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
    private void RegisterQualityAssessmentDTOs(NodeRegistry registry)
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
