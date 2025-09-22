using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Gallery Module - Visual expressions of consciousness and creativity
/// Manages visual content including images, videos, and interactive media
/// </summary>
[MetaNode(Id = "codex.gallery", Name = "Gallery Module", Description = "Visual expressions of consciousness and creativity")]
public sealed class GalleryModule : ModuleBase
{
    public override string Name => "Gallery Module";
    public override string Description => "Visual expressions of consciousness and creativity";
    public override string Version => "1.0.0";

    private new IApiRouter? _apiRouter;
    private new CoreApiService? _coreApiService;

    public GalleryModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.gallery",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "gallery", "visual", "creativity", "art", "media", "consciousness" },
            capabilities: new[] { 
                "media-upload", "visual-discovery", "creative-expression",
                "resonance-scoring", "ai-generation"
            },
            spec: "codex.spec.gallery"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Gallery Module API handlers registered");
        _apiRouter = router;
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Gallery Module HTTP endpoints registered");
        _coreApiService = coreApi;
    }

    // Public methods for direct testing
    public async Task<object> ListGalleryItems(int? limit = 20, string? filter = null, string? sort = "resonance")
    {
        return await ListGalleryItemsImpl(limit, filter, sort);
    }

    public async Task<object> CreateGalleryItem(GalleryItemCreateRequest request)
    {
        return await CreateGalleryItemImpl(request);
    }

    public async Task<object> GetGalleryItem(string itemId)
    {
        return await GetGalleryItemImpl(itemId);
    }

    public async Task<object> GenerateAIImage(AIImageGenerateRequest request)
    {
        return await GenerateAIImageImpl(request);
    }

    // List gallery items
    [ApiRoute("GET", "/gallery/list", "list-gallery-items", "Get list of gallery items", "codex.gallery")]
    public async Task<object> ListGalleryItemsImpl(
        [ApiParameter("limit", "Number of items to return", Required = false)] int? limit = 20,
        [ApiParameter("filter", "Filter by axis", Required = false)] string? filter = null,
        [ApiParameter("sort", "Sort order", Required = false)] string? sort = "resonance")
    {
        try
        {
            var galleryNodes = _registry.AllNodes()
                .Where(n => n.TypeId == "codex.gallery.item")
                .ToList();

            // Apply filter if specified
            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                galleryNodes = galleryNodes.Where(n => 
                {
                    var axes = GetItemAxes(n);
                    return axes.Contains(filter);
                }).ToList();
            }

            // Apply sorting
            switch (sort)
            {
                case "recent":
                    galleryNodes = galleryNodes.OrderByDescending(n => n.Meta?.GetValueOrDefault("createdAt")).ToList();
                    break;
                case "popular":
                    galleryNodes = galleryNodes.OrderByDescending(n => GetPopularityScore(n)).ToList();
                    break;
                default: // resonance
                    galleryNodes = galleryNodes.OrderByDescending(n => GetItemResonance(n)).ToList();
                    break;
            }

            var items = galleryNodes.Take(limit ?? 20).Select(n => new
            {
                id = n.Id,
                title = n.Title,
                description = n.Description,
                imageUrl = n.Meta?.GetValueOrDefault("imageUrl")?.ToString(),
                thumbnailUrl = n.Meta?.GetValueOrDefault("thumbnailUrl")?.ToString(),
                author = new
                {
                    id = n.Meta?.GetValueOrDefault("authorId")?.ToString() ?? "",
                    name = n.Meta?.GetValueOrDefault("authorName")?.ToString() ?? "Unknown",
                    avatar = n.Meta?.GetValueOrDefault("authorAvatar")?.ToString()
                },
                createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString(),
                resonance = GetItemResonance(n),
                axes = GetItemAxes(n),
                tags = GetItemTags(n),
                mediaType = n.Meta?.GetValueOrDefault("mediaType")?.ToString() ?? "image",
                dimensions = GetItemDimensions(n),
                aiGenerated = n.Meta?.GetValueOrDefault("aiGenerated")?.ToString() == "true",
                prompt = n.Meta?.GetValueOrDefault("aiPrompt")?.ToString()
            }).ToList();

            return new { success = true, items, totalCount = items.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing gallery items: {ex.Message}", ex);
            return new ErrorResponse($"Error listing gallery items: {ex.Message}");
        }
    }

    // Create gallery item
    [ApiRoute("POST", "/gallery/create", "create-gallery-item", "Create a new gallery item", "codex.gallery")]
    public async Task<object> CreateGalleryItemImpl([ApiParameter("body", "Gallery item creation request", Required = true, Location = "body")] GalleryItemCreateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.ImageUrl))
            {
                return new ErrorResponse("Title and image URL are required");
            }

            var itemId = $"gallery-item-{Guid.NewGuid():N}";
            var itemNode = new Node(
                Id: itemId,
                TypeId: "codex.gallery.item",
                State: ContentState.Water,
                Locale: "en-US",
                Title: request.Title,
                Description: request.Description ?? "",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request),
                    InlineBytes: null,
                    ExternalUri: !string.IsNullOrEmpty(request.ImageUrl) ? new Uri(request.ImageUrl) : null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["authorId"] = request.AuthorId ?? "",
                    ["authorName"] = request.AuthorName ?? "Anonymous",
                    ["authorAvatar"] = request.AuthorAvatar ?? "",
                    ["createdAt"] = DateTime.UtcNow,
                    ["imageUrl"] = request.ImageUrl,
                    ["thumbnailUrl"] = request.ThumbnailUrl ?? request.ImageUrl,
                    ["axes"] = request.Axes ?? new[] { "consciousness", "unity" },
                    ["tags"] = request.Tags ?? new string[0],
                    ["mediaType"] = request.MediaType ?? "image",
                    ["dimensions"] = request.Dimensions ?? new { width = 800, height = 600 },
                    ["aiGenerated"] = request.AiGenerated ?? false,
                    ["aiPrompt"] = request.AiPrompt ?? ""
                }
            );

            _registry.Upsert(itemNode);

            // Integrate with AI module to enrich item metadata (concepts/tags/quality)
            try
            {
                if (_apiRouter != null)
                {
                    // Concept extraction
                    var extractReq = new {
                        text = $"{request.Title}\n\n{request.Description}",
                        maxConcepts = 10
                    };
                    var extractJson = JsonSerializer.SerializeToElement(extractReq);
                    if (_apiRouter.TryGetHandler("ai", "extract-concepts", out var extractHandler))
                    {
                        var extractResult = await extractHandler(extractJson);
                        var extractDoc = JsonSerializer.Serialize(extractResult);
                        var extractElement = JsonSerializer.Deserialize<JsonElement>(extractDoc);
                        var concepts = extractElement.TryGetProperty("Concepts", out var c) ? c : default;

                        // Scoring analysis
                        var scoreReq = new {
                            text = request.Description ?? request.Title,
                            axes = new[] { "consciousness", "unity", "resonance" }
                        };
                        var scoreJson = JsonSerializer.SerializeToElement(scoreReq);
                        double qualityScore = 0.5;
                        if (_apiRouter.TryGetHandler("ai", "score-analysis", out var scoreHandler))
                        {
                            var scoreResult = await scoreHandler(scoreJson);
                            var scoreDoc = JsonSerializer.Serialize(scoreResult);
                            var scoreElem = JsonSerializer.Deserialize<JsonElement>(scoreDoc);
                            if (scoreElem.TryGetProperty("OverallScore", out var overall))
                            {
                                qualityScore = overall.GetDouble();
                            }
                        }

                        // Update node with AI metadata
                        var updatedMeta = new Dictionary<string, object>(itemNode.Meta ?? new Dictionary<string, object>())
                        {
                            ["concepts"] = concepts.ValueKind != JsonValueKind.Undefined ? concepts : JsonSerializer.SerializeToElement(Array.Empty<string>()),
                            ["qualityScore"] = qualityScore
                        };
                        var updatedNode = itemNode with { Meta = updatedMeta };
                        _registry.Upsert(updatedNode);
                    }
                }
            }
            catch (Exception aiEx)
            {
                _logger.Warn($"Gallery AI enrichment failed: {aiEx.Message}");
            }

            return new 
            { 
                success = true, 
                itemId = itemId,
                message = "Gallery item created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating gallery item: {ex.Message}", ex);
            return new ErrorResponse($"Error creating gallery item: {ex.Message}");
        }
    }

    // Get gallery item details
    [ApiRoute("GET", "/gallery/{itemId}", "get-gallery-item", "Get detailed information about a gallery item", "codex.gallery")]
    public async Task<object> GetGalleryItemImpl([ApiParameter("itemId", "Gallery item ID", Required = true, Location = "path")] string itemId)
    {
        try
        {
            if (!_registry.TryGet(itemId, out var itemNode))
            {
                return new ErrorResponse("Gallery item not found");
            }

            return new
            {
                success = true,
                item = new
                {
                    id = itemNode.Id,
                    title = itemNode.Title,
                    description = itemNode.Description,
                    imageUrl = itemNode.Meta?.GetValueOrDefault("imageUrl")?.ToString(),
                    thumbnailUrl = itemNode.Meta?.GetValueOrDefault("thumbnailUrl")?.ToString(),
                    author = new
                    {
                        id = itemNode.Meta?.GetValueOrDefault("authorId")?.ToString() ?? "",
                        name = itemNode.Meta?.GetValueOrDefault("authorName")?.ToString() ?? "Unknown",
                        avatar = itemNode.Meta?.GetValueOrDefault("authorAvatar")?.ToString()
                    },
                    createdAt = itemNode.Meta?.GetValueOrDefault("createdAt")?.ToString(),
                    resonance = GetItemResonance(itemNode),
                    axes = GetItemAxes(itemNode),
                    tags = GetItemTags(itemNode),
                    mediaType = itemNode.Meta?.GetValueOrDefault("mediaType")?.ToString() ?? "image",
                    dimensions = GetItemDimensions(itemNode),
                    aiGenerated = itemNode.Meta?.GetValueOrDefault("aiGenerated")?.ToString() == "true",
                    prompt = itemNode.Meta?.GetValueOrDefault("aiPrompt")?.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting gallery item: {ex.Message}", ex);
            return new ErrorResponse($"Error getting gallery item: {ex.Message}");
        }
    }

    // Generate AI image
    [ApiRoute("POST", "/gallery/generate", "generate-ai-image", "Generate an AI image for the gallery", "codex.gallery")]
    public async Task<object> GenerateAIImageImpl([ApiParameter("body", "AI image generation request", Required = true, Location = "body")] AIImageGenerateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Prompt))
            {
                return new ErrorResponse("Prompt is required for AI image generation");
            }

            // Use AI module to refine the prompt via fractal-transform
            try
            {
                if (_apiRouter != null && _apiRouter.TryGetHandler("ai", "fractal-transform", out var ftHandler))
                {
                    var ftReq = new { input = request.Prompt, style = request.Style ?? "consciousness" };
                    var ftJson = JsonSerializer.SerializeToElement(ftReq);
                    var ftResult = await ftHandler(ftJson);
                    var ftDoc = JsonSerializer.Serialize(ftResult);
                    var ftElem = JsonSerializer.Deserialize<JsonElement>(ftDoc);
                    if (ftElem.TryGetProperty("TransformedText", out var transformed) && transformed.ValueKind == JsonValueKind.String)
                    {
                        request.Prompt = transformed.GetString() ?? request.Prompt;
                    }
                }
            }
            catch (Exception aiEx)
            {
                _logger.Warn($"AI prompt refinement failed: {aiEx.Message}");
            }

            // Use the ConceptImageModule for AI generation
            var imageResponse = await GenerateImageWithAI(request.Prompt, request.Style ?? "consciousness");
            
            if (imageResponse.Success)
            {
                // Create gallery item from generated image
                var createRequest = new GalleryItemCreateRequest
                {
                    Title = request.Title ?? $"AI Generated: {request.Prompt}",
                    Description = request.Description ?? $"AI-generated image based on: {request.Prompt}",
                    ImageUrl = imageResponse.ImageUrl,
                    ThumbnailUrl = imageResponse.ThumbnailUrl ?? imageResponse.ImageUrl,
                    AuthorId = request.AuthorId,
                    AuthorName = request.AuthorName ?? "AI Creator",
                    AuthorAvatar = request.AuthorAvatar,
                    Axes = request.Axes ?? new[] { "consciousness", "innovation" },
                    Tags = new[] { "ai-generated", "consciousness", "creativity" },
                    MediaType = "image",
                    AiGenerated = true,
                    AiPrompt = request.Prompt
                };

                var createResult = await CreateGalleryItemImpl(createRequest);
                return createResult;
            }
            else
            {
                return new ErrorResponse($"AI image generation failed: {imageResponse.Error}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating AI image: {ex.Message}", ex);
            return new ErrorResponse($"Error generating AI image: {ex.Message}");
        }
    }

    // Helper methods
    private double GetItemResonance(Node itemNode)
    {
        // Calculate resonance based on content quality, engagement, and axes alignment
        var baseResonance = 0.5;
        
        // Boost for AI-generated content
        if (itemNode.Meta?.GetValueOrDefault("aiGenerated")?.ToString() == "true")
        {
            baseResonance += 0.1;
        }
        
        // Boost for consciousness-related content
        var axes = GetItemAxes(itemNode);
        if (axes.Contains("consciousness"))
            baseResonance += 0.1;
        if (axes.Contains("resonance"))
            baseResonance += 0.1;
            
        return Math.Min(0.95, baseResonance);
    }

    private double GetPopularityScore(Node itemNode)
    {
        // Real popularity calculation based on engagement metrics
        var resonance = GetItemResonance(itemNode);
        var meta = itemNode.Meta ?? new Dictionary<string, object>();
        
        // Extract engagement metrics from node metadata
        var views = Convert.ToInt32(meta.GetValueOrDefault("views", 0));
        var likes = Convert.ToInt32(meta.GetValueOrDefault("likes", 0));
        var comments = Convert.ToInt32(meta.GetValueOrDefault("comments", 0));
        var shares = Convert.ToInt32(meta.GetValueOrDefault("shares", 0));
        var downloads = Convert.ToInt32(meta.GetValueOrDefault("downloads", 0));
        
        // Calculate engagement score with weighted metrics
        var engagementScore = 
            (views * 0.1) +           // Views have lower weight
            (likes * 1.0) +           // Likes are primary engagement
            (comments * 2.0) +        // Comments show deeper engagement
            (shares * 3.0) +          // Shares indicate high value
            (downloads * 2.5);        // Downloads show utility
        
        // Normalize engagement score (log scale to prevent extreme values)
        var normalizedEngagement = Math.Log10(engagementScore + 1) / 10.0;
        
        // Calculate age factor for recency boost
        var createdAt = meta.GetValueOrDefault("createdAt");
        var ageInDays = createdAt != null 
            ? (DateTime.UtcNow - Convert.ToDateTime(createdAt)).TotalDays
            : 0;
        var recencyBoost = Math.Max(0, 1 - (ageInDays / 30)) * 0.15;
        
        // Calculate quality factor based on AI analysis if available
        var qualityScore = Convert.ToDouble(meta.GetValueOrDefault("qualityScore", 0.5));
        var qualityFactor = qualityScore * 0.2;
        
        // Combine all factors: base resonance + engagement + recency + quality
        var totalScore = resonance + normalizedEngagement + recencyBoost + qualityFactor;
        
        // Cap at maximum score
        return Math.Min(1.0, totalScore);
    }

    private string[] GetItemAxes(Node itemNode)
    {
        var axesMeta = itemNode.Meta?.GetValueOrDefault("axes");
        if (axesMeta is string[] axes)
        {
            return axes;
        }
        return new[] { "consciousness", "unity" };
    }

    private string[] GetItemTags(Node itemNode)
    {
        var tagsMeta = itemNode.Meta?.GetValueOrDefault("tags");
        if (tagsMeta is string[] tags)
        {
            return tags;
        }
        return new string[0];
    }

    private object GetItemDimensions(Node itemNode)
    {
        var dimensionsMeta = itemNode.Meta?.GetValueOrDefault("dimensions");
        if (dimensionsMeta != null)
        {
            return dimensionsMeta;
        }
        return new { width = 800, height = 600 };
    }

    private async Task<AIImageResponse> GenerateImageWithAI(string prompt, string style)
    {
        try
        {
            // Integrate with AI module for image generation
            var aiImageResult = await GenerateAIImage(prompt, style, "1024x1024");
            
            if (aiImageResult.Success)
            {
                // Create gallery item for the generated image
                var galleryItem = new GalleryItemCreateRequest
                {
                    Title = $"AI Generated: {prompt}",
                    Description = $"AI-generated image using {style} style",
                    ImageUrl = aiImageResult.ImageUrl,
                    Tags = new[] { "ai-generated", style, "dalle", "generated-content" },
                    AuthorId = "ai-system"
                };

                var createResult = await CreateGalleryItem(galleryItem);
                
                return new AIImageResponse
                {
                    Success = true,
                    ImageUrl = aiImageResult.ImageUrl,
                    ThumbnailUrl = aiImageResult.ImageUrl // Use same URL for thumbnail
                };
            }
            else
            {
                return new AIImageResponse
                {
                    Success = false,
                    Error = aiImageResult.Error ?? "Failed to generate AI image"
                };
            }
        }
        catch (Exception ex)
        {
            return new AIImageResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<(bool Success, string? ImageUrl, string? Error)> GenerateAIImage(string prompt, string style, string size)
    {
        try
        {
            // Check if AI module is available
            var aiModule = GetAIModule();
            if (aiModule != null)
            {
                // Use AI module for image generation
                var imageResult = await CallAIModuleForImageGeneration(aiModule, prompt, style, size);
                return imageResult;
            }

            // Fallback to external AI service (e.g., OpenAI DALL-E)
            return await GenerateImageWithExternalService(prompt, style, size);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to generate AI image: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    private object? GetAIModule()
    {
        // Try to get AI module from the registry
        try
        {
            var aiModuleNode = _registry.AllNodes()
                .FirstOrDefault(n => n.TypeId == "codex.module" && n.Title?.Contains("AI") == true);
            return aiModuleNode;
        }
        catch
        {
            return null;
        }
    }

    private async Task<(bool Success, string? ImageUrl, string? Error)> CallAIModuleForImageGeneration(object aiModule, string prompt, string style, string size)
    {
        try
        {
            // Route to AI module's image generation handler via internal router
            if (_apiRouter != null && _apiRouter.TryGetHandler("ai", "generate-image", out var handler))
            {
                var request = new { prompt, style, size };
                var args = JsonSerializer.SerializeToElement(request);
                var result = await handler(args);
                var json = JsonSerializer.Serialize(result);
                var elem = JsonSerializer.Deserialize<JsonElement>(json);
                var success = elem.TryGetProperty("success", out var s) && s.GetBoolean();
                string? imageUrl = null;
                if (elem.TryGetProperty("imageUrl", out var iu) && iu.ValueKind == JsonValueKind.String)
                {
                    imageUrl = iu.GetString();
                }
                string? error = null;
                if (elem.TryGetProperty("error", out var er) && er.ValueKind == JsonValueKind.String)
                {
                    error = er.GetString();
                }
                return (success, imageUrl, error ?? (success ? null : "AI image generation failed"));
            }

            return (false, null, "AI image generation handler not available");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private async Task<(bool Success, string? ImageUrl, string? Error)> GenerateImageWithExternalService(string prompt, string style, string size)
    {
        try
        {
            // No external placeholder: route through AI module; if unavailable, return failure
            if (_apiRouter != null && _apiRouter.TryGetHandler("ai", "generate-image", out var handler))
            {
                var request = new { prompt, style, size };
                var args = JsonSerializer.SerializeToElement(request);
                var result = await handler(args);
                var json = JsonSerializer.Serialize(result);
                var elem = JsonSerializer.Deserialize<JsonElement>(json);
                var success = elem.TryGetProperty("success", out var s) && s.GetBoolean();
                string? imageUrl = null;
                if (elem.TryGetProperty("imageUrl", out var iu) && iu.ValueKind == JsonValueKind.String)
                {
                    imageUrl = iu.GetString();
                }
                string? error = null;
                if (elem.TryGetProperty("error", out var er) && er.ValueKind == JsonValueKind.String)
                {
                    error = er.GetString();
                }
                return (success, imageUrl, error ?? (success ? null : "AI image generation failed"));
            }

            return (false, null, "No external image generation configured");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

// Data structures for gallery
[MetaNode(Id = "codex.gallery.create-request", Name = "Gallery Item Create Request", Description = "Request to create a new gallery item")]
public record GalleryItemCreateRequest
{
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string ImageUrl { get; init; } = "";
    public string? ThumbnailUrl { get; init; }
    public string? AuthorId { get; init; }
    public string? AuthorName { get; init; }
    public string? AuthorAvatar { get; init; }
    public string[]? Axes { get; init; }
    public string[]? Tags { get; init; }
    public string? MediaType { get; init; }
    public object? Dimensions { get; init; }
    public bool? AiGenerated { get; init; }
    public string? AiPrompt { get; init; }
}

[MetaNode(Id = "codex.gallery.ai-generate-request", Name = "AI Image Generate Request", Description = "Request to generate an AI image")]
public record AIImageGenerateRequest
{
    public string Prompt { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Style { get; set; }
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
    public string[]? Axes { get; set; }
}

public record AIImageResponse
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Error { get; set; }
}
