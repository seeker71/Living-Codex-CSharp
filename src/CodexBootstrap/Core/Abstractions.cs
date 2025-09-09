using System.Text.Json;

namespace CodexBootstrap.Core;

public enum ContentState { Ice, Water, Gas }

public sealed record Node(
    string Id,
    string TypeId,
    ContentState State,
    string? Locale,
    string? Title,
    string? Description,
    ContentRef? Content,
    Dictionary<string, object>? Meta
);

public sealed record ContentRef(
    string? MediaType,
    string? InlineJson,
    byte[]? InlineBytes,
    Uri? ExternalUri,
    string? Selector = null,
    string? Query = null,
    Dictionary<string,string>? Headers = null,
    string? AuthRef = null,
    string? CacheKey = null
);

public sealed record Edge(
    string FromId,
    string ToId,
    string Role,
    double? Weight,
    Dictionary<string, object>? Meta
);

// Enhanced type system to self‑describe payloads and APIs
public sealed record TypeSpec(
    string Name,
    string? Description,
    IReadOnlyList<FieldSpec>? Fields,
    TypeKind Kind = TypeKind.Object,
    string? ArrayItemType = null,
    string? ReferenceType = null,
    IReadOnlyList<string>? EnumValues = null
);

public enum TypeKind
{
    Object,
    Array,
    Reference,
    Enum,
    Primitive
}

public sealed record FieldSpec(
    string Name,
    string Type,
    bool Required,
    string? Description,
    TypeKind Kind = TypeKind.Primitive,
    string? ArrayItemType = null,
    string? ReferenceType = null,
    IReadOnlyList<string>? EnumValues = null
);

public sealed record ParameterSpec(
    string Name,
    string Type,
    bool Required,
    string? Description
);

public sealed record ApiSpec(
    string Name,
    string Verb,
    string Route,
    string? Description,
    IReadOnlyList<ParameterSpec>? Parameters
);

public sealed record ModuleRef(string Id, string Version);

public sealed record ModuleSpec(
    string Id,
    string Name,
    string Version,
    string? Description,
    string? Title,
    IReadOnlyList<ModuleRef> Dependencies,
    IReadOnlyList<TypeSpec> Types,
    IReadOnlyList<ApiSpec> Apis
);

public interface IModule
{
    Node GetModuleNode();
    void Register(NodeRegistry registry);
    void RegisterApiHandlers(IApiRouter router, NodeRegistry registry);
}

public interface IApiRouter
{
    void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler);
    bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler);
}

public interface ISynthesizer
{
    Task<Node> SynthesizeAsync(Node node, NodeRegistry registry);
}

// External source adapters resolve ContentRef.ExternalUri and/or Query
public interface ISourceAdapter
{
    string Scheme { get; } // e.g., http, https, file, ipfs, data, prompt
    Task<ContentRef?> ResolveAsync(ContentRef reference, NodeRegistry registry);
}

public interface IAdapterRegistry
{
    void Register(ISourceAdapter adapter);
    bool TryGet(string scheme, out ISourceAdapter adapter);
}

// Interface for modules to provide their OpenAPI specifications
public interface IOpenApiProvider
{
    object GetOpenApiSpec();
}

public sealed record DynamicCall(string ModuleId, string Api, JsonElement? Args);

// Additional types needed for the API
public sealed record ModuleAtoms(string Id, IReadOnlyList<Node> Nodes, IReadOnlyList<Edge> Edges);
public sealed record ResonanceProposal(string Id, string? AnchorId);
public sealed record ResonanceReport(bool IsValid, string Message);
public sealed record AdapterRegistration(string Scheme);
public sealed record PatchDoc(IReadOnlyList<PatchOperation> Operations);
public sealed record PatchOperation(string Op, string Path, object? Value);

// Interfaces for additional services
public interface ISpecGenerator
{
    ModuleSpec Generate(ModuleAtoms atoms);
}

public interface IValidator
{
    bool Validate(ModuleSpec spec);
}

public interface ISpecReflector
{
    IEnumerable<Node> ToNodes(ModuleSpec spec);
    ModuleSpec FromNodes(IEnumerable<Node> nodes);
}
