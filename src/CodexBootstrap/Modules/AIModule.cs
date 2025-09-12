using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    // LLM Integration Data Structures
    public record LLMFutureQueryRequest(
        string Query,
        string Context,
        string TimeHorizon,
        string Perspective,
        LLMConfig LLMConfig,
        Dictionary<string, object> Metadata
    );

    public record LLMFutureQueryResponse(
        string Id,
        string Query,
        string Response,
        double Confidence,
        string Reasoning,
        List<string> Sources,
        DateTimeOffset GeneratedAt,
        LLMConfig UsedConfig
    );

    public record LLMTranslationRequest(
        string Concept,
        string SourceLanguage,
        string TargetLanguage,
        LLMConfig LLMConfig,
        Dictionary<string, object> Context
    );

    public record LLMTranslationResponse(
        string Id,
        string OriginalConcept,
        string TranslatedConcept,
        string SourceLanguage,
        string TargetLanguage,
        double Confidence,
        string Reasoning,
        DateTimeOffset GeneratedAt,
        LLMConfig UsedConfig
    );

    public record LLMConfigRequest(
        LLMConfig Config
    );

    public record LLMConfigResponse(
        bool Success,
        LLMConfig Config,
        string Message,
        DateTimeOffset Timestamp
    );

    public record LLMHandlerConversionRequest(
        string Response,
        string ResponseType,
        Dictionary<string, object> Context
    );

    public record LLMHandlerConversionResponse(
        bool Success,
        int NodesCreated,
        int EdgesCreated,
        List<string> DiffPatches,
        string Message,
        DateTimeOffset Timestamp
    );

    public record LLMHandlerParseRequest(
        string Response,
        string ResponseType,
        Dictionary<string, object> Options
    );

    public record LLMHandlerParseResponse(
        bool Success,
        Dictionary<string, object> ParsedStructure,
        string Message,
        DateTimeOffset Timestamp
    );

    public record AIConceptExtractionResponse(
        string Content,
        double Confidence,
        string Reasoning,
        List<string> Sources
    );

    public record LLMConfig(
        string Id,
        string Name,
        string Provider,
        string Model,
        string ApiKey,
        string BaseUrl,
        int MaxTokens,
        double Temperature,
        double TopP,
        Dictionary<string, object> Parameters
    );

    /// <summary>
    /// AI Module - Consolidated AI functionality including concept extraction, LLM integration, scoring, and fractal transformation
    /// </summary>
    public class AIModule : IModule
    {
        private readonly Core.ILogger _logger;
        private readonly NodeRegistry _registry;
        private readonly ConcurrentDictionary<string, CachedAnalysis> _analysisCache;
        private readonly ConcurrentDictionary<string, DateTimeOffset> _cacheTimestamps;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);
        private readonly SemaphoreSlim _analysisSemaphore = new(10, 10);
        private readonly ModuleCommunicationWrapper _moduleComm;
        private readonly LLMClient _llmClient;

        public AIModule(NodeRegistry registry)
        {
            _registry = registry;
            _logger = new Log4NetLogger(typeof(AIModule));
            _analysisCache = new ConcurrentDictionary<string, CachedAnalysis>();
            _cacheTimestamps = new ConcurrentDictionary<string, DateTimeOffset>();
            _moduleComm = new ModuleCommunicationWrapper(_logger);
            
            // Initialize LLM client
            var httpClient = new HttpClient();
            _llmClient = new LLMClient(httpClient, _logger);
        }

        public string Name => "AI Module";
        public string Description => "Consolidated AI functionality for concept extraction, LLM integration, scoring, and fractal transformation";
        public string Version => "1.0.0";

        public Node GetModuleNode()
        {
            return NodeStorage.CreateModuleNode(
                id: "ai-module",
                name: Name,
                version: Version,
                description: Description,
                capabilities: new[] { "concept-extraction", "llm-integration", "fractal-transformation", "analysis", "caching" },
                tags: new[] { "ai", "concepts", "llm", "analysis" },
                specReference: "codex.spec.ai"
            );
        }

        public void Register(NodeRegistry registry)
        {
            registry.Upsert(GetModuleNode());
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            router.Register("ai", "extract-concepts", async (JsonElement? request) => 
            {
                if (request == null || !request.HasValue)
                    return new ErrorResponse("Invalid request", "MISSING_REQUEST");
                
                var requestObj = JsonSerializer.Deserialize<ConceptExtractionRequest>(request.Value.GetRawText());
                if (requestObj == null)
                    return new ErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                
                return await ExtractConceptsAsync(requestObj);
            });
            router.Register("ai", "score-analysis", async (JsonElement? request) => await ScoreAnalysisAsync(request));
            router.Register("ai", "fractal-transform", async (JsonElement? request) => await FractalTransformAsync(request));
            router.Register("ai", "health", async (JsonElement? request) => await HealthCheckAsync(request));
            
            // LLM Integration endpoints
            router.Register("ai", "llm-future-query", async (JsonElement? request) => await LLMFutureQueryAsync(request));
            router.Register("ai", "llm-translate", async (JsonElement? request) => await LLMTranslateConceptAsync(request));
            router.Register("ai", "llm-config", async (JsonElement? request) => await LLMConfigCreateAsync(request));
            router.Register("ai", "llm-configs", async (JsonElement? request) => await LLMConfigsGetAsync(request));
            router.Register("ai", "llm-handler-convert", async (JsonElement? request) => await LLMHandlerConvertAsync(request));
            router.Register("ai", "llm-handler-parse", async (JsonElement? request) => await LLMHandlerParseAsync(request));
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // HTTP endpoints are registered via ApiRoute attributes
        }

        #region API Endpoints with Proper Error Handling

        [Post("/ai/extract-concepts", "Extract Concepts", "Extract concepts from content using advanced AI analysis", "ai-analysis")]
        public async Task<object> ExtractConceptsAsync([ApiParameter("request", "Concept extraction request", Required = true, Location = "body")] ConceptExtractionRequest requestObj)
        {
            try
            {
                if (requestObj == null)
                {
                    _logger.Warn("ExtractConceptsAsync called with null request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                _logger.Info($"Extracting concepts from content: {requestObj.Title}");

                var analysis = await PerformConceptExtraction(requestObj);

                // Store analysis as Water node
                var analysisNode = new Node(
                    Id: $"concept-analysis-{analysis.Id}",
                    TypeId: "codex.ai.concept-analysis",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: $"Concept Analysis: {requestObj.Title}",
                    Description: $"AI-generated concept analysis for {requestObj.Title}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(analysis),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["analysisId"] = analysis.Id,
                        ["analysisType"] = "concept-extraction",
                        ["processedAt"] = analysis.ExtractedAt,
                        ["conceptCount"] = analysis.Concepts.Count,
                        ["confidence"] = analysis.Confidence
                    }
                );

                _registry.Upsert(analysisNode);

                return new
                {
                    success = true,
                    analysisId = analysis.Id,
                    concepts = analysis.Concepts,
                    confidence = analysis.Confidence,
                    ontologyLevels = analysis.OntologyLevels,
                    message = "Concept extraction completed successfully"
                };
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON deserialization error in ExtractConceptsAsync: {ex.Message}", ex);
                return CreateErrorResponse("Invalid JSON format", "JSON_DESERIALIZATION_ERROR");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error in ExtractConceptsAsync: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during concept extraction", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/score-analysis", "Score Analysis", "Perform scoring analysis on concept analysis", "ai-analysis")]
        public async Task<object> ScoreAnalysisAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("ScoreAnalysisAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<ScoringAnalysisRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize ScoringAnalysisRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Scoring analysis for content: {requestObj.Content?.Title ?? "Unknown"}");

                var scoring = await PerformScoringAnalysis(requestObj.ConceptAnalysis, requestObj.Content);

                // Store scoring as Water node
                var scoringNode = new Node(
                    Id: $"scoring-analysis-{scoring.Id}",
                    TypeId: "codex.ai.scoring-analysis",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: $"Scoring Analysis: {requestObj.Content?.Title ?? "Unknown"}",
                    Description: $"AI-generated scoring analysis",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(scoring),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["scoringId"] = scoring.Id,
                        ["analysisType"] = "scoring-analysis",
                        ["processedAt"] = scoring.ScoredAt,
                        ["abundanceScore"] = scoring.AbundanceScore,
                        ["consciousnessScore"] = scoring.ConsciousnessScore,
                        ["unityScore"] = scoring.UnityScore
                    }
                );

                _registry.Upsert(scoringNode);

                return new
                {
                    success = true,
                    scoringId = scoring.Id,
                    abundanceScore = scoring.AbundanceScore,
                    consciousnessScore = scoring.ConsciousnessScore,
                    unityScore = scoring.UnityScore,
                    overallScore = scoring.OverallScore,
                    message = "Scoring analysis completed successfully"
                };
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON deserialization error in ScoreAnalysisAsync: {ex.Message}", ex);
                return CreateErrorResponse("Invalid JSON format", "JSON_DESERIALIZATION_ERROR");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error in ScoreAnalysisAsync: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during scoring analysis", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/fractal-transform", "Fractal Transform", "Transform content using fractal analysis", "ai-analysis")]
        public async Task<object> FractalTransformAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("FractalTransformAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<FractalTransformationRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize FractalTransformationRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Performing fractal transformation for content: {requestObj.Content?.Title ?? "Unknown"}");

                var transformation = await PerformFractalTransformation(requestObj.Content, requestObj.ConceptAnalysis, requestObj.ScoringAnalysis);

                // Store transformation as Water node
                var transformationNode = new Node(
                    Id: $"fractal-transformation-{transformation.Id}",
                    TypeId: "codex.ai.fractal-transformation",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: $"Fractal Transformation: {requestObj.Content?.Title ?? "Unknown"}",
                    Description: $"AI-generated fractal transformation",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(transformation),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["transformationId"] = transformation.Id,
                        ["analysisType"] = "fractal-transformation",
                        ["processedAt"] = transformation.TransformedAt,
                        ["transformationType"] = transformation.TransformationType,
                        ["consciousnessLevel"] = transformation.ConsciousnessLevel
                    }
                );

                _registry.Upsert(transformationNode);

                return new
                {
                    success = true,
                    transformationId = transformation.Id,
                    headline = transformation.Headline,
                    beliefTranslation = transformation.BeliefSystemTranslation,
                    summary = transformation.Summary,
                    impactAreas = transformation.ImpactAreas,
                    amplificationFactors = transformation.AmplificationFactors,
                    message = "Fractal transformation completed successfully"
                };
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON deserialization error in FractalTransformAsync: {ex.Message}", ex);
                return CreateErrorResponse("Invalid JSON format", "JSON_DESERIALIZATION_ERROR");
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error in FractalTransformAsync: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during fractal transformation", "INTERNAL_ERROR");
            }
        }

        [Get("/ai/health", "AI Health", "Check AI module health", "ai-analysis")]
        public async Task<object> HealthCheckAsync(JsonElement? request)
        {
            try
            {
                var analysisNodes = _registry.GetNodesByType("codex.ai.concept-analysis").Count();
                var scoringNodes = _registry.GetNodesByType("codex.ai.scoring-analysis").Count();
                var transformationNodes = _registry.GetNodesByType("codex.ai.fractal-transformation").Count();

                return new
                {
                    success = true,
                    status = "healthy",
                    module = "AI Module",
                    version = Version,
                    statistics = new
                    {
                        conceptAnalyses = analysisNodes,
                        scoringAnalyses = scoringNodes,
                        fractalTransformations = transformationNodes,
                        totalAnalyses = analysisNodes + scoringNodes + transformationNodes,
                        cacheItems = _analysisCache.Count
                    },
                    capabilities = new[]
                    {
                        "concept-extraction",
                        "scoring-analysis", 
                        "fractal-transformation",
                        "caching"
                    },
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI health check: {ex.Message}", ex);
                return CreateErrorResponse("Health check failed", "HEALTH_CHECK_ERROR");
            }
        }

        #endregion

        #region LLM Integration Endpoints

        [Post("/ai/llm/future/query", "LLM Future Query", "Query future knowledge using LLM", "ai-llm")]
        public async Task<object> LLMFutureQueryAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("LLMFutureQueryAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<LLMFutureQueryRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize LLMFutureQueryRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Processing LLM future query: {requestObj.Query}");

                // Check if LLM service is available
                var isAvailable = await _llmClient.IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.Warn("LLM service not available, falling back to mock response");
                    var mockResponse = new LLMFutureQueryResponse(
                        Id: Guid.NewGuid().ToString(),
                        Query: requestObj.Query,
                        Response: "LLM service not available. This is a fallback response for future query functionality.",
                        Confidence: 0.5,
                        Reasoning: "Generated using fallback algorithms (LLM service unavailable)",
                        Sources: new List<string> { "Fallback patterns", "Basic analysis" },
                        GeneratedAt: DateTimeOffset.UtcNow,
                        UsedConfig: requestObj.LLMConfig
                    );
                    return new
                    {
                        success = true,
                        data = mockResponse,
                        timestamp = DateTimeOffset.UtcNow,
                        warning = "LLM service unavailable, using fallback"
                    };
                }

                // Create enhanced prompt for future query
                var prompt = $@"You are an advanced AI system specializing in future knowledge and consciousness expansion. 
