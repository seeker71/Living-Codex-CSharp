using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using CodexBootstrap.Core;
using Xunit;
using System.Net.Http;
using System.Text;
using System.Diagnostics;

namespace CodexBootstrap.Tests;

public class GraphPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphPerformanceTests(WebApplicationFactory<Program> factory)
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
    public async Task GetNodes_WithLargeTakeValue_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=1000");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetEdges_WithLargeTakeValue_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=1000");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithComplexSearch_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=consciousness&typeId=codex.concept&state=ice&take=100");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetEdges_WithComplexFilters_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=instance-of&minWeight=1.0&maxWeight=1.0&take=100");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 3000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetStorageStats_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/stats");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodeTypes_ReturnsWithinReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/types");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetNodes_WithPagination_ConsistentPerformance()
    {
        // Test multiple pages to ensure consistent performance
        var pageSizes = new[] { 10, 50, 100 };
        var maxResponseTime = 2000; // 2 seconds

        foreach (var pageSize in pageSizes)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?take={pageSize}&skip=0");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Page size {pageSize} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetEdges_WithPagination_ConsistentPerformance()
    {
        // Test multiple pages to ensure consistent performance
        var pageSizes = new[] { 10, 50, 100 };
        var maxResponseTime = 2000; // 2 seconds

        foreach (var pageSize in pageSizes)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/edges?take={pageSize}&skip=0");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Page size {pageSize} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithDeepPagination_HandlesGracefully()
    {
        // Test deep pagination (high skip values)
        var skipValues = new[] { 1000, 5000, 10000 };
        var maxResponseTime = 3000; // 3 seconds

        foreach (var skip in skipValues)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?take=10&skip={skip}");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Skip {skip} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetEdges_WithDeepPagination_HandlesGracefully()
    {
        // Test deep pagination (high skip values)
        var skipValues = new[] { 1000, 5000, 10000 };
        var maxResponseTime = 3000; // 3 seconds

        foreach (var skip in skipValues)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/edges?take=10&skip={skip}");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Skip {skip} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithMultipleConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        var maxResponseTime = 5000; // 5 seconds

        // Start multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"/storage-endpoints/nodes?take=10&skip={i * 10}"));
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
    public async Task GetEdges_WithMultipleConcurrentRequests_HandlesGracefully()
    {
        // Test concurrent requests
        var tasks = new List<Task<HttpResponseMessage>>();
        var maxResponseTime = 5000; // 5 seconds

        // Start multiple concurrent requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_client.GetAsync($"/storage-endpoints/edges?take=10&skip={i * 10}"));
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
    public async Task GetNodes_WithSearchTerm_PerformanceScalesWithResultSize()
    {
        // Test that performance scales reasonably with result size
        var searchTerms = new[] { "a", "con", "consciousness", "quantum consciousness" };
        var maxResponseTime = 3000; // 3 seconds

        foreach (var searchTerm in searchTerms)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?searchTerm={searchTerm}&take=50");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Search term '{searchTerm}' took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithTypeFilter_PerformanceScalesWithResultSize()
    {
        // Test that performance scales reasonably with result size
        var typeIds = new[] { "codex.concept", "codex.meta/type", "codex.file/python" };
        var maxResponseTime = 3000; // 3 seconds

        foreach (var typeId in typeIds)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?typeId={typeId}&take=50");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Type ID '{typeId}' took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithComplexFilters_PerformanceScalesWithResultSize()
    {
        // Test that performance scales reasonably with result size
        var filters = new[]
        {
            "typeId=codex.concept&state=ice",
            "typeId=codex.concept&state=ice&locale=en-US",
            "typeId=codex.concept&state=ice&locale=en-US&searchTerm=quantum"
        };
        var maxResponseTime = 3000; // 3 seconds

        foreach (var filter in filters)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?{filter}&take=50");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Filter '{filter}' took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithLargeSkipValues_HandlesGracefully()
    {
        // Test with very large skip values
        var skipValues = new[] { 100000, 500000, 1000000 };
        var maxResponseTime = 5000; // 5 seconds

        foreach (var skip in skipValues)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes?take=10&skip={skip}");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Skip {skip} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetEdges_WithLargeSkipValues_HandlesGracefully()
    {
        // Test with very large skip values
        var skipValues = new[] { 100000, 500000, 1000000 };
        var maxResponseTime = 5000; // 5 seconds

        foreach (var skip in skipValues)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync($"/storage-endpoints/edges?take=10&skip={skip}");

            // Assert
            stopwatch.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(stopwatch.ElapsedMilliseconds < maxResponseTime, 
                $"Skip {skip} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxResponseTime}ms");
        }
    }

    [Fact]
    public async Task GetNodes_WithMemoryIntensiveQuery_HandlesGracefully()
    {
        // Test with memory-intensive query (large take value)
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=5000");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }

    [Fact]
    public async Task GetEdges_WithMemoryIntensiveQuery_HandlesGracefully()
    {
        // Test with memory-intensive query (large take value)
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=5000");

        // Assert
        stopwatch.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Request took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(success.GetBoolean());
    }
}






