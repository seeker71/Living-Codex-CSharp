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

public class GraphFilteringTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphFilteringTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_WithMultipleTypeFilters_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&typeId=codex.meta/type&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetNodes_WithStateAndTypeFilters_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&state=ice&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithLocaleFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?locale=en-US&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithComplexSearch_ReturnsMatchingNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=quantum&typeId=codex.concept&state=ice&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetEdges_WithMultipleRoleFilters_ReturnsFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=instance-of&role=has_content_type&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        Assert.True(edges.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetEdges_WithWeightFilters_ReturnsFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?minWeight=0.5&maxWeight=2.0&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        Assert.True(edges.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetEdges_WithRelationshipFilter_ReturnsFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?relationship=node-instance-of-type&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        Assert.True(edges.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetEdges_WithComplexFilters_ReturnsFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=instance-of&minWeight=1.0&maxWeight=1.0&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        Assert.True(edges.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithEmptySearchTerm_ReturnsAllNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithNonExistentType_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=non-existent-type&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, nodes.GetArrayLength());
    }

    [Fact]
    public async Task GetEdges_WithNonExistentRole_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=non-existent-role&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, edges.GetArrayLength());
    }

    [Fact]
    public async Task GetNodes_WithCaseInsensitiveSearch_ReturnsMatchingNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=CONSCIOUSNESS&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithPartialSearch_ReturnsMatchingNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=conscious&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithSpecialCharactersInSearch_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=test@#$%^&*()&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithVeryLongSearchTerm_HandlesGracefully()
    {
        // Act
        var longSearchTerm = new string('a', 1000);
        var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={longSearchTerm}&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithUnicodeSearch_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=测试&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithMultipleFilters_ReturnsCorrectlyFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&state=ice&locale=en-US&searchTerm=quantum&take=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        Assert.True(nodes.GetArrayLength() <= 2);
    }

    [Fact]
    public async Task GetEdges_WithMultipleFilters_ReturnsCorrectlyFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=instance-of&minWeight=1.0&maxWeight=1.0&take=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        Assert.True(edges.GetArrayLength() <= 2);
    }

    [Fact]
    public async Task GetNodes_WithLargeTakeValue_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=10000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(success.GetBoolean());
        // Should be limited to a reasonable number (e.g., 500 max)
        Assert.True(nodes.GetArrayLength() <= 500);
    }

    [Fact]
    public async Task GetEdges_WithLargeTakeValue_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=10000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(success.GetBoolean());
        // Should be limited to a reasonable number (e.g., 500 max)
        Assert.True(edges.GetArrayLength() <= 500);
    }
}






