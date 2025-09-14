using Xunit;
using FluentAssertions;
using System.Text.Json;

namespace CodexBootstrap.Tests;

public class ApiEndpointTests : TestBase
{
    public ApiEndpointTests() : base() { }

    [Fact]
    public async Task HelloModule_ShouldHaveWorkingEndpoints()
    {
        // Test echo endpoint
        var echoResponse = await PostJsonAsync<dynamic>("/hello/echo", new { test = "data" });
        echoResponse.Should().NotBeNull();

        // Test greet endpoint
        var greetResponse = await PostJsonAsync<dynamic>("/hello/greet", new { name = "TestUser" });
        greetResponse.Should().NotBeNull();
        var greetString = greetResponse.ToString();
        greetString.Should().Contain("Hello, TestUser");

        // Test hot-reload test endpoint
        var hotReloadResponse = await GetJsonAsync<dynamic>("/hello/hot-reload-test");
        hotReloadResponse.Should().NotBeNull();
        var hotReloadString = hotReloadResponse.ToString();
        hotReloadString.Should().Contain("Hot-reload test endpoint");
    }

    [Fact]
    public async Task AIModule_ShouldHaveHealthEndpoint()
    {
        // Act
        var aiHealth = await GetJsonAsync<dynamic>("/ai/health");

        // Assert
        aiHealth.Should().NotBeNull();
    }

    [Fact]
    public async Task SpecModule_ShouldHaveModulesEndpoint()
    {
        // Act
        var modules = await GetJsonAsync<dynamic>("/spec/modules");

        // Assert
        modules.Should().NotBeNull();
    }

    [Fact]
    public async Task SpecModule_ShouldHaveRoutesEndpoint()
    {
        // Act
        var routes = await GetJsonAsync<dynamic>("/spec/routes/all");

        // Assert
        routes.Should().NotBeNull();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/ai/health")]
    [InlineData("/spec/modules")]
    [InlineData("/spec/routes/all")]
    [InlineData("/hello/hot-reload-test")]
    public async Task CriticalEndpoints_ShouldBeAccessible(string endpoint)
    {
        // Act & Assert
        await AssertEndpointExists(endpoint);
    }

    [Fact]
    public async Task AIModule_ShouldSupportConceptExtraction()
    {
        // Arrange
        var request = new
        {
            content = "This is a test article about artificial intelligence and machine learning.",
            provider = "ollama",
            model = "llama3.1:8b"
        };

        // Act
        var response = await PostJsonAsync<dynamic>("/ai/extract-concepts", request);

        // Assert
        response.Should().NotBeNull();
        var responseString = response.ToString();
        responseString.Should().Contain("success");
    }

    [Fact]
    public async Task AIModule_ShouldSupportScoringAnalysis()
    {
        // Arrange
        var request = new
        {
            content = "This is a test article about artificial intelligence and machine learning.",
            provider = "ollama",
            model = "llama3.1:8b"
        };

        // Act
        var response = await PostJsonAsync<dynamic>("/ai/score-analysis", request);

        // Assert
        response.Should().NotBeNull();
        var responseString = response.ToString();
        responseString.Should().Contain("success");
    }

    [Fact]
    public async Task AIModule_ShouldSupportFractalTransformation()
    {
        // Arrange
        var request = new
        {
            content = "This is a test article about artificial intelligence and machine learning.",
            provider = "ollama",
            model = "llama3.1:8b"
        };

        // Act
        var response = await PostJsonAsync<dynamic>("/ai/fractal-transform", request);

        // Assert
        response.Should().NotBeNull();
        var responseString = response.ToString();
        responseString.Should().Contain("success");
    }
}
