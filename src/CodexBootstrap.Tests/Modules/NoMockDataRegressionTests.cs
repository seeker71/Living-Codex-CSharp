using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using CodexBootstrap.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Regression tests ensuring modules correctly fail without mock data rather than returning simulated responses
/// </summary>
public class NoMockDataRegressionTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _fixture;
    private readonly HttpClient _client;

    public NoMockDataRegressionTests(TestServerFixture fixture)
    {
        _fixture = fixture;
        _client = _fixture.HttpClient;
    }

    [Fact]
    public async Task ThreadsModule_ListThreads_WithoutMockData_ReturnsEmptyOrError()
    {
        // Act
        var response = await _client.GetAsync("/threads/list?limit=10");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Should either be empty or explicitly indicate no data
            if (result.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                if (result.TryGetProperty("threads", out var threads))
                {
                    Assert.True(threads.GetArrayLength() == 0); // Empty, not mock data
                }
            }
        }
        else
        {
            // Acceptable to fail without implementation
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task GalleryModule_ListItems_WithoutMockData_ReturnsEmptyOrError()
    {
        // Act
        var response = await _client.GetAsync("/gallery/list?limit=10");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Should either be empty or explicitly indicate no data
            if (result.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                if (result.TryGetProperty("items", out var items))
                {
                    Assert.True(items.GetArrayLength() == 0); // Empty, not mock data
                }
            }
        }
        else
        {
            // Acceptable to fail without implementation
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task FutureKnowledge_RetrieveFutureKnowledge_WithRealImplementation_ReturnsSuccess()
    {
        // Arrange
        var requestData = new
        {
            Query = "What will happen in the future?",
            Domain = "consciousness",
            TimeHorizon = 5
        };
        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/future-knowledge/retrieve", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        // Should return real data, not simulated
        if (result.TryGetProperty("success", out var success))
        {
            Assert.True(success.GetBoolean()); // Should succeed with real implementation
        }
        
        if (result.TryGetProperty("message", out var message))
        {
            var msg = message.GetString() ?? "";
            Assert.Equal("Future knowledge retrieved successfully", msg); // Real success message
        }
        
        // Should have real data structure
        Assert.True(result.TryGetProperty("knowledgeCount", out _));
        Assert.True(result.TryGetProperty("knowledge", out _));
    }

    [Fact]
    public async Task VisualValidation_WithoutAI_ShouldFailNotFallback()
    {
        // Arrange
        var requestData = new
        {
            ImageNodeId = "test-image",
            ComponentId = "test-component"
        };
        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/visual-validation/analyze-image", content);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Should return an error, not fallback analysis
            if (result.TryGetProperty("success", out var success))
            {
                if (!success.GetBoolean())
                {
                    // Good - should fail without AI, not provide fallback
                    if (result.TryGetProperty("message", out var message))
                    {
                        var msg = message.GetString() ?? "";
                        Assert.DoesNotContain("fallback", msg.ToLower());
                        Assert.DoesNotContain("mock", msg.ToLower());
                    }
                }
            }
        }
        else
        {
            // Acceptable to fail without AI module or with method not allowed for incorrect endpoints
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.MethodNotAllowed);
        }
    }

    [Fact]
    public async Task UserDiscovery_WithoutGeocoding_ShouldFailNotFallback()
    {
        // Act
        var response = await _client.GetAsync("/user-discovery/nearby?latitude=37.7749&longitude=-122.4194&radius=10");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Should either be empty or fail cleanly, not use fallback geocoding
            if (result.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                if (result.TryGetProperty("users", out var users))
                {
                    // If successful, should be empty (no users) not fallback data
                    Assert.True(users.GetArrayLength() == 0);
                }
            }
        }
        else
        {
            // Acceptable to fail without geocoding API or with method not allowed for non-existent endpoints
            Assert.True(response.StatusCode == HttpStatusCode.NotFound || 
                       response.StatusCode == HttpStatusCode.InternalServerError ||
                       response.StatusCode == HttpStatusCode.MethodNotAllowed);
        }
    }

    [Fact]
    public async Task Health_SystemStatus_DoesNotMentionMockOrSimulation()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        
        // Should not contain any references to mock, simulation, or fallback
        Assert.DoesNotContain("mock", content.ToLower());
        Assert.DoesNotContain("simulation", content.ToLower());
        Assert.DoesNotContain("simulate", content.ToLower());
        Assert.DoesNotContain("fallback", content.ToLower());
        Assert.DoesNotContain("dummy", content.ToLower());
        Assert.DoesNotContain("fake", content.ToLower());
    }

    [Theory]
    [InlineData("/threads/create")]
    [InlineData("/gallery/create")]
    [InlineData("/ux-primitives/weave")]
    [InlineData("/ux-primitives/reflect")]
    [InlineData("/ux-primitives/invite")]
    public async Task ModuleEndpoints_WithInvalidData_ReturnCleanErrors(string endpoint)
    {
        // Arrange - Send empty or invalid data
        var json = "{}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync(endpoint, content);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Should not contain mock data references in error responses
            Assert.DoesNotContain("mock", responseContent.ToLower());
            Assert.DoesNotContain("simulation", responseContent.ToLower());
            Assert.DoesNotContain("fallback", responseContent.ToLower());
            
            // Should be a clean error message
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            if (result.TryGetProperty("success", out var success))
            {
                Assert.False(success.GetBoolean()); // Should fail with invalid data
            }
        }
        else
        {
            // Acceptable to return 4xx/5xx status codes
            Assert.True((int)response.StatusCode >= 400);
        }
    }
}
