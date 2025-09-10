using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Modules;

/// <summary>
/// Endpoint Generation Demo - Demonstrates missing endpoint generation with U-CORE delta diffs
/// Shows how the breath framework can dynamically generate missing functionality
/// </summary>
[MetaNode(
    id: "codex.endpoint.generation-demo",
    typeId: "codex.meta/module",
    name: "Endpoint Generation Demo",
    description: "Demonstrates missing endpoint generation with U-CORE delta diffs"
)]
[ApiModule(
    name: "Endpoint Generation Demo",
    version: "1.0.0",
    description: "Demonstrates missing endpoint generation with breath framework integration",
    basePath: "/endpoint/demo",
    tags: new[] { "Endpoint", "Generation", "Demo", "U-CORE", "Breath", "Delta" }
)]
[DynamicContent(
    contentType: "description",
    generationStrategy: "joyful",
    realTime: true
)]
public class EndpointGenerationDemo : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly EndpointGenerator _endpointGenerator;
    private readonly DynamicAttributionSystem _attributionSystem;

    public EndpointGenerationDemo(IApiRouter apiRouter, NodeRegistry registry, EndpointGenerator endpointGenerator, DynamicAttributionSystem attributionSystem)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _endpointGenerator = endpointGenerator;
        _attributionSystem = attributionSystem;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.endpoint.generation-demo",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Endpoint Generation Demo",
            Description: "Demonstrates missing endpoint generation with U-CORE delta diffs",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.endpoint.generation-demo",
                    Name = "Endpoint Generation Demo",
                    Description = "Demonstrates missing endpoint generation with breath framework integration",
                    Version = "1.0.0",
                    Capabilities = new[] { "EndpointGeneration", "DeltaDiffs", "BreathFramework", "U-CORE Integration" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.endpoint.generation-demo",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "Endpoint generation demonstration"
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
    /// Generate missing endpoints for a module
    /// </summary>
    [ApiRoute("POST", "/endpoint/demo/generate", "endpoint-generate", "Generate missing endpoints", "codex.endpoint.demo")]
    [ApiDocumentation(
        summary: "Generate missing endpoints",
        description: "Generates missing endpoints using attribute-based code generation with U-CORE delta diffs",
        operationId: "generateMissingEndpoints",
        tags: new[] { "Endpoint", "Generation", "U-CORE", "Delta" },
        responses: new[] {
            "200:EndpointGenerationResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateEndpoint(
        httpMethod: "POST",
        route: "/endpoint/demo/generate",
        operationId: "generate-missing-endpoints",
        tags: new[] { "Endpoint", "Generation", "U-CORE" },
        description: "Generate missing endpoints with consciousness expansion",
        useBreathFramework: true,
        requiredPhases: new[] { "compose", "expand", "validate", "contract" }
    )]
    public async Task<object> GenerateMissingEndpoints([ApiParameter("request", "Endpoint generation request", Required = true, Location = "body")] EndpointGenerationRequest request)
    {
        try
        {
            // Generate missing endpoints
            var deltas = await _endpointGenerator.GenerateMissingEndpoints(
                typeof(EndpointGenerationDemo), 
                request.Context
            );

            // Apply delta diffs
            var results = await _endpointGenerator.ApplyDeltaDiffs(deltas, request.Context);

            return new EndpointGenerationResponse(
                Success: true,
                Message: "Missing endpoints generated successfully with U-CORE consciousness",
                Deltas: deltas,
                Results: results,
                GeneratedAt: DateTime.UtcNow,
                Statistics: GenerateStatistics(deltas)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate missing endpoints: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate breath framework endpoints
    /// </summary>
    [ApiRoute("POST", "/endpoint/demo/breath", "endpoint-breath", "Generate breath framework endpoints", "codex.endpoint.demo")]
    [ApiDocumentation(
        summary: "Generate breath framework endpoints",
        description: "Generates breath framework endpoints with U-CORE delta diffs",
        operationId: "generateBreathEndpoints",
        tags: new[] { "Endpoint", "Generation", "Breath", "U-CORE" },
        responses: new[] {
            "200:BreathEndpointResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateEndpoint(
        httpMethod: "POST",
        route: "/endpoint/demo/breath",
        operationId: "generate-breath-endpoints",
        tags: new[] { "Endpoint", "Breath", "Consciousness" },
        description: "Generate breath framework endpoints for consciousness expansion",
        useBreathFramework: true,
        requiredPhases: new[] { "compose", "expand", "validate", "melt", "patch", "refreeze", "contract" }
    )]
    public async Task<object> GenerateBreathEndpoints([ApiParameter("request", "Breath endpoint generation request", Required = true, Location = "body")] BreathEndpointRequest request)
    {
        try
        {
            // Generate breath framework endpoints
            var deltas = await _endpointGenerator.GenerateBreathFrameworkEndpoints(request.Context);

            // Apply delta diffs
            var results = await _endpointGenerator.ApplyDeltaDiffs(deltas, request.Context);

            return new BreathEndpointResponse(
                Success: true,
                Message: "Breath framework endpoints generated with spiritual resonance",
                Deltas: deltas,
                Results: results,
                Phases: new[] { "compose", "expand", "validate", "melt", "patch", "refreeze", "contract" },
                GeneratedAt: DateTime.UtcNow,
                Statistics: GenerateBreathStatistics(deltas)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate breath endpoints: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate U-CORE specific endpoints
    /// </summary>
    [ApiRoute("POST", "/endpoint/demo/ucore", "endpoint-ucore", "Generate U-CORE endpoints", "codex.endpoint.demo")]
    [ApiDocumentation(
        summary: "Generate U-CORE endpoints",
        description: "Generates U-CORE specific endpoints with consciousness expansion",
        operationId: "generateUcoreEndpoints",
        tags: new[] { "Endpoint", "Generation", "U-CORE", "Consciousness" },
        responses: new[] {
            "200:UcoreEndpointResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateEndpoint(
        httpMethod: "POST",
        route: "/endpoint/demo/ucore",
        operationId: "generate-ucore-endpoints",
        tags: new[] { "Endpoint", "U-CORE", "Consciousness", "Resonance" },
        description: "Generate U-CORE endpoints for consciousness expansion and healing",
        useBreathFramework: true,
        requiredPhases: new[] { "compose", "expand", "validate", "contract" }
    )]
    public async Task<object> GenerateUcoreEndpoints([ApiParameter("request", "U-CORE endpoint generation request", Required = true, Location = "body")] UcoreEndpointRequest request)
    {
        try
        {
            // Generate U-CORE endpoints
            var deltas = await _endpointGenerator.GenerateUcoreEndpoints(request.Context);

            // Apply delta diffs
            var results = await _endpointGenerator.ApplyDeltaDiffs(deltas, request.Context);

            return new UcoreEndpointResponse(
                Success: true,
                Message: "U-CORE endpoints generated with divine frequencies",
                Deltas: deltas,
                Results: results,
                Frequencies: new[] { 432.0, 528.0, 741.0 },
                GeneratedAt: DateTime.UtcNow,
                Statistics: GenerateUcoreStatistics(deltas)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate U-CORE endpoints: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all U-CORE delta diffs
    /// </summary>
    [ApiRoute("GET", "/endpoint/demo/deltas", "endpoint-deltas", "Get U-CORE delta diffs", "codex.endpoint.demo")]
    [ApiDocumentation(
        summary: "Get U-CORE delta diffs",
        description: "Retrieves all U-CORE delta diffs generated by the endpoint system",
        operationId: "getDeltaDiffs",
        tags: new[] { "Endpoint", "Delta", "U-CORE", "History" },
        responses: new[] {
            "200:DeltaDiffsResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateEndpoint(
        httpMethod: "GET",
        route: "/endpoint/demo/deltas",
        operationId: "get-delta-diffs",
        tags: new[] { "Endpoint", "Delta", "U-CORE" },
        description: "Get all U-CORE delta diffs with consciousness expansion",
        useBreathFramework: true,
        requiredPhases: new[] { "validate" }
    )]
    public async Task<object> GetDeltaDiffs()
    {
        try
        {
            var deltas = _endpointGenerator.GetDeltaDiffs();
            
            return new DeltaDiffsResponse(
                Success: true,
                Message: "U-CORE delta diffs retrieved with spiritual resonance",
                Deltas: deltas,
                Count: deltas.Count,
                RetrievedAt: DateTime.UtcNow,
                Statistics: GenerateDeltaStatistics(deltas)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get delta diffs: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply delta diffs to the system
    /// </summary>
    [ApiRoute("POST", "/endpoint/demo/apply", "endpoint-apply", "Apply delta diffs", "codex.endpoint.demo")]
    [ApiDocumentation(
        summary: "Apply delta diffs",
        description: "Applies U-CORE delta diffs to the system with consciousness expansion",
        operationId: "applyDeltaDiffs",
        tags: new[] { "Endpoint", "Delta", "U-CORE", "Apply" },
        responses: new[] {
            "200:DeltaApplyResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
    [GenerateEndpoint(
        httpMethod: "POST",
        route: "/endpoint/demo/apply",
        operationId: "apply-delta-diffs",
        tags: new[] { "Endpoint", "Delta", "U-CORE", "Apply" },
        description: "Apply delta diffs with consciousness expansion and healing",
        useBreathFramework: true,
        requiredPhases: new[] { "compose", "expand", "validate", "contract" }
    )]
    public async Task<object> ApplyDeltaDiffs([ApiParameter("request", "Delta apply request", Required = true, Location = "body")] DeltaApplyRequest request)
    {
        try
        {
            // Apply delta diffs
            var results = await _endpointGenerator.ApplyDeltaDiffs(request.Deltas, request.Context);

            return new DeltaApplyResponse(
                Success: true,
                Message: "Delta diffs applied with consciousness expansion",
                Results: results,
                AppliedCount: request.Deltas.Count,
                AppliedAt: DateTime.UtcNow,
                Statistics: GenerateApplyStatistics(results)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to apply delta diffs: {ex.Message}");
        }
    }

    // Helper methods

    private Dictionary<string, object> GenerateStatistics(List<UcoreDelta> deltas)
    {
        return new Dictionary<string, object>
        {
            ["totalDeltas"] = deltas.Count,
            ["endpointDeltas"] = deltas.Count(d => d.Type == "endpoint_added"),
            ["phaseDeltas"] = deltas.Count(d => d.Type == "phase_added"),
            ["resonanceDeltas"] = deltas.Count(d => d.Type == "resonance_updated"),
            ["averageResonance"] = deltas.Average(d => d.Resonance),
            ["totalFrequencies"] = deltas.SelectMany(d => d.Frequencies).Distinct().Count(),
            ["generatedAt"] = DateTime.UtcNow
        };
    }

    private Dictionary<string, object> GenerateBreathStatistics(List<UcoreDelta> deltas)
    {
        return new Dictionary<string, object>
        {
            ["totalDeltas"] = deltas.Count,
            ["breathPhases"] = deltas.Select(d => d.Phases.First()).Distinct().ToList(),
            ["averageResonance"] = deltas.Average(d => d.Resonance),
            ["totalFrequencies"] = deltas.SelectMany(d => d.Frequencies).Distinct().Count(),
            ["consciousnessExpansion"] = true,
            ["generatedAt"] = DateTime.UtcNow
        };
    }

    private Dictionary<string, object> GenerateUcoreStatistics(List<UcoreDelta> deltas)
    {
        return new Dictionary<string, object>
        {
            ["totalDeltas"] = deltas.Count,
            ["ucoreEndpoints"] = deltas.Count(d => d.Metadata.ContainsKey("ucoreEndpoint")),
            ["averageResonance"] = deltas.Average(d => d.Resonance),
            ["sacredFrequencies"] = new[] { 432.0, 528.0, 741.0 },
            ["consciousnessExpansion"] = true,
            ["frequencyHealing"] = true,
            ["generatedAt"] = DateTime.UtcNow
        };
    }

    private Dictionary<string, object> GenerateDeltaStatistics(List<UcoreDelta> deltas)
    {
        return new Dictionary<string, object>
        {
            ["totalDeltas"] = deltas.Count,
            ["deltaTypes"] = deltas.GroupBy(d => d.Type).ToDictionary(g => g.Key, g => g.Count()),
            ["averageResonance"] = deltas.Average(d => d.Resonance),
            ["totalFrequencies"] = deltas.SelectMany(d => d.Frequencies).Distinct().Count(),
            ["oldestDelta"] = deltas.Min(d => d.CreatedAt),
            ["newestDelta"] = deltas.Max(d => d.CreatedAt),
            ["retrievedAt"] = DateTime.UtcNow
        };
    }

    private Dictionary<string, object> GenerateApplyStatistics(Dictionary<string, object> results)
    {
        return new Dictionary<string, object>
        {
            ["totalApplied"] = results.Count,
            ["successful"] = results.Count(r => r.Value is Dictionary<string, object> dict && dict.ContainsKey("status") && dict["status"].ToString() == "success"),
            ["failed"] = results.Count(r => r.Value is Dictionary<string, object> dict && dict.ContainsKey("error")),
            ["appliedAt"] = DateTime.UtcNow
        };
    }
}

// Request/Response Types

[RequestType("codex.endpoint.generation-request", "EndpointGenerationRequest", "Endpoint generation request")]
public record EndpointGenerationRequest(
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.endpoint.generation-response", "EndpointGenerationResponse", "Endpoint generation response")]
public record EndpointGenerationResponse(
    bool Success,
    string Message,
    List<UcoreDelta> Deltas,
    Dictionary<string, object> Results,
    DateTime GeneratedAt,
    Dictionary<string, object> Statistics
);

[RequestType("codex.endpoint.breath-request", "BreathEndpointRequest", "Breath endpoint request")]
public record BreathEndpointRequest(
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.endpoint.breath-response", "BreathEndpointResponse", "Breath endpoint response")]
public record BreathEndpointResponse(
    bool Success,
    string Message,
    List<UcoreDelta> Deltas,
    Dictionary<string, object> Results,
    string[] Phases,
    DateTime GeneratedAt,
    Dictionary<string, object> Statistics
);

[RequestType("codex.endpoint.ucore-request", "UcoreEndpointRequest", "U-CORE endpoint request")]
public record UcoreEndpointRequest(
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.endpoint.ucore-response", "UcoreEndpointResponse", "U-CORE endpoint response")]
public record UcoreEndpointResponse(
    bool Success,
    string Message,
    List<UcoreDelta> Deltas,
    Dictionary<string, object> Results,
    double[] Frequencies,
    DateTime GeneratedAt,
    Dictionary<string, object> Statistics
);

[ResponseType("codex.endpoint.delta-diffs-response", "DeltaDiffsResponse", "Delta diffs response")]
public record DeltaDiffsResponse(
    bool Success,
    string Message,
    List<UcoreDelta> Deltas,
    int Count,
    DateTime RetrievedAt,
    Dictionary<string, object> Statistics
);

[RequestType("codex.endpoint.delta-apply-request", "DeltaApplyRequest", "Delta apply request")]
public record DeltaApplyRequest(
    List<UcoreDelta> Deltas,
    Dictionary<string, object>? Context = null
);

[ResponseType("codex.endpoint.delta-apply-response", "DeltaApplyResponse", "Delta apply response")]
public record DeltaApplyResponse(
    bool Success,
    string Message,
    Dictionary<string, object> Results,
    int AppliedCount,
    DateTime AppliedAt,
    Dictionary<string, object> Statistics
);
