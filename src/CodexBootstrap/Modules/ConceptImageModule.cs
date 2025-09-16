using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Concept Image Generation Data Types

[MetaNodeAttribute("codex.image.config", "codex.meta/type", "ImageConfig", "Configuration for image generation models")]
[ApiType(
    Name = "Image Configuration",
    Type = "object",
    Description = "Configuration settings for image generation models (DALL-E, Stable Diffusion, Midjourney, Custom)",
    Example = @"{
      ""id"": ""dalle-3"",
      ""name"": ""OpenAI DALL-E 3"",
      ""provider"": ""OpenAI"",
      ""model"": ""dall-e-3"",
      ""apiKey"": ""sk-..."",
      ""baseUrl"": ""https://api.openai.com/v1"",
      ""maxImages"": 4,
      ""imageSize"": ""1024x1024"",
      ""quality"": ""standard"",
      ""style"": ""vivid""
    }"
)]
public record ImageConfig(
    string Id,
    string Name,
    string Provider,
    string Model,
    string ApiKey,
    string BaseUrl,
    int MaxImages,
    string ImageSize,
    string Quality,
    string Style,
    Dictionary<string, object> Parameters
);

[MetaNodeAttribute("codex.image.concept", "codex.meta/type", "ConceptImage", "Concept to be rendered as an image")]
[ApiType(
    Name = "Concept Image",
    Type = "object",
    Description = "A concept that can be rendered into an image using AI image generation",
    Example = @"{
      ""id"": ""concept-123"",
      ""title"": ""Joy Amplification"",
      ""description"": ""A visualization of joy being amplified through frequency resonance"",
      ""conceptType"": ""spiritual"",
      ""style"": ""abstract"",
      ""mood"": ""uplifting"",
      ""colors"": [""gold"", ""white"", ""rainbow""],
      ""elements"": [""light"", ""energy"", ""frequency"", ""heart""]
    }"
)]
public record ConceptImage(
    string Id,
    string Title,
    string Description,
    string ConceptType,
    string Style,
    string Mood,
    List<string> Colors,
    List<string> Elements,
    Dictionary<string, object> Metadata
);

[MetaNodeAttribute("codex.image.generation", "codex.meta/type", "ImageGeneration", "Image generation request and result")]
[ApiType(
    Name = "Image Generation",
    Type = "object",
    Description = "Request and result for generating images from concepts",
    Example = @"{
      ""id"": ""gen-456"",
      ""concept"": { ""id"": ""concept-123"" },
      ""prompt"": ""A beautiful visualization of joy amplification..."",
      ""imageConfig"": { ""id"": ""dalle-3"" },
      ""status"": ""completed"",
      ""images"": [""https://example.com/image1.png""],
      ""generatedAt"": ""2025-01-27T10:30:00Z""
    }"
)]
public record ImageGeneration(
    string Id,
    ConceptImage Concept,
    string Prompt,
    ImageConfig ImageConfig,
    string Status,
    List<string> Images,
    string? Error,
    DateTime GeneratedAt
);

/// <summary>
/// Concept Image Generation Module - Renders concepts into images using configurable image generation models
/// </summary>
[MetaNodeAttribute(
    id: "codex.image.concept-module",
    typeId: "codex.meta/module",
    name: "Concept Image Generation Module",
    description: "Renders concepts into images using configurable local and remote image generation models"
)]
[ApiModule(
    Name = "Concept Image Generation",
    Version = "1.0.0",
    Description = "Configurable image generation for visualizing concepts",
    Tags = new[] { "Image Generation", "AI", "Visualization", "Concepts", "Art" }
)]
public class ConceptImageModule : ModuleBase
{
    private IApiRouter _apiRouter;
    private readonly Dictionary<string, ImageConfig> _imageConfigs;

    public override string Name => "Concept Image Generation Module";
    public override string Description => "Renders concepts into images using configurable local and remote image generation models";
    public override string Version => "1.0.0";

