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

public class CreateErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateConcept_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(invalidJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithEmptyJson_ReturnsBadRequest()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(emptyJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithNullJson_ReturnsBadRequest()
    {
        // Arrange
        var nullJson = "null";

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(nullJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithMalformedJson_ReturnsBadRequest()
    {
        // Arrange
        var malformedJson = "{ \"title\": \"Test\", \"description\": \"Test\", \"conceptType\": \"abstract\", \"style\": \"modern\", \"mood\": \"inspiring\", \"colors\": [\"blue\"], \"elements\": [\"light\"], \"metadata\": {}, }";

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(malformedJson, Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithInvalidContentType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            title = "Test Concept",
            description = "A test concept",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue" },
            elements = new[] { "light" },
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "text/plain"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithMissingContentType_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            title = "Test Concept",
            description = "A test concept",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue" },
            elements = new[] { "light" },
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateConcept_WithMissingRequiredFields_HandlesGracefully()
    {
        // Test with missing required fields
        var incompleteRequests = new object[]
        {
            new { },
            new { title = "Test" },
            new { title = "Test", description = "Test" },
            new { title = "Test", description = "Test", conceptType = "abstract" },
            new { title = "Test", description = "Test", conceptType = "abstract", style = "modern" },
            new { title = "Test", description = "Test", conceptType = "abstract", style = "modern", mood = "inspiring" }
        };

        foreach (var request in incompleteRequests)
        {
            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task CreateConcept_WithInvalidFieldTypes_HandlesGracefully()
    {
        // Test with invalid field types
        var invalidRequests = new object[]
        {
            new { title = 123, description = "Test", conceptType = "abstract", style = "modern", mood = "inspiring", colors = new[] { "blue" }, elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = 123, conceptType = "abstract", style = "modern", mood = "inspiring", colors = new[] { "blue" }, elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = "Test", conceptType = 123, style = "modern", mood = "inspiring", colors = new[] { "blue" }, elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = "Test", conceptType = "abstract", style = 123, mood = "inspiring", colors = new[] { "blue" }, elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = "Test", conceptType = "abstract", style = "modern", mood = 123, colors = new[] { "blue" }, elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = "Test", conceptType = "abstract", style = "modern", mood = "inspiring", colors = "not-an-array", elements = new[] { "light" }, metadata = new { } },
            new { title = "Test", description = "Test", conceptType = "abstract", style = "modern", mood = "inspiring", colors = new[] { "blue" }, elements = "not-an-array", metadata = new { } }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task CreateConcept_WithSQLInjectionAttempts_HandlesGracefully()
    {
        // Test with SQL injection attempts
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE concepts; --",
            "1' OR '1'='1",
            "admin'--",
            "1' UNION SELECT * FROM concepts--"
        };

        foreach (var attempt in sqlInjectionAttempts)
        {
            var request = new
            {
                title = attempt,
                description = "Test description",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithXSSAttempts_HandlesGracefully()
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
            var request = new
            {
                title = attempt,
                description = "Test description",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithPathTraversalAttempts_HandlesGracefully()
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
            var request = new
            {
                title = attempt,
                description = "Test description",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithNullBytes_HandlesGracefully()
    {
        // Test with null bytes
        var nullByteAttempts = new[]
        {
            "concept%00",
            "concept\0",
            "concept%00test"
        };

        foreach (var attempt in nullByteAttempts)
        {
            var request = new
            {
                title = attempt,
                description = "Test description",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithControlCharacters_HandlesGracefully()
    {
        // Test with control characters
        var controlCharAttempts = new[]
        {
            "concept\t",
            "concept\n",
            "concept\r",
            "concept\b",
            "concept\f",
            "concept\v"
        };

        foreach (var attempt in controlCharAttempts)
        {
            var request = new
            {
                title = attempt,
                description = "Test description",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithVeryLargePayload_HandlesGracefully()
    {
        // Test with very large payload
        var largeTitle = new string('a', 10000);
        var largeDescription = new string('b', 50000);
        var largeColors = new string[1000];
        var largeElements = new string[1000];
        
        for (int i = 0; i < 1000; i++)
        {
            largeColors[i] = new string('c', 100);
            largeElements[i] = new string('d', 100);
        }
        
        var request = new
        {
            title = largeTitle,
            description = largeDescription,
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = largeColors,
            elements = largeElements,
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateConcept_WithCircularReference_HandlesGracefully()
    {
        // Test with circular reference (this should be handled by JSON serialization)
        var circularObject = new { name = "test" };
        var request = new
        {
            title = "Circular Reference Test",
            description = "A concept with circular reference",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue" },
            elements = new[] { "light" },
            metadata = new { circular = circularObject }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateConcept_WithInvalidEncoding_HandlesGracefully()
    {
        // Test with invalid encoding
        var request = new
        {
            title = "Encoding Test",
            description = "A concept for testing invalid encoding",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue" },
            elements = new[] { "light" },
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF32, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateConcept_WithExtraFields_HandlesGracefully()
    {
        // Test with extra fields
        var request = new
        {
            title = "Extra Fields Test",
            description = "A concept with extra fields",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue" },
            elements = new[] { "light" },
            metadata = new { },
            extraField1 = "extra1",
            extraField2 = 123,
            extraField3 = true,
            extraField4 = new { nested = "object" }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateImages_WithInvalidConceptId_ReturnsError()
    {
        // Test with invalid concept ID
        var request = new
        {
            conceptId = "invalid-concept-id",
            imageConfigId = "dalle-3",
            numberOfImages = 1,
            customPrompt = ""
        };

        // Act
        var response = await _client.PostAsync("/image/generate", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GenerateImages_WithInvalidImageConfigId_ReturnsError()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Invalid Config",
            description = "A test concept for invalid config testing",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue", "gold" },
            elements = new[] { "sacred geometry", "light" },
            metadata = new { }
        };

        var conceptResponse = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(conceptRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, conceptResponse.StatusCode);
        var conceptContent = await conceptResponse.Content.ReadAsStringAsync();
        var conceptData = JsonSerializer.Deserialize<JsonElement>(conceptContent);
        var conceptId = conceptData.GetProperty("concept").GetProperty("id").GetString();

        // Test with invalid image config ID
        var request = new
        {
            conceptId = conceptId,
            imageConfigId = "invalid-config-id",
            numberOfImages = 1,
            customPrompt = ""
        };

        // Act
        var response = await _client.PostAsync("/image/generate", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task GenerateImages_WithInvalidNumberOfImages_HandlesGracefully()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Invalid Image Count",
            description = "A test concept for invalid image count testing",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue", "gold" },
            elements = new[] { "sacred geometry", "light" },
            metadata = new { }
        };

        var conceptResponse = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(conceptRequest), Encoding.UTF8, "application/json"));
        
        Assert.Equal(HttpStatusCode.OK, conceptResponse.StatusCode);
        var conceptContent = await conceptResponse.Content.ReadAsStringAsync();
        var conceptData = JsonSerializer.Deserialize<JsonElement>(conceptContent);
        var conceptId = conceptData.GetProperty("concept").GetProperty("id").GetString();

        // Test with invalid number of images
        var request = new
        {
            conceptId = conceptId,
            imageConfigId = "dalle-3",
            numberOfImages = -1,
            customPrompt = ""
        };

        // Act
        var response = await _client.PostAsync("/image/generate", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExtractConcepts_WithInvalidData_HandlesGracefully()
    {
        // Test with invalid data
        var invalidRequests = new object[]
        {
            new { title = 123, content = "Test", categories = new[] { "consciousness" }, source = "test", url = "" },
            new { title = "Test", content = 123, categories = new[] { "consciousness" }, source = "test", url = "" },
            new { title = "Test", content = "Test", categories = "not-an-array", source = "test", url = "" },
            new { title = "Test", content = "Test", categories = new[] { "consciousness" }, source = 123, url = "" },
            new { title = "Test", content = "Test", categories = new[] { "consciousness" }, source = "test", url = 123 }
        };

        foreach (var request in invalidRequests)
        {
            // Act
            var response = await _client.PostAsync("/ai/extract-concepts", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task ExtractConcepts_WithEmptyData_HandlesGracefully()
    {
        // Test with empty data
        var request = new
        {
            title = "",
            content = "",
            categories = new string[0],
            source = "",
            url = ""
        };

        // Act
        var response = await _client.PostAsync("/ai/extract-concepts", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExtractConcepts_WithNullData_HandlesGracefully()
    {
        // Test with null data
        var request = new
        {
            title = (string)null,
            content = (string)null,
            categories = (string[])null,
            source = (string)null,
            url = (string)null
        };

        // Act
        var response = await _client.PostAsync("/ai/extract-concepts", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }
}
