using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public class Node
{
    public string Id { get; set; } = string.Empty;
    public string TypeId { get; set; } = string.Empty;
    public ContentState State { get; set; } = ContentState.Ice;
    public string? Locale { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ContentRef? Content { get; set; }
    public Dictionary<string, object>? Meta { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Edge
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double? Weight { get; set; }
    public Dictionary<string, object>? Meta { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ContentState
{
    Ice,    // Frozen/spec state
    Water,  // Mutable state
    Gas     // Transient state
}

public class ContentRef
{
    public string? InlineJson { get; set; }
    public string? ExternalUrl { get; set; }
    public string? MediaType { get; set; }
    public long? Size { get; set; }
    public string? Hash { get; set; }
}

// Query models
public record NodeListQuery(
    string? TypeId = null,
    ContentState? State = null,
    string? Locale = null,
    string? SearchTerm = null,
    int? Skip = null,
    int? Take = null
);

public record EdgeListQuery(
    string? FromId = null,
    string? ToId = null,
    string? Role = null,
    double? MinWeight = null,
    double? MaxWeight = null,
    int? Skip = null,
    int? Take = null
);

public record GraphSearchRequest(
    string Query = "",
    Dictionary<string, object>? Filters = null
);

public record EdgeListResponse(
    bool Success,
    List<Edge> Edges,
    int TotalCount,
    int Skip,
    int Take,
    string? Message = null
);

public record GraphQueryResult(
    string NodeId,
    string NodeType,
    string Title,
    string Description,
    Dictionary<string, object> Metadata,
    string Content
);

public record GraphSearchResponse(
    bool Success,
    List<GraphQueryResult> Results,
    string? Message = null
);

// Additional models for mapped API responses
public class NodeSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object>? Filters { get; set; }
    public int Limit { get; set; } = 10;
    public int Skip { get; set; } = 0;
}

public class NodeSearchResponse
{
    public bool Success { get; set; }
    public List<Node> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class NodeResponse
{
    public bool Success { get; set; }
    public Node Node { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class NodeListResponse
{
    public bool Success { get; set; }
    public List<Node> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class EdgeResponse
{
    public bool Success { get; set; }
    public Edge Edge { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class GraphRelationshipsResponse
{
    public bool Success { get; set; }
    public List<Node> RelatedNodes { get; set; } = new();
    public List<Edge> RelatedEdges { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserDiscoveryRequest
{
    public string UserId { get; set; } = string.Empty;
    public string DiscoveryType { get; set; } = string.Empty; // "concept", "recommendations", etc.
    public List<string>? Interests { get; set; }
    public int Limit { get; set; } = 10;
    public Dictionary<string, object>? Filters { get; set; }
}

public class UserDiscoveryResult
{
    public bool Success { get; set; }
    public List<Node> Concepts { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CreateEdgeRequest
{
    public string FromId { get; set; } = string.Empty;
    public string ToId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public Dictionary<string, object>? Meta { get; set; }
}

public class UserConceptsResponse
{
    public bool Success { get; set; }
    public List<Node> Concepts { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserConceptLinkRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ConceptId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public Dictionary<string, object>? Meta { get; set; }
}

public class UserConceptUnlinkRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ConceptId { get; set; } = string.Empty;
}

public class ContributionAnalysisRequest
{
    public string EntityId { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class ContributionAnalysisResponse
{
    public bool Success { get; set; }
    public Dictionary<string, object> Analysis { get; set; } = new();
    public string AnalysisId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
