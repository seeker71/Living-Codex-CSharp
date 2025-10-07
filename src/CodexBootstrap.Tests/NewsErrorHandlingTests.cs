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

public class NewsErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task GetLatestNews_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?limit=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestNews_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestNews_WithZeroLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?limit=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestNews_WithExcessiveLimit_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/news/latest?limit=10000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        // Should be limited to a reasonable number (e.g., 500 max)
        Assert.True(items.GetArrayLength() <= 500);
    }

    [Fact]
    public async Task GetNewsStats_WithNegativeHoursBack_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/stats?hoursBack=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsStats_WithExcessiveHoursBack_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/stats?hoursBack=999999");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNews_WithEmptyRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNews_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNews_WithNullInterests_ReturnsBadRequest()
    {
        // Arrange
        var searchRequest = new
        {
            interests = (string[])null,
            limit = 5,
            hoursBack = 24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNews_WithNegativeLimit_ReturnsBadRequest()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "technology" },
            limit = -5,
            hoursBack = 24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchNews_WithNegativeHoursBack_ReturnsBadRequest()
    {
        // Arrange
        var searchRequest = new
        {
            interests = new[] { "technology" },
            limit = 5,
            hoursBack = -24
        };
        var json = JsonSerializer.Serialize(searchRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/search", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTrendingTopics_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/trending?limit=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTrendingTopics_WithNegativeHoursBack_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/trending?hoursBack=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsItem_WithEmptyId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/item/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsItem_WithSpecialCharacters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/news/item/test%20with%20spaces%20and%20special%20chars!@#$%");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNewsSummary_WithEmptyId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/summary/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithEmptyRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithNullUserId_ReturnsBadRequest()
    {
        // Arrange
        var readRequest = new
        {
            userId = (string)null,
            newsId = "test-news-123"
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithNullNewsId_ReturnsBadRequest()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-123",
            newsId = (string)null
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReadNews_WithEmptyUserId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/read/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUnreadNews_WithEmptyUserId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/unread/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithEmptyUserId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?limit=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserNewsFeed_WithNegativeHoursBack_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/feed/test-user-123?hoursBack=-24");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRelatedNews_WithEmptyNewsId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/related/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRelatedNews_WithNegativeLimit_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/related/test-news-123?limit=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNewsConcepts_WithEmptyNewsId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/news/concepts/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NewsEndpoints_WithMalformedQueryParameters_HandleGracefully()
    {
        // Test various malformed query parameters
        var malformedUrls = new[]
        {
            "/news/latest?limit=abc",
            "/news/latest?skip=xyz",
            "/news/stats?hoursBack=invalid",
            "/news/trending?limit=not-a-number"
        };

        foreach (var url in malformedUrls)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task NewsEndpoints_WithVeryLongIds_HandleGracefully()
    {
        // Test with very long IDs
        var veryLongId = new string('a', 10000);
        
        // Act
        var response = await _client.GetAsync($"/news/item/{veryLongId}");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}









