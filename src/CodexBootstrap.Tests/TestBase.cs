using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests;

public abstract class TestBase
{
    public readonly HttpClient Client;
    public readonly JsonSerializerOptions JsonOptions;
    protected readonly string BaseUrl = "http://localhost:5002";

    protected TestBase()
    {
        Client = new HttpClient();
        Client.BaseAddress = new Uri(BaseUrl);
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    protected async Task<T> GetJsonAsync<T>(string endpoint)
    {
        var response = await Client.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions) ?? throw new InvalidOperationException("Deserialization returned null");
    }

    protected async Task<T> PostJsonAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await Client.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, JsonOptions) ?? throw new InvalidOperationException("Deserialization returned null");
    }

    protected async Task AssertEndpointExists(string endpoint, string method = "GET")
    {
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        var response = await Client.SendAsync(request);
        
        // Should not return 404 Not Found
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.NotFound, 
            $"Endpoint {method} {endpoint} should exist");
    }

    protected async Task AssertHealthCheck()
    {
        var health = await GetJsonAsync<dynamic>("/health");
        health.Should().NotBeNull();
        
        // Check if it's a health response
        var healthString = health.ToString();
        healthString.Should().Contain("status");
    }
}
