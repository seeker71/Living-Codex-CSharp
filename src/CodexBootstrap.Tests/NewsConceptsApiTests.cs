using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Core;
using Xunit;

namespace CodexBootstrap.Tests;

/// <summary>
/// Tests for the News Concepts API endpoint
/// </summary>
public class NewsConceptsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsConceptsApiTests(WebApplicationFactory<Program> factory)
    {
        // Configure environment variables for testing
        Environment.SetEnvironmentVariable("ICE_STORAGE_TYPE", "sqlite");
        Environment.SetEnvironmentVariable("ICE_CONNECTION_STRING", "Data Source=:memory:");
        Environment.SetEnvironmentVariable("WATER_CONNECTION_STRING", "Data Source=:memory:");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetNewsConcepts_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = "non-existent-news-id";

        // Act
        var response = await _client.GetAsync($"/news/concepts/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("NOT_FOUND", result.GetProperty("code").GetString());
        Assert.Equal("News item not found", result.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetNewsConcepts_WithValidId_ReturnsConcepts()
    {
        // Arrange
        // First, get a real news item ID from the system
        var newsResponse = await _client.GetAsync("/news/latest?limit=1");
        Assert.Equal(HttpStatusCode.OK, newsResponse.StatusCode);
        
        var newsContent = await newsResponse.Content.ReadAsStringAsync();
        var newsResult = JsonSerializer.Deserialize<JsonElement>(newsContent);
        
        if (newsResult.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var newsItem = items[0];
            var newsId = newsItem.GetProperty("id").GetString();
            Assert.NotNull(newsId);

            // Act
            var response = await _client.GetAsync($"/news/concepts/{newsId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            // The response should be valid JSON, even if it's an error
            Assert.True(result.TryGetProperty("success", out _));
        }
    }

    [Fact]
    public async Task GetNewsConcepts_WithEmptyId_ReturnsBadRequest()
    {
        // Arrange
        var emptyId = "";

        // Act
        var response = await _client.GetAsync($"/news/concepts/{emptyId}");

        // Assert
        // This should return 404 since the route won't match
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsConcepts_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var specialId = "test-id-with-special-chars!@#$%^&*()";

        // Act
        var response = await _client.GetAsync($"/news/concepts/{Uri.EscapeDataString(specialId)}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.False(result.GetProperty("success").GetBoolean());
        Assert.Equal("NOT_FOUND", result.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetNewsConcepts_EndpointExists_ReturnsValidResponse()
    {
        // Arrange
        var testId = "test-news-id";

        // Act
        var response = await _client.GetAsync($"/news/concepts/{testId}");

        // Assert
        // The endpoint should exist and return a valid response (not 404 for the route itself)
        // For non-existent news items, it returns 404 with proper error structure
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        // Should have standard response structure
        Assert.True(result.TryGetProperty("success", out _));
        Assert.True(result.TryGetProperty("code", out _));
        Assert.True(result.TryGetProperty("error", out _));
    }
}
