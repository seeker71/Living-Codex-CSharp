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
    /// Streamlined wrapper for module-to-module communication via API router dynamic calls
    /// Provides templates, error handling, and logging for simplified inter-module communication
    /// </summary>
    public class ModuleCommunicationWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public ModuleCommunicationWrapper(ILogger logger, string baseUrl = null)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _baseUrl = baseUrl?.TrimEnd('/') ?? GlobalConfiguration.BaseUrl;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Generic method for making API calls between modules with full error handling and logging
        /// </summary>
        public async Task<ModuleResponse<TResponse>> CallModuleAsync<TRequest, TResponse>(
            string moduleName,
            string endpoint,
            TRequest request,
            HttpMethod method = null,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            var callId = Guid.NewGuid().ToString("N")[..8];
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.Info($"[{callId}] Calling {moduleName}/{endpoint} - {method?.Method ?? "POST"}");

                method ??= HttpMethod.Post;
                var url = $"{_baseUrl}/{moduleName}/{endpoint}";
                
                using var requestMessage = new HttpRequestMessage(method, url);
                
                // Add headers
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        requestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                // Add content for POST/PUT requests
                if (method == HttpMethod.Post || method == HttpMethod.Put)
                {
                    var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                    requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    _logger.Debug($"[{callId}] Request payload: {jsonContent}");
                }

                // Set timeout
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                
                var response = await _httpClient.SendAsync(requestMessage, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                var duration = DateTime.UtcNow - startTime;
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions);
                        _logger.Info($"[{callId}] Success - {moduleName}/{endpoint} completed in {duration.TotalMilliseconds:F0}ms");
                        return new ModuleResponse<TResponse>
                        {
                            Success = true,
                            Data = result,
                            StatusCode = response.StatusCode,
                            Duration = duration,
                            CallId = callId
                        };
                    }
                    catch (JsonException ex)
                    {
                        _logger.Error($"[{callId}] JSON deserialization failed for {moduleName}/{endpoint}: {ex.Message}");
                        return new ModuleResponse<TResponse>
                        {
                            Success = false,
                            Error = $"Failed to deserialize response: {ex.Message}",
                            StatusCode = response.StatusCode,
                            Duration = duration,
                            CallId = callId
                        };
                    }
                }
                else
                {
                    _logger.Warn($"[{callId}] HTTP error {response.StatusCode} for {moduleName}/{endpoint}: {responseContent}");
                    return new ModuleResponse<TResponse>
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        StatusCode = response.StatusCode,
                        Duration = duration,
                        CallId = callId
                    };
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.Error($"[{callId}] Timeout calling {moduleName}/{endpoint} after {duration.TotalSeconds:F1}s");
                return new ModuleResponse<TResponse>
                {
                    Success = false,
                    Error = $"Timeout after {timeoutSeconds}s",
                    Duration = duration,
                    CallId = callId
                };
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.Error($"[{callId}] HTTP request failed for {moduleName}/{endpoint}: {ex.Message}");
                return new ModuleResponse<TResponse>
                {
                    Success = false,
                    Error = $"HTTP request failed: {ex.Message}",
                    Duration = duration,
                    CallId = callId
                };
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.Error($"[{callId}] Unexpected error calling {moduleName}/{endpoint}: {ex.Message}", ex);
                return new ModuleResponse<TResponse>
                {
                    Success = false,
                    Error = $"Unexpected error: {ex.Message}",
                    Duration = duration,
                    CallId = callId
                };
            }
        }

        /// <summary>
        /// Simplified GET call for modules
        /// </summary>
        public async Task<ModuleResponse<TResponse>> GetAsync<TResponse>(
            string moduleName,
            string endpoint,
            Dictionary<string, string> queryParams = null,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            var url = $"{_baseUrl}/{moduleName}/{endpoint}";
            
            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                url += $"?{queryString}";
            }

            return await CallModuleAsync<object, TResponse>(
                moduleName, 
                endpoint, 
                null, 
                HttpMethod.Get, 
                headers, 
                timeoutSeconds);
        }

        /// <summary>
        /// Simplified POST call for modules
        /// </summary>
        public async Task<ModuleResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string moduleName,
            string endpoint,
            TRequest request,
            Dictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            return await CallModuleAsync<TRequest, TResponse>(
                moduleName, 
                endpoint, 
                request, 
                HttpMethod.Post, 
                headers, 
                timeoutSeconds);
        }

        /// <summary>
        /// Call AI module for concept extraction
        /// </summary>
        public async Task<ModuleResponse<ConceptAnalysis>> ExtractConceptsAsync(ConceptExtractionRequest request)
        {
            return await PostAsync<ConceptExtractionRequest, ConceptAnalysis>(
                "ai", 
                "extract-concepts", 
                request);
        }

        /// <summary>
        /// Call AI module for LLM future query
        /// </summary>
        public async Task<ModuleResponse<LLMFutureQueryResponse>> LLMFutureQueryAsync(LLMFutureQueryRequest request)
        {
            return await PostAsync<LLMFutureQueryRequest, LLMFutureQueryResponse>(
                "ai", 
                "llm-future-query", 
                request);
        }

        /// <summary>
        /// Call concept module for concept registration
        /// </summary>
        public async Task<ModuleResponse<ConceptRegistrationResponse>> RegisterConceptAsync(ConceptRegistrationRequest request)
        {
            return await PostAsync<ConceptRegistrationRequest, ConceptRegistrationResponse>(
                "concept", 
                "register", 
                request);
        }

        /// <summary>
        /// Call concept module for concept discovery
        /// </summary>
        public async Task<ModuleResponse<ConceptDiscoveryResponse>> DiscoverConceptsAsync(ConceptDiscoveryRequest request)
        {
            return await PostAsync<ConceptDiscoveryRequest, ConceptDiscoveryResponse>(
                "concept", 
                "discover", 
                request);
        }

        /// <summary>
        /// Call joy module for joy calculation
        /// </summary>
        public async Task<ModuleResponse<JoyCalculationResponse>> CalculateJoyAsync(JoyCalculationRequest request)
        {
            return await PostAsync<JoyCalculationRequest, JoyCalculationResponse>(
                "joy", 
                "calculate", 
                request);
        }

        /// <summary>
        /// Call joy module for resonance calculation
        /// </summary>
        public async Task<ModuleResponse<ResonanceCalculationResponse>> CalculateResonanceAsync(ResonanceCalculationRequest request)
        {
            return await PostAsync<ResonanceCalculationRequest, ResonanceCalculationResponse>(
                "joy", 
                "resonance/calculate", 
                request);
        }

        /// <summary>
        /// Call translation module for text translation
        /// </summary>
        public async Task<ModuleResponse<TranslationResponse>> TranslateAsync(TranslationRequest request)
        {
            return await PostAsync<TranslationRequest, TranslationResponse>(
                "translation", 
                "translate", 
                request);
        }

        /// <summary>
        /// Get module health status
        /// </summary>
        public async Task<ModuleResponse<ModuleHealthResponse>> GetModuleHealthAsync(string moduleName)
        {
            return await GetAsync<ModuleHealthResponse>(
                moduleName, 
                "health");
        }

        /// <summary>
        /// Get all available modules
        /// </summary>
        public async Task<ModuleResponse<List<ModuleInfo>>> GetModulesAsync()
        {
            return await GetAsync<List<ModuleInfo>>(
                "", 
                "modules");
        }

        /// <summary>
        /// Batch call multiple modules in parallel
        /// </summary>
        public async Task<Dictionary<string, ModuleResponse<object>>> BatchCallAsync(
            Dictionary<string, ModuleCall> calls,
            int maxConcurrency = 5)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = calls.Select(async kvp =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var call = kvp.Value;
                    var response = await CallModuleAsync<object, object>(
                        call.ModuleName,
                        call.Endpoint,
                        call.Request,
                        call.Method,
                        call.Headers,
                        call.TimeoutSeconds);
                    
                    return new KeyValuePair<string, ModuleResponse<object>>(kvp.Key, response);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Response wrapper for module communication
    /// </summary>
    public class ModuleResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public System.Net.HttpStatusCode? StatusCode { get; set; }
        public TimeSpan Duration { get; set; }
        public string CallId { get; set; }
    }

    /// <summary>
    /// Module call definition for batch operations
    /// </summary>
    public class ModuleCall
    {
        public string ModuleName { get; set; }
        public string Endpoint { get; set; }
        public object Request { get; set; }
        public HttpMethod Method { get; set; } = HttpMethod.Post;
        public Dictionary<string, string> Headers { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Module health response
    /// </summary>
    public class ModuleHealthResponse
    {
        public string Status { get; set; }
        public string ModuleName { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metrics { get; set; }
    }


    // Data structures for common module calls
    public class ConceptExtractionRequest
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string Source { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class ConceptAnalysis
    {
        public string Id { get; set; }
        public string NewsItemId { get; set; }
        public List<string> Concepts { get; set; } = new();
        public double Confidence { get; set; }
        public List<string> OntologyLevels { get; set; } = new();
        public DateTimeOffset ExtractedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class LLMFutureQueryRequest
    {
        public string Query { get; set; } = "";
        public string Context { get; set; } = "";
        public string TimeHorizon { get; set; } = "";
        public string Perspective { get; set; } = "";
        public LLMConfig LLMConfig { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class LLMFutureQueryResponse
    {
        public string Id { get; set; }
        public string Query { get; set; }
        public string Response { get; set; }
        public double Confidence { get; set; }
        public string Reasoning { get; set; }
        public List<string> Sources { get; set; } = new();
        public DateTimeOffset GeneratedAt { get; set; }
        public LLMConfig UsedConfig { get; set; }
    }


    public class ConceptRegistrationRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Frequency { get; set; } = "";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ConceptRegistrationResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ConceptDiscoveryRequest
    {
        public string Content { get; set; } = "";
        public string[] Categories { get; set; } = Array.Empty<string>();
        public int MaxConcepts { get; set; } = 10;
    }

    public class ConceptDiscoveryResponse
    {
        public List<ConceptInfo> Concepts { get; set; } = new();
        public int TotalFound { get; set; }
        public double Confidence { get; set; }
    }

    public class ConceptInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public double Score { get; set; }
        public string Frequency { get; set; }
    }

    public class JoyCalculationRequest
    {
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string Context { get; set; } = "";
    }

    public class JoyCalculationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public double JoyLevel { get; set; }
    }

    public class ResonanceCalculationRequest
    {
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string Context { get; set; } = "";
    }

    public class ResonanceCalculationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public double ResonanceLevel { get; set; }
        public List<ResonancePattern> Patterns { get; set; } = new();
    }

    public class ResonancePattern
    {
        public string Name { get; set; }
        public double Strength { get; set; }
        public string Frequency { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TranslationRequest
    {
        public string Text { get; set; } = "";
        public string SourceLanguage { get; set; } = "en";
        public string TargetLanguage { get; set; } = "es";
        public string Context { get; set; } = "";
    }

    public class TranslationResponse
    {
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }
        public double Confidence { get; set; }
        public DateTimeOffset TranslatedAt { get; set; }
    }
}
