using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Core node entity meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type/node",
    typeId: "codex.meta/type",
    name: "Node",
    description: "Core node entity"
)]
public class NodeMetaNode
{
    [MetaNodeField("id", "string", Required = true, Description = "Unique identifier")]
    public string Id { get; set; } = string.Empty;

    [MetaNodeField("typeId", "string", Required = true, Description = "Type identifier")]
    public string TypeId { get; set; } = string.Empty;

    [MetaNodeField("state", "string", Required = true, Description = "Content state", Kind = "Enum", EnumValues = new[] { "Ice", "Water", "Gas" })]
    public string State { get; set; } = string.Empty;

    [MetaNodeField("locale", "string", Description = "Locale information")]
    public string? Locale { get; set; }

    [MetaNodeField("title", "string", Description = "Display title")]
    public string? Title { get; set; }

    [MetaNodeField("description", "string", Description = "Description")]
    public string? Description { get; set; }

    [MetaNodeField("content", "ContentRef", Description = "Content reference", Kind = "Reference", ReferenceType = "ContentRef")]
    public object? Content { get; set; }

    [MetaNodeField("meta", "object", Description = "Metadata dictionary", Kind = "Object")]
    public object? Meta { get; set; }
}

/// <summary>
/// Core edge entity meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type/edge",
    typeId: "codex.meta/type",
    name: "Edge",
    description: "Core edge entity"
)]
public class EdgeMetaNode
{
    [MetaNodeField("fromId", "string", Required = true, Description = "Source node identifier")]
    public string FromId { get; set; } = string.Empty;

    [MetaNodeField("toId", "string", Required = true, Description = "Target node identifier")]
    public string ToId { get; set; } = string.Empty;

    [MetaNodeField("role", "string", Required = true, Description = "Edge role")]
    public string Role { get; set; } = string.Empty;

    [MetaNodeField("weight", "number", Description = "Edge weight")]
    public double? Weight { get; set; }

    [MetaNodeField("meta", "object", Description = "Edge metadata", Kind = "Object")]
    public object? Meta { get; set; }
}

/// <summary>
/// Content reference meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type/content-ref",
    typeId: "codex.meta/type",
    name: "ContentRef",
    description: "Content reference"
)]
public class ContentRefMetaNode
{
    [MetaNodeField("mediaType", "string", Description = "MIME type")]
    public string? MediaType { get; set; }

    [MetaNodeField("inlineJson", "string", Description = "Inline JSON content")]
    public string? InlineJson { get; set; }

    [MetaNodeField("inlineBytes", "array", Description = "Inline binary content", Kind = "Array", ArrayItemType = "byte")]
    public byte[]? InlineBytes { get; set; }

    [MetaNodeField("externalUri", "string", Description = "External URI")]
    public string? ExternalUri { get; set; }

    [MetaNodeField("selector", "string", Description = "Content selector")]
    public string? Selector { get; set; }

    [MetaNodeField("query", "string", Description = "Content query")]
    public string? Query { get; set; }

    [MetaNodeField("headers", "object", Description = "HTTP headers", Kind = "Object")]
    public object? Headers { get; set; }

    [MetaNodeField("authRef", "string", Description = "Authentication reference")]
    public string? AuthRef { get; set; }

    [MetaNodeField("cacheKey", "string", Description = "Cache key")]
    public string? CacheKey { get; set; }
}

/// <summary>
/// Ice state meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/state/ice",
    typeId: "codex.meta/state",
    name: "Ice",
    description: "Frozen, immutable state"
)]
public class IceStateMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Ice";

    [MetaNodeField("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Frozen, immutable state";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeField("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "ice";
}

/// <summary>
/// Water state meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/state/water",
    typeId: "codex.meta/state",
    name: "Water",
    description: "Liquid, mutable state"
)]
public class WaterStateMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Water";

    [MetaNodeField("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Liquid, mutable state";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeField("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "water";
}

/// <summary>
/// Gas state meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/state/gas",
    typeId: "codex.meta/state",
    name: "Gas",
    description: "Transient, derivable state"
)]
public class GasStateMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Gas";

    [MetaNodeField("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Transient, derivable state";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeField("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "gas";
}

/// <summary>
/// Object type kind meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type-kind/object",
    typeId: "codex.meta/type-kind",
    name: "Object",
    description: "Object type"
)]
public class ObjectTypeKindMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Object";

    [MetaNodeField("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Object type";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeField("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "object";
}

/// <summary>
/// Array type kind meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type-kind/array",
    typeId: "codex.meta/type-kind",
    name: "Array",
    description: "Array type"
)]
public class ArrayTypeKindMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Array";

    [MetaNodeField("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Array type";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeField("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "array";
}

/// <summary>
/// Reference type kind meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type-kind/reference",
    typeId: "codex.meta/type-kind",
    name: "Reference",
    description: "Reference type"
)]
public class ReferenceTypeKindMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Reference";

    [MetaNodeField("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Reference type";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeField("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "reference";
}

/// <summary>
/// Enum type kind meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type-kind/enum",
    typeId: "codex.meta/type-kind",
    name: "Enum",
    description: "Enumeration type"
)]
public class EnumTypeKindMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Enum";

    [MetaNodeField("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Enumeration type";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeField("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "enum";
}

/// <summary>
/// Primitive type kind meta-node
/// </summary>
[MetaNode(
    id: "codex.meta/type-kind/primitive",
    typeId: "codex.meta/type-kind",
    name: "Primitive",
    description: "Primitive type"
)]
public class PrimitiveTypeKindMetaNode
{
    [MetaNodeField("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Primitive";

    [MetaNodeField("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Primitive type";

    [MetaNodeField("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeField("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "primitive";
}
