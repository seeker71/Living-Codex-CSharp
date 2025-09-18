using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    
    [RequestType("codex.ai.scoring-analysis-request", "ScoringAnalysisRequest", "Request for scoring analysis")]
    public record ScoringAnalysisRequest(string Content, string? AnalysisType = null, string[]? Criteria = null, string? Model = null, string? Provider = null);

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

        // OpenAI code generation configuration (Sept 2025 best available)
        public static readonly LLMConfig OpenAI_CodeGeneration = CreateConfig(
            "openai-codegen", "OpenAI Code Generation", "openai", 
            Environment.GetEnvironmentVariable("OPENAI_CODEGEN_MODEL") ?? "gpt-5-codex",
            0.2f, 8192, 0.95f, Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1");

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
            // Prefer OpenAI GPT-5 Codex for UI code generation when configured
            var isOpenAIConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            var normalizedTask = task.ToLowerInvariant();

            if ((normalizedTask == "ui-page-generation" || normalizedTask == "ui-component-generation" || normalizedTask == "ui-pattern-evolution")
                && isOpenAIConfigured)
            {
                return OpenAI_CodeGeneration;
            }

            // Defaults (local fast models) for analysis/transformation when OpenAI isn't configured
            return normalizedTask switch
            {
                "concept-extraction" => MacM1_ConceptExtraction,
                "fractal-transformation" => MacM1_FractalTransform,
                "fractal-transform" => MacM1_FractalTransform,
                "future-query" => MacM1_FutureQuery,
                _ => isOpenAIConfigured ? OpenAI_CodeGeneration : MacM1_ConceptExtraction
            };
        }

        private static float GetDefaultTemperature(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 0.3f,
            "fractal-transformation" => 0.7f,
            "fractal-transform" => 0.7f,
            "future-query" => 0.8f,
            _ => 0.5f
        };

        private static int GetDefaultMaxTokens(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 2048,
            "fractal-transformation" => 4096,
            "fractal-transform" => 4096,
            "future-query" => 3072,
            _ => 2048
        };

        private static float GetDefaultTopP(string task) => task.ToLowerInvariant() switch
        {
            "concept-extraction" => 0.9f,
            "fractal-transformation" => 0.95f,
            "fractal-transform" => 0.95f,
            "future-query" => 0.9f,
            _ => 0.9f
        };
    }

    /// <summary>
    /// Refactored AI Module - Concise, configurable, and pattern-driven
    /// </summary>
    [MetaNodeAttribute("codex.ai.module", "codex.meta/module", "AIModule", "AI Module for concept extraction, fractal transformation, and future queries")]
    public class AIModule : ModuleBase
    {
        private readonly LLMOrchestrator _llmOrchestrator;
        private readonly PromptTemplateRepository _promptRepo;

        public override string Name => "AI Module (Refactored)";
        public override string Description => "Streamlined AI functionality with configurable prompts and reusable patterns";
        public override string Version => "2.0.0";

        public AIModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
            : base(registry, logger)
        {
            // Initialize LLM infrastructure
            var llmClient = new LLMClient(httpClient, logger);
            _promptRepo = new PromptTemplateRepository(registry);
            _llmOrchestrator = new LLMOrchestrator(llmClient, _promptRepo, logger);
            
            // Register default prompt templates
            RegisterPromptTemplates();
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "ai-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "ai", "concepts", "llm", "analysis", "refactored" },
                capabilities: new[] { "concept-extraction", "llm-integration", "fractal-transformation", "analysis" },
                spec: "codex.spec.ai"
            );
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            // Register AI handlers for internal module communication
            router.Register("ai", "extract-concepts", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<ConceptExtractionRequest>(json?.GetRawText() ?? "{}");
                return await HandleConceptExtractionAsync(request);
            });
            
            router.Register("ai", "score-analysis", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<ScoringAnalysisRequest>(json?.GetRawText() ?? "{}");
                return await HandleScoringAnalysisAsync(request);
            });
            
            router.Register("ai", "fractal-transform", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<FractalTransformRequest>(json?.GetRawText() ?? "{}");
                return await HandleFractalTransformAsync(request);
            });
            
            router.Register("ai", "generate-ui-page", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<UIPageGenerationRequest>(json?.GetRawText() ?? "{}");
                return await HandleUIPageGenerationAsync(request);
            });
            
            router.Register("ai", "generate-ui-component", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<UIComponentGenerationRequest>(json?.GetRawText() ?? "{}");
                return await HandleUIComponentGenerationAsync(request);
            });
            
            router.Register("ai", "analyze-ui-feedback", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<UIFeedbackAnalysisRequest>(json?.GetRawText() ?? "{}");
                return await HandleUIFeedbackAnalysisAsync(request);
            });
            
            router.Register("ai", "evolve-ui-pattern", async (JsonElement? json) => 
            {
                var request = JsonSerializer.Deserialize<UIPatternEvolutionRequest>(json?.GetRawText() ?? "{}");
                return await HandleUIPatternEvolutionAsync(request);
            });
            
            _logger.Info("AI module API handlers registered for internal communication");
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApiService, ModuleLoader moduleLoader)
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

