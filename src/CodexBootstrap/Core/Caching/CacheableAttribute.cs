using System;

namespace CodexBootstrap.Core.Caching
{
    /// <summary>
    /// Attribute to mark endpoints as cacheable with specified TTL
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CacheableAttribute : Attribute
    {
        /// <summary>
        /// Time to live in seconds
        /// </summary>
        public int TtlSeconds { get; set; } = 300; // Default 5 minutes

        /// <summary>
        /// Cache key suffix for differentiation
        /// </summary>
        public string? KeySuffix { get; set; }

        /// <summary>
        /// Whether to include request body in cache key
        /// </summary>
        public bool IncludeBody { get; set; } = true;

        /// <summary>
        /// Whether to include query parameters in cache key
        /// </summary>
        public bool IncludeQuery { get; set; } = true;

        public CacheableAttribute(int ttlSeconds = 300)
        {
            TtlSeconds = ttlSeconds;
        }
    }
}




