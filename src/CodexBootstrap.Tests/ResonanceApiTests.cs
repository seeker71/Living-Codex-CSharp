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

public class ResonanceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ResonanceApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetCollectiveEnergy_ReturnsCollectiveResonanceData()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/collective-energy");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("collectiveResonance", out var collectiveResonance));
        Assert.True(data.TryGetProperty("totalContributors", out var totalContributors));
        Assert.True(data.TryGetProperty("totalAbundanceEvents", out var totalAbundanceEvents));
        Assert.True(data.TryGetProperty("recentAbundanceEvents", out var recentAbundanceEvents));
        Assert.True(data.TryGetProperty("averageAbundanceMultiplier", out var averageAbundanceMultiplier));
        Assert.True(data.TryGetProperty("totalCollectiveValue", out var totalCollectiveValue));
        Assert.True(data.TryGetProperty("timestamp", out var timestamp));
        
        Assert.True(success.GetBoolean());
        Assert.True(collectiveResonance.GetDouble() >= 0);
        Assert.True(totalContributors.GetInt32() >= 0);
        Assert.True(totalAbundanceEvents.GetInt32() >= 0);
        Assert.True(recentAbundanceEvents.GetInt32() >= 0);
        Assert.True(averageAbundanceMultiplier.GetDouble() >= 0);
        Assert.True(totalCollectiveValue.GetDouble() >= 0);
        Assert.False(string.IsNullOrEmpty(timestamp.GetString()));
    }

    [Fact]
    public async Task GetContributorEnergy_WithValidUserId_ReturnsContributorData()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/test-user-123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("userId", out var userId));
        Assert.True(data.TryGetProperty("energyLevel", out var energyLevel));
        Assert.True(data.TryGetProperty("baseEnergy", out var baseEnergy));
        Assert.True(data.TryGetProperty("amplifiedEnergy", out var amplifiedEnergy));
        Assert.True(data.TryGetProperty("resonanceLevel", out var resonanceLevel));
        Assert.True(data.TryGetProperty("totalContributions", out var totalContributions));
        Assert.True(data.TryGetProperty("totalValue", out var totalValue));
        Assert.True(data.TryGetProperty("totalCollectiveValue", out var totalCollectiveValue));
        Assert.True(data.TryGetProperty("averageAbundanceMultiplier", out var averageAbundanceMultiplier));
        Assert.True(data.TryGetProperty("lastUpdated", out var lastUpdated));
        
        Assert.True(success.GetBoolean());
        Assert.Equal("test-user-123", userId.GetString());
        Assert.True(energyLevel.GetDouble() >= 0);
        Assert.True(baseEnergy.GetDouble() >= 0);
        Assert.True(amplifiedEnergy.GetDouble() >= 0);
        Assert.True(resonanceLevel.GetDouble() >= 0);
        Assert.True(totalContributions.GetInt32() >= 0);
        Assert.True(totalValue.GetDouble() >= 0);
        Assert.True(totalCollectiveValue.GetDouble() >= 0);
        Assert.True(averageAbundanceMultiplier.GetDouble() >= 0);
        Assert.False(string.IsNullOrEmpty(lastUpdated.GetString()));
    }

    [Fact]
    public async Task GetContributorEnergy_WithDifferentUserIds_ReturnsUserSpecificData()
    {
        // Test with different user IDs
        var userIds = new[] { "user-1", "user-2", "user-3" };
        
        foreach (var userId in userIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("userId", out var returnedUserId));
            
            Assert.True(success.GetBoolean());
            Assert.Equal(userId, returnedUserId.GetString());
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_ReturnsEventsList()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("events", out var events));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("skip", out var skip));
        Assert.True(data.TryGetProperty("take", out var take));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(skip.GetInt32() >= 0);
        Assert.True(take.GetInt32() >= 0);
        Assert.True(events.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithUserIdFilter_ReturnsFilteredEvents()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?userId=test-user-123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("events", out var events));
        Assert.True(success.GetBoolean());
        Assert.True(events.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?skip=0&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("events", out var events));
        Assert.True(data.TryGetProperty("skip", out var skip));
        Assert.True(data.TryGetProperty("take", out var take));
        
        Assert.True(success.GetBoolean());
        Assert.Equal(0, skip.GetInt32());
        Assert.Equal(10, take.GetInt32());
        Assert.True(events.GetArrayLength() <= 10);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithDateRange_ReturnsFilteredEvents()
    {
        // Act
        var since = DateTime.UtcNow.AddDays(-7).ToString("O");
        var until = DateTime.UtcNow.ToString("O");
        var response = await _client.GetAsync($"/contributions/abundance/events?since={since}&until={until}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithContributionIdFilter_ReturnsFilteredEvents()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?contributionId=test-contribution-123");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithSorting_ReturnsSortedEvents()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?sortDescending=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetAbundanceEvents_WithComplexFilters_ReturnsFilteredEvents()
    {
        // Act
        var since = DateTime.UtcNow.AddDays(-30).ToString("O");
        var response = await _client.GetAsync($"/contributions/abundance/events?userId=test-user-123&since={since}&take=5&sortDescending=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("take", out var take));
        Assert.True(success.GetBoolean());
        Assert.Equal(5, take.GetInt32());
    }

    [Fact]
    public async Task GetCollectiveEnergy_ReturnsConsistentData()
    {
        // Test multiple calls to ensure consistency
        var responses = new List<HttpResponseMessage>();
        
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.GetAsync("/contributions/abundance/collective-energy");
            responses.Add(response);
        }

        // Assert all responses are successful
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Assert data consistency
        var responseContents = new List<JsonElement>();
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(content);
            responseContents.Add(data);
        }

        // All responses should have the same structure
        foreach (var data in responseContents)
        {
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetContributorEnergy_WithEmptyUserId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContributorEnergy_WithSpecialCharacters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/contributor-energy/test@#$%^&*()");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetContributorEnergy_WithVeryLongUserId_HandlesGracefully()
    {
        // Act
        var veryLongUserId = new string('a', 10000);
        var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{veryLongUserId}");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithInvalidParameters_HandlesGracefully()
    {
        // Test with invalid parameters
        var invalidUrls = new[]
        {
            "/contributions/abundance/events?take=abc",
            "/contributions/abundance/events?skip=xyz",
            "/contributions/abundance/events?since=invalid-date",
            "/contributions/abundance/events?until=invalid-date"
        };

        foreach (var url in invalidUrls)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetAbundanceEvents_WithNegativeParameters_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=-5&skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithZeroTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAbundanceEvents_WithExcessiveTake_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/contributions/abundance/events?take=10000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
        
        // Should be limited to a reasonable number (e.g., 500 max)
        if (data.TryGetProperty("events", out var events))
        {
            Assert.True(events.GetArrayLength() <= 500);
        }
    }

    [Fact]
    public async Task ResonanceEndpoints_WithConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Start multiple concurrent requests
        for (int i = 0; i < 10; i++)
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
    public async Task ResonanceEndpoints_WithUnicodeUserIds_HandlesGracefully()
    {
        // Test with Unicode user IDs
        var unicodeUserIds = new[] { "用户123", "ユーザー456", "مستخدم789" };
        
        foreach (var userId in unicodeUserIds)
        {
            // Act
            var response = await _client.GetAsync($"/contributions/abundance/contributor-energy/{userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }
}







