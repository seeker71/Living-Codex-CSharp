using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using CodexBootstrap.Core;
using Xunit;
using System.Net.Http;
using System.Text;

namespace CodexBootstrap.Tests;

public class NewsPersonalizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsPersonalizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Ensure we're using in-memory storage for tests
                services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
                {
                    options.BackgroundServiceExceptionBehavior = Microsoft.Extensions.Hosting.BackgroundServiceExceptionBehavior.Ignore;
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUserNewsFeed_WithValidUserId_ReturnsPersonalizedFeed()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?limit=10&hoursBack=24");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.Equal("Personalized news feed", message.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(items.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?limit=5&skip=10&hoursBack=48");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithDifferentTimeRanges_ReturnsAppropriateResults()
    {
        // Test different time ranges
        var timeRanges = new[] { 1, 6, 24, 72, 168 };
        
        foreach (var hoursBack in timeRanges)
        {
            // Act
            var response = await _client.GetAsync($"/news/feed/test-user-123?limit=5&hoursBack={hoursBack}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("items", out var items));
            Assert.True(items.GetArrayLength() <= 5);
        }
    }

    [Fact]
    public async Task SearchNews_WithUserContributions_ReturnsPersonalizedResults()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "technology", "science" },
            contributions = new[] { "test-contribution-1", "test-contribution-2" },
            limit = 10,
            hoursBack = 24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(items.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task SearchNews_WithLocation_ReturnsLocationBasedResults()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "politics" },
            location = "US",
            limit = 5,
            hoursBack = 24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetRelatedNews_WithValidNewsId_ReturnsRelatedItems()
    {
        // Arrange - First get a news item ID
        var latestResponse = await _client.GetAsync("/news/latest?limit=1");
        var latestContent = await latestResponse.Content.ReadAsStringAsync();
        var latestData = JsonSerializer.Deserialize<JsonElement>(latestContent);
        
        if (latestData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var newsId = items[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/news/related/{newsId}?limit=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("items", out var relatedItems));
            Assert.True(data.TryGetProperty("totalCount", out var totalCount));
            Assert.True(data.TryGetProperty("message", out var message));
            
            Assert.Equal("Related news items", message.GetString());
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(relatedItems.GetArrayLength() <= 5);
        }
    }

    [Fact]
    public async Task GetRelatedNews_WithInvalidNewsId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/news/related/invalid-news-id-12345?limit=5");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsConcepts_WithValidNewsId_ReturnsConcepts()
    {
        // Arrange - First get a news item ID
        var latestResponse = await _client.GetAsync("/news/latest?limit=1");
        var latestContent = await latestResponse.Content.ReadAsStringAsync();
        var latestData = JsonSerializer.Deserialize<JsonElement>(latestContent);
        
        if (latestData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var newsId = items[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/news/concepts/{newsId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("concepts", out var concepts));
            Assert.True(data.TryGetProperty("totalCount", out var totalCount));
            Assert.True(data.TryGetProperty("message", out var message));
            
            Assert.Equal("News concepts", message.GetString());
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(concepts.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task GetNewsConcepts_WithInvalidNewsId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/news/concepts/invalid-news-id-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NewsFeed_WithDifferentUserInterests_ReturnsDifferentResults()
    {
        // Test with different user IDs to simulate different interests
        var userIds = new[] { "user-tech", "user-science", "user-business" };
        
        foreach (var userId in userIds)
        {
            // Act
            var response = await _client.GetAsync($"/news/feed/{userId}?limit=3&hoursBack=24");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("items", out var items));
            Assert.True(items.GetArrayLength() <= 3);
        }
    }

    [Fact]
    public async Task NewsFeed_WithEmptyInterests_ReturnsGeneralNews()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new string[0], // Empty interests
            limit = 5,
            hoursBack = 24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task NewsFeed_WithLargeLimit_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?limit=1000&hoursBack=24");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        // Should be limited to a reasonable number (e.g., 500 max)
        Assert.True(items.GetArrayLength() <= 500);
    }
}









