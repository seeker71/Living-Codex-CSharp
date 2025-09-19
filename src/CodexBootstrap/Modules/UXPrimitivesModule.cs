using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// UX Primitives Module - Core interaction patterns: Weave, Reflect, Invite
/// Provides the fundamental interaction primitives that enable resonance-driven user experiences
/// </summary>
[MetaNode(Id = "codex.ux-primitives", Name = "UX Primitives Module", Description = "Core interaction patterns: Weave, Reflect, Invite")]
public sealed class UXPrimitivesModule : ModuleBase
{
    public override string Name => "UX Primitives Module";
    public override string Description => "Core interaction patterns: Weave, Reflect, Invite";
    public override string Version => "1.0.0";

    public UXPrimitivesModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.ux-primitives",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "ux", "primitives", "weave", "reflect", "invite", "interaction", "resonance" },
            capabilities: new[] { 
                "weave-connections", "reflect-insights", "invite-collaboration",
                "relationship-creation", "insight-generation", "invitation-system" 
            },
            spec: "codex.spec.ux-primitives"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _logger.Info("UX Primitives Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("UX Primitives Module HTTP endpoints registered");
    }

    // Weave - Create connections between concepts, people, or content
    [ApiRoute("POST", "/weave/create", "create-weave", "Create a weave connection between entities", "codex.ux-primitives")]
    public async Task<object> CreateWeave([ApiParameter("body", "Weave request", Required = true, Location = "body")] WeaveRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SourceId) || string.IsNullOrEmpty(request.TargetId))
            {
                return new ErrorResponse("Source ID and Target ID are required");
            }

            // Create weave as an edge in the registry
            var weaveId = $"weave-{Guid.NewGuid():N}";
            var weaveNode = new Node(
                Id: weaveId,
                TypeId: "codex.ux.weave",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Weave: {request.Relationship}",
                Description: $"Connection between {request.SourceId} and {request.TargetId}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["sourceId"] = request.SourceId,
                    ["targetId"] = request.TargetId,
                    ["relationship"] = request.Relationship,
                    ["strength"] = request.Strength,
                    ["userId"] = request.UserId ?? "",
                    ["createdAt"] = DateTime.UtcNow,
                    ["weaveType"] = "connection"
                }
            );

            _registry.Upsert(weaveNode);

            // Note: Edge creation would be handled by graph visualization module

            return new 
            { 
                success = true, 
                weaveId = weaveId,
                message = "Weave connection created successfully",
                relationship = request.Relationship,
                strength = request.Strength
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating weave: {ex.Message}", ex);
            return new ErrorResponse($"Error creating weave: {ex.Message}");
        }
    }

    // Reflect - Generate insights and deep contemplation
    [ApiRoute("POST", "/reflect/generate", "generate-reflection", "Generate reflection and insights for content", "codex.ux-primitives")]
    public async Task<object> GenerateReflection([ApiParameter("body", "Reflection request", Required = true, Location = "body")] ReflectRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ContentId))
            {
                return new ErrorResponse("Content ID is required");
            }

            // Get the content to reflect on
            if (!_registry.TryGet(request.ContentId, out var contentNode))
            {
                return new ErrorResponse("Content not found");
            }

            // Use AI to generate reflection
            var aiRequest = new
            {
                content = $"{contentNode.Title}\n\n{contentNode.Description}",
                provider = "openai",
                model = "gpt-5-mini"
            };

            using var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(aiRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync(GlobalConfiguration.GetUrl("/ai/extract-concepts"), content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (aiResponse.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    var reflection = GenerateReflectionFromAI(aiResponse, request.ReflectionType, request.Depth);
                    
                    // Store reflection as a node
                    var reflectionId = $"reflection-{Guid.NewGuid():N}";
                    var reflectionNode = new Node(
                        Id: reflectionId,
                        TypeId: "codex.ux.reflection",
                        State: ContentState.Water,
                        Locale: "en-US",
                        Title: $"Reflection on {contentNode.Title}",
                        Description: reflection.Insight,
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(reflection),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["contentId"] = request.ContentId,
                            ["reflectionType"] = request.ReflectionType,
                            ["depth"] = request.Depth,
                            ["userId"] = request.UserId ?? "",
                            ["createdAt"] = DateTime.UtcNow
                        }
                    );

                    _registry.Upsert(reflectionNode);

                    return new { success = true, reflection };
                }
            }

            return new ErrorResponse("AI reflection generation failed - no fallback data provided");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating reflection: {ex.Message}", ex);
            return new ErrorResponse($"Error generating reflection: {ex.Message}");
        }
    }

    // Invite - Send invitations for collaboration
    [ApiRoute("POST", "/invite/send", "send-invite", "Send invitation for collaboration or exploration", "codex.ux-primitives")]
    public async Task<object> SendInvite([ApiParameter("body", "Invite request", Required = true, Location = "body")] InviteRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ContentId) || string.IsNullOrEmpty(request.Message))
            {
                return new ErrorResponse("Content ID and message are required");
            }

            // Create invitation as a node
            var inviteId = $"invite-{Guid.NewGuid():N}";
            var inviteNode = new Node(
                Id: inviteId,
                TypeId: "codex.ux.invite",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Invitation: {request.InviteType}",
                Description: request.Message,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(request),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["contentId"] = request.ContentId,
                    ["inviteType"] = request.InviteType,
                    ["targetUserId"] = request.TargetUserId ?? "public",
                    ["fromUserId"] = request.FromUserId ?? "",
                    ["message"] = request.Message,
                    ["createdAt"] = DateTime.UtcNow,
                    ["status"] = "sent"
                }
            );

            _registry.Upsert(inviteNode);

            return new 
            { 
                success = true, 
                inviteId = inviteId,
                message = "Invitation sent successfully",
                inviteType = request.InviteType
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error sending invite: {ex.Message}", ex);
            return new ErrorResponse($"Error sending invite: {ex.Message}");
        }
    }

    // Get user's received invitations
    [ApiRoute("GET", "/invite/received/{userId}", "get-received-invites", "Get invitations received by user", "codex.ux-primitives")]
    public async Task<object> GetReceivedInvites([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            var inviteNodes = _registry.AllNodes()
                .Where(n => n.TypeId == "codex.ux.invite" && 
                           (n.Meta?.GetValueOrDefault("targetUserId")?.ToString() == userId ||
                            n.Meta?.GetValueOrDefault("targetUserId")?.ToString() == "public"))
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("createdAt"))
                .Take(20)
                .ToList();

            var invites = inviteNodes.Select(n => new
            {
                inviteId = n.Id,
                contentId = n.Meta?.GetValueOrDefault("contentId")?.ToString(),
                inviteType = n.Meta?.GetValueOrDefault("inviteType")?.ToString(),
                fromUserId = n.Meta?.GetValueOrDefault("fromUserId")?.ToString(),
                message = n.Description,
                createdAt = n.Meta?.GetValueOrDefault("createdAt"),
                status = n.Meta?.GetValueOrDefault("status")?.ToString()
            }).ToList();

            return new { success = true, invites, totalCount = invites.Count };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting received invites: {ex.Message}", ex);
            return new ErrorResponse($"Error getting received invites: {ex.Message}");
        }
    }

    // Helper methods
    private ReflectionResult GenerateReflectionFromAI(JsonElement aiResponse, string reflectionType, int depth)
    {
        // Extract insights from AI response and format as reflection
        var insight = "This content invites deeper exploration of consciousness and connection.";
        var guidance = "Consider how this relates to your personal journey and the collective evolution.";

        if (aiResponse.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
        {
            var concepts = dataElement.EnumerateArray().ToList();
            if (concepts.Any())
            {
                var topConcept = concepts.First();
                if (topConcept.TryGetProperty("description", out var descProp))
                {
                    insight = $"Key insight: {descProp.GetString()}";
                }
            }
        }

        return new ReflectionResult
        {
            Insight = insight,
            Guidance = guidance,
            ReflectionType = reflectionType,
            Depth = depth,
            Confidence = 0.8
        };
    }

}

