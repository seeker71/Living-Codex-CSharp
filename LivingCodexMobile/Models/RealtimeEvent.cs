using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public record RealtimeMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("data")] object Data,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("sender")] string Sender
);

public record RealtimeEvent(
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("nodeId")] string? NodeId,
    [property: JsonPropertyName("nodeType")] string? NodeType,
    [property: JsonPropertyName("fromId")] string? FromId,
    [property: JsonPropertyName("toId")] string? ToId,
    [property: JsonPropertyName("userId")] string? UserId,
    [property: JsonPropertyName("data")] object? Data,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record ContributionEvent(
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("entityId")] string EntityId,
    [property: JsonPropertyName("data")] Dictionary<string, object> Data,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record AbundanceEvent(
    [property: JsonPropertyName("abundanceMultiplier")] decimal AbundanceMultiplier,
    [property: JsonPropertyName("collectiveValue")] decimal CollectiveValue,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record SystemEvent(
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("data")] object Data,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record ConnectionInfo(
    [property: JsonPropertyName("connectionId")] string ConnectionId,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record GroupMessage(
    [property: JsonPropertyName("groupName")] string GroupName,
    [property: JsonPropertyName("message")] object Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);

public record DirectMessage(
    [property: JsonPropertyName("targetConnectionId")] string TargetConnectionId,
    [property: JsonPropertyName("message")] object Message,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp
);


