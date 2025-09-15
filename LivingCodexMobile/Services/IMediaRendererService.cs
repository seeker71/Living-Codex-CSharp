using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface IMediaRendererService
{
    Task<MediaRenderResult> RenderContentAsync(ContentRef content, string? mediaType = null);
    bool CanRender(string mediaType);
    List<string> GetSupportedMediaTypes();
}

public class MediaRendererService : IMediaRendererService
{
    private readonly Dictionary<string, IContentRenderer> _renderers;

    public MediaRendererService()
    {
        _renderers = new Dictionary<string, IContentRenderer>
        {
            ["text/plain"] = new PlainTextRenderer(),
            ["text/markdown"] = new MarkdownRenderer(),
            ["text/html"] = new HtmlRenderer(),
            ["application/json"] = new JsonRenderer(),
            ["application/xml"] = new XmlRenderer(),
            ["image/jpeg"] = new ImageRenderer(),
            ["image/png"] = new ImageRenderer(),
            ["image/gif"] = new ImageRenderer(),
            ["image/webp"] = new ImageRenderer(),
            ["video/mp4"] = new VideoRenderer(),
            ["video/webm"] = new VideoRenderer(),
            ["audio/mp3"] = new AudioRenderer(),
            ["audio/wav"] = new AudioRenderer(),
            ["application/pdf"] = new PdfRenderer(),
            ["application/zip"] = new ArchiveRenderer(),
            ["text/code"] = new CodeRenderer(),
            ["application/x-yaml"] = new YamlRenderer(),
            ["text/csv"] = new CsvRenderer()
        };
    }

    public async Task<MediaRenderResult> RenderContentAsync(ContentRef content, string? mediaType = null)
    {
        var actualMediaType = mediaType ?? content.MediaType ?? "text/plain";
        
        if (_renderers.TryGetValue(actualMediaType, out var renderer))
        {
            return await renderer.RenderAsync(content);
        }

        // Fallback to plain text
        return await _renderers["text/plain"].RenderAsync(content);
    }

    public bool CanRender(string mediaType)
    {
        return _renderers.ContainsKey(mediaType);
    }

    public List<string> GetSupportedMediaTypes()
    {
        return _renderers.Keys.ToList();
    }
}

public interface IContentRenderer
{
    Task<MediaRenderResult> RenderAsync(ContentRef content);
    bool CanHandle(string mediaType);
}

public class MediaRenderResult
{
    public string RenderedContent { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

// Specific renderers
public class PlainTextRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "text/plain",
            IsSuccess = true
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "text/plain";
}

public class MarkdownRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        // For now, return the markdown as-is
        // In a real implementation, you'd use a markdown parser like Markdig
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "text/markdown",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["needsParsing"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "text/markdown";
}

public class HtmlRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "text/html",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["isHtml"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "text/html";
}

public class JsonRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        try
        {
            var json = content.InlineJson ?? content.ExternalUrl ?? "{}";
            var formatted = System.Text.Json.JsonSerializer.Serialize(
                System.Text.Json.JsonSerializer.Deserialize<object>(json), 
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
            
            return new MediaRenderResult
            {
                RenderedContent = formatted,
                MediaType = "application/json",
                IsSuccess = true,
                Metadata = new Dictionary<string, object> { ["isFormatted"] = true }
            };
        }
        catch (Exception ex)
        {
            return new MediaRenderResult
            {
                RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
                MediaType = "application/json",
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public bool CanHandle(string mediaType) => mediaType == "application/json";
}

public class XmlRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "application/xml",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["needsFormatting"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "application/xml";
}

public class ImageRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.ExternalUrl ?? "",
            MediaType = content.MediaType ?? "image/jpeg",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> 
            { 
                ["isImage"] = true,
                ["url"] = content.ExternalUrl ?? "",
                ["size"] = content.Size ?? 0
            }
        };
    }

    public bool CanHandle(string mediaType) => mediaType.StartsWith("image/");
}

public class VideoRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.ExternalUrl ?? "",
            MediaType = content.MediaType ?? "video/mp4",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> 
            { 
                ["isVideo"] = true,
                ["url"] = content.ExternalUrl ?? "",
                ["size"] = content.Size ?? 0
            }
        };
    }

    public bool CanHandle(string mediaType) => mediaType.StartsWith("video/");
}

public class AudioRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.ExternalUrl ?? "",
            MediaType = content.MediaType ?? "audio/mp3",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> 
            { 
                ["isAudio"] = true,
                ["url"] = content.ExternalUrl ?? "",
                ["size"] = content.Size ?? 0
            }
        };
    }

    public bool CanHandle(string mediaType) => mediaType.StartsWith("audio/");
}

public class PdfRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.ExternalUrl ?? "",
            MediaType = "application/pdf",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> 
            { 
                ["isPdf"] = true,
                ["url"] = content.ExternalUrl ?? "",
                ["size"] = content.Size ?? 0
            }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "application/pdf";
}

public class ArchiveRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.ExternalUrl ?? "",
            MediaType = "application/zip",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> 
            { 
                ["isArchive"] = true,
                ["url"] = content.ExternalUrl ?? "",
                ["size"] = content.Size ?? 0
            }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "application/zip";
}

public class CodeRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "text/code",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["needsSyntaxHighlighting"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "text/code";
}

public class YamlRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "application/x-yaml",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["needsFormatting"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "application/x-yaml";
}

public class CsvRenderer : IContentRenderer
{
    public async Task<MediaRenderResult> RenderAsync(ContentRef content)
    {
        return new MediaRenderResult
        {
            RenderedContent = content.InlineJson ?? content.ExternalUrl ?? "",
            MediaType = "text/csv",
            IsSuccess = true,
            Metadata = new Dictionary<string, object> { ["needsTableFormatting"] = true }
        };
    }

    public bool CanHandle(string mediaType) => mediaType == "text/csv";
}
