using System;

namespace CodexBootstrap.Core;

/// <summary>
/// Attribute to declare a class as a meta-node in the Living Codex system
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class MetaNodeAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for this meta-node
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The type identifier for this meta-node (e.g., "codex.meta/type", "codex.meta/state")
    /// </summary>
    public new string TypeId { get; }

    /// <summary>
    /// The display name for this meta-node
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of this meta-node
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The version of this meta-node
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The locale for this meta-node
    /// </summary>
    public string Locale { get; set; } = "en";

    /// <summary>
    /// Whether this meta-node is a core system node
    /// </summary>
    public bool IsCore { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is a state definition
    /// </summary>
    public bool IsState { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is a type definition
    /// </summary>
    public bool IsType { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is an API definition
    /// </summary>
    public bool IsApi { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is a response definition
    /// </summary>
    public bool IsResponse { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is a request definition
    /// </summary>
    public bool IsRequest { get; set; } = false;

    /// <summary>
    /// Whether this meta-node is a module definition
    /// </summary>
    public bool IsModule { get; set; } = false;

    /// <summary>
    /// The parent type for state meta-nodes (e.g., "ContentState", "TypeKind")
    /// </summary>
    public string? ParentType { get; set; }

    /// <summary>
    /// The value for state meta-nodes (e.g., "ice", "water", "gas")
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// The kind for type meta-nodes (e.g., "Object", "Array", "Reference")
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// The module ID for module-specific meta-nodes
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The API name for API meta-nodes
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// The route for API meta-nodes
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// The HTTP verb for API meta-nodes
    /// </summary>
    public string? Verb { get; set; }

    /// <summary>
    /// Additional metadata as key-value pairs
    /// </summary>
    public string[]? AdditionalMeta { get; set; }

    /// <summary>
    /// Initializes a new instance of the MetaNodeAttribute
    /// </summary>
    /// <param name="id">The unique identifier for this meta-node</param>
    /// <param name="typeId">The type identifier for this meta-node</param>
    /// <param name="name">The display name for this meta-node</param>
    /// <param name="description">The description of this meta-node</param>
    public MetaNodeAttribute(string id, string typeId, string name, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TypeId = typeId ?? throw new ArgumentNullException(nameof(typeId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Attribute to declare a property as a field in a meta-node type definition
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class MetaNodeFieldAttribute : Attribute
{
    /// <summary>
    /// The name of the field
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the field
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Whether the field is required
    /// </summary>
    public bool Required { get; set; } = false;

    /// <summary>
    /// The description of the field
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The kind of the field (e.g., "Primitive", "Object", "Array", "Reference")
    /// </summary>
    public string Kind { get; set; } = "Primitive";

    /// <summary>
    /// The array item type for array fields
    /// </summary>
    public string? ArrayItemType { get; set; }

    /// <summary>
    /// The reference type for reference fields
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// The enum values for enum fields
    /// </summary>
    public string[]? EnumValues { get; set; }

    /// <summary>
    /// Initializes a new instance of the MetaNodeFieldAttribute
    /// </summary>
    /// <param name="name">The name of the field</param>
    /// <param name="type">The type of the field</param>
    public MetaNodeFieldAttribute(string name, string type)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }
}

/// <summary>
/// Attribute to declare a class as a response type in the Living Codex system
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class ResponseTypeAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for this response type
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The name of this response type
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of this response type
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The module ID that uses this response type
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The API name that uses this response type
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// Initializes a new instance of the ResponseTypeAttribute
    /// </summary>
    /// <param name="id">The unique identifier for this response type</param>
    /// <param name="name">The name of this response type</param>
    /// <param name="description">The description of this response type</param>
    public ResponseTypeAttribute(string id, string name, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Attribute to declare a class as a request type in the Living Codex system
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class RequestTypeAttribute : Attribute
{
    /// <summary>
    /// The unique identifier for this request type
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The name of this request type
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The description of this request type
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The module ID that uses this request type
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// The API name that uses this request type
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// Initializes a new instance of the RequestTypeAttribute
    /// </summary>
    /// <param name="id">The unique identifier for this request type</param>
    /// <param name="name">The name of this request type</param>
    /// <param name="description">The description of this request type</param>
    public RequestTypeAttribute(string id, string name, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}
