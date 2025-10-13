using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Collaboration Module - Enables reflection, discussion, and collaboration around concepts
/// </summary>
public sealed class ConceptCollaborationModule : ModuleBase
{
    private readonly HttpClient _httpClient;

    public override string Name => "Concept Collaboration Module";
    public override string Description => "Enables reflection, discussion, and collaboration around concepts with activity feeds and threaded conversations";
    public override string Version => "1.0.0";

    public ConceptCollaborationModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _httpClient = httpClient;
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.concept.collaboration",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "collaboration", "reflection", "discussion", "activity", "concept" },
            capabilities: new[] { "concept-activity", "threaded-discussions", "collaboration-tracking", "improvement-proposals" },
            spec: "codex.spec.concept-collaboration"
        );
    }

    /// <summary>
    /// Get all users attuned or amplified to a concept
    /// </summary>
    [Get("/concept/{conceptId}/collaborators", "concept-collaborators", "Get users collaborating on a concept", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Concept not found")]
    public async Task<object> GetConceptCollaborators(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            // Validate concept exists
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return ResponseHelpers.CreateErrorResponse("Concept not found", "CONCEPT_NOT_FOUND");
            }

            // Get all edges pointing to this concept
            var incomingEdges = _registry.GetEdgesTo(conceptId).ToList();

            // Filter for user-concept relationships (attuned)
            var attuned = incomingEdges
                .Where(e => e.FromId.StartsWith("user.") && e.Role == "attuned")
                .Select(e => CreateCollaboratorInfo(e, "attuned"))
                .ToList();

            // Get contributions (amplifications)
            var contributionNodes = _registry.GetNodesByType("codex.contribution")
                .Where(n => n.Meta?.GetValueOrDefault("entityId")?.ToString() == conceptId)
                .ToList();

            var amplified = contributionNodes
                .Select(n => new
                {
                    userId = n.Meta?.GetValueOrDefault("userId")?.ToString() ?? "",
                    relationshipType = "amplified",
                    strength = 1.0,
                    createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                    contributionType = n.Meta?.GetValueOrDefault("contributionType")?.ToString() ?? "",
                    description = n.Meta?.GetValueOrDefault("description")?.ToString() ?? ""
                })
                .Where(c => !string.IsNullOrEmpty(c.userId))
                .ToList();

            var allCollaborators = attuned.Concat(amplified.Cast<object>()).ToList();

            return new
            {
                success = true,
                conceptId,
                conceptTitle = conceptNode.Title,
                collaborators = allCollaborators,
                totalCount = allCollaborators.Count,
                attuneCount = attuned.Count,
                amplifyCount = amplified.Count,
                message = "Collaborators retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get collaborators: {ex.Message}", "GET_COLLABORATORS_ERROR");
        }
    }

    /// <summary>
    /// Get activity feed for a concept
    /// </summary>
    [Get("/concept/{conceptId}/activity", "concept-activity-feed", "Get activity feed for a concept", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Concept not found")]
    public async Task<object> GetConceptActivity(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId,
        [ApiParameter("limit", "Number of activities to return", Required = false, Location = "query")] int limit = 50,
        [ApiParameter("skip", "Number of activities to skip", Required = false, Location = "query")] int skip = 0)
    {
        try
        {
            // Validate concept exists
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return ResponseHelpers.CreateErrorResponse("Concept not found", "CONCEPT_NOT_FOUND");
            }

            var activities = new List<object>();

            // Get attune events (edges to this concept)
            var incomingEdges = _registry.GetEdgesTo(conceptId)
                .Where(e => e.FromId.StartsWith("user.") && e.Role == "attuned")
                .ToList();

            foreach (var edge in incomingEdges)
            {
                if (_registry.TryGet(edge.FromId, out var userNode))
                {
                    activities.Add(new
                    {
                        type = "attune",
                        userId = edge.FromId,
                        username = userNode.Title,
                        conceptId,
                        timestamp = edge.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                        description = $"{userNode.Title} attuned to this concept"
                    });
                }
            }

            // Get contribution events (amplifications)
            var contributions = _registry.GetNodesByType("codex.contribution")
                .Where(n => n.Meta?.GetValueOrDefault("entityId")?.ToString() == conceptId)
                .ToList();

            foreach (var contrib in contributions)
            {
                var userId = contrib.Meta?.GetValueOrDefault("userId")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(userId) && _registry.TryGet(userId, out var userNode))
                {
                    activities.Add(new
                    {
                        type = "amplify",
                        userId,
                        username = userNode.Title,
                        conceptId,
                        timestamp = contrib.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                        description = contrib.Meta?.GetValueOrDefault("description")?.ToString() ?? $"{userNode.Title} amplified this concept",
                        contributionType = contrib.Meta?.GetValueOrDefault("contributionType")?.ToString() ?? ""
                    });
                }
            }

            // Get discussion threads (if they exist as nodes)
            var discussions = _registry.GetNodesByType("codex.discussion")
                .Where(n => n.Meta?.GetValueOrDefault("conceptId")?.ToString() == conceptId)
                .ToList();

            foreach (var discussion in discussions)
            {
                var userId = discussion.Meta?.GetValueOrDefault("userId")?.ToString() ?? "";
                if (!string.IsNullOrEmpty(userId) && _registry.TryGet(userId, out var userNode))
                {
                    activities.Add(new
                    {
                        type = "discussion",
                        userId,
                        username = userNode.Title,
                        conceptId,
                        discussionId = discussion.Id,
                        timestamp = discussion.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                        description = discussion.Title,
                        preview = discussion.Description
                    });
                }
            }

            // Sort by timestamp descending
            var sortedActivities = activities
                .OrderByDescending(a => 
                {
                    var actDict = a as dynamic;
                    DateTime dt;
                    return DateTime.TryParse(actDict?.timestamp?.ToString(), out dt) ? dt : DateTime.MinValue;
                })
                .Skip(skip)
                .Take(limit)
                .ToList();

            return new
            {
                success = true,
                conceptId,
                conceptTitle = conceptNode.Title,
                activities = sortedActivities,
                totalCount = activities.Count,
                skip,
                limit,
                message = "Activity feed retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get activity feed: {ex.Message}", "GET_ACTIVITY_ERROR");
        }
    }

    /// <summary>
    /// Create a discussion thread for a concept
    /// </summary>
    [RequireAuth]
    [Post("/concept/{conceptId}/discussion", "concept-create-discussion", "Create discussion thread for concept", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> CreateDiscussion(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId,
        [ApiParameter("request", "Discussion request", Required = true, Location = "body")] CreateDiscussionRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { success = false, error = "Title is required", code = "VALIDATION_ERROR" });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { success = false, error = "Content is required", code = "VALIDATION_ERROR" });
            }

            // Validate concept exists
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return Results.NotFound(new { success = false, error = "Concept not found", code = "CONCEPT_NOT_FOUND" });
            }

            // Validate user exists
            if (!_registry.TryGet(request.UserId, out var userNode))
            {
                return Results.NotFound(new { success = false, error = "User not found", code = "USER_NOT_FOUND" });
            }

            var discussionId = $"discussion.{Guid.NewGuid()}";
            var discussionNode = new Node(
                Id: discussionId,
                TypeId: "codex.discussion",
                State: ContentState.Water,
                Locale: "en",
                Title: request.Title,
                Description: request.Content,
                Content: new ContentRef(
                    MediaType: "text/markdown",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        content = request.Content,
                        discussionType = request.DiscussionType
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["conceptId"] = conceptId,
                    ["userId"] = request.UserId,
                    ["username"] = userNode.Title,
                    ["discussionType"] = request.DiscussionType,
                    ["createdAt"] = DateTime.UtcNow,
                    ["replyCount"] = 0,
                    ["status"] = "open"
                }
            );

            _registry.Upsert(discussionNode);

            // Create edge from concept to discussion
            var edge = new Edge(
                FromId: conceptId,
                ToId: discussionId,
                Role: "has-discussion",
                Weight: 1.0,
                Meta: new Dictionary<string, object>
                {
                    ["createdAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(edge);

            return new
            {
                success = true,
                discussion = new
                {
                    id = discussionId,
                    conceptId,
                    userId = request.UserId,
                    username = userNode.Title,
                    title = request.Title,
                    content = request.Content,
                    discussionType = request.DiscussionType,
                    createdAt = DateTime.UtcNow,
                    replyCount = 0
                },
                message = "Discussion created successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to create discussion: {ex.Message}", "CREATE_DISCUSSION_ERROR");
        }
    }

    /// <summary>
    /// Get discussions for a concept
    /// </summary>
    [Get("/concept/{conceptId}/discussions", "concept-discussions", "Get discussion threads for concept", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Concept not found")]
    public async Task<object> GetDiscussions(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId,
        [ApiParameter("limit", "Number of discussions to return", Required = false, Location = "query")] int limit = 20,
        [ApiParameter("skip", "Number of discussions to skip", Required = false, Location = "query")] int skip = 0)
    {
        try
        {
            // Validate concept exists
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return ResponseHelpers.CreateErrorResponse("Concept not found", "CONCEPT_NOT_FOUND");
            }

            // Get all discussions for this concept
            var discussions = _registry.GetNodesByType("codex.discussion")
                .Where(n => n.Meta?.GetValueOrDefault("conceptId")?.ToString() == conceptId)
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("createdAt"))
                .Skip(skip)
                .Take(limit)
                .Select(n => new
                {
                    id = n.Id,
                    conceptId,
                    userId = n.Meta?.GetValueOrDefault("userId")?.ToString() ?? "",
                    username = n.Meta?.GetValueOrDefault("username")?.ToString() ?? "",
                    title = n.Title,
                    content = n.Description,
                    discussionType = n.Meta?.GetValueOrDefault("discussionType")?.ToString() ?? "general",
                    createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? "",
                    replyCount = Convert.ToInt32(n.Meta?.GetValueOrDefault("replyCount") ?? 0),
                    status = n.Meta?.GetValueOrDefault("status")?.ToString() ?? "open"
                })
                .ToList();

            var totalCount = _registry.GetNodesByType("codex.discussion")
                .Count(n => n.Meta?.GetValueOrDefault("conceptId")?.ToString() == conceptId);

            return new
            {
                success = true,
                conceptId,
                conceptTitle = conceptNode.Title,
                discussions,
                totalCount,
                skip,
                limit,
                message = "Discussions retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get discussions: {ex.Message}", "GET_DISCUSSIONS_ERROR");
        }
    }

    /// <summary>
    /// Reply to a discussion
    /// </summary>
    [RequireAuth]
    [Post("/discussion/{discussionId}/reply", "discussion-reply", "Reply to a discussion", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Discussion not found")]
    public async Task<object> ReplyToDiscussion(
        [ApiParameter("discussionId", "Discussion ID", Required = true, Location = "path")] string discussionId,
        [ApiParameter("request", "Reply request", Required = true, Location = "body")] DiscussionReplyRequest request)
    {
        try
        {
            // Validate discussion exists
            if (!_registry.TryGet(discussionId, out var discussionNode))
            {
                return Results.NotFound(new { success = false, error = "Discussion not found", code = "DISCUSSION_NOT_FOUND" });
            }

            // Validate user exists
            if (!_registry.TryGet(request.UserId, out var userNode))
            {
                return Results.NotFound(new { success = false, error = "User not found", code = "USER_NOT_FOUND" });
            }

            var replyId = $"reply.{Guid.NewGuid()}";
            var replyNode = new Node(
                Id: replyId,
                TypeId: "codex.discussion.reply",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Reply by {userNode.Title}",
                Description: request.Content,
                Content: new ContentRef(
                    MediaType: "text/markdown",
                    InlineJson: JsonSerializer.Serialize(new { content = request.Content }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["discussionId"] = discussionId,
                    ["userId"] = request.UserId,
                    ["username"] = userNode.Title,
                    ["createdAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(replyNode);

            // Create edge from discussion to reply
            var edge = new Edge(
                FromId: discussionId,
                ToId: replyId,
                Role: "has-reply",
                Weight: 1.0,
                Meta: new Dictionary<string, object>
                {
                    ["createdAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(edge);

            // Update reply count
            var currentCount = Convert.ToInt32(discussionNode.Meta?.GetValueOrDefault("replyCount") ?? 0);
            discussionNode.Meta["replyCount"] = currentCount + 1;
            _registry.Upsert(discussionNode);

            return new
            {
                success = true,
                reply = new
                {
                    id = replyId,
                    discussionId,
                    userId = request.UserId,
                    username = userNode.Title,
                    content = request.Content,
                    createdAt = DateTime.UtcNow
                },
                message = "Reply added successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to add reply: {ex.Message}", "REPLY_ERROR");
        }
    }

    /// <summary>
    /// Get replies for a discussion
    /// </summary>
    [Get("/discussion/{discussionId}/replies", "discussion-replies", "Get replies for a discussion", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Discussion not found")]
    public async Task<object> GetDiscussionReplies(
        [ApiParameter("discussionId", "Discussion ID", Required = true, Location = "path")] string discussionId)
    {
        try
        {
            // Validate discussion exists
            if (!_registry.TryGet(discussionId, out var discussionNode))
            {
                return ResponseHelpers.CreateErrorResponse("Discussion not found", "DISCUSSION_NOT_FOUND");
            }

            // Get all reply edges from this discussion
            var replyEdges = _registry.GetEdgesFrom(discussionId)
                .Where(e => e.Role == "has-reply")
                .ToList();

            var replies = new List<object>();
            foreach (var edge in replyEdges)
            {
                if (_registry.TryGet(edge.ToId, out var replyNode))
                {
                    replies.Add(new
                    {
                        id = replyNode.Id,
                        discussionId,
                        userId = replyNode.Meta?.GetValueOrDefault("userId")?.ToString() ?? "",
                        username = replyNode.Meta?.GetValueOrDefault("username")?.ToString() ?? "",
                        content = replyNode.Description,
                        createdAt = replyNode.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? ""
                    });
                }
            }

            // Sort by creation time
            var sortedReplies = replies.OrderBy(r =>
            {
                var rDict = r as dynamic;
                DateTime dt;
                return DateTime.TryParse(rDict?.createdAt?.ToString(), out dt) ? dt : DateTime.MinValue;
            }).ToList();

            return new
            {
                success = true,
                discussionId,
                discussionTitle = discussionNode.Title,
                replies = sortedReplies,
                totalCount = replies.Count,
                message = "Replies retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get replies: {ex.Message}", "GET_REPLIES_ERROR");
        }
    }

    /// <summary>
    /// Get concept improvement proposals
    /// </summary>
    [Get("/concept/{conceptId}/proposals", "concept-proposals", "Get improvement proposals for concept", "codex.concept.collaboration")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Concept not found")]
    public async Task<object> GetConceptProposals(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            // Validate concept exists
            if (!_registry.TryGet(conceptId, out var conceptNode))
            {
                return ResponseHelpers.CreateErrorResponse("Concept not found", "CONCEPT_NOT_FOUND");
            }

            // Get discussions of type "proposal"
            var proposals = _registry.GetNodesByType("codex.discussion")
                .Where(n => n.Meta?.GetValueOrDefault("conceptId")?.ToString() == conceptId &&
                           n.Meta?.GetValueOrDefault("discussionType")?.ToString() == "proposal")
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("createdAt"))
                .Select(n => new
                {
                    id = n.Id,
                    conceptId,
                    userId = n.Meta?.GetValueOrDefault("userId")?.ToString() ?? "",
                    username = n.Meta?.GetValueOrDefault("username")?.ToString() ?? "",
                    title = n.Title,
                    content = n.Description,
                    discussionType = n.Meta?.GetValueOrDefault("discussionType")?.ToString() ?? "proposal",
                    createdAt = n.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? "",
                    status = n.Meta?.GetValueOrDefault("status")?.ToString() ?? "open",
                    replyCount = Convert.ToInt32(n.Meta?.GetValueOrDefault("replyCount") ?? 0)
                })
                .ToList();

            return new
            {
                success = true,
                conceptId,
                conceptTitle = conceptNode.Title,
                proposals,
                totalCount = proposals.Count,
                message = "Proposals retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get proposals: {ex.Message}", "GET_PROPOSALS_ERROR");
        }
    }

    private object CreateCollaboratorInfo(Edge edge, string relationshipType)
    {
        var userId = edge.FromId;
        var username = "Unknown User";
        
        if (_registry.TryGet(userId, out var userNode))
        {
            username = userNode.Title;
        }

        return new
        {
            userId,
            username,
            relationshipType,
            strength = edge.Weight,
            createdAt = edge.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString()
        };
    }
}

// Request/Response DTOs
public record CreateDiscussionRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("discussionType")] string DiscussionType = "general"
);

public record DiscussionReplyRequest(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("content")] string Content
);

