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

public class CreateInteractionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateInteractionTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateConcept_WithDifferentComplexityLevels_ReturnsSuccess()
    {
        // Test with different complexity levels
        var complexityLevels = new[]
        {
            new { title = "Simple Concept", description = "A simple concept", conceptType = "basic", style = "simple", mood = "neutral" },
            new { title = "Moderate Concept", description = "A moderately complex concept with some details", conceptType = "intermediate", style = "modern", mood = "inspiring" },
            new { title = "Complex Concept", description = "A highly complex concept with many intricate details and sophisticated elements", conceptType = "advanced", style = "sophisticated", mood = "mysterious" }
        };
        
        foreach (var level in complexityLevels)
        {
            var request = new
            {
                title = level.title,
                description = level.description,
                conceptType = level.conceptType,
                style = level.style,
                mood = level.mood,
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { complexity = level.conceptType }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task CreateConcept_WithDifferentMetadataStructures_ReturnsSuccess()
    {
        // Test with different metadata structures
        var metadataStructures = new object[]
        {
            new { },
            new { author = "test-user", version = "1.0" },
            new { tags = new[] { "consciousness", "ai" }, priority = "high", category = "philosophy" },
            new { 
                culturalContext = "universal", 
                emotionalTone = "transcendent", 
                inspiration = "sacred art",
                technicalDetails = new { complexity = "high", abstraction = "medium" }
            }
        };
        
        foreach (var metadata in metadataStructures)
        {
            var request = new
            {
                title = "Metadata Test Concept",
                description = "A concept for testing different metadata structures",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = metadata
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task CreateConcept_WithRapidSuccession_HandlesGracefully()
    {
        // Test rapid successive concept creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 20; i++)
        {
            var request = new
            {
                title = $"Rapid Concept {i}",
                description = $"A rapidly created concept {i}",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };
            
            tasks.Add(_client.PostAsync("/image/concept/create", 
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
    public async Task CreateConcept_WithMixedConcurrentRequests_HandlesGracefully()
    {
        // Test mixed concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Mix of different request types
        for (int i = 0; i < 5; i++)
        {
            // Concept creation
            var conceptRequest = new
            {
                title = $"Mixed Concept {i}",
                description = $"A mixed test concept {i}",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
                metadata = new { }
            };
            tasks.Add(_client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(conceptRequest), Encoding.UTF8, "application/json")));
            
            // Image config retrieval
            tasks.Add(_client.GetAsync("/image/configs"));
            
            // Concept extraction
            var extractionRequest = new
            {
                title = $"Extraction Test {i}",
                content = $"Test content for extraction {i}",
                categories = new[] { "consciousness", "ai" },
                source = "test",
                url = ""
            };
            tasks.Add(_client.PostAsync("/ai/extract-concepts", 
                new StringContent(JsonSerializer.Serialize(extractionRequest), Encoding.UTF8, "application/json")));
        }

        // Wait for all requests to complete
        var responses = await Task.WhenAll(tasks);

        // Assert all requests succeeded or failed gracefully
        foreach (var response in responses)
        {
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task CreateConcept_WithLongTermConsistency_ReturnsConsistentResults()
    {
        // Test consistency over multiple calls
        var request = new
        {
            title = "Consistency Test Concept",
            description = "A concept for testing consistency across multiple calls",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue", "gold" },
            elements = new[] { "sacred geometry", "light" },
            metadata = new { }
        };

        var responses = new List<JsonElement>();
        
        for (int i = 0; i < 5; i++)
        {
            // Act
            var response = await _client.PostAsync("/image/concept/create", 
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
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("message", out var message));
            Assert.True(data.TryGetProperty("concept", out var concept));
            Assert.True(data.TryGetProperty("nextSteps", out var nextSteps));
            Assert.True(success.GetBoolean());
            Assert.Equal("Concept created successfully", message.GetString());
            Assert.True(concept.TryGetProperty("id", out var conceptId));
            Assert.False(string.IsNullOrEmpty(conceptId.GetString()));
            Assert.True(nextSteps.GetArrayLength() > 0);
        }
    }

    [Fact]
    public async Task CreateConcept_WithSpecialCharacters_HandlesGracefully()
    {
        // Test with special characters in various fields
        var specialCharTests = new[]
        {
            new { title = "Concept@#$%^&*()", description = "Description!@#$%^&*()_+", conceptType = "abstract", style = "modern", mood = "inspiring" },
            new { title = "Concept[]{}|\\:;\"'<>?,./", description = "Description`~", conceptType = "abstract", style = "modern", mood = "inspiring" },
            new { title = "Concept\t\n\r", description = "Description\b\f\v", conceptType = "abstract", style = "modern", mood = "inspiring" }
        };
        
        foreach (var test in specialCharTests)
        {
            var request = new
            {
                title = test.title,
                description = test.description,
                conceptType = test.conceptType,
                style = test.style,
                mood = test.mood,
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task CreateConcept_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters in various fields
        var unicodeTests = new[]
        {
            new { title = "概念测试", description = "这是一个测试概念", conceptType = "抽象", style = "现代", mood = "启发" },
            new { title = "コンセプトテスト", description = "これはテストコンセプトです", conceptType = "抽象", style = "現代", mood = "インスピレーション" },
            new { title = "مفهوم الاختبار", description = "هذا مفهوم اختبار", conceptType = "مجرد", style = "حديث", mood = "ملهم" }
        };
        
        foreach (var test in unicodeTests)
        {
            var request = new
            {
                title = test.title,
                description = test.description,
                conceptType = test.conceptType,
                style = test.style,
                mood = test.mood,
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task CreateConcept_WithVeryLongArrays_HandlesGracefully()
    {
        // Test with very long arrays
        var longColors = new string[100];
        var longElements = new string[100];
        
        for (int i = 0; i < 100; i++)
        {
            longColors[i] = $"color-{i}";
            longElements[i] = $"element-{i}";
        }
        
        var request = new
        {
            title = "Long Arrays Concept",
            description = "A concept with very long arrays",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = longColors,
            elements = longElements,
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task CreateConcept_WithEmptyArrays_HandlesGracefully()
    {
        // Test with empty arrays
        var request = new
        {
            title = "Empty Arrays Concept",
            description = "A concept with empty arrays",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new string[0],
            elements = new string[0],
            metadata = new { }
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task CreateConcept_WithNullValues_HandlesGracefully()
    {
        // Test with null values
        var request = new
        {
            title = "Null Values Concept",
            description = "A concept with null values",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = (string[])null,
            elements = (string[])null,
            metadata = (object)null
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task CreateConcept_WithComplexNestedMetadata_HandlesGracefully()
    {
        // Test with complex nested metadata
        var complexMetadata = new
        {
            author = new
            {
                name = "Test User",
                email = "test@example.com",
                preferences = new
                {
                    theme = "dark",
                    language = "en",
                    notifications = true
                }
            },
            technical = new
            {
                version = "1.0.0",
                dependencies = new[] { "ai-module", "image-module" },
                configuration = new
                {
                    maxImages = 4,
                    quality = "high",
                    style = "vivid"
                }
            },
            cultural = new
            {
                context = "universal",
                language = "en",
                region = "global"
            }
        };
        
        var request = new
        {
            title = "Complex Metadata Concept",
            description = "A concept with complex nested metadata",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
            colors = new[] { "blue", "gold" },
            elements = new[] { "sacred geometry", "light" },
            metadata = complexMetadata
        };

        // Act
        var response = await _client.PostAsync("/image/concept/create", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task CreateConcept_WithDifferentContentTypes_HandlesGracefully()
    {
        // Test with different content types
        var contentTypes = new[]
        {
            "text/plain",
            "application/json",
            "application/xml",
            "text/html"
        };
        
        foreach (var contentType in contentTypes)
        {
            var request = new
            {
                title = "Content Type Test",
                description = "A concept for testing different content types",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { contentType = contentType }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, contentType));

            // Assert
            // Should either succeed or fail gracefully based on content type
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithDifferentEncodings_HandlesGracefully()
    {
        // Test with different encodings
        var encodings = new[]
        {
            Encoding.UTF8,
            Encoding.UTF32,
            Encoding.Unicode,
            Encoding.ASCII
        };
        
        foreach (var encoding in encodings)
        {
            var request = new
            {
                title = "Encoding Test",
                description = "A concept for testing different encodings",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { encoding = encoding.EncodingName }
            };

            // Act
            var response = await _client.PostAsync("/image/concept/create", 
                new StringContent(JsonSerializer.Serialize(request), encoding, "application/json"));

            // Assert
            // Should either succeed or fail gracefully based on encoding
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task CreateConcept_WithDifferentHttpMethods_HandlesGracefully()
    {
        // Test with different HTTP methods
        var methods = new[]
        {
            HttpMethod.Get,
            HttpMethod.Put,
            HttpMethod.Delete,
            HttpMethod.Patch
        };
        
        foreach (var method in methods)
        {
            var request = new
            {
                title = "HTTP Method Test",
                description = "A concept for testing different HTTP methods",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue", "gold" },
                elements = new[] { "sacred geometry", "light" },
                metadata = new { method = method.Method }
            };

            // Act
            var response = await _client.SendAsync(new HttpRequestMessage(method, "/image/concept/create")
            {
                Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            });

            // Assert
            // Should either succeed or fail gracefully based on method
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }
}
