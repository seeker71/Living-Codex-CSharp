using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Concept Image Generation Data Types

[MetaNode("codex.image.config", "codex.meta/type", "ImageConfig", "Configuration for image generation models")]
[ApiType(
    name: "Image Configuration",
    description: "Configuration settings for image generation models (DALL-E, Stable Diffusion, Midjourney, Custom)",
    example: """
    {
      "id": "dalle-3",
      "name": "OpenAI DALL-E 3",
      "provider": "OpenAI",
      "model": "dall-e-3",
      "apiKey": "sk-...",
      "baseUrl": "https://api.openai.com/v1",
      "maxImages": 4,
      "imageSize": "1024x1024",
      "quality": "standard",
      "style": "vivid"
    }
    """
)]
public record ImageConfig(
    [MetaNodeField("id", "string", Required = true, Description = "Unique identifier for the image configuration")]
    string Id,
    
    [MetaNodeField("name", "string", Required = true, Description = "Human-readable name for the configuration")]
    string Name,
    
    [MetaNodeField("provider", "string", Required = true, Description = "Image generation provider", Kind = "Enum", EnumValues = new[] { "OpenAI", "StabilityAI", "Midjourney", "Custom", "Local" })]
    string Provider,
    
    [MetaNodeField("model", "string", Required = true, Description = "Specific model name (e.g., dall-e-3, stable-diffusion-xl, midjourney-v6)")]
    string Model,
    
    [MetaNodeField("apiKey", "string", Description = "API key for the image provider (optional for local models)")]
    string ApiKey,
    
    [MetaNodeField("baseUrl", "string", Description = "Base URL for the image generation API endpoint")]
    string BaseUrl,
    
    [MetaNodeField("maxImages", "number", Required = true, Description = "Maximum number of images to generate per request", MinValue = 1, MaxValue = 10)]
    int MaxImages,
    
    [MetaNodeField("imageSize", "string", Required = true, Description = "Image dimensions", Kind = "Enum", EnumValues = new[] { "256x256", "512x512", "1024x1024", "1792x1024", "1024x1792" })]
    string ImageSize,
    
    [MetaNodeField("quality", "string", Description = "Image quality setting", Kind = "Enum", EnumValues = new[] { "standard", "hd" })]
    string Quality,
    
    [MetaNodeField("style", "string", Description = "Image style setting", Kind = "Enum", EnumValues = new[] { "vivid", "natural" })]
    string Style,
    
    [MetaNodeField("parameters", "object", Description = "Additional provider-specific parameters", Kind = "Object")]
    Dictionary<string, object> Parameters
);

[MetaNode("codex.image.concept", "codex.meta/type", "ConceptImage", "Concept to be rendered as an image")]
[ApiType(
    name: "Concept Image",
    description: "A concept that can be rendered into an image using AI image generation",
    example: """
    {
      "id": "concept-123",
      "title": "Joy Amplification",
      "description": "A visualization of joy being amplified through frequency resonance",
      "conceptType": "spiritual",
      "style": "abstract",
      "mood": "uplifting",
      "colors": ["gold", "white", "rainbow"],
      "elements": ["light", "energy", "frequency", "heart"]
    }
    """
)]
public record ConceptImage(
    [MetaNodeField("id", "string", Required = true, Description = "Unique identifier for the concept")]
    string Id,
    
    [MetaNodeField("title", "string", Required = true, Description = "Title of the concept")]
    string Title,
    
    [MetaNodeField("description", "string", Required = true, Description = "Detailed description of the concept to visualize")]
    string Description,
    
    [MetaNodeField("conceptType", "string", Required = true, Description = "Type of concept", Kind = "Enum", EnumValues = new[] { "spiritual", "scientific", "abstract", "concrete", "emotional", "technical" })]
    string ConceptType,
    
    [MetaNodeField("style", "string", Required = true, Description = "Visual style for the image", Kind = "Enum", EnumValues = new[] { "realistic", "abstract", "minimalist", "detailed", "artistic", "scientific" })]
    string Style,
    
    [MetaNodeField("mood", "string", Required = true, Description = "Emotional mood of the image", Kind = "Enum", EnumValues = new[] { "uplifting", "calm", "energetic", "mysterious", "peaceful", "powerful" })]
    string Mood,
    
    [MetaNodeField("colors", "array", Description = "Preferred color palette", Kind = "Array", ArrayItemType = "string")]
    List<string> Colors,
    
    [MetaNodeField("elements", "array", Description = "Key visual elements to include", Kind = "Array", ArrayItemType = "string")]
    List<string> Elements,
    
    [MetaNodeField("metadata", "object", Description = "Additional metadata for the concept", Kind = "Object")]
    Dictionary<string, object> Metadata
);

