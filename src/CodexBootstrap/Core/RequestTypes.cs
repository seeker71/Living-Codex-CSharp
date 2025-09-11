using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Concept create request type
/// </summary>
[RequestType(
    id: "codex.meta/type/concept-create-request",
    name: "ConceptCreateRequest",
    description: "Request for concept creation"
)]
public class ConceptCreateRequest
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Concept name")]
    public string Name { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Concept description")]
    public string Description { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("domain", "string", Required = true, Description = "Concept domain")]
    public string Domain { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("complexity", "string", Required = true, Description = "Concept complexity")]
    public string Complexity { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("tags", "array", Required = true, Description = "Concept tags", Kind = "Array", ArrayItemType = "string")]
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Concept define request type
/// </summary>
[RequestType(
    id: "codex.meta/type/concept-define-request",
    name: "ConceptDefineRequest",
    description: "Request for concept definition"
)]
public class ConceptDefineRequest
{
    [MetaNodeFieldAttribute("conceptId", "string", Required = true, Description = "Concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("definition", "string", Required = true, Description = "Concept definition")]
    public string Definition { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("examples", "array", Description = "Concept examples", Kind = "Array", ArrayItemType = "string")]
    public string[]? Examples { get; set; }

    [MetaNodeFieldAttribute("relationships", "array", Description = "Concept relationships", Kind = "Array", ArrayItemType = "string")]
    public string[]? Relationships { get; set; }
}

/// <summary>
/// Concept relate request type
/// </summary>
[RequestType(
    id: "codex.meta/type/concept-relate-request",
    name: "ConceptRelateRequest",
    description: "Request for concept relationships"
)]
public class ConceptRelateRequest
{
    [MetaNodeFieldAttribute("sourceConceptId", "string", Required = true, Description = "Source concept identifier")]
    public string SourceConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("targetConceptId", "string", Required = true, Description = "Target concept identifier")]
    public string TargetConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("relationshipType", "string", Required = true, Description = "Type of relationship")]
    public string RelationshipType { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("weight", "number", Required = true, Description = "Relationship weight")]
    public double Weight { get; set; }
}

/// <summary>
/// Concept search request type
/// </summary>
[RequestType(
    id: "codex.meta/type/concept-search-request",
    name: "ConceptSearchRequest",
    description: "Request for concept search"
)]
public class ConceptSearchRequest
{
    [MetaNodeFieldAttribute("query", "string", Description = "Search query")]
    public string? Query { get; set; }

    [MetaNodeFieldAttribute("domain", "string", Description = "Domain filter")]
    public string? Domain { get; set; }

    [MetaNodeFieldAttribute("tags", "array", Description = "Tag filters", Kind = "Array", ArrayItemType = "string")]
    public string[]? Tags { get; set; }
}

/// <summary>
/// User concept link request type
/// </summary>
[RequestType(
    id: "codex.meta/type/user-concept-link-request",
    name: "UserConceptLinkRequest",
    description: "Request for user-concept linking"
)]
public class UserConceptLinkRequest
{
    [MetaNodeFieldAttribute("userId", "string", Required = true, Description = "User identifier")]
    public string UserId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("conceptId", "string", Required = true, Description = "Concept identifier")]
    public string ConceptId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("relationshipType", "string", Required = true, Description = "Type of relationship")]
    public string RelationshipType { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("weight", "number", Required = true, Description = "Relationship weight")]
    public double Weight { get; set; }
}

/// <summary>
/// User concept unlink request type
/// </summary>
[RequestType(
    id: "codex.meta/type/user-concept-unlink-request",
    name: "UserConceptUnlinkRequest",
    description: "Request for user-concept unlinking"
)]
public class UserConceptUnlinkRequest
{
    [MetaNodeFieldAttribute("userId", "string", Required = true, Description = "User identifier")]
    public string UserId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("conceptId", "string", Required = true, Description = "Concept identifier")]
    public string ConceptId { get; set; } = string.Empty;
}

/// <summary>
/// Breath loop request type
/// </summary>
[RequestType(
    id: "codex.meta/type/breath-loop-request",
    name: "BreathLoopRequest",
    description: "Request for breath loop operations"
)]
public class BreathLoopRequest
{
    [MetaNodeFieldAttribute("id", "string", Required = true, Description = "Node identifier")]
    public string Id { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("operations", "array", Description = "List of operations to perform", Kind = "Array", ArrayItemType = "string")]
    public string[]? Operations { get; set; }
}

/// <summary>
/// Spec compose request type
/// </summary>
[RequestType(
    id: "codex.meta/type/spec-compose-request",
    name: "SpecComposeRequest",
    description: "Request for spec composition"
)]
public class SpecComposeRequest
{
    [MetaNodeFieldAttribute("moduleId", "string", Description = "Module identifier")]
    public string? ModuleId { get; set; }

    [MetaNodeFieldAttribute("atoms", "array", Required = true, Description = "Atoms to compose", Kind = "Array", ArrayItemType = "object")]
    public object[] Atoms { get; set; } = Array.Empty<object>();
}