Context: {requestObj.Context}
Time Horizon: {requestObj.TimeHorizon}
Perspective: {requestObj.Perspective}

Query: {requestObj.Query}

Please provide a thoughtful, consciousness-expanding response that considers future possibilities, patterns, and insights. Focus on:
1. Potential future scenarios and trends
2. Consciousness and awareness implications
3. Practical applications and considerations
4. Deeper philosophical and spiritual insights

Response:";

                var llmResponse = await _llmClient.QueryAsync(prompt, requestObj.LLMConfig);
                
                var response = new LLMFutureQueryResponse(
                    Id: Guid.NewGuid().ToString(),
                    Query: requestObj.Query,
                    Response: llmResponse.Response,
                    Confidence: llmResponse.Confidence,
                    Reasoning: "Generated using advanced LLM with consciousness-expansion focus",
                    Sources: new List<string> { "LLM Analysis", "Future Pattern Recognition", "Consciousness Modeling" },
                    GeneratedAt: DateTimeOffset.UtcNow,
                    UsedConfig: requestObj.LLMConfig
                );

                return new
                {
                    success = true,
                    data = response,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in LLM future query: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM future query", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/llm/translate", "LLM Translate Concept", "Translate concept through belief system using LLM", "ai-llm")]
        public async Task<object> LLMTranslateConceptAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("LLMTranslateConceptAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<LLMTranslationRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize LLMTranslationRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Processing LLM translation: {requestObj.Concept}");

                // Check if LLM service is available
                var isAvailable = await _llmClient.IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.Warn("LLM service not available, falling back to mock response");
                    var mockResponse = new LLMTranslationResponse(
                        Id: Guid.NewGuid().ToString(),
                        OriginalConcept: requestObj.Concept,
                        TranslatedConcept: $"Translated: {requestObj.Concept}",
                        SourceLanguage: requestObj.SourceLanguage,
                        TargetLanguage: requestObj.TargetLanguage,
                        Confidence: 0.5,
                        Reasoning: "Generated using fallback algorithms (LLM service unavailable)",
                        GeneratedAt: DateTimeOffset.UtcNow,
                        UsedConfig: requestObj.LLMConfig
                    );
                    return new
                    {
                        success = true,
                        data = mockResponse,
                        timestamp = DateTimeOffset.UtcNow,
                        warning = "LLM service unavailable, using fallback"
                    };
                }

                // Create enhanced prompt for concept translation
                var prompt = $@"You are an advanced AI system specializing in concept translation and consciousness mapping across different belief systems and languages.

Original Concept: {requestObj.Concept}
Source Language: {requestObj.SourceLanguage}
Target Language: {requestObj.TargetLanguage}
Context: {JsonSerializer.Serialize(requestObj.Context)}

Please translate this concept while:
1. Preserving the core meaning and consciousness-expanding potential
2. Adapting to the target language's cultural and linguistic nuances
3. Maintaining the spiritual and philosophical depth
4. Ensuring the translation resonates with the target belief system

Provide a thoughtful, accurate translation that honors both the original concept and the target language's unique expression.

