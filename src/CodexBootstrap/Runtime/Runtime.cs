using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Runtime;

public sealed class ModuleRegistry
{
    private readonly Dictionary<string, IModule> _mods = new(StringComparer.OrdinalIgnoreCase);
    public void Register(IModule m) => _mods[m.Spec.Id] = m;
    public bool TryGet(string id, out IModule m) => _mods.TryGetValue(id, out m!);
    public IEnumerable<IModule> All() => _mods.Values;
}

public sealed class NodeRegistry : IRegistry
{
    private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Edge> _edges = new();

    public void Upsert(Node node) => _nodes[node.Id] = node;
    public void Upsert(Edge edge) => _edges.Add(edge);
    public bool TryGet(string id, out Node node) => _nodes.TryGetValue(id, out node!);
    public IEnumerable<Edge> AllEdges() => _edges;
    public IEnumerable<Node> AllNodes() => _nodes.Values;
}

public sealed class ApiRouter : IApiRouter
{
    private readonly Dictionary<(string module, string api), Func<JsonElement?, Task<object?>>> _handlers = new();
    public void Register(string moduleId, string api, Func<JsonElement?, Task<object?>> handler) =>
        _handlers[(moduleId, api)] = handler;
    public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object?>> handler) =>
        _handlers.TryGetValue((moduleId, api), out handler!);
}

// --------- Adapters & hydration ----------
public sealed class AdapterRegistry : IAdapterRegistry
{
    private readonly Dictionary<string, ISourceAdapter> _byScheme = new(StringComparer.OrdinalIgnoreCase);
    public void Register(ISourceAdapter adapter) => _byScheme[adapter.Scheme] = adapter;
    public bool TryGet(string scheme, out ISourceAdapter adapter) => _byScheme.TryGetValue(scheme, out adapter!);
}

public sealed class EchoSynthesizer : ISynthesizer
{
    private readonly IAdapterRegistry? _adapters;
    public EchoSynthesizer(IAdapterRegistry? adapters = null) { _adapters = adapters; }

    public async Task<Node> SynthesizeAsync(Node node, IRegistry registry)
    {
        // If node has ExternalUri → use adapter; else mirror Description.
        var cref = node.Content;
        if (cref?.ExternalUri is Uri uri && _adapters != null && _adapters.TryGet(uri.Scheme, out var adapter))
        {
            var resolved = await adapter.ResolveAsync(cref, registry) ?? cref;
            var hydrated = node with { State = ContentState.Water, Content = resolved with { CacheKey = Hash(resolved) } };
            return hydrated;
        }

        if (!string.IsNullOrWhiteSpace(node.Description))
        {
            var bytes = Encoding.UTF8.GetBytes(node.Description!);
            var content = new ContentRef("text/markdown", null, bytes, null, CacheKey: Hash(bytes));
            return node with { State = ContentState.Water, Content = content };
        }

        // Nothing to do → remain Gas (transient) or Ice (spec only)
        return node with { State = node.State == ContentState.Ice ? ContentState.Ice : ContentState.Gas };
    }

    private static string Hash(ContentRef c)
    {
        if (c.InlineBytes != null) return Hash(c.InlineBytes);
        if (c.InlineJson != null) return Hash(Encoding.UTF8.GetBytes(c.InlineJson));
        if (c.ExternalUri != null) return Hash(Encoding.UTF8.GetBytes(c.ExternalUri.ToString()));
        return "";
    }
    private static string Hash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}

// Built‑ins for file:// and http(s):// to keep core useful but minimal
public sealed class FileAdapter : ISourceAdapter
{
    public string Scheme => "file";
    public async Task<ContentRef?> ResolveAsync(ContentRef reference, IRegistry registry)
    {
        if (reference.ExternalUri == null) return null;
        var path = reference.ExternalUri.LocalPath;
        var bytes = await File.ReadAllBytesAsync(path);
        return reference with { InlineBytes = bytes, MediaType = reference.MediaType ?? "application/octet-stream" };
    }
}

public class HttpAdapter : ISourceAdapter
{
    private readonly HttpClient _http = new();
    public virtual string Scheme => "http"; // https handled by registering twice
    public async Task<ContentRef?> ResolveAsync(ContentRef reference, IRegistry registry)
    {
        if (reference.ExternalUri == null) return null;
        using var req = new HttpRequestMessage(HttpMethod.Get, reference.ExternalUri);
        if (reference.Headers != null)
            foreach (var kv in reference.Headers) req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var bytes = await res.Content.ReadAsByteArrayAsync();
        var media = reference.MediaType ?? res.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return reference with { InlineBytes = bytes, MediaType = media };
    }
}

