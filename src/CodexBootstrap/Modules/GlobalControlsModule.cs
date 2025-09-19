using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Global Controls Module - Resonance Compass, Joy Tuner, Serendipity Dial, Curiosity Prompts
/// Provides the core UX primitives for the Living Codex experience
/// </summary>
[MetaNode(Id = "codex.global-controls", Name = "Global Controls Module", Description = "Core UX primitives: Resonance Compass, Joy Tuner, Serendipity Dial, Curiosity Prompts")]
public sealed class GlobalControlsModule : ModuleBase
{
    private readonly Dictionary<string, UserControlPreferences> _userPreferences = new();
    private readonly Random _serendipityRandom = new();

    public override string Name => "Global Controls Module";
    public override string Description => "Core UX primitives: Resonance Compass, Joy Tuner, Serendipity Dial, Curiosity Prompts";
    public override string Version => "1.0.0";

    public GlobalControlsModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.global-controls",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "controls", "ux", "resonance", "joy", "serendipity", "curiosity", "global" },
            capabilities: new[] { 
                "resonance-compass", "joy-tuner", "serendipity-dial", "curiosity-prompts",
                "user-preferences", "global-state-management", "ux-primitives" 
            },
            spec: "codex.spec.global-controls"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("Global Controls Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Global Controls Module HTTP endpoints registered");
    }

    // Get user control preferences
    [ApiRoute("GET", "/user-preferences/{userId}/controls", "get-user-controls", "Get user's global control preferences", "codex.global-controls")]
    public async Task<object> GetUserControlPreferences([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            // Try to get from registry first
            if (_registry.TryGet($"user-controls-{userId}", out var userControlsNode))
            {
                if (userControlsNode.Content?.InlineJson != null)
                {
                    var preferences = JsonSerializer.Deserialize<UserControlPreferences>(userControlsNode.Content.InlineJson);
                    return new { success = true, controls = preferences?.Controls };
                }
            }

            // Return default preferences
            var defaultControls = new GlobalControlsState
            {
                ResonanceLevel = 50,
                JoyLevel = 70,
                SerendipityLevel = 30,
                CuriosityLevel = 80
            };

            return new { success = true, controls = defaultControls };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting user control preferences: {ex.Message}", ex);
            return new ErrorResponse($"Error getting control preferences: {ex.Message}");
        }
    }

    // Update user control preferences
    [ApiRoute("PUT", "/user-preferences/{userId}/controls", "update-user-controls", "Update user's global control preferences", "codex.global-controls")]
    public async Task<object> UpdateUserControlPreferences(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("body", "Control preferences", Required = true, Location = "body")] UserControlPreferencesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            var preferences = new UserControlPreferences
            {
                UserId = userId,
                Controls = request.Controls,
                UpdatedAt = DateTime.UtcNow
            };

            // Store in registry
            var userControlsNode = new Node(
                Id: $"user-controls-{userId}",
                TypeId: "codex.user.controls",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Global Controls for {userId}",
                Description: "User's global control preferences and settings",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(preferences),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["updatedAt"] = preferences.UpdatedAt,
                    ["resonanceLevel"] = preferences.Controls.ResonanceLevel,
                    ["joyLevel"] = preferences.Controls.JoyLevel,
                    ["serendipityLevel"] = preferences.Controls.SerendipityLevel,
                    ["curiosityLevel"] = preferences.Controls.CuriosityLevel
                }
            );

            _registry.Upsert(userControlsNode);
            _userPreferences[userId] = preferences;

            return new { success = true, message = "Control preferences updated successfully" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating user control preferences: {ex.Message}", ex);
            return new ErrorResponse($"Error updating control preferences: {ex.Message}");
        }
    }

    // Trigger serendipitous discovery
    [ApiRoute("POST", "/serendipity/trigger", "trigger-serendipity", "Trigger serendipitous content discovery", "codex.global-controls")]
    public async Task<object> TriggerSerendipity([ApiParameter("body", "Serendipity request", Required = true, Location = "body")] SerendipityRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required");
            }

            // Get random concepts/nodes based on serendipity level
            var allNodes = _registry.AllNodes().ToList();
            var serendipityFactor = request.SerendipityLevel / 100.0;
            
            // Higher serendipity = more random, lower = more contextual
            var candidateNodes = serendipityFactor > 0.7 
                ? allNodes.OrderBy(_ => _serendipityRandom.Next()).Take(10).ToList()
                : allNodes.Where(n => n.TypeId.Contains("concept") || n.TypeId.Contains("news"))
                         .OrderBy(_ => _serendipityRandom.Next()).Take(10).ToList();

            if (candidateNodes.Any())
            {
                var selectedNode = candidateNodes[_serendipityRandom.Next(candidateNodes.Count)];
                var suggestion = new SerendipitySuggestion
                {
                    Id = selectedNode.Id,
                    Title = selectedNode.Title,
                    Description = selectedNode.Description ?? "Discover something new...",
                    Type = selectedNode.TypeId,
                    RelevanceScore = serendipityFactor,
                    DiscoveryReason = GenerateDiscoveryReason(serendipityFactor)
                };

                return new { success = true, suggestion };
            }

            return new { success = false, error = "No serendipitous content found" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error triggering serendipity: {ex.Message}", ex);
            return new ErrorResponse($"Error triggering serendipity: {ex.Message}");
        }
    }

    // Generate curiosity prompt
    [ApiRoute("POST", "/curiosity/generate-prompt", "generate-curiosity-prompt", "Generate curiosity-driven exploration prompt", "codex.global-controls")]
    public async Task<object> GenerateCuriosityPrompt([ApiParameter("body", "Curiosity request", Required = true, Location = "body")] CuriosityRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return new ErrorResponse("User ID is required");
            }

            var curiosityLevel = request.CuriosityLevel / 100.0;
            var interests = request.CurrentInterests ?? new List<string> { "consciousness", "technology", "unity" };

            // Generate prompt based on curiosity level and interests
            var prompt = GenerateCuriosityPromptText(curiosityLevel, interests);
            var guidance = GenerateCuriosityGuidance(curiosityLevel);

            var curiosityPrompt = new CuriosityPrompt
            {
                Question = prompt,
                Guidance = guidance,
                DifficultyLevel = curiosityLevel > 0.8 ? "Advanced" : curiosityLevel > 0.5 ? "Intermediate" : "Beginner",
                EstimatedTimeMinutes = (int)(curiosityLevel * 30) + 5,
                RelatedConcepts = interests.Take(3).ToList()
            };

            return new { success = true, prompt = curiosityPrompt };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating curiosity prompt: {ex.Message}", ex);
            return new ErrorResponse($"Error generating curiosity prompt: {ex.Message}");
        }
    }

    // Get resonance compass data
    [ApiRoute("GET", "/resonance/compass/{userId}", "get-resonance-compass", "Get resonance compass data for user", "codex.global-controls")]
    public async Task<object> GetResonanceCompass([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            // Calculate current resonance based on user interactions
            var userNodes = _registry.AllNodes()
                .Where(n => n.Meta?.GetValueOrDefault("userId")?.ToString() == userId)
                .ToList();

            var currentResonance = CalculateUserResonance(userNodes);
            var targetResonance = Math.Min(1.0, currentResonance + 0.2); // Always room for growth

            var compassData = new ResonanceCompassData
            {
                CurrentResonance = currentResonance,
                TargetResonance = targetResonance,
                ResonanceDirection = targetResonance > currentResonance ? "ascending" : "stable",
                HarmonicFrequency = 432 + (int)(currentResonance * 100) // 432-532 Hz range
            };

            return new { success = true, compass = compassData };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting resonance compass: {ex.Message}", ex);
            return new ErrorResponse($"Error getting resonance compass: {ex.Message}");
        }
    }

    // Helper methods
    private double CalculateUserResonance(List<Node> userNodes)
    {
        if (!userNodes.Any()) return 0.5; // Default resonance

        // Calculate based on node types and interactions
        var conceptNodes = userNodes.Count(n => n.TypeId.Contains("concept"));
        var contributionNodes = userNodes.Count(n => n.TypeId.Contains("contribution"));
        var interactionScore = Math.Min(1.0, (conceptNodes + contributionNodes) / 10.0);

        return Math.Max(0.3, Math.Min(1.0, interactionScore + 0.2));
    }

    private string GenerateDiscoveryReason(double serendipityFactor)
    {
        var reasons = serendipityFactor > 0.8 
            ? new[] { "Pure randomness led you here", "The universe conspired to show you this", "A wild discovery appeared!" }
            : serendipityFactor > 0.5
            ? new[] { "This relates to your recent interests", "You might find this intriguing", "Similar to what you've explored" }
            : new[] { "Directly relevant to your work", "Follows your current path", "Next logical step" };

        return reasons[_serendipityRandom.Next(reasons.Length)];
    }

    private string GenerateCuriosityPromptText(double curiosityLevel, List<string> interests)
    {
        var basePrompts = new[]
        {
            "What if {interest} could be experienced differently?",
            "How might {interest} evolve in the next decade?",
            "What connections exist between {interest} and consciousness?",
            "What would happen if we amplified {interest} through unity?",
            "How does {interest} relate to abundance and joy?"
        };

        var interest = interests.Any() ? interests[_serendipityRandom.Next(interests.Count)] : "consciousness";
        var promptTemplate = basePrompts[_serendipityRandom.Next(basePrompts.Length)];
        
        return promptTemplate.Replace("{interest}", interest);
    }

    private string GenerateCuriosityGuidance(double curiosityLevel)
    {
        if (curiosityLevel > 0.8)
        {
            return "Dive deep and explore the edges of possibility. Question everything and seek novel connections.";
        }
        else if (curiosityLevel > 0.5)
        {
            return "Explore with gentle curiosity. Notice what draws your attention and follow those threads.";
        }
        else
        {
            return "Start with what feels familiar and gradually expand your exploration from there.";
        }
    }
}

