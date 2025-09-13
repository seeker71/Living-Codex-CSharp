using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public record Conversation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("type")] ConversationType Type,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("participants")] List<ConversationParticipant> Participants,
    [property: JsonPropertyName("conceptId")] string? ConceptId = null,
    [property: JsonPropertyName("parentConversationId")] string? ParentConversationId = null,
    [property: JsonPropertyName("resonanceThreshold")] double ResonanceThreshold = 0.5,
    [property: JsonPropertyName("isPublic")] bool IsPublic = false,
    [property: JsonPropertyName("waterNodeId")] string? WaterNodeId = null,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null
);

public record ConversationParticipant(
    [property: JsonPropertyName("participantId")] string ParticipantId,
    [property: JsonPropertyName("participantType")] ParticipantType ParticipantType,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("role")] ParticipantRole Role,
    [property: JsonPropertyName("joinedAt")] DateTimeOffset JoinedAt,
    [property: JsonPropertyName("userId")] string? UserId = null,
    [property: JsonPropertyName("conceptId")] string? ConceptId = null,
    [property: JsonPropertyName("lastSeenAt")] DateTimeOffset? LastSeenAt = null,
    [property: JsonPropertyName("isActive")] bool IsActive = true,
    [property: JsonPropertyName("resonanceScore")] double? ResonanceScore = null
);

public record ConversationMessage(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("senderId")] string SenderId,
    [property: JsonPropertyName("senderType")] ParticipantType SenderType,
    [property: JsonPropertyName("senderName")] string SenderName,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("messageType")] MessageType MessageType,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("editedAt")] DateTimeOffset? EditedAt = null,
    [property: JsonPropertyName("parentMessageId")] string? ParentMessageId = null,
    [property: JsonPropertyName("waterNodeId")] string? WaterNodeId = null,
    [property: JsonPropertyName("resonanceScore")] double? ResonanceScore = null,
    [property: JsonPropertyName("correlations")] List<MessageCorrelation>? Correlations = null,
    [property: JsonPropertyName("eventId")] string? EventId = null,
    [property: JsonPropertyName("isWaterNode")] bool IsWaterNode = false,
    [property: JsonPropertyName("conceptLinks")] List<string>? ConceptLinks = null,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null
);

public record MessageCorrelation(
    [property: JsonPropertyName("targetMessageId")] string TargetMessageId,
    [property: JsonPropertyName("correlationType")] CorrelationType CorrelationType,
    [property: JsonPropertyName("strength")] double Strength,
    [property: JsonPropertyName("conceptId")] string? ConceptId = null,
    [property: JsonPropertyName("description")] string? Description = null
);

public record FractalChannel(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("conversationId")] string ConversationId,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("parentChannelId")] string? ParentChannelId = null,
    [property: JsonPropertyName("conceptId")] string? ConceptId = null,
    [property: JsonPropertyName("resonanceThreshold")] double ResonanceThreshold = 0.5,
    [property: JsonPropertyName("isActive")] bool IsActive = true
);

public record ChannelResonance(
    [property: JsonPropertyName("channelId")] string ChannelId,
    [property: JsonPropertyName("messageId")] string MessageId,
    [property: JsonPropertyName("resonanceScore")] double ResonanceScore,
    [property: JsonPropertyName("resonanceType")] ResonanceType ResonanceType,
    [property: JsonPropertyName("correlatedConcepts")] List<string> CorrelatedConcepts,
    [property: JsonPropertyName("amplifiedBy")] List<string> AmplifiedBy,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt
);

public enum ConversationType
{
    General,
    ConceptFocused,
    Collaborative,
    FractalChannel,
    ResonanceFlow
}

public enum ParticipantType
{
    User,
    Concept,
    System,
    WaterNode
}

public enum ParticipantRole
{
    Observer,
    Contributor,
    Moderator,
    Creator,
    Resonator,
    Amplifier
}

public enum MessageType
{
    Text,
    WaterNode,
    ConceptLink,
    Resonance,
    Fractal,
    System
}

public enum CorrelationType
{
    Semantic,
    Conceptual,
    Temporal,
    Emotional,
    Resonant,
    Fractal
}

public enum ResonanceType
{
    Direct,
    Amplified,
    Collective,
    Fractal,
    CrossChannel
}

// Request/Response DTOs
public record ConversationCreateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("type")] ConversationType Type,
    [property: JsonPropertyName("createdBy")] string CreatedBy,
    [property: JsonPropertyName("conceptId")] string? ConceptId = null,
    [property: JsonPropertyName("parentConversationId")] string? ParentConversationId = null,
    [property: JsonPropertyName("resonanceThreshold")] double ResonanceThreshold = 0.5,
    [property: JsonPropertyName("isPublic")] bool IsPublic = false,
    [property: JsonPropertyName("waterNodeId")] string? WaterNodeId = null,
    [property: JsonPropertyName("metadata")] Dictionary<string, object>? Metadata = null
);