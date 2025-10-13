using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodexBootstrap.Modules;

[MetaNode(Id = "codex.gamification", Name = "Gamification Module", Description = "Points, badges, and achievement system for user engagement")]
public sealed class GamificationModule : ModuleBase
{
    private readonly Dictionary<string, UserBadge> _badgeTemplates = new();

    public override string Name => "Gamification Module";
    public override string Description => "Points, badges, and achievement system for user engagement";
    public override string Version => "1.0.0";

    public GamificationModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) : base(registry, logger)
    {
        InitializeDefaultBadges();
    }

    private void InitializeDefaultBadges()
    {
        // Define all available badges
        var defaultBadges = new[]
        {
            new UserBadge
            {
                BadgeId = "newcomer",
                Name = "Newcomer",
                Description = "Welcome to the Living Codex",
                Icon = "🌸",
                Rarity = "common",
                Points = 5,
                EarnedAt = ""
            },
            new UserBadge
            {
                BadgeId = "first_steps",
                Name = "First Steps",
                Description = "Complete basic profile",
                Icon = "👣",
                Rarity = "common",
                Points = 10,
                EarnedAt = ""
            },
            new UserBadge
            {
                BadgeId = "resonance_seeker",
                Name = "Resonance Seeker",
                Description = "Explore your interests",
                Icon = "🔮",
                Rarity = "uncommon",
                Points = 25,
                EarnedAt = ""
            },
            new UserBadge
            {
                BadgeId = "belief_weaver",
                Name = "Belief Weaver",
                Description = "Define your worldview",
                Icon = "🧵",
                Rarity = "rare",
                Points = 50,
                EarnedAt = ""
            },
            new UserBadge
            {
                BadgeId = "consciousness_explorer",
                Name = "Consciousness Explorer",
                Description = "Deepen your awareness",
                Icon = "🧠",
                Rarity = "epic",
                Points = 75,
                EarnedAt = ""
            },
            new UserBadge
            {
                BadgeId = "master_resonator",
                Name = "Master Resonator",
                Description = "Achieve full harmony",
                Icon = "🌟",
                Rarity = "legendary",
                Points = 100,
                EarnedAt = ""
            }
        };

        foreach (var badge in defaultBadges)
        {
            _badgeTemplates[badge.BadgeId] = badge;
        }
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.gamification",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "gamification", "points", "badges", "achievements" },
            capabilities: new[] { "points_tracking", "badge_awards", "achievement_system", "user_levels" },
            spec: "codex.spec.gamification"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Gamification Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Gamification Module HTTP endpoints registered");
    }

    [ApiRoute("POST", "/gamification/award-points", "AwardPoints", "Award points to a user", "codex.gamification")]
    public async Task<object> AwardPointsAsync([ApiParameter("body", "Points award request")] AwardPointsRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new { success = false, error = "User ID is required", message = "User ID is required" };
            }

            if (request.Points <= 0)
            {
                return new { success = false, error = "Points must be greater than 0", message = "Points must be greater than 0" };
            }

            // Get user points data from node registry
            var pointsData = await GetUserPointsData(request.UserId);

            // Award points
            var oldPoints = pointsData.TotalPoints;
            pointsData.TotalPoints += request.Points;
            pointsData.LastUpdated = DateTimeOffset.UtcNow.ToString("O");

            // Calculate level (100 points per level)
            pointsData.Level = (pointsData.TotalPoints / 100) + 1;

            _logger.Info($"Awarding points: User={request.UserId}, Old={oldPoints}, Adding={request.Points}, New={pointsData.TotalPoints}");

            // Record achievement
            var achievement = new Achievement
            {
                AchievementId = Guid.NewGuid().ToString(),
                Name = request.Reason ?? "Points Earned",
                Description = $"Earned {request.Points} points for {request.Category ?? "activity"}",
                Points = request.Points,
                Category = request.Category ?? "general",
                EarnedAt = DateTimeOffset.UtcNow.ToString("O")
            };
            pointsData.Achievements.Add(achievement);

            // Save updated points data to node registry
            await SaveUserPointsData(pointsData);

            // Check for badge awards based on total points
            await CheckAndAwardBadges(request.UserId, pointsData);

            _logger.Info($"Awarded {request.Points} points to user {request.UserId} (Total: {pointsData.TotalPoints}, Level: {pointsData.Level})");

            return new
            {
                success = true,
                userId = request.UserId,
                pointsAwarded = request.Points,
                totalPoints = pointsData.TotalPoints,
                level = pointsData.Level,
                newBadges = pointsData.Badges.Where(b => b.EarnedAt == pointsData.LastUpdated).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error awarding points: {ex.Message}", ex);
            return new { success = false, error = $"Failed to award points: {ex.Message}", message = $"Failed to award points: {ex.Message}" };
        }
    }

    [ApiRoute("GET", "/gamification/points/{userId}", "GetUserPoints", "Get user points and achievements", "codex.gamification")]
    public async Task<object> GetUserPointsAsync([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new { success = false, error = "User ID is required", message = "User ID is required" };
            }

            // Get user points data from node registry
            var pointsData = await GetUserPointsData(userId);

            return new
            {
                success = true,
                data = pointsData
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user points: {ex.Message}", ex);
            return new { success = false, error = $"Failed to get user points: {ex.Message}", message = $"Failed to get user points: {ex.Message}" };
        }
    }

    [ApiRoute("POST", "/gamification/award-badge", "AwardBadge", "Award a badge to a user", "codex.gamification")]
    public async Task<object> AwardBadgeAsync([ApiParameter("body", "Badge award request")] AwardBadgeRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new { success = false, error = "User ID is required", message = "User ID is required" };
            }

            if (string.IsNullOrEmpty(request.BadgeId))
            {
                return new { success = false, error = "Badge ID is required", message = "Badge ID is required" };
            }

            if (!_badgeTemplates.TryGetValue(request.BadgeId, out var badge))
            {
                return new { success = false, error = $"Badge '{request.BadgeId}' not found", message = $"Badge '{request.BadgeId}' not found" };
            }

            // Get user points data from node registry
            var pointsData = await GetUserPointsData(request.UserId);

            // Check if user already has this badge
            if (pointsData.Badges.Any(b => b.BadgeId == request.BadgeId))
            {
                return new { success = false, error = $"User already has badge '{badge.Name}'", message = $"User already has badge '{badge.Name}'" };
            }

            // Award badge
            var userBadge = new UserBadge
            {
                BadgeId = badge.BadgeId,
                Name = badge.Name,
                Description = badge.Description,
                Icon = badge.Icon,
                Rarity = badge.Rarity,
                Points = badge.Points,
                EarnedAt = DateTimeOffset.UtcNow.ToString("O")
            };
            pointsData.Badges.Add(userBadge);
            pointsData.TotalPoints += badge.Points;
            pointsData.Level = (pointsData.TotalPoints / 100) + 1;
            pointsData.LastUpdated = DateTimeOffset.UtcNow.ToString("O");

            // Save updated points data
            await SaveUserPointsData(pointsData);

            _logger.Info($"Awarded badge '{badge.Name}' to user {request.UserId}");

            return new
            {
                success = true,
                userId = request.UserId,
                badge = userBadge,
                totalPoints = pointsData.TotalPoints,
                level = pointsData.Level
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error awarding badge: {ex.Message}", ex);
            return new { success = false, error = $"Failed to award badge: {ex.Message}", message = $"Failed to award badge: {ex.Message}" };
        }
    }

    [ApiRoute("GET", "/gamification/badges", "GetAllBadges", "Get all available badges", "codex.gamification")]
    public async Task<object> GetAllBadgesAsync()
    {
        try
        {
            return new
            {
                success = true,
                badges = _badgeTemplates.Values.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting badges: {ex.Message}", ex);
            return new { success = false, error = $"Failed to get badges: {ex.Message}", message = $"Failed to get badges: {ex.Message}" };
        }
    }

    private async Task CheckAndAwardBadges(string userId, UserPointsData pointsData)
    {
        // Check for level-based badges
        var badgesToAward = new List<string>();

        if (pointsData.TotalPoints >= 5 && !pointsData.Badges.Any(b => b.BadgeId == "newcomer"))
        {
            badgesToAward.Add("newcomer");
        }

        if (pointsData.TotalPoints >= 25 && !pointsData.Badges.Any(b => b.BadgeId == "first_steps"))
        {
            badgesToAward.Add("first_steps");
        }

        if (pointsData.TotalPoints >= 50 && !pointsData.Badges.Any(b => b.BadgeId == "resonance_seeker"))
        {
            badgesToAward.Add("resonance_seeker");
        }

        if (pointsData.TotalPoints >= 100 && !pointsData.Badges.Any(b => b.BadgeId == "belief_weaver"))
        {
            badgesToAward.Add("belief_weaver");
        }

        if (pointsData.TotalPoints >= 150 && !pointsData.Badges.Any(b => b.BadgeId == "consciousness_explorer"))
        {
            badgesToAward.Add("consciousness_explorer");
        }

        if (pointsData.TotalPoints >= 200 && !pointsData.Badges.Any(b => b.BadgeId == "master_resonator"))
        {
            badgesToAward.Add("master_resonator");
        }

        // Award badges
        foreach (var badgeId in badgesToAward)
        {
            if (_badgeTemplates.TryGetValue(badgeId, out var badge))
            {
                var userBadge = new UserBadge
                {
                    BadgeId = badge.BadgeId,
                    Name = badge.Name,
                    Description = badge.Description,
                    Icon = badge.Icon,
                    Rarity = badge.Rarity,
                    Points = badge.Points,
                    EarnedAt = DateTimeOffset.UtcNow.ToString("O")
                };
                pointsData.Badges.Add(userBadge);
                _logger.Info($"Auto-awarded badge '{badge.Name}' to user {userId}");
            }
        }

        // Save updated points data with new badges
        await SaveUserPointsData(pointsData);
    }

    private async Task<UserPointsData> GetUserPointsData(string userId)
    {
        var nodeId = $"gamification.points.{userId}";
        
        try
        {
            var node = await _registry.GetNodeAsync(nodeId);
            if (node != null && node.Meta != null && node.Meta.TryGetValue("pointsData", out var jsonData))
            {
                var json = jsonData?.ToString();
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var data = System.Text.Json.JsonSerializer.Deserialize<UserPointsData>(json) ?? CreateNewUserPointsData(userId);
                        _logger.Info($"Retrieved points data: NodeId={nodeId}, TotalPoints={data.TotalPoints}, Achievements={data.Achievements.Count}");
                        return data;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to deserialize points data for user {userId}: {ex.Message}, creating new");
                    }
                }
                else
                {
                    _logger.Info($"Node {nodeId} found but pointsData is empty");
                }
            }
            else
            {
                _logger.Info($"Node {nodeId} not found or has no meta, creating new");
            }
        }
        catch (Exception ex)
        {
            _logger.Info($"Exception getting node {nodeId}: {ex.Message}, creating new");
        }

        return CreateNewUserPointsData(userId);
    }

    private UserPointsData CreateNewUserPointsData(string userId)
    {
        return new UserPointsData
        {
            UserId = userId,
            TotalPoints = 0,
            Level = 1,
            Badges = new List<UserBadge>(),
            Achievements = new List<Achievement>(),
            LastUpdated = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    private async Task SaveUserPointsData(UserPointsData pointsData)
    {
        var nodeId = $"gamification.points.{pointsData.UserId}";
        var json = System.Text.Json.JsonSerializer.Serialize(pointsData);

        _logger.Info($"Saving points data: NodeId={nodeId}, TotalPoints={pointsData.TotalPoints}, Achievements={pointsData.Achievements.Count}");

        var node = new Node(
            Id: nodeId,
            TypeId: "gamification.user-points",
            State: ContentState.Ice,
            Locale: null,
            Title: $"Points for {pointsData.UserId}",
            Description: $"User has {pointsData.TotalPoints} points at level {pointsData.Level}",
            Content: null,
            Meta: new Dictionary<string, object>
            {
                { "pointsData", json },
                { "userId", pointsData.UserId },
                { "totalPoints", pointsData.TotalPoints },
                { "level", pointsData.Level },
                { "lastUpdated", pointsData.LastUpdated }
            }
        );

        _registry.Upsert(node);
        _logger.Info($"Points data saved to registry");
    }
}

public class AwardPointsRequest
{
    public string UserId { get; set; } = "";
    public int Points { get; set; }
    public string? Reason { get; set; }
    public string? Category { get; set; }
}

public class AwardBadgeRequest
{
    public string UserId { get; set; } = "";
    public string BadgeId { get; set; } = "";
}

public class UserPointsData
{
    public string UserId { get; set; } = "";
    public int TotalPoints { get; set; }
    public int Level { get; set; }
    public List<UserBadge> Badges { get; set; } = new();
    public List<Achievement> Achievements { get; set; } = new();
    public string LastUpdated { get; set; } = "";
}

public class UserBadge
{
    public string BadgeId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Rarity { get; set; } = "";
    public int Points { get; set; }
    public string EarnedAt { get; set; } = "";
}

public class Achievement
{
    public string AchievementId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Points { get; set; }
    public string Category { get; set; } = "";
    public string EarnedAt { get; set; } = "";
}

