using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Helper class for cross-module communication using HTTP calls
    /// Handles port configuration, error handling, and response parsing
    /// </summary>
    public class CrossModuleCommunicator
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public CrossModuleCommunicator()
        {
            _logger = new Log4NetLogger(typeof(CrossModuleCommunicator));
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Call another module's API endpoint with proper error handling
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="moduleName">Name of the target module</param>
        /// <param name="endpoint">API endpoint (without leading slash)</param>
        /// <param name="request">Request object</param>
        /// <param name="httpMethod">HTTP method (default: POST)</param>
        /// <returns>Response object or null if failed</returns>
        public async Task<TResponse?> CallModuleAsync<TRequest, TResponse>(
            string moduleName, 
            string endpoint, 
            TRequest request, 
            string httpMethod = "POST") 
            where TResponse : class
        {
            try
            {
                var url = BuildModuleUrl(moduleName, endpoint);
                _logger.Debug($"Calling {moduleName} module at {url}");

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (httpMethod.ToUpper() == "GET")
                {
                    response = await _httpClient.GetAsync(url);
                }
                else
                {
                    response = await _httpClient.PostAsync(url, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"Module {moduleName} returned error status {response.StatusCode} for endpoint {endpoint}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Handle standard API response format
                if (jsonResponse.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    if (jsonResponse.TryGetProperty("data", out var data))
                    {
                        return JsonSerializer.Deserialize<TResponse>(data.GetRawText());
                    }
                    else
                    {
                        // If no data property, try to deserialize the whole response
                        return JsonSerializer.Deserialize<TResponse>(responseContent);
                    }
                }
                else
                {
                    if (jsonResponse.TryGetProperty("error", out var error))
                    {
                        _logger.Error($"Module {moduleName} returned error: {error.GetString()}");
                    }
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"HTTP error calling {moduleName} module: {ex.Message}", ex);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON parsing error calling {moduleName} module: {ex.Message}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error calling {moduleName} module: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Call another module's API endpoint and return raw JsonElement
        /// </summary>
        public async Task<JsonElement?> CallModuleRawAsync(
            string moduleName, 
            string endpoint, 
            object request, 
            string httpMethod = "POST")
        {
            try
            {
                var url = BuildModuleUrl(moduleName, endpoint);
                _logger.Debug($"Calling {moduleName} module at {url}");

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (httpMethod.ToUpper() == "GET")
                {
                    response = await _httpClient.GetAsync(url);
                }
                else
                {
                    response = await _httpClient.PostAsync(url, content);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"Module {moduleName} returned error status {response.StatusCode} for endpoint {endpoint}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(responseContent);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error calling {moduleName} module: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Check if a module is available and responding
        /// </summary>
        public async Task<bool> IsModuleAvailableAsync(string moduleName)
        {
            try
            {
                var url = BuildModuleUrl(moduleName, "health");
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Build the full URL for a module endpoint
        /// </summary>
        private string BuildModuleUrl(string moduleName, string endpoint)
        {
            var baseUrl = GlobalConfiguration.BaseUrl;
            var port = GlobalConfiguration.Port;
            
            // Remove trailing slash from baseUrl if present
            if (baseUrl.EndsWith("/"))
            {
                baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            }
            
            // Ensure endpoint starts with slash
            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }
            
            // Build URL properly - if baseUrl already contains port, don't add it again
            if (baseUrl.Contains(":"))
            {
                return $"{baseUrl}/{moduleName}{endpoint}";
            }
            else
            {
                return $"{baseUrl}:{port}/{moduleName}{endpoint}";
            }
        }

        /// <summary>
        /// Dispose the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

}
