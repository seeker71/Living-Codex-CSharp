using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Threads Module - discussion threads and replies as nodes/edges
/// </summary>
public sealed class ThreadsModule : ModuleBase
{
    public override string Name => "Threads Module";
    public override string Description => "Discussion threads and replies";
    public override string Version => "1.0.0";

    public ThreadsModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.threads",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "threads", "discussion", "replies" },
            capabilities: new[] { "create_thread", "list_threads", "create_reply" },
            spec: "codex.spec.threads"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Threads API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Threads HTTP endpoints registered");
    }

    [ApiRoute("GET", "/threads/list", "ListThreads", "List recent discussion threads", "codex.threads")]
    public async Task<object> ListThreadsAsync()
    {
        try
        {
            var threadNodes = _registry.GetNodesByTypePrefix("codex.thread/")
                .OrderByDescending(n =>
                {
                    if (n.Meta != null && n.Meta.TryGetValue("createdAt", out var createdObj) && createdObj is DateTimeOffset cdt)
                    {
                        return cdt;
                    }
                    return DateTimeOffset.MinValue;
                })
                .Take(100)
                .ToList();

            var result = new List<object>();

            foreach (var thread in threadNodes)
            {
                var threadId = thread.Id;
                var replies = _registry.GetEdgesFrom(threadId)
                    .Where(e => e.Role == "has_reply")
                    .Select(e => _registry.GetNode(e.ToId))
                    .Where(n => n != null)
                    .Cast<Node>()
                    .ToList();

                var authorId = thread.Meta.TryGetValue("authorId", out var a) ? a?.ToString() : null;
                var author = ResolveUser(authorId);

                result.Add(new
                {
                    id = thread.Id,
                    title = thread.Title,
                    content = thread.Description ?? ExtractInlineText(thread.Content),
                    author = author,
                    createdAt = thread.Meta.TryGetValue("createdAt", out var created) ? created : DateTimeOffset.UtcNow,
                    updatedAt = thread.Meta.TryGetValue("updatedAt", out var updated) ? updated : DateTimeOffset.UtcNow,
                    replies = replies.Select(r => new
                    {
                        id = r.Id,
                        content = r.Description ?? ExtractInlineText(r.Content),
                        author = ResolveUser(r.Meta.TryGetValue("authorId", out var ra) ? ra?.ToString() : null),
                        createdAt = r.Meta.TryGetValue("createdAt", out var rc) ? rc : DateTimeOffset.UtcNow,
                        resonance = r.Meta.TryGetValue("resonance", out var rr) ? Convert.ToDouble(rr) : 0.0,
                        isAccepted = r.Meta.TryGetValue("isAccepted", out var ia) && ia is bool && (bool)ia
                    }),
                    resonance = thread.Meta.TryGetValue("resonance", out var tr) ? Convert.ToDouble(tr) : 0.5,
                    axes = thread.Meta.TryGetValue("axes", out var axes) && axes is IEnumerable<object> list
                        ? list.Select(x => x.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray()
                        : Array.Empty<string>(),
                    isResolved = thread.Meta.TryGetValue("isResolved", out var ir) && ir is bool rb && rb
                });
            }

            return new { success = true, threads = result };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error listing threads: {ex.Message}", ex);
            return new ErrorResponse($"Failed to list threads: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/threads/create", "CreateThread", "Create a new discussion thread", "codex.threads")]
    public async Task<object> CreateThreadAsync([ApiParameter("request", "Thread creation request")] CreateThreadRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AuthorId))
            {
                return new ErrorResponse("Title, Content, and AuthorId are required");
            }

            var threadId = $"thread-{Guid.NewGuid():N}";

            var meta = new Dictionary<string, object>
            {
                ["moduleId"] = "codex.threads",
                ["createdAt"] = DateTimeOffset.UtcNow,
                ["updatedAt"] = DateTimeOffset.UtcNow,
                ["axes"] = request.Axes ?? Array.Empty<string>(),
                ["isResolved"] = false,
                ["resonance"] = 0.5
            };

            meta["authorId"] = request.AuthorId!;

            var node = new Node(
                Id: threadId,
                TypeId: "codex.thread/root",
                State: ContentState.Water,
                Locale: "en",
                Title: request.Title,
                Description: request.Content,
                Content: new ContentRef(MediaType: "application/json", InlineJson: JsonSerializer.Serialize(new { content = request.Content }), InlineBytes: null, ExternalUri: null),
                Meta: meta
            );

            _registry.Upsert(node);

            return new { success = true, threadId = threadId };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating thread: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create thread: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/threads/reply", "CreateReply", "Create a reply to a thread", "codex.threads")]
    public async Task<object> CreateReplyAsync([ApiParameter("request", "Reply creation request")] CreateReplyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ThreadId) || string.IsNullOrWhiteSpace(request.Content) || string.IsNullOrWhiteSpace(request.AuthorId))
            {
                return new ErrorResponse("ThreadId, Content, and AuthorId are required");
            }

            if (_registry.GetNode(request.ThreadId) == null)
            {
                return new ErrorResponse("Thread not found");
            }

            var replyId = $"reply-{Guid.NewGuid():N}";

            var meta = new Dictionary<string, object>
            {
                ["moduleId"] = "codex.threads",
                ["createdAt"] = DateTimeOffset.UtcNow,
                ["resonance"] = 0.0,
                ["isAccepted"] = false
            };

            meta["authorId"] = request.AuthorId!;

            var replyNode = new Node(
                Id: replyId,
                TypeId: "codex.thread/reply",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Reply to {request.ThreadId}",
                Description: request.Content,
                Content: new ContentRef(MediaType: "application/json", InlineJson: JsonSerializer.Serialize(new { content = request.Content }), InlineBytes: null, ExternalUri: null),
                Meta: meta
            );

            _registry.Upsert(replyNode);
            _registry.Upsert(new Edge(
                FromId: request.ThreadId,
                ToId: replyId,
                Role: "has_reply",
                Weight: 1.0,
                Meta: new Dictionary<string, object>()
            ));

            return new { success = true, replyId = replyId };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating reply: {ex.Message}", ex);
            return new ErrorResponse($"Failed to create reply: {ex.Message}");
        }
    }

    private static string ExtractInlineText(ContentRef content)
    {
        if (!string.IsNullOrEmpty(content.InlineJson))
        {
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(content.InlineJson);
                if (dict != null && dict.TryGetValue("content", out var c))
                {
                    return c?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // ignore
            }
        }
        return string.Empty;
    }

    private object ResolveUser(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new { id = "anonymous", name = "Anonymous", avatar = "/public/vercel.svg" };
        }

        // Try to resolve user profile node if present
        var userNode = _registry.GetNode(userId);
        if (userNode != null)
        {
            var name = userNode.Title ?? (userNode.Meta.TryGetValue("displayName", out var dn) ? dn?.ToString() : userId);
            var avatar = userNode.Meta.TryGetValue("avatar", out var av) ? av?.ToString() : null;
            return new { id = userId, name = name ?? userId, avatar = avatar };
        }

        return new { id = userId, name = userId, avatar = (string?)null };
    }
}

[RequestType("codex.threads.create-thread", "CreateThreadRequest", "Create thread request")]
public record CreateThreadRequest(
    string Title,
    string Content,
    string? AuthorId,
    IEnumerable<string>? Axes
);

[RequestType("codex.threads.create-reply", "CreateReplyRequest", "Create reply request")]
public record CreateReplyRequest(
    string ThreadId,
    string Content,
    string? AuthorId
);
