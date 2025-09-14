using System;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Global configuration for the Living Codex system
    /// </summary>
    public static class GlobalConfiguration
    {
        private static string _baseUrl = "http://localhost:5000";
        private static int _port = 5000;
        private static bool _isInitialized = false;

        /// <summary>
        /// Gets the base URL for the API
        /// </summary>
        public static string BaseUrl => _baseUrl;

        /// <summary>
        /// Gets the port number
        /// </summary>
        public static int Port => _port;

        /// <summary>
        /// Gets whether the configuration has been initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initialize the global configuration with the base URL
        /// </summary>
        /// <param name="baseUrl">The base URL (e.g., "http://localhost:5001")</param>
        public static void Initialize(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
                throw new ArgumentException("Base URL cannot be null or empty", nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/');
            
            // Extract port from URL with error handling
            _port = 5001; // Default port
            try
            {
                if (Uri.TryCreate(_baseUrl, UriKind.Absolute, out var uri))
                {
                    _port = uri.Port;
                }
                else
                {
                    // Fallback to environment variable or default
                    var portEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
                    if (!string.IsNullOrEmpty(portEnv) && portEnv.Contains(":"))
                    {
                        var portStr = portEnv.Split(':').LastOrDefault();
                        if (int.TryParse(portStr, out var envPort))
                        {
                            _port = envPort;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not parse URL '{_baseUrl}', using default port 5001. Error: {ex.Message}");
            }
            
            _isInitialized = true;
            
            Console.WriteLine($"Global configuration initialized: BaseUrl={_baseUrl}, Port={_port}");
        }

        /// <summary>
        /// Initialize with just a port number (assumes localhost)
        /// </summary>
        /// <param name="port">The port number</param>
        public static void Initialize(int port)
        {
            Initialize($"http://localhost:{port}");
        }

        /// <summary>
        /// Get the full URL for a specific endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint path (e.g., "/health", "/ai/extract-concepts")</param>
        /// <returns>The full URL</returns>
        public static string GetUrl(string endpoint)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Global configuration has not been initialized. Call Initialize() first.");

            if (string.IsNullOrEmpty(endpoint))
                return _baseUrl;

            var cleanEndpoint = endpoint.TrimStart('/');
            return $"{_baseUrl}/{cleanEndpoint}";
        }

        /// <summary>
        /// Reset the configuration (useful for testing)
        /// </summary>
        public static void Reset()
        {
            _baseUrl = "http://localhost:5000";
            _port = 5000;
            _isInitialized = false;
        }
    }
}
