using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;

namespace CodexBootstrap.Core.Caching
{
    /// <summary>
    /// Endpoint-level caching service that stores endpoint results as cached nodes with TTL
    /// Each endpoint result is represented as a node with automatic expiration
    /// </summary>
    public class EndpointCacheService
    {
        private readonly INodeRegistry _registry;
        private readonly ICodexLogger _logger;
        private readonly TimeSpan _defaultTtl;

        public EndpointCacheService(INodeRegistry registry, ICodexLogger logger, TimeSpan? defaultTtl = null)
        {
            _registry = registry;
            _logger = logger;
            _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5); // Default 5-minute TTL
        }

        /// <summary>
        /// Get cached result for endpoint with given arguments
        /// </summary>
        /// <param name="endpoint">The endpoint path</param>
        /// <param name="method">HTTP method</param>
        /// <param name="arguments">Serializable arguments/parameters</param>
        /// <returns>Cached result if valid, null if expired or not found</returns>
        public T? GetCached<T>(string endpoint, string method, object? arguments = null) where T : class
        {
            var cacheKey = GenerateCacheKey(endpoint, method, arguments);
            var cacheNodeId = $"cache.endpoint.{cacheKey}";

            if (_registry.TryGet(cacheNodeId, out var cacheNode))
            {
                // Check if cache is still valid
                if (IsCacheValid(cacheNode))
                {
                    try
                    {
                        if (cacheNode.Content?.InlineJson != null)
                        {
                            var result = JsonSerializer.Deserialize<T>(cacheNode.Content.InlineJson);
                            _logger.Debug($"Cache hit for endpoint {endpoint} {method}");
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to deserialize cached result for {endpoint}: {ex.Message}");
                    }
                }
                else
                {
                    // Cache expired, remove it
                    _registry.RemoveNode(cacheNodeId);
                    _logger.Debug($"Cache expired for endpoint {endpoint} {method}");
                }
            }

            _logger.Debug($"Cache miss for endpoint {endpoint} {method}");
            return null;
        }

        /// <summary>
        /// Store result in cache with TTL
        /// </summary>
        /// <param name="endpoint">The endpoint path</param>
        /// <param name="method">HTTP method</param>
        /// <param name="result">Result to cache</param>
        /// <param name="arguments">Serializable arguments/parameters</param>
        /// <param name="ttl">Time to live, uses default if null</param>
        public void SetCache<T>(string endpoint, string method, T result, object? arguments = null, TimeSpan? ttl = null)
        {
            var cacheKey = GenerateCacheKey(endpoint, method, arguments);
            var cacheNodeId = $"cache.endpoint.{cacheKey}";
            var cacheTtl = ttl ?? _defaultTtl;
            var expiresAt = DateTimeOffset.UtcNow.Add(cacheTtl);

            try
            {
                var cacheNode = new Node(
                    Id: cacheNodeId,
                    TypeId: "codex.cache.endpoint-result",
                    State: ContentState.Water, // Cache is always water (ephemeral)
                    Locale: "en",
                    Title: $"Cached result for {endpoint} {method}",
                    Description: $"Cached endpoint result with TTL {cacheTtl.TotalMinutes:F1} minutes",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(result),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["endpoint"] = endpoint,
                        ["method"] = method,
                        ["arguments"] = arguments ?? new object(),
                        ["cachedAt"] = DateTimeOffset.UtcNow,
                        ["expiresAt"] = expiresAt,
                        ["ttlSeconds"] = cacheTtl.TotalSeconds,
                        ["cacheKey"] = cacheKey
                    }
                );

                _registry.Upsert(cacheNode);
                _logger.Debug($"Cached result for endpoint {endpoint} {method} with TTL {cacheTtl.TotalMinutes:F1} minutes");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to cache result for endpoint {endpoint}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Invalidate cache for specific endpoint and arguments
        /// </summary>
        public void InvalidateCache(string endpoint, string method, object? arguments = null)
        {
            var cacheKey = GenerateCacheKey(endpoint, method, arguments);
            var cacheNodeId = $"cache.endpoint.{cacheKey}";
            
            _registry.RemoveNode(cacheNodeId);
            _logger.Debug($"Invalidated cache for endpoint {endpoint} {method}");
        }

        /// <summary>
        /// Clear all expired cache entries
        /// </summary>
        public void ClearExpiredCache()
        {
            try
            {
                var cacheNodes = _registry.GetNodesByType("codex.cache.endpoint-result");
                var expiredCount = 0;

                foreach (var node in cacheNodes)
                {
                    if (!IsCacheValid(node))
                    {
                        _registry.RemoveNode(node.Id);
                        expiredCount++;
                    }
                }

                if (expiredCount > 0)
                {
                    _logger.Info($"Cleared {expiredCount} expired cache entries");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to clear expired cache: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public object GetCacheStats()
        {
            try
            {
                var cacheNodes = _registry.GetNodesByType("codex.cache.endpoint-result").ToList();
                var now = DateTimeOffset.UtcNow;
                var validCount = 0;
                var expiredCount = 0;
                var totalSize = 0L;

                foreach (var node in cacheNodes)
                {
                    if (IsCacheValid(node))
                    {
                        validCount++;
                    }
                    else
                    {
                        expiredCount++;
                    }

                    if (node.Content?.InlineJson != null)
                    {
                        totalSize += Encoding.UTF8.GetByteCount(node.Content.InlineJson);
                    }
                }

                return new
                {
                    totalEntries = cacheNodes.Count,
                    validEntries = validCount,
                    expiredEntries = expiredCount,
                    totalSizeBytes = totalSize,
                    defaultTtlMinutes = _defaultTtl.TotalMinutes
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get cache stats: {ex.Message}", ex);
                return new { error = "Failed to retrieve cache statistics" };
            }
        }

        private string GenerateCacheKey(string endpoint, string method, object? arguments)
        {
            var keyData = $"{method}:{endpoint}:{JsonSerializer.Serialize(arguments ?? new object())}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
            return Convert.ToHexString(hashBytes).ToLowerInvariant()[..16]; // First 16 chars for brevity
        }

        private bool IsCacheValid(Node cacheNode)
        {
            if (cacheNode.Meta?.TryGetValue("expiresAt", out var expiresAtObj) == true)
            {
                if (expiresAtObj is DateTimeOffset expiresAt)
                {
                    return DateTimeOffset.UtcNow < expiresAt;
                }
                else if (DateTime.TryParse(expiresAtObj.ToString(), out var expiresAtDateTime))
                {
                    return DateTime.UtcNow < expiresAtDateTime;
                }
            }
            
            // If no expiration info, consider expired
            return false;
        }
    }
}
