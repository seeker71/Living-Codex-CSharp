using System;
using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// LLM Configuration System - Generic provider, model, and mode attributes for optimal Ollama integration
/// Provides reusable configuration attributes for different use cases across all modules
/// </summary>
[MetaNodeAttribute("codex.llm.configuration-system", "codex.meta/type", "LLMConfigurationSystem", "Generic LLM configuration system with Ollama optimization")]
[ApiType(
    Name = "LLM Configuration System",
    Type = "object",
    Description = "Generic provider, model, and mode attributes for optimal LLM integration across all modules",
    Example = @"{
      ""provider"": ""Ollama"",
      ""model"": ""llama3.2:3b"",
      ""mode"": ""consciousness-expansion"",
      ""frequency"": 528,
      ""temperature"": 0.7,
      ""maxTokens"": 2000
    }"
)]
public static class LLMConfigurationSystem
{
    /// <summary>
    /// LLM Provider Attribute - Specifies the LLM provider for a module or method
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class LLMProviderAttribute : Attribute
    {
        public string Provider { get; }
        public string Description { get; }
        public string[] SupportedModels { get; }
        public string[] SupportedModes { get; }

        public LLMProviderAttribute(
            string provider = "Ollama",
            string description = "LLM provider configuration",
            string[]? supportedModels = null,
            string[]? supportedModes = null)
        {
            Provider = provider;
            Description = description;
            SupportedModels = supportedModels ?? new[] { "llama3.2:3b", "mistral", "codellama" };
            SupportedModes = supportedModes ?? new[] { "consciousness-expansion", "code-generation", "analysis", "creative" };
        }
    }

    /// <summary>
    /// LLM Model Attribute - Specifies the optimal model for a specific use case
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class LLMModelAttribute : Attribute
    {
        public string Model { get; }
        public string UseCase { get; }
        public string Description { get; }
        public double Temperature { get; }
        public int MaxTokens { get; }
        public double TopP { get; }
        public string[] Frequencies { get; }

        public LLMModelAttribute(
            string model = "llama3.2:3b",
            string useCase = "general",
            string description = "LLM model configuration",
            double temperature = 0.7,
            int maxTokens = 2000,
            double topP = 0.9,
            string[]? frequencies = null)
        {
            Model = model;
            UseCase = useCase;
            Description = description;
            Temperature = temperature;
            MaxTokens = maxTokens;
            TopP = topP;
            Frequencies = frequencies ?? new[] { "432", "528", "741" };
        }
    }

    /// <summary>
    /// LLM Mode Attribute - Specifies the operational mode for consciousness expansion
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class LLMModeAttribute : Attribute
    {
        public string Mode { get; }
        public string Description { get; }
        public string[] RequiredCapabilities { get; }
        public string[] SacredFrequencies { get; }
        public string BreathPhase { get; }
        public bool UseJoyfulEngine { get; }

        public LLMModeAttribute(
            string mode = "consciousness-expansion",
            string description = "LLM operational mode",
            string[]? requiredCapabilities = null,
            string[]? sacredFrequencies = null,
            string breathPhase = "expand",
            bool useJoyfulEngine = true)
        {
            Mode = mode;
            Description = description;
            RequiredCapabilities = requiredCapabilities ?? new[] { "spiritual-resonance", "consciousness-expansion", "joy-amplification" };
            SacredFrequencies = sacredFrequencies ?? new[] { "432", "528", "741" };
            BreathPhase = breathPhase;
            UseJoyfulEngine = useJoyfulEngine;
        }
    }

