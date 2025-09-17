using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Image Analysis Data Types

[MetaNodeAttribute("codex.analysis.config", "codex.meta/type", "AnalysisConfig", "Configuration for image analysis models")]
[ApiType(
    Name = "Analysis Configuration",
    Type = "object",
    Description = "Configuration settings for image analysis models (GPT-4V, Claude Vision, Custom Vision APIs)",
    Example = @"{
      ""id"": ""gpt4v"",
      ""name"": ""OpenAI GPT-4 Vision"",
      ""provider"": ""OpenAI"",
      ""model"": ""gpt-4-vision-preview"",
      ""apiKey"": ""sk-..."",
      ""baseUrl"": ""https://api.openai.com/v1"",
      ""maxTokens"": 2000,
      ""temperature"": 0.3,
      ""analysisType"": ""comprehensive""
    }"
)]
public record AnalysisConfig(
    string Id,
    string Name,
    string Provider,
    string Model,
    string ApiKey,
    string BaseUrl,
    int MaxTokens,
    double Temperature,
    string AnalysisType,
    Dictionary<string, object> Parameters
);

[MetaNodeAttribute("codex.analysis.image", "codex.meta/type", "ImageAnalysis", "Image to be analyzed for node/edge extraction")]
[ApiType(
    Name = "Image Analysis",
    Type = "object",
    Description = "An image that will be analyzed to extract nodes and edges",
    Example = @"{
      ""id"": ""analysis-123"",
      ""imageUrl"": ""https://example.com/image.png"",
      ""imageData"": ""base64-encoded-image-data"",
      ""analysisType"": ""comprehensive"",
      ""context"": ""Analyzing a concept visualization for U-CORE Joy system"",
      ""metadata"": {}
    }"
)]
public record ImageAnalysis(
    string Id,
    string? ImageUrl,
    string? ImageData,
    string AnalysisType,
    string? Context,
    Dictionary<string, object> Metadata
);

[MetaNodeAttribute("codex.analysis.result", "codex.meta/type", "AnalysisResult", "Result of image analysis with extracted nodes and edges")]
[ApiType(
    Name = "Analysis Result",
    Type = "object",
    Description = "Result of image analysis containing extracted nodes and edges",
    Example = @"{
      ""id"": ""result-456"",
      ""imageAnalysis"": { ""id"": ""analysis-123"" },
      ""analysisConfig"": { ""id"": ""gpt4v"" },
      ""nodes"": [
        {
          ""id"": ""node-1"",
          ""typeId"": ""codex.concept.joy"",
          ""title"": ""Joy Amplification"",
          ""description"": ""Visual representation of joy being amplified"",
          ""properties"": { ""color"": ""gold"", ""intensity"": ""high"" }
        }
      ],
      ""edges"": [
        {
          ""id"": ""edge-1"",
          ""fromId"": ""node-1"",
          ""toId"": ""node-2"",
          ""role"": ""amplifies"",
          ""weight"": 0.9
        }
      ],
      ""confidence"": 0.85,
      ""status"": ""completed""
    }"
)]
public record AnalysisResult(
    string Id,
    ImageAnalysis ImageAnalysis,
    AnalysisConfig AnalysisConfig,
    List<ExtractedNode> Nodes,
    List<ExtractedEdge> Edges,
    double Confidence,
    string Status,
    string? Error,
    DateTime AnalyzedAt
);

