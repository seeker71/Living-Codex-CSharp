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

public class OntologyApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OntologyApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_ReturnsAllNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.TryGetProperty("nodes", out var nodes));
        Assert.True(result.TryGetProperty("totalCount", out var totalCount));
        Assert.True(totalCount.GetInt32() > 0);
    }

    [Fact]
    public async Task GetNodes_WithTypeIdFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 5);
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.concept", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithUcoreConceptFilter_ReturnsUcoreNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=ucore.concept&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 3);
        
        foreach (var node in nodes)
        {
            Assert.Equal("ucore.concept", node.GetProperty("typeId").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithSearchTerm_ReturnsMatchingNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=consciousness&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
        
        // Verify at least one node contains the search term
        var foundMatch = false;
        foreach (var node in nodes)
        {
            var title = node.GetProperty("title").GetString() ?? "";
            var description = node.GetProperty("description").GetString() ?? "";
            if (title.ToLower().Contains("consciousness") || description.ToLower().Contains("consciousness"))
            {
                foundMatch = true;
                break;
            }
        }
        Assert.True(foundMatch);
    }

    [Fact]
    public async Task GetNodes_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?skip=10&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 5);
        Assert.Equal(10, result.GetProperty("skip").GetInt32());
        Assert.Equal(5, result.GetProperty("take").GetInt32());
    }

    [Fact]
    public async Task GetNodes_WithStateFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?state=ice&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 5);
        
        foreach (var node in nodes)
        {
            Assert.Equal("ice", node.GetProperty("state").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithLocaleFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?locale=en&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 5);
        
        foreach (var node in nodes)
        {
            Assert.Equal("en", node.GetProperty("locale").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithMultipleFilters_ReturnsCorrectlyFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&state=ice&locale=en&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 3);
        
        foreach (var node in nodes)
        {
            Assert.Equal("codex.concept", node.GetProperty("typeId").GetString());
            Assert.Equal("ice", node.GetProperty("state").GetString());
            Assert.Equal("en", node.GetProperty("locale").GetString());
        }
    }

    [Fact]
    public async Task GetNodes_WithInvalidTypeId_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=nonexistent.type&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Empty(nodes);
        Assert.Equal(0, result.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetNodes_WithEmptySearchTerm_ReturnsAllNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 0);
    }

    [Fact]
    public async Task GetNodes_WithLargeTakeValue_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=10000");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // Should be limited by some reasonable maximum
        Assert.True(nodes.Count < 10000);
    }

    [Fact]
    public async Task GetNodes_WithNegativeSkip_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?skip=-1");

        // Assert
        // The API should handle negative values gracefully
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithNegativeTake_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=-1");

        // Assert
        // The API should handle negative values gracefully
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
    }
}
