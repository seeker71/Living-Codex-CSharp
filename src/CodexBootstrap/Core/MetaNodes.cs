using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Core node entity meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type/node",
    typeId: "codex.meta/type",
    name: "Node",
    description: "Core node entity"
)]
public class NodeMetaNode
{
    [MetaNodeFieldAttribute("id", "string", Required = true, Description = "Unique identifier")]
    public string Id { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("typeId", "string", Required = true, Description = "Type identifier")]
    public string TypeId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("state", "string", Required = true, Description = "Content state", Kind = "Enum", EnumValues = new[] { "Ice", "Water", "Gas" })]
    public string State { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("locale", "string", Description = "Locale information")]
    public string? Locale { get; set; }

    [MetaNodeFieldAttribute("title", "string", Description = "Display title")]
    public string? Title { get; set; }

    [MetaNodeFieldAttribute("description", "string", Description = "Description")]
    public string? Description { get; set; }

    [MetaNodeFieldAttribute("content", "ContentRef", Description = "Content reference", Kind = "Reference", ReferenceType = "ContentRef")]
    public object? Content { get; set; }

    [MetaNodeFieldAttribute("meta", "object", Description = "Metadata dictionary", Kind = "Object")]
    public object? Meta { get; set; }
}

/// <summary>
/// Core edge entity meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type/edge",
    typeId: "codex.meta/type",
    name: "Edge",
    description: "Core edge entity"
)]
public class EdgeMetaNode
{
    [MetaNodeFieldAttribute("fromId", "string", Required = true, Description = "Source node identifier")]
    public string FromId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("toId", "string", Required = true, Description = "Target node identifier")]
    public string ToId { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("role", "string", Required = true, Description = "Edge role")]
    public string Role { get; set; } = string.Empty;

    [MetaNodeFieldAttribute("roleId", "string", Description = "Relationship type node id (codex.relationship.core)")]
    public string? RoleId { get; set; }

    [MetaNodeFieldAttribute("weight", "number", Description = "Edge weight")]
    public double? Weight { get; set; }

    [MetaNodeFieldAttribute("meta", "object", Description = "Edge metadata", Kind = "Object")]
    public object? Meta { get; set; }
}

/// <summary>
/// Content reference meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type/content-ref",
    typeId: "codex.meta/type",
    name: "ContentRef",
    description: "Content reference"
)]
public class ContentRefMetaNode
{
    [MetaNodeFieldAttribute("mediaType", "string", Description = "MIME type")]
    public string? MediaType { get; set; }

    [MetaNodeFieldAttribute("inlineJson", "string", Description = "Inline JSON content")]
    public string? InlineJson { get; set; }

    [MetaNodeFieldAttribute("inlineBytes", "array", Description = "Inline binary content", Kind = "Array", ArrayItemType = "byte")]
    public byte[]? InlineBytes { get; set; }

    [MetaNodeFieldAttribute("externalUri", "string", Description = "External URI")]
    public string? ExternalUri { get; set; }

    [MetaNodeFieldAttribute("selector", "string", Description = "Content selector")]
    public string? Selector { get; set; }

    [MetaNodeFieldAttribute("query", "string", Description = "Content query")]
    public string? Query { get; set; }

    [MetaNodeFieldAttribute("headers", "object", Description = "HTTP headers", Kind = "Object")]
    public object? Headers { get; set; }

    [MetaNodeFieldAttribute("authRef", "string", Description = "Authentication reference")]
    public string? AuthRef { get; set; }

    [MetaNodeFieldAttribute("cacheKey", "string", Description = "Cache key")]
    public string? CacheKey { get; set; }
}

/// <summary>
/// Ice state meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/state/ice",
    typeId: "codex.meta/state",
    name: "Ice",
    description: "Frozen, immutable state"
)]
public class IceStateMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Ice";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Frozen, immutable state";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "ice";
}

/// <summary>
/// Water state meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/state/water",
    typeId: "codex.meta/state",
    name: "Water",
    description: "Liquid, mutable state"
)]
public class WaterStateMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Water";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Liquid, mutable state";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "water";
}

/// <summary>
/// Gas state meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/state/gas",
    typeId: "codex.meta/state",
    name: "Gas",
    description: "Transient, derivable state"
)]
public class GasStateMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "State name")]
    public string Name { get; set; } = "Gas";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "State description")]
    public string Description { get; set; } = "Transient, derivable state";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "ContentState";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "State value")]
    public string Value { get; set; } = "gas";
}

/// <summary>
/// Object type kind meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type-kind/object",
    typeId: "codex.meta/type-kind",
    name: "Object",
    description: "Object type"
)]
public class ObjectTypeKindMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Object";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Object type";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "object";
}

/// <summary>
/// Array type kind meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type-kind/array",
    typeId: "codex.meta/type-kind",
    name: "Array",
    description: "Array type"
)]
public class ArrayTypeKindMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Array";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Array type";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "array";
}

/// <summary>
/// Reference type kind meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type-kind/reference",
    typeId: "codex.meta/type-kind",
    name: "Reference",
    description: "Reference type"
)]
public class ReferenceTypeKindMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Reference";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Reference type";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "reference";
}

/// <summary>
/// Enum type kind meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type-kind/enum",
    typeId: "codex.meta/type-kind",
    name: "Enum",
    description: "Enumeration type"
)]
public class EnumTypeKindMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Enum";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Enumeration type";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "enum";
}

/// <summary>
/// Primitive type kind meta-node
/// </summary>
[MetaNodeAttribute(
    id: "codex.meta/type-kind/primitive",
    typeId: "codex.meta/type-kind",
    name: "Primitive",
    description: "Primitive type"
)]
public class PrimitiveTypeKindMetaNode
{
    [MetaNodeFieldAttribute("name", "string", Required = true, Description = "Type kind name")]
    public string Name { get; set; } = "Primitive";

    [MetaNodeFieldAttribute("description", "string", Required = true, Description = "Type kind description")]
    public string Description { get; set; } = "Primitive type";

    [MetaNodeFieldAttribute("parentType", "string", Required = true, Description = "Parent type")]
    public string ParentType { get; set; } = "TypeKind";

    [MetaNodeFieldAttribute("value", "string", Required = true, Description = "Type kind value")]
    public string Value { get; set; } = "primitive";
}
