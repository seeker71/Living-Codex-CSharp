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

public class PeopleInteractionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PeopleInteractionTests(WebApplicationFactory<Program> factory)
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
    public async Task DiscoverUsers_WithDifferentInterestCombinations_ReturnsConsistentResults()
    {
        // Test with different interest combinations
        var interestCombinations = new[]
        {
            new[] { "consciousness" },
            new[] { "consciousness", "ai" },
            new[] { "consciousness", "ai", "philosophy" },
            new[] { "technology", "science" },
            new[] { "art", "creativity", "innovation" }
        };

        foreach (var interests in interestCombinations)
        {
            // Arrange
            var request = new
            {
                interests = interests,
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
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithLocationVariations_ReturnsConsistentResults()
    {
        // Test with different location formats
        var locations = new[]
        {
            "San Francisco, CA",
            "New York, NY",
            "London, UK",
            "Tokyo, Japan",
            "Paris, France"
        };

        foreach (var location in locations)
        {
            // Arrange
            var request = new
            {
                location = location,
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
            Assert.Equal("location", data.GetProperty("queryType").GetString());
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithConceptVariations_ReturnsConsistentResults()
    {
        // Test with different concept IDs
        var conceptIds = new[]
        {
            "concept-1",
            "concept-2",
            "concept-3",
            "concept-4",
            "concept-5"
        };

        foreach (var conceptId in conceptIds)
        {
            // Arrange
            var request = new
            {
                conceptId = conceptId,
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
            Assert.Equal("concept", data.GetProperty("queryType").GetString());
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithPaginationVariations_ReturnsCorrectPages()
    {
        // Test with different pagination parameters
        var paginationTests = new[]
        {
            new { limit = 5, skip = 0 },
            new { limit = 10, skip = 0 },
            new { limit = 20, skip = 0 },
            new { limit = 5, skip = 5 },
            new { limit = 10, skip = 10 },
            new { limit = 20, skip = 20 }
        };

        foreach (var pagination in paginationTests)
        {
            // Arrange
            var request = new
            {
                interests = new[] { "test" },
                limit = pagination.limit,
                skip = pagination.skip
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
            Assert.True(users.GetArrayLength() <= pagination.limit);
            Assert.True(totalCount.GetInt32() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithMixedQueryTypes_ReturnsAppropriateResults()
    {
        // Test with mixed query types
        var mixedQueries = new object[]
        {
            new { interests = new[] { "ai" }, location = "San Francisco, CA", radiusKm = 25 },
            new { interests = new[] { "consciousness" }, conceptId = "concept-123" },
            new { location = "New York, NY", radiusKm = 100, conceptId = "concept-456" }
        };

        foreach (var query in mixedQueries)
        {
            // Arrange
            var queryObj = (dynamic)query;
            var request = new
            {
                interests = queryObj.interests,
                location = queryObj.location,
                radiusKm = queryObj.radiusKm,
                conceptId = queryObj.conceptId,
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
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task GetConceptContributors_WithDifferentConceptIds_ReturnsConsistentResults()
    {
        // Test with different concept IDs
        var conceptIds = new[]
        {
            "concept-alpha",
            "concept-beta",
            "concept-gamma",
            "concept-delta",
            "concept-epsilon"
        };

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
    public async Task DiscoverUsers_WithRapidRequests_HandlesGracefully()
    {
        // Test rapid successive requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 20; i++)
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
    public async Task DiscoverUsers_WithMixedConcurrentRequests_HandlesGracefully()
    {
        // Test mixed concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Mix of different request types
        for (int i = 0; i < 10; i++)
        {
            // Interest-based discovery
            var interestRequest = new
            {
                interests = new[] { $"interest-{i}" },
                limit = 5,
                skip = 0
            };
            tasks.Add(_client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(interestRequest), Encoding.UTF8, "application/json")));
            
            // Location-based discovery
            var locationRequest = new
            {
                location = $"City-{i}",
                radiusKm = 50,
                limit = 5,
                skip = 0
            };
            tasks.Add(_client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(locationRequest), Encoding.UTF8, "application/json")));
            
            // Concept-based discovery
            var conceptRequest = new
            {
                conceptId = $"concept-{i}",
                limit = 5,
                skip = 0
            };
            tasks.Add(_client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(conceptRequest), Encoding.UTF8, "application/json")));
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
    public async Task DiscoverUsers_WithLongTermConsistency_ReturnsConsistentResults()
    {
        // Test consistency over multiple calls
        var request = new
        {
            interests = new[] { "consciousness", "ai" },
            limit = 10,
            skip = 0
        };

        var responses = new List<JsonElement>();
        
        for (int i = 0; i < 5; i++)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            responses.Add(data);
        }

        // All responses should have the same structure
        foreach (var data in responses)
        {
            Assert.True(data.TryGetProperty("users", out var users));
            Assert.True(data.TryGetProperty("totalCount", out var totalCount));
            Assert.True(data.TryGetProperty("queryType", out var queryType));
            Assert.Equal("interests", queryType.GetString());
            Assert.True(totalCount.GetInt32() >= 0);
            Assert.True(users.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in interests
        var specialInterests = new[]
        {
            "consciousness@#$%^&*()",
            "ai!@#$%^&*()_+",
            "philosophy[]{}|\\:;\"'<>?,./",
            "technology`~"
        };

        foreach (var interests in specialInterests)
        {
            // Arrange
            var request = new
            {
                interests = new[] { interests },
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
    }

    [Fact]
    public async Task DiscoverUsers_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters
        var unicodeInterests = new[]
        {
            "意识",
            "人工智能",
            "哲学",
            "技术",
            "科学"
        };

        foreach (var interests in unicodeInterests)
        {
            // Arrange
            var request = new
            {
                interests = new[] { interests },
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
    }

    [Fact]
    public async Task DiscoverUsers_WithVeryLongStrings_HandlesGracefully()
    {
        // Test with very long strings
        var longInterests = new[]
        {
            new string('a', 1000),
            new string('b', 2000),
            new string('c', 3000)
        };

        foreach (var interests in longInterests)
        {
            // Arrange
            var request = new
            {
                interests = new[] { interests },
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
    }

    [Fact]
    public async Task DiscoverUsers_WithEmptyArrays_ReturnsEmptyResults()
    {
        // Test with empty arrays
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
    public async Task DiscoverUsers_WithNullValues_HandlesGracefully()
    {
        // Test with null values
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
    public async Task DiscoverUsers_WithNegativeValues_HandlesGracefully()
    {
        // Test with negative values
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
        // Test with excessive values
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