// Data structures for global controls
[MetaNode(Id = "codex.global-controls.state", Name = "Global Controls State", Description = "State of global UX controls")]
public record GlobalControlsState
{
    public int ResonanceLevel { get; set; } = 50;
    public int JoyLevel { get; set; } = 70;
    public int SerendipityLevel { get; set; } = 30;
    public int CuriosityLevel { get; set; } = 80;
}

[MetaNode(Id = "codex.global-controls.user-preferences", Name = "User Control Preferences", Description = "User's global control preferences")]
public record UserControlPreferences
{
    public string UserId { get; set; } = "";
    public GlobalControlsState Controls { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[MetaNode(Id = "codex.global-controls.user-preferences-request", Name = "User Control Preferences Request", Description = "Request to update user control preferences")]
public record UserControlPreferencesRequest
{
    public GlobalControlsState Controls { get; set; } = new();
}

[MetaNode(Id = "codex.global-controls.serendipity-request", Name = "Serendipity Request", Description = "Request for serendipitous discovery")]
public record SerendipityRequest
{
    public string UserId { get; set; } = "";
    public int SerendipityLevel { get; set; } = 50;
    public string CurrentContext { get; set; } = "";
}

[MetaNode(Id = "codex.global-controls.serendipity-suggestion", Name = "Serendipity Suggestion", Description = "Serendipitous content suggestion")]
public record SerendipitySuggestion
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    public double RelevanceScore { get; set; } = 0.0;
    public string DiscoveryReason { get; set; } = "";
}

[MetaNode(Id = "codex.global-controls.curiosity-request", Name = "Curiosity Request", Description = "Request for curiosity prompt generation")]
public record CuriosityRequest
{
    public string UserId { get; set; } = "";
    public int CuriosityLevel { get; set; } = 80;
    public List<string>? CurrentInterests { get; set; } = null;
}

[MetaNode(Id = "codex.global-controls.curiosity-prompt", Name = "Curiosity Prompt", Description = "Generated curiosity exploration prompt")]
public record CuriosityPrompt
{
    public string Question { get; set; } = "";
    public string Guidance { get; set; } = "";
    public string DifficultyLevel { get; set; } = "Intermediate";
    public int EstimatedTimeMinutes { get; set; } = 15;
    public List<string> RelatedConcepts { get; set; } = new();
}

[MetaNode(Id = "codex.global-controls.resonance-compass-data", Name = "Resonance Compass Data", Description = "Data for resonance compass visualization")]
public record ResonanceCompassData
{
    public double CurrentResonance { get; set; } = 0.5;
    public double TargetResonance { get; set; } = 0.8;
    public string ResonanceDirection { get; set; } = "ascending";
    public int HarmonicFrequency { get; set; } = 432;
}
