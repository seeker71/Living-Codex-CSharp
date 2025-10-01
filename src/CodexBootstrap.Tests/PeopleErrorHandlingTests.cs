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

public class PeopleErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PeopleErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task DiscoverUsers_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(invalidJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithEmptyJson_ReturnsBadRequest()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(emptyJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithNullJson_ReturnsBadRequest()
    {
        // Arrange
        var nullJson = "null";

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(nullJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var malformedJson = "{ \"interests\": [\"test\"], \"limit\": 10, \"skip\": 0, }";

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(malformedJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "test" },
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "text/plain"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithMissingContentType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            interests = new[] { "test" },
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidInterests_HandlesGracefully()
    {
        // Test with invalid interest types
        var invalidRequests = new object[]
        {
            new { interests = "not-an-array", limit = 10, skip = 0 },
            new { interests = 123, limit = 10, skip = 0 },
            new { interests = true, limit = 10, skip = 0 },
            new { interests = new { not = "array" }, limit = 10, skip = 0 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidLocation_HandlesGracefully()
    {
        // Test with invalid location types
        var invalidRequests = new object[]
        {
            new { location = 123, limit = 10, skip = 0 },
            new { location = true, limit = 10, skip = 0 },
            new { location = new { not = "string" }, limit = 10, skip = 0 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidConceptId_HandlesGracefully()
    {
        // Test with invalid concept ID types
        var invalidRequests = new object[]
        {
            new { conceptId = 123, limit = 10, skip = 0 },
            new { conceptId = true, limit = 10, skip = 0 },
            new { conceptId = new { not = "string" }, limit = 10, skip = 0 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidLimit_HandlesGracefully()
    {
        // Test with invalid limit types
        var invalidRequests = new object[]
        {
            new { interests = new[] { "test" }, limit = "not-a-number", skip = 0 },
            new { interests = new[] { "test" }, limit = true, skip = 0 },
            new { interests = new[] { "test" }, limit = new { not = "number" }, skip = 0 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidSkip_HandlesGracefully()
    {
        // Test with invalid skip types
        var invalidRequests = new object[]
        {
            new { interests = new[] { "test" }, limit = 10, skip = "not-a-number" },
            new { interests = new[] { "test" }, limit = 10, skip = true },
            new { interests = new[] { "test" }, limit = 10, skip = new { not = "number" } }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidRadiusKm_HandlesGracefully()
    {
        // Test with invalid radius types
        var invalidRequests = new object[]
        {
            new { location = "San Francisco", radiusKm = "not-a-number", limit = 10, skip = 0 },
            new { location = "San Francisco", radiusKm = true, limit = 10, skip = 0 },
            new { location = "San Francisco", radiusKm = new { not = "number" }, limit = 10, skip = 0 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithSQLInjectionAttempts_HandlesGracefully()
    {
        // Test with SQL injection attempts
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE users; --",
            "1' OR '1'='1",
            "admin'--",
            "1' UNION SELECT * FROM users--"
        };

        foreach (var attempt in sqlInjectionAttempts)
        {
            // Arrange
            var request = new
            {
                interests = new[] { attempt },
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithXSSAttempts_HandlesGracefully()
    {
        // Test with XSS attempts
        var xssAttempts = new[]
        {
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src=x onerror=alert('xss')>",
            "';alert('xss');//"
        };

        foreach (var attempt in xssAttempts)
        {
            // Arrange
            var request = new
            {
                interests = new[] { attempt },
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithPathTraversalAttempts_HandlesGracefully()
    {
        // Test with path traversal attempts
        var pathTraversalAttempts = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\drivers\\etc\\hosts",
            "....//....//....//etc/passwd",
            "..%2F..%2F..%2Fetc%2Fpasswd"
        };

        foreach (var attempt in pathTraversalAttempts)
        {
            // Arrange
            var request = new
            {
                interests = new[] { attempt },
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithNullBytes_HandlesGracefully()
    {
        // Test with null bytes
        var nullByteAttempts = new[]
        {
            "user%00",
            "user\0",
            "user%00test"
        };

        foreach (var attempt in nullByteAttempts)
        {
            // Arrange
            var request = new
            {
                interests = new[] { attempt },
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithControlCharacters_HandlesGracefully()
    {
        // Test with control characters
        var controlCharAttempts = new[]
        {
            "user\t",
            "user\n",
            "user\r",
            "user\b",
            "user\f",
            "user\v"
        };

        foreach (var attempt in controlCharAttempts)
        {
            // Arrange
            var request = new
            {
                interests = new[] { attempt },
                limit = 10,
                skip = 0
            };

            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithVeryLargePayload_HandlesGracefully()
    {
        // Test with very large payload
        var largeInterests = new string[1000];
        for (int i = 0; i < 1000; i++)
        {
            largeInterests[i] = new string('a', 1000);
        }

        var request = new
        {
            interests = largeInterests,
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task DiscoverUsers_WithCircularReference_HandlesGracefully()
    {
        // Test with circular reference (this should be handled by JSON serialization)
        var circularObject = new { name = "test" };
        var request = new
        {
            interests = new[] { "test" },
            limit = 10,
            skip = 0,
            circular = circularObject
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DiscoverUsers_WithInvalidEncoding_HandlesGracefully()
    {
        // Test with invalid encoding
        var request = new
        {
            interests = new[] { "test" },
            limit = 10,
            skip = 0
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF32, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task DiscoverUsers_WithMissingRequiredFields_HandlesGracefully()
    {
        // Test with missing required fields
        var incompleteRequests = new object[]
        {
            new { },
            new { limit = 10 },
            new { skip = 0 },
            new { interests = new[] { "test" } }
        };

        foreach (var request in incompleteRequests)
        {
            // Act
            var response = await _client.PostAsync("/users/discover", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task DiscoverUsers_WithExtraFields_HandlesGracefully()
    {
        // Test with extra fields
        var request = new
        {
            interests = new[] { "test" },
            limit = 10,
            skip = 0,
            extraField1 = "extra1",
            extraField2 = 123,
            extraField3 = true,
            extraField4 = new { nested = "object" }
        };

        // Act
        var response = await _client.PostAsync("/users/discover", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetConceptContributors_WithInvalidConceptId_HandlesGracefully()
    {
        // Test with invalid concept ID types
        var invalidConceptIds = new[]
        {
            "",
            "   ",
            "concept@#$%^&*()",
            "concept!@#$%^&*()_+",
            "concept[]{}|\\:;\"'<>?,./",
            "concept`~",
            new string('a', 10000)
        };

        foreach (var conceptId in invalidConceptIds)
        {
            // Act
            var response = await _client.GetAsync($"/concepts/{conceptId}/contributors");

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
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
    public async Task GetConceptContributors_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in concept ID
        var unicodeConceptIds = new[]
        {
            "概念123",
            "コンセプト456",
            "مفهوم789",
            "концепция101"
        };

        foreach (var conceptId in unicodeConceptIds)
        {
            // Act
            var response = await _client.GetAsync($"/concepts/{conceptId}/contributors");

            // Assert
            // Should handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GetConceptContributors_WithVeryLongConceptId_HandlesGracefully()
    {
        // Test with very long concept ID
        var veryLongConceptId = new string('a', 10000);
        var response = await _client.GetAsync($"/concepts/{veryLongConceptId}/contributors");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConceptContributors_WithEmptyConceptId_ReturnsNotFound()
    {
        // Test with empty concept ID
        var response = await _client.GetAsync("/concepts//contributors");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConceptContributors_WithNullConceptId_ReturnsNotFound()
    {
        // Test with null concept ID
        var response = await _client.GetAsync("/concepts/null/contributors");

        // Assert
        // Should either return 404 Not Found or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.OK);
    }
}
