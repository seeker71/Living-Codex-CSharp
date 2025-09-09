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

// Minimal type system to self‑describe payloads and APIs
public sealed record TypeSpec(
    string Name,
    string Kind, // "object" | "string" | "number" | "boolean" | "array" | "ref"
    Dictionary<string, TypeSpec>? Properties = null,
    TypeSpec? Items = null,
    string? Ref = null,
    string? MediaType = null
);

public sealed record ApiSpec(
    string Name,
    string Route,
    string? Description,
    TypeSpec? Input,
    TypeSpec? Output
);

public sealed record ModuleRef(string Id, string Version);

public sealed record ModuleSpec(
    string Id,
    string Version,
    string Name,
    string? Description,
    IReadOnlyList<ModuleRef> Dependencies,
    IReadOnlyList<TypeSpec> Types,
    IReadOnlyList<ApiSpec> Apis
);

public interface IModule
{
    ModuleSpec Spec { get; }
    void Register(IApiRouter router, IRegistry registry);
}

public interface IApiRouter
{
    void Register(string moduleId, string api, Func<JsonElement?, Task<object?>> handler);
}

public interface IRegistry
{
    void Upsert(Node node);
    void Upsert(Edge edge);
    bool TryGet(string id, out Node node);
    IEnumerable<Edge> AllEdges();
}

public interface ISynthesizer
{
    Task<Node> SynthesizeAsync(Node node, IRegistry registry);
}

// External source adapters resolve ContentRef.ExternalUri and/or Query
public interface ISourceAdapter
{
    string Scheme { get; } // e.g., http, https, file, ipfs, data, prompt
    Task<ContentRef?> ResolveAsync(ContentRef reference, IRegistry registry);
}

public interface IAdapterRegistry
{
    void Register(ISourceAdapter adapter);
    bool TryGet(string scheme, out ISourceAdapter adapter);
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
