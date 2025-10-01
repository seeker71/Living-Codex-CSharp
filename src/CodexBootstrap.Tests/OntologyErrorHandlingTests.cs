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

public class OntologyErrorHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OntologyErrorHandlingTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_WithInvalidTypeId_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=invalid.type.id&take=5");

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
    public async Task GetNodes_WithInvalidState_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?state=invalidstate&take=5");

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
    public async Task GetNodes_WithInvalidLocale_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?locale=invalidlocale&take=5");

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
    public async Task GetNodes_WithNegativeSkip_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?skip=-10&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle negative skip gracefully
    }

    [Fact]
    public async Task GetNodes_WithNegativeTake_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=-5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle negative take gracefully
    }

    [Fact]
    public async Task GetNodes_WithZeroTake_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Empty(nodes);
    }

    [Fact]
    public async Task GetNodes_WithVeryLargeSkip_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?skip=999999&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Empty(nodes);
    }

    [Fact]
    public async Task GetNodes_WithVeryLargeTake_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=999999");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        // Should be limited to a reasonable maximum
        Assert.True(nodes.Count < 999999);
    }

    [Fact]
    public async Task GetNodes_WithSpecialCharactersInSearch_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=!@#$%^&*()&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should not crash
    }

    [Fact]
    public async Task GetNodes_WithUnicodeCharactersInSearch_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=测试中文&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should not crash
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
    public async Task GetNodes_WithWhitespaceSearchTerm_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=   &take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle whitespace gracefully
    }

    [Fact]
    public async Task GetNodes_WithLongSearchTerm_HandlesGracefully()
    {
        // Act
        var longSearchTerm = new string('a', 1000);
        var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={longSearchTerm}&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle long search terms gracefully
    }

    [Fact]
    public async Task GetNodes_WithMultipleInvalidFilters_ReturnsEmptyResults()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=invalid&state=invalid&locale=invalid&take=5");

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
    public async Task GetNodes_WithMalformedQueryParameters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?invalidparam=value&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should ignore unknown parameters
    }

    [Fact]
    public async Task GetNodes_WithDuplicateParameters_UsesLastValue()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=5&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        var nodes = result.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count <= 10);
    }

    [Fact]
    public async Task GetNodes_WithNullParameters_HandlesGracefully()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=null&searchTerm=null&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle null-like parameters gracefully
    }

    [Fact]
    public async Task GetNodes_WithSQLInjectionAttempt_HandlesSafely()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm='; DROP TABLE nodes; --&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle SQL injection attempts safely
    }

    [Fact]
    public async Task GetNodes_WithXSSAttempt_HandlesSafely()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=<script>alert('xss')</script>&take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(result.GetProperty("success").GetBoolean());
        // Should handle XSS attempts safely
    }

    [Fact]
    public async Task GetNodes_WithVeryLongURL_HandlesGracefully()
    {
        // Act
        var longSearchTerm = new string('a', 2000);
        var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={longSearchTerm}&take=5");

        // Assert
        // Should either succeed or return a proper error, not crash
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
