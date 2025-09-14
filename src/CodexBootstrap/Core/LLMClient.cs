using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// Simple LLM client for Ollama integration
/// </summary>
[MetaNodeAttribute("codex.core.llm-client", "codex.meta/type", "LLMClient", "LLM client for Ollama integration")]
public class LLMClient
{
    private readonly HttpClient _httpClient;
    private readonly Core.ILogger _logger;
    private readonly string _baseUrl;

    public LLMClient(HttpClient httpClient, Core.ILogger logger, string baseUrl = "http://localhost:11434")
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
    /// Send a query to the LLM
    /// </summary>
    public async Task<LLMResponse> QueryAsync(string prompt, CodexBootstrap.Modules.LLMConfig config)
    {
        try
        {
            var request = new
            {
                model = config.Model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = config.Temperature,
                    top_p = config.TopP,
                    max_tokens = config.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.Info($"Sending LLM query to {_baseUrl}/api/generate with model {config.Model}");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"LLM request failed with status {response.StatusCode}");
                return new LLMResponse
                {
                    Success = false,
                    Response = $"LLM request failed: {response.StatusCode}",
                    Confidence = 0.0
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.Debug($"Raw LLM response content: '{responseContent}'");
            
            var llmResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
            _logger.Debug($"Deserialized LLM response: Response='{llmResponse?.Response}', Done={llmResponse?.Done}");

            return new LLMResponse
            {
                Success = true,
                Response = llmResponse?.Response ?? "No response from LLM",
                Confidence = 0.8, // Default confidence for now
                Model = config.Model,
                TokensUsed = llmResponse?.Done == true ? 1 : 0
            };
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