IMPORTANT: Return ONLY a valid JSON array, no markdown, no explanations, no additional text. Start with [ and end with ].

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
                    Template: @"You are a JSON API. You must respond with ONLY valid JSON. No markdown, no explanations, no other text.

Transform this content: ""{content}""

Return this exact JSON structure:
{
  ""headline"": ""transformed headline"",
  ""beliefTranslation"": ""how this translates to belief systems"",
  ""summary"": ""transformed summary"",
  ""impactAreas"": [""area1"", ""area2""],
  ""consciousnessLevel"": ""L3"",
  ""resonanceFrequency"": 0.8,
  ""unityScore"": 0.7
}

RESPOND WITH ONLY THE JSON OBJECT. NO OTHER TEXT.",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["content"] = "", 
                        ["consciousnessLevel"] = "L3" 
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "transformation"
                ),

                new PromptTemplate(
                    Id: "scoring-analysis",
                    Name: "Scoring Analysis",
                    Template: @"Analyze and score the following content based on the specified criteria.

Content: ""{content}""
Analysis Type: {analysisType}
Criteria: {criteria}

Score each criterion from 0.0 to 1.0:
- Abundance: Growth, prosperity, opportunity, collective benefit
- Consciousness: Awareness, mindfulness, wisdom, spiritual insight
- Unity: Connection, collaboration, harmony, global perspective

IMPORTANT: Return ONLY a valid JSON object, no markdown, no explanations, no additional text. Start with {{ and end with }}.

{{
  ""abundanceScore"": 0.0-1.0,
  ""consciousnessScore"": 0.0-1.0,
  ""unityScore"": 0.0-1.0,
  ""overallScore"": 0.0-1.0,
  ""reasoning"": ""brief explanation of scores""
}}",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["content"] = "",
                        ["analysisType"] = "general",
                        ["criteria"] = new[] { "relevance", "quality", "impact" }
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "analysis"
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
                ),

                new PromptTemplate(
                    Id: "ui-page-generation",
                    Name: "UI Page Generation",
                    Template: @"Generate a Next.js page component from this UI atom specification.

UI Atom: {uiAtom}

Requirements:
- Use TypeScript and Tailwind CSS
- Implement the specified lenses and controls
- Connect to the declared API endpoints
- Follow the Living Codex design principles (resonance, joy, unity)
- Use React Query for data fetching
- Include proper error handling and loading states

IMPORTANT: Return ONLY valid TypeScript code, no markdown, no explanations, no additional text.

```typescript
{generatedCode}
```",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["uiAtom"] = "",
                        ["generatedCode"] = ""
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "ui-generation"
                ),

                new PromptTemplate(
                    Id: "ui-component-generation",
                    Name: "UI Component Generation",
                    Template: @"Generate a React component from this lens specification.

Lens Spec: {lensSpec}
Component Type: {componentType}
Requirements: {requirements}

Requirements:
- Use the specified projection type (list, masonry, thread, etc.)
- Implement the declared actions (attune, amplify, weave, reflect, invite)
- Connect to the adapter endpoints
- Follow resonance-driven design principles
- Use TypeScript and Tailwind CSS
- Include proper TypeScript interfaces

IMPORTANT: Return ONLY valid TypeScript code, no markdown, no explanations, no additional text.

```typescript
{generatedCode}
```",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["lensSpec"] = "",
                        ["componentType"] = "list",
                        ["requirements"] = "",
                        ["generatedCode"] = ""
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "ui-generation"
                ),

                new PromptTemplate(
                    Id: "ui-feedback-analysis",
                    Name: "UI Feedback Analysis",
                    Template: @"Analyze user feedback on this UI component and suggest specific improvements.

Component ID: {componentId}
Component Code: {componentCode}
User Feedback: {feedback}
Usage Metrics: {metrics}

Analyze and suggest improvements to:
1. Copy and messaging (resonance, joy, clarity)
2. Interaction patterns (attune, amplify, weave, reflect, invite)
3. Visual design (resonance-driven, inviting, not overwhelming)
4. API integration (endpoint bindings, error handling)
5. Performance and accessibility

IMPORTANT: Return ONLY a valid JSON object, no markdown, no explanations, no additional text.

{{
  ""analysis"": {{
    ""strengths"": [""strength1"", ""strength2""],
    ""weaknesses"": [""weakness1"", ""weakness2""],
    ""suggestions"": [
      {{
        ""area"": ""copy"",
        ""priority"": ""high"",
        ""suggestion"": ""specific improvement"",
        ""reasoning"": ""why this helps""
      }}
    ],
    ""resonanceScore"": 0.0-1.0,
    ""joyScore"": 0.0-1.0,
    ""unityScore"": 0.0-1.0
  }},
  ""recommendedChanges"": [
    {{
      ""type"": ""copy"",
      ""change"": ""specific change to make"",
      ""impact"": ""expected improvement""
    }}
  ]
}}",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["componentId"] = "",
                        ["componentCode"] = "",
                        ["feedback"] = "",
                        ["metrics"] = ""
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "ui-feedback"
                ),

                new PromptTemplate(
                    Id: "ui-pattern-evolution",
                    Name: "UI Pattern Evolution",
                    Template: @"Evolve this successful UI pattern into a reusable template for future components.

Original Pattern: {originalPattern}
Success Metrics: {successMetrics}
Evolution Context: {evolutionContext}

Create an evolved template that:
1. Generalizes the successful elements
2. Maintains the resonance-driven principles
3. Adds flexibility for different use cases
4. Preserves the joy and unity aspects
5. Includes proper TypeScript interfaces
6. Documents the pattern's purpose and usage

IMPORTANT: Return ONLY a valid JSON object with the evolved template, no markdown, no explanations, no additional text.

{{
  ""templateId"": ""evolved-pattern-id"",
  ""name"": ""Evolved Pattern Name"",
  ""description"": ""What this pattern does"",
  ""category"": ""lens|page|component|action"",
  ""template"": {{
    ""code"": ""evolved TypeScript code"",
    ""interfaces"": ""TypeScript interfaces"",
    ""props"": [""prop1"", ""prop2""],
    ""endpoints"": [""endpoint1"", ""endpoint2""],
    ""actions"": [""attune"", ""amplify"", ""weave""]
  }},
  ""usage"": {{
    ""whenToUse"": ""when to apply this pattern"",
    ""variations"": [""variation1"", ""variation2""],
    ""customization"": [""customization1"", ""customization2""]
  }},
  ""resonanceFactors"": {{
    ""joy"": 0.0-1.0,
    ""unity"": 0.0-1.0,
    ""clarity"": 0.0-1.0,
    ""engagement"": 0.0-1.0
  }}
}}",
                    DefaultParameters: new Dictionary<string, object> 
                    { 
                        ["originalPattern"] = "",
                        ["successMetrics"] = "",
                        ["evolutionContext"] = ""
                    },
                    DefaultLLMConfig: defaultLLMConfig,
                    Category: "ui-evolution"
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

                return _llmOrchestrator.ParseStructuredResponse<List<ConceptScore>>(result, "concept extraction");
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

                var config = LLMConfigurations.GetConfigForTask("fractal-transformation", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("fractal-transformation", new Dictionary<string, object>
                {
                    ["content"] = request.Content,
                    ["consciousnessLevel"] = "L3" // Default consciousness level
                }, config);

                return _llmOrchestrator.ParseStructuredResponse<FractalTransformationResult>(result, "fractal transformation");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in fractal transformation: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/score-analysis", "ai-score-analysis", "Score and analyze content using AI", "ai-module")]
        public async Task<object> HandleScoringAnalysisAsync([ApiParameter("request", "Scoring analysis request", Required = true, Location = "body")] ScoringAnalysisRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Content))
                {
                    return new { success = false, error = "Content is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("scoring-analysis", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("scoring-analysis", new Dictionary<string, object>
                {
                    ["content"] = request.Content,
                    ["analysisType"] = request.AnalysisType ?? "general",
                    ["criteria"] = request.Criteria ?? new[] { "relevance", "quality", "impact" }
                }, config);

                return _llmOrchestrator.ParseStructuredResponse<ScoringAnalysisResult>(result, "scoring analysis");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in scoring analysis: {ex.Message}", ex);
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

        [ApiRoute("POST", "/ai/generate-ui-page", "ai-generate-ui-page", "Generate UI page component from atom specification", "ai-module")]
        public async Task<object> HandleUIPageGenerationAsync([ApiParameter("request", "UI page generation request", Required = true, Location = "body")] UIPageGenerationRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UiAtom))
                {
                    return new { success = false, error = "UI atom specification is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("ui-page-generation", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("ui-page-generation", new Dictionary<string, object>
                {
                    ["uiAtom"] = request.UiAtom,
                    ["generatedCode"] = ""
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
                        pageId = request.PageId,
                        generatedCode = result.Content,
                        confidence = result.Confidence,
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
                _logger.Error($"Error in UI page generation: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/generate-ui-component", "ai-generate-ui-component", "Generate UI component from lens specification", "ai-module")]
        public async Task<object> HandleUIComponentGenerationAsync([ApiParameter("request", "UI component generation request", Required = true, Location = "body")] UIComponentGenerationRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.LensSpec))
                {
                    return new { success = false, error = "Lens specification is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("ui-component-generation", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("ui-component-generation", new Dictionary<string, object>
                {
                    ["lensSpec"] = request.LensSpec,
                    ["componentType"] = request.ComponentType ?? "list",
                    ["requirements"] = request.Requirements ?? "",
                    ["generatedCode"] = ""
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
                        componentId = request.ComponentId,
                        generatedCode = result.Content,
                        confidence = result.Confidence,
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
                _logger.Error($"Error in UI component generation: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/analyze-ui-feedback", "ai-analyze-ui-feedback", "Analyze UI feedback and suggest improvements", "ai-module")]
        public async Task<object> HandleUIFeedbackAnalysisAsync([ApiParameter("request", "UI feedback analysis request", Required = true, Location = "body")] UIFeedbackAnalysisRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.ComponentId) || string.IsNullOrEmpty(request.Feedback))
                {
                    return new { success = false, error = "Component ID and feedback are required" };
                }

                var config = LLMConfigurations.GetConfigForTask("ui-feedback-analysis", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("ui-feedback-analysis", new Dictionary<string, object>
                {
                    ["componentId"] = request.ComponentId,
                    ["componentCode"] = request.ComponentCode ?? "",
                    ["feedback"] = request.Feedback,
                    ["metrics"] = request.Metrics ?? ""
                }, config);

                if (!result.Success)
                {
                    return new { success = false, error = result.Error };
                }

                return _llmOrchestrator.ParseStructuredResponse<UIFeedbackAnalysisResult>(result, "UI feedback analysis");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in UI feedback analysis: {ex.Message}", ex);
                return new { success = false, error = ex.Message };
            }
        }

        [ApiRoute("POST", "/ai/evolve-ui-pattern", "ai-evolve-ui-pattern", "Evolve successful UI pattern into reusable template", "ai-module")]
        public async Task<object> HandleUIPatternEvolutionAsync([ApiParameter("request", "UI pattern evolution request", Required = true, Location = "body")] UIPatternEvolutionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.OriginalPattern))
                {
                    return new { success = false, error = "Original pattern is required" };
                }

                var config = LLMConfigurations.GetConfigForTask("ui-pattern-evolution", request.Provider, request.Model);
                var result = await _llmOrchestrator.ExecuteAsync("ui-pattern-evolution", new Dictionary<string, object>
                {
                    ["originalPattern"] = request.OriginalPattern,
                    ["successMetrics"] = request.SuccessMetrics ?? "",
                    ["evolutionContext"] = request.EvolutionContext ?? ""
                }, config);

                if (!result.Success)
                {
                    return new { success = false, error = result.Error };
                }

                return _llmOrchestrator.ParseStructuredResponse<UIPatternEvolutionResult>(result, "UI pattern evolution");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in UI pattern evolution: {ex.Message}", ex);
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
        [property: JsonPropertyName("concept")] string Concept,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("confidence")] double Confidence
    );

    [ResponseType("codex.ai.fractal-transformation-result", "FractalTransformationResult", "Result of fractal transformation")]
    public record FractalTransformationResult(
        [property: JsonPropertyName("headline")] string Headline,
        [property: JsonPropertyName("beliefTranslation")] string BeliefTranslation,
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("impactAreas")] string[] ImpactAreas,
        [property: JsonPropertyName("consciousnessLevel")] string ConsciousnessLevel,
        [property: JsonPropertyName("resonanceFrequency")] double ResonanceFrequency,
        [property: JsonPropertyName("unityScore")] double UnityScore
    );

    [ResponseType("codex.ai.scoring-analysis-result", "ScoringAnalysisResult", "Result of scoring analysis")]
    public record ScoringAnalysisResult(
        [property: JsonPropertyName("abundanceScore")] double AbundanceScore,
        [property: JsonPropertyName("consciousnessScore")] double ConsciousnessScore,
        [property: JsonPropertyName("unityScore")] double UnityScore,
        [property: JsonPropertyName("overallScore")] double OverallScore,
        [property: JsonPropertyName("reasoning")] string Reasoning
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

    // UI Generation Request/Response Types
    [RequestType("codex.ai.ui-page-generation-request", "UIPageGenerationRequest", "Request for UI page generation")]
    public record UIPageGenerationRequest(
        string PageId,
        string UiAtom,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.ai.ui-component-generation-request", "UIComponentGenerationRequest", "Request for UI component generation")]
    public record UIComponentGenerationRequest(
        string ComponentId,
        string LensSpec,
        string? ComponentType = null,
        string? Requirements = null,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.ai.ui-feedback-analysis-request", "UIFeedbackAnalysisRequest", "Request for UI feedback analysis")]
    public record UIFeedbackAnalysisRequest(
        string ComponentId,
        string Feedback,
        string? ComponentCode = null,
        string? Metrics = null,
        string? Provider = null,
        string? Model = null
    );

    [RequestType("codex.ai.ui-pattern-evolution-request", "UIPatternEvolutionRequest", "Request for UI pattern evolution")]
    public record UIPatternEvolutionRequest(
        string OriginalPattern,
        string? SuccessMetrics = null,
        string? EvolutionContext = null,
        string? Provider = null,
        string? Model = null
    );

    [ResponseType("codex.ai.ui-feedback-analysis-result", "UIFeedbackAnalysisResult", "Result from UI feedback analysis")]
    public record UIFeedbackAnalysisResult(
        [property: JsonPropertyName("analysis")] UIFeedbackAnalysis Analysis,
        [property: JsonPropertyName("recommendedChanges")] List<UIRecommendedChange> RecommendedChanges
    );

    [ResponseType("codex.ai.ui-feedback-analysis", "UIFeedbackAnalysis", "UI feedback analysis data")]
    public record UIFeedbackAnalysis(
        [property: JsonPropertyName("strengths")] List<string> Strengths,
        [property: JsonPropertyName("weaknesses")] List<string> Weaknesses,
        [property: JsonPropertyName("suggestions")] List<UIFeedbackSuggestion> Suggestions,
        [property: JsonPropertyName("resonanceScore")] double ResonanceScore,
        [property: JsonPropertyName("joyScore")] double JoyScore,
        [property: JsonPropertyName("unityScore")] double UnityScore
    );

    [ResponseType("codex.ai.ui-feedback-suggestion", "UIFeedbackSuggestion", "UI feedback suggestion")]
    public record UIFeedbackSuggestion(
        [property: JsonPropertyName("area")] string Area,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("suggestion")] string Suggestion,
        [property: JsonPropertyName("reasoning")] string Reasoning
    );

    [ResponseType("codex.ai.ui-recommended-change", "UIRecommendedChange", "UI recommended change")]
    public record UIRecommendedChange(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("change")] string Change,
        [property: JsonPropertyName("impact")] string Impact
    );

    [ResponseType("codex.ai.ui-pattern-evolution-result", "UIPatternEvolutionResult", "Result from UI pattern evolution")]
    public record UIPatternEvolutionResult(
        [property: JsonPropertyName("templateId")] string TemplateId,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("template")] UIPatternTemplate Template,
        [property: JsonPropertyName("usage")] UIPatternUsage Usage,
        [property: JsonPropertyName("resonanceFactors")] UIPatternResonanceFactors ResonanceFactors
    );

    [ResponseType("codex.ai.ui-pattern-template", "UIPatternTemplate", "UI pattern template")]
    public record UIPatternTemplate(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("interfaces")] string Interfaces,
        [property: JsonPropertyName("props")] List<string> Props,
        [property: JsonPropertyName("endpoints")] List<string> Endpoints,
        [property: JsonPropertyName("actions")] List<string> Actions
    );

    [ResponseType("codex.ai.ui-pattern-usage", "UIPatternUsage", "UI pattern usage information")]
    public record UIPatternUsage(
        [property: JsonPropertyName("whenToUse")] string WhenToUse,
        [property: JsonPropertyName("variations")] List<string> Variations,
        [property: JsonPropertyName("customization")] List<string> Customization
    );

    [ResponseType("codex.ai.ui-pattern-resonance-factors", "UIPatternResonanceFactors", "UI pattern resonance factors")]
    public record UIPatternResonanceFactors(
        [property: JsonPropertyName("joy")] double Joy,
        [property: JsonPropertyName("unity")] double Unity,
        [property: JsonPropertyName("clarity")] double Clarity,
        [property: JsonPropertyName("engagement")] double Engagement
    );


    #endregion
}
