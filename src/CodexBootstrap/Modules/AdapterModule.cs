using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Adapter module specific response types
[ResponseType("codex.adapter.registration-response", "AdapterRegistrationResponse", "Response for adapter registration")]
public record AdapterRegistrationResponse(string AdapterId, bool Success, string Message = "Adapter registered successfully");

[ResponseType("codex.adapter.hydrate-response", "HydrateResponse", "Response for adapter hydration")]
public record HydrateResponse(string NodeId, bool Success, string Message = "Node hydrated successfully", object? Content = null);

// Adapter data structures
[ResponseType("codex.adapter.info", "AdapterInfo", "Adapter information structure")]
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

public sealed record AdapterRegistrationRequest(
    string Scheme,
    string Name,
    string? Description = null,
    IReadOnlyList<string>? SupportedMediaTypes = null,
    Dictionary<string, object>? Configuration = null
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

            var mediaType = contentRef.MediaType ?? "text/plain";
            var fileInfo = new FileInfo(filePath);
            var lastModified = fileInfo.LastWriteTime;

            // Determine if this is a binary file based on media type or file extension
            var isBinary = AdapterModule.IsBinaryFile(mediaType, filePath);
            
            if (isBinary)
            {
                // For binary files, read as bytes and return as base64
                var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                var base64Content = Convert.ToBase64String(bytes);
                
                return new
                {
                    content = base64Content,
                    mediaType,
                    path = filePath,
                    size = bytes.Length,
                    lastModified,
                    encoding = "base64"
                };
            }
            else
            {
                // For text files, read as text
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                
                return new
                {
                    content,
                    mediaType,
                    path = filePath,
                    size = content.Length,
                    lastModified
                };
            }
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

public sealed class AdapterModule : ModuleBase
{
    private readonly Dictionary<string, IContentAdapter> _adapters = new();
    private readonly HttpClient _httpClient;

    public override string Name => "Adapter Module";
    public override string Description => "Module for managing content adapters and external resource linking.";
    public override string Version => "0.1.0";

    public AdapterModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _httpClient = httpClient;
        // Register built-in adapters
        RegisterBuiltInAdapters();
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.adapters",
            name: "Adapter Module",
            version: "0.1.0",
            description: "Module for managing content adapters and external resource linking.",
            tags: new[] { "adapter", "content", "external", "link" },
            capabilities: new[] { "adapters", "content", "external-resources", "linking" },
            spec: "codex.spec.adapters"
        );
    }



    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
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

        router.Register("codex.adapters", "list", args =>
        {
            try
            {
                var adapters = _adapters.Values.Select(adapter => new AdapterInfo(
                    Id: adapter.Scheme,
                    Scheme: adapter.Scheme,
                    Name: adapter.GetType().Name,
                    Description: $"Built-in {adapter.Scheme} adapter",
                    SupportedMediaTypes: new[] { "text/plain", "application/json" },
                    Configuration: null
                )).ToList();

                return Task.FromResult<object>(new
                {
                    adapters,
                    count = adapters.Count
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult<object>(new ErrorResponse($"Failed to list adapters: {ex.Message}"));
            }
        });
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

    public static bool IsBinaryFile(string mediaType, string filePath)
    {
        // Check media type first
        if (!string.IsNullOrEmpty(mediaType))
        {
            var binaryTypes = new[]
            {
                "image/", "video/", "audio/", "application/octet-stream",
                "application/pdf", "application/zip", "application/x-",
                "font/", "model/"
            };
            
            if (binaryTypes.Any(type => mediaType.StartsWith(type, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // Check file extension as fallback
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var binaryExtensions = new[]
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".webp", ".svg",
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm",
            ".mp3", ".wav", ".flac", ".aac", ".ogg",
            ".pdf", ".zip", ".rar", ".7z", ".tar", ".gz",
            ".exe", ".dll", ".so", ".dylib",
            ".woff", ".woff2", ".ttf", ".otf", ".eot"
        };

        return binaryExtensions.Contains(extension);
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Adapter module exposes content hydration endpoint for external URIs
    }

    [ApiRoute("POST", "/adapters/register", "adapter-register", "Register a new content adapter", "codex.adapters")]
    public async Task<object> RegisterAdapter([ApiParameter("request", "Adapter registration request", Required = true, Location = "body")] AdapterRegistrationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Scheme))
            {
                return new ErrorResponse("Scheme is required");
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                return new ErrorResponse("Name is required");
            }

            var adapterId = $"adapter-{Guid.NewGuid()}";
            var adapterInfo = new AdapterInfo(
                Id: adapterId,
                Scheme: request.Scheme,
                Name: request.Name,
                Description: request.Description ?? $"Adapter for {request.Scheme} scheme",
                SupportedMediaTypes: request.SupportedMediaTypes ?? new[] { "text/plain", "application/json" },
                Configuration: request.Configuration
            );

            // Store adapter info in registry
            var adapterNode = new Node(
                Id: adapterId,
                TypeId: "codex.adapters/adapter",
                State: ContentState.Ice,
                Locale: "en",
                Title: request.Name,
                Description: request.Description ?? $"Adapter for {request.Scheme} scheme",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(adapterInfo, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["moduleId"] = "codex.adapters",
                    ["scheme"] = request.Scheme,
                    ["name"] = request.Name
                }
            );

            // Note: We can't access registry here directly, so we'll return the adapter info
            // The actual registration would need to be handled by the calling code
            return new AdapterRegistrationResponse(AdapterId: adapterId, Success: true);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to register adapter: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/adapters/list", "adapter-list", "List all registered adapters", "codex.adapters")]
    public object ListAdapters()
    {
        try
        {
            var adapters = _adapters.Values.Select(adapter => new AdapterInfo(
                Id: adapter.Scheme,
                Scheme: adapter.Scheme,
                Name: adapter.GetType().Name,
                Description: $"Built-in {adapter.Scheme} adapter",
                SupportedMediaTypes: new[] { "text/plain", "application/json" },
                Configuration: null
            )).ToList();

            return new
            {
                adapters,
                count = adapters.Count
            };
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to list adapters: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolve a node's ContentRef using registered adapters and return renderable content
    /// </summary>
    [ApiRoute("GET", "/adapters/content/{nodeId}", "adapter-content", "Resolve node content via adapters", "codex.adapters")]
    public async Task<object> GetAdapterContent(
        [ApiParameter("nodeId", "Node ID to resolve", Required = true, Location = "path")] string nodeId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return new ErrorResponse("nodeId is required");
            }

            if (!_registry.TryGet(nodeId, out var node))
            {
                return new ErrorResponse($"Node '{nodeId}' not found");
            }

            var contentRef = node.Content;
            if (contentRef == null)
            {
                return new ErrorResponse($"Node '{nodeId}' has no content");
            }

            // If inline content is already present, return it directly
            if (!string.IsNullOrEmpty(contentRef.InlineJson))
            {
                return new
                {
                    success = true,
                    nodeId = node.Id,
                    mediaType = contentRef.MediaType ?? "text/plain",
                    content = contentRef.InlineJson,
                    source = "inlineJson"
                };
            }

            if (contentRef.InlineBytes != null && contentRef.InlineBytes.Length > 0)
            {
                // Return base64 for UI rendering when bytes are present
                var base64 = Convert.ToBase64String(contentRef.InlineBytes);
                return new
                {
                    success = true,
                    nodeId = node.Id,
                    mediaType = contentRef.MediaType ?? "application/octet-stream",
                    content = base64,
                    encoding = "base64",
                    source = "inlineBytes"
                };
            }

            // External URI handling via adapters
            if (contentRef.ExternalUri != null)
            {
                var scheme = contentRef.ExternalUri.Scheme.ToLowerInvariant();

                // Use adapters for all external content resolution, including files

                var adapter = GetAdapter(scheme);
                if (adapter == null)
                {
                    return new ErrorResponse($"No adapter registered for scheme '{scheme}'");
                }

                var result = await adapter.HydrateAsync(contentRef);
                if (result is null)
                {
                    return new ErrorResponse("Adapter failed to hydrate content");
                }

                // Attempt to normalize known shapes
                var content = result.GetType().GetProperty("content")?.GetValue(result) ?? result;
                var mediaType = (result.GetType().GetProperty("mediaType")?.GetValue(result) as string)
                                ?? contentRef.MediaType
                                ?? "text/plain";

                return new
                {
                    success = true,
                    nodeId = node.Id,
                    mediaType,
                    content,
                    source = $"adapter:{scheme}"
                };
            }

            return new ErrorResponse("ContentRef has neither inline content nor externalUri");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to resolve adapter content: {ex.Message}");
        }
    }
}
