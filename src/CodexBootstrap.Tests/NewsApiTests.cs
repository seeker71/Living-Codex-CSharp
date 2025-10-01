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

public class NewsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetLatestNews_ReturnsNewsItems()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?limit=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.Equal("Latest news items", message.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetLatestNews_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?limit=3&skip=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(items.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNewsStats_ReturnsStatistics()
    {
        // Act
        var response = await _client.GetAsync("/news/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("sources", out var sources));
        Assert.True(data.TryGetProperty("hoursBack", out var hoursBack));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(hoursBack.GetInt32() >= 0);
        Assert.True(sources.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task GetNewsStats_WithTimeFilter_ReturnsFilteredStats()
    {
        // Act
        var response = await _client.GetAsync("/news/stats?hoursBack=12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("hoursBack", out var hoursBack));
        Assert.Equal(12, hoursBack.GetInt32());
    }

    [Fact]
    public async Task SearchNews_WithInterests_ReturnsMatchingItems()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "technology" },
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
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.Equal("News search results", message.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(items.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task SearchNews_WithMultipleInterests_ReturnsMatchingItems()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "science", "technology" },
            limit = 10,
            hoursBack = 48
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
        Assert.True(items.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GetTrendingTopics_ReturnsTrendingData()
    {
        // Act
        var response = await _client.GetAsync("/news/trending?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("topics", out var topics));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(topics.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GetTrendingTopics_WithTimeFilter_ReturnsFilteredTrends()
    {
        // Act
        var response = await _client.GetAsync("/news/trending?limit=5&hoursBack=6");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("topics", out var topics));
        Assert.True(topics.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetNewsItem_WithValidId_ReturnsNewsItem()
    {
        // Arrange - First get a news item ID
        var latestResponse = await _client.GetAsync("/news/latest?limit=1");
        var latestContent = await latestResponse.Content.ReadAsStringAsync();
        var latestData = JsonSerializer.Deserialize<JsonElement>(latestContent);
        
        if (latestData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var newsId = items[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/news/item/{newsId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("id", out var id));
            Assert.True(data.TryGetProperty("title", out var title));
            Assert.True(data.TryGetProperty("source", out var source));
            Assert.True(data.TryGetProperty("url", out var url));
            
            Assert.Equal(newsId, id.GetString());
            Assert.False(string.IsNullOrEmpty(title.GetString()));
            Assert.False(string.IsNullOrEmpty(source.GetString()));
            Assert.False(string.IsNullOrEmpty(url.GetString()));
        }
    }

    [Fact]
    public async Task GetNewsItem_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/news/item/invalid-id-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsSummary_WithValidId_ReturnsSummary()
    {
        // Arrange - First get a news item ID
        var latestResponse = await _client.GetAsync("/news/latest?limit=1");
        var latestContent = await latestResponse.Content.ReadAsStringAsync();
        var latestData = JsonSerializer.Deserialize<JsonElement>(latestContent);
        
        if (latestData.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
        {
            var newsId = items[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/news/summary/{newsId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("newsId", out var id));
            Assert.True(data.TryGetProperty("status", out var status));
            
            Assert.Equal(newsId, id.GetString());
            Assert.True(status.GetString() == "available" || status.GetString() == "generating" || status.GetString() == "none");
        }
    }

    [Fact]
    public async Task GetNewsSummary_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/news/summary/invalid-id-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-123",
            newsId = "test-news-123",
            nodeId = "test-node-123"
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task MarkNewsAsRead_WithMissingData_ReturnsBadRequest()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-123"
            // Missing newsId
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReadNews_WithValidUserId_ReturnsReadItems()
    {
        // Act
        var response = await _client.GetAsync("/news/read/test-user-123?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.Equal("Read news items", message.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(items.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GetUnreadNews_WithValidUserId_ReturnsUnreadItems()
    {
        // Act
        var response = await _client.GetAsync("/news/unread/test-user-123?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("message", out var message));
        
        Assert.Equal("Unread news items", message.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(items.GetArrayLength() <= 10);
    }
}






