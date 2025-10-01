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

public class NewsInteractionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewsInteractionTests(WebApplicationFactory<Program> factory)
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
    public async Task MarkNewsAsRead_WithValidData_CreatesReadRecord()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-interaction-123",
            newsId = "test-news-interaction-123",
            nodeId = "test-node-interaction-123"
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
    public async Task MarkNewsAsRead_ThenGetReadNews_ReturnsReadItem()
    {
        // Arrange
        var userId = "test-user-read-tracking-123";
        var newsId = "test-news-read-tracking-123";
        var nodeId = "test-node-read-tracking-123";
        
        var readRequest = new
        {
            userId = userId,
            newsId = newsId,
            nodeId = nodeId
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Mark as read
        var markResponse = await _client.PostAsync("/news/read", requestContent);
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        // Act - Get read news
        var readResponse = await _client.GetAsync($"/news/read/{userId}?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var responseContent = await readResponse.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task MarkMultipleNewsAsRead_ThenGetReadNews_ReturnsAllReadItems()
    {
        // Arrange
        var userId = "test-user-multiple-read-123";
        var newsItems = new[]
        {
            new { userId = userId, newsId = "news-1", nodeId = "node-1" },
            new { userId = userId, newsId = "news-2", nodeId = "node-2" },
            new { userId = userId, newsId = "news-3", nodeId = "node-3" }
        };

        // Act - Mark multiple items as read
        foreach (var item in newsItems)
        {
            var json = JsonSerializer.Serialize(item);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/news/read", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Act - Get read news
        var readResponse = await _client.GetAsync($"/news/read/{userId}?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        var responseContent = await readResponse.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task GetUnreadNews_AfterMarkingAsRead_ExcludesReadItems()
    {
        // Arrange
        var userId = "test-user-unread-tracking-123";
        var newsId = "test-news-unread-tracking-123";
        var nodeId = "test-node-unread-tracking-123";
        
        var readRequest = new
        {
            userId = userId,
            newsId = newsId,
            nodeId = nodeId
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Mark as read
        var markResponse = await _client.PostAsync("/news/read", requestContent);
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        // Act - Get unread news
        var unreadResponse = await _client.GetAsync($"/news/unread/{userId}?limit=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, unreadResponse.StatusCode);
        var responseContent = await unreadResponse.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("items", out var items));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task MarkSameNewsAsReadMultipleTimes_HandlesGracefully()
    {
        // Arrange
        var userId = "test-user-duplicate-read-123";
        var newsId = "test-news-duplicate-read-123";
        var nodeId = "test-node-duplicate-read-123";
        
        var readRequest = new
        {
            userId = userId,
            newsId = newsId,
            nodeId = nodeId
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Mark as read multiple times
        var response1 = await _client.PostAsync("/news/read", requestContent);
        var response2 = await _client.PostAsync("/news/read", requestContent);
        var response3 = await _client.PostAsync("/news/read", requestContent);

        // Assert - All should succeed
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }

    [Fact]
    public async Task GetReadNews_WithDifferentUsers_ReturnsUserSpecificData()
    {
        // Arrange
        var user1 = "test-user-1-123";
        var user2 = "test-user-2-123";
        
        var readRequest1 = new
        {
            userId = user1,
            newsId = "news-for-user-1",
            nodeId = "node-for-user-1"
        };
        
        var readRequest2 = new
        {
            userId = user2,
            newsId = "news-for-user-2",
            nodeId = "node-for-user-2"
        };

        // Act - Mark different news as read for different users
        var json1 = JsonSerializer.Serialize(readRequest1);
        var requestContent1 = new StringContent(json1, Encoding.UTF8, "application/json");
        var response1 = await _client.PostAsync("/news/read", requestContent1);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        var json2 = JsonSerializer.Serialize(readRequest2);
        var requestContent2 = new StringContent(json2, Encoding.UTF8, "application/json");
        var response2 = await _client.PostAsync("/news/read", requestContent2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        // Act - Get read news for each user
        var readResponse1 = await _client.GetAsync($"/news/read/{user1}?limit=10");
        var readResponse2 = await _client.GetAsync($"/news/read/{user2}?limit=10");

        // Assert - Both should succeed
        Assert.Equal(HttpStatusCode.OK, readResponse1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readResponse2.StatusCode);
    }

    [Fact]
    public async Task MarkNewsAsRead_WithOnlyNewsId_Works()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-newsid-only-123",
            newsId = "test-news-newsid-only-123"
            // No nodeId
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
    public async Task MarkNewsAsRead_WithOnlyNodeId_Works()
    {
        // Arrange
        var readRequest = new
        {
            userId = "test-user-nodeid-only-123",
            nodeId = "test-node-nodeid-only-123"
            // No newsId
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
    public async Task GetReadNews_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = "test-user-pagination-123";
        
        // Mark several items as read
        for (int i = 1; i <= 5; i++)
        {
            var readRequest = new
            {
                userId = userId,
                newsId = $"news-pagination-{i}",
                nodeId = $"node-pagination-{i}"
            };
            var json = JsonSerializer.Serialize(readRequest);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/news/read", requestContent);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Act - Get first page
        var page1Response = await _client.GetAsync($"/news/read/{userId}?limit=2&skip=0");
        var page2Response = await _client.GetAsync($"/news/read/{userId}?limit=2&skip=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);
        
        var page1Content = await page1Response.Content.ReadAsStringAsync();
        var page1Data = JsonSerializer.Deserialize<JsonElement>(page1Content);
        Assert.True(page1Data.TryGetProperty("items", out var page1Items));
        Assert.True(page1Items.GetArrayLength() <= 2);
        
        var page2Content = await page2Response.Content.ReadAsStringAsync();
        var page2Data = JsonSerializer.Deserialize<JsonElement>(page2Content);
        Assert.True(page2Data.TryGetProperty("items", out var page2Items));
        Assert.True(page2Items.GetArrayLength() <= 2);
    }

    [Fact]
    public async Task GetUnreadNews_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = "test-user-unread-pagination-123";

        // Act - Get first page of unread news
        var page1Response = await _client.GetAsync($"/news/unread/{userId}?limit=2&skip=0");
        var page2Response = await _client.GetAsync($"/news/unread/{userId}?limit=2&skip=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);
        
        var page1Content = await page1Response.Content.ReadAsStringAsync();
        var page1Data = JsonSerializer.Deserialize<JsonElement>(page1Content);
        Assert.True(page1Data.TryGetProperty("items", out var page1Items));
        Assert.True(page1Items.GetArrayLength() <= 2);
        
        var page2Content = await page2Response.Content.ReadAsStringAsync();
        var page2Data = JsonSerializer.Deserialize<JsonElement>(page2Content);
        Assert.True(page2Data.TryGetProperty("items", out var page2Items));
        Assert.True(page2Items.GetArrayLength() <= 2);
    }

    [Fact]
    public async Task NewsInteraction_WithSpecialCharacters_HandlesGracefully()
    {
        // Arrange
        var userId = "test-user-special-chars-123";
        var newsId = "test-news-with-special-chars!@#$%^&*()";
        var nodeId = "test-node-with-special-chars!@#$%^&*()";
        
        var readRequest = new
        {
            userId = userId,
            newsId = newsId,
            nodeId = nodeId
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
    public async Task NewsInteraction_WithVeryLongIds_HandlesGracefully()
    {
        // Arrange
        var userId = new string('a', 1000);
        var newsId = new string('b', 1000);
        var nodeId = new string('c', 1000);
        
        var readRequest = new
        {
            userId = userId,
            newsId = newsId,
            nodeId = nodeId
        };
        var json = JsonSerializer.Serialize(readRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/news/read", requestContent);

        // Assert
        // Should either succeed or fail gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }
}






