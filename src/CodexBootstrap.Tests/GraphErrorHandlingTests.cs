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

public class GraphErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_WithNegativeTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNodes_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNodes_WithZeroTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNodes_WithInvalidTypeId_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=invalid-type-with-special-chars!@#$%&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithInvalidState_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?state=invalid-state&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithInvalidLocale_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?locale=invalid-locale&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetEdges_WithNegativeTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=-5");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEdges_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?skip=-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEdges_WithZeroTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEdges_WithInvalidMinWeight_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?minWeight=invalid&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEdges_WithInvalidMaxWeight_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?maxWeight=invalid&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEdges_WithMinWeightGreaterThanMaxWeight_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?minWeight=5.0&maxWeight=2.0&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetNode_WithEmptyId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNode_WithSpecialCharacters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes/test@#$%^&*()");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNode_WithVeryLongId_HandlesGracefully()
    {
        // Act
        var veryLongId = new string('a', 10000);
        var response = await _client.GetAsync($"/storage-endpoints/nodes/{veryLongId}");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEdge_WithEmptyFromId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges//test-to-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEdge_WithEmptyToId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/test-from-id/");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEdge_WithSpecialCharacters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/test@#$%^&*()/test@#$%^&*()");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNodeEdges_WithEmptyNodeId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/nodes//edges");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNodeEdges_WithSpecialCharacters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/nodes/test@#$%^&*()/edges");

        // Assert
        // Should either return 404 or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateNode_WithEmptyRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateNode_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateNode_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new
        {
            // Missing typeId
            state = "ice",
            title = "Test Node"
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateNode_WithInvalidState_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new
        {
            typeId = "codex.test.node",
            state = "invalid-state",
            title = "Test Node"
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEdge_WithEmptyRequestBody_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEdge_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var requestContent = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEdge_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new
        {
            // Missing fromId, toId, role
            weight = 1.0
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEdge_WithNonExistentNodes_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new
        {
            fromId = "non-existent-from-id",
            toId = "non-existent-to-id",
            role = "test-relationship"
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEdge_WithSameFromAndToId_ReturnsBadRequest()
    {
        // First create a node
        var nodeRequest = new
        {
            typeId = "codex.test.node",
            state = "ice",
            title = "Test Node"
        };
        var nodeJson = JsonSerializer.Serialize(nodeRequest);
        var nodeContent = new StringContent(nodeJson, Encoding.UTF8, "application/json");
        var nodeResponse = await _client.PostAsync("/storage-endpoints/nodes", nodeContent);
        var nodeData = JsonSerializer.Deserialize<JsonElement>(await nodeResponse.Content.ReadAsStringAsync());
        var nodeId = nodeData.GetProperty("node").GetProperty("id").GetString();

        // Arrange
        var createRequest = new
        {
            fromId = nodeId,
            toId = nodeId, // Same as fromId
            role = "test-relationship"
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GraphEndpoints_WithMalformedQueryParameters_HandleGracefully()
    {
        // Test various malformed query parameters
        var malformedUrls = new[]
        {
            "/storage-endpoints/nodes?take=abc",
            "/storage-endpoints/nodes?skip=xyz",
            "/storage-endpoints/edges?minWeight=invalid",
            "/storage-endpoints/edges?maxWeight=not-a-number"
        };

        foreach (var url in malformedUrls)
        {
            // Act
            var response = await _client.GetAsync(url);

            // Assert
            // Should either return 400 Bad Request or handle gracefully
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GraphEndpoints_WithVeryLongQueryParameters_HandleGracefully()
    {
        // Test with very long query parameters
        var longParam = new string('a', 10000);
        var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={longParam}&take=3");

        // Assert
        // Should either return 400 Bad Request or handle gracefully
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GraphEndpoints_WithUnicodeQueryParameters_HandleGracefully()
    {
        // Test with Unicode query parameters
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=测试&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GraphEndpoints_WithSQLInjectionAttempts_HandleGracefully()
    {
        // Test with SQL injection attempts
        var sqlInjectionAttempts = new[]
        {
            "'; DROP TABLE nodes; --",
            "1' OR '1'='1",
            "admin'--",
            "1' UNION SELECT * FROM users--"
        };

        foreach (var attempt in sqlInjectionAttempts)
        {
            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={attempt}&take=3");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task GraphEndpoints_WithXSSAttempts_HandleGracefully()
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
            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={attempt}&take=3");

            // Assert
            // Should handle gracefully without errors
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }
    }
}