[MetaNode("codex.image.generation", "codex.meta/type", "ImageGeneration", "Image generation request and result")]
[ApiType(
    name: "Image Generation",
    description: "Request and result for generating images from concepts",
    example: """
    {
      "id": "gen-456",
      "concept": { "id": "concept-123" },
      "prompt": "A beautiful visualization of joy amplification...",
      "imageConfig": { "id": "dalle-3" },
      "status": "completed",
      "images": ["https://example.com/image1.png"],
      "generatedAt": "2025-01-27T10:30:00Z"
    }
    """
)]
public record ImageGeneration(
    [MetaNodeField("id", "string", Required = true, Description = "Unique identifier for the generation")]
    string Id,
    
    [MetaNodeField("concept", "ConceptImage", Required = true, Description = "The concept being visualized", Kind = "Reference", ReferenceType = "ConceptImage")]
    ConceptImage Concept,
    
    [MetaNodeField("prompt", "string", Required = true, Description = "The final prompt sent to the image generation model")]
    string Prompt,
    
    [MetaNodeField("imageConfig", "ImageConfig", Required = true, Description = "Image generation configuration used", Kind = "Reference", ReferenceType = "ImageConfig")]
    ImageConfig ImageConfig,
    
    [MetaNodeField("status", "string", Required = true, Description = "Generation status", Kind = "Enum", EnumValues = new[] { "pending", "processing", "completed", "failed" })]
    string Status,
    
    [MetaNodeField("images", "array", Description = "Generated image URLs or base64 data", Kind = "Array", ArrayItemType = "string")]
    List<string> Images,
    
    [MetaNodeField("error", "string", Description = "Error message if generation failed")]
    string? Error,
    
    [MetaNodeField("generatedAt", "string", Required = true, Description = "When the generation was completed")]
    DateTime GeneratedAt
);

/// <summary>
/// Concept Image Generation Module - Renders concepts into images using configurable image generation models
/// </summary>
[MetaNode(
    id: "codex.image.concept-module",
    typeId: "codex.meta/module",
    name: "Concept Image Generation Module",
    description: "Renders concepts into images using configurable local and remote image generation models"
)]
[ApiModule(
    name: "Concept Image Generation",
    version: "1.0.0",
    description: "Configurable image generation for visualizing concepts",
    basePath: "/image",
    tags: new[] { "Image Generation", "AI", "Visualization", "Concepts", "Art" }
)]
public class ConceptImageModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private readonly Dictionary<string, ImageConfig> _imageConfigs;

    public ConceptImageModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _imageConfigs = new Dictionary<string, ImageConfig>();
        InitializeDefaultConfigs();
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.image.concept",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Image Generation Module",
            Description: "Renders concepts into images using configurable image generation models",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.image.concept",
                    Name = "Concept Image Generation Module",
                    Description = "Configurable image generation for visualizing concepts",
                    Version = "1.0.0",
                    SupportedProviders = new[] { "OpenAI", "StabilityAI", "Midjourney", "Custom", "Local" },
                    Capabilities = new[] { "ImageGeneration", "ConceptVisualization", "ConfigurableProviders", "LocalAndRemote" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.image.concept",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "AI-powered concept visualization"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
        
        // Register default image configurations
        foreach (var config in _imageConfigs.Values)
        {
            var configNode = CreateImageConfigNode(config);
            registry.Upsert(configNode);
        }
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    [ApiRoute("POST", "/image/concept/create", "image-concept-create", "Create a concept for image generation", "codex.image.concept")]
    [ApiDocumentation(
        summary: "Create a concept for image generation",
        description: "Creates a new concept that can be rendered into an image using AI image generation",
        operationId: "createConcept",
        tags: new[] { "Concepts", "Image Generation", "Creation" },
        responses: new[] {
            "200:ConceptImageResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
    [ApiDocumentation(
        summary: "Generate images from a concept",
        description: "Generates images from a concept using configurable image generation models",
        operationId: "generateImages",
        tags: new[] { "Image Generation", "AI", "Visualization" },
        responses: new[] {
            "200:ImageGenerationResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
    [ApiDocumentation(
        summary: "Create or update image configuration",
        description: "Configures a new image generation provider (OpenAI, StabilityAI, Midjourney, Custom, Local)",
        operationId: "createImageConfig",
        tags: new[] { "Configuration", "Image Generation", "Setup" },
        responses: new[] {
            "200:ImageConfigResponse:Success",
            "400:ErrorResponse:Bad Request",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
    [ApiDocumentation(
        summary: "Get all image configurations",
        description: "Retrieves all configured image generation providers and their settings",
        operationId: "getImageConfigs",
        tags: new[] { "Configuration", "Image Generation", "List" },
        responses: new[] {
            "200:ImageConfigsResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
    [ApiDocumentation(
        summary: "Get all concepts",
        description: "Retrieves all created concepts available for image generation",
        operationId: "getConcepts",
        tags: new[] { "Concepts", "List" },
        responses: new[] {
            "200:ConceptsResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
    [ApiDocumentation(
        summary: "Get all image generations",
        description: "Retrieves all image generation requests and their results",
        operationId: "getImageGenerations",
        tags: new[] { "Image Generation", "History", "List" },
        responses: new[] {
            "200:ImageGenerationsResponse:Success",
            "500:ErrorResponse:Internal Server Error"
        }
    )]
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
