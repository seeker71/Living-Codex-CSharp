using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CodexBootstrap.Core;

namespace CodexBootstrap.Middleware;

public sealed class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICodexLogger _logger;
    private readonly ResponseCache _cache;
    private readonly Timer _cleanupTimer;

    public ResponseCachingMiddleware(RequestDelegate next, ICodexLogger logger)
    {
        _next = next;
        _logger = logger;
        _cache = new ResponseCache();

        _cleanupTimer = new Timer(
            CleanupExpiredEntries,
            null,
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10)
        );

        _logger.Info("[ResponseCaching] Middleware initialized");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        if (!ShouldCache(method, path))
        {
            await _next(context);
            return;
        }

        var cacheKey = GenerateCacheKey(context);
        var cachedResponse = _cache.Get(cacheKey);

        if (cachedResponse != null && !IsExpired(cachedResponse))
        {
            await ServeCachedResponse(context, cachedResponse);
            _logger.Debug($"[ResponseCaching] Served from cache: {path}");
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == 200 && ShouldCacheResponse(context))
            {
                responseBodyStream.Position = 0;
                using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
                var responseBody = await reader.ReadToEndAsync();

                var cacheEntry = new CacheEntry(
                    cacheKey,
                    responseBody,
                    context.Response.ContentType ?? "application/json",
                    context.Response.StatusCode,
                    GetRelevantHeaders(context.Response.Headers),
                    DateTime.UtcNow,
                    CalculateExpiration(context)
                );

                _cache.Set(cacheEntry);
            }

            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldCache(string method, string path)
    {
        if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            return false;

        var skipPatterns = new[]
        {
            "/health",
            "/metrics",
            "/auth/",
            "/swagger",
            "/favicon.ico"
        };

        return !skipPatterns.Any(pattern => path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ShouldCacheResponse(HttpContext context)
    {
        var nonCacheableStatusCodes = new[] { 400, 401, 403, 404, 500, 502, 503, 504 };
        if (nonCacheableStatusCodes.Contains(context.Response.StatusCode))
            return false;

        if (context.Response.Headers.ContainsKey("Cache-Control") &&
            context.Response.Headers["Cache-Control"].ToString().Contains("no-cache"))
            return false;

        return true;
    }

    private string GenerateCacheKey(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        var queryString = context.Request.QueryString.Value ?? "";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();

        var keyData = $"{path}{queryString}{userAgent}{acceptLanguage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyData));
        return Convert.ToHexString(hash)[..16];
    }

    private DateTime CalculateExpiration(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        return path switch
        {
            var p when p.StartsWith("/concepts/") => DateTime.UtcNow.AddMinutes(30),
            var p when p.StartsWith("/nodes/") => DateTime.UtcNow.AddMinutes(15),
            var p when p.StartsWith("/spec/") => DateTime.UtcNow.AddHours(1),
            var p when p.StartsWith("/api/") => DateTime.UtcNow.AddMinutes(5),
            _ => DateTime.UtcNow.AddMinutes(10)
        };
    }

    private Dictionary<string, string> GetRelevantHeaders(IHeaderDictionary headers)
    {
        var relevant = new Dictionary<string, string>();
        var toCache = new[] { "Content-Type", "Content-Encoding", "ETag", "Last-Modified" };
        foreach (var h in toCache)
        {
            if (headers.ContainsKey(h))
            {
                relevant[h] = headers[h].ToString();
            }
        }
        return relevant;
    }

    private async Task ServeCachedResponse(HttpContext context, CacheEntry cached)
    {
        context.Response.StatusCode = cached.StatusCode;
        context.Response.ContentType = cached.ContentType;
        foreach (var header in cached.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }
        context.Response.Headers["X-Cache"] = "HIT";
        context.Response.Headers["X-Cache-Key"] = cached.Key;
        context.Response.Headers["X-Cache-Age"] = ((DateTime.UtcNow - cached.CreatedAt).TotalSeconds).ToString("F0");
        await context.Response.WriteAsync(cached.Content);
    }

    private bool IsExpired(CacheEntry entry) => DateTime.UtcNow > entry.ExpiresAt;

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            _cache.CleanupExpired();
        }
        catch (Exception ex)
        {
            _logger.Error("[ResponseCaching] Error during cleanup", ex);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _logger.Info("[ResponseCaching] Middleware disposed");
    }
}

public sealed class ResponseCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    private const int MAX_CACHE_SIZE = 10000;
    private const int CLEANUP_BATCH_SIZE = 100;

    public CacheEntry? Get(string key) => _cache.TryGetValue(key, out var entry) ? entry : null;

    public void Set(CacheEntry entry)
    {
        _cache[entry.Key] = entry;
        if (_cache.Count > MAX_CACHE_SIZE)
        {
            EvictOldestEntries();
        }
    }

    public int CleanupExpired()
    {
        var expiredKeys = _cache.Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
        var cleaned = 0;
        foreach (var key in expiredKeys)
        {
            if (_cache.TryRemove(key, out _)) cleaned++;
        }
        return cleaned;
    }

    private void EvictOldestEntries()
    {
        var toEvict = _cache.OrderBy(kvp => kvp.Value.CreatedAt)
            .Take(_cache.Count - MAX_CACHE_SIZE + CLEANUP_BATCH_SIZE)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in toEvict)
        {
            _cache.TryRemove(key, out _);
        }
    }
}

public record CacheEntry(
    string Key,
    string Content,
    string ContentType,
    int StatusCode,
    Dictionary<string, string> Headers,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

