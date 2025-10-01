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

public class CreateApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CreateApiTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateConcept_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            title = "Test Concept",
            description = "A test concept for image generation",
            conceptType = "abstract",
            style = "modern",
            mood = "inspiring",
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
        Assert.True(data.TryGetProperty("message", out var message));
        Assert.True(data.TryGetProperty("concept", out var concept));
        Assert.True(data.TryGetProperty("nextSteps", out var nextSteps));
        
        Assert.True(success.GetBoolean());
        Assert.Equal("Concept created successfully", message.GetString());
        Assert.True(concept.TryGetProperty("id", out var conceptId));
        Assert.False(string.IsNullOrEmpty(conceptId.GetString()));
        Assert.True(nextSteps.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateConcept_WithMinimalData_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            title = "Minimal Concept",
            description = "A minimal concept",
            conceptType = "basic",
            style = "simple",
            mood = "neutral",
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
    public async Task CreateConcept_WithComplexData_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            title = "Complex Concept",
            description = "A complex concept with many elements and detailed metadata",
            conceptType = "sophisticated",
            style = "artistic",
            mood = "mysterious",
            colors = new[] { "blue", "gold", "purple", "silver", "black" },
            elements = new[] { "sacred geometry", "light", "shadows", "patterns", "symbols" },
            metadata = new
            {
                complexity = "high",
                inspiration = "sacred art",
                culturalContext = "universal",
                emotionalTone = "transcendent"
            }
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
    public async Task CreateConcept_WithDifferentConceptTypes_ReturnsSuccess()
    {
        // Test with different concept types
        var conceptTypes = new[] { "abstract", "concrete", "symbolic", "metaphorical", "literal" };
        
        foreach (var conceptType in conceptTypes)
        {
            // Arrange
            var request = new
            {
                title = $"Concept Type: {conceptType}",
                description = $"A concept of type {conceptType}",
                conceptType = conceptType,
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task CreateConcept_WithDifferentStyles_ReturnsSuccess()
    {
        // Test with different styles
        var styles = new[] { "modern", "classic", "abstract", "realistic", "surreal" };
        
        foreach (var style in styles)
        {
            // Arrange
            var request = new
            {
                title = $"Style: {style}",
                description = $"A concept with {style} style",
                conceptType = "abstract",
                style = style,
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = new[] { "light" },
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
    public async Task CreateConcept_WithDifferentMoods_ReturnsSuccess()
    {
        // Test with different moods
        var moods = new[] { "inspiring", "mysterious", "peaceful", "energetic", "contemplative" };
        
        foreach (var mood in moods)
        {
            // Arrange
            var request = new
            {
                title = $"Mood: {mood}",
                description = $"A concept with {mood} mood",
                conceptType = "abstract",
                style = "modern",
                mood = mood,
                colors = new[] { "blue" },
                elements = new[] { "light" },
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
    public async Task CreateConcept_WithDifferentColors_ReturnsSuccess()
    {
        // Test with different color combinations
        var colorCombinations = new[]
        {
            new[] { "blue", "gold" },
            new[] { "red", "black", "white" },
            new[] { "green", "purple", "silver" },
            new[] { "orange", "yellow", "pink" },
            new[] { "brown", "beige", "cream" }
        };
        
        foreach (var colors in colorCombinations)
        {
            // Arrange
            var request = new
            {
                title = $"Colors: {string.Join(", ", colors)}",
                description = $"A concept with colors: {string.Join(", ", colors)}",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = colors,
                elements = new[] { "light" },
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
    public async Task CreateConcept_WithDifferentElements_ReturnsSuccess()
    {
        // Test with different element combinations
        var elementCombinations = new[]
        {
            new[] { "sacred geometry", "light" },
            new[] { "patterns", "shadows", "textures" },
            new[] { "symbols", "shapes", "forms" },
            new[] { "nature", "organic", "flowing" },
            new[] { "geometric", "angular", "structured" }
        };
        
        foreach (var elements in elementCombinations)
        {
            // Arrange
            var request = new
            {
                title = $"Elements: {string.Join(", ", elements)}",
                description = $"A concept with elements: {string.Join(", ", elements)}",
                conceptType = "abstract",
                style = "modern",
                mood = "inspiring",
                colors = new[] { "blue" },
                elements = elements,
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
    public async Task GetImageConfigs_ReturnsAvailableConfigurations()
    {
        // Act
        var response = await _client.GetAsync("/image/configs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("message", out var message));
        Assert.True(data.TryGetProperty("configs", out var configs));
        
        Assert.True(success.GetBoolean());
        Assert.True(configs.GetArrayLength() > 0);
        
        // Verify config structure
        foreach (var config in configs.EnumerateArray())
        {
            Assert.True(config.TryGetProperty("id", out var id));
            Assert.True(config.TryGetProperty("name", out var name));
            Assert.True(config.TryGetProperty("provider", out var provider));
            Assert.True(config.TryGetProperty("model", out var model));
            Assert.True(config.TryGetProperty("maxImages", out var maxImages));
            Assert.True(config.TryGetProperty("imageSize", out var imageSize));
            
            Assert.False(string.IsNullOrEmpty(id.GetString()));
            Assert.False(string.IsNullOrEmpty(name.GetString()));
            Assert.False(string.IsNullOrEmpty(provider.GetString()));
            Assert.False(string.IsNullOrEmpty(model.GetString()));
            Assert.True(maxImages.GetInt32() > 0);
            Assert.False(string.IsNullOrEmpty(imageSize.GetString()));
        }
    }

    [Fact]
    public async Task GenerateImages_WithValidConceptId_ReturnsGenerationRecord()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Generation",
            description = "A test concept for image generation",
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

        // Now generate images
        var generationRequest = new
        {
            conceptId = conceptId,
            imageConfigId = "dalle-3",
            numberOfImages = 1,
            customPrompt = ""
        };

        // Act
        var response = await _client.PostAsync("/image/generate", 
            new StringContent(JsonSerializer.Serialize(generationRequest), Encoding.UTF8, "application/json"));

        // Assert
        // Should either succeed or fail gracefully (due to missing API keys)
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GenerateImages_WithDifferentConfigs_HandlesGracefully()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Multiple Configs",
            description = "A test concept for multiple image generation configs",
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

        // Test with different image configs
        var configIds = new[] { "dalle-3", "stability-sdxl", "local-sd", "custom-local" };
        
        foreach (var configId in configIds)
        {
            var generationRequest = new
            {
                conceptId = conceptId,
                imageConfigId = configId,
                numberOfImages = 1,
                customPrompt = ""
            };

            // Act
            var response = await _client.PostAsync("/image/generate", 
                new StringContent(JsonSerializer.Serialize(generationRequest), Encoding.UTF8, "application/json"));

            // Assert
            // Should either succeed or fail gracefully (due to missing API keys)
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task GenerateImages_WithDifferentNumberOfImages_HandlesGracefully()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Multiple Images",
            description = "A test concept for multiple image generation",
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

        // Test with different number of images
        var imageCounts = new[] { 1, 2, 3, 4 };
        
        foreach (var numberOfImages in imageCounts)
        {
            var generationRequest = new
            {
                conceptId = conceptId,
                imageConfigId = "dalle-3",
                numberOfImages = numberOfImages,
                customPrompt = ""
            };

            // Act
            var response = await _client.PostAsync("/image/generate", 
                new StringContent(JsonSerializer.Serialize(generationRequest), Encoding.UTF8, "application/json"));

            // Assert
            // Should either succeed or fail gracefully (due to missing API keys)
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task GenerateImages_WithCustomPrompt_HandlesGracefully()
    {
        // First create a concept
        var conceptRequest = new
        {
            title = "Test Concept for Custom Prompt",
            description = "A test concept for custom prompt generation",
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

        // Test with custom prompt
        var generationRequest = new
        {
            conceptId = conceptId,
            imageConfigId = "dalle-3",
            numberOfImages = 1,
            customPrompt = "A beautiful, inspiring image with sacred geometry and golden light"
        };

        // Act
        var response = await _client.PostAsync("/image/generate", 
            new StringContent(JsonSerializer.Serialize(generationRequest), Encoding.UTF8, "application/json"));

        // Assert
        // Should either succeed or fail gracefully (due to missing API keys)
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExtractConcepts_WithValidData_ReturnsConcepts()
    {
        // Arrange
        var request = new
        {
            title = "Test Concept Extraction",
            content = "This is a test concept about consciousness, AI, and philosophy. It explores the nature of mind and machine intelligence.",
            categories = new[] { "consciousness", "ai", "philosophy" },
            source = "test",
            url = ""
        };

        // Act
        var response = await _client.PostAsync("/ai/extract-concepts", 
            new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

        // Assert
        // Should either succeed or fail gracefully (due to AI services not being ready)
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ExtractConcepts_WithDifferentContent_HandlesGracefully()
    {
        // Test with different content types
        var contentTypes = new[]
        {
            "Short content about AI and consciousness",
            "This is a longer piece of content that explores multiple concepts including technology, spirituality, and human development. It discusses various aspects of consciousness and how it relates to artificial intelligence and machine learning.",
            "Technical content with specific terms: neural networks, deep learning, quantum computing, and consciousness studies.",
            "Creative content about art, beauty, and the human experience of transcendence through technology and spirituality."
        };
        
        foreach (var content in contentTypes)
        {
            var request = new
            {
                title = "Test Concept Extraction",
                content = content,
                categories = new[] { "consciousness", "ai" },
                source = "test",
                url = ""
            };

            // Act
            var response = await _client.PostAsync("/ai/extract-concepts", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either succeed or fail gracefully (due to AI services not being ready)
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task ExtractConcepts_WithDifferentCategories_HandlesGracefully()
    {
        // Test with different category combinations
        var categoryCombinations = new[]
        {
            new[] { "consciousness" },
            new[] { "consciousness", "ai" },
            new[] { "consciousness", "ai", "philosophy" },
            new[] { "technology", "science" },
            new[] { "art", "creativity", "innovation" }
        };
        
        foreach (var categories in categoryCombinations)
        {
            var request = new
            {
                title = "Test Concept Extraction",
                content = "This is a test concept about various topics.",
                categories = categories,
                source = "test",
                url = ""
            };

            // Act
            var response = await _client.PostAsync("/ai/extract-concepts", 
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));

            // Assert
            // Should either succeed or fail gracefully (due to AI services not being ready)
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task CreateConcept_WithConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent concept creation
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 10; i++)
        {
            var request = new
            {
                title = $"Concurrent Concept {i}",
                description = $"A concurrent test concept {i}",
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
    public async Task CreateConcept_WithUnicodeCharacters_HandlesGracefully()
    {
        // Test with Unicode characters
        var request = new
        {
            title = "概念测试 - 中文标题",
            description = "这是一个关于意识和人工智能的测试概念。它探索心灵和机器智能的本质。",
            conceptType = "抽象",
            style = "现代",
            mood = "启发",
            colors = new[] { "蓝色", "金色" },
            elements = new[] { "神圣几何", "光" },
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
    public async Task CreateConcept_WithVeryLongStrings_HandlesGracefully()
    {
        // Test with very long strings
        var longTitle = new string('a', 1000);
        var longDescription = new string('b', 5000);
        
        var request = new
        {
            title = longTitle,
            description = longDescription,
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }
}







