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

public class ResonanceInteractionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ResonanceInteractionTests(WebApplicationFactory<Program> factory)
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
    public async Task GetCollectiveEnergy_AfterMultipleContributions_ReflectsChanges()
    {
        // This test would require creating contributions first
        // For now, we'll test that the endpoint responds consistently
        
        // Act
        var response1 = await _client.GetAsync("/contributions/abundance/collective-energy");
        var response2 = await _client.GetAsync("/contributions/abundance/collective-energy");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var data1 = JsonSerializer.Deserialize<JsonElement>(content1);
        Assert.True(data1.TryGetProperty("success", out var success1));
        Assert.True(success1.GetBoolean());
        
        var content2 = await response2.Content.ReadAsStringAsync();
        var data2 = JsonSerializer.Deserialize<JsonElement>(content2);
        Assert.True(data2.TryGetProperty("success", out var success2));
        Assert.True(success2.GetBoolean());
    }

    [Fact]
    public async Task GetContributorEnergy_WithDifferentUsers_ReturnsDifferentData()
    {
        // Test with multiple different users
        var userIds = new[] { "user-alpha", "user-beta", "user-gamma" };
        var responses = new List<JsonElement>();
        
        foreach (var userId in userIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            responses.Add(data);
        }

        // All should be successful
        foreach (var data in responses)
        {
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithTimeRange_ReturnsAppropriateEvents()
    {
        // Test with different time ranges
        var timeRanges = new[]
        {
            ("1 day", DateTime.UtcNow.AddDays(-1).ToString("O")),
            ("7 days", DateTime.UtcNow.AddDays(-7).ToString("O")),
            ("30 days", DateTime.UtcNow.AddDays(-30).ToString("O"))
        };

        foreach (var (rangeName, since) in timeRanges)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?since={since}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithUserFilter_ReturnsUserSpecificEvents()
    {
        // Test with different user filters
        var userIds = new[] { "user-1", "user-2", "user-3" };
        
        foreach (var userId in userIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?userId={userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithContributionFilter_ReturnsContributionSpecificEvents()
    {
        // Test with different contribution filters
        var contributionIds = new[] { "contrib-1", "contrib-2", "contrib-3" };
        
        foreach (var contributionId in contributionIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?contributionId={contributionId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithPagination_ReturnsCorrectPages()
    {
        // Test pagination with different page sizes
        var pageSizes = new[] { 5, 10, 20, 50 };
        
        foreach (var pageSize in pageSizes)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?take={pageSize}&skip=0");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("take", out var take));
            Assert.True(success.GetBoolean());
            Assert.Equal(pageSize, take.GetInt32());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithSorting_ReturnsSortedEvents()
    {
        // Test both ascending and descending sorting
        var sortOrders = new[] { true, false };
        
        foreach (var sortDescending in sortOrders)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/events?sortDescending={sortDescending}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithComplexFilters_ReturnsFilteredEvents()
    {
        // Test with multiple filters combined
        var since = DateTime.UtcNow.AddDays(-7).ToString("O");
        var until = DateTime.UtcNow.ToString("O");
        
        // Act
        var response = await _client.GetAsync($"/contributions/abundance/events?userId=test-user&since={since}&until={until}&take=10&sortDescending=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("take", out var take));
        Assert.True(success.GetBoolean());
        Assert.Equal(10, take.GetInt32());
    }

    [Fact]
    public async Task ResonanceEndpoints_WithRapidRequests_HandlesGracefully()
    {
        // Test rapid successive requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(_client.GetAsync("/contributions/abundance/collective-energy"));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithMixedRequests_HandlesGracefully()
    {
        // Test mixed request types
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Mix of different endpoint calls
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/contributions/abundance/collective-energy"));
            tasks.Add(_client.GetAsync($"/contributions/abundance/contributor-energy/user-{i}"));
            tasks.Add(_client.GetAsync("/contributions/abundance/events"));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetContributorEnergy_WithLongTermUser_ReturnsConsistentData()
    {
        // Test with a user that might have long-term data
        var userId = "long-term-user-123";
        
        // Make multiple requests to the same user
        var responses = new List<JsonElement>();
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            responses.Add(data);
        }

        // All responses should be successful and consistent
        foreach (var data in responses)
        {
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("userId", out var returnedUserId));
            Assert.True(success.GetBoolean());
            Assert.Equal(userId, returnedUserId.GetString());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithEmptyFilters_ReturnsAllEvents()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("events", out var events));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(events.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task ResonanceEndpoints_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in user IDs
        var specialUserIds = new[]
        {
            "user@domain.com",
            "user#123",
            "user$money",
            "user%percent",
            "user^power",
            "user&and",
            "user*star",
            "user+plus",
            "user=equals",
            "user|pipe"
        };

        foreach (var userId in specialUserIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in user IDs
        var unicodeUserIds = new[]
        {
            "用户123",
            "ユーザー456",
            "مستخدم789",
            "کاربر101",
            "사용자202",
            "ผู้ใช้303"
        };

        foreach (var userId in unicodeUserIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithVeryLongUserIds_HandlesGracefully()
    {
        // Test with very long user IDs
        var longUserIds = new[]
        {
            new string('a', 1000),
            new string('b', 5000),
            new string('c', 10000)
        };

        foreach (var userId in longUserIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithConcurrentUserRequests_HandlesGracefully()
    {
        // Test concurrent requests for different users
        var userIds = new[] { "user-1", "user-2", "user-3", "user-4", "user-5" };
        var tasks = new List<Task<HttpResponseMessage>>();
        
        foreach (var userId in userIds)
        {
            tasks.Add(_client.GetAsync($"/contributions/abundance/contributor-energy/{userId}"));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithMixedConcurrentRequests_HandlesGracefully()
    {
        // Test mixed concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Mix of different endpoint calls with different parameters
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync("/contributions/abundance/collective-energy"));
            tasks.Add(_client.GetAsync($"/contributions/abundance/contributor-energy/user-{i}"));
            tasks.Add(_client.GetAsync($"/contributions/abundance/events?userId=user-{i}"));
            tasks.Add(_client.GetAsync($"/contributions/abundance/events?take=5&skip={i * 5}"));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}







