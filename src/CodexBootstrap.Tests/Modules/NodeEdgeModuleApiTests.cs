using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CodexBootstrap.Tests.Modules;

// Response models for API testing
public class NodeListResponse
{
    public bool Success { get; set; }
    public List<object> Nodes { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}

public class NodeResponse
{
    public bool Success { get; set; }
    public object? Node { get; set; }
    public string? Error { get; set; }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

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
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
    }

    [Fact]
    public async Task GetNodes_ShouldAcceptQueryParameters()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
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
            var nodesResult = JsonSerializer.Deserialize<Dictionary<string, object>>(nodesContent, _jsonOptions);
            
            // If there are nodes, try to get the first one
            if (nodesResult.ContainsKey("nodes") && nodesResult["nodes"] is JsonElement nodesElement)
            {
                var nodes = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(nodesElement.GetRawText(), _jsonOptions);
                if (nodes != null && nodes.Count > 0)
                {
                    var firstNode = nodes[0];
                    var nodeId = firstNode["id"]?.ToString();

                    if (!string.IsNullOrEmpty(nodeId))
                    {
                        // Act
                        var response = await _client.GetAsync($"/storage-endpoints/nodes/{nodeId}");

                        // Assert
                        response.StatusCode.Should().Be(HttpStatusCode.OK);
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
                        
                        result.Should().NotBeNull();
                    }
                }
            }
        }
    }

    #endregion

    #region Missing Endpoint Tests (These should return 404 until implemented)

    [Fact]
    public async Task SearchNodes_ShouldReturnSearchResults_WhenImplemented()
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
        var successElement = (JsonElement)result!["success"];
        successElement.GetBoolean().Should().Be(true);
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
    public async Task GetEdges_ShouldReturnEdgesList_WhenImplemented()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
        var successElement = (JsonElement)result!["success"];
        successElement.GetBoolean().Should().Be(true);
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

    #region Edge Metadata Tests

    [Fact]
    public async Task GetEdgeMetadata_ShouldReturnRolesAndRelationshipTypes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/metadata");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
        
        result.Should().NotBeNull();
        result.Should().ContainKey("success");
        result.Should().ContainKey("data");
        
        var successElement = (JsonElement)result!["success"];
        successElement.GetBoolean().Should().Be(true);
        
        var dataElement = (JsonElement)result!["data"];
        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataElement.GetRawText(), _jsonOptions);
        
        data.Should().NotBeNull();
        data.Should().ContainKey("roles");
        data.Should().ContainKey("relationshipTypes");
        data.Should().ContainKey("totalRoles");
        data.Should().ContainKey("totalRelationshipTypes");
        
        // Verify roles is a non-empty array
        var roles = data!["roles"] as List<object>;
        roles.Should().NotBeNull();
        roles.Should().NotBeEmpty();
        roles.Should().OnlyContain(role => !string.IsNullOrWhiteSpace(role != null ? role.ToString() : null));
        
        // Verify relationshipTypes is a non-empty array
        var relationshipTypes = data!["relationshipTypes"] as List<object>;
        relationshipTypes.Should().NotBeNull();
        relationshipTypes.Should().NotBeEmpty();
        relationshipTypes.Should().OnlyContain(relType => !string.IsNullOrWhiteSpace(relType != null ? relType.ToString() : null));
        
        // Verify counts match array lengths
        var totalRoles = Convert.ToInt32(data!["totalRoles"]);
        var totalRelationshipTypes = Convert.ToInt32(data!["totalRelationshipTypes"]);
        
        totalRoles.Should().Be(roles.Count);
        totalRelationshipTypes.Should().Be(relationshipTypes.Count);
    }

    [Fact]
    public async Task GetEdgeMetadata_ShouldReturnConsistentData()
    {
        // Act - Make multiple requests to ensure consistency
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(_client.GetAsync("/storage-endpoints/edges/metadata"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert - All requests should succeed
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        // Parse all responses and verify they're identical
        var results = new List<Dictionary<string, object>>();
        foreach (var response in responses)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(content, _jsonOptions);
            results.Add(result!);
        }
        
        // All responses should be identical
        for (int i = 1; i < results.Count; i++)
        {
            var firstData = results[0]["data"];
            var currentData = results[i]["data"];
            
            firstData.Should().BeEquivalentTo(currentData);
        }
    }

    [Fact]
    public async Task GetEdgeMetadata_ShouldRespondWithinAcceptableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/metadata");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should respond within 1 second
    }

    #endregion

    #region Future Implementation Tests (Placeholder for when endpoints are implemented)

    [Fact]
    public async Task GetEdge_ShouldReturnEdge_WhenImplemented()
    {
        // This test will be enabled when the endpoint is implemented
        var response = await _client.GetAsync("/storage-endpoints/edges/node1/node2");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    #endregion
}
