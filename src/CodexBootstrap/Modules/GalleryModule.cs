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
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Gallery Module HTTP endpoints registered");
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
        // Simple popularity calculation - in a real system this would be based on views, likes, etc.
        var resonance = GetItemResonance(itemNode);
        var ageInDays = (DateTime.UtcNow - Convert.ToDateTime(itemNode.Meta?.GetValueOrDefault("createdAt") ?? DateTime.UtcNow)).TotalDays;
        
        // Boost recent content slightly
        var recencyBoost = Math.Max(0, 1 - (ageInDays / 30)) * 0.1;
        
        return resonance + recencyBoost;
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
            // This would integrate with the ConceptImageModule or external AI service
            // For now, return a placeholder response
            return new AIImageResponse
            {
                Success = false,
                Error = "AI image generation not yet implemented"
            };
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