// Additional service implementations
public sealed class SpecRegistry
{
    private readonly Dictionary<string, ModuleAtoms> _atoms = new();
    private readonly Dictionary<string, ModuleSpec> _specs = new();

    public void UpsertAtoms(ModuleAtoms atoms) => _atoms[atoms.Id] = atoms;
    public void UpsertSpec(ModuleSpec spec) => _specs[spec.Id] = spec;
    public bool TryGetAtoms(string id, out ModuleAtoms atoms) => _atoms.TryGetValue(id, out atoms!);
    public bool TryGetSpec(string id, out ModuleSpec spec) => _specs.TryGetValue(id, out spec!);
    public object Topology(string id) => new { id, nodes = _atoms.GetValueOrDefault(id)?.Nodes.Count ?? 0 };
}

public sealed class SpecComposer
{
    public ModuleSpec Compose(ModuleAtoms atoms)
    {
        // Simple composition - in a real implementation this would be more sophisticated
        return new ModuleSpec(
            Id: atoms.Id,
            Version: "0.1.0",
            Name: $"Module {atoms.Id}",
            Description: $"Composed from {atoms.Nodes.Count} nodes",
            Dependencies: Array.Empty<ModuleRef>(),
            Types: Array.Empty<TypeSpec>(),
            Apis: Array.Empty<ApiSpec>()
        );
    }
}

public sealed class BreathEngine
{
    public object Expand(string id) => new { id, phase = "expanded" };
    public object Validate(string id) => new { id, valid = true };
    public object Contract(string id) => new { id, phase = "contracted" };
}

public sealed class BasicSpecGenerator : ISpecGenerator
{
    public ModuleSpec Generate(ModuleAtoms atoms)
    {
        return new ModuleSpec(
            Id: atoms.Id,
            Version: "0.1.0",
            Name: $"Generated {atoms.Id}",
            Description: "Auto-generated spec",
            Dependencies: Array.Empty<ModuleRef>(),
            Types: Array.Empty<TypeSpec>(),
            Apis: Array.Empty<ApiSpec>()
        );
    }
}

public sealed class NoopValidator : IValidator
{
    public bool Validate(ModuleSpec spec) => true;
}

public sealed class SpecReflector : ISpecReflector
{
    public IEnumerable<Node> ToNodes(ModuleSpec spec)
    {
        yield return new Node(
            Id: spec.Id,
            TypeId: "spec",
            State: ContentState.Ice,
            Locale: null,
            Title: spec.Name,
            Description: spec.Description,
            Content: null,
            Meta: new() { ["version"] = spec.Version }
        );
    }

    public ModuleSpec FromNodes(IEnumerable<Node> nodes)
    {
        var specNode = nodes.FirstOrDefault(n => n.TypeId == "spec");
        return new ModuleSpec(
            Id: specNode?.Id ?? "unknown",
            Version: "0.1.0",
            Name: specNode?.Title ?? "Unknown",
            Description: specNode?.Description,
            Dependencies: Array.Empty<ModuleRef>(),
            Types: Array.Empty<TypeSpec>(),
            Apis: Array.Empty<ApiSpec>()
        );
    }
}

public static class PatchEngine
{
    public static PatchDoc Diff(Node from, Node to)
    {
        var operations = new List<PatchOperation>();
        if (from.Title != to.Title)
            operations.Add(new PatchOperation("replace", "/title", to.Title));
        if (from.Description != to.Description)
            operations.Add(new PatchOperation("replace", "/description", to.Description));
        return new PatchDoc(operations);
    }

    public static Node Apply(Node node, PatchDoc patch)
    {
        var updated = node;
        foreach (var op in patch.Operations)
        {
            if (op.Op == "replace" && op.Path == "/title")
                updated = updated with { Title = op.Value?.ToString() };
            else if (op.Op == "replace" && op.Path == "/description")
                updated = updated with { Description = op.Value?.ToString() };
        }
        return updated;
    }
}

public static class OpenApiHelper
{
    public static object FromModuleSpec(ModuleSpec spec)
    {
        return new
        {
            openapi = "3.0.0",
            info = new { title = spec.Name, version = spec.Version },
            paths = new { }
        };
    }
}

public static class CoreAtomsSeed
{
    public static ModuleAtoms Atoms()
    {
        var nodes = new List<Node>
        {
            new Node(
                Id: "core:node",
                TypeId: "core:type",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node Type",
                Description: "Core node type definition",
                Content: null,
                Meta: new() { ["core"] = true }
            )
        };

        var edges = new List<Edge>
        {
            new Edge("core:node", "core:edge", "defines", 1.0, new() { ["core"] = true })
        };

        return new ModuleAtoms("core", nodes, edges);
    }
}