[MetaNodeAttribute("codex.analysis.extracted-node", "codex.meta/type", "ExtractedNode", "Node extracted from image analysis")]
[ApiType(
    Name = "Extracted Node",
    Type = "object",
    Description = "A node extracted from image analysis with properties and relationships",
    Example = @"{
      ""id"": ""node-1"",
      ""typeId"": ""codex.concept.joy"",
      ""title"": ""Joy Amplification"",
      ""description"": ""Visual representation of joy being amplified through frequency resonance"",
      ""properties"": {
        ""color"": ""gold"",
        ""intensity"": ""high"",
        ""position"": ""center"",
        ""size"": ""large""
      },
      ""confidence"": 0.9,
      ""boundingBox"": { ""x"": 100, ""y"": 100, ""width"": 200, ""height"": 200 }
    }"
)]
public record ExtractedNode(
    string Id,
    string TypeId,
    string Title,
    string Description,
    Dictionary<string, object> Properties,
    double Confidence,
    Dictionary<string, object>? BoundingBox
);

[MetaNodeAttribute("codex.analysis.extracted-edge", "codex.meta/type", "ExtractedEdge", "Edge extracted from image analysis")]
[ApiType(
    Name = "Extracted Edge",
    Type = "object",
    Description = "An edge extracted from image analysis representing relationships between nodes",
    Example = @"{
      ""id"": ""edge-1"",
      ""fromId"": ""node-1"",
      ""toId"": ""node-2"",
      ""role"": ""amplifies"",
      ""weight"": 0.9,
      ""properties"": {
        ""strength"": ""strong"",
        ""direction"": ""unidirectional"",
        ""color"": ""gold""
      },
      ""confidence"": 0.8
    }"
)]
public record ExtractedEdge(
    string Id,
    string FromId,
    string ToId,
    string Role,
    double Weight,
    Dictionary<string, object> Properties,
    double Confidence
);

/// <summary>
/// Image Analysis Module - Analyzes images to extract nodes and edges for the node-based system
/// </summary>
[MetaNodeAttribute(
    id: "codex.analysis.image-module",
    typeId: "codex.meta/module",
    name: "Image Analysis Module",
    description: "Analyzes images to extract nodes and edges for integration with the node-based system"
)]
[ApiModule(
    Name = "Image Analysis",
    Version = "1.0.0",
    Description = "Configurable image analysis for extracting structured data",
    Tags = new[] { "Image Analysis", "AI", "Computer Vision", "Node Extraction", "Edge Detection" }
)]
public class ImageAnalysisModule : ModuleBase
{
    private readonly Dictionary<string, AnalysisConfig> _analysisConfigs;

    public override string Name => "Image Analysis Module";
    public override string Description => "Analyzes images to extract nodes and edges for integration with the node-based system";
    public override string Version => "1.0.0";

