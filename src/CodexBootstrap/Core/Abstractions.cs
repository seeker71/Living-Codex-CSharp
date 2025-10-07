using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Runtime;

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
)
{
    public Node DeepClone()
    {
        return new Node(
            Id: Id,
            TypeId: TypeId,
            State: State,
            Locale: Locale,
            Title: Title,
            Description: Description,
            Content: Content,
            Meta: Meta != null ? new Dictionary<string, object>(Meta) : null
        );
    }
}

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
    string? RoleId,
    double? Weight,
    Dictionary<string, object>? Meta
)
{
    public Edge(string FromId, string ToId, string Role, double? Weight, Dictionary<string, object>? Meta)
        : this(FromId, ToId, Role, null, Weight, Meta)
    {
    }

    public Edge DeepClone()
    {
        return new Edge(
            FromId: FromId,
            ToId: ToId,
            Role: Role,
            RoleId: RoleId,
            Weight: Weight,
            Meta: Meta != null ? new Dictionary<string, object>(Meta) : null
        );
    }
}

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
    void Register(INodeRegistry registry);
    void RegisterApiHandlers(IApiRouter router, INodeRegistry registry);
    void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader);
    
    /// <summary>
    /// Initialize the module asynchronously after construction
    /// This allows modules to perform heavy initialization work without blocking the module loading process
    /// Default implementation does nothing
    /// </summary>
    Task InitializeAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Setup inter-module communication after all modules are created
    /// This allows modules to reference each other and set up dependencies
    /// Default implementation does nothing
    /// </summary>
    void SetupInterModuleCommunication(IServiceProvider services) { }
    
    /// <summary>
    /// Unregister the module and clean up resources
    /// Default implementation does nothing
    /// </summary>
    void Unregister() { }

    // Readiness tracking properties and events
    /// <summary>
    /// Current readiness state of the module
    /// </summary>
    ReadinessState CurrentReadinessState { get; }
    
    /// <summary>
    /// Event fired when the module's readiness state changes
    /// </summary>
    event EventHandler<ReadinessChangedEventArgs>? ReadinessChanged;
    
    /// <summary>
    /// Get detailed readiness information for the module
    /// </summary>
    ReadinessResult GetReadinessResult();
    
    /// <summary>
    /// Get list of endpoints provided by this module
    /// </summary>
    IEnumerable<string> GetProvidedEndpoints();
}

public interface IApiRouter
{
    void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler);
    bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler);
    INodeRegistry GetRegistry();
}

public interface ISynthesizer
{
    Task<Node> SynthesizeAsync(Node node, INodeRegistry registry);
}

// External source adapters resolve ContentRef.ExternalUri and/or Query
public interface ISourceAdapter
{
    string Scheme { get; } // e.g., http, https, file, ipfs, data, prompt
    Task<ContentRef?> ResolveAsync(ContentRef reference, INodeRegistry registry);
}

public interface IAdapterRegistry
{
    void Register(ISourceAdapter adapter);
    bool TryGet(string scheme, out ISourceAdapter adapter);
}


public sealed record DynamicCall(string ModuleId, [property: JsonPropertyName("apiName")] string Api, JsonElement? Args);

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
