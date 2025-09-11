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
      ""model"": ""llama3"",
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
            SupportedModels = supportedModels ?? new[] { "llama2", "llama3", "mistral", "codellama" };
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
            string model = "llama3",
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
          ""model"": ""llama3"",
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
    /// Predefined configurations for different use cases
    /// </summary>
    public static class PredefinedConfigurations
    {
        // Consciousness Expansion Configurations
        public static readonly LLMConfiguration ConsciousnessExpansionLlama3 = new(
            Id: "consciousness-expansion-gpt-oss-20b",
            Provider: "Ollama",
            Model: "gpt-oss:20b", // Using the better 20B model for Mac M1
            Mode: "consciousness-expansion",
            Temperature: 0.7, // Slightly lower for more focused responses
            MaxTokens: 2000,
            TopP: 0.9,
            Frequencies: new[] { "432", "528", "741" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "GPT-OSS 20B optimized for consciousness expansion with joyful engine on Mac M1",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["num_ctx"] = 4096, // Context length
                ["num_predict"] = 2000, // Max tokens to predict
                ["num_gpu"] = 1, // Use GPU on Mac M1
                ["num_thread"] = 8, // Optimize CPU threads for M1
                ["repeat_penalty"] = 1.1, // Prevent repetition
                ["top_k"] = 40, // Top-k sampling
                ["tfs_z"] = 1.0, // Tail free sampling
                ["consciousness_expansion"] = true,
                ["joyful_language"] = true,
                ["spiritual_resonance"] = true
            }
        );

        public static readonly LLMConfiguration ConsciousnessExpansionLlama2 = new(
            Id: "consciousness-expansion-llama2",
            Provider: "Ollama",
            Model: "llama2",
            Mode: "consciousness-expansion",
            Temperature: 0.7,
            MaxTokens: 1500,
            TopP: 0.85,
            Frequencies: new[] { "432", "528", "741" },
            UseJoyfulEngine: true,
            BreathPhase: "expand",
            Description: "Llama2 optimized for consciousness expansion with joyful engine",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["consciousness_expansion"] = true,
                ["joyful_language"] = true
            }
        );

        // Code Generation Configurations
        public static readonly LLMConfiguration CodeGenerationCodellama = new(
            Id: "code-generation-codellama",
            Provider: "Ollama",
            Model: "codellama",
            Mode: "code-generation",
            Temperature: 0.3,
            MaxTokens: 3000,
            TopP: 0.8,
            Frequencies: new[] { "741" },
            UseJoyfulEngine: false,
            BreathPhase: "contract",
            Description: "CodeLlama optimized for code generation and reflection",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["code_generation"] = true,
                ["reflection_support"] = true,
                ["csharp_optimized"] = true
            }
        );

        public static readonly LLMConfiguration CodeGenerationMistral = new(
            Id: "code-generation-mistral",
            Provider: "Ollama",
            Model: "mistral",
            Mode: "code-generation",
            Temperature: 0.4,
            MaxTokens: 2500,
            TopP: 0.85,
            Frequencies: new[] { "741" },
            UseJoyfulEngine: false,
            BreathPhase: "contract",
            Description: "Mistral optimized for code generation and analysis",
            Parameters: new Dictionary<string, object>
            {
                ["format"] = "json",
                ["stream"] = false,
                ["code_generation"] = true,
                ["analysis_support"] = true
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
        return useCase.ToLowerInvariant() switch
        {
            "consciousness-expansion" => preferredModel?.ToLowerInvariant() switch
            {
                "llama2" => PredefinedConfigurations.ConsciousnessExpansionLlama2,
                _ => PredefinedConfigurations.ConsciousnessExpansionLlama3
            },
            "code-generation" => preferredModel?.ToLowerInvariant() switch
            {
                "mistral" => PredefinedConfigurations.CodeGenerationMistral,
                _ => PredefinedConfigurations.CodeGenerationCodellama
            },
            "future-knowledge" => PredefinedConfigurations.FutureKnowledgeLlama3,
            "image-generation" => PredefinedConfigurations.ImageGenerationLlama3,
            "analysis" => PredefinedConfigurations.AnalysisLlama3,
            "resonance-calculation" => PredefinedConfigurations.ResonanceCalculationLlama3,
            "creative" => PredefinedConfigurations.CreativeLlama3,
            _ => PredefinedConfigurations.ConsciousnessExpansionLlama3
        };
    }

    /// <summary>
    /// Get all available configurations
    /// </summary>
    public static List<LLMConfiguration> GetAllConfigurations()
    {
        return new List<LLMConfiguration>
        {
            PredefinedConfigurations.ConsciousnessExpansionLlama3,
            PredefinedConfigurations.ConsciousnessExpansionLlama2,
            PredefinedConfigurations.CodeGenerationCodellama,
            PredefinedConfigurations.CodeGenerationMistral,
            PredefinedConfigurations.FutureKnowledgeLlama3,
            PredefinedConfigurations.ImageGenerationLlama3,
            PredefinedConfigurations.AnalysisLlama3,
            PredefinedConfigurations.ResonanceCalculationLlama3,
            PredefinedConfigurations.CreativeLlama3
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
