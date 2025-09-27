using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Module responsible for managing shared metadata as meta-nodes to avoid repetition
/// </summary>
[MetaNode(Id = "codex.shared-metadata", Name = "Shared Metadata Module", Description = "Manages shared metadata as meta-nodes")]
public sealed class SharedMetadataModule : ModuleBase
{
    public override string Name => "Shared Metadata Module";
    public override string Description => "Manages shared metadata as meta-nodes to avoid repetition";
    public override string Version => "1.0.0";

    public SharedMetadataModule(INodeRegistry registry, ICodexLogger logger) : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.shared-metadata",
            name: "Shared Metadata Module",
            version: "1.0.0",
            description: "Manages shared metadata as meta-nodes to avoid repetition",
            tags: new[] { "metadata", "meta-nodes", "shared", "optimization" },
            capabilities: new[] { "metadata-management", "meta-node-creation", "shared-data" },
            spec: "codex.spec.shared-metadata"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        router.Register("codex.shared-metadata", "create-shared-metadata", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing request body");
            var request = JsonSerializer.Deserialize<CreateSharedMetadataRequest>(args.Value.GetRawText());
            if (request == null) return new ErrorResponse("Invalid request");
            return await CreateSharedMetadataAsync(request);
        });

        router.Register("codex.shared-metadata", "get-shared-metadata", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing metadata ID");
            var metadataId = args.Value.GetString();
            if (string.IsNullOrEmpty(metadataId)) return new ErrorResponse("Invalid metadata ID");
            return await GetSharedMetadataAsync(metadataId);
        });

        router.Register("codex.shared-metadata", "link-node-to-metadata", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing request body");
            var request = JsonSerializer.Deserialize<LinkNodeToMetadataRequest>(args.Value.GetRawText());
            if (request == null) return new ErrorResponse("Invalid request");
            return await LinkNodeToMetadataAsync(request);
        });
    }

    [ApiRoute("POST", "/shared-metadata/create", "Create Shared Metadata", "Creates a new shared metadata meta-node", "codex.shared-metadata")]
    public async Task<object> CreateSharedMetadataAsync([ApiParameter("request", "Shared metadata creation request", Required = true, Location = "body")] CreateSharedMetadataRequest request)
    {
        try
        {
            var metadataId = $"codex.shared-metadata.{request.Category}.{request.Key}.{Guid.NewGuid():N}";
            
            // Check if metadata already exists
            var existingNode = _registry.GetNode(metadataId);
            if (existingNode != null)
            {
                return new ErrorResponse($"Shared metadata {metadataId} already exists");
            }

            var metadataNode = new Node(
                Id: metadataId,
                TypeId: "codex.meta/shared-metadata",
                State: ContentState.Ice,
                Locale: "en",
                Title: $"Shared Metadata: {request.Category}.{request.Key}",
                Description: request.Description ?? $"Shared metadata for {request.Category}.{request.Key}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request.Data),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["category"] = request.Category,
                    ["key"] = request.Key,
                    ["dataType"] = request.DataType,
                    ["createdAt"] = DateTimeOffset.UtcNow,
                    ["version"] = request.Version ?? "1.0.0"
                }
            );

            _registry.Upsert(metadataNode);

            return new SuccessResponse($"Created shared metadata {metadataId}", new { metadataId });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating shared metadata: {ex.Message}", ex);
            return new ErrorResponse($"Error creating shared metadata: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/shared-metadata/{metadataId}", "Get Shared Metadata", "Retrieves shared metadata by ID", "codex.shared-metadata")]
    public async Task<object> GetSharedMetadataAsync([ApiParameter("metadataId", "Metadata identifier", Required = true, Location = "path")] string metadataId)
    {
        try
        {
            var metadataNode = _registry.GetNode(metadataId);
            if (metadataNode == null)
            {
                return new ErrorResponse($"Shared metadata {metadataId} not found");
            }

            var data = metadataNode.Content?.InlineJson != null 
                ? JsonSerializer.Deserialize<object>(metadataNode.Content.InlineJson)
                : null;

            return new SuccessResponse("Shared metadata retrieved", new
            {
                id = metadataNode.Id,
                category = metadataNode.Meta?.GetValueOrDefault("category"),
                key = metadataNode.Meta?.GetValueOrDefault("key"),
                dataType = metadataNode.Meta?.GetValueOrDefault("dataType"),
                data = data,
                createdAt = metadataNode.Meta?.GetValueOrDefault("createdAt"),
                version = metadataNode.Meta?.GetValueOrDefault("version")
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting shared metadata {metadataId}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting shared metadata: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/shared-metadata/link", "Link Node to Metadata", "Links a node to shared metadata", "codex.shared-metadata")]
    public async Task<object> LinkNodeToMetadataAsync([ApiParameter("request", "Node to metadata linking request", Required = true, Location = "body")] LinkNodeToMetadataRequest request)
    {
        try
        {
            var node = _registry.GetNode(request.NodeId);
            if (node == null)
            {
                return new ErrorResponse($"Node {request.NodeId} not found");
            }

            var metadataNode = _registry.GetNode(request.MetadataId);
            if (metadataNode == null)
            {
                return new ErrorResponse($"Shared metadata {request.MetadataId} not found");
            }

            // Create edge from node to shared metadata
            var edge = NodeHelpers.CreateEdge(
                request.NodeId,
                request.MetadataId,
                "references",
                1.0,
                new Dictionary<string, object>
                {
                    ["relationship"] = "node-references-shared-metadata",
                    ["metadataCategory"] = metadataNode.Meta?.GetValueOrDefault("category"),
                    ["metadataKey"] = metadataNode.Meta?.GetValueOrDefault("key"),
                    ["linkedAt"] = DateTimeOffset.UtcNow
                }
            );

            _registry.Upsert(edge);

            return new SuccessResponse($"Linked node {request.NodeId} to shared metadata {request.MetadataId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error linking node to metadata: {ex.Message}", ex);
            return new ErrorResponse($"Error linking node to metadata: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates shared metadata for common patterns used across the system
    /// </summary>
    public async Task InitializeCommonSharedMetadataAsync()
    {
        try
        {
            // News source metadata
            await CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                Category: "news",
                Key: "source-types",
                Description: "Common news source type metadata",
                DataType: "object",
                Data: new
                {
                    sourceTypes = new[]
                    {
                        new { type = "RSS", description = "RSS feed source", fields = new[] { "rssId", "feedUrl" } },
                        new { type = "API", description = "API-based source", fields = new[] { "score", "by", "apiKey" } },
                        new { type = "WebScraping", description = "Web scraping source", fields = new[] { "selector", "url" } }
                    }
                }
            ));

            // AI model metadata
            await CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                Category: "ai",
                Key: "model-configs",
                Description: "Common AI model configuration metadata",
                DataType: "object",
                Data: new
                {
                    models = new[]
                    {
                        new { name = "llama3.2:3b", provider = "ollama", type = "llm", capabilities = new[] { "text-generation", "concept-extraction" } },
                        new { name = "gpt-4", provider = "openai", type = "llm", capabilities = new[] { "text-generation", "concept-extraction", "analysis" } },
                        new { name = "claude-3", provider = "anthropic", type = "llm", capabilities = new[] { "text-generation", "analysis" } }
                    }
                }
            ));

            // Content type metadata
            await CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                Category: "content",
                Key: "media-types",
                Description: "Common media type metadata",
                DataType: "object",
                Data: new
                {
                    mediaTypes = new[]
                    {
                        new { type = "text/plain", renderer = "TextRenderer", icon = "📄" },
                        new { type = "text/markdown", renderer = "MarkdownRenderer", icon = "📝" },
                        new { type = "application/json", renderer = "JsonRenderer", icon = "📋" },
                        new { type = "text/html", renderer = "HtmlRenderer", icon = "🌐" },
                        new { type = "image/png", renderer = "ImageRenderer", icon = "🖼️" },
                        new { type = "image/jpeg", renderer = "ImageRenderer", icon = "🖼️" },
                        new { type = "image/svg+xml", renderer = "SvgRenderer", icon = "🎨" },
                        new { type = "video/mp4", renderer = "VideoRenderer", icon = "🎥" },
                        new { type = "audio/mp3", renderer = "AudioRenderer", icon = "🎵" }
                    }
                }
            ));

            // U-CORE ontology metadata
            await CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                Category: "ontology",
                Key: "ucore-axes",
                Description: "U-CORE ontology axes metadata",
                DataType: "object",
                Data: new
                {
                    axes = new[]
                    {
                        new { name = "Consciousness", keywords = new[] { "awareness", "mind", "consciousness", "perception" } },
                        new { name = "Energy", keywords = new[] { "energy", "vibration", "frequency", "resonance" } },
                        new { name = "Information", keywords = new[] { "data", "knowledge", "information", "wisdom" } },
                        new { name = "Matter", keywords = new[] { "physical", "material", "substance", "form" } },
                        new { name = "Space", keywords = new[] { "space", "dimension", "location", "position" } },
                        new { name = "Time", keywords = new[] { "time", "temporal", "duration", "sequence" } }
                    }
                }
            ));

            // System configuration metadata
            await CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                Category: "system",
                Key: "default-configs",
                Description: "Default system configuration metadata",
                DataType: "object",
                Data: new
                {
                    newsIngestion = new { intervalMinutes = 15, cleanupIntervalHours = 24 },
                    aiProcessing = new { defaultModel = "llama3.2:3b", defaultProvider = "ollama" },
                    contentExtraction = new { maxContentLength = 50000, minContentLength = 100 },
                    conceptExtraction = new { maxConceptsPerItem = 10, similarityThreshold = 0.3 }
                }
            ));

            _logger.Info("Initialized common shared metadata");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error initializing common shared metadata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets shared metadata by category and key
    /// </summary>
    public async Task<object?> GetSharedMetadataByCategoryAndKeyAsync(string category, string key)
    {
        try
        {
            var metadataId = $"codex.shared-metadata.{category}.{key}.{Guid.NewGuid():N}";
            var result = await GetSharedMetadataAsync(metadataId);
            
            if (result is SuccessResponse successResponse)
            {
                return successResponse.Data;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error getting shared metadata {category}.{key}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Links a node to shared metadata by category and key
    /// </summary>
    public async Task<bool> LinkNodeToSharedMetadataAsync(string nodeId, string category, string key)
    {
        try
        {
            var metadataId = $"codex.shared-metadata.{category}.{key}.{Guid.NewGuid():N}";
            var result = await LinkNodeToMetadataAsync(new LinkNodeToMetadataRequest(
                NodeId: nodeId,
                MetadataId: metadataId
            ));
            
            return result is SuccessResponse;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Error linking node {nodeId} to shared metadata {category}.{key}: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Request to create shared metadata
/// </summary>
public record CreateSharedMetadataRequest(
    string Category,
    string Key,
    string? Description = null,
    string DataType = "object",
    object? Data = null,
    string? Version = null
);

/// <summary>
/// Request to link a node to shared metadata
/// </summary>
public record LinkNodeToMetadataRequest(
    string NodeId,
    string MetadataId
);
