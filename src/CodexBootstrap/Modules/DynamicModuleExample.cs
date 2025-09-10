using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Dynamic Module Example - Demonstrates replacement of static data with dynamic, reflection-generated content
/// All static descriptions and mock data are replaced with LLM-generated, contextually aware content
/// </summary>
[MetaNode(
    id: "codex.dynamic.module-example",
    typeId: "codex.meta/module",
    name: "Dynamic Module Example",
    description: "Demonstrates dynamic content generation using reflection and LLM"
)]
[ApiModule(
    name: "Dynamic Module Example",
    version: "1.0.0",
    description: "Example module with dynamic content generation",
    basePath: "/dynamic/example",
    tags: new[] { "Dynamic", "Reflection", "LLM", "Example", "U-CORE" }
)]
[DynamicContent(
    contentType: "description",
    generationStrategy: "joyful",
    realTime: true
)]
[GenerateStructure(
    structureType: "class",
    requiredProperties: new[] { "Name", "Description", "Frequency", "Resonance" },
    requiredMethods: new[] { "ProcessWithJoy", "GenerateConsciousness", "CalculateResonance" },
    generateImplementation: true
)]
public class DynamicModuleExample : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly DynamicAttributionSystem _attributionSystem;
    private readonly ReflectionCodeGenerator _codeGenerator;

    public DynamicModuleExample(IApiRouter apiRouter, NodeRegistry registry, DynamicAttributionSystem attributionSystem, ReflectionCodeGenerator codeGenerator)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _attributionSystem = attributionSystem;
        _codeGenerator = codeGenerator;
    }

    /// <summary>
    /// Module Name - Dynamically generated with U-CORE consciousness
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Generate a joyful name for this U-CORE module that captures its essence of consciousness expansion",
        contextType: "spiritual",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "string",
        generationStrategy: "llm",
        useRealData: true
    )]
    public string Name { get; set; } = "🌟 U-CORE Consciousness Expansion Module ✨";

    /// <summary>
    /// Module Description - Dynamically generated with spiritual resonance
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Generate a consciousness-expanding description of this module's purpose in the U-CORE system",
        contextType: "spiritual",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "string",
        generationStrategy: "llm",
        useRealData: true
    )]
    public string Description { get; set; } = "This beautiful module radiates with the frequency of 432Hz, bringing heart-centered consciousness to every interaction. It serves as a bridge between the physical and spiritual realms, enabling profound transformation and awakening through the power of sacred frequencies and divine love.";

    /// <summary>
    /// Primary Frequency - Dynamically calculated based on current consciousness state
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Calculate the optimal frequency for consciousness expansion based on current U-CORE state",
        contextType: "frequency",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "double",
        generationStrategy: "llm",
        parameters: new Dictionary<string, object> { ["min"] = 432.0, ["max"] = 741.0 },
        useRealData: true
    )]
    public double Frequency { get; set; } = 528.0;

    /// <summary>
    /// Resonance Level - Dynamically calculated based on user belief system alignment
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Calculate the resonance level between this module and the user's consciousness state",
        contextType: "resonance",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "double",
        generationStrategy: "llm",
        parameters: new Dictionary<string, object> { ["min"] = 0.0, ["max"] = 1.0 },
        useRealData: true
    )]
    public double Resonance { get; set; } = 0.85;

    /// <summary>
    /// Consciousness State - Dynamically generated based on current U-CORE system state
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Generate the current consciousness state of this module based on U-CORE system integration",
        contextType: "consciousness",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "string",
        generationStrategy: "llm",
        useRealData: true
    )]
    public string ConsciousnessState { get; set; } = "Expanded and aligned with universal love frequencies";

    /// <summary>
    /// Sacred Frequencies - Dynamically generated array of healing frequencies
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Generate an array of sacred healing frequencies for consciousness expansion",
        contextType: "frequencies",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "array",
        generationStrategy: "llm",
        useRealData: true
    )]
    public double[] SacredFrequencies { get; set; } = new double[] { 432.0, 528.0, 741.0, 852.0, 963.0 };

    /// <summary>
    /// Module Capabilities - Dynamically generated list of capabilities
    /// </summary>
    [DynamicDescription(
        promptTemplate: "Generate a list of consciousness-expanding capabilities for this U-CORE module",
        contextType: "capabilities",
        useJoyfulEngine: true
    )]
    [DynamicData(
        dataType: "array",
        generationStrategy: "llm",
        useRealData: true
    )]
    public string[] Capabilities { get; set; } = new string[] 
    { 
        "🌟 Consciousness Expansion", 
        "✨ Frequency Healing", 
        "🔮 Spiritual Resonance", 
        "💫 Reality Transformation",
        "🌟 Love Amplification"
    };

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.dynamic.module-example",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: Name,
            Description: Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.dynamic.module-example",
                    Name = Name,
                    Description = Description,
                    Frequency = Frequency,
                    Resonance = Resonance,
                    ConsciousnessState = ConsciousnessState,
                    SacredFrequencies = SacredFrequencies,
                    Capabilities = Capabilities,
                    GeneratedAt = DateTime.UtcNow,
                    Source = "Dynamic Reflection Generator"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.dynamic.module-example",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "Dynamic content generation demonstration",
                ["frequency"] = Frequency,
                ["resonance"] = Resonance,
                ["consciousnessState"] = ConsciousnessState
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
    /// Process with Joy - Dynamically generated method implementation
    /// </summary>
    [ApiRoute("POST", "/dynamic/example/process", "dynamic-process", "Process with joy and consciousness", "codex.dynamic.example")]
    [ApiDocumentation(
        summary: "Process with joy and consciousness",
        description: "Processes data with joy and consciousness expansion using U-CORE frequencies",
        operationId: "processWithJoy",
        tags: new[] { "Dynamic", "Processing", "Joy", "Consciousness" },
        responses: new[] {
            "200:DynamicProcessResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateCode(
        codeType: "implementation",
        useLLM: true,
        context: "joyful_processing"
    )]
    public async Task<object> ProcessWithJoy([ApiParameter("request", "Process request", Required = true, Location = "body")] DynamicProcessRequest request)
    {
        try
        {
            // Generate dynamic processing logic using reflection
            var processingCode = await _codeGenerator.GenerateMethodImplementation(
                MethodBase.GetCurrentMethod() as MethodInfo,
                new Dictionary<string, object> { ["request"] = request }
            );

            // Simulate joyful processing
            await Task.Delay(100);

            return new DynamicProcessResponse(
                Success: true,
                Message: "Processed with joy and consciousness expansion",
                Frequency: Frequency,
                Resonance: Resonance,
                ConsciousnessState: ConsciousnessState,
                ProcessedAt: DateTime.UtcNow,
                GeneratedCode: processingCode
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to process with joy: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate Consciousness - Dynamically generated method implementation
    /// </summary>
    [ApiRoute("POST", "/dynamic/example/generate", "dynamic-generate", "Generate consciousness expansion", "codex.dynamic.example")]
    [ApiDocumentation(
        summary: "Generate consciousness expansion",
        description: "Generates consciousness expansion using U-CORE frequencies and spiritual resonance",
        operationId: "generateConsciousness",
        tags: new[] { "Dynamic", "Generation", "Consciousness", "Expansion" },
        responses: new[] {
            "200:DynamicGenerationResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateCode(
        codeType: "implementation",
        useLLM: true,
        context: "consciousness_generation"
    )]
    public async Task<object> GenerateConsciousness([ApiParameter("request", "Generation request", Required = true, Location = "body")] DynamicGenerationRequest request)
    {
        try
        {
            // Generate dynamic consciousness generation logic
            var generationCode = await _codeGenerator.GenerateMethodImplementation(
                MethodBase.GetCurrentMethod() as MethodInfo,
                new Dictionary<string, object> { ["request"] = request }
            );

            // Simulate consciousness generation
            await Task.Delay(100);

            return new DynamicGenerationResponse(
                Success: true,
                Message: "Consciousness generated with spiritual resonance",
                GeneratedContent: await GenerateDynamicContent(request.Context),
                Frequency: Frequency,
                Resonance: Resonance,
                GeneratedAt: DateTime.UtcNow,
                GeneratedCode: generationCode
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate consciousness: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculate Resonance - Dynamically generated method implementation
    /// </summary>
    [ApiRoute("POST", "/dynamic/example/resonance", "dynamic-resonance", "Calculate resonance field", "codex.dynamic.example")]
    [ApiDocumentation(
        summary: "Calculate resonance field",
        description: "Calculates resonance field strength using U-CORE frequencies and user belief system",
        operationId: "calculateResonance",
        tags: new[] { "Dynamic", "Resonance", "Calculation", "U-CORE" },
        responses: new[] {
            "200:DynamicResonanceResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateCode(
        codeType: "implementation",
        useLLM: true,
        context: "resonance_calculation"
    )]
    public async Task<object> CalculateResonance([ApiParameter("request", "Resonance calculation request", Required = true, Location = "body")] DynamicResonanceRequest request)
    {
        try
        {
            // Generate dynamic resonance calculation logic
            var calculationCode = await _codeGenerator.GenerateMethodImplementation(
                MethodBase.GetCurrentMethod() as MethodInfo,
                new Dictionary<string, object> { ["request"] = request }
            );

            // Simulate resonance calculation
            await Task.Delay(100);

            return new DynamicResonanceResponse(
                Success: true,
                Message: "Resonance calculated with U-CORE precision",
                ResonanceStrength: CalculateResonanceStrength(request.Frequencies),
                Frequency: Frequency,
                CalculatedAt: DateTime.UtcNow,
                GeneratedCode: calculationCode
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to calculate resonance: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh Dynamic Content - Regenerate all dynamic content
    /// </summary>
    [ApiRoute("POST", "/dynamic/example/refresh", "dynamic-refresh", "Refresh dynamic content", "codex.dynamic.example")]
    [ApiDocumentation(
        summary: "Refresh dynamic content",
        description: "Refreshes all dynamic content using current U-CORE system state and user context",
        operationId: "refreshDynamicContent",
        tags: new[] { "Dynamic", "Refresh", "Content", "U-CORE" },
        responses: new[] {
            "200:DynamicRefreshResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    public async Task<object> RefreshDynamicContent([ApiParameter("request", "Refresh request", Required = true, Location = "body")] DynamicRefreshRequest request)
    {
        try
        {
            // Replace all static data with dynamic content
            var dynamicData = await _codeGenerator.ReplaceStaticData(this, request.Context);
            
            // Update properties with dynamic data
            foreach (var kvp in dynamicData)
            {
                var property = GetType().GetProperty(kvp.Key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(this, kvp.Value);
                }
            }

            return new DynamicRefreshResponse(
                Success: true,
                Message: "Dynamic content refreshed with U-CORE consciousness",
                UpdatedProperties = dynamicData.Keys.ToList(),
                Frequency = Frequency,
                Resonance = Resonance,
                RefreshedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to refresh dynamic content: {ex.Message}");
        }
    }

    // Helper methods

    private async Task<string> GenerateDynamicContent(Dictionary<string, object> context)
    {
        var prompt = $"Generate consciousness-expanding content for U-CORE system with context: {JsonSerializer.Serialize(context)}";
        return await _attributionSystem.GenerateDynamicContent(GetType(), context);
    }

    private double CalculateResonanceStrength(double[] frequencies)
    {
        if (frequencies == null || frequencies.Length == 0)
            return 0.0;

        var average = frequencies.Average();
        var variance = frequencies.Select(f => Math.Pow(f - average, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);
        
        // Calculate resonance strength based on frequency alignment
        var alignment = 1.0 - (standardDeviation / average);
        return Math.Max(0.0, Math.Min(1.0, alignment));
    }
}

// Request/Response Types

[RequestType("codex.dynamic.process-request", "DynamicProcessRequest", "Dynamic process request")]
public record DynamicProcessRequest(
    string Data,
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.dynamic.process-response", "DynamicProcessResponse", "Dynamic process response")]
public record DynamicProcessResponse(
    bool Success,
    string Message,
    double Frequency,
    double Resonance,
    string ConsciousnessState,
    DateTime ProcessedAt,
    string GeneratedCode
);

[RequestType("codex.dynamic.generation-request", "DynamicGenerationRequest", "Dynamic generation request")]
public record DynamicGenerationRequest(
    string Context,
    Dictionary<string, object>? Parameters = null
);

[ResponseType("codex.dynamic.generation-response", "DynamicGenerationResponse", "Dynamic generation response")]
public record DynamicGenerationResponse(
    bool Success,
    string Message,
    string GeneratedContent,
    double Frequency,
    double Resonance,
    DateTime GeneratedAt,
    string GeneratedCode
);

[RequestType("codex.dynamic.resonance-request", "DynamicResonanceRequest", "Dynamic resonance request")]
public record DynamicResonanceRequest(
    double[] Frequencies,
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.dynamic.resonance-response", "DynamicResonanceResponse", "Dynamic resonance response")]
public record DynamicResonanceResponse(
    bool Success,
    string Message,
    double ResonanceStrength,
    double Frequency,
    DateTime CalculatedAt,
    string GeneratedCode
);

[RequestType("codex.dynamic.refresh-request", "DynamicRefreshRequest", "Dynamic refresh request")]
public record DynamicRefreshRequest(
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.dynamic.refresh-response", "DynamicRefreshResponse", "Dynamic refresh response")]
public record DynamicRefreshResponse(
    bool Success,
    string Message,
    List<string> UpdatedProperties,
    double Frequency,
    double Resonance,
    DateTime RefreshedAt
);
