using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    [RequestType("codex.ai.concept-extraction-request", "ConceptExtractionRequest", "Request for concept extraction")]
    public record ConceptExtractionRequest(string Content, string? Model = null, string? Provider = null);
    
    [RequestType("codex.ai.fractal-transform-request", "FractalTransformRequest", "Request for fractal transformation")]
    public record FractalTransformRequest(string Content, string? Model = null, string? Provider = null);
    
    [RequestType("codex.ai.future-query-request", "AIFutureQueryRequest", "Request for future knowledge query")]
    public record AIFutureQueryRequest(string Query, string? Model = null, string? Provider = null);

    [MetaNodeAttribute("codex.ai.llm-configurations", "codex.meta/type", "LLMConfigurations", "LLM configuration management")]
    public static class LLMConfigurations
    {
        private static LLMConfig CreateConfig(string id, string name, string provider, string model, 
            double temperature, int maxTokens, double topP, string baseUrl = "http://localhost:11434")
        {
            var apiKey = provider.ToLowerInvariant() == "openai" 
                ? Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? ""
                : "";
                
            return new LLMConfig(
                Id: id,
                Name: name,
                Provider: provider,
                Model: model,
                ApiKey: apiKey,
                BaseUrl: baseUrl,
                MaxTokens: maxTokens,
                Temperature: temperature,
                TopP: topP,
                Parameters: new Dictionary<string, object>()
            );
        }

        // Mac M1 optimized configurations
        public static readonly LLMConfig MacM1_ConceptExtraction = CreateConfig(
            "macm1-concept-extraction", "Mac M1 Concept Extraction", "ollama", "llama3.1:8b", 
            0.3f, 2048, 0.9f);

        public static readonly LLMConfig MacM1_FractalTransform = CreateConfig(
            "macm1-fractal-transform", "Mac M1 Fractal Transform", "ollama", "llama3.1:8b", 
            0.7f, 4096, 0.95f);

        public static readonly LLMConfig MacM1_FutureQuery = CreateConfig(
            "macm1-future-query", "Mac M1 Future Query", "ollama", "llama3.1:8b", 
            0.8f, 3072, 0.9f);

        // Ollama Turbo mode configurations
        public static readonly LLMConfig OllamaTurbo_ConceptExtraction = CreateConfig(
            "ollama-turbo-concept-extraction", "Ollama Turbo Concept Extraction", "ollama", "llama3.1:8b-instruct-q4_0", 
            0.2f, 1024, 0.8f);

        public static readonly LLMConfig OllamaTurbo_FractalTransform = CreateConfig(
            "ollama-turbo-fractal-transform", "Ollama Turbo Fractal Transform", "ollama", "llama3.1:8b-instruct-q4_0", 
            0.6f, 2048, 0.9f);

        public static readonly LLMConfig OllamaTurbo_FutureQuery = CreateConfig(
            "ollama-turbo-future-query", "Ollama Turbo Future Query", "ollama", "llama3.1:8b-instruct-q4_0", 
            0.7f, 1536, 0.85f);

        // OpenAI configurations
        public static readonly LLMConfig OpenAI_ConceptExtraction = CreateConfig(
            "openai-concept-extraction", "OpenAI Concept Extraction", "openai", "gpt-4o-mini", 
            0.3f, 2048, 0.9f, "https://api.openai.com/v1");

        public static readonly LLMConfig OpenAI_FractalTransform = CreateConfig(
            "openai-fractal-transform", "OpenAI Fractal Transform", "openai", "gpt-4o", 
            0.7f, 4096, 0.95f, "https://api.openai.com/v1");

        public static readonly LLMConfig OpenAI_FutureQuery = CreateConfig(
            "openai-future-query", "OpenAI Future Query", "openai", "gpt-4o", 
            0.8f, 3072, 0.9f, "https://api.openai.com/v1");

        // Get configuration for a specific task
        public static LLMConfig GetConfigForTask(string task, string? preferredProvider = null, string? preferredModel = null)
        {
            // If specific model/provider requested, use them
            if (!string.IsNullOrEmpty(preferredProvider) && !string.IsNullOrEmpty(preferredModel))
            {
                var endpoint = preferredProvider.ToLowerInvariant() == "openai" ? "https://api.openai.com/v1" : "http://localhost:11434";
                return CreateConfig(
                    $"{preferredProvider}-{task}-custom", 
                    $"{preferredProvider} {task} (Custom)", 
                    preferredProvider, 
                    preferredModel,
                    GetDefaultTemperature(task),
                    GetDefaultMaxTokens(task),
                    GetDefaultTopP(task),
                    endpoint
                );
            }

            // Auto-detect best configuration based on available services
            // For now, default to Mac M1 optimized configs
            return task.ToLowerInvariant() switch
            {
                "concept-extraction" => MacM1_ConceptExtraction,
                "fractal-transform" => MacM1_FractalTransform,
                "future-query" => MacM1_FutureQuery,
                _ => MacM1_ConceptExtraction
            };
        }

        private static float GetDefaultTemperature(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 0.3f,
            "fractal-transform" => 0.7f,
            "future-query" => 0.8f,
            _ => 0.5f
        };

        private static int GetDefaultMaxTokens(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 2048,
            "fractal-transform" => 4096,
            "future-query" => 3072,
            _ => 2048
        };

        private static float GetDefaultTopP(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 0.9f,
            "fractal-transform" => 0.95f,
            "future-query" => 0.9f,
            _ => 0.9f
        };
    }

    /// <summary>
    /// Refactored AI Module - Concise, configurable, and pattern-driven
    /// </summary>
    [MetaNodeAttribute("codex.ai.module", "codex.meta/module", "AIModule", "AI Module for concept extraction, fractal transformation, and future queries")]
    public class AIModule : IModule
    {
        private readonly NodeRegistry _registry;
        private readonly Core.ILogger _logger;
        private readonly LLMOrchestrator _llmOrchestrator;
        private readonly PromptTemplateRepository _promptRepo;

        public AIModule(NodeRegistry registry)
        {
            _registry = registry;
            _logger = new Log4NetLogger(typeof(AIModule));
            
            // Initialize LLM infrastructure
            var httpClient = new HttpClient();
            var llmClient = new LLMClient(httpClient, _logger);
            _promptRepo = new PromptTemplateRepository(registry);
            _llmOrchestrator = new LLMOrchestrator(llmClient, _promptRepo, _logger);
            
            // Register default prompt templates
            RegisterPromptTemplates();
        }

        public string Name => "AI Module (Refactored)";
        public string Description => "Streamlined AI functionality with configurable prompts and reusable patterns";
        public string Version => "2.0.0";

        public Node GetModuleNode()
        {
            return NodeStorage.CreateModuleNode(
                id: "ai-module",
                name: Name,
                version: Version,
                description: Description,
                capabilities: new[] { "concept-extraction", "llm-integration", "fractal-transformation", "analysis" },
                tags: new[] { "ai", "concepts", "llm", "analysis", "refactored" },
                specReference: "codex.spec.ai"
            );
        }

        public void Register(NodeRegistry registry)
        {
            registry.Upsert(GetModuleNode());
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via [ApiRoute] attributes
        }

        public void RegisterHttpEndpoints(Microsoft.AspNetCore.Builder.WebApplication app, NodeRegistry registry, Runtime.CoreApiService coreApiService, Runtime.ModuleLoader moduleLoader)
        {
            // HTTP endpoints registration - not needed for this module
        }

        private void RegisterPromptTemplates()
        {
            var defaultLLMConfig = new LLMConfig(
                Id: "default-ai",
                Name: "Default AI Config",
                Provider: "ollama",
                Model: "llama2",
                ApiKey: "",
                BaseUrl: "http://localhost:11434",
                MaxTokens: 1000,
                Temperature: 0.3,
                TopP: 0.9,
                Parameters: new Dictionary<string, object> { ["stop"] = new[] { "---", "###" } }
            );

            var templates = new[]
            {
                new PromptTemplate(
                    Id: "concept-extraction",
                    Name: "Concept Extraction",
                    Template: @"Analyze the following text for consciousness-related concepts.

Text: ""{content}""

Focus on: consciousness, transformation, unity, love, wisdom, energy, healing, abundance, sacred patterns.

Return JSON array:
[
  {{
    ""concept"": ""concept_name"",
    ""score"": 0.0-1.0,
    ""description"": ""brief description"",
    ""category"": ""consciousness|transformation|unity|love|wisdom|energy|healing|abundance|sacred|fractal"",
    ""confidence"": 0.0-1.0
  }}
]",
                    DefaultParameters: new Dictionary<string, object> { ["content"] = "" },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "analysis"
                ),

                new PromptTemplate(
                    Id: "fractal-transformation",
                    Name: "Fractal Transformation",
                    Template: @"Transform the following content using fractal consciousness principles.

Content: ""{content}""
Consciousness Level: {consciousnessLevel}

Apply fractal patterns for:
1. Self-similarity across scales
2. Recursive depth expansion
3. Consciousness elevation
4. Unity resonance amplification

Return JSON:
{{
  ""transformedContent"": ""transformed version"",
  ""fractalPatterns"": [""pattern1"", ""pattern2""],
  ""consciousnessLevel"": ""L1-L7"",
  ""resonanceFrequency"": 0.0-1.0,
  ""unityScore"": 0.0-1.0
}}",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["content"] = "", 
                        ["consciousnessLevel"] = "L3" 
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "transformation"
                ),

                new PromptTemplate(
                    Id: "future-query",
                    Name: "Future Knowledge Query",
                    Template: @"You are an advanced AI specializing in future knowledge and consciousness expansion.

Context: {context}
Time Horizon: {timeHorizon}
Perspective: {perspective}

Query: {query}

Provide insights focusing on:
1. Future scenarios and trends
2. Consciousness implications
3. Practical applications
4. Spiritual insights

Response:",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["context"] = "",
                        ["timeHorizon"] = "5 years",
                        ["perspective"] = "consciousness-expanding",
                        ["query"] = ""
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "future"
                )
            };

            foreach (var template in templates)
            {
                _promptRepo.RegisterTemplate(template);
            }
        }

        #region API Handlers

        [ApiRoute("GET", "/ai/health", "ai-health", "AI Module health check", "ai-module")]
        public async Task<object> HandleHealthAsync(Dictionary<string, string> parameters)
        {
            var llmAvailable = await new LLMClient(new HttpClient(), _logger).IsServiceAvailableAsync();
            var promptCount = _promptRepo.GetTemplatesByCategory("analysis").Count + 
                             _promptRepo.GetTemplatesByCategory("transformation").Count + 
                             _promptRepo.GetTemplatesByCategory("future").Count;

            return new
            {
                success = true,
                module = Name,
                version = Version,
                llmServiceAvailable = llmAvailable,
                promptTemplatesLoaded = promptCount,
                timestamp = DateTimeOffset.UtcNow
            };
        }

        [ApiRoute("POST", "/ai/extract-concepts", "ai-extract-concepts", "Extract concepts using configurable prompts", "ai-module")]
        public async Task<object> HandleConceptExtractionAsync([ApiParameter("request", "Concept extraction request", Required = true, Location = "body")] ConceptExtractionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Content))
                {
                    return new { success = false, error = "Content is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("concept-extraction", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("concept-extraction", new Dictionary<string, object>
                {
                    ["content"] = request.Content
                }, config);

                if (!result.Success)
                {
                    return new { success = false, error = result.Error };
                }

                var concepts = _llmOrchestrator.ParseStructuredResponse<List<ConceptScore>>(result);

                return new
                {
                    success = true,
                    data = new
                    {
                        concepts = concepts ?? new List<ConceptScore>(),
                        confidence = result.Confidence,
                        isFallback = result.IsFallback,
                        timestamp = result.Timestamp,
                        tracking = new
                        {
                            templateId = result.TemplateId,
                            provider = result.Provider,
                            model = result.Model,
                            executionTimeMs = result.ExecutionTime.TotalMilliseconds
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in concept extraction: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/fractal-transform", "ai-fractal-transform", "Transform content using fractal patterns", "ai-module")]
        public async Task<object> HandleFractalTransformAsync([ApiParameter("request", "Fractal transform request", Required = true, Location = "body")] FractalTransformRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Content))
                {
                    return new { success = false, error = "Content is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("fractal-transform", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("fractal-transformation", new Dictionary<string, object>
                {
                    ["content"] = request.Content,
                    ["consciousnessLevel"] = "L3" // Default consciousness level
                }, config);

                if (!result.Success)
                {
                    return new { success = false, error = result.Error };
                }

                var transformation = _llmOrchestrator.ParseStructuredResponse<FractalTransformationResult>(result);

                return new
                {
                    success = true,
                    data = transformation,
                    confidence = result.Confidence,
                    isFallback = result.IsFallback,
                    timestamp = result.Timestamp,
                    tracking = new
                    {
                        templateId = result.TemplateId,
                        provider = result.Provider,
                        model = result.Model,
                        executionTimeMs = result.ExecutionTime.TotalMilliseconds
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in fractal transformation: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/future-query", "ai-future-query", "Query future knowledge with consciousness expansion", "ai-module")]
        public async Task<object> HandleFutureQueryAsync([ApiParameter("request", "Future query request", Required = true, Location = "body")] AIFutureQueryRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Query))
                {
                    return new { success = false, error = "Query is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("future-query", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("future-query", new Dictionary<string, object>
                {
                    ["query"] = request.Query,
                    ["context"] = "", // Default context
                    ["timeHorizon"] = "5 years", // Default time horizon
                    ["perspective"] = "consciousness-expanding" // Default perspective
                }, config);

                if (!result.Success)
                {
                    return new { success = false, error = result.Error };
                }

                return new
                {
                    success = true,
                    data = new
                    {
                        query = request.Query,
                        response = result.Content,
                        confidence = result.Confidence,
                        isFallback = result.IsFallback,
                        context = "", // Default context
                        timeHorizon = "5 years", // Default time horizon
                        perspective = "consciousness-expanding", // Default perspective
                        timestamp = result.Timestamp,
                        tracking = new
                        {
                            templateId = result.TemplateId,
                            provider = result.Provider,
                            model = result.Model,
                            executionTimeMs = result.ExecutionTime.TotalMilliseconds
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in future query: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("GET", "/ai/prompts", "ai-list-prompts", "List all available prompt templates", "ai-module")]
        public async Task<object> HandleListPromptsAsync(Dictionary<string, string> parameters)
        {
            try
            {
                var allTemplates = new List<PromptTemplate>();
                var categories = new[] { "analysis", "transformation", "future" };
                
                foreach (var category in categories)
                {
                    allTemplates.AddRange(_promptRepo.GetTemplatesByCategory(category));
                }

                return new
                {
                    success = true,
                    data = allTemplates.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        category = t.Category,
                        parameters = t.DefaultParameters.Keys.ToArray()
                    }).ToArray(),
                    totalCount = allTemplates.Count
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error listing prompts: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("GET", "/ai/prompts/{category}", "ai-get-prompts-by-category", "Get prompt templates by category", "ai-module")]
        public async Task<object> HandleGetPromptsByCategoryAsync(Dictionary<string, string> parameters)
        {
            try
            {
                if (!parameters.TryGetValue("category", out var category) || string.IsNullOrEmpty(category))
                {
                    return new { success = false, error = "Category is required" };
                }

                var templates = _promptRepo.GetTemplatesByCategory(category);
                
                return new
                {
                    success = true,
                    data = templates.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        template = t.Template,
                        defaultParameters = t.DefaultParameters,
                        defaultLLMConfig = t.DefaultLLMConfig
                    }).ToArray(),
                    category = category,
                    count = templates.Count
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting prompts by category: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("GET", "/ai/configurations", "ai-get-configurations", "Get available LLM configurations", "ai-module")]
        public async Task<object> HandleGetConfigurationsAsync(Dictionary<string, string> parameters)
        {
            try
            {
                var configurations = new
                {
                    macM1 = new
                    {
                        conceptExtraction = LLMConfigurations.MacM1_ConceptExtraction,
                        fractalTransform = LLMConfigurations.MacM1_FractalTransform,
                        futureQuery = LLMConfigurations.MacM1_FutureQuery
                    },
                    ollamaTurbo = new
                    {
                        conceptExtraction = LLMConfigurations.OllamaTurbo_ConceptExtraction,
                        fractalTransform = LLMConfigurations.OllamaTurbo_FractalTransform,
                        futureQuery = LLMConfigurations.OllamaTurbo_FutureQuery
                    },
                    openai = new
                    {
                        conceptExtraction = LLMConfigurations.OpenAI_ConceptExtraction,
                        fractalTransform = LLMConfigurations.OpenAI_FractalTransform,
                        futureQuery = LLMConfigurations.OpenAI_FutureQuery
                    }
                };

                return new
                {
                    success = true,
                    data = configurations,
                    timestamp = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting configurations: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

    #endregion
    }

    #region Data Structures

    [MetaNodeAttribute("codex.ai.concept-score", "codex.meta/type", "ConceptScore", "Score for a concept")]
    public record ConceptScore(
        string Concept,
        double Score,
        string Description,
        string Category,
        double Confidence
    );

    [ResponseType("codex.ai.fractal-transformation-result", "FractalTransformationResult", "Result of fractal transformation")]
    public record FractalTransformationResult(
        string TransformedContent,
        string[] FractalPatterns,
        string ConsciousnessLevel,
        double ResonanceFrequency,
        double UnityScore
    );

    // Legacy data structures for compatibility
    [MetaNodeAttribute("codex.ai.fractal-transformation", "codex.meta/type", "FractalTransformation", "Fractal transformation algorithm")]
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

    [MetaNodeAttribute("codex.ai.cached-analysis", "codex.meta/type", "CachedAnalysis", "Cached analysis result")]
    public class CachedAnalysis
    {
        public object Data { get; set; } = new();
    }

    // Legacy record types for compatibility with other modules
    [RequestType("codex.ai.llm-config-request", "LLMConfigRequest", "Request for LLM configuration")]
    public record LLMConfigRequest(LLMConfig Config);

    [ResponseType("codex.ai.concept-extraction-response", "AIConceptExtractionResponse", "Response from concept extraction")]
    public record AIConceptExtractionResponse(
        string Content,
        double Confidence,
        string Reasoning,
        List<string> Sources
    );

    [MetaNodeAttribute("codex.ai.llm-config", "codex.meta/type", "LLMConfig", "LLM configuration record")]
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

    [MetaNodeAttribute("codex.ai.scoring-analysis", "codex.meta/type", "ScoringAnalysis", "Analysis scoring system")]
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

    // Additional missing record types
    [ResponseType("codex.ai.llm-config-response", "LLMConfigResponse", "Response with LLM configuration")]
    public record LLMConfigResponse(
        bool Success,
        LLMConfig Config,
        string Message,
        DateTimeOffset Timestamp
    );


    #endregion
}