    public ImageAnalysisModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _analysisConfigs = new Dictionary<string, AnalysisConfig>();
        InitializeDefaultConfigs();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.analysis.image",
            name: "Image Analysis Module",
            version: "1.0.0",
            description: "Analyzes images to extract nodes and edges for the node-based system",
            tags: new[] { "image", "analysis", "extraction", "ai" },
            capabilities: new[] { "image-analysis", "node-extraction", "edge-extraction", "ai-integration" },
            spec: "codex.spec.image-analysis"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _apiRouter = router;
        // API handlers are registered via attributes
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    [ApiRoute("POST", "/analysis/image", "analysis-image", "Analyze image to extract nodes and edges", "codex.analysis.image")]
    public async Task<object> AnalyzeImage([ApiParameter("request", "Image analysis request", Required = true, Location = "body")] ImageAnalysisRequest request)
    {
        try
        {
            // Get analysis configuration
            var analysisConfig = GetAnalysisConfig(request.AnalysisConfigId);
            if (analysisConfig == null)
            {
                return new ErrorResponse($"Analysis configuration '{request.AnalysisConfigId}' not found");
            }

            // Create image analysis record
            var imageAnalysis = new ImageAnalysis(
                Id: Guid.NewGuid().ToString(),
                ImageUrl: request.ImageUrl,
                ImageData: request.ImageData,
                AnalysisType: request.AnalysisType ?? "comprehensive",
                Context: request.Context,
                Metadata: request.Metadata ?? new Dictionary<string, object>()
            );

            // Store image analysis as a node
            var analysisNode = CreateImageAnalysisNode(imageAnalysis);
            _registry.Upsert(analysisNode);

            // Perform image analysis
            var result = await PerformImageAnalysis(imageAnalysis, analysisConfig);
            
            // Store analysis result as a node
            var resultNode = CreateAnalysisResultNode(result);
            _registry.Upsert(resultNode);

            // Create actual nodes and edges in the registry
            await CreateNodesAndEdgesFromAnalysis(result);

            return new AnalysisResultResponse(
                Success: true,
                Message: "Image analysis completed successfully",
                Result: result,
                NodesCreated: result.Nodes.Count,
                EdgesCreated: result.Edges.Count,
                Insights: GenerateAnalysisInsights(result)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to analyze image: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/analysis/config", "analysis-config-create", "Create or update analysis configuration", "codex.analysis.image")]
    public async Task<object> CreateAnalysisConfig([ApiParameter("request", "Analysis config request", Required = true, Location = "body")] AnalysisConfigRequest request)
    {
        try
        {
            var config = new AnalysisConfig(
                Id: request.Id ?? Guid.NewGuid().ToString(),
                Name: request.Name,
                Provider: request.Provider,
                Model: request.Model,
                ApiKey: request.ApiKey ?? "",
                BaseUrl: request.BaseUrl ?? GetDefaultBaseUrl(request.Provider),
                MaxTokens: request.MaxTokens ?? 2000,
                Temperature: request.Temperature ?? 0.3,
                AnalysisType: request.AnalysisType ?? "comprehensive",
                Parameters: request.Parameters ?? new Dictionary<string, object>()
            );

            // Validate configuration
            var validation = await ValidateAnalysisConfig(config);
            if (!validation.IsValid)
            {
                return new ErrorResponse($"Analysis configuration validation failed: {validation.ErrorMessage}");
            }

            // Store configuration
            _analysisConfigs[config.Id] = config;
            var configNode = CreateAnalysisConfigNode(config);
            _registry.Upsert(configNode);

            return new AnalysisConfigResponse(
                Success: true,
                Message: "Analysis configuration created successfully",
                Config: config,
                Validation: validation
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to create analysis configuration: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/analysis/configs", "analysis-configs", "Get all analysis configurations", "codex.analysis.image")]
    public async Task<object> GetAnalysisConfigs()
    {
        try
        {
            var configs = _analysisConfigs.Values.ToList();
            return new AnalysisConfigsResponse(
                Success: true,
                Message: $"Retrieved {configs.Count} analysis configurations",
                Configs: configs
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get analysis configurations: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/analysis/results", "analysis-results", "Get all analysis results", "codex.analysis.image")]
    public async Task<object> GetAnalysisResults()
    {
        try
        {
            var allNodes = _registry.AllNodes();
            var resultNodes = allNodes
                .Where(n => n.TypeId == "codex.analysis.result")
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("analyzedAt", DateTime.MinValue))
                .ToList();

            var results = new List<AnalysisResult>();
            foreach (var node in resultNodes)
            {
                if (node.Content?.InlineJson != null)
                {
                    var result = JsonSerializer.Deserialize<AnalysisResult>(node.Content.InlineJson);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }

            return new AnalysisResultsResponse(
                Success: true,
                Message: $"Retrieved {results.Count} analysis results",
                Results: results
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get analysis results: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/analysis/batch", "analysis-batch", "Batch analyze multiple images", "codex.analysis.image")]
    public async Task<object> BatchAnalyzeImages([ApiParameter("request", "Batch analysis request", Required = true, Location = "body")] BatchAnalysisRequest request)
    {
        try
        {
            var results = new List<AnalysisResultResponse>();
            var analysisConfig = GetAnalysisConfig(request.AnalysisConfigId);

            if (analysisConfig == null)
            {
                return new ErrorResponse($"Analysis configuration '{request.AnalysisConfigId}' not found");
            }

            foreach (var imageRequest in request.Images)
            {
                try
                {
                    var imageAnalysis = new ImageAnalysis(
                        Id: Guid.NewGuid().ToString(),
                        ImageUrl: imageRequest.ImageUrl,
                        ImageData: imageRequest.ImageData,
                        AnalysisType: imageRequest.AnalysisType ?? request.AnalysisType ?? "comprehensive",
                        Context: imageRequest.Context ?? request.Context,
                        Metadata: imageRequest.Metadata ?? request.Metadata ?? new Dictionary<string, object>()
                    );

                    var analysisNode = CreateImageAnalysisNode(imageAnalysis);
                    _registry.Upsert(analysisNode);

                    var result = await PerformImageAnalysis(imageAnalysis, analysisConfig);
                    var resultNode = CreateAnalysisResultNode(result);
                    _registry.Upsert(resultNode);

                    await CreateNodesAndEdgesFromAnalysis(result);

                    results.Add(new AnalysisResultResponse(
                        Success: true,
                        Message: "Image analysis completed successfully",
                        Result: result,
                        NodesCreated: result.Nodes.Count,
                        EdgesCreated: result.Edges.Count,
                        Insights: GenerateAnalysisInsights(result)
                    ));
                }
                catch (Exception ex)
                {
                    results.Add(new AnalysisResultResponse(
                        Success: false,
                        Message: $"Failed to analyze image: {ex.Message}",
                        Result: null,
                        NodesCreated: 0,
                        EdgesCreated: 0,
                        Insights: new List<string>()
                    ));
                }
            }

            return new BatchAnalysisResponse(
                Success: true,
                Message: $"Processed {results.Count} images",
                Results: results,
                SuccessCount: results.Count(r => r.Success),
                FailureCount: results.Count(r => !r.Success)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to process batch analysis: {ex.Message}");
        }
    }

    // Helper methods

    private void InitializeDefaultConfigs()
    {
        // OpenAI GPT-4 Vision Configuration
        _analysisConfigs["gpt4v"] = new AnalysisConfig(
            Id: "gpt4v",
            Name: "OpenAI GPT-4 Vision",
            Provider: "OpenAI",
            Model: "gpt-4-vision-preview",
            ApiKey: "",
            BaseUrl: "https://api.openai.com/v1",
            MaxTokens: 2000,
            Temperature: 0.3,
            AnalysisType: "comprehensive",
            Parameters: new Dictionary<string, object>()
        );

        // Anthropic Claude Vision Configuration
        _analysisConfigs["claude-vision"] = new AnalysisConfig(
            Id: "claude-vision",
            Name: "Anthropic Claude Vision",
            Provider: "Anthropic",
            Model: "claude-3-vision-20240229",
            ApiKey: "",
            BaseUrl: "https://api.anthropic.com",
            MaxTokens: 2000,
            Temperature: 0.3,
            AnalysisType: "comprehensive",
            Parameters: new Dictionary<string, object>()
        );

        // Google Gemini Vision Configuration
        _analysisConfigs["gemini-vision"] = new AnalysisConfig(
            Id: "gemini-vision",
            Name: "Google Gemini Vision",
            Provider: "Google",
            Model: "gemini-pro-vision",
            ApiKey: "",
            BaseUrl: "https://generativelanguage.googleapis.com",
            MaxTokens: 2000,
            Temperature: 0.3,
            AnalysisType: "comprehensive",
            Parameters: new Dictionary<string, object>()
        );

        // Local Vision Configuration
        _analysisConfigs["local-vision"] = new AnalysisConfig(
            Id: "local-vision",
            Name: "Local Vision Model",
            Provider: "Local",
            Model: "local-vision-model",
            ApiKey: "",
            BaseUrl: "http://localhost:8000",
            MaxTokens: 2000,
            Temperature: 0.3,
            AnalysisType: "comprehensive",
            Parameters: new Dictionary<string, object>()
        );
    }

    private AnalysisConfig? GetAnalysisConfig(string configId)
    {
        return _analysisConfigs.GetValueOrDefault(configId);
    }

    private string GetDefaultBaseUrl(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "anthropic" => "https://api.anthropic.com",
            "google" => "https://generativelanguage.googleapis.com",
            "azure" => "https://your-resource.cognitiveservices.azure.com",
            "local" => "http://localhost:8000",
            "custom" => "http://localhost:8000",
            _ => "http://localhost:8000"
        };
    }

    private async Task<AnalysisResult> PerformImageAnalysis(ImageAnalysis imageAnalysis, AnalysisConfig config)
    {
        // This is a simplified implementation
        // In a real implementation, you would call the actual vision APIs
        
        await Task.Delay(2000); // Simulate async operation
        
        // Generate sample extracted nodes and edges based on analysis type
        var nodes = GenerateSampleNodes(imageAnalysis);
        var edges = GenerateSampleEdges(nodes);
        
        var confidence = CalculateOverallConfidence(nodes, edges);

        return new AnalysisResult(
            Id: Guid.NewGuid().ToString(),
            ImageAnalysis: imageAnalysis,
            AnalysisConfig: config,
            Nodes: nodes,
            Edges: edges,
            Confidence: confidence,
            Status: "completed",
            Error: null,
            AnalyzedAt: DateTime.UtcNow
        );
    }

    private List<ExtractedNode> GenerateSampleNodes(ImageAnalysis imageAnalysis)
    {
        var nodes = new List<ExtractedNode>();
        
        // Generate nodes based on analysis type
        switch (imageAnalysis.AnalysisType.ToLowerInvariant())
        {
            case "comprehensive":
                nodes.AddRange(GenerateComprehensiveNodes());
                break;
            case "objects":
                nodes.AddRange(GenerateObjectNodes());
                break;
            case "relationships":
                nodes.AddRange(GenerateRelationshipNodes());
                break;
            case "concepts":
                nodes.AddRange(GenerateConceptNodes());
                break;
            case "emotions":
                nodes.AddRange(GenerateEmotionNodes());
                break;
            case "text":
                nodes.AddRange(GenerateTextNodes());
                break;
        }

        return nodes;
    }

    private List<ExtractedNode> GenerateComprehensiveNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.concept.joy",
                Title: "Joy Amplification",
                Description: "Visual representation of joy being amplified through frequency resonance",
                Properties: new Dictionary<string, object>
                {
                    ["color"] = "gold",
                    ["intensity"] = "high",
                    ["position"] = "center",
                    ["size"] = "large",
                    ["shape"] = "spiral"
                },
                Confidence: 0.9,
                BoundingBox: new Dictionary<string, object> { ["x"] = 100, ["y"] = 100, ["width"] = 200, ["height"] = 200 }
            ),
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.concept.frequency",
                Title: "Sacred Frequency",
                Description: "Visual representation of a sacred frequency wave",
                Properties: new Dictionary<string, object>
                {
                    ["color"] = "violet",
                    ["frequency"] = "341.3 Hz",
                    ["position"] = "top-right",
                    ["size"] = "medium",
                    ["shape"] = "wave"
                },
                Confidence: 0.85,
                BoundingBox: new Dictionary<string, object> { ["x"] = 300, ["y"] = 50, ["width"] = 150, ["height"] = 100 }
            ),
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.concept.energy",
                Title: "Energy Field",
                Description: "Visual representation of an energy field surrounding the joy amplification",
                Properties: new Dictionary<string, object>
                {
                    ["color"] = "white",
                    ["intensity"] = "medium",
                    ["position"] = "background",
                    ["size"] = "large",
                    ["shape"] = "aura"
                },
                Confidence: 0.8,
                BoundingBox: new Dictionary<string, object> { ["x"] = 50, ["y"] = 50, ["width"] = 300, ["height"] = 300 }
            )
        };
    }

    private List<ExtractedNode> GenerateObjectNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.object.light",
                Title: "Golden Light",
                Description: "A beam of golden light representing joy energy",
                Properties: new Dictionary<string, object>
                {
                    ["color"] = "gold",
                    ["brightness"] = "high",
                    ["direction"] = "upward"
                },
                Confidence: 0.95,
                BoundingBox: new Dictionary<string, object> { ["x"] = 150, ["y"] = 200, ["width"] = 20, ["height"] = 100 }
            )
        };
    }

    private List<ExtractedNode> GenerateRelationshipNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.relationship.amplification",
                Title: "Amplification Process",
                Description: "The process of amplifying joy through frequency resonance",
                Properties: new Dictionary<string, object>
                {
                    ["type"] = "amplification",
                    ["strength"] = "strong",
                    ["direction"] = "bidirectional"
                },
                Confidence: 0.88,
                BoundingBox: new Dictionary<string, object> { ["x"] = 200, ["y"] = 150, ["width"] = 100, ["height"] = 50 }
            )
        };
    }

    private List<ExtractedNode> GenerateConceptNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.concept.consciousness",
                Title: "Consciousness Expansion",
                Description: "Visual representation of consciousness expanding through joy",
                Properties: new Dictionary<string, object>
                {
                    ["level"] = "expanded",
                    ["state"] = "awakened",
                    ["growth"] = "exponential"
                },
                Confidence: 0.82,
                BoundingBox: new Dictionary<string, object> { ["x"] = 100, ["y"] = 300, ["width"] = 200, ["height"] = 100 }
            )
        };
    }

    private List<ExtractedNode> GenerateEmotionNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.emotion.joy",
                Title: "Pure Joy",
                Description: "Visual representation of pure joy emotion",
                Properties: new Dictionary<string, object>
                {
                    ["emotion"] = "joy",
                    ["intensity"] = "high",
                    ["purity"] = "pure"
                },
                Confidence: 0.9,
                BoundingBox: new Dictionary<string, object> { ["x"] = 120, ["y"] = 120, ["width"] = 160, ["height"] = 160 }
            )
        };
    }

    private List<ExtractedNode> GenerateTextNodes()
    {
        return new List<ExtractedNode>
        {
            new(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.text.label",
                Title: "Joy Amplification Label",
                Description: "Text label identifying the joy amplification process",
                Properties: new Dictionary<string, object>
                {
                    ["text"] = "Joy Amplification",
                    ["font"] = "sans-serif",
                    ["size"] = "large"
                },
                Confidence: 0.95,
                BoundingBox: new Dictionary<string, object> { ["x"] = 50, ["y"] = 50, ["width"] = 200, ["height"] = 30 }
            )
        };
    }

    private List<ExtractedEdge> GenerateSampleEdges(List<ExtractedNode> nodes)
    {
        var edges = new List<ExtractedEdge>();
        
        if (nodes.Count >= 2)
        {
            // Create edges between nodes
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                edges.Add(new ExtractedEdge(
                    Id: Guid.NewGuid().ToString(),
                    FromId: nodes[i].Id,
                    ToId: nodes[i + 1].Id,
                    Role: "influences",
                    Weight: 0.8,
                    Properties: new Dictionary<string, object>
                    {
                        ["strength"] = "strong",
                        ["direction"] = "unidirectional",
                        ["type"] = "influence"
                    },
                    Confidence: 0.85
                ));
            }
        }

        return edges;
    }

    private double CalculateOverallConfidence(List<ExtractedNode> nodes, List<ExtractedEdge> edges)
    {
        var nodeConfidence = nodes.Any() ? nodes.Average(n => n.Confidence) : 0.0;
        var edgeConfidence = edges.Any() ? edges.Average(e => e.Confidence) : 0.0;
        
        return (nodeConfidence + edgeConfidence) / 2.0;
    }

    private async Task CreateNodesAndEdgesFromAnalysis(AnalysisResult result)
    {
        // Create actual nodes in the registry
        foreach (var extractedNode in result.Nodes)
        {
            var node = new Node(
                Id: extractedNode.Id,
                TypeId: extractedNode.TypeId,
                State: ContentState.Water,
                Locale: "en",
                Title: extractedNode.Title,
                Description: extractedNode.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(extractedNode.Properties),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["confidence"] = extractedNode.Confidence,
                    ["extractedFrom"] = result.ImageAnalysis.Id,
                    ["analysisResult"] = result.Id,
                    ["boundingBox"] = extractedNode.BoundingBox ?? new Dictionary<string, object>()
                }
            );
            
            _registry.Upsert(node);
        }

        // Create actual edges in the registry
        foreach (var extractedEdge in result.Edges)
        {
            var edge = new Edge(
                FromId: extractedEdge.FromId,
                ToId: extractedEdge.ToId,
                Role: extractedEdge.Role,
                Weight: extractedEdge.Weight,
                Meta: new Dictionary<string, object>
                {
                    ["confidence"] = extractedEdge.Confidence,
                    ["extractedFrom"] = result.ImageAnalysis.Id,
                    ["analysisResult"] = result.Id,
                    ["properties"] = extractedEdge.Properties
                }
            );
            
            _registry.Upsert(edge);
        }
    }

    private async Task<AnalysisConfigValidation> ValidateAnalysisConfig(AnalysisConfig config)
    {
        // Basic validation
        if (string.IsNullOrEmpty(config.Name))
        {
            return new AnalysisConfigValidation(false, "Name is required");
        }

        if (string.IsNullOrEmpty(config.Provider))
        {
            return new AnalysisConfigValidation(false, "Provider is required");
        }

        if (string.IsNullOrEmpty(config.Model))
        {
            return new AnalysisConfigValidation(false, "Model is required");
        }

        if (config.MaxTokens <= 0)
        {
            return new AnalysisConfigValidation(false, "MaxTokens must be greater than 0");
        }

        if (config.Temperature < 0 || config.Temperature > 2)
        {
            return new AnalysisConfigValidation(false, "Temperature must be between 0 and 2");
        }

        // Test connection (simplified)
        try
        {
            await TestAnalysisConfigConnection(config);
            return new AnalysisConfigValidation(true, "Configuration is valid");
        }
        catch (Exception ex)
        {
            return new AnalysisConfigValidation(false, $"Connection test failed: {ex.Message}");
        }
    }

    private async Task TestAnalysisConfigConnection(AnalysisConfig config)
    {
        // Simplified connection test
        await Task.Delay(100);
        
        // In a real implementation, you would make an actual API call
        if (config.Provider == "OpenAI" && string.IsNullOrEmpty(config.ApiKey))
        {
            throw new Exception("API key required for OpenAI");
        }
    }

    private List<string> GenerateAnalysisInsights(AnalysisResult result)
    {
        return new List<string>
        {
            $"Extracted {result.Nodes.Count} nodes and {result.Edges.Count} edges",
            $"Overall confidence: {result.Confidence:P1}",
            $"Analysis type: {result.ImageAnalysis.AnalysisType}",
            $"Used {result.AnalysisConfig.Provider} {result.AnalysisConfig.Model}",
            $"Analyzed at: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss}"
        };
    }

    // Node creation methods
    private Node CreateAnalysisConfigNode(AnalysisConfig config)
    {
        return new Node(
            Id: config.Id,
            TypeId: "codex.analysis.config",
            State: ContentState.Ice,
            Locale: "en",
            Title: config.Name,
            Description: $"{config.Provider} {config.Model} configuration",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(config),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["provider"] = config.Provider,
                ["model"] = config.Model,
                ["maxTokens"] = config.MaxTokens,
                ["analysisType"] = config.AnalysisType
            }
        );
    }

    private Node CreateImageAnalysisNode(ImageAnalysis analysis)
    {
        return new Node(
            Id: analysis.Id,
            TypeId: "codex.analysis.image",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Image Analysis: {analysis.AnalysisType}",
            Description: analysis.Context ?? "Image analysis for node extraction",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(analysis),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["analysisType"] = analysis.AnalysisType,
                ["hasImageUrl"] = !string.IsNullOrEmpty(analysis.ImageUrl),
                ["hasImageData"] = !string.IsNullOrEmpty(analysis.ImageData)
            }
        );
    }

    private Node CreateAnalysisResultNode(AnalysisResult result)
    {
        return new Node(
            Id: result.Id,
            TypeId: "codex.analysis.result",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Analysis Result: {result.Nodes.Count} nodes, {result.Edges.Count} edges",
            Description: $"Confidence: {result.Confidence:P1}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(result),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["imageAnalysisId"] = result.ImageAnalysis.Id,
                ["analysisConfigId"] = result.AnalysisConfig.Id,
                ["nodeCount"] = result.Nodes.Count,
                ["edgeCount"] = result.Edges.Count,
                ["confidence"] = result.Confidence,
                ["status"] = result.Status,
                ["analyzedAt"] = result.AnalyzedAt
            }
        );
    }
}

