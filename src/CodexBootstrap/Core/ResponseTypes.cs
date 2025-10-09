using System;
using System.Text.Json.Serialization;

namespace CodexBootstrap.Core;

/// <summary>
/// API Type Attribute for documenting API types
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public class ApiTypeAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

/// <summary>
/// API Module Attribute for documenting API modules
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ApiModuleAttribute : Attribute
{
    public string ModuleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Generate Endpoint Attribute for dynamic endpoint generation
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class GenerateEndpointAttribute : Attribute
{
    public string HttpMethod { get; set; } = "GET";
    public string Route { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
    public bool UseBreathFramework { get; set; } = true;
    public string[] RequiredPhases { get; set; } = new[] { "compose", "expand", "validate", "contract" };
}

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
    [MetaNodeFieldAttribute("success", "boolean", Required = true, Description = "Success indicator")]
    public bool Success { get; set; } = true;

    [MetaNodeFieldAttribute("message", "string", Description = "Success message")]
    public string? Message { get; set; }

    [MetaNodeFieldAttribute("data", "object", Description = "Response data")]
    public object? Data { get; set; }

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
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
    [MetaNodeFieldAttribute("success", "boolean", Required = true, Description = "Success indicator")]
    public bool Success { get; set; } = false;

    [MetaNodeFieldAttribute("error", "string", Required = true, Description = "Error message")]
    public string Error { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("code", "string", Description = "Error code")]
    public string? Code { get; set; }

    [MetaNodeFieldAttribute("details", "object", Description = "Error details")]
    public object? Details { get; set; }

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
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
    [MetaNodeFieldAttribute("status", "string", Required = true, Description = "System status")]
    public string Status { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("uptime", "string", Required = true, Description = "System uptime")]
    public string Uptime { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("requestCount", "number", Required = true, Description = "Total request count")]
    public long RequestCount { get; set; }

    [MetaNodeFieldAttribute("nodeCount", "number", Required = true, Description = "Total node count")]
    public int NodeCount { get; set; }

    [MetaNodeFieldAttribute("edgeCount", "number", Required = true, Description = "Total edge count")]
    public int EdgeCount { get; set; }

    [MetaNodeFieldAttribute("moduleCount", "number", Required = true, Description = "Total module count")]
    public int ModuleCount { get; set; }

    [MetaNodeFieldAttribute("version", "string", Required = true, Description = "Application version")]
    public string Version { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
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
    [MetaNodeFieldAttribute("success", "boolean", Required = true, Description = "Whether creation succeeded")]
    public bool Success { get; set; }

    [MetaNodeFieldAttribute("conceptId", "string", Required = true, Description = "Created concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("message", "string", Required = true, Description = "Response message")]
    public string Message { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
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
    [MetaNodeFieldAttribute("conceptId", "string", Required = true, Description = "Concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("properties", "object", Required = true, Description = "Defined properties", Kind = "Object")]
    public object Properties { get; set; } = new object();

    [MetaNodeFieldAttribute("message", "string", Required = true, Description = "Response message")]
    public string Message { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
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
public record UserConceptLinkResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("relationshipId")] string RelationshipId,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("relationshipType")] string? RelationshipType = null,
    [property: JsonPropertyName("weight")] double? Weight = null,
    [property: JsonPropertyName("timestamp")] DateTime? Timestamp = null
)
{
    public UserConceptLinkResponse(bool success, string relationshipId, string message)
        : this(success, relationshipId, message, null, null, DateTime.UtcNow) { }
};

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
    [MetaNodeFieldAttribute("id", "string", Required = true, Description = "Node identifier")]
    public string Id { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("operation", "string", Required = true, Description = "Operation performed")]
    public string Operation { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("success", "boolean", Required = true, Description = "Whether operation succeeded")]
    public bool Success { get; set; }

    [MetaNodeFieldAttribute("message", "string", Description = "Response message")]
    public string? Message { get; set; }

    [MetaNodeFieldAttribute("data", "object", Description = "Response data")]
    public object? Data { get; set; }

    [MetaNodeFieldAttribute("timestamp", "string", Required = true, Description = "Response timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}