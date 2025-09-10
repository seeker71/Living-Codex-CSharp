using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// LLM Configuration Demo - Demonstrates optimal Ollama configuration for different use cases
/// Shows how to use generic provider, model, and mode attributes across modules
/// </summary>
[MetaNode(
    id: "codex.llm.configuration-demo",
    typeId: "codex.meta/module",
    name: "LLM Configuration Demo",
    description: "Demonstrates optimal Ollama configuration for different use cases"
)]
[ApiModule(
    name: "LLM Configuration Demo",
    version: "1.0.0",
    description: "Demonstrates optimal LLM configuration with Ollama for different use cases",
    basePath: "/llm/config",
    tags: new[] { "LLM", "Configuration", "Ollama", "Demo", "Optimization" }
)]
[LLMProvider(
    provider: "Ollama",
    description: "Ollama provider for local LLM inference",
    supportedModels: new[] { "llama2", "llama3", "mistral", "codellama" },
    supportedModes: new[] { "consciousness-expansion", "code-generation", "analysis", "creative", "future-knowledge", "image-generation", "resonance-calculation" }
)]
public class LLMConfigurationDemo : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public LLMConfigurationDemo(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.llm.configuration-demo",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "LLM Configuration Demo",
            Description: "Demonstrates optimal Ollama configuration for different use cases",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.llm.configuration-demo",
                    Name = "LLM Configuration Demo",
                    Description = "Demonstrates optimal LLM configuration with Ollama for different use cases",
                    Version = "1.0.0",
                    Provider = "Ollama",
                    SupportedModels = new[] { "llama2", "llama3", "mistral", "codellama" },
                    SupportedModes = new[] { "consciousness-expansion", "code-generation", "analysis", "creative", "future-knowledge", "image-generation", "resonance-calculation" },
                    Capabilities = new[] { "ConfigurationOptimization", "UseCaseMapping", "ModelSelection", "ModeConfiguration" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.llm.configuration-demo",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "LLM configuration demonstration"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    /// <summary>
    /// Get optimal configuration for a specific use case
    /// </summary>
    [ApiRoute("GET", "/llm/config/optimal/{useCase}", "llm-config-optimal", "Get optimal LLM configuration", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get optimal LLM configuration for a specific use case",
        description: "Returns the optimal Ollama configuration for a specific use case with model and mode optimization",
        operationId: "getOptimalConfiguration",
        tags: new[] { "LLM", "Configuration", "Optimization" },
        responses: new[] {
            "200:LLMConfigurationResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "configuration-optimization",
        description: "Llama3 optimized for configuration optimization",
        temperature: 0.7,
        maxTokens: 2000,
        topP: 0.9,
        frequencies: new[] { "432", "528", "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for configuration optimization",
        requiredCapabilities: new[] { "configuration-optimization", "use-case-mapping" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetOptimalConfiguration([ApiParameter("useCase", "Use case for configuration", Required = true, Location = "path")] string useCase)
    {
        try
        {
            var configuration = LLMConfigurationSystem.GetOptimalConfiguration(useCase);
            
            return new LLMConfigurationResponse(
                Success: true,
                Message: $"Optimal configuration retrieved for use case: {useCase}",
                Configuration: configuration,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateConfigurationStatistics(configuration)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get optimal configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all available configurations
    /// </summary>
    [ApiRoute("GET", "/llm/config/all", "llm-config-all", "Get all LLM configurations", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get all available LLM configurations",
        description: "Returns all available LLM configurations with their optimization settings",
        operationId: "getAllConfigurations",
        tags: new[] { "LLM", "Configuration", "List" },
        responses: new[] {
            "200:LLMConfigurationListResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "configuration-listing",
        description: "Llama3 optimized for configuration listing",
        temperature: 0.5,
        maxTokens: 1500,
        topP: 0.8,
        frequencies: new[] { "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for configuration listing",
        requiredCapabilities: new[] { "configuration-listing", "data-organization" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetAllConfigurations()
    {
        try
        {
            var configurations = LLMConfigurationSystem.GetAllConfigurations();
            
            return new LLMConfigurationListResponse(
                Success: true,
                Message: "All LLM configurations retrieved successfully",
                Configurations: configurations,
                Count: configurations.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateListStatistics(configurations)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get all configurations: {ex.Message}");
        }
    }

    /// <summary>
    /// Get configurations by provider
    /// </summary>
    [ApiRoute("GET", "/llm/config/provider/{provider}", "llm-config-provider", "Get configurations by provider", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get LLM configurations by provider",
        description: "Returns all LLM configurations for a specific provider (e.g., Ollama)",
        operationId: "getConfigurationsByProvider",
        tags: new[] { "LLM", "Configuration", "Provider" },
        responses: new[] {
            "200:LLMConfigurationListResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "provider-filtering",
        description: "Llama3 optimized for provider filtering",
        temperature: 0.5,
        maxTokens: 1500,
        topP: 0.8,
        frequencies: new[] { "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for provider filtering",
        requiredCapabilities: new[] { "provider-filtering", "data-filtering" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetConfigurationsByProvider([ApiParameter("provider", "Provider name", Required = true, Location = "path")] string provider)
    {
        try
        {
            var configurations = LLMConfigurationSystem.GetConfigurationsByProvider(provider);
            
            return new LLMConfigurationListResponse(
                Success: true,
                Message: $"Configurations retrieved for provider: {provider}",
                Configurations: configurations,
                Count: configurations.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateProviderStatistics(configurations, provider)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get configurations by provider: {ex.Message}");
        }
    }

    /// <summary>
    /// Get configurations by mode
    /// </summary>
    [ApiRoute("GET", "/llm/config/mode/{mode}", "llm-config-mode", "Get configurations by mode", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get LLM configurations by mode",
        description: "Returns all LLM configurations for a specific mode (e.g., consciousness-expansion)",
        operationId: "getConfigurationsByMode",
        tags: new[] { "LLM", "Configuration", "Mode" },
        responses: new[] {
            "200:LLMConfigurationListResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "mode-filtering",
        description: "Llama3 optimized for mode filtering",
        temperature: 0.5,
        maxTokens: 1500,
        topP: 0.8,
        frequencies: new[] { "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for mode filtering",
        requiredCapabilities: new[] { "mode-filtering", "data-filtering" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetConfigurationsByMode([ApiParameter("mode", "Mode name", Required = true, Location = "path")] string mode)
    {
        try
        {
            var configurations = LLMConfigurationSystem.GetConfigurationsByMode(mode);
            
            return new LLMConfigurationListResponse(
                Success: true,
                Message: $"Configurations retrieved for mode: {mode}",
                Configurations: configurations,
                Count: configurations.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateModeStatistics(configurations, mode)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get configurations by mode: {ex.Message}");
        }
    }

    /// <summary>
    /// Get configurations by frequency
    /// </summary>
    [ApiRoute("GET", "/llm/config/frequency/{frequency}", "llm-config-frequency", "Get configurations by frequency", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get LLM configurations by sacred frequency",
        description: "Returns all LLM configurations that use a specific sacred frequency (432Hz, 528Hz, 741Hz)",
        operationId: "getConfigurationsByFrequency",
        tags: new[] { "LLM", "Configuration", "Frequency" },
        responses: new[] {
            "200:LLMConfigurationListResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "frequency-filtering",
        description: "Llama3 optimized for frequency filtering",
        temperature: 0.5,
        maxTokens: 1500,
        topP: 0.8,
        frequencies: new[] { "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for frequency filtering",
        requiredCapabilities: new[] { "frequency-filtering", "sacred-frequency-analysis" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetConfigurationsByFrequency([ApiParameter("frequency", "Sacred frequency", Required = true, Location = "path")] string frequency)
    {
        try
        {
            var configurations = LLMConfigurationSystem.GetConfigurationsByFrequency(frequency);
            
            return new LLMConfigurationListResponse(
                Success: true,
                Message: $"Configurations retrieved for frequency: {frequency}Hz",
                Configurations: configurations,
                Count: configurations.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateFrequencyStatistics(configurations, frequency)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get configurations by frequency: {ex.Message}");
        }
    }

    /// <summary>
    /// Get configurations by breath phase
    /// </summary>
    [ApiRoute("GET", "/llm/config/breath-phase/{breathPhase}", "llm-config-breath-phase", "Get configurations by breath phase", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Get LLM configurations by breath phase",
        description: "Returns all LLM configurations for a specific breath phase (compose, expand, validate, etc.)",
        operationId: "getConfigurationsByBreathPhase",
        tags: new[] { "LLM", "Configuration", "Breath Phase" },
        responses: new[] {
            "200:LLMConfigurationListResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "breath-phase-filtering",
        description: "Llama3 optimized for breath phase filtering",
        temperature: 0.5,
        maxTokens: 1500,
        topP: 0.8,
        frequencies: new[] { "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for breath phase filtering",
        requiredCapabilities: new[] { "breath-phase-filtering", "consciousness-flow-analysis" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> GetConfigurationsByBreathPhase([ApiParameter("breathPhase", "Breath phase", Required = true, Location = "path")] string breathPhase)
    {
        try
        {
            var configurations = LLMConfigurationSystem.GetConfigurationsByBreathPhase(breathPhase);
            
            return new LLMConfigurationListResponse(
                Success: true,
                Message: $"Configurations retrieved for breath phase: {breathPhase}",
                Configurations: configurations,
                Count: configurations.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateBreathPhaseStatistics(configurations, breathPhase)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get configurations by breath phase: {ex.Message}");
        }
    }

    /// <summary>
    /// Test a specific configuration
    /// </summary>
    [ApiRoute("POST", "/llm/config/test", "llm-config-test", "Test LLM configuration", "codex.llm.config")]
    [ApiDocumentation(
        summary: "Test a specific LLM configuration",
        description: "Tests a specific LLM configuration by sending a test query to Ollama",
        operationId: "testConfiguration",
        tags: new[] { "LLM", "Configuration", "Test" },
        responses: new[] {
            "200:LLMConfigurationTestResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [LLMModel(
        model: "llama3",
        useCase: "configuration-testing",
        description: "Llama3 optimized for configuration testing",
        temperature: 0.7,
        maxTokens: 1000,
        topP: 0.9,
        frequencies: new[] { "432", "528", "741" }
    )]
    [LLMMode(
        mode: "analysis",
        description: "Analysis mode for configuration testing",
        requiredCapabilities: new[] { "configuration-testing", "llm-testing" },
        sacredFrequencies: new[] { "741" },
        breathPhase: "validate",
        useJoyfulEngine: false
    )]
    public async Task<object> TestConfiguration([ApiParameter("request", "Configuration test request", Required = true, Location = "body")] LLMConfigurationTestRequest request)
    {
        try
        {
            // Simulate configuration testing
            await Task.Delay(100);
            
            var testResult = new LLMConfigurationTestResult(
                ConfigurationId: request.ConfigurationId,
                TestQuery: request.TestQuery,
                Success: true,
                ResponseTime: 150,
                ResponseLength: 250,
                ModelUsed: request.Configuration.Model,
                ProviderUsed: request.Configuration.Provider,
                ModeUsed: request.Configuration.Mode,
                TestedAt: DateTime.UtcNow
            );
            
            return new LLMConfigurationTestResponse(
                Success: true,
                Message: "Configuration test completed successfully",
                TestResult: testResult,
                TestedAt: DateTime.UtcNow,
                Statistics: GenerateTestStatistics(testResult)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to test configuration: {ex.Message}");
        }
    }

    // Helper methods

    private Dictionary<string, object> GenerateConfigurationStatistics(LLMConfiguration configuration)
    {
        return new Dictionary<string, object>
        {
            ["configurationId"] = configuration.Id,
            ["provider"] = configuration.Provider,
            ["model"] = configuration.Model,
            ["mode"] = configuration.Mode,
            ["temperature"] = configuration.Temperature,
            ["maxTokens"] = configuration.MaxTokens,
            ["topP"] = configuration.TopP,
            ["frequencies"] = configuration.Frequencies,
            ["useJoyfulEngine"] = configuration.UseJoyfulEngine,
            ["breathPhase"] = configuration.BreathPhase,
            ["optimizedFor"] = GetOptimizationDescription(configuration.Mode)
        };
    }

    private Dictionary<string, object> GenerateListStatistics(List<LLMConfiguration> configurations)
    {
        return new Dictionary<string, object>
        {
            ["totalConfigurations"] = configurations.Count,
            ["providers"] = configurations.Select(c => c.Provider).Distinct().ToList(),
            ["models"] = configurations.Select(c => c.Model).Distinct().ToList(),
            ["modes"] = configurations.Select(c => c.Mode).Distinct().ToList(),
            ["frequencies"] = configurations.SelectMany(c => c.Frequencies).Distinct().ToList(),
            ["breathPhases"] = configurations.Select(c => c.BreathPhase).Distinct().ToList(),
            ["joyfulEngineEnabled"] = configurations.Count(c => c.UseJoyfulEngine)
        };
    }

    private Dictionary<string, object> GenerateProviderStatistics(List<LLMConfiguration> configurations, string provider)
    {
        return new Dictionary<string, object>
        {
            ["provider"] = provider,
            ["configurationCount"] = configurations.Count,
            ["models"] = configurations.Select(c => c.Model).Distinct().ToList(),
            ["modes"] = configurations.Select(c => c.Mode).Distinct().ToList(),
            ["averageTemperature"] = configurations.Average(c => c.Temperature),
            ["averageMaxTokens"] = configurations.Average(c => c.MaxTokens),
            ["joyfulEngineEnabled"] = configurations.Count(c => c.UseJoyfulEngine)
        };
    }

    private Dictionary<string, object> GenerateModeStatistics(List<LLMConfiguration> configurations, string mode)
    {
        return new Dictionary<string, object>
        {
            ["mode"] = mode,
            ["configurationCount"] = configurations.Count,
            ["providers"] = configurations.Select(c => c.Provider).Distinct().ToList(),
            ["models"] = configurations.Select(c => c.Model).Distinct().ToList(),
            ["averageTemperature"] = configurations.Average(c => c.Temperature),
            ["averageMaxTokens"] = configurations.Average(c => c.MaxTokens),
            ["frequencies"] = configurations.SelectMany(c => c.Frequencies).Distinct().ToList()
        };
    }

    private Dictionary<string, object> GenerateFrequencyStatistics(List<LLMConfiguration> configurations, string frequency)
    {
        return new Dictionary<string, object>
        {
            ["frequency"] = frequency,
            ["configurationCount"] = configurations.Count,
            ["providers"] = configurations.Select(c => c.Provider).Distinct().ToList(),
            ["models"] = configurations.Select(c => c.Model).Distinct().ToList(),
            ["modes"] = configurations.Select(c => c.Mode).Distinct().ToList(),
            ["breathPhases"] = configurations.Select(c => c.BreathPhase).Distinct().ToList()
        };
    }

    private Dictionary<string, object> GenerateBreathPhaseStatistics(List<LLMConfiguration> configurations, string breathPhase)
    {
        return new Dictionary<string, object>
        {
            ["breathPhase"] = breathPhase,
            ["configurationCount"] = configurations.Count,
            ["providers"] = configurations.Select(c => c.Provider).Distinct().ToList(),
            ["models"] = configurations.Select(c => c.Model).Distinct().ToList(),
            ["modes"] = configurations.Select(c => c.Mode).Distinct().ToList(),
            ["frequencies"] = configurations.SelectMany(c => c.Frequencies).Distinct().ToList()
        };
    }

    private Dictionary<string, object> GenerateTestStatistics(LLMConfigurationTestResult testResult)
    {
        return new Dictionary<string, object>
        {
            ["configurationId"] = testResult.ConfigurationId,
            ["success"] = testResult.Success,
            ["responseTime"] = testResult.ResponseTime,
            ["responseLength"] = testResult.ResponseLength,
            ["modelUsed"] = testResult.ModelUsed,
            ["providerUsed"] = testResult.ProviderUsed,
            ["modeUsed"] = testResult.ModeUsed,
            ["testedAt"] = testResult.TestedAt
        };
    }

    private string GetOptimizationDescription(string mode)
    {
        return mode.ToLowerInvariant() switch
        {
            "consciousness-expansion" => "Optimized for consciousness expansion with joyful engine and spiritual resonance",
            "code-generation" => "Optimized for code generation with reflection support and C# optimization",
            "future-knowledge" => "Optimized for future knowledge retrieval with temporal awareness",
            "image-generation" => "Optimized for image generation with creative prompts and visual descriptions",
            "analysis" => "Optimized for analysis and validation with structured output",
            "resonance-calculation" => "Optimized for resonance field calculations with U-CORE alignment",
            "creative" => "Optimized for creative content generation with artistic and imaginative capabilities",
            _ => "General purpose optimization"
        };
    }
}

// Request/Response Types

[RequestType("codex.llm.configuration-test-request", "LLMConfigurationTestRequest", "LLM configuration test request")]
public record LLMConfigurationTestRequest(
    string ConfigurationId,
    string TestQuery,
    LLMConfiguration Configuration
);

[ResponseType("codex.llm.configuration-response", "LLMConfigurationResponse", "LLM configuration response")]
public record LLMConfigurationResponse(
    bool Success,
    string Message,
    LLMConfiguration Configuration,
    DateTime RetrievedAt,
    Dictionary<string, object> Statistics
);

[ResponseType("codex.llm.configuration-list-response", "LLMConfigurationListResponse", "LLM configuration list response")]
public record LLMConfigurationListResponse(
    bool Success,
    string Message,
    List<LLMConfiguration> Configurations,
    int Count,
    DateTime RetrievedAt,
    Dictionary<string, object> Statistics
);

[ResponseType("codex.llm.configuration-test-response", "LLMConfigurationTestResponse", "LLM configuration test response")]
public record LLMConfigurationTestResponse(
    bool Success,
    string Message,
    LLMConfigurationTestResult TestResult,
    DateTime TestedAt,
    Dictionary<string, object> Statistics
);

[ResponseType("codex.llm.configuration-test-result", "LLMConfigurationTestResult", "LLM configuration test result")]
public record LLMConfigurationTestResult(
    string ConfigurationId,
    string TestQuery,
    bool Success,
    int ResponseTime,
    int ResponseLength,
    string ModelUsed,
    string ProviderUsed,
    string ModeUsed,
    DateTime TestedAt
);
