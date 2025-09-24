using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Simple LLM client for Ollama integration
/// </summary>
[MetaNodeAttribute("codex.core.llm-client", "codex.meta/type", "LLMClient", "LLM client for Ollama integration")]
public class LLMClient
{
    private readonly HttpClient _httpClient;
    private readonly Core.ICodexLogger _logger;
    private readonly string _baseUrl;

    public LLMClient(HttpClient httpClient, Core.ICodexLogger logger, string baseUrl = "http://localhost:11434")
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Check if LLM service is available
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Provider-aware availability check using supplied configuration
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync(CodexBootstrap.Modules.LLMConfig config)
    {
        try
        {
            var provider = (config.Provider ?? "").ToLowerInvariant();
            var forceOllamaEnv = Environment.GetEnvironmentVariable("USE_OLLAMA_ONLY");
            var forceOllama = string.Equals(forceOllamaEnv ?? "true", "true", StringComparison.OrdinalIgnoreCase);
            if (forceOllama)
            {
                provider = "ollama";
            }
            if (provider == "openai")
            {
                // Check if API key is available and not empty
                if (string.IsNullOrEmpty(config.ApiKey))
                {
                    _logger.Warn("OpenAI API key is not configured");
                    return false;
                }
                
                // Use the configured HttpClient with proper timeout
                var request = new HttpRequestMessage(HttpMethod.Get, $"{config.BaseUrl.TrimEnd('/')}/models");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            if (provider == "cursor")
            {
                // Cursor Background Agent API availability check
                if (string.IsNullOrEmpty(config.ApiKey))
                {
                    _logger.Warn("Cursor API key is not configured");
                    return false;
                }
                
                var request = new HttpRequestMessage(HttpMethod.Get, $"{config.BaseUrl.TrimEnd('/')}/health");
                request.Headers.Add("X-API-Key", config.ApiKey);
                
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            
            // Default to Ollama check
            var ollamaRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost:11434/api/tags");
            var ollamaResponse = await _httpClient.SendAsync(ollamaRequest);
            return ollamaResponse.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Warn($"LLM service availability check failed for {config.Provider}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Send a query to the LLM
    /// </summary>
    public async Task<LLMResponse> QueryAsync(string prompt, CodexBootstrap.Modules.LLMConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = (config.Provider ?? "").ToLowerInvariant();
            var forceOllamaEnv = Environment.GetEnvironmentVariable("USE_OLLAMA_ONLY");
            var forceOllama = string.Equals(forceOllamaEnv ?? "true", "true", StringComparison.OrdinalIgnoreCase);
            if (forceOllama)
            {
                provider = "ollama";
            }

            if (provider == "openai")
            {
                // OpenAI Chat Completions
                var body = new
                {
                    model = config.Model,
                    messages = new object[]
                    {
                        new { role = "user", content = prompt }
                    },
                    // Omit temperature/top_p for OpenAI chat to avoid unsupported_value errors on some models
                    max_completion_tokens = config.MaxTokens
                };

                var json = JsonSerializer.Serialize(body);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                }

                _logger.Info($"Sending OpenAI chat completion with model {config.Model}");
                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.Error($"OpenAI request failed: {response.StatusCode} - {err}");

                    // Retry on 500 errors (server issues) or model fallback on 404
                    var status = (int)response.StatusCode;
                    if (status == 500)
                    {
                        _logger.Warn($"OpenAI server error (500), retrying in 2 seconds...");
                        await Task.Delay(2000);
                        
                        // Retry the same request
                        var retryResponse = await _httpClient.SendAsync(request, cancellationToken);
                        if (retryResponse.IsSuccessStatusCode)
                        {
                            var retryContent = await retryResponse.Content.ReadAsStringAsync();
                            return LLMResponseParsers.ParseOpenAIChatResponse(retryContent, config.Model);
                        }
                        else
                        {
                            var retryErr = await retryResponse.Content.ReadAsStringAsync();
                            _logger.Error($"OpenAI retry also failed: {retryResponse.StatusCode} - {retryErr}");
                        }
                    }
                    else if (status == 404 || err.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Task-aware fallback models
                        var fallbackCandidates = GetFallbackModelsForTask(config.Model);

                        foreach (var candidate in fallbackCandidates)
                        {
                            if (string.IsNullOrWhiteSpace(candidate) || string.Equals(candidate, config.Model, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            _logger.Warn($"Retrying OpenAI call with fallback model '{candidate}'...");
                            var retryBody = new
                            {
                                model = candidate,
                                messages = new object[] { new { role = "user", content = prompt } },
                                max_completion_tokens = config.MaxTokens
                            };
                            var retryJson = JsonSerializer.Serialize(retryBody);
                            var retryReq = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/chat/completions")
                            {
                                Content = new StringContent(retryJson, Encoding.UTF8, "application/json")
                            };
                            if (!string.IsNullOrEmpty(config.ApiKey))
                            {
                                retryReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
                            }

                            var retryResp = await _httpClient.SendAsync(retryReq);
                            if (retryResp.IsSuccessStatusCode)
                            {
                                var retryContent = await retryResp.Content.ReadAsStringAsync();
                                using var retryDoc = JsonDocument.Parse(retryContent);
                                var retryText = retryDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                                int retryTokens = 0;
                                if (retryDoc.RootElement.TryGetProperty("usage", out var usage2) && usage2.TryGetProperty("total_tokens", out var total2))
                                {
                                    retryTokens = total2.GetInt32();
                                }

                                return new LLMResponse
                                {
                                    Success = true,
                                    Response = retryText,
                                    Confidence = 0.85,
                                    Model = candidate,
                                    TokensUsed = retryTokens
                                };
                            }
                        }
                    }

                    return new LLMResponse { Success = false, Response = $"OpenAI error: {response.StatusCode}", Confidence = 0.0 };
                }

                var respContent = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(respContent))
                {
                    var contentText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                    int tokensUsed = 0;
                    if (doc.RootElement.TryGetProperty("usage", out var usage))
                    {
                        if (usage.TryGetProperty("total_tokens", out var totalTokens))
                        {
                            tokensUsed = totalTokens.GetInt32();
                        }
                    }

                    return new LLMResponse
                    {
                        Success = true,
                        Response = contentText,
                        Confidence = 0.85,
                        Model = config.Model,
                        TokensUsed = tokensUsed
                    };
                }
            }
            else if (provider == "cursor")
            {
                // Cursor Background Agent API - use their specific endpoint structure
                var body = new
                {
                    prompt = prompt,
                    model = config.Model,
                    temperature = config.Temperature,
                    max_completion_tokens = config.MaxTokens,
                    stream = false
                };

                var json = JsonSerializer.Serialize(body);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl.TrimEnd('/')}/generate")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    request.Headers.Add("X-API-Key", config.ApiKey);
                }

                _logger.Info($"Sending Cursor background agent request with model {config.Model}");
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.Error($"Cursor request failed: {response.StatusCode} - {err}");
                    return new LLMResponse { Success = false, Response = $"Cursor error: {response.StatusCode}", Confidence = 0.0 };
                }

                var respContent = await response.Content.ReadAsStringAsync();
                using (var doc = JsonDocument.Parse(respContent))
                {
                    // Cursor API response format (adjust based on actual response structure)
                    var contentText = "";
                    if (doc.RootElement.TryGetProperty("text", out var textProp))
                    {
                        contentText = textProp.GetString() ?? "";
                    }
                    else if (doc.RootElement.TryGetProperty("response", out var respProp))
                    {
                        contentText = respProp.GetString() ?? "";
                    }
                    else if (doc.RootElement.TryGetProperty("content", out var contentProp))
                    {
                        contentText = contentProp.GetString() ?? "";
                    }

                    int tokensUsed = 0;
                    if (doc.RootElement.TryGetProperty("usage", out var usage))
                    {
                        if (usage.TryGetProperty("total_tokens", out var totalTokens))
                        {
                            tokensUsed = totalTokens.GetInt32();
                        }
                    }

                    return new LLMResponse
                    {
                        Success = true,
                        Response = contentText,
                        Confidence = 0.85,
                        Model = config.Model,
                        TokensUsed = tokensUsed
                    };
                }
            }
            else
            {
                // Default: Ollama-compatible endpoint
                var request = new
                {
                    model = config.Model,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = config.Temperature,
                        top_p = config.TopP,
                        max_completion_tokens = config.MaxTokens
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var targetBase = string.IsNullOrEmpty(config.BaseUrl) ? _baseUrl : config.BaseUrl;
                _logger.Info($"Sending LLM query to {targetBase}/api/generate with model {config.Model}");
                var response = await _httpClient.PostAsync($"{targetBase.TrimEnd('/')}/api/generate", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error($"LLM request failed with status {response.StatusCode}");
                    return new LLMResponse { Success = false, Response = $"LLM request failed: {response.StatusCode}", Confidence = 0.0 };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.Debug($"Raw LLM response content: '{responseContent}'");
                var llmResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                _logger.Debug($"Deserialized LLM response: Response='{llmResponse?.Response}', Done={llmResponse?.Done}");

                return new LLMResponse
                {
                    Success = true,
                    Response = llmResponse?.Response ?? "No response from LLM",
                    Confidence = 0.8,
                    Model = config.Model,
                    TokensUsed = llmResponse?.Done == true ? 1 : 0
                };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calling LLM: {ex.Message}", ex);
            return new LLMResponse
            {
                Success = false,
                Response = $"Error calling LLM: {ex.Message}",
                Confidence = 0.0
            };
        }
    }

    /// <summary>
    /// Get appropriate fallback models based on the current model name
    /// </summary>
    private static string[] GetFallbackModelsForTask(string currentModel)
    {
        // If it's a code generation model, use code-specific fallbacks
        if (currentModel.Contains("codex") || currentModel.Contains("code"))
        {
            return new[]
            {
                Environment.GetEnvironmentVariable("OPENAI_CODEGEN_MODEL") ?? "gpt-4o",
                "gpt-4o", // Good for code generation
                "gpt-4o-mini" // Cheaper fallback
            };
        }
        
        // For news analysis and other non-code tasks, use cheap, fast models
        return new[]
        {
            Environment.GetEnvironmentVariable("OPENAI_DEFAULT_MODEL") ?? "gpt-4o-mini",
            "gpt-4o-mini", // Cheapest, fastest for news analysis
            "gpt-4o", // Slightly better quality
            "gpt-4.1-mini" // Alternative cheap option
        };
    }

    /// <summary>
    /// Check if the LLM service is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Helpers for parsing provider-specific responses
/// </summary>
internal static class LLMResponseParsers
{
    public static LLMResponse ParseOpenAIChatResponse(string json, string model)
    {
        using var doc = JsonDocument.Parse(json);
        var contentText = doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        int tokensUsed = 0;
        if (doc.RootElement.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("total_tokens", out var totalTokens))
            {
                tokensUsed = totalTokens.GetInt32();
            }
        }

        return new LLMResponse
        {
            Success = true,
            Response = contentText,
            Confidence = 0.85,
            Model = model,
            TokensUsed = tokensUsed
        };
    }
}

/// <summary>
/// LLM response structure
/// </summary>
[ResponseType("codex.core.llm-response", "LLMResponse", "Response from LLM client")]
public class LLMResponse
{
    public bool Success { get; set; }
    public string Response { get; set; } = "";
    public double Confidence { get; set; }
    public string Model { get; set; } = "";
    public int TokensUsed { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Ollama API response structure
/// </summary>
[ResponseType("codex.core.ollama-response", "OllamaResponse", "Response from Ollama API")]
public class OllamaResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = "";
    
    [JsonPropertyName("done")]
    public bool Done { get; set; }
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";
}