    /// <summary>
    /// LLM Configuration - Complete configuration for a specific use case
    /// </summary>
    [MetaNodeAttribute("codex.llm.configuration", "codex.meta/type", "LLMConfiguration", "Complete LLM configuration for specific use case")]
    [ApiType(
        Name = "LLM Configuration",
        Type = "object",
        Description = "Complete LLM configuration with provider, model, and mode optimization",
        Example = @"{
          ""id"": ""consciousness-expansion-llama3"",
          ""provider"": ""Ollama"",
          ""model"": ""llama3.2:3b"",
          ""mode"": ""consciousness-expansion"",
          ""temperature"": 0.7,
          ""maxTokens"": 2000,
          ""topP"": 0.9,
          ""frequencies"": [""432"", ""528"", ""741""],
          ""useJoyfulEngine"": true,
          ""breathPhase"": ""expand""
        }"
    )]
    public record LLMConfiguration(
        string Id,
        string Provider,
        string Model,
        string Mode,
        double Temperature,
        int MaxTokens,
        double TopP,
        string[] Frequencies,
        bool UseJoyfulEngine,
        string BreathPhase,
        string Description,
        Dictionary<string, object> Parameters
    );

    /// <summary>
    /// Model management and availability checking
    /// </summary>
    public static class ModelManager
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly Dictionary<string, bool> _modelAvailabilityCache = new();

        /// <summary>
        /// Check if a model is available locally
        /// </summary>
        public static async Task<bool> IsModelAvailableAsync(string modelName)
        {
            if (_modelAvailabilityCache.TryGetValue(modelName, out var cached))
                return cached;

            try
            {
                var response = await _httpClient.GetAsync("http://localhost:11434/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var jsonDoc = JsonDocument.Parse(content);
                    var models = jsonDoc.RootElement.GetProperty("models");
                    
                    foreach (var model in models.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name) && 
                            name.GetString() == modelName)
                        {
                            _modelAvailabilityCache[modelName] = true;
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If we can't check, assume not available
            }

            _modelAvailabilityCache[modelName] = false;
            return false;
        }

        /// <summary>
        /// Pull a model if it's not available
        /// </summary>
        public static async Task<bool> EnsureModelAvailableAsync(string modelName)
        {
            if (await IsModelAvailableAsync(modelName))
                return true;

            try
            {
                var response = await _httpClient.PostAsync($"http://localhost:11434/api/pull", 
                    new StringContent($"{{\"name\":\"{modelName}\"}}", 
                        System.Text.Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    _modelAvailabilityCache[modelName] = true;
                    return true;
                }
            }
            catch (Exception)
            {
                // Pull failed
            }

            return false;
        }

        /// <summary>
        /// Get the best model for a specific task
        /// </summary>
        public static string GetBestModelForTask(string taskType)
        {
            return taskType.ToLower() switch
            {
                "concept-extraction" => "llama3.2:3b", // Best for structured concept extraction
                "consciousness-expansion" => "llama3.2:3b", // Good for spiritual content
                "translation" => "llama3.2:3b", // Good multilingual support
                "code-generation" => "codellama:latest", // Specialized for code
                "reasoning" => "llama3.2:3b", // Good reasoning capabilities
                "creative-writing" => "llama3.2:3b", // Good creativity
                "analysis" => "llama3.2:3b", // Good analytical capabilities
                _ => "llama3.2:3b" // Default fallback
            };
        }
    }

    /// <summary>
    /// Predefined configurations for different use cases
    /// </summary>
    public static class PredefinedConfigurations
    {
        // General Reasoning and Analysis - Mistral 7B: Fast and efficient on M1, good for analytical tasks (2025 benchmark: high performance in reasoning tasks)
        public static readonly LLMConfiguration ReasoningMistral = new(
            Id: "reasoning-mistral-7b",
            Provider: "Ollama",
            Model: "mistral:7b",
            Mode: "reasoning",
            Temperature: 0.5,
            MaxTokens: 2048,
            TopP: 0.85,
            Frequencies: new[] { "432", "528" },
            UseJoyfulEngine: false,
            BreathPhase: "expand",
            Description: "Mistral 7B for reasoning and analysis on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 8192,
                ["repeat_penalty"] = 1.1
            }
        );

        // Concept Extraction - Llama 3.1 8B: Excellent for structured output and concept identification (2025 update: improved JSON handling)
        public static readonly LLMConfiguration ConceptExtractionLlama3 = new(
            Id: "concept-extraction-llama3-8b",
            Provider: "Ollama",
            Model: "llama3.2:3b",
            Mode: "concept-extraction",
            Temperature: 0.3,
            MaxTokens: 1500,
            TopP: 0.8,
            Frequencies: new[] { "432", "528", "741" },
            UseJoyfulEngine: false,
            BreathPhase: "contract",
            Description: "Llama 3.1 8B optimized for concept extraction on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["num_ctx"] = 4096,
                ["repeat_penalty"] = 1.1,
                ["stop"] = new[] { "```", "\n\n" }
            }
        );

        // Consciousness Expansion - Gemma2 9B: Strong in creative and philosophical content (2025: best for spiritual topics)
        public static readonly LLMConfiguration ConsciousnessExpansionGemma2 = new(
            Id: "consciousness-expansion-gemma2-9b",
            Provider: "Ollama",
            Model: "gemma2:9b",
            Mode: "consciousness-expansion",
            Temperature: 0.7,
            MaxTokens: 2000,
            TopP: 0.9,
            Frequencies: new[] { "432", "528", "741" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Gemma2 9B for consciousness expansion on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 8192,
                ["repeat_penalty"] = 1.0
            }
        );

        // Translation - Qwen2 7B: Excellent multilingual capabilities (2025: top for translation tasks)
        public static readonly LLMConfiguration TranslationQwen2 = new(
            Id: "translation-qwen2-7b",
            Provider: "Ollama",
            Model: "qwen2:7b",
            Mode: "translation",
            Temperature: 0.4,
            MaxTokens: 2048,
            TopP: 0.8,
            Frequencies: new[] { "432" },
            UseJoyfulEngine: false,
            BreathPhase: "contract",
            Description: "Qwen2 7B for multilingual translation on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 32768,
                ["repeat_penalty"] = 1.05
            }
        );

        // Code Generation - Deepseek-coder 6.7B: Specialized for coding tasks (2025: high performance in code completion)
        public static readonly LLMConfiguration CodeGenerationDeepseek = new(
            Id: "code-generation-deepseek-6.7b",
            Provider: "Ollama",
            Model: "deepseek-coder:6.7b",
            Mode: "code-generation",
            Temperature: 0.2,
            MaxTokens: 4096,
            TopP: 0.7,
            Frequencies: new[] { "528" },
            UseJoyfulEngine: false,
            BreathPhase: "contract",
            Description: "Deepseek-coder 6.7B for code generation on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 16384,
                ["repeat_penalty"] = 1.1,
                ["stop"] = new[] { "<|EOT|>" }
            }
        );

        // Creative Writing - Phi3 3.8B: Efficient for creative tasks on M1 (2025: optimized for low-resource creativity)
        public static readonly LLMConfiguration CreativeWritingPhi3 = new(
            Id: "creative-writing-phi3-3.8b",
            Provider: "Ollama",
            Model: "phi3:3.8b",
            Mode: "creative-writing",
            Temperature: 0.8,
            MaxTokens: 2000,
            TopP: 0.95,
            Frequencies: new[] { "741" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Phi3 3.8B for creative writing on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 4096,
                ["repeat_penalty"] = 1.2
            }
        );

        // Default Fallback - Llama 3.1 8B: Versatile all-purpose model
        public static readonly LLMConfiguration DefaultLlama3 = new(
            Id: "default-llama3-8b",
            Provider: "Ollama",
            Model: "llama3.2:3b",
            Mode: "default",
            Temperature: 0.7,
            MaxTokens: 2048,
            TopP: 0.9,
            Frequencies: new[] { "432" },
            UseJoyfulEngine: false,
            BreathPhase: "expand",
            Description: "Default Llama 3.1 8B for general tasks on M1 Mac",
            Parameters: new Dictionary<string, object>
            {
                ["num_ctx"] = 8192,
                ["repeat_penalty"] = 1.1
            }
        );

        // Future Knowledge Configurations
        public static readonly LLMConfiguration FutureKnowledgeLlama3 = new(
            Id: "future-knowledge-llama3",
            Provider: "Ollama",
            Model: "llama3",
            Mode: "future-knowledge",
            Temperature: 0.9,
            MaxTokens: 2000,
            TopP: 0.95,
            Frequencies: new[] { "741" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Llama3 optimized for future knowledge retrieval and prediction",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["future_knowledge"] = true,
                ["temporal_awareness"] = true,
                ["prediction_optimized"] = true
            }
        );

        // Image Generation Configurations
        public static readonly LLMConfiguration ImageGenerationLlama3 = new(
            Id: "image-generation-llama3",
            Provider: "Ollama",
            Model: "llama3",
            Mode: "image-generation",
            Temperature: 0.8,
            MaxTokens: 1500,
            TopP: 0.9,
            Frequencies: new[] { "528" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Llama3 optimized for image generation prompts and descriptions",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["image_generation"] = true,
                ["visual_description"] = true,
                ["creative_prompts"] = true
            }
        );

        // Analysis Configurations
        public static readonly LLMConfiguration AnalysisLlama3 = new(
            Id: "analysis-llama3",
            Provider: "Ollama",
            Model: "llama3",
            Mode: "analysis",
            Temperature: 0.5,
            MaxTokens: 2000,
            TopP: 0.8,
            Frequencies: new[] { "741" },
            UseJoyfulEngine: false,
            BreathPhase: "validate",
            Description: "Llama3 optimized for analysis and validation",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["analysis"] = true,
                ["validation"] = true,
                ["structured_output"] = true
            }
        );

        // Resonance Calculation Configurations
        public static readonly LLMConfiguration ResonanceCalculationLlama3 = new(
            Id: "resonance-calculation-llama3",
            Provider: "Ollama",
            Model: "llama3",
            Mode: "resonance-calculation",
            Temperature: 0.6,
            MaxTokens: 1500,
            TopP: 0.85,
            Frequencies: new[] { "432", "528", "741" },
            UseJoyfulEngine: true,
            BreathPhase: "validate",
            Description: "Llama3 optimized for resonance field calculations and U-CORE alignment",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["resonance_calculation"] = true,
                ["ucore_alignment"] = true,
                ["frequency_optimization"] = true
            }
        );

        // Creative Configurations
        public static readonly LLMConfiguration CreativeLlama3 = new(
            Id: "creative-llama3",
            Provider: "Ollama",
            Model: "llama3",
            Mode: "creative",
            Temperature: 0.9,
            MaxTokens: 2000,
            TopP: 0.95,
            Frequencies: new[] { "528" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Llama3 optimized for creative content generation",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["creative"] = true,
                ["artistic"] = true,
                ["imaginative"] = true
            }
        );
    }

    /// <summary>
    /// Get optimal configuration for a specific use case
    /// </summary>
    public static LLMConfiguration GetOptimalConfiguration(string useCase, string? preferredModel = null)
    {
        var baseConfig = useCase.ToLowerInvariant() switch
        {
            "concept-extraction" => PredefinedConfigurations.ConceptExtractionLlama3,
            "consciousness-expansion" => PredefinedConfigurations.ConsciousnessExpansionGemma2,
            "translation" => PredefinedConfigurations.TranslationQwen2,
            "code-generation" => PredefinedConfigurations.CodeGenerationDeepseek,
            "creative-writing" => PredefinedConfigurations.CreativeWritingPhi3,
            "reasoning" => PredefinedConfigurations.ReasoningMistral,
            "analysis" => PredefinedConfigurations.ConsciousnessExpansionGemma2, // Use same as expansion for analysis
            _ => PredefinedConfigurations.DefaultLlama3
        };

        if (preferredModel != null)
        {
            baseConfig = baseConfig with { Model = preferredModel };
        }

        return baseConfig;
    }

    /// <summary>
    /// Get all available configurations
    /// </summary>
    public static List<LLMConfiguration> GetAllConfigurations()
    {
        return new List<LLMConfiguration>
        {
            PredefinedConfigurations.ConsciousnessExpansionGemma2,
            PredefinedConfigurations.ConceptExtractionLlama3,
            PredefinedConfigurations.CodeGenerationDeepseek,
            PredefinedConfigurations.CreativeWritingPhi3,
            PredefinedConfigurations.ReasoningMistral,
            PredefinedConfigurations.TranslationQwen2,
            PredefinedConfigurations.DefaultLlama3
        };
    }

    /// <summary>
    /// Get configurations by provider
    /// </summary>
    public static List<LLMConfiguration> GetConfigurationsByProvider(string provider)
    {
        return GetAllConfigurations().Where(c => c.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Get configurations by mode
    /// </summary>
    public static List<LLMConfiguration> GetConfigurationsByMode(string mode)
    {
        return GetAllConfigurations().Where(c => c.Mode.Equals(mode, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Get configurations by frequency
    /// </summary>
    public static List<LLMConfiguration> GetConfigurationsByFrequency(string frequency)
    {
        return GetAllConfigurations().Where(c => c.Frequencies.Contains(frequency)).ToList();
    }

    /// <summary>
    /// Get configurations by breath phase
    /// </summary>
    public static List<LLMConfiguration> GetConfigurationsByBreathPhase(string breathPhase)
    {
        return GetAllConfigurations().Where(c => c.BreathPhase.Equals(breathPhase, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
