using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Adapter module specific response types
public record AdapterRegistrationResponse(string AdapterId, bool Success, string Message = "Adapter registered successfully");
public record HydrateResponse(string NodeId, bool Success, string Message = "Node hydrated successfully", object? Content = null);

// Adapter data structures
public sealed record AdapterInfo(
    string Id,
    string Scheme,
    string Name,
    string Description,
    IReadOnlyList<string> SupportedMediaTypes,
    Dictionary<string, object>? Configuration
);

public sealed record AdapterRegistration(
    string Scheme,
    string Name,
    string Description,
    IReadOnlyList<string> SupportedMediaTypes,
    Dictionary<string, object>? Configuration
);

public interface IContentAdapter
{
    string Scheme { get; }
    Task<object?> HydrateAsync(ContentRef contentRef, CancellationToken cancellationToken = default);
    bool CanHandle(ContentRef contentRef);
}

public sealed class FileAdapter : IContentAdapter
{
    public string Scheme => "file";

    public bool CanHandle(ContentRef contentRef)
    {
        return contentRef.ExternalUri?.Scheme == "file";
    }

    public async Task<object?> HydrateAsync(ContentRef contentRef, CancellationToken cancellationToken = default)
    {
        if (!CanHandle(contentRef))
            return null;

        try
        {
            var filePath = contentRef.ExternalUri?.LocalPath;
            if (string.IsNullOrEmpty(filePath))
                return null;

            if (!File.Exists(filePath))
                return new { error = "File not found", path = filePath };

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var mediaType = contentRef.MediaType ?? "text/plain";

            return new
            {
                content,
                mediaType,
                path = filePath,
                size = content.Length,
                lastModified = File.GetLastWriteTime(filePath)
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message, path = contentRef.ExternalUri?.LocalPath };
        }
    }
}

public sealed class HttpAdapter : IContentAdapter
{
    private readonly HttpClient _httpClient;

    public HttpAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Scheme => "http";

    public bool CanHandle(ContentRef contentRef)
    {
        return contentRef.ExternalUri?.Scheme == "http" || contentRef.ExternalUri?.Scheme == "https";
    }

    public async Task<object?> HydrateAsync(ContentRef contentRef, CancellationToken cancellationToken = default)
    {
        if (!CanHandle(contentRef))
            return null;

        try
        {
            var uri = contentRef.ExternalUri;
            if (uri == null)
                return null;

            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // Add headers if specified
            if (contentRef.Headers != null)
            {
                foreach (var header in contentRef.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add query parameters if specified
            if (!string.IsNullOrEmpty(contentRef.Query))
            {
                var queryString = contentRef.Query.StartsWith("?") ? contentRef.Query : "?" + contentRef.Query;
                var uriBuilder = new UriBuilder(uri) { Query = queryString };
                request.RequestUri = uriBuilder.Uri;
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? contentRef.MediaType ?? "text/plain";

            return new
            {
                content,
                mediaType,
                statusCode = (int)response.StatusCode,
                headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                url = uri.ToString()
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message, url = contentRef.ExternalUri?.ToString() };
        }
    }
}

public sealed class AdapterModule : IModule
{
    private readonly Dictionary<string, IContentAdapter> _adapters = new();
    private readonly HttpClient _httpClient;

    public AdapterModule(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Register built-in adapters
        RegisterBuiltInAdapters();
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.adapters",
            name: "Adapter Module",
            version: "0.1.0",
            description: "Module for managing content adapters and external resource linking."
        );
    }


    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());

        // Register AdapterInfo type definition as node
        var adapterInfoType = new Node(
            Id: "codex.adapters/adapterinfo",
            TypeId: "codex.meta/type",
            State: ContentState.Ice,
            Locale: "en",
            Title: "AdapterInfo Type",
            Description: "Represents information about a registered content adapter",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "AdapterInfo",
                    fields = new[]
                    {
                        new { name = "id", type = "string", required = true, description = "Adapter identifier" },
                        new { name = "scheme", type = "string", required = true, description = "URI scheme handled" },
                        new { name = "name", type = "string", required = true, description = "Adapter name" },
                        new { name = "description", type = "string", required = true, description = "Adapter description" },
                        new { name = "supportedMediaTypes", type = "array", required = true, description = "Supported media types" },
                        new { name = "configuration", type = "object", required = false, description = "Adapter configuration" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.adapters",
                ["typeName"] = "AdapterInfo"
            }
        );
        registry.Upsert(adapterInfoType);

        // Register API nodes
        var registerApiNode = new Node(
            Id: "codex.adapters/register-api",
            TypeId: "codex.meta/api",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Register Adapter API",
            Description: "Register a new content adapter",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    name = "register",
                    verb = "POST",
                    route = "/adapters/register",
                    parameters = new[]
                    {
                        new { name = "adapter", type = "AdapterRegistration", required = true, description = "Adapter registration information" }
                    }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.adapters",
                ["apiName"] = "register"
            }
        );
        registry.Upsert(registerApiNode);

        // Hydrate API is now handled by HydrateModule

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.adapters", "register"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        router.Register("codex.adapters", "register", async args =>
        {
            try
            {
                if (args == null || !args.HasValue)
                {
                    return Task.FromResult<object>(new ErrorResponse("Missing request body"));
                }

                var adapterJson = args.Value.TryGetProperty("adapter", out var adapterElement) ? adapterElement.GetRawText() : null;

                if (string.IsNullOrEmpty(adapterJson))
                {
                    return new ErrorResponse("Adapter information is required");
                }

                var registration = JsonSerializer.Deserialize<AdapterRegistration>(adapterJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                if (registration == null)
                {
                    return new ErrorResponse("Invalid adapter registration");
                }

                var adapterId = $"adapter-{Guid.NewGuid()}";
                var adapterInfo = new AdapterInfo(
                    Id: adapterId,
                    Scheme: registration.Scheme,
                    Name: registration.Name,
                    Description: registration.Description,
                    SupportedMediaTypes: registration.SupportedMediaTypes,
                    Configuration: registration.Configuration
                );

                // Store adapter info in registry
                await Task.Run(() => {
                var adapterNode = new Node(
                    Id: adapterId,
                    TypeId: "codex.adapters/adapter",
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: registration.Name,
                    Description: registration.Description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(adapterInfo, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["moduleId"] = "codex.adapters",
                        ["scheme"] = registration.Scheme,
                        ["name"] = registration.Name
                    }
                );
                registry.Upsert(adapterNode);
                });

                return new AdapterRegistrationResponse(AdapterId: adapterId, Success: true);
            }
            catch (Exception ex)
            {
                return new ErrorResponse($"Failed to register adapter: {ex.Message}");
            }
        });

        // Hydrate API is now handled by HydrateModule
    }

    private void RegisterBuiltInAdapters()
    {
        // Register file adapter
        var fileAdapter = new FileAdapter();
        _adapters["file"] = fileAdapter;

        // Register HTTP adapter
        var httpAdapter = new HttpAdapter(_httpClient);
        _adapters["http"] = httpAdapter;
        _adapters["https"] = httpAdapter;
    }

    public IContentAdapter? GetAdapter(string scheme)
    {
        return _adapters.TryGetValue(scheme, out var adapter) ? adapter : null;
    }

    public IEnumerable<IContentAdapter> GetAllAdapters()
    {
        return _adapters.Values;
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Adapter module doesn't need any custom HTTP endpoints
        // All functionality is exposed through the generic /route endpoint
    }
}
