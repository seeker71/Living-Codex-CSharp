using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CodexBootstrap.Core;

/// <summary>
/// Content addressing system for deterministic content hashing and deduplication
/// Implements Coil L7 - Persistence & Content Addressing
/// </summary>
public static class ContentAddressing
{
    /// <summary>
    /// Generate a deterministic content hash for any content
    /// </summary>
    /// <param name="content">The content to hash</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>Deterministic content hash</returns>
    public static string GenerateContentHash(string content, string algorithm = "sha256")
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        using var hasher = CreateHasher(algorithm);
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = hasher.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generate a deterministic content hash for binary content
    /// </summary>
    /// <param name="content">The binary content to hash</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>Deterministic content hash</returns>
    public static string GenerateContentHash(byte[] content, string algorithm = "sha256")
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        using var hasher = CreateHasher(algorithm);
        var hashBytes = hasher.ComputeHash(content);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generate a deterministic content hash for a Node's content
    /// </summary>
    /// <param name="node">The node to generate hash for</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>Deterministic content hash</returns>
    public static string GenerateNodeContentHash(Node node, string algorithm = "sha256")
    {
        if (node.Content == null)
            return string.Empty;

        // Create a canonical representation of the node's content
        var contentData = new
        {
            mediaType = node.Content.MediaType,
            inlineJson = node.Content.InlineJson,
            inlineBytes = node.Content.InlineBytes != null ? Convert.ToBase64String(node.Content.InlineBytes) : null,
            externalUri = node.Content.ExternalUri?.ToString(),
            selector = node.Content.Selector,
            query = node.Content.Query,
            headers = node.Content.Headers,
            authRef = node.Content.AuthRef
        };

        var json = JsonSerializer.Serialize(contentData, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return GenerateContentHash(json, algorithm);
    }

    /// <summary>
    /// Generate a deterministic content hash for a Node's structure (excluding content)
    /// </summary>
    /// <param name="node">The node to generate structure hash for</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>Deterministic structure hash</returns>
    public static string GenerateNodeStructureHash(Node node, string algorithm = "sha256")
    {
        var structureData = new
        {
            id = node.Id,
            typeId = node.TypeId,
            state = node.State.ToString(),
            locale = node.Locale,
            title = node.Title,
            description = node.Description,
            meta = node.Meta
        };

        var json = JsonSerializer.Serialize(structureData, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return GenerateContentHash(json, algorithm);
    }

    /// <summary>
    /// Generate a deterministic content hash for an Edge
    /// </summary>
    /// <param name="edge">The edge to generate hash for</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>Deterministic edge hash</returns>
    public static string GenerateEdgeHash(Edge edge, string algorithm = "sha256")
    {
        var edgeData = new
        {
            fromId = edge.FromId,
            toId = edge.ToId,
            role = edge.Role,
            weight = edge.Weight,
            meta = edge.Meta
        };

        var json = JsonSerializer.Serialize(edgeData, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return GenerateContentHash(json, algorithm);
    }

    /// <summary>
    /// Create a content-addressed ContentRef with automatic cache key generation
    /// </summary>
    /// <param name="mediaType">Media type of the content</param>
    /// <param name="inlineJson">Inline JSON content</param>
    /// <param name="inlineBytes">Inline binary content</param>
    /// <param name="externalUri">External URI reference</param>
    /// <param name="selector">Content selector</param>
    /// <param name="query">Content query</param>
    /// <param name="headers">HTTP headers</param>
    /// <param name="authRef">Authentication reference</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>ContentRef with generated cache key</returns>
    public static ContentRef CreateContentAddressedRef(
        string? mediaType = null,
        string? inlineJson = null,
        byte[]? inlineBytes = null,
        Uri? externalUri = null,
        string? selector = null,
        string? query = null,
        Dictionary<string, string>? headers = null,
        string? authRef = null,
        string algorithm = "sha256")
    {
        // Generate cache key based on content
        string? cacheKey = null;
        
        if (!string.IsNullOrEmpty(inlineJson))
        {
            cacheKey = GenerateContentHash(inlineJson, algorithm);
        }
        else if (inlineBytes != null && inlineBytes.Length > 0)
        {
            cacheKey = GenerateContentHash(inlineBytes, algorithm);
        }
        else if (externalUri != null)
        {
            // For external URIs, hash the URI and any additional context
            var uriData = new
            {
                uri = externalUri.ToString(),
                selector,
                query,
                headers
            };
            var json = JsonSerializer.Serialize(uriData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            cacheKey = GenerateContentHash(json, algorithm);
        }

        return new ContentRef(
            MediaType: mediaType,
            InlineJson: inlineJson,
            InlineBytes: inlineBytes,
            ExternalUri: externalUri,
            Selector: selector,
            Query: query,
            Headers: headers,
            AuthRef: authRef,
            CacheKey: cacheKey
        );
    }

    /// <summary>
    /// Verify content integrity using cache key
    /// </summary>
    /// <param name="contentRef">ContentRef to verify</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <returns>True if content integrity is valid</returns>
    public static bool VerifyContentIntegrity(ContentRef contentRef, string algorithm = "sha256")
    {
        if (string.IsNullOrEmpty(contentRef.CacheKey))
            return false;

        string? currentHash = null;

        if (!string.IsNullOrEmpty(contentRef.InlineJson))
        {
            currentHash = GenerateContentHash(contentRef.InlineJson, algorithm);
        }
        else if (contentRef.InlineBytes != null && contentRef.InlineBytes.Length > 0)
        {
            currentHash = GenerateContentHash(contentRef.InlineBytes, algorithm);
        }
        else if (contentRef.ExternalUri != null)
        {
            var uriData = new
            {
                uri = contentRef.ExternalUri.ToString(),
                selector = contentRef.Selector,
                query = contentRef.Query,
                headers = contentRef.Headers
            };
            var json = JsonSerializer.Serialize(uriData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            currentHash = GenerateContentHash(json, algorithm);
        }

        return currentHash == contentRef.CacheKey;
    }

    /// <summary>
    /// Get content lineage information from cache key
    /// </summary>
    /// <param name="cacheKey">Cache key to analyze</param>
    /// <returns>Lineage information</returns>
    public static ContentLineage GetContentLineage(string cacheKey)
    {
        return new ContentLineage
        {
            CacheKey = cacheKey,
            Algorithm = "sha256", // Default assumption
            CreatedAt = DateTime.UtcNow, // Would be stored in metadata in real implementation
            Size = cacheKey.Length * 4, // Rough estimate
            IsValid = !string.IsNullOrEmpty(cacheKey)
        };
    }

    private static HashAlgorithm CreateHasher(string algorithm)
    {
        return algorithm.ToLowerInvariant() switch
        {
            "sha256" => SHA256.Create(),
            "sha1" => SHA1.Create(),
            "md5" => MD5.Create(),
            _ => SHA256.Create()
        };
    }
}

/// <summary>
/// Content lineage information for tracking content history
/// </summary>
public record ContentLineage
{
    public string CacheKey { get; init; } = string.Empty;
    public string Algorithm { get; init; } = "sha256";
    public DateTime CreatedAt { get; init; }
    public long Size { get; init; }
    public bool IsValid { get; init; }
    public List<string> Dependencies { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}