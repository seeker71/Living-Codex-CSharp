using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Threads Module - Deep conversations and collaborative exploration
/// Enables threaded discussions around concepts, ideas, and consciousness exploration
/// </summary>
[MetaNode(Id = "codex.threads", Name = "Threads Module", Description = "Deep conversations and collaborative exploration")]
public sealed class ThreadsModule : ModuleBase
{
    public override string Name => "Threads Module";
    public override string Description => "Deep conversations and collaborative exploration";
    public override string Version => "1.0.0";

    public ThreadsModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.threads",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "threads", "discussion", "collaboration", "exploration", "conversation" },
            capabilities: new[] { 
                "thread-creation", "reply-management", "discussion-tracking",
                "resonance-scoring", "collaboration-facilitation"
            },
            spec: "codex.spec.threads"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Threads Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Threads Module HTTP endpoints registered");
    }

    // List threads
    [ApiRoute("GET", "/threads/list", "list-threads", "Get list of discussion threads", "codex.threads")]
    public async Task<object> ListThreads([ApiParameter("limit", "Number of threads to return", Required = false)] int? limit = 20)
    {
        try
        {
            var threadNodes = _registry.AllNodes()
                .Where(n => n.TypeId == "codex.thread")
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("createdAt"))
                .Take(limit ?? 20)
                .ToList();

            var threads = threadNodes.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                content = n.Description,
                author = new
                {
                    id = n.Meta?.GetValueOrDefault("authorId")?.ToString() ?? "",
                    name = n.Meta?.GetValueOrDefault("authorName")?.ToString() ?? "Unknown",
                    avatar = n.Meta?.GetValueOrDefault("authorAvatar")?.ToString()
                },
                createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString(),
                updatedAt = n.Meta?.GetValueOrDefault("updatedAt")?.ToString(),
                replies = GetThreadReplies(n.Id),
                resonance = GetResonanceScore(n.Id),
                axes = GetThreadAxes(n),
                isResolved = n.Meta?.GetValueOrDefault("isResolved")?.ToString() == "true"
            }).ToList();

            return new { success = true, threads, totalCount = threads.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing threads: {ex.Message}", ex);
            return new ErrorResponse($"Error listing threads: {ex.Message}");
        }
    }

    // Create new thread
    [ApiRoute("POST", "/threads/create", "create-thread", "Create a new discussion thread", "codex.threads")]
    public async Task<object> CreateThread([ApiParameter("body", "Thread creation request", Required = true, Location = "body")] ThreadCreateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Content))
            {
                return new ErrorResponse("Title and content are required");
            }

            var threadId = $"thread-{Guid.NewGuid():N}";
            var threadNode = new Node(
                Id: threadId,
                TypeId: "codex.thread",
                State: ContentState.Water,
                Locale: "en-US",
                Title: request.Title,
                Description: request.Content,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["authorId"] = request.AuthorId ?? "",
                    ["authorName"] = request.AuthorName ?? "Anonymous",
                    ["authorAvatar"] = request.AuthorAvatar ?? "",
                    ["createdAt"] = DateTime.UtcNow,
                    ["updatedAt"] = DateTime.UtcNow,
                    ["axes"] = request.Axes ?? new[] { "consciousness", "unity" },
                    ["isResolved"] = false
                }
            );

            _registry.Upsert(threadNode);

            return new 
            { 
                success = true, 
                threadId = threadId,
                message = "Thread created successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating thread: {ex.Message}", ex);
            return new ErrorResponse($"Error creating thread: {ex.Message}");
        }
    }

    // Get thread details
    [ApiRoute("GET", "/threads/{threadId}", "get-thread", "Get detailed information about a thread", "codex.threads")]
    public async Task<object> GetThread([ApiParameter("threadId", "Thread ID", Required = true, Location = "path")] string threadId)
    {
        try
        {
            if (!_registry.TryGet(threadId, out var threadNode))
            {
                return new ErrorResponse("Thread not found");
            }

            var replies = GetThreadReplies(threadId);
            var resonance = GetResonanceScore(threadId);

            return new
            {
                success = true,
                thread = new
                {
                    id = threadNode.Id,
                    title = threadNode.Title,
                    content = threadNode.Description,
                    author = new
                    {
                        id = threadNode.Meta?.GetValueOrDefault("authorId")?.ToString() ?? "",
                        name = threadNode.Meta?.GetValueOrDefault("authorName")?.ToString() ?? "Unknown",
                        avatar = threadNode.Meta?.GetValueOrDefault("authorAvatar")?.ToString()
                    },
                    createdAt = threadNode.Meta?.GetValueOrDefault("createdAt")?.ToString(),
                    updatedAt = threadNode.Meta?.GetValueOrDefault("updatedAt")?.ToString(),
                    replies,
                    resonance,
                    axes = GetThreadAxes(threadNode),
                    isResolved = threadNode.Meta?.GetValueOrDefault("isResolved")?.ToString() == "true"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting thread: {ex.Message}", ex);
            return new ErrorResponse($"Error getting thread: {ex.Message}");
        }
    }

    // Add reply to thread
    [ApiRoute("POST", "/threads/{threadId}/reply", "add-reply", "Add a reply to a thread", "codex.threads")]
    public async Task<object> AddReply(
        [ApiParameter("threadId", "Thread ID", Required = true, Location = "path")] string threadId,
        [ApiParameter("body", "Reply request", Required = true, Location = "body")] ThreadReplyRequest request)
    {
        try
        {
            if (!_registry.TryGet(threadId, out var threadNode))
            {
                return new ErrorResponse("Thread not found");
            }

            if (string.IsNullOrEmpty(request.Content))
            {
                return new ErrorResponse("Reply content is required");
            }

            var replyId = $"reply-{Guid.NewGuid():N}";
            var replyNode = new Node(
                Id: replyId,
                TypeId: "codex.thread.reply",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Reply to {threadNode.Title}",
                Description: request.Content,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["threadId"] = threadId,
                    ["authorId"] = request.AuthorId ?? "",
                    ["authorName"] = request.AuthorName ?? "Anonymous",
                    ["authorAvatar"] = request.AuthorAvatar ?? "",
                    ["createdAt"] = DateTime.UtcNow,
                    ["isAccepted"] = false,
                    ["resonance"] = CalculateReplyResonance(request.Content)
                }
            );

            _registry.Upsert(replyNode);

            // Update thread's updatedAt timestamp
            var updatedMeta = new Dictionary<string, object>(threadNode.Meta ?? new Dictionary<string, object>())
            {
                ["updatedAt"] = DateTime.UtcNow
            };
            var updatedThreadNode = threadNode with { Meta = updatedMeta };
            _registry.Upsert(updatedThreadNode);

            return new 
            { 
                success = true, 
                replyId = replyId,
                message = "Reply added successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error adding reply: {ex.Message}", ex);
            return new ErrorResponse($"Error adding reply: {ex.Message}");
        }
    }

    // Helper methods
    private List<object> GetThreadReplies(string threadId)
    {
        var replyNodes = _registry.AllNodes()
            .Where(n => n.TypeId == "codex.thread.reply" && 
                       n.Meta?.GetValueOrDefault("threadId")?.ToString() == threadId)
            .OrderBy(n => n.Meta?.GetValueOrDefault("createdAt"))
            .ToList();

        return replyNodes.Select(n => new
        {
            id = n.Id,
            content = n.Description,
            author = new
            {
                id = n.Meta?.GetValueOrDefault("authorId")?.ToString() ?? "",
                name = n.Meta?.GetValueOrDefault("authorName")?.ToString() ?? "Unknown",
                avatar = n.Meta?.GetValueOrDefault("authorAvatar")?.ToString()
            },
            createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString(),
            resonance = Convert.ToDouble(n.Meta?.GetValueOrDefault("resonance") ?? 0.5),
            isAccepted = n.Meta?.GetValueOrDefault("isAccepted")?.ToString() == "true"
        }).Cast<object>().ToList();
    }

    private double GetResonanceScore(string threadId)
    {
        // Calculate resonance based on replies, engagement, and content quality
        var replies = GetThreadReplies(threadId);
        var baseResonance = 0.5;
        
        if (replies.Count > 0)
        {
            var avgReplyResonance = replies.Average(r => Convert.ToDouble(r.GetType().GetProperty("resonance")?.GetValue(r) ?? 0.5));
            baseResonance = Math.Min(0.95, baseResonance + (avgReplyResonance * 0.3));
        }

        return baseResonance;
    }

    private string[] GetThreadAxes(Node threadNode)
    {
        var axesMeta = threadNode.Meta?.GetValueOrDefault("axes");
        if (axesMeta is string[] axes)
        {
            return axes;
        }
        return new[] { "consciousness", "unity" };
    }

    private double CalculateReplyResonance(string content)
    {
        // Simple resonance calculation based on content length and keywords
        var baseResonance = 0.5;
        
        if (content.Length > 100)
            baseResonance += 0.1;
        if (content.Length > 300)
            baseResonance += 0.1;
            
        var resonanceKeywords = new[] { "consciousness", "unity", "abundance", "resonance", "connection", "exploration" };
        var keywordCount = resonanceKeywords.Count(keyword => content.ToLower().Contains(keyword));
        baseResonance += keywordCount * 0.05;
        
        return Math.Min(0.95, baseResonance);
    }
}

// Data structures for threads
[MetaNode(Id = "codex.threads.create-request", Name = "Thread Create Request", Description = "Request to create a new thread")]
public record ThreadCreateRequest
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
    public string[]? Axes { get; set; }
}

[MetaNode(Id = "codex.threads.reply-request", Name = "Thread Reply Request", Description = "Request to add a reply to a thread")]
public record ThreadReplyRequest
{
    public string Content { get; set; } = "";
    public string? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorAvatar { get; set; }
}
