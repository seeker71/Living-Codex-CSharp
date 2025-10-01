using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// User Interactions Module - Manages user interactions with content
/// Handles votes, bookmarks, likes, and shares with persistent backend storage
/// </summary>
public class UserInteractionsModule : ModuleBase
{
    public override string Name => "User Interactions Module";
    public override string Description => "Manages user interactions (votes, bookmarks, likes, shares) with persistent backend storage";
    public override string Version => "1.0.0";

    public UserInteractionsModule(INodeRegistry registry, ICodexLogger logger)
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.user-interactions",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "user", "interactions", "votes", "bookmarks", "likes", "shares" },
            capabilities: new[] { "vote_tracking", "bookmark_management", "like_tracking", "share_tracking", "interaction_persistence" },
            spec: "codex.spec.user-interactions"
        );
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("User Interactions Module HTTP endpoints registered");
    }

    // ==================== VOTES ====================

    /// <summary>
    /// Set or update a user's vote on an entity (concept, node, etc.)
    /// </summary>
    [RequireAuth]
    [ApiRoute("POST", "/interactions/vote", "set-vote", "Set or update user vote on an entity", "codex.user-interactions")]
    public async Task<object> SetVote([ApiParameter("request", "Vote request", Required = true, Location = "body")] VoteRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("UserId and EntityId are required");
            }

            var voteEdgeId = $"vote-{request.UserId}-{request.EntityId}";

            // If vote is null, remove the vote
            if (request.Vote == null)
            {
                // Find and remove existing vote edge
                var existingEdges = _registry.GetEdgesFrom(request.UserId)
                    .Where(e => e.ToId == request.EntityId && e.Role == "voted")
                    .ToList();

                foreach (var edge in existingEdges)
                {
                    _registry.RemoveEdge(edge.FromId, edge.ToId);
                }

                return new { success = true, vote = (string?)null, message = "Vote removed" };
            }

            // Create or update vote edge
            var voteEdge = new Edge(
                FromId: request.UserId,
                ToId: request.EntityId,
                Role: "voted",
                Weight: request.Vote == "up" ? 1.0 : -1.0,
                Meta: new Dictionary<string, object>
                {
                    ["voteType"] = request.Vote,
                    ["entityType"] = request.EntityType ?? "concept",
                    ["timestamp"] = DateTime.UtcNow.ToString("o"),
                    ["voteEdgeId"] = voteEdgeId
                }
            );

            _registry.Upsert(voteEdge);

            return new { success = true, vote = request.Vote, message = "Vote recorded successfully" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error setting vote: {ex.Message}", ex);
            return new ErrorResponse($"Failed to set vote: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a user's vote for a specific entity
    /// </summary>
    [ApiRoute("GET", "/interactions/vote/{userId}/{entityId}", "get-vote", "Get user's vote for an entity", "codex.user-interactions")]
    public async Task<object> GetVote(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var voteEdges = _registry.GetEdgesFrom(userId)
                .Where(e => e.ToId == entityId && e.Role == "voted")
                .ToList();

            if (!voteEdges.Any())
            {
                return new { success = true, vote = (string?)null };
            }

            var voteEdge = voteEdges.First();
            var voteType = voteEdge.Meta?.ContainsKey("voteType") == true
                ? voteEdge.Meta["voteType"]?.ToString()
                : (voteEdge.Weight >= 0 ? "up" : "down");

            return new { success = true, vote = voteType };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting vote: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get vote: {ex.Message}");
        }
    }

    /// <summary>
    /// Get vote counts for an entity
    /// </summary>
    [ApiRoute("GET", "/interactions/votes/{entityId}", "get-vote-counts", "Get vote counts for an entity", "codex.user-interactions")]
    public async Task<object> GetVoteCounts(
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var voteEdges = _registry.GetEdgesTo(entityId)
                .Where(e => e.Role == "voted")
                .ToList();

            var upvotes = voteEdges.Count(e => e.Weight >= 0);
            var downvotes = voteEdges.Count(e => e.Weight < 0);

            return new { success = true, upvotes, downvotes, total = upvotes - downvotes };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting vote counts: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get vote counts: {ex.Message}");
        }
    }

    // ==================== BOOKMARKS ====================

    /// <summary>
    /// Toggle bookmark for an entity
    /// </summary>
    [RequireAuth]
    [ApiRoute("POST", "/interactions/bookmark", "toggle-bookmark", "Toggle bookmark for an entity", "codex.user-interactions")]
    public async Task<object> ToggleBookmark([ApiParameter("request", "Bookmark request", Required = true, Location = "body")] BookmarkRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("UserId and EntityId are required");
            }

            // Check if bookmark exists
            var existingBookmarks = _registry.GetEdgesFrom(request.UserId)
                .Where(e => e.ToId == request.EntityId && e.Role == "bookmarked")
                .ToList();

            bool isBookmarked = existingBookmarks.Any();

            if (isBookmarked)
            {
                // Remove bookmark
                foreach (var edge in existingBookmarks)
                {
                    _registry.RemoveEdge(edge.FromId, edge.ToId);
                }
                return new { success = true, bookmarked = false, message = "Bookmark removed" };
            }
            else
            {
                // Add bookmark
                var bookmarkEdge = new Edge(
                    FromId: request.UserId,
                    ToId: request.EntityId,
                    Role: "bookmarked",
                    Weight: 1.0,
                    Meta: new Dictionary<string, object>
                    {
                        ["entityType"] = request.EntityType ?? "concept",
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    }
                );

                _registry.Upsert(bookmarkEdge);
                return new { success = true, bookmarked = true, message = "Bookmark added" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error toggling bookmark: {ex.Message}", ex);
            return new ErrorResponse($"Failed to toggle bookmark: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if entity is bookmarked by user
    /// </summary>
    [ApiRoute("GET", "/interactions/bookmark/{userId}/{entityId}", "check-bookmark", "Check if entity is bookmarked", "codex.user-interactions")]
    public async Task<object> CheckBookmark(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var bookmarks = _registry.GetEdgesFrom(userId)
                .Where(e => e.ToId == entityId && e.Role == "bookmarked")
                .ToList();

            return new { success = true, bookmarked = bookmarks.Any() };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking bookmark: {ex.Message}", ex);
            return new ErrorResponse($"Failed to check bookmark: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all bookmarks for a user
    /// </summary>
    [RequireAuth]
    [ApiRoute("GET", "/interactions/bookmarks/{userId}", "get-bookmarks", "Get all bookmarks for a user", "codex.user-interactions")]
    public async Task<object> GetBookmarks(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("skip", "Number of items to skip", Required = false, Location = "query")] int skip = 0,
        [ApiParameter("take", "Number of items to return", Required = false, Location = "query")] int take = 50)
    {
        try
        {
            var bookmarkEdges = _registry.GetEdgesFrom(userId)
                .Where(e => e.Role == "bookmarked")
                .OrderByDescending(e => e.Meta?.ContainsKey("timestamp") == true ? e.Meta["timestamp"] : "")
                .Skip(skip)
                .Take(take)
                .ToList();

            var bookmarks = bookmarkEdges.Select(e => new
            {
                entityId = e.ToId,
                entityType = e.Meta?.ContainsKey("entityType") == true ? e.Meta["entityType"]?.ToString() : "concept",
                timestamp = e.Meta?.ContainsKey("timestamp") == true ? e.Meta["timestamp"]?.ToString() : null
            }).ToList();

            return new { success = true, bookmarks, totalCount = bookmarkEdges.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting bookmarks: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get bookmarks: {ex.Message}");
        }
    }

    // ==================== LIKES ====================

    /// <summary>
    /// Toggle like for an entity
    /// </summary>
    [RequireAuth]
    [ApiRoute("POST", "/interactions/like", "toggle-like", "Toggle like for an entity", "codex.user-interactions")]
    public async Task<object> ToggleLike([ApiParameter("request", "Like request", Required = true, Location = "body")] LikeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("UserId and EntityId are required");
            }

            // Check if like exists
            var existingLikes = _registry.GetEdgesFrom(request.UserId)
                .Where(e => e.ToId == request.EntityId && e.Role == "liked")
                .ToList();

            bool isLiked = existingLikes.Any();

            if (isLiked)
            {
                // Remove like
                foreach (var edge in existingLikes)
                {
                    _registry.RemoveEdge(edge.FromId, edge.ToId);
                }
                return new { success = true, liked = false, message = "Like removed" };
            }
            else
            {
                // Add like
                var likeEdge = new Edge(
                    FromId: request.UserId,
                    ToId: request.EntityId,
                    Role: "liked",
                    Weight: 1.0,
                    Meta: new Dictionary<string, object>
                    {
                        ["entityType"] = request.EntityType ?? "concept",
                        ["timestamp"] = DateTime.UtcNow.ToString("o")
                    }
                );

                _registry.Upsert(likeEdge);
                return new { success = true, liked = true, message = "Like added" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error toggling like: {ex.Message}", ex);
            return new ErrorResponse($"Failed to toggle like: {ex.Message}");
        }
    }

    /// <summary>
    /// Get like count for an entity
    /// </summary>
    [ApiRoute("GET", "/interactions/likes/{entityId}", "get-like-count", "Get like count for an entity", "codex.user-interactions")]
    public async Task<object> GetLikeCount(
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var likeCount = _registry.GetEdgesTo(entityId)
                .Count(e => e.Role == "liked");

            return new { success = true, likes = likeCount };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting like count: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get like count: {ex.Message}");
        }
    }

    // ==================== SHARES ====================

    /// <summary>
    /// Record a share action
    /// </summary>
    [RequireAuth]
    [ApiRoute("POST", "/interactions/share", "record-share", "Record a share action", "codex.user-interactions")]
    public async Task<object> RecordShare([ApiParameter("request", "Share request", Required = true, Location = "body")] ShareRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.EntityId))
            {
                return new ErrorResponse("UserId and EntityId are required");
            }

            var shareEdge = new Edge(
                FromId: request.UserId,
                ToId: request.EntityId,
                Role: "shared",
                Weight: 1.0,
                Meta: new Dictionary<string, object>
                {
                    ["entityType"] = request.EntityType ?? "concept",
                    ["shareMethod"] = request.ShareMethod ?? "link",
                    ["timestamp"] = DateTime.UtcNow.ToString("o")
                }
            );

            _registry.Upsert(shareEdge);

            return new { success = true, message = "Share recorded successfully" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error recording share: {ex.Message}", ex);
            return new ErrorResponse($"Failed to record share: {ex.Message}");
        }
    }

    /// <summary>
    /// Get share count for an entity
    /// </summary>
    [ApiRoute("GET", "/interactions/shares/{entityId}", "get-share-count", "Get share count for an entity", "codex.user-interactions")]
    public async Task<object> GetShareCount(
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var shareCount = _registry.GetEdgesTo(entityId)
                .Count(e => e.Role == "shared");

            return new { success = true, shares = shareCount };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting share count: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get share count: {ex.Message}");
        }
    }

    // ==================== BULK OPERATIONS ====================

    /// <summary>
    /// Get all interactions for a user on a specific entity
    /// </summary>
    [ApiRoute("GET", "/interactions/{userId}/{entityId}", "get-user-interactions", "Get all user interactions for an entity", "codex.user-interactions")]
    public async Task<object> GetUserInteractions(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("entityId", "Entity ID", Required = true, Location = "path")] string entityId)
    {
        try
        {
            var edges = _registry.GetEdgesFrom(userId)
                .Where(e => e.ToId == entityId && (e.Role == "voted" || e.Role == "bookmarked" || e.Role == "liked" || e.Role == "shared"))
                .ToList();

            var vote = edges.FirstOrDefault(e => e.Role == "voted");
            var bookmarked = edges.Any(e => e.Role == "bookmarked");
            var liked = edges.Any(e => e.Role == "liked");
            var shared = edges.Any(e => e.Role == "shared");

            return new
            {
                success = true,
                vote = vote?.Meta?.ContainsKey("voteType") == true ? vote.Meta["voteType"]?.ToString() : null,
                bookmarked,
                liked,
                shared
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user interactions: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get user interactions: {ex.Message}");
        }
    }
}

// Request/Response models
public record VoteRequest(string UserId, string EntityId, string? Vote, string? EntityType = "concept");
public record BookmarkRequest(string UserId, string EntityId, string? EntityType = "concept");
public record LikeRequest(string UserId, string EntityId, string? EntityType = "concept");
public record ShareRequest(string UserId, string EntityId, string? ShareMethod, string? EntityType = "concept");