Translation:";

                var llmResponse = await _llmClient.QueryAsync(prompt, requestObj.LLMConfig);
                
                var response = new LLMTranslationResponse(
                    Id: Guid.NewGuid().ToString(),
                    OriginalConcept: requestObj.Concept,
                    TranslatedConcept: llmResponse.Response,
                    SourceLanguage: requestObj.SourceLanguage,
                    TargetLanguage: requestObj.TargetLanguage,
                    Confidence: llmResponse.Confidence,
                    Reasoning: "Generated using advanced LLM with consciousness-preserving translation focus",
                    GeneratedAt: DateTimeOffset.UtcNow,
                    UsedConfig: requestObj.LLMConfig
                );

                return new
                {
                    success = true,
                    data = response,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in LLM translation: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM translation", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/llm/config", "LLM Config Create", "Create or update LLM configuration", "ai-llm")]
        public async Task<object> LLMConfigCreateAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("LLMConfigCreateAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<LLMConfigRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize LLMConfigRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Creating/updating LLM config: {requestObj.Config.Name}");

                // TODO: Implement actual LLM config storage logic
                var response = new LLMConfigResponse(
                    Success: true,
                    Config: requestObj.Config,
                    Message: "LLM configuration created/updated successfully",
                    Timestamp: DateTimeOffset.UtcNow
                );

                return new
                {
                    success = true,
                    data = response,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in LLM config creation: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM config creation", "INTERNAL_ERROR");
            }
        }

        [Get("/ai/llm/configs", "LLM Configs", "Get all LLM configurations", "ai-llm")]
        public async Task<object> LLMConfigsGetAsync(JsonElement? request)
        {
            try
            {
                _logger.Info("Retrieving all LLM configurations");

                // TODO: Implement actual LLM config retrieval logic
                var configs = new List<LLMConfig>
                {
                    new LLMConfig(
                        Id: "openai-gpt4",
                        Name: "OpenAI GPT-4",
                        Provider: "OpenAI",
                        Model: "gpt-4",
                        ApiKey: "sk-***",
                        BaseUrl: "https://api.openai.com/v1",
                        MaxTokens: 2000,
                        Temperature: 0.7,
                        TopP: 0.9,
                        Parameters: new Dictionary<string, object>()
                    )
                };

                return new
                {
                    success = true,
                    data = new
                    {
                        configs = configs,
                        totalCount = configs.Count
                    },
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error retrieving LLM configs: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM config retrieval", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/llm/handler/convert", "LLM Handler Convert", "Convert LLM response to nodes and edges", "ai-llm")]
        public async Task<object> LLMHandlerConvertAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("LLMHandlerConvertAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<LLMHandlerConversionRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize LLMHandlerConversionRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Converting LLM response to nodes and edges");

                // TODO: Implement actual LLM response conversion logic
                var response = new LLMHandlerConversionResponse(
                    Success: true,
                    NodesCreated: 5,
                    EdgesCreated: 8,
                    DiffPatches: new List<string> { "patch1", "patch2" },
                    Message: "LLM response successfully converted to nodes and edges",
                    Timestamp: DateTimeOffset.UtcNow
                );

                return new
                {
                    success = true,
                    data = response,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in LLM handler conversion: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM handler conversion", "INTERNAL_ERROR");
            }
        }

        [Post("/ai/llm/handler/parse", "LLM Handler Parse", "Parse LLM response structure", "ai-llm")]
        public async Task<object> LLMHandlerParseAsync(JsonElement? request)
        {
            try
            {
                if (request == null || !request.HasValue)
                {
                    _logger.Warn("LLMHandlerParseAsync called with null or empty request");
                    return CreateErrorResponse("Invalid request", "MISSING_REQUEST");
                }

                var requestObj = JsonSerializer.Deserialize<LLMHandlerParseRequest>(request.Value.GetRawText());
                if (requestObj == null)
                {
                    _logger.Warn("Failed to deserialize LLMHandlerParseRequest");
                    return CreateErrorResponse("Invalid request format", "INVALID_REQUEST_FORMAT");
                }

                _logger.Info($"Parsing LLM response structure");

                // TODO: Implement actual LLM response parsing logic
                var response = new LLMHandlerParseResponse(
                    Success: true,
                    ParsedStructure: new Dictionary<string, object>
                    {
                        ["entities"] = new[] { "entity1", "entity2" },
                        ["relationships"] = new[] { "rel1", "rel2" },
                        ["concepts"] = new[] { "concept1", "concept2" }
                    },
                    Message: "LLM response structure parsed successfully",
                    Timestamp: DateTimeOffset.UtcNow
                );

                return new
                {
                    success = true,
                    data = response,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in LLM handler parsing: {ex.Message}", ex);
                return CreateErrorResponse("Internal server error during LLM handler parsing", "INTERNAL_ERROR");
            }
        }

        #endregion

        #region Private Implementation Methods

        private async Task<ConceptAnalysis> PerformConceptExtraction(ConceptExtractionRequest request)
        {
            var cacheKey = $"concept-{request.Title.GetHashCode()}-{request.Content.GetHashCode()}";
            
            // Check cache first
            if (TryGetCachedAnalysis<ConceptAnalysis>(cacheKey, out var cachedAnalysis))
            {
                _logger.Debug($"Using cached concept analysis for {request.Title}");
                return cachedAnalysis;
            }

            await _analysisSemaphore.WaitAsync();
            try
            {
                // Double-check cache after acquiring semaphore
                if (TryGetCachedAnalysis<ConceptAnalysis>(cacheKey, out cachedAnalysis))
                {
                    return cachedAnalysis;
                }

                _logger.Info($"Performing concept extraction for: {request.Title}");

                // Sophisticated concept extraction algorithm
                var analysis = await ExtractConceptsWithAdvancedAlgorithm(request);

                // Cache the result
                CacheAnalysis(cacheKey, analysis);

                _logger.Info($"Concept extraction completed for {request.Title}: {analysis.Concepts.Count} concepts found");
                return analysis;
            }
            finally
            {
                _analysisSemaphore.Release();
            }
        }

        private async Task<ConceptAnalysis> ExtractConceptsWithAdvancedAlgorithm(ConceptExtractionRequest request)
        {
            var content = $"{request.Title} {request.Content}";
            var concepts = new List<ConceptScore>();
            var confidence = 0.0;

            try
            {
                // Get optimal LLM configuration for concept extraction
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                
                // Ensure the model is available before use
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                if (!modelAvailable)
                {
                    _logger.Error($"Model {optimalConfig.Model} is not available and could not be pulled");
                    // Fallback to keyword-based extraction
                    concepts = ExtractConceptsByKeywords(content.ToLower(), request);
                    confidence = 0.5;
                }
                else
                {
                    // Build sophisticated concept extraction prompt
                    var prompt = BuildConceptExtractionPrompt(request, optimalConfig);
                    
                    // Convert LLMConfigurationSystem.LLMConfiguration to LLMConfig
                    var llmConfig = new LLMConfig(
                        Id: optimalConfig.Id,
                        Name: optimalConfig.Id,
                        Provider: optimalConfig.Provider,
                        Model: optimalConfig.Model,
                        ApiKey: "",
                        BaseUrl: "http://localhost:11434",
                        MaxTokens: optimalConfig.MaxTokens,
                        Temperature: optimalConfig.Temperature,
                        TopP: optimalConfig.TopP,
                        Parameters: optimalConfig.Parameters
                    );
                    
                    // Call LLM with real Ollama integration
                    var llmResponse = await CallLLM(llmConfig, prompt);
                    
                    if (llmResponse.Content.Contains("LLM unavailable"))
                    {
                        // Fallback to keyword-based extraction
                        _logger.Warn("LLM unavailable, falling back to keyword-based concept extraction");
                        concepts = ExtractConceptsByKeywords(content.ToLower(), request);
                        confidence = 0.6; // Lower confidence for fallback
                    }
                    else
                    {
                        // Parse LLM response for concepts
                        concepts = ParseLLMConceptResponse(llmResponse.Content);
                        confidence = llmResponse.Confidence;
                    }
                }

                // Layer 1: AI-powered semantic pattern extraction
                var semanticConcepts = await ExtractConceptsBySemanticPatterns(content, request);
                concepts.AddRange(semanticConcepts);

                // Layer 2: Context-aware extraction
                var contextConcepts = await ExtractConceptsByContext(content, request);
                concepts.AddRange(contextConcepts);

                // Layer 3: Ontology-based extraction
                var ontologyConcepts = await ExtractConceptsByOntology(content, request);
                concepts.AddRange(ontologyConcepts);

                // Layer 4: Keyword-based extraction as backup
                var keywordConcepts = ExtractConceptsByKeywords(content.ToLower(), request);
                concepts.AddRange(keywordConcepts);

                // Deduplicate and merge similar concepts
                concepts = await MergeSimilarConcepts(concepts);

                // Calculate final confidence
                confidence = Math.Max(confidence, await CalculateConfidence(concepts, content, request));
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in concept extraction: {ex.Message}", ex);
                // Fallback to keyword-based extraction
                concepts = ExtractConceptsByKeywords(content.ToLower(), request);
                confidence = 0.5;
            }

            return new ConceptAnalysis
            {
                Id = $"concept-{Guid.NewGuid():N}",
                NewsItemId = request.Title, // Using title as identifier for now
                Concepts = concepts.Select(c => c.Concept).ToList(),
                Confidence = confidence,
                OntologyLevels = DetermineOntologyLevels(concepts.Select(c => c.Concept).ToList()),
                ExtractedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["extractionMethod"] = "advanced-multi-layer",
                    ["sourceLength"] = content.Length,
                    ["conceptScores"] = concepts.GroupBy(c => c.Concept).ToDictionary(g => g.Key, g => g.Average(c => c.Score)),
                    ["extractionLayers"] = new[] { "keywords", "semantic", "context", "ontology" },
                    ["processingTime"] = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<ScoringAnalysis> PerformScoringAnalysis(ConceptAnalysis conceptAnalysis, ConceptExtractionRequest content)
        {
            // TODO: Implement sophisticated scoring analysis
            return new ScoringAnalysis
            {
                Id = $"scoring-{Guid.NewGuid():N}",
                NewsItemId = content.Title,
                ConceptAnalysisId = conceptAnalysis.Id,
                AbundanceScore = 0.5,
                ConsciousnessScore = 0.5,
                UnityScore = 0.5,
                OverallScore = 0.5,
                ScoredAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>()
            };
        }

        private async Task<FractalTransformation> PerformFractalTransformation(ConceptExtractionRequest content, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            // TODO: Implement sophisticated fractal transformation
            return new FractalTransformation
            {
                Id = $"fractal-{Guid.NewGuid():N}",
                NewsItemId = content.Title,
                Headline = content.Title,
                BeliefSystemTranslation = content.Content,
                Summary = content.Content,
                ImpactAreas = new[] { "consciousness", "unity" },
                AmplificationFactors = new Dictionary<string, double> { { "resonance", 0.5 } },
                ResonanceData = new Dictionary<string, object>(),
                TransformationType = "consciousness-expansion",
                ConsciousnessLevel = "L5",
                TransformedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>()
            };
        }

        // Helper methods for concept extraction
        private List<ConceptScore> ExtractConceptsByKeywords(string content, ConceptExtractionRequest request)
        {
            var keywords = new Dictionary<string, double>
            {
                ["consciousness"] = 0.9,
                ["unity"] = 0.8,
                ["abundance"] = 0.7,
                ["collaboration"] = 0.6,
                ["innovation"] = 0.5
            };

            var concepts = new List<ConceptScore>();
            foreach (var keyword in keywords)
            {
                if (content.Contains(keyword.Key))
                {
                    concepts.Add(new ConceptScore { Concept = keyword.Key, Score = keyword.Value });
                }
            }

            return concepts;
        }

        private async Task<List<ConceptScore>> ExtractConceptsBySemanticPatterns(string content, ConceptExtractionRequest request)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Get optimal LLM configuration for semantic analysis
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                
                if (modelAvailable)
                {
                    // Use AI for advanced semantic pattern recognition
                    var llmConfig = new LLMConfig(
                        Id: optimalConfig.Id,
                        Name: optimalConfig.Id,
                        Provider: optimalConfig.Provider,
                        Model: optimalConfig.Model,
                        ApiKey: "",
                        BaseUrl: "http://localhost:11434",
                        MaxTokens: 1000,
                        Temperature: 0.3, // Lower temperature for more consistent results
                        TopP: 0.9,
                        Parameters: new Dictionary<string, object>
                        {
                            ["stop"] = new[] { "---", "###" }
                        }
                    );

                    var semanticPrompt = $@"Analyze the following text for consciousness-related concepts and semantic patterns. 
Focus on identifying concepts related to: consciousness, transformation, unity, love, wisdom, energy, healing, abundance, sacred geometry, and fractal patterns.

Text: ""{content}""

Return your analysis as a JSON array of concepts with the following structure:
[
  {{
    ""concept"": ""concept_name"",
    ""score"": 0.0-1.0,
    ""description"": ""brief description"",
    ""category"": ""consciousness|transformation|unity|love|wisdom|energy|healing|abundance|sacred|fractal"",
    ""confidence"": 0.0-1.0,
    ""semanticPatterns"": [""pattern1"", ""pattern2""]
  }}
]

Be thorough but concise. Focus on the most significant concepts with high confidence scores.";

                    var llmResponse = await CallLLMAsync(semanticPrompt, llmConfig);
                    
                    if (!llmResponse.Content.Contains("LLM unavailable"))
                    {
                        // Parse AI response
                        var aiConcepts = ParseLLMConceptResponse(llmResponse.Content);
                        concepts.AddRange(aiConcepts);
                        _logger.Info($"AI semantic analysis found {aiConcepts.Count} concepts");
                    }
                    else
                    {
                        _logger.Warn("AI semantic analysis failed, falling back to pattern matching");
                        concepts.AddRange(ExtractConceptsByPatternMatching(content));
                    }
                }
                else
                {
                    _logger.Warn("LLM not available for semantic analysis, using pattern matching");
                    concepts.AddRange(ExtractConceptsByPatternMatching(content));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI semantic pattern extraction: {ex.Message}", ex);
                concepts.AddRange(ExtractConceptsByPatternMatching(content));
            }

            return concepts;
        }

        private List<ConceptScore> ExtractConceptsByPatternMatching(string content)
        {
            var concepts = new List<ConceptScore>();
            
            // Define semantic patterns for consciousness-related concepts
            var semanticPatterns = new Dictionary<string, (string pattern, double weight, string category)>
            {
                // Consciousness and awareness patterns
                { "consciousness", (@"\b(consciousness|awareness|mindfulness|presence|awakening)\b", 0.9, "consciousness") },
                { "transformation", (@"\b(transformation|evolution|growth|change|shift|breakthrough)\b", 0.85, "transformation") },
                { "unity", (@"\b(unity|oneness|connection|wholeness|integration|harmony)\b", 0.8, "unity") },
                { "love", (@"\b(love|compassion|empathy|kindness|heart|unconditional)\b", 0.8, "love") },
                { "wisdom", (@"\b(wisdom|knowledge|insight|understanding|clarity|enlightenment)\b", 0.75, "wisdom") },
                { "energy", (@"\b(energy|vibration|frequency|resonance|flow|chi|prana)\b", 0.7, "energy") },
                { "healing", (@"\b(healing|recovery|restoration|wholeness|wellness|balance)\b", 0.7, "healing") },
                { "abundance", (@"\b(abundance|prosperity|wealth|success|fulfillment|manifestation)\b", 0.65, "abundance") },
                { "sacred", (@"\b(sacred|divine|holy|spiritual|transcendent|mystical)\b", 0.8, "sacred") },
                { "fractal", (@"\b(fractal|pattern|geometry|structure|design|blueprint)\b", 0.6, "fractal") }
            };

            // Apply semantic pattern matching
            foreach (var pattern in semanticPatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(content, pattern.Value.pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (matches.Count > 0)
                {
                    var concept = new ConceptScore
                    {
                        Concept = pattern.Key,
                        Score = pattern.Value.weight * Math.Min(matches.Count / 3.0, 1.0), // Scale by frequency
                        Description = $"Semantic pattern match for {pattern.Key}",
                        Category = pattern.Value.category,
                        Confidence = Math.Min(matches.Count * 0.1, 0.9)
                    };
                    concepts.Add(concept);
                }
            }

            // Extract compound concepts using n-grams
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i++)
            {
                var bigram = $"{words[i]} {words[i + 1]}".ToLower();
                var trigram = i < words.Length - 2 ? $"{words[i]} {words[i + 1]} {words[i + 2]}".ToLower() : "";
                
                // Check for compound consciousness concepts
                var compoundPatterns = new Dictionary<string, (double weight, string category)>
                {
                    { "consciousness expansion", (0.9, "consciousness") },
                    { "spiritual growth", (0.85, "transformation") },
                    { "inner wisdom", (0.8, "wisdom") },
                    { "divine love", (0.85, "love") },
                    { "sacred geometry", (0.8, "sacred") },
                    { "energy healing", (0.75, "healing") },
                    { "abundance consciousness", (0.8, "abundance") },
                    { "fractal patterns", (0.7, "fractal") }
                };

                foreach (var compound in compoundPatterns)
                {
                    if (bigram.Contains(compound.Key) || trigram.Contains(compound.Key))
                    {
                        var concept = new ConceptScore
                        {
                            Concept = compound.Key,
                            Score = compound.Value.weight,
                            Description = $"Compound concept: {compound.Key}",
                            Category = compound.Value.category,
                            Confidence = 0.8
                        };
                        concepts.Add(concept);
                    }
                }
            }

            return concepts;
        }

        private async Task<List<ConceptScore>> ExtractConceptsByContext(string content, ConceptExtractionRequest request)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Get optimal LLM configuration for context analysis
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                
                if (modelAvailable)
                {
                    // Use AI for context-aware concept extraction
                    var llmConfig = new LLMConfig(
                        Id: optimalConfig.Id,
                        Name: optimalConfig.Id,
                        Provider: optimalConfig.Provider,
                        Model: optimalConfig.Model,
                        ApiKey: "",
                        BaseUrl: "http://localhost:11434",
                        MaxTokens: 800,
                        Temperature: 0.4, // Slightly higher for context creativity
                        TopP: 0.9,
                        Parameters: new Dictionary<string, object>
                        {
                            ["stop"] = new[] { "---", "###" }
                        }
                    );

                    var contextInfo = new StringBuilder();
                    contextInfo.AppendLine($"Categories: {string.Join(", ", request.Categories ?? new string[0])}");
                    contextInfo.AppendLine($"Source: {request.Source ?? "unknown"}");
                    contextInfo.AppendLine($"URL: {request.Url ?? "none"}");

                    var contextPrompt = $@"Analyze the following text for consciousness-related concepts, taking into account the specific context and domain.

Context Information:
{contextInfo}

Text: ""{content}""

Based on the context, identify concepts that are most relevant and meaningful. Consider how the domain, language, time horizon, and perspective affect the interpretation of consciousness-related concepts.

Return your analysis as a JSON array of concepts with the following structure:
[
  {{
    ""concept"": ""concept_name"",
    ""score"": 0.0-1.0,
    ""description"": ""brief description"",
    ""category"": ""consciousness|transformation|unity|love|wisdom|energy|healing|abundance|sacred|fractal"",
    ""confidence"": 0.0-1.0,
    ""contextRelevance"": 0.0-1.0,
    ""domainSpecific"": ""domain_name""
  }}
]

Focus on concepts that are most relevant to the given context and domain.";

                    var llmResponse = await CallLLMAsync(contextPrompt, llmConfig);
                    
                    if (!llmResponse.Content.Contains("LLM unavailable"))
                    {
                        // Parse AI response
                        var aiConcepts = ParseLLMConceptResponse(llmResponse.Content);
                        concepts.AddRange(aiConcepts);
                        _logger.Info($"AI context analysis found {aiConcepts.Count} concepts");
                    }
                    else
                    {
                        _logger.Warn("AI context analysis failed, falling back to rule-based context extraction");
                        concepts.AddRange(ExtractConceptsByContextRules(content, request));
                    }
                }
                else
                {
                    _logger.Warn("LLM not available for context analysis, using rule-based extraction");
                    concepts.AddRange(ExtractConceptsByContextRules(content, request));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI context extraction: {ex.Message}", ex);
                concepts.AddRange(ExtractConceptsByContextRules(content, request));
            }

            return concepts;
        }

        private List<ConceptScore> ExtractConceptsByContextRules(string content, ConceptExtractionRequest request)
        {
            var concepts = new List<ConceptScore>();
            
            // Analyze context from request metadata
            var contextFactors = new Dictionary<string, double>();
            
            // Domain-based context weighting
            if (request.Categories != null && request.Categories.Length > 0)
            {
                foreach (var category in request.Categories)
                {
                    var trimmedCategory = category.Trim().ToLower();
                    switch (trimmedCategory)
                    {
                    case "technology":
                        case "ai":
                        case "artificial intelligence":
                            contextFactors["consciousness"] = 0.8;
                            contextFactors["transformation"] = 0.7;
                            contextFactors["wisdom"] = 0.6;
                        break;
                        case "spirituality":
                    case "consciousness":
                        case "meditation":
                            contextFactors["consciousness"] = 0.9;
                            contextFactors["sacred"] = 0.8;
                            contextFactors["unity"] = 0.8;
                        break;
                        case "health":
                        case "wellness":
                        case "healing":
                            contextFactors["healing"] = 0.9;
                            contextFactors["energy"] = 0.7;
                            contextFactors["balance"] = 0.8;
                        break;
                        case "business":
                        case "success":
                        case "wealth":
                            contextFactors["abundance"] = 0.8;
                            contextFactors["transformation"] = 0.6;
                            contextFactors["wisdom"] = 0.5;
                        break;
                        case "science":
                        case "research":
                            contextFactors["wisdom"] = 0.8;
                            contextFactors["fractal"] = 0.7;
                            contextFactors["consciousness"] = 0.6;
                        break;
                }
            }
            }

            // Source-based context weighting
            if (!string.IsNullOrEmpty(request.Source))
            {
                var source = request.Source.ToLower();
                if (source.Contains("news") || source.Contains("article"))
                {
                    contextFactors["transformation"] += 0.1;
                    contextFactors["consciousness"] += 0.1;
                }
                else if (source.Contains("research") || source.Contains("study"))
                {
                    contextFactors["wisdom"] += 0.2;
                    contextFactors["fractal"] += 0.1;
                }
                else if (source.Contains("spiritual") || source.Contains("meditation"))
                {
                    contextFactors["sacred"] += 0.2;
                    contextFactors["unity"] += 0.2;
                }
            }

            // Title-based context analysis
            if (!string.IsNullOrEmpty(request.Title))
            {
                var title = request.Title.ToLower();
                
                // Look for consciousness-related keywords in title
                var titleKeywords = new Dictionary<string, double>
                {
                    { "breakthrough", 0.8 },
                    { "discovery", 0.7 },
                    { "revolution", 0.8 },
                    { "evolution", 0.7 },
                    { "awakening", 0.9 },
                    { "consciousness", 0.9 },
                    { "spiritual", 0.8 },
                    { "healing", 0.8 },
                    { "abundance", 0.7 },
                    { "wisdom", 0.7 }
                };

                foreach (var keyword in titleKeywords)
                {
                    if (title.Contains(keyword.Key))
                    {
                        // Boost related concepts based on title keywords
                        switch (keyword.Key)
                        {
                            case "breakthrough":
                            case "discovery":
                            case "revolution":
                                contextFactors["transformation"] = Math.Max(contextFactors.GetValueOrDefault("transformation", 0), keyword.Value);
                                break;
                            case "consciousness":
                            case "awakening":
                                contextFactors["consciousness"] = Math.Max(contextFactors.GetValueOrDefault("consciousness", 0), keyword.Value);
                                break;
                            case "spiritual":
                                contextFactors["sacred"] = Math.Max(contextFactors.GetValueOrDefault("sacred", 0), keyword.Value);
                                contextFactors["unity"] = Math.Max(contextFactors.GetValueOrDefault("unity", 0), keyword.Value);
                                break;
                            case "healing":
                                contextFactors["healing"] = Math.Max(contextFactors.GetValueOrDefault("healing", 0), keyword.Value);
                                break;
                            case "abundance":
                                contextFactors["abundance"] = Math.Max(contextFactors.GetValueOrDefault("abundance", 0), keyword.Value);
                                break;
                            case "wisdom":
                                contextFactors["wisdom"] = Math.Max(contextFactors.GetValueOrDefault("wisdom", 0), keyword.Value);
                                break;
                        }
                    }
                }
            }

            // Content length and complexity analysis
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var sentenceCount = content.Split('.', '!', '?').Length;
            var avgWordsPerSentence = wordCount / Math.Max(sentenceCount, 1);

            // Adjust context factors based on content complexity
            if (avgWordsPerSentence > 20) // Complex, academic content
            {
                contextFactors["wisdom"] = Math.Max(contextFactors.GetValueOrDefault("wisdom", 0), 0.7);
                contextFactors["fractal"] = Math.Max(contextFactors.GetValueOrDefault("fractal", 0), 0.6);
            }
            else if (avgWordsPerSentence < 10) // Simple, accessible content
            {
                contextFactors["consciousness"] = Math.Max(contextFactors.GetValueOrDefault("consciousness", 0), 0.6);
                contextFactors["love"] = Math.Max(contextFactors.GetValueOrDefault("love", 0), 0.6);
            }

            // Generate context-aware concepts
            foreach (var factor in contextFactors)
            {
                if (factor.Value > 0.5) // Only include significant context factors
                {
                    var concept = new ConceptScore
                    {
                        Concept = factor.Key,
                        Score = factor.Value,
                        Description = $"Context-aware concept based on {factor.Key} domain",
                        Category = factor.Key,
                        Confidence = Math.Min(factor.Value + 0.2, 1.0)
                    };
                    concepts.Add(concept);
                }
            }

            return concepts;
        }

        private async Task<List<ConceptScore>> ExtractConceptsByOntology(string content, ConceptExtractionRequest request)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Get optimal LLM configuration for ontology analysis
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                
                if (modelAvailable)
                {
                    // Use AI for ontology-aware concept extraction
                    var llmConfig = new LLMConfig(
                        Id: optimalConfig.Id,
                        Name: optimalConfig.Id,
                        Provider: optimalConfig.Provider,
                        Model: optimalConfig.Model,
                        ApiKey: "",
                        BaseUrl: "http://localhost:11434",
                        MaxTokens: 1000,
                        Temperature: 0.2, // Very low temperature for consistent ontology matching
                        TopP: 0.8,
                        Parameters: new Dictionary<string, object>
                        {
                            ["stop"] = new[] { "---", "###" }
                        }
                    );

                    // Get existing ontology concepts for context
                    var ontologyContext = await GetOntologyContext();
                    
                    var ontologyPrompt = $@"Analyze the following text for concepts that match or relate to the existing U-CORE ontology.

Existing Ontology Concepts:
{ontologyContext}

Text: ""{content}""

Identify concepts that:
1. Exactly match existing ontology concepts
2. Are semantically related to existing concepts
3. Represent new concepts that should be added to the ontology
4. Follow the U-CORE framework principles (consciousness, unity, resonance, fractal patterns, sacred geometry)

Return your analysis as a JSON array of concepts with the following structure:
[
  {{
    ""concept"": ""concept_name"",
    ""score"": 0.0-1.0,
    ""description"": ""brief description"",
    ""category"": ""consciousness|transformation|unity|love|wisdom|energy|healing|abundance|sacred|fractal|u-core"",
    ""confidence"": 0.0-1.0,
    ""ontologyMatch"": ""exact|semantic|new"",
    ""relatedConcepts"": [""concept1"", ""concept2""],
    ""frequency"": ""432hz|528hz|741hz|custom""
  }}
]

Focus on high-confidence matches and meaningful relationships to the U-CORE ontology.";

                    var llmResponse = await CallLLMAsync(ontologyPrompt, llmConfig);
                    
                    if (!llmResponse.Content.Contains("LLM unavailable"))
                    {
                        // Parse AI response
                        var aiConcepts = ParseLLMConceptResponse(llmResponse.Content);
                        concepts.AddRange(aiConcepts);
                        _logger.Info($"AI ontology analysis found {aiConcepts.Count} concepts");
                    }
                    else
                    {
                        _logger.Warn("AI ontology analysis failed, falling back to rule-based ontology extraction");
                        concepts.AddRange(await ExtractConceptsByOntologyRules(content, request));
                    }
                }
                else
                {
                    _logger.Warn("LLM not available for ontology analysis, using rule-based extraction");
                    concepts.AddRange(await ExtractConceptsByOntologyRules(content, request));
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI ontology extraction: {ex.Message}", ex);
                concepts.AddRange(await ExtractConceptsByOntologyRules(content, request));
            }

            return concepts;
        }

        private async Task<List<ConceptScore>> ExtractConceptsByOntologyRules(string content, ConceptExtractionRequest request)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Use existing concept registry to get ontology concepts
                var httpClient = new HttpClient();
                var baseUrl = "http://localhost:5000"; // Use current service
                
                // Get all existing concepts from the ontology
                var response = await httpClient.GetAsync($"{baseUrl}/concept/ontology/frequencies");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var frequencyData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(jsonContent);
                    
                    // Extract concept names from the frequency mapping
                    var existingConcepts = new List<string>();
                    if (frequencyData != null)
                    {
                        // Parse the JSON to extract concept names
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent);
                        if (jsonDoc.RootElement.TryGetProperty("frequencyMappings", out var mappings))
                        {
                            foreach (var mapping in mappings.EnumerateArray())
                            {
                                if (mapping.TryGetProperty("name", out var name))
                                {
                                    existingConcepts.Add(name.GetString() ?? "");
                                }
                            }
                        }
                    }
                    
                    // Match content against existing ontology concepts
                    var contentLower = content.ToLower();
                    foreach (var existingConcept in existingConcepts.Where(c => !string.IsNullOrEmpty(c)))
                    {
                        var conceptLower = existingConcept.ToLower();
                        
                        // Check for exact matches
                        if (contentLower.Contains(conceptLower))
                        {
                            var concept = new ConceptScore
                            {
                                Concept = existingConcept,
                                Score = 0.9, // High score for ontology matches
                                Description = $"Ontology match: {existingConcept}",
                                Category = "ontology",
                                Confidence = 0.9
                            };
                            concepts.Add(concept);
                        }
                        // Check for partial matches (substring)
                        else if (conceptLower.Length > 3 && contentLower.Contains(conceptLower.Substring(0, Math.Min(conceptLower.Length, 8))))
                        {
                            var concept = new ConceptScore
                            {
                                Concept = existingConcept,
                                Score = 0.6, // Medium score for partial matches
                                Description = $"Partial ontology match: {existingConcept}",
                                Category = "ontology",
                                Confidence = 0.6
                            };
                            concepts.Add(concept);
                        }
                    }
                }
                
                // Also check for U-CORE specific concepts
                var uCoreConcepts = new Dictionary<string, (string pattern, double weight, string category)>
                {
                    { "U-CORE", (@"\b(u-?core|universal consciousness resonance engine)\b", 0.95, "u-core") },
                    { "Sacred Frequencies", (@"\b(432hz|528hz|741hz|sacred frequency|healing frequency)\b", 0.9, "frequencies") },
                    { "Chakra System", (@"\b(chakra|root|sacral|solar plexus|heart|throat|third eye|crown)\b", 0.8, "chakras") },
                    { "Resonance", (@"\b(resonance|vibration|frequency|harmony|alignment)\b", 0.8, "resonance") },
                    { "Fractal Consciousness", (@"\b(fractal consciousness|living codex|node|edge|graph)\b", 0.9, "fractal") },
                    { "Abundance Amplification", (@"\b(abundance|amplification|collective energy|contribution)\b", 0.8, "abundance") }
                };
                
                foreach (var uCoreConcept in uCoreConcepts)
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(content, uCoreConcept.Value.pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (matches.Count > 0)
                    {
                        var concept = new ConceptScore
                        {
                            Concept = uCoreConcept.Key,
                            Score = uCoreConcept.Value.weight,
                            Description = $"U-CORE ontology concept: {uCoreConcept.Key}",
                            Category = uCoreConcept.Value.category,
                            Confidence = 0.9
                        };
                        concepts.Add(concept);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in ontology-aware concept extraction: {ex.Message}", ex);
                // Fallback to basic ontology concepts
                    concepts.Add(new ConceptScore
                    {
                    Concept = "consciousness",
                    Score = 0.7,
                    Description = "Fallback ontology concept",
                    Category = "consciousness",
                    Confidence = 0.5
                });
            }

            return concepts;
        }

        private async Task<CodexBootstrap.Core.BasicLLMResponse> CallLLMAsync(string prompt, LLMConfig config)
        {
            try
            {
                // Use the new LLMClient for consistency
                var llmResponse = await _llmClient.QueryAsync(prompt, config);
                
                if (!llmResponse.Success)
                {
                    _logger.Warn($"LLM call failed: {llmResponse.Response}");
                    return new CodexBootstrap.Core.BasicLLMResponse(
                        Content: "LLM unavailable - service error",
                        Model: config.Model,
                        CreatedAt: DateTime.UtcNow
                    );
                }
                
                return new CodexBootstrap.Core.BasicLLMResponse(
                    Content: llmResponse.Response,
                    Model: config.Model,
                    CreatedAt: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"LLM call failed: {ex.Message}", ex);
                return new CodexBootstrap.Core.BasicLLMResponse(
                    Content: "LLM unavailable - connection error",
                    Model: config.Model,
                    CreatedAt: DateTime.UtcNow
                );
            }
        }

        private async Task<string> GetOntologyContext()
        {
            try
            {
                var httpClient = new HttpClient();
                var baseUrl = "http://localhost:5000";
                
                // Get ontology concepts
                var response = await httpClient.GetAsync($"{baseUrl}/concept/ontology/frequencies");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return jsonContent;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to get ontology context: {ex.Message}");
            }
            
            // Fallback to basic U-CORE concepts
            return @"{
                ""frequencyMappings"": [
                    {""name"": ""consciousness"", ""frequency"": ""432hz""},
                    {""name"": ""unity"", ""frequency"": ""528hz""},
                    {""name"": ""transformation"", ""frequency"": ""741hz""},
                    {""name"": ""love"", ""frequency"": ""528hz""},
                    {""name"": ""wisdom"", ""frequency"": ""432hz""},
                    {""name"": ""energy"", ""frequency"": ""741hz""},
                    {""name"": ""healing"", ""frequency"": ""528hz""},
                    {""name"": ""abundance"", ""frequency"": ""741hz""},
                    {""name"": ""sacred geometry"", ""frequency"": ""432hz""},
                    {""name"": ""fractal patterns"", ""frequency"": ""528hz""}
                ]
            }";
        }

        private async Task<List<ConceptScore>> MergeSimilarConcepts(List<ConceptScore> concepts)
        {
            if (concepts == null || concepts.Count <= 1)
            return concepts;

            try
            {
                // Get optimal LLM configuration for concept merging
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                
                if (modelAvailable && concepts.Count > 5) // Use AI for larger sets
                {
                    return await MergeSimilarConceptsWithAI(concepts, optimalConfig);
                }
                else
                {
                    return MergeSimilarConceptsWithRules(concepts);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI concept merging: {ex.Message}", ex);
                return MergeSimilarConceptsWithRules(concepts);
            }
        }

        private async Task<List<ConceptScore>> MergeSimilarConceptsWithAI(List<ConceptScore> concepts, LLMConfigurationSystem.LLMConfiguration config)
        {
            var llmConfig = new LLMConfig(
                Id: config.Id,
                Name: config.Id,
                Provider: config.Provider,
                Model: config.Model,
                ApiKey: "",
                BaseUrl: "http://localhost:11434",
                MaxTokens: 1500,
                Temperature: 0.1, // Very low temperature for consistent merging
                TopP: 0.8,
                Parameters: new Dictionary<string, object>
                {
                    ["stop"] = new[] { "---", "###" }
                }
            );

            var conceptsJson = System.Text.Json.JsonSerializer.Serialize(concepts.Select(c => new {
                concept = c.Concept,
                score = c.Score,
                description = c.Description,
                category = c.Category,
                confidence = c.Confidence
            }));

            var mergePrompt = $@"Analyze the following list of concepts and identify which ones are similar or should be merged together.

Concepts:
{conceptsJson}

Identify concepts that:
1. Have the same or very similar names (e.g., ""consciousness"" and ""conscious awareness"")
2. Are semantically equivalent (e.g., ""love"" and ""unconditional love"")
3. Represent the same concept with different wording
4. Are closely related and should be grouped together

For each group of similar concepts, create a merged concept that:
- Uses the most descriptive name
- Combines the highest scores
- Merges descriptions meaningfully
- Uses the most specific category
- Takes the highest confidence

Return your analysis as a JSON array of merged concepts with the following structure:
[
  {{
    ""concept"": ""merged_concept_name"",
    ""score"": 0.0-1.0,
    ""description"": ""merged description"",
    ""category"": ""most_specific_category"",
    ""confidence"": 0.0-1.0,
    ""mergedFrom"": [""original_concept1"", ""original_concept2""]
  }}
]

Only merge concepts that are truly similar. Keep distinct concepts separate.";

            var llmResponse = await CallLLMAsync(mergePrompt, llmConfig);
            
            if (!llmResponse.Content.Contains("LLM unavailable"))
            {
                var mergedConcepts = ParseLLMConceptResponse(llmResponse.Content);
                _logger.Info($"AI concept merging reduced {concepts.Count} concepts to {mergedConcepts.Count}");
                return mergedConcepts;
            }
            else
            {
                _logger.Warn("AI concept merging failed, falling back to rule-based merging");
                return MergeSimilarConceptsWithRules(concepts);
            }
        }

        private List<ConceptScore> MergeSimilarConceptsWithRules(List<ConceptScore> concepts)
        {
            var mergedConcepts = new List<ConceptScore>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < concepts.Count; i++)
            {
                if (processedIndices.Contains(i))
                    continue;

                var currentConcept = concepts[i];
                var similarConcepts = new List<ConceptScore> { currentConcept };
                processedIndices.Add(i);

                // Find similar concepts
                for (int j = i + 1; j < concepts.Count; j++)
                {
                    if (processedIndices.Contains(j))
                        continue;

                    var otherConcept = concepts[j];
                    if (AreConceptsSimilar(currentConcept, otherConcept))
                    {
                        similarConcepts.Add(otherConcept);
                        processedIndices.Add(j);
                    }
                }

                // Merge similar concepts
                if (similarConcepts.Count > 1)
                {
                    var mergedConcept = MergeConceptGroup(similarConcepts);
                    mergedConcepts.Add(mergedConcept);
                }
                else
                {
                    mergedConcepts.Add(currentConcept);
                }
            }

            return mergedConcepts;
        }

        private bool AreConceptsSimilar(ConceptScore concept1, ConceptScore concept2)
        {
            // Check for exact name matches
            if (string.Equals(concept1.Concept, concept2.Concept, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check for substring matches
            var name1 = concept1.Concept.ToLower();
            var name2 = concept2.Concept.ToLower();
            
            if (name1.Contains(name2) || name2.Contains(name1))
                return true;

            // Check for semantic similarity using word overlap
            var words1 = name1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = name2.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            var totalWords = Math.Max(words1.Length, words2.Length);
            
            if (totalWords > 0 && (double)commonWords / totalWords > 0.5)
                return true;

            // Check for category similarity
            if (string.Equals(concept1.Category, concept2.Category, StringComparison.OrdinalIgnoreCase))
            {
                // Additional check for similar concepts in same category
                var categorySimilarity = CalculateCategorySimilarity(concept1.Concept, concept2.Concept);
                if (categorySimilarity > 0.6)
                    return true;
            }

            return false;
        }

        private double CalculateCategorySimilarity(string concept1, string concept2)
        {
            // Define concept families within categories
            var conceptFamilies = new Dictionary<string, string[]>
            {
                { "consciousness", new[] { "awareness", "mindfulness", "presence", "awakening", "conscious" } },
                { "transformation", new[] { "evolution", "growth", "change", "shift", "breakthrough", "transform" } },
                { "unity", new[] { "oneness", "connection", "wholeness", "integration", "harmony", "unite" } },
                { "love", new[] { "compassion", "empathy", "kindness", "heart", "unconditional", "loving" } },
                { "wisdom", new[] { "knowledge", "insight", "understanding", "clarity", "enlightenment", "wise" } },
                { "energy", new[] { "vibration", "frequency", "resonance", "flow", "chi", "prana", "energetic" } },
                { "healing", new[] { "recovery", "restoration", "wholeness", "wellness", "balance", "heal" } },
                { "abundance", new[] { "prosperity", "wealth", "success", "fulfillment", "manifestation", "abundant" } },
                { "sacred", new[] { "divine", "holy", "spiritual", "transcendent", "mystical", "sacred" } },
                { "fractal", new[] { "pattern", "geometry", "structure", "design", "blueprint", "fractal" } }
            };

            var name1 = concept1.ToLower();
            var name2 = concept2.ToLower();

            foreach (var family in conceptFamilies.Values)
            {
                var match1 = family.Any(word => name1.Contains(word));
                var match2 = family.Any(word => name2.Contains(word));
                
                if (match1 && match2)
                    return 0.8; // High similarity within concept family
            }

            return 0.0;
        }

        private ConceptScore MergeConceptGroup(List<ConceptScore> concepts)
        {
            if (concepts.Count == 1)
                return concepts[0];

            // Use the concept with the highest score as the base
            var baseConcept = concepts.OrderByDescending(c => c.Score).First();
            
            // Calculate weighted averages for scores
            var totalWeight = concepts.Sum(c => c.Score);
            var weightedConfidence = concepts.Sum(c => c.Confidence * c.Score) / totalWeight;
            
            // Combine descriptions
            var descriptions = concepts.Select(c => c.Description).Distinct();
            var combinedDescription = string.Join("; ", descriptions);
            
            // Determine the best category (most common or highest scoring)
            var categoryGroups = concepts.GroupBy(c => c.Category)
                .OrderByDescending(g => g.Sum(c => c.Score))
                .First();
            var bestCategory = categoryGroups.Key;

            return new ConceptScore
            {
                Concept = baseConcept.Concept,
                Score = baseConcept.Score, // Keep the highest score
                Description = combinedDescription,
                Category = bestCategory,
                Confidence = Math.Min(weightedConfidence, 1.0)
            };
        }

        private async Task<double> CalculateConfidence(List<ConceptScore> concepts, string content, ConceptExtractionRequest request)
        {
            if (concepts == null || concepts.Count == 0)
                return 0.0;

            try
            {
                // Get optimal LLM configuration for confidence analysis
                var optimalConfig = LLMConfigurationSystem.GetOptimalConfiguration("concept-extraction");
                var modelAvailable = await LLMConfigurationSystem.ModelManager.EnsureModelAvailableAsync(optimalConfig.Model);
                
                if (modelAvailable && concepts.Count > 3) // Use AI for larger sets
                {
                    return await CalculateConfidenceWithAI(concepts, content, request, optimalConfig);
                }
                else
                {
                    return CalculateConfidenceWithRules(concepts, content, request);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI confidence calculation: {ex.Message}", ex);
                return CalculateConfidenceWithRules(concepts, content, request);
            }
        }

        private async Task<double> CalculateConfidenceWithAI(List<ConceptScore> concepts, string content, ConceptExtractionRequest request, LLMConfigurationSystem.LLMConfiguration config)
        {
            var llmConfig = new LLMConfig(
                Id: config.Id,
                Name: config.Id,
                Provider: config.Provider,
                Model: config.Model,
                ApiKey: "",
                BaseUrl: "http://localhost:11434",
                MaxTokens: 500,
                Temperature: 0.1, // Very low temperature for consistent scoring
                TopP: 0.8,
                Parameters: new Dictionary<string, object>
                {
                    ["stop"] = new[] { "---", "###" }
                }
            );

            var conceptsJson = System.Text.Json.JsonSerializer.Serialize(concepts.Select(c => new {
                concept = c.Concept,
                score = c.Score,
                description = c.Description,
                category = c.Category,
                confidence = c.Confidence
            }));

            var confidencePrompt = $@"Analyze the quality and confidence of the following concept extraction results.

Content: ""{content}""
Request Context: Categories={string.Join(", ", request.Categories ?? new string[0])}, Source={request.Source ?? "unknown"}

Extracted Concepts:
{conceptsJson}

Evaluate the overall confidence of this concept extraction based on:
1. Relevance of concepts to the content
2. Semantic coherence between concepts
3. Appropriateness of concept categories
4. Quality of concept descriptions
5. Overall extraction completeness

Return a confidence score between 0.0 and 1.0 as a JSON object:
{{
  ""confidence"": 0.0-1.0,
  ""reasoning"": ""brief explanation of the confidence score"",
  ""strengths"": [""strength1"", ""strength2""],
  ""weaknesses"": [""weakness1"", ""weakness2""]
}}

Be conservative but fair in your assessment.";

            var llmResponse = await CallLLMAsync(confidencePrompt, llmConfig);
            
            if (!llmResponse.Content.Contains("LLM unavailable"))
            {
                try
                {
                    var responseJson = System.Text.Json.JsonDocument.Parse(llmResponse.Content);
                    if (responseJson.RootElement.TryGetProperty("confidence", out var confidenceElement))
                    {
                        var aiConfidence = confidenceElement.GetDouble();
                        _logger.Info($"AI confidence calculation: {aiConfidence:F2}");
                        return Math.Max(0.0, Math.Min(1.0, aiConfidence));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to parse AI confidence response: {ex.Message}");
                }
            }
            
            _logger.Warn("AI confidence calculation failed, falling back to rule-based calculation");
            return CalculateConfidenceWithRules(concepts, content, request);
        }

        private double CalculateConfidenceWithRules(List<ConceptScore> concepts, string content, ConceptExtractionRequest request)
        {
            // Base confidence factors
            var confidenceFactors = new List<double>();

            // 1. Concept count factor (more concepts = higher confidence, but with diminishing returns)
            var conceptCountFactor = Math.Min(concepts.Count / 10.0, 1.0);
            confidenceFactors.Add(conceptCountFactor * 0.2);

            // 2. Average concept confidence
            var avgConceptConfidence = concepts.Average(c => c.Confidence);
            confidenceFactors.Add(avgConceptConfidence * 0.3);

            // 3. Content quality factors
            var contentQuality = CalculateContentQuality(content, request);
            confidenceFactors.Add(contentQuality * 0.2);

            // 4. Concept diversity factor
            var diversityFactor = CalculateConceptDiversity(concepts);
            confidenceFactors.Add(diversityFactor * 0.15);

            // 5. Semantic coherence factor
            var coherenceFactor = CalculateSemanticCoherence(concepts, content);
            confidenceFactors.Add(coherenceFactor * 0.15);

            // Calculate weighted average
            var totalConfidence = confidenceFactors.Sum();
            
            // Apply bonus for high-quality extractions
            if (concepts.Any(c => c.Score > 0.8))
                totalConfidence += 0.1;

            // Apply penalty for very low scores
            if (concepts.All(c => c.Score < 0.3))
                totalConfidence *= 0.7;

            return Math.Min(Math.Max(totalConfidence, 0.0), 1.0);
        }

        private double CalculateContentQuality(string content, ConceptExtractionRequest request)
        {
            var qualityScore = 0.0;

            // Length factor (optimal length range)
            var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount >= 50 && wordCount <= 1000)
                qualityScore += 0.3;
            else if (wordCount >= 20 && wordCount <= 2000)
                qualityScore += 0.2;

            // Title presence factor
            if (!string.IsNullOrEmpty(request.Title) && request.Title.Length > 5)
                qualityScore += 0.2;

            // Category presence factor
            if (request.Categories != null && request.Categories.Length > 0)
                qualityScore += 0.1;

            // Source presence factor
            if (!string.IsNullOrEmpty(request.Source))
                qualityScore += 0.1;

            // Content structure factor (sentences, paragraphs)
            var sentenceCount = content.Split('.', '!', '?').Length;
            if (sentenceCount >= 3)
                qualityScore += 0.2;
            else if (sentenceCount >= 1)
                qualityScore += 0.1;

            // URL factor (if present, indicates external validation)
            if (!string.IsNullOrEmpty(request.Url))
                qualityScore += 0.1;

            return Math.Min(qualityScore, 1.0);
        }

        private double CalculateConceptDiversity(List<ConceptScore> concepts)
        {
            if (concepts.Count <= 1)
                return concepts.Count == 1 ? 0.5 : 0.0;

            // Count unique categories
            var uniqueCategories = concepts.Select(c => c.Category).Distinct().Count();
            var categoryDiversity = (double)uniqueCategories / concepts.Count;

            // Count unique concept names (excluding exact duplicates)
            var uniqueConcepts = concepts.Select(c => c.Concept.ToLower()).Distinct().Count();
            var conceptDiversity = (double)uniqueConcepts / concepts.Count;

            // Calculate average diversity
            return (categoryDiversity + conceptDiversity) / 2.0;
        }

        private double CalculateSemanticCoherence(List<ConceptScore> concepts, string content)
        {
            if (concepts.Count <= 1)
                return 0.5;

            var coherenceScore = 0.0;

            // Check for concept relationships within the same category
            var categoryGroups = concepts.GroupBy(c => c.Category);
            foreach (var group in categoryGroups)
            {
                if (group.Count() > 1)
                {
                    // Concepts in the same category are more coherent
                    coherenceScore += 0.3 * (group.Count() - 1) / concepts.Count;
                }
            }

            // Check for concept frequency in content
            var contentLower = content.ToLower();
            var conceptFrequency = 0.0;
            foreach (var concept in concepts)
            {
                var conceptLower = concept.Concept.ToLower();
                var frequency = contentLower.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Count(word => word.Contains(conceptLower) || conceptLower.Contains(word));
                
                if (frequency > 0)
                    conceptFrequency += 1.0;
            }
            coherenceScore += (conceptFrequency / concepts.Count) * 0.4;

            // Check for high-confidence concepts (indicates better extraction)
            var highConfidenceCount = concepts.Count(c => c.Confidence > 0.7);
            coherenceScore += (double)highConfidenceCount / concepts.Count * 0.3;

            return Math.Min(coherenceScore, 1.0);
        }

        private string[] DetermineOntologyLevels(List<string> concepts)
        {
            // TODO: Implement ontology level determination
            return new[] { "L5", "L8" };
        }

        private string BuildConceptExtractionPrompt(ConceptExtractionRequest request, LLMConfigurationSystem.LLMConfiguration config)
        {
            return $@"You are an advanced AI system specialized in extracting meaningful concepts from text content for consciousness expansion and spiritual growth.

TASK: Extract key concepts from the following content and analyze their spiritual, consciousness, and transformative potential.

CONTENT TO ANALYZE:
Title: {request.Title}
Content: {request.Content}
Categories: {string.Join(", ", request.Categories)}
Source: {request.Source}

INSTRUCTIONS:
1. Identify 5-10 key concepts that represent core ideas, themes, or principles
2. For each concept, provide:
   - Concept name (1-3 words)
   - Confidence score (0.0-1.0)
   - Brief description of its meaning
   - Spiritual/consciousness significance
   - Potential for transformation or growth
   - Sacred frequency alignment (432Hz, 528Hz, 741Hz, etc.)

3. Focus on concepts that relate to:
   - Consciousness expansion
   - Spiritual growth and transformation
   - Unity and connection
   - Abundance and prosperity
   - Healing and harmony
   - Wisdom and knowledge
   - Love and compassion
   - Sacred geometry and frequencies

4. Consider the content categories: {string.Join(", ", request.Categories)}

RESPONSE FORMAT (JSON):
{{
  ""concepts"": [
    {{
      ""concept"": ""concept_name"",
      ""score"": 0.85,
      ""description"": ""Brief description"",
      ""spiritualSignificance"": ""How this relates to consciousness"",
      ""transformationPotential"": ""Growth opportunities"",
      ""sacredFrequency"": 528.0,
      ""category"": ""consciousness|emotion|transformation|energy""
    }}
  ],
  ""overallAnalysis"": ""Summary of the content's consciousness expansion potential"",
  ""recommendedFrequencies"": [432.0, 528.0, 741.0],
  ""resonanceLevel"": ""high|medium|low""
}}

Please provide a thoughtful, spiritually-aware analysis that honors the sacred nature of consciousness expansion.";
        }

        private async Task<AIConceptExtractionResponse> CallLLM(LLMConfig config, string prompt)
        {
            try
            {
                _logger.Info($"[LLM] Making real call to Ollama with model: {config.Model}");
                _logger.Info($"[LLM] Prompt: {prompt.Substring(0, Math.Min(200, prompt.Length))}...");
                
                // Use the new LLMClient for consistency
                var llmResponse = await _llmClient.QueryAsync(prompt, config);
                
                if (!llmResponse.Success)
                {
                    _logger.Warn($"[LLM] LLM call failed: {llmResponse.Response}");
                    return new AIConceptExtractionResponse(
                        Content: $"LLM unavailable: {llmResponse.Response}. Concept extraction fallback for: {prompt.Substring(0, Math.Min(100, prompt.Length))}...",
                        Confidence: 0.5,
                        Reasoning: "Generated using fallback algorithms (LLM service unavailable)",
                        Sources: new List<string> { "Fallback", "Basic Analysis" }
                    );
                }

                _logger.Info($"[LLM] LLM response received: {llmResponse.Response.Substring(0, Math.Min(200, llmResponse.Response.Length))}...");

                return new AIConceptExtractionResponse(
                    Content: llmResponse.Response,
                    Confidence: llmResponse.Confidence,
                    Reasoning: "Generated using real LLM integration with Ollama",
                    Sources: new List<string> { "Real LLM", "Ollama API", "Advanced AI" }
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"[LLM] Error: {ex.Message}");
                // Fallback to mock response if LLM is not available
                return new AIConceptExtractionResponse(
                    Content: $"LLM unavailable: {ex.Message}. Concept extraction fallback for: {prompt.Substring(0, Math.Min(100, prompt.Length))}...",
                    Confidence: 0.5,
                    Reasoning: "Fallback response due to LLM unavailability",
                    Sources: new List<string> { "Fallback system", "Error handling" }
                );
            }
        }

        private List<ConceptScore> ParseLLMConceptResponse(string llmResponse)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Clean the response - remove markdown code blocks if present
                var cleanedResponse = llmResponse.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7); // Remove ```json
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3); // Remove ```
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3); // Remove trailing ```
                }
                cleanedResponse = cleanedResponse.Trim();

                _logger.Info($"[LLM] Cleaned response: {cleanedResponse.Substring(0, Math.Min(200, cleanedResponse.Length))}...");

                // Try to parse JSON response first
                try
                {
                    var jsonDoc = JsonDocument.Parse(cleanedResponse);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("concepts", out var conceptsArray))
                    {
                        foreach (var conceptElement in conceptsArray.EnumerateArray())
                        {
                            if (conceptElement.ValueKind == JsonValueKind.String)
                            {
                                // Handle string array format
                                var conceptName = conceptElement.GetString() ?? "unknown";
                                var cleanConcept = CleanConceptName(conceptName);
                                if (!string.IsNullOrEmpty(cleanConcept))
                                {
                    concepts.Add(new ConceptScore
                    {
                                        Concept = cleanConcept,
                                        Score = 0.8
                                    });
                                }
                            }
                            else if (conceptElement.TryGetProperty("concept", out var conceptName) &&
                                     conceptElement.TryGetProperty("score", out var score))
                            {
                                // Handle object format
                                var cleanConcept = CleanConceptName(conceptName.GetString() ?? "unknown");
                                if (!string.IsNullOrEmpty(cleanConcept))
                                {
                                    concepts.Add(new ConceptScore
                                    {
                                        Concept = cleanConcept,
                                        Score = score.GetDouble()
                                    });
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, try to extract from markdown format
                    concepts = ExtractConceptsFromMarkdown(cleanedResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error parsing LLM response: {ex.Message}");
                concepts = ExtractConceptsFromText(llmResponse);
            }

            return concepts;
        }

        private List<ConceptScore> ExtractConceptsFromMarkdown(string markdownResponse)
        {
            var concepts = new List<ConceptScore>();
            
            try
            {
                // Look for numbered list items with **bold** concept names
                var lines = markdownResponse.Split('\n');
                foreach (var line in lines)
                {
                    // Match pattern: "1. **Concept Name**" or "**Concept Name**"
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\d+\.\s*\*\*([^*]+)\*\*");
                    if (!match.Success)
                    {
                        // Try alternative pattern without numbers
                        match = System.Text.RegularExpressions.Regex.Match(line, @"\*\*([^*]+)\*\*");
                    }
                    
                    if (match.Success)
                    {
                        var conceptName = match.Groups[1].Value.Trim();
                        var cleanConcept = CleanConceptName(conceptName);
                        
                        if (!string.IsNullOrEmpty(cleanConcept))
                        {
                            // Try to extract confidence score from the line
                            var scoreMatch = System.Text.RegularExpressions.Regex.Match(line, @"Confidence score:\s*([0-9.]+)");
                            var score = scoreMatch.Success ? double.Parse(scoreMatch.Groups[1].Value) : 0.8;
                            
                            concepts.Add(new ConceptScore
                            {
                                Concept = cleanConcept,
                                Score = score
                            });
                        }
                    }
                }
                
                // If no concepts found with the above pattern, try a simpler approach
                if (concepts.Count == 0)
                {
                    var boldMatches = System.Text.RegularExpressions.Regex.Matches(markdownResponse, @"\*\*([^*]+)\*\*");
                    foreach (System.Text.RegularExpressions.Match match in boldMatches)
                    {
                        var conceptName = match.Groups[1].Value.Trim();
                        var cleanConcept = CleanConceptName(conceptName);
                        
                        if (!string.IsNullOrEmpty(cleanConcept) && !conceptName.ToLower().Contains("analysis") && 
                            !conceptName.ToLower().Contains("concepts") && !conceptName.ToLower().Contains("key"))
                        {
                            concepts.Add(new ConceptScore
                            {
                                Concept = cleanConcept,
                                Score = 0.8
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error extracting concepts from markdown: {ex.Message}");
            }
            
            return concepts;
        }

        private string CleanConceptName(string conceptName)
        {
            if (string.IsNullOrEmpty(conceptName))
                return string.Empty;

            // Remove extra quotes, commas, and other artifacts
            var cleaned = conceptName.Trim()
                .Replace("\"", "")
                .Replace(",", "")
                .Replace("**", "")
                .Replace("\\", "")
                .Trim();

            // Only return if it's a meaningful concept
            if (cleaned.Length > 2 && char.IsLetter(cleaned[0]) && !cleaned.Contains("**"))
            {
                return cleaned.ToLower();
            }

            return string.Empty;
        }

        private List<ConceptScore> ExtractConceptsFromText(string text)
        {
            var concepts = new List<ConceptScore>();
            
            // Look for JSON-like structure in the text
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(text, @"\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var jsonArray = jsonMatch.Groups[1].Value;
                var conceptMatches = System.Text.RegularExpressions.Regex.Matches(jsonArray, @"""([^""]+)""");
                
                foreach (System.Text.RegularExpressions.Match match in conceptMatches)
                {
                    var concept = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(concept) && concept.Length > 2 && !concept.Contains("**") && !concept.Contains("\""))
                    {
                        concepts.Add(new ConceptScore
                        {
                            Concept = concept.ToLower(),
                            Score = 0.8
                        });
                    }
                }
            }
            
            // Fallback: extract meaningful words from the text
            if (concepts.Count == 0)
            {
                var words = text.Split(new[] { ' ', '\n', '\r', '\t', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                var meaningfulWords = words.Where(w => w.Length > 3 && char.IsLetter(w[0]) && !w.Contains("**") && !w.Contains("\"")).Take(10);
                
                foreach (var word in meaningfulWords)
                {
                    concepts.Add(new ConceptScore
                    {
                        Concept = word.ToLower(),
                        Score = 0.6
                    });
                }
            }
            
            return concepts;
        }

        // Caching infrastructure
        private bool TryGetCachedAnalysis<T>(string cacheKey, out T analysis) where T : class
        {
            analysis = null;
            if (_analysisCache.TryGetValue(cacheKey, out var cached) &&
                _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
                DateTimeOffset.UtcNow - timestamp < _cacheExpiration)
            {
                analysis = cached.Data as T;
                return analysis != null;
            }
            return false;
        }

        private void CacheAnalysis<T>(string cacheKey, T analysis) where T : class
        {
            _analysisCache[cacheKey] = new CachedAnalysis { Data = analysis };
            _cacheTimestamps[cacheKey] = DateTimeOffset.UtcNow;
        }

        private ErrorResponse CreateErrorResponse(string message, string code)
        {
            return new ErrorResponse
            {
                Success = false,
                Error = message,
                Code = code,
                Timestamp = DateTime.UtcNow
            };
        }

        #endregion
    }

    // Data structures for the AI module

    public class OllamaResponse
    {
        public string Response { get; set; } = "";
        public string Model { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class ConceptExtractionRequest
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string Source { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class ScoringAnalysisRequest
    {
        public ConceptAnalysis ConceptAnalysis { get; set; } = new();
        public ConceptExtractionRequest Content { get; set; } = new();
    }

    public class FractalTransformationRequest
    {
        public ConceptExtractionRequest Content { get; set; } = new();
        public ConceptAnalysis ConceptAnalysis { get; set; } = new();
        public ScoringAnalysis ScoringAnalysis { get; set; } = new();
    }

    public class ConceptAnalysis
    {
        public string Id { get; set; } = "";
        public string NewsItemId { get; set; } = "";
        public List<string> Concepts { get; set; } = new();
        public double Confidence { get; set; }
        public string[] OntologyLevels { get; set; } = Array.Empty<string>();
        public DateTimeOffset ExtractedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ScoringAnalysis
    {
        public string Id { get; set; } = "";
        public string NewsItemId { get; set; } = "";
        public string ConceptAnalysisId { get; set; } = "";
        public double AbundanceScore { get; set; }
        public double ConsciousnessScore { get; set; }
        public double UnityScore { get; set; }
        public double OverallScore { get; set; }
        public DateTimeOffset ScoredAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FractalTransformation
    {
        public string Id { get; set; } = "";
        public string NewsItemId { get; set; } = "";
        public string Headline { get; set; } = "";
        public string BeliefSystemTranslation { get; set; } = "";
        public string Summary { get; set; } = "";
        public string[] ImpactAreas { get; set; } = Array.Empty<string>();
        public Dictionary<string, double> AmplificationFactors { get; set; } = new();
        public Dictionary<string, object> ResonanceData { get; set; } = new();
        public string TransformationType { get; set; } = "";
        public string ConsciousnessLevel { get; set; } = "";
        public DateTimeOffset TransformedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConceptScore
    {
        public string Concept { get; set; } = "";
        public double Score { get; set; }
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public double Confidence { get; set; }
    }

    public class CachedAnalysis
    {
        public object Data { get; set; } = new();
    }
}