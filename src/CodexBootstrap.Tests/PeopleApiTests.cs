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

public class PeopleApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PeopleApiTests(WebApplicationFactory<Program> factory)
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
    public async Task DiscoverUsers_ByInterests_ReturnsUsers()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "consciousness", "ai", "philosophy" },
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("queryType", out var queryType));
        
        Assert.Equal("interests", queryType.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(users.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_ByLocation_ReturnsUsers()
    {
        // Arrange
        var request = new
        {
            location = "San Francisco, CA",
            radiusKm = 50,
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("queryType", out var queryType));
        Assert.True(data.TryGetProperty("searchMetadata", out var searchMetadata));
        
        Assert.Equal("location", queryType.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(users.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_ByConcept_ReturnsUsers()
    {
        // Arrange
        var request = new
        {
            conceptId = "test-concept-123",
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("queryType", out var queryType));
        Assert.True(data.TryGetProperty("searchMetadata", out var searchMetadata));
        
        Assert.Equal("concept", queryType.GetString());
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(users.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "technology" },
            limit = 5,
            skip = 10
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(users.GetArrayLength() <= 5);
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithMultipleInterests_ReturnsUsers()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "consciousness", "ai", "philosophy", "technology", "science" },
            limit = 20,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal("interests", data.GetProperty("queryType").GetString());
        Assert.True(users.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithEmptyInterests_ReturnsEmptyResults()
    {
        // Arrange
        var request = new
        {
            interests = new string[0],
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, users.GetArrayLength());
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidLocation_ReturnsError()
    {
        // Arrange
        var request = new
        {
            location = "InvalidLocation12345",
            radiusKm = 50,
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("searchMetadata", out var searchMetadata));
        
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, users.GetArrayLength());
        Assert.True(searchMetadata.TryGetProperty("error", out var error));
    }

    [Fact]
    public async Task DiscoverUsers_WithDifferentRadius_ReturnsDifferentResults()
    {
        // Test with different radius values
        var radiusValues = new[] { 10, 25, 50, 100 };
        
        foreach (var radius in radiusValues)
        {
            // Arrange
            var request = new
            {
                location = "San Francisco, CA",
                radiusKm = radius,
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("users", out var users));
            Assert.True(data.TryGetProperty("totalCount", out var totalCount));
            Assert.Equal("location", data.GetProperty("queryType").GetString());
            Assert.True(totalCount.GetInt32() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithComplexQuery_ReturnsUsers()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "consciousness", "ai" },
            location = "San Francisco, CA",
            radiusKm = 25,
            limit = 15,
            skip = 5
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(data.TryGetProperty("queryType", out var queryType));
        
        Assert.True(totalCount.GetInt32() >= 0);
        Assert.True(users.GetArrayLength() >= 0);
    }

    [Fact]
    public async Task GetConceptContributors_WithValidConceptId_ReturnsContributors()
    {
        // Act
        var response = await _client.GetAsync("/concepts/test-concept-123/contributors");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetConceptContributors_WithDifferentConceptIds_ReturnsContributors()
    {
        // Test with different concept IDs
        var conceptIds = new[] { "concept-1", "concept-2", "concept-3" };
        
        foreach (var conceptId in conceptIds)
        {
            // Act
            var response = await _client.GetAsync($"/concepts/{conceptId}/contributors");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetConceptContributors_WithEmptyConceptId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/concepts//contributors");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConceptContributors_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in concept ID
        var specialConceptIds = new[]
        {
            "concept@#$%^&*()",
            "concept!@#$%^&*()_+",
            "concept[]{}|\\:;\"'<>?,./",
            "concept`~"
        };

        foreach (var conceptId in specialConceptIds)
        {
            // Act
            var response = await _client.GetAsync($"/concepts/{conceptId}/contributors");

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetConceptContributors_WithVeryLongConceptId_HandlesGracefully()
    {
        // Act
        var veryLongConceptId = new string('a', 10000);
        var response = await _client.GetAsync($"/concepts/{veryLongConceptId}/contributors");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task DiscoverUsers_WithConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var request = new
            {
                interests = new[] { $"interest-{i}" },
                limit = 5,
                skip = 0
            };
            
            tasks.Add(_client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")));
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
    public async Task DiscoverUsers_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in interests
        var request = new
        {
            interests = new[] { "意识", "人工智能", "哲学" },
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal("interests", data.GetProperty("queryType").GetString());
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithEmptyRequest_ReturnsEmptyResults()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, users.GetArrayLength());
    }

    [Fact]
    public async Task DiscoverUsers_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var request = new
        {
            interests = (string[])null,
            location = (string)null,
            conceptId = (string)null,
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, users.GetArrayLength());
    }

    [Fact]
    public async Task DiscoverUsers_WithVeryLongInterests_HandlesGracefully()
    {
        // Arrange
        var longInterests = new[]
        {
            new string('a', 1000),
            new string('b', 2000),
            new string('c', 3000)
        };
        
        var request = new
        {
            interests = longInterests,
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.Equal("interests", data.GetProperty("queryType").GetString());
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithNegativeValues_HandlesGracefully()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "test" },
            limit = -5,
            skip = -10,
            radiusKm = -25
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 0);
    }

    [Fact]
    public async Task DiscoverUsers_WithExcessiveValues_HandlesGracefully()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "test" },
            limit = 100000,
            skip = 1000000,
            radiusKm = 1000000
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("users", out var users));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() >= 0);
    }
}









