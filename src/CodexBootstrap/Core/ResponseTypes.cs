using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Success response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/success",
    name: "SuccessResponse",
    description: "Success response"
)]
public class SuccessResponse
{
    [MetaNodeField("success", "boolean", Required = true, Description = "Success indicator")]
    public bool Success { get; set; } = true;

    [MetaNodeField("message", "string", Description = "Success message")]
    public string? Message { get; set; }

    [MetaNodeField("data", "object", Description = "Response data")]
    public object? Data { get; set; }

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public SuccessResponse() { }

    public SuccessResponse(string message)
    {
        Message = message;
    }

    public SuccessResponse(object data)
    {
        Data = data;
    }

    public SuccessResponse(string message, object data)
    {
        Message = message;
        Data = data;
    }
}

/// <summary>
/// Error response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/error",
    name: "ErrorResponse",
    description: "Error response"
)]
public class ErrorResponse
{
    [MetaNodeField("success", "boolean", Required = true, Description = "Success indicator")]
    public bool Success { get; set; } = false;

    [MetaNodeField("error", "string", Required = true, Description = "Error message")]
    public string Error { get; set; } = string.Empty;

    [MetaNodeField("code", "string", Description = "Error code")]
    public string? Code { get; set; }

    [MetaNodeField("details", "object", Description = "Error details")]
    public object? Details { get; set; }

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ErrorResponse() { }

    public ErrorResponse(string error)
    {
        Error = error;
    }

    public ErrorResponse(string error, string code)
    {
        Error = error;
        Code = code;
    }

    public ErrorResponse(string error, string code, object details)
    {
        Error = error;
        Code = code;
        Details = details;
    }
}

/// <summary>
/// Health status response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/health-status",
    name: "HealthStatusResponse",
    description: "Health status response"
)]
public class HealthStatusResponse
{
    [MetaNodeField("status", "string", Required = true, Description = "System status")]
    public string Status { get; set; } = string.Empty;

    [MetaNodeField("uptime", "string", Required = true, Description = "System uptime")]
    public string Uptime { get; set; } = string.Empty;

    [MetaNodeField("requestCount", "number", Required = true, Description = "Total request count")]
    public long RequestCount { get; set; }

    [MetaNodeField("nodeCount", "number", Required = true, Description = "Total node count")]
    public int NodeCount { get; set; }

    [MetaNodeField("edgeCount", "number", Required = true, Description = "Total edge count")]
    public int EdgeCount { get; set; }

    [MetaNodeField("moduleCount", "number", Required = true, Description = "Total module count")]
    public int ModuleCount { get; set; }

    [MetaNodeField("version", "string", Required = true, Description = "Application version")]
    public string Version { get; set; } = string.Empty;

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Concept create response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/concept-create",
    name: "ConceptCreateResponse",
    description: "Response for concept creation"
)]
public class ConceptCreateResponse
{
    [MetaNodeField("success", "boolean", Required = true, Description = "Whether creation succeeded")]
    public bool Success { get; set; }

    [MetaNodeField("conceptId", "string", Required = true, Description = "Created concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeField("message", "string", Required = true, Description = "Response message")]
    public string Message { get; set; } = string.Empty;

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Concept define response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/concept-define",
    name: "ConceptDefineResponse",
    description: "Response for concept definition"
)]
public class ConceptDefineResponse
{
    [MetaNodeField("conceptId", "string", Required = true, Description = "Concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeField("properties", "object", Required = true, Description = "Defined properties", Kind = "Object")]
    public object Properties { get; set; } = new object();

    [MetaNodeField("message", "string", Required = true, Description = "Response message")]
    public string Message { get; set; } = string.Empty;

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User concept link response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/user-concept-link",
    name: "UserConceptLinkResponse",
    description: "Response for user-concept linking"
)]
public class UserConceptLinkResponse
{
    [MetaNodeField("success", "boolean", Required = true, Description = "Whether linking succeeded")]
    public bool Success { get; set; }

    [MetaNodeField("relationshipId", "string", Required = true, Description = "Relationship identifier")]
    public string RelationshipId { get; set; } = string.Empty;

    [MetaNodeField("relationshipType", "string", Required = true, Description = "Type of relationship")]
    public string RelationshipType { get; set; } = string.Empty;

    [MetaNodeField("weight", "number", Required = true, Description = "Relationship weight")]
    public double Weight { get; set; }

    [MetaNodeField("message", "string", Required = true, Description = "Response message")]
    public string Message { get; set; } = string.Empty;

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Breath loop response type
/// </summary>
[ResponseType(
    id: "codex.meta/response/breath-loop",
    name: "BreathLoopResponse",
    description: "Response for breath loop operations"
)]
public class BreathLoopResponse
{
    [MetaNodeField("id", "string", Required = true, Description = "Node identifier")]
    public string Id { get; set; } = string.Empty;

    [MetaNodeField("operation", "string", Required = true, Description = "Operation performed")]
    public string Operation { get; set; } = string.Empty;

    [MetaNodeField("success", "boolean", Required = true, Description = "Whether operation succeeded")]
    public bool Success { get; set; }

    [MetaNodeField("message", "string", Description = "Response message")]
    public string? Message { get; set; }

    [MetaNodeField("data", "object", Description = "Response data")]
    public object? Data { get; set; }

    [MetaNodeField("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}