// Data types
public record AnalysisConfigValidation(bool IsValid, string ErrorMessage);

// Request/Response Types
[ResponseType("codex.analysis.result-response", "AnalysisResultResponse", "Analysis result response")]
public record AnalysisResultResponse(
    bool Success,
    string Message,
    AnalysisResult? Result,
    int NodesCreated,
    int EdgesCreated,
    List<string> Insights
);

[ResponseType("codex.analysis.config-response", "AnalysisConfigResponse", "Analysis config response")]
public record AnalysisConfigResponse(
    bool Success,
    string Message,
    AnalysisConfig Config,
    AnalysisConfigValidation Validation
);

[ResponseType("codex.analysis.configs-response", "AnalysisConfigsResponse", "Analysis configs response")]
public record AnalysisConfigsResponse(
    bool Success,
    string Message,
    List<AnalysisConfig> Configs
);

[ResponseType("codex.analysis.results-response", "AnalysisResultsResponse", "Analysis results response")]
public record AnalysisResultsResponse(
    bool Success,
    string Message,
    List<AnalysisResult> Results
);

[ResponseType("codex.analysis.batch-response", "BatchAnalysisResponse", "Batch analysis response")]
public record BatchAnalysisResponse(
    bool Success,
    string Message,
    List<AnalysisResultResponse> Results,
    int SuccessCount,
    int FailureCount
);

[RequestType("codex.analysis.image-request", "ImageAnalysisRequest", "Image analysis request")]
public record ImageAnalysisRequest(
    string? ImageUrl = null,
    string? ImageData = null,
    string? AnalysisType = null,
    string? Context = null,
    string AnalysisConfigId = "gpt4v",
    Dictionary<string, object>? Metadata = null
);

[RequestType("codex.analysis.config-request", "AnalysisConfigRequest", "Analysis config request")]
public record AnalysisConfigRequest(
    string Name,
    string Provider,
    string Model,
    string? Id = null,
    string? ApiKey = null,
    string? BaseUrl = null,
    int? MaxTokens = null,
    double? Temperature = null,
    string? AnalysisType = null,
    Dictionary<string, object>? Parameters = null
);

[RequestType("codex.analysis.batch-request", "BatchAnalysisRequest", "Batch analysis request")]
public record BatchAnalysisRequest(
    List<ImageAnalysisRequest> Images,
    string? AnalysisType = null,
    string? Context = null,
    string AnalysisConfigId = "gpt4v",
    Dictionary<string, object>? Metadata = null
);
