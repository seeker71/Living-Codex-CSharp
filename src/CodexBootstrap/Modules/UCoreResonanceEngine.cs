using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// U-CORE Resonance Engine - Performs resonance field optimization calculations
/// Maps LLM responses to U-CORE ontology and calculates resonance matches
/// </summary>
[MetaNode(
    id: "codex.ucore.resonance-engine",
    typeId: "codex.meta/module",
    name: "U-CORE Resonance Engine",
    description: "Performs resonance field optimization calculations for LLM response mapping"
)]
[ApiModule(
    name: "U-CORE Resonance Engine",
    version: "1.0.0",
    description: "Resonance field optimization engine for U-CORE ontology mapping",
    basePath: "/ucore/resonance",
    tags: new[] { "U-CORE", "Resonance", "Optimization", "Ontology", "Matching" }
)]
public class UCoreResonanceEngine : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly UCoreOntology _ontology;
    private readonly Dictionary<string, UserBeliefSystem> _userBeliefSystems;

    public UCoreResonanceEngine(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _userBeliefSystems = new Dictionary<string, UserBeliefSystem>();
        _ontology = InitializeUCoreOntology();
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.ucore.resonance-engine",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "U-CORE Resonance Engine",
            Description: "Performs resonance field optimization calculations for LLM response mapping",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.ucore.resonance-engine",
                    Name = "U-CORE Resonance Engine",
                    Description = "Resonance field optimization engine for U-CORE ontology mapping",
                    Version = "1.0.0",
                    Capabilities = new[] { "ResonanceCalculation", "OntologyMapping", "BeliefSystemMatching", "Optimization", "FrequencyAnalysis" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.ucore.resonance-engine",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "Resonance field optimization"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
        
        // Register the U-CORE ontology
        var ontologyNode = CreateOntologyNode(_ontology);
        registry.Upsert(ontologyNode);
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    [ApiRoute("POST", "/ucore/resonance/calculate", "ucore-resonance-calculate", "Calculate resonance match for LLM response", "codex.ucore.resonance")]
    [ApiDocumentation(
        summary: "Calculate resonance match for LLM response",
        description: "Calculates resonance field optimization between LLM response and user belief system using U-CORE ontology",
        operationId: "calculateResonanceMatch",
        tags: new[] { "Resonance", "U-CORE", "Optimization", "Matching" },
        responses: new[] {
            "200:ResonanceMatchResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    public async Task<object> CalculateResonanceMatch([ApiParameter("request", "Resonance calculation request", Required = true, Location = "body")] ResonanceCalculationRequest request)
    {
        try
        {
            // Get user belief system
            var userBeliefSystem = GetUserBeliefSystem(request.UserId);
            if (userBeliefSystem == null)
            {
                return new ErrorResponse($"User belief system not found for user: {request.UserId}");
            }

            // Map LLM response to U-CORE ontology
            var responseMapping = await MapResponseToOntology(request.Response, request.ResponseType);
            
            // Calculate resonance matches
            var resonanceMatch = await CalculateResonanceMatch(
                responseMapping, 
                userBeliefSystem, 
                request.ResponseId
            );

            // Store the match result
            var matchNode = CreateResonanceMatchNode(resonanceMatch);
            _registry.Upsert(matchNode);

            return new ResonanceMatchResponse(
                Success: true,
                Message: "Resonance match calculated successfully",
                Match: resonanceMatch,
                Optimization: GenerateOptimizationRecommendations(resonanceMatch),
                Statistics: GenerateResonanceStatistics(resonanceMatch)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to calculate resonance match: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ucore/resonance/optimize", "ucore-resonance-optimize", "Optimize resonance field for user", "codex.ucore.resonance")]
    [ApiDocumentation(
        summary: "Optimize resonance field for user",
        description: "Optimizes resonance field parameters for maximum alignment with user belief system",
        operationId: "optimizeResonanceField",
        tags: new[] { "Resonance", "U-CORE", "Optimization", "Field" },
        responses: new[] {
            "200:ResonanceOptimizationResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    public async Task<object> OptimizeResonanceField([ApiParameter("request", "Resonance optimization request", Required = true, Location = "body")] ResonanceOptimizationRequest request)
    {
        try
        {
            var userBeliefSystem = GetUserBeliefSystem(request.UserId);
            if (userBeliefSystem == null)
            {
                return new ErrorResponse($"User belief system not found for user: {request.UserId}");
            }

            // Perform resonance field optimization
            var optimization = await OptimizeResonanceField(userBeliefSystem, request.Parameters);
            
            // Update user belief system with optimized parameters
            var updatedBeliefSystem = UpdateBeliefSystemWithOptimization(userBeliefSystem, optimization);
            _userBeliefSystems[request.UserId] = updatedBeliefSystem;

            return new ResonanceOptimizationResponse(
                Success: true,
                Message: "Resonance field optimized successfully",
                Optimization: optimization,
                UpdatedBeliefSystem: updatedBeliefSystem,
                Recommendations: GenerateOptimizationRecommendations(optimization)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to optimize resonance field: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ucore/resonance/map-response", "ucore-resonance-map-response", "Map LLM response to U-CORE ontology", "codex.ucore.resonance")]
    [ApiDocumentation(
        summary: "Map LLM response to U-CORE ontology",
        description: "Maps LLM response content to U-CORE ontology axes and concepts",
        operationId: "mapResponseToOntology",
        tags: new[] { "Resonance", "U-CORE", "Mapping", "Ontology" },
        responses: new[] {
            "200:ResponseMappingResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    public async Task<object> MapResponseToOntology([ApiParameter("request", "Response mapping request", Required = true, Location = "body")] ResponseMappingRequest request)
    {
        try
        {
            var mapping = await MapResponseToOntology(request.Response, request.ResponseType);
            
            return new ResponseMappingResponse(
                Success: true,
                Message: "Response mapped to U-CORE ontology successfully",
                Mapping: mapping,
                Ontology: _ontology,
                Analysis: AnalyzeResponseMapping(mapping)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to map response to ontology: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/ucore/resonance/ontology", "ucore-resonance-ontology", "Get U-CORE ontology", "codex.ucore.resonance")]
    [ApiDocumentation(
        summary: "Get U-CORE ontology",
        description: "Retrieves the complete U-CORE ontology structure",
        operationId: "getUCoreOntology",
        tags: new[] { "Resonance", "U-CORE", "Ontology" },
        responses: new[] {
            "200:UCoreOntologyResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    public async Task<object> GetUCoreOntology()
    {
        try
        {
            return new UCoreOntologyResponse(
                Success: true,
                Message: "U-CORE ontology retrieved successfully",
                Ontology: _ontology,
                Statistics: GenerateOntologyStatistics(_ontology)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get U-CORE ontology: {ex.Message}");
        }
    }

    // Helper methods

    private UCoreOntology InitializeUCoreOntology()
    {
        return new UCoreOntology(
            Id: "ucore-ontology-v1",
            Version: "1.0.0",
            Axes: new Dictionary<string, UCoreAxis>
            {
                ["consciousness"] = new UCoreAxis(
                    Name: "consciousness",
                    Dimensions: new List<string> { "awareness", "intention", "presence", "clarity" },
                    Range: new List<double> { 0.0, 1.0 },
                    ResonanceFrequency: 432.0,
                    Weight: 1.0,
                    Description: "The axis of conscious awareness and intention",
                    Properties: new Dictionary<string, object>
                    {
                        ["chakra"] = "heart",
                        ["element"] = "air",
                        ["color"] = "green"
                    }
                ),
                ["reality"] = new UCoreAxis(
                    Name: "reality",
                    Dimensions: new List<string> { "physical", "mental", "emotional", "spiritual" },
                    Range: new List<double> { 0.0, 1.0 },
                    ResonanceFrequency: 528.0,
                    Weight: 0.8,
                    Description: "The axis of reality manifestation",
                    Properties: new Dictionary<string, object>
                    {
                        ["chakra"] = "solar_plexus",
                        ["element"] = "fire",
                        ["color"] = "yellow"
                    }
                ),
                ["connection"] = new UCoreAxis(
                    Name: "connection",
                    Dimensions: new List<string> { "unity", "harmony", "flow", "integration" },
                    Range: new List<double> { 0.0, 1.0 },
                    ResonanceFrequency: 741.0,
                    Weight: 0.9,
                    Description: "The axis of connection and unity",
                    Properties: new Dictionary<string, object>
                    {
                        ["chakra"] = "crown",
                        ["element"] = "ether",
                        ["color"] = "violet"
                    }
                )
            },
            Topology: new UCoreTopology(
                Nodes: new List<string> { "consciousness", "reality", "connection" },
                Edges: new List<UCoreEdge>
                {
                    new UCoreEdge(
                        From: "consciousness",
                        To: "reality",
                        Weight: 0.8,
                        Type: "influences",
                        ResonanceStrength: 0.85,
                        Description: "Consciousness influences reality manifestation",
                        Properties: new Dictionary<string, object>()
                    ),
                    new UCoreEdge(
                        From: "reality",
                        To: "connection",
                        Weight: 0.7,
                        Type: "enables",
                        ResonanceStrength: 0.75,
                        Description: "Reality enables connection",
                        Properties: new Dictionary<string, object>()
                    ),
                    new UCoreEdge(
                        From: "connection",
                        To: "consciousness",
                        Weight: 0.9,
                        Type: "amplifies",
                        ResonanceStrength: 0.95,
                        Description: "Connection amplifies consciousness",
                        Properties: new Dictionary<string, object>()
                    )
                },
                ResonanceMatrix: new List<List<double>>
                {
                    new List<double> { 1.0, 0.8, 0.9 },
                    new List<double> { 0.8, 1.0, 0.7 },
                    new List<double> { 0.9, 0.7, 1.0 }
                },
                Properties: new Dictionary<string, object>()
            ),
            ResonanceFields: new List<ResonanceField>
            {
                new ResonanceField(
                    Id: "heart-chakra-field",
                    Name: "Heart Chakra Resonance Field",
                    Frequency: 432.0,
                    Axes: new List<string> { "consciousness", "connection" },
                    Strength: 0.85,
                    Description: "Resonance field for heart chakra alignment",
                    Properties: new Dictionary<string, object>()
                ),
                new ResonanceField(
                    Id: "crown-chakra-field",
                    Name: "Crown Chakra Resonance Field",
                    Frequency: 741.0,
                    Axes: new List<string> { "connection" },
                    Strength: 0.9,
                    Description: "Resonance field for crown chakra alignment",
                    Properties: new Dictionary<string, object>()
                )
            },
            CreatedAt: DateTime.UtcNow
        );
    }

    private async Task<ResponseOntologyMapping> MapResponseToOntology(string response, string responseType)
    {
        await Task.Delay(10); // Simulate async processing
        
        // Analyze response content for U-CORE axis alignment
        var axisScores = AnalyzeAxisAlignment(response);
        var conceptScores = AnalyzeConceptAlignment(response);
        var frequencyAnalysis = AnalyzeFrequencyContent(response);
        
        return new ResponseOntologyMapping(
            ResponseId: Guid.NewGuid().ToString(),
            Response: response,
            ResponseType: responseType,
            AxisScores: axisScores,
            ConceptScores: conceptScores,
            FrequencyAnalysis: frequencyAnalysis,
            MappedAt: DateTime.UtcNow
        );
    }

    private Dictionary<string, double> AnalyzeAxisAlignment(string response)
    {
        var axisScores = new Dictionary<string, double>();
        
        // Consciousness axis analysis
        var consciousnessKeywords = new[] { "awareness", "consciousness", "mind", "intention", "presence", "clarity", "awakening" };
        var consciousnessScore = CalculateKeywordScore(response, consciousnessKeywords);
        axisScores["consciousness"] = consciousnessScore;
        
        // Reality axis analysis
        var realityKeywords = new[] { "reality", "manifestation", "physical", "material", "world", "experience", "form" };
        var realityScore = CalculateKeywordScore(response, realityKeywords);
        axisScores["reality"] = realityScore;
        
        // Connection axis analysis
        var connectionKeywords = new[] { "connection", "unity", "harmony", "flow", "integration", "oneness", "love" };
        var connectionScore = CalculateKeywordScore(response, connectionKeywords);
        axisScores["connection"] = connectionScore;
        
        return axisScores;
    }

    private Dictionary<string, double> AnalyzeConceptAlignment(string response)
    {
        var conceptScores = new Dictionary<string, double>();
        
        // Common spiritual and consciousness concepts
        var concepts = new Dictionary<string, string[]>
        {
            ["spirituality"] = new[] { "spiritual", "divine", "sacred", "holy", "transcendent", "enlightenment" },
            ["consciousness"] = new[] { "consciousness", "awareness", "mind", "soul", "spirit", "being" },
            ["transformation"] = new[] { "transform", "change", "evolve", "growth", "development", "progress" },
            ["healing"] = new[] { "heal", "healing", "recovery", "restoration", "wholeness", "balance" },
            ["love"] = new[] { "love", "compassion", "kindness", "caring", "empathy", "understanding" },
            ["wisdom"] = new[] { "wisdom", "knowledge", "understanding", "insight", "clarity", "truth" }
        };
        
        foreach (var concept in concepts)
        {
            var score = CalculateKeywordScore(response, concept.Value);
            conceptScores[concept.Key] = score;
        }
        
        return conceptScores;
    }

    private FrequencyAnalysis AnalyzeFrequencyContent(string response)
    {
        // Analyze response for frequency-related content
        var frequencyKeywords = new[] { "432", "528", "741", "frequency", "vibration", "resonance", "hz" };
        var frequencyScore = CalculateKeywordScore(response, frequencyKeywords);
        
        return new FrequencyAnalysis(
            DetectedFrequencies: ExtractFrequencies(response),
            ResonanceScore: frequencyScore,
            HarmonicContent: AnalyzeHarmonicContent(response),
            AnalyzedAt: DateTime.UtcNow
        );
    }

    private double CalculateKeywordScore(string text, string[] keywords)
    {
        var textLower = text.ToLowerInvariant();
        var matches = keywords.Count(keyword => textLower.Contains(keyword.ToLowerInvariant()));
        return Math.Min(1.0, (double)matches / keywords.Length);
    }

    private List<double> ExtractFrequencies(string text)
    {
        var frequencies = new List<double>();
        var frequencyPattern = @"\b(432|528|741|852|963)\b";
        var matches = System.Text.RegularExpressions.Regex.Matches(text, frequencyPattern);
        
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (double.TryParse(match.Value, out var frequency))
            {
                frequencies.Add(frequency);
            }
        }
        
        return frequencies;
    }

    private Dictionary<string, double> AnalyzeHarmonicContent(string text)
    {
        return new Dictionary<string, double>
        {
            ["fundamental"] = 1.0,
            ["second_harmonic"] = 0.5,
            ["third_harmonic"] = 0.33
        };
    }

    private async Task<ResonanceMatch> CalculateResonanceMatch(
        ResponseOntologyMapping responseMapping, 
        UserBeliefSystem userBeliefSystem, 
        string responseId)
    {
        await Task.Delay(10); // Simulate async processing
        
        // Calculate axis matches
        var axisMatches = new Dictionary<string, double>();
        foreach (var axis in _ontology.Axes.Keys)
        {
            var responseScore = responseMapping.AxisScores.GetValueOrDefault(axis, 0.0);
            var userPreference = userBeliefSystem.Axes.GetValueOrDefault(axis, 0.0);
            var match = CalculateAxisMatch(responseScore, userPreference);
            axisMatches[axis] = match;
        }
        
        // Calculate concept matches
        var conceptMatches = new List<ConceptMatch>();
        foreach (var userConcept in userBeliefSystem.Concepts)
        {
            var responseScore = responseMapping.ConceptScores.GetValueOrDefault(userConcept.Name, 0.0);
            var match = CalculateConceptMatch(responseScore, userConcept);
            conceptMatches.Add(match);
        }
        
        // Calculate overall match
        var overallMatch = CalculateOverallMatch(axisMatches, conceptMatches);
        
        // Calculate optimization score
        var optimizationScore = CalculateOptimizationScore(axisMatches, conceptMatches);
        
        // Generate recommendations
        var recommendations = GenerateResonanceRecommendations(axisMatches, conceptMatches);
        
        return new ResonanceMatch(
            MatchId: Guid.NewGuid().ToString(),
            UserId: userBeliefSystem.UserId,
            ResponseId: responseId,
            OverallMatch: overallMatch,
            AxisMatches: axisMatches,
            ConceptMatches: conceptMatches,
            OptimizationScore: optimizationScore,
            Recommendations: recommendations,
            CreatedAt: DateTime.UtcNow
        );
    }

    private double CalculateAxisMatch(double responseScore, double userPreference)
    {
        // Weighted harmonic mean of response score and user preference
        if (responseScore == 0.0 && userPreference == 0.0) return 0.0;
        if (responseScore == 0.0 || userPreference == 0.0) return 0.0;
        
        return 2.0 * (responseScore * userPreference) / (responseScore + userPreference);
    }

    private ConceptMatch CalculateConceptMatch(double responseScore, WeightedConcept userConcept)
    {
        var match = CalculateAxisMatch(responseScore, userConcept.Weight);
        var resonance = match * userConcept.Investment;
        
        return new ConceptMatch(
            Concept: userConcept.Name,
            Match: match,
            Resonance: resonance,
            Weight: userConcept.Weight,
            Investment: userConcept.Investment,
            Description: $"Match for {userConcept.Name} concept"
        );
    }

    private double CalculateOverallMatch(Dictionary<string, double> axisMatches, List<ConceptMatch> conceptMatches)
    {
        var axisAverage = axisMatches.Values.Average();
        var conceptAverage = conceptMatches.Average(c => c.Match);
        
        return (axisAverage + conceptAverage) / 2.0;
    }

    private double CalculateOptimizationScore(Dictionary<string, double> axisMatches, List<ConceptMatch> conceptMatches)
    {
        var axisScore = axisMatches.Values.Average();
        var conceptScore = conceptMatches.Average(c => c.Resonance);
        
        return (axisScore + conceptScore) / 2.0;
    }

    private List<string> GenerateResonanceRecommendations(Dictionary<string, double> axisMatches, List<ConceptMatch> conceptMatches)
    {
        var recommendations = new List<string>();
        
        // Find lowest scoring axis
        var lowestAxis = axisMatches.OrderBy(kvp => kvp.Value).First();
        if (lowestAxis.Value < 0.5)
        {
            recommendations.Add($"Focus on {lowestAxis.Key} axis development");
        }
        
        // Find lowest scoring concept
        var lowestConcept = conceptMatches.OrderBy(c => c.Match).First();
        if (lowestConcept.Match < 0.5)
        {
            recommendations.Add($"Explore {lowestConcept.Concept} concept deeper");
        }
        
        // General recommendations
        if (axisMatches.Values.Average() < 0.7)
        {
            recommendations.Add("Consider expanding consciousness practices");
        }
        
        return recommendations;
    }

    private UserBeliefSystem? GetUserBeliefSystem(string userId)
    {
        return _userBeliefSystems.GetValueOrDefault(userId);
    }

    private async Task<ResonanceFieldOptimization> OptimizeResonanceField(
        UserBeliefSystem userBeliefSystem, 
        Dictionary<string, object> parameters)
    {
        await Task.Delay(10); // Simulate async processing
        
        // Perform resonance field optimization calculations
        var optimizedFrequencies = OptimizeFrequencies(userBeliefSystem);
        var optimizedAxes = OptimizeAxisWeights(userBeliefSystem);
        var optimizedConcepts = OptimizeConceptWeights(userBeliefSystem);
        
        return new ResonanceFieldOptimization(
            Id: Guid.NewGuid().ToString(),
            UserId: userBeliefSystem.UserId,
            OptimizedFrequencies: optimizedFrequencies,
            OptimizedAxes: optimizedAxes,
            OptimizedConcepts: optimizedConcepts,
            OptimizationScore: 0.85,
            CreatedAt: DateTime.UtcNow
        );
    }

    private Dictionary<string, double> OptimizeFrequencies(UserBeliefSystem userBeliefSystem)
    {
        return new Dictionary<string, double>
        {
            ["primary"] = userBeliefSystem.ResonancePreferences.Frequency,
            ["secondary"] = userBeliefSystem.ResonancePreferences.Frequency * 1.5,
            ["harmonic"] = userBeliefSystem.ResonancePreferences.Frequency * 2.0
        };
    }

    private Dictionary<string, double> OptimizeAxisWeights(UserBeliefSystem userBeliefSystem)
    {
        var optimized = new Dictionary<string, double>();
        foreach (var axis in userBeliefSystem.Axes)
        {
            optimized[axis.Key] = Math.Min(1.0, axis.Value * 1.1);
        }
        return optimized;
    }

    private Dictionary<string, double> OptimizeConceptWeights(UserBeliefSystem userBeliefSystem)
    {
        var optimized = new Dictionary<string, double>();
        foreach (var concept in userBeliefSystem.Concepts)
        {
            optimized[concept.Name] = Math.Min(1.0, concept.Weight * 1.05);
        }
        return optimized;
    }

    private UserBeliefSystem UpdateBeliefSystemWithOptimization(
        UserBeliefSystem beliefSystem, 
        ResonanceFieldOptimization optimization)
    {
        // Update belief system with optimized parameters
        var updatedConcepts = beliefSystem.Concepts.Select(concept =>
        {
            var optimizedWeight = optimization.OptimizedConcepts.GetValueOrDefault(concept.Name, concept.Weight);
            return concept with { Weight = optimizedWeight };
        }).ToList();

        return beliefSystem with
        {
            Concepts = updatedConcepts,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Node CreateOntologyNode(UCoreOntology ontology)
    {
        return new Node(
            Id: ontology.Id,
            TypeId: "codex.ucore.ontology",
            State: ContentState.Ice,
            Locale: "en",
            Title: "U-CORE Ontology",
            Description: "The fundamental U-CORE ontology structure",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(ontology),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["version"] = ontology.Version,
                ["createdAt"] = ontology.CreatedAt,
                ["axisCount"] = ontology.Axes.Count,
                ["resonanceFieldCount"] = ontology.ResonanceFields.Count
            }
        );
    }

    private Node CreateResonanceMatchNode(ResonanceMatch match)
    {
        return new Node(
            Id: match.MatchId,
            TypeId: "codex.ucore.resonance-match",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Resonance Match: {match.OverallMatch:P0}",
            Description: $"Resonance match for user {match.UserId}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(match),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["userId"] = match.UserId,
                ["responseId"] = match.ResponseId,
                ["overallMatch"] = match.OverallMatch,
                ["optimizationScore"] = match.OptimizationScore,
                ["createdAt"] = match.CreatedAt
            }
        );
    }

    private Dictionary<string, object> GenerateOptimizationRecommendations(ResonanceMatch match)
    {
        return new Dictionary<string, object>
        {
            ["overallScore"] = match.OverallMatch,
            ["optimizationScore"] = match.OptimizationScore,
            ["recommendations"] = match.Recommendations,
            ["axisScores"] = match.AxisMatches,
            ["conceptScores"] = match.ConceptMatches.Select(c => new { c.Concept, c.Match, c.Resonance }).ToList()
        };
    }

    private Dictionary<string, object> GenerateResonanceStatistics(ResonanceMatch match)
    {
        return new Dictionary<string, object>
        {
            ["overallMatch"] = match.OverallMatch,
            ["optimizationScore"] = match.OptimizationScore,
            ["axisCount"] = match.AxisMatches.Count,
            ["conceptCount"] = match.ConceptMatches.Count,
            ["averageAxisMatch"] = match.AxisMatches.Values.Average(),
            ["averageConceptMatch"] = match.ConceptMatches.Average(c => c.Match),
            ["recommendationCount"] = match.Recommendations.Count
        };
    }

    private Dictionary<string, object> GenerateOntologyStatistics(UCoreOntology ontology)
    {
        return new Dictionary<string, object>
        {
            ["version"] = ontology.Version,
            ["axisCount"] = ontology.Axes.Count,
            ["edgeCount"] = ontology.Topology.Edges.Count,
            ["resonanceFieldCount"] = ontology.ResonanceFields.Count,
            ["createdAt"] = ontology.CreatedAt
        };
    }

    private Dictionary<string, object> AnalyzeResponseMapping(ResponseOntologyMapping mapping)
    {
        return new Dictionary<string, object>
        {
            ["responseLength"] = mapping.Response.Length,
            ["responseType"] = mapping.ResponseType,
            ["axisScores"] = mapping.AxisScores,
            ["conceptScores"] = mapping.ConceptScores,
            ["frequencyAnalysis"] = mapping.FrequencyAnalysis,
            ["mappedAt"] = mapping.MappedAt
        };
    }
}

// Additional data types

[MetaNode("codex.ucore.response-mapping", "codex.meta/type", "ResponseOntologyMapping", "LLM response mapped to U-CORE ontology")]
public record ResponseOntologyMapping(
    string ResponseId,
    string Response,
    string ResponseType,
    Dictionary<string, double> AxisScores,
    Dictionary<string, double> ConceptScores,
    FrequencyAnalysis FrequencyAnalysis,
    DateTime MappedAt
);

[MetaNode("codex.ucore.frequency-analysis", "codex.meta/type", "FrequencyAnalysis", "Frequency analysis of response content")]
public record FrequencyAnalysis(
    List<double> DetectedFrequencies,
    double ResonanceScore,
    Dictionary<string, double> HarmonicContent,
    DateTime AnalyzedAt
);

[MetaNode("codex.ucore.resonance-optimization", "codex.meta/type", "ResonanceFieldOptimization", "Resonance field optimization result")]
public record ResonanceFieldOptimization(
    string Id,
    string UserId,
    Dictionary<string, double> OptimizedFrequencies,
    Dictionary<string, double> OptimizedAxes,
    Dictionary<string, double> OptimizedConcepts,
    double OptimizationScore,
    DateTime CreatedAt
);

// Request/Response Types

[RequestType("codex.ucore.resonance-calculation-request", "ResonanceCalculationRequest", "Resonance calculation request")]
public record ResonanceCalculationRequest(
    string UserId,
    string Response,
    string ResponseType = "text",
    string ResponseId = ""
);

[ResponseType("codex.ucore.resonance-match-response", "ResonanceMatchResponse", "Resonance match response")]
public record ResonanceMatchResponse(
    bool Success,
    string Message,
    ResonanceMatch Match,
    Dictionary<string, object> Optimization,
    Dictionary<string, object> Statistics
);

[RequestType("codex.ucore.resonance-optimization-request", "ResonanceOptimizationRequest", "Resonance optimization request")]
public record ResonanceOptimizationRequest(
    string UserId,
    Dictionary<string, object> Parameters
);

[ResponseType("codex.ucore.resonance-optimization-response", "ResonanceOptimizationResponse", "Resonance optimization response")]
public record ResonanceOptimizationResponse(
    bool Success,
    string Message,
    ResonanceFieldOptimization Optimization,
    UserBeliefSystem UpdatedBeliefSystem,
    List<string> Recommendations
);

[RequestType("codex.ucore.response-mapping-request", "ResponseMappingRequest", "Response mapping request")]
public record ResponseMappingRequest(
    string Response,
    string ResponseType = "text"
);

[ResponseType("codex.ucore.response-mapping-response", "ResponseMappingResponse", "Response mapping response")]
public record ResponseMappingResponse(
    bool Success,
    string Message,
    ResponseOntologyMapping Mapping,
    UCoreOntology Ontology,
    Dictionary<string, object> Analysis
);

[ResponseType("codex.ucore.ontology-response", "UCoreOntologyResponse", "U-CORE ontology response")]
public record UCoreOntologyResponse(
    bool Success,
    string Message,
    UCoreOntology Ontology,
    Dictionary<string, object> Statistics
);