    public ConceptImageModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient, IApiRouter? apiRouter = null) 
        : base(registry, logger)
    {
        _apiRouter = apiRouter;
        _imageConfigs = new Dictionary<string, ImageConfig>();
        InitializeDefaultConfigs();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.image.concept",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "image", "concept", "ai", "visualization" },
            capabilities: new[] { "image-generation", "concept-visualization", "ai-integration", "multi-provider-support" },
            spec: "codex.spec.concept-image"
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

    [ApiRoute("POST", "/image/concept/create", "image-concept-create", "Create a concept for image generation", "codex.image.concept")]
    public async Task<object> CreateConcept([ApiParameter("request", "Concept creation request", Required = true, Location = "body")] ConceptCreationRequest request)
    {
        try
        {
            var concept = new ConceptImage(
                Id: Guid.NewGuid().ToString(),
                Title: request.Title,
                Description: request.Description,
                ConceptType: request.ConceptType,
                Style: request.Style,
                Mood: request.Mood,
                Colors: request.Colors ?? new List<string>(),
                Elements: request.Elements ?? new List<string>(),
                Metadata: request.Metadata ?? new Dictionary<string, object>()
            );

            // Store concept as a node
            var conceptNode = CreateConceptNode(concept);
            _registry.Upsert(conceptNode);

            return new ConceptImageResponse(
                Success: true,
                Message: "Concept created successfully",
                Concept: concept,
                NextSteps: GenerateConceptNextSteps(concept)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to create concept: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/image/generate", "image-generate", "Generate images from a concept", "codex.image.concept")]
    public async Task<object> GenerateImages([ApiParameter("request", "Image generation request", Required = true, Location = "body")] ImageGenerationRequest request)
    {
        try
        {
            // Get concept
            if (!_registry.TryGet(request.ConceptId, out var conceptNode))
            {
                return new ErrorResponse($"Concept '{request.ConceptId}' not found");
            }

            var concept = JsonSerializer.Deserialize<ConceptImage>(conceptNode.Content?.InlineJson ?? "{}");
            if (concept == null)
            {
                return new ErrorResponse("Invalid concept data");
            }

            // Get image configuration
            var imageConfig = GetImageConfig(request.ImageConfigId);
            if (imageConfig == null)
            {
                return new ErrorResponse($"Image configuration '{request.ImageConfigId}' not found");
            }

            // Generate prompt from concept
            var prompt = GenerateImagePrompt(concept, request.CustomPrompt);

            // Create generation record
            var generation = new ImageGeneration(
                Id: Guid.NewGuid().ToString(),
                Concept: concept,
                Prompt: prompt,
                ImageConfig: imageConfig,
                Status: "processing",
                Images: new List<string>(),
                Error: null,
                GeneratedAt: DateTime.UtcNow
            );

            // Store generation as a node
            var generationNode = CreateGenerationNode(generation);
            _registry.Upsert(generationNode);

            // Generate images (simplified - in real implementation, call actual image generation API)
            var images = await GenerateImagesAsync(imageConfig, prompt, request.NumberOfImages ?? 1);

            // Update generation with results
            var completedGeneration = generation with
            {
                Status = "completed",
                Images = images,
                GeneratedAt = DateTime.UtcNow
            };

            var completedNode = CreateGenerationNode(completedGeneration);
            _registry.Upsert(completedNode);

            return new ImageGenerationResponse(
                Success: true,
                Message: "Images generated successfully",
                Generation: completedGeneration,
                Images: images,
                Prompt: prompt,
                Insights: GenerateImageInsights(completedGeneration)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to generate images: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/image/config", "image-config-create", "Create or update image configuration", "codex.image.concept")]
    public async Task<object> CreateImageConfig([ApiParameter("request", "Image config request", Required = true, Location = "body")] ImageConfigRequest request)
    {
        try
        {
            var config = new ImageConfig(
                Id: request.Id ?? Guid.NewGuid().ToString(),
                Name: request.Name,
                Provider: request.Provider,
                Model: request.Model,
                ApiKey: request.ApiKey ?? "",
                BaseUrl: request.BaseUrl ?? GetDefaultBaseUrl(request.Provider),
                MaxImages: request.MaxImages ?? 4,
                ImageSize: request.ImageSize ?? "1024x1024",
                Quality: request.Quality ?? "standard",
                Style: request.Style ?? "vivid",
                Parameters: request.Parameters ?? new Dictionary<string, object>()
            );

            // Validate configuration
            var validation = await ValidateImageConfig(config);
            if (!validation.IsValid)
            {
                return new ErrorResponse($"Image configuration validation failed: {validation.ErrorMessage}");
            }

            // Store configuration
            _imageConfigs[config.Id] = config;
            var configNode = CreateImageConfigNode(config);
            _registry.Upsert(configNode);

            return new ImageConfigResponse(
                Success: true,
                Message: "Image configuration created successfully",
                Config: config,
                Validation: validation
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to create image configuration: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/image/configs", "image-configs", "Get all image configurations", "codex.image.concept")]
    public async Task<object> GetImageConfigs()
    {
        try
        {
            var configs = _imageConfigs.Values.ToList();
            return new ImageConfigsResponse(
                Success: true,
                Message: $"Retrieved {configs.Count} image configurations",
                Configs: configs
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get image configurations: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/image/concepts", "image-concepts", "Get all concepts", "codex.image.concept")]
    public async Task<object> GetConcepts()
    {
        try
        {
            var allNodes = _registry.AllNodes();
            var conceptNodes = allNodes
                .Where(n => n.TypeId == "codex.image.concept")
                .ToList();

            var concepts = new List<ConceptImage>();
            foreach (var node in conceptNodes)
            {
                if (node.Content?.InlineJson != null)
                {
                    var concept = JsonSerializer.Deserialize<ConceptImage>(node.Content.InlineJson);
                    if (concept != null)
                    {
                        concepts.Add(concept);
                    }
                }
            }

            return new ConceptsResponse(
                Success: true,
                Message: $"Retrieved {concepts.Count} concepts",
                Concepts: concepts
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get concepts: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/image/generations", "image-generations", "Get all image generations", "codex.image.concept")]
    public async Task<object> GetImageGenerations()
    {
        try
        {
            var allNodes = _registry.AllNodes();
            var generationNodes = allNodes
                .Where(n => n.TypeId == "codex.image.generation")
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("generatedAt", DateTime.MinValue))
                .ToList();

            var generations = new List<ImageGeneration>();
            foreach (var node in generationNodes)
            {
                if (node.Content?.InlineJson != null)
                {
                    var generation = JsonSerializer.Deserialize<ImageGeneration>(node.Content.InlineJson);
                    if (generation != null)
                    {
                        generations.Add(generation);
                    }
                }
            }

            return new ImageGenerationsResponse(
                Success: true,
                Message: $"Retrieved {generations.Count} image generations",
                Generations: generations
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get image generations: {ex.Message}");
        }
    }

    // Helper methods

    private void InitializeDefaultConfigs()
    {
        // OpenAI DALL-E Configuration
        _imageConfigs["dalle-3"] = new ImageConfig(
            Id: "dalle-3",
            Name: "OpenAI DALL-E 3",
            Provider: "OpenAI",
            Model: "dall-e-3",
            ApiKey: "",
            BaseUrl: "https://api.openai.com/v1",
            MaxImages: 4,
            ImageSize: "1024x1024",
            Quality: "standard",
            Style: "vivid",
            Parameters: new Dictionary<string, object>()
        );

        // Stability AI Configuration
        _imageConfigs["stability-sdxl"] = new ImageConfig(
            Id: "stability-sdxl",
            Name: "Stability AI SDXL",
            Provider: "StabilityAI",
            Model: "stable-diffusion-xl-1024-v1-0",
            ApiKey: "",
            BaseUrl: "https://api.stability.ai",
            MaxImages: 4,
            ImageSize: "1024x1024",
            Quality: "standard",
            Style: "vivid",
            Parameters: new Dictionary<string, object>()
        );

        // Local Stable Diffusion Configuration
        _imageConfigs["local-sd"] = new ImageConfig(
            Id: "local-sd",
            Name: "Local Stable Diffusion",
            Provider: "Local",
            Model: "stable-diffusion-2.1",
            ApiKey: "",
            BaseUrl: "http://localhost:7860",
            MaxImages: 4,
            ImageSize: "512x512",
            Quality: "standard",
            Style: "vivid",
            Parameters: new Dictionary<string, object>()
        );

        // Custom Local Configuration
        _imageConfigs["custom-local"] = new ImageConfig(
            Id: "custom-local",
            Name: "Custom Local Model",
            Provider: "Custom",
            Model: "custom-image-model",
            ApiKey: "",
            BaseUrl: "http://localhost:8000",
            MaxImages: 4,
            ImageSize: "1024x1024",
            Quality: "standard",
            Style: "vivid",
            Parameters: new Dictionary<string, object>()
        );
    }

    private ImageConfig? GetImageConfig(string configId)
    {
        return _imageConfigs.GetValueOrDefault(configId);
    }

    private string GetDefaultBaseUrl(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "openai" => "https://api.openai.com/v1",
            "stabilityai" => "https://api.stability.ai",
            "midjourney" => "https://api.midjourney.com",
            "local" => "http://localhost:7860",
            "custom" => "http://localhost:8000",
            _ => "http://localhost:8000"
        };
    }

    private string GenerateImagePrompt(ConceptImage concept, string? customPrompt = null)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        var prompt = $"{concept.Description}";
        
        if (concept.Style != "realistic")
        {
            prompt += $", {concept.Style} style";
        }
        
        if (concept.Mood != "neutral")
        {
            prompt += $", {concept.Mood} mood";
        }
        
        if (concept.Colors.Any())
        {
            prompt += $", colors: {string.Join(", ", concept.Colors)}";
        }
        
        if (concept.Elements.Any())
        {
            prompt += $", elements: {string.Join(", ", concept.Elements)}";
        }

        // Add quality and style modifiers based on concept type
        switch (concept.ConceptType.ToLowerInvariant())
        {
            case "spiritual":
                prompt += ", ethereal, luminous, transcendent, divine light";
                break;
            case "scientific":
                prompt += ", precise, detailed, technical, data visualization";
                break;
            case "abstract":
                prompt += ", abstract, conceptual, symbolic, artistic";
                break;
            case "emotional":
                prompt += ", expressive, emotional, heartfelt, moving";
                break;
        }

        return prompt;
    }

    private async Task<List<string>> GenerateImagesAsync(ImageConfig config, string prompt, int numberOfImages)
    {
        // This is a simplified implementation
        // In a real implementation, you would call the actual image generation APIs
        
        await Task.Delay(2000); // Simulate async operation
        
        var images = new List<string>();
        for (int i = 0; i < numberOfImages; i++)
        {
            // Simulate generated image URL
            images.Add($"https://example.com/generated-image-{Guid.NewGuid().ToString("N")[..8]}.png");
        }

        return images;
    }

    private async Task<ImageConfigValidation> ValidateImageConfig(ImageConfig config)
    {
        // Basic validation
        if (string.IsNullOrEmpty(config.Name))
        {
            return new ImageConfigValidation(false, "Name is required");
        }

        if (string.IsNullOrEmpty(config.Provider))
        {
            return new ImageConfigValidation(false, "Provider is required");
        }

        if (string.IsNullOrEmpty(config.Model))
        {
            return new ImageConfigValidation(false, "Model is required");
        }

        if (config.MaxImages <= 0)
        {
            return new ImageConfigValidation(false, "MaxImages must be greater than 0");
        }

        // Test connection (simplified)
        try
        {
            await TestImageConfigConnection(config);
            return new ImageConfigValidation(true, "Configuration is valid");
        }
        catch (Exception ex)
        {
            return new ImageConfigValidation(false, $"Connection test failed: {ex.Message}");
        }
    }

    private async Task TestImageConfigConnection(ImageConfig config)
    {
        // Simplified connection test
        await Task.Delay(100);
        
        // In a real implementation, you would make an actual API call
        if (config.Provider == "OpenAI" && string.IsNullOrEmpty(config.ApiKey))
        {
            throw new Exception("API key required for OpenAI");
        }
    }

    private List<string> GenerateConceptNextSteps(ConceptImage concept)
    {
        return new List<string>
        {
            "Generate images from this concept using the /image/generate endpoint",
            "Experiment with different image generation models",
            "Try different styles and moods for the same concept",
            "Share the concept with others for feedback",
            "Create variations of this concept"
        };
    }

    private List<string> GenerateImageInsights(ImageGeneration generation)
    {
        return new List<string>
        {
            $"Generated {generation.Images.Count} image(s) using {generation.ImageConfig.Provider} {generation.ImageConfig.Model}",
            $"Concept: {generation.Concept.Title}",
            $"Style: {generation.Concept.Style}, Mood: {generation.Concept.Mood}",
            $"Generated at: {generation.GeneratedAt:yyyy-MM-dd HH:mm:ss}",
            $"Status: {generation.Status}"
        };
    }

    // Node creation methods
    private Node CreateImageConfigNode(ImageConfig config)
    {
        return new Node(
            Id: config.Id,
            TypeId: "codex.image.config",
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
                ["maxImages"] = config.MaxImages,
                ["imageSize"] = config.ImageSize
            }
        );
    }

    private Node CreateConceptNode(ConceptImage concept)
    {
        return new Node(
            Id: concept.Id,
            TypeId: "codex.image.concept",
            State: ContentState.Water,
            Locale: "en",
            Title: concept.Title,
            Description: concept.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(concept),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["conceptType"] = concept.ConceptType,
                ["style"] = concept.Style,
                ["mood"] = concept.Mood,
                ["colorCount"] = concept.Colors.Count,
                ["elementCount"] = concept.Elements.Count
            }
        );
    }

    private Node CreateGenerationNode(ImageGeneration generation)
    {
        return new Node(
            Id: generation.Id,
            TypeId: "codex.image.generation",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Image Generation: {generation.Concept.Title}",
            Description: $"Status: {generation.Status}, Images: {generation.Images.Count}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(generation),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["conceptId"] = generation.Concept.Id,
                ["imageConfigId"] = generation.ImageConfig.Id,
                ["status"] = generation.Status,
                ["imageCount"] = generation.Images.Count,
                ["generatedAt"] = generation.GeneratedAt
            }
        );
    }
}

// Data types
public record ImageConfigValidation(bool IsValid, string ErrorMessage);

// Request/Response Types
[ResponseType("codex.image.concept-response", "ConceptImageResponse", "Concept image response")]
public record ConceptImageResponse(
    bool Success,
    string Message,
    ConceptImage Concept,
    List<string> NextSteps
);

[ResponseType("codex.image.generation-response", "ImageGenerationResponse", "Image generation response")]
public record ImageGenerationResponse(
    bool Success,
    string Message,
    ImageGeneration Generation,
    List<string> Images,
    string Prompt,
    List<string> Insights
);

[ResponseType("codex.image.config-response", "ImageConfigResponse", "Image config response")]
public record ImageConfigResponse(
    bool Success,
    string Message,
    ImageConfig Config,
    ImageConfigValidation Validation
);

[ResponseType("codex.image.configs-response", "ImageConfigsResponse", "Image configs response")]
public record ImageConfigsResponse(
    bool Success,
    string Message,
    List<ImageConfig> Configs
);

[ResponseType("codex.image.concepts-response", "ConceptsResponse", "Concepts response")]
public record ConceptsResponse(
    bool Success,
    string Message,
    List<ConceptImage> Concepts
);

[ResponseType("codex.image.generations-response", "ImageGenerationsResponse", "Image generations response")]
public record ImageGenerationsResponse(
    bool Success,
    string Message,
    List<ImageGeneration> Generations
);

[RequestType("codex.image.concept-creation-request", "ConceptCreationRequest", "Concept creation request")]
public record ConceptCreationRequest(
    string Title,
    string Description,
    string ConceptType,
    string Style,
    string Mood,
    List<string>? Colors = null,
    List<string>? Elements = null,
    Dictionary<string, object>? Metadata = null
);

[RequestType("codex.image.generation-request", "ImageGenerationRequest", "Image generation request")]
public record ImageGenerationRequest(
    string ConceptId,
    string ImageConfigId,
    int? NumberOfImages = null,
    string? CustomPrompt = null
);

[RequestType("codex.image.config-request", "ImageConfigRequest", "Image config request")]
public record ImageConfigRequest(
    string Name,
    string Provider,
    string Model,
    string? Id = null,
    string? ApiKey = null,
    string? BaseUrl = null,
    int? MaxImages = null,
    string? ImageSize = null,
    string? Quality = null,
    string? Style = null,
    Dictionary<string, object>? Parameters = null
);