// Data structures for UX primitives
[MetaNode(Id = "codex.ux-primitives.weave-request", Name = "Weave Request", Description = "Request to create a weave connection")]
public record WeaveRequest
{
    public string SourceId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string Relationship { get; set; } = "related";
    public double Strength { get; set; } = 0.7;
    public string? UserId { get; set; }
}

[MetaNode(Id = "codex.ux-primitives.reflect-request", Name = "Reflect Request", Description = "Request to generate reflection")]
public record ReflectRequest
{
    public string ContentId { get; set; } = "";
    public string ReflectionType { get; set; } = "insight";
    public int Depth { get; set; } = 3;
    public string? UserId { get; set; }
}

[MetaNode(Id = "codex.ux-primitives.invite-request", Name = "Invite Request", Description = "Request to send invitation")]
public record InviteRequest
{
    public string ContentId { get; set; } = "";
    public string InviteType { get; set; } = "collaboration";
    public string Message { get; set; } = "";
    public string? TargetUserId { get; set; }
    public string? FromUserId { get; set; }
}

[MetaNode(Id = "codex.ux-primitives.reflection-result", Name = "Reflection Result", Description = "Result of reflection generation")]
public record ReflectionResult
{
    public string Insight { get; set; } = "";
    public string Guidance { get; set; } = "";
    public string ReflectionType { get; set; } = "";
    public int Depth { get; set; } = 0;
    public double Confidence { get; set; } = 0.0;
}
