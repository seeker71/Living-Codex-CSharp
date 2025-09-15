using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Comprehensive API tests for Node/Edge Management endpoints
/// Tests all mobile app API calls for node and edge operations
/// </summary>
public class NodeEdgeModuleApiTests : IClassFixture<TestServerFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public NodeEdgeModuleApiTests(TestServerFixture fixture)
    {
        _client = fixture.HttpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region GET /storage-endpoints/nodes - Get All Nodes

    [Fact]
    public async Task GetNodes_ShouldReturnNodesList()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNodes_ShouldAcceptQueryParameters()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
        
        result.Should().NotBeNull();
    }

    #endregion

    #region GET /storage-endpoints/nodes/{id} - Get Specific Node

    [Fact]
    public async Task GetNode_ShouldReturnNotFound_WhenNodeDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes/nonexistent-node");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNode_ShouldReturnNode_WhenNodeExists()
    {
        // First, get all nodes to find an existing one
        var nodesResponse = await _client.GetAsync("/storage-endpoints/nodes");
        if (nodesResponse.StatusCode == HttpStatusCode.OK)
        {
            var nodesContent = await nodesResponse.Content.ReadAsStringAsync();
            var nodesResult = JsonSerializer.Deserialize<dynamic>(nodesContent, _jsonOptions);
            
            // If there are nodes, try to get the first one
            var nodes = nodesResult?.GetProperty("nodes");
            if (nodes != null && nodes.GetArrayLength() > 0)
            {
                var firstNode = nodes[0];
                var nodeId = firstNode.GetProperty("id").GetString();

                // Act
                var response = await _client.GetAsync($"/storage-endpoints/nodes/{nodeId}");

                // Assert
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<dynamic>(content, _jsonOptions);
                
                result.Should().NotBeNull();
            }
        }
    }

    #endregion

    #region Missing Endpoint Tests (These should return 404 until implemented)

    [Fact]
    public async Task SearchNodes_ShouldReturnNotFound_WhenNotImplemented()
    {
        // Arrange
        var request = new
        {
            query = "test search",
            filters = new { typeId = "codex.concept" },
            limit = 10
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes/search", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEdge_ShouldReturnNotFound_WhenNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/node1/node2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEdges_ShouldReturnNotFound_WhenNotImplemented()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetNodes_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should respond within 2 seconds
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetNodes_ShouldHandleInvalidQueryParameters()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?invalidParam=value&anotherInvalid=123");

        // Assert
        // Should either return OK (ignoring invalid params) or BadRequest
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNodes_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Make 5 concurrent requests
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_client.GetAsync("/storage-endpoints/nodes"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should succeed
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    #endregion

    #region Future Implementation Tests (Placeholder for when endpoints are implemented)

    [Fact(Skip = "Endpoint not yet implemented")]
    public async Task SearchNodes_ShouldReturnSearchResults_WhenImplemented()
    {
        // This test will be enabled when the endpoint is implemented
        var request = new
        {
            query = "artificial intelligence",
            filters = new { typeId = "codex.concept" },
            limit = 10
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/storage-endpoints/nodes/search", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Endpoint not yet implemented")]
    public async Task GetEdges_ShouldReturnEdgesList_WhenImplemented()
    {
        // This test will be enabled when the endpoint is implemented
        var response = await _client.GetAsync("/storage-endpoints/edges");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Endpoint not yet implemented")]
    public async Task GetEdge_ShouldReturnEdge_WhenImplemented()
    {
        // This test will be enabled when the endpoint is implemented
        var response = await _client.GetAsync("/storage-endpoints/edges/node1/node2");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    #endregion
}
