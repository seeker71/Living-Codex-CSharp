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

public class GraphApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetStorageStats_ReturnsStatistics()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("stats", out var stats));
        
        Assert.True(success.GetBoolean());
        Assert.True(stats.TryGetProperty("nodeCount", out var nodeCount));
        Assert.True(stats.TryGetProperty("edgeCount", out var edgeCount));
        Assert.True(stats.TryGetProperty("totalItems", out var totalItems));
        Assert.True(stats.TryGetProperty("storageBackend", out var storageBackend));
        Assert.True(stats.TryGetProperty("timestamp", out var timestamp));
        
        Assert.True(nodeCount.GetInt32() >= 0);
        Assert.True(edgeCount.GetInt32() >= 0);
        Assert.True(totalItems.GetInt32() >= 0);
        Assert.False(string.IsNullOrEmpty(storageBackend.GetString()));
        Assert.False(string.IsNullOrEmpty(timestamp.GetString()));
    }

    [Fact]
    public async Task GetNodeTypes_ReturnsNodeTypes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/types");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodeTypes", out var nodeTypes));
        Assert.True(data.TryGetProperty("totalTypes", out var totalTypes));
        Assert.True(data.TryGetProperty("totalNodes", out var totalNodes));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalTypes.GetInt32() > 0);
        Assert.True(totalNodes.GetInt32() > 0);
        Assert.True(nodeTypes.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetNodes_WithBasicQuery_ReturnsNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(nodes.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetNodes_WithSearchTerm_ReturnsMatchingNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?searchTerm=consciousness&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithTypeFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.concept&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithStateFilter_ReturnsFilteredNodes()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes?state=ice&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("nodes", out var nodes));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(nodes.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetNodes_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var response1 = await _client.GetAsync("/storage-endpoints/nodes?take=3&skip=0");
        var response2 = await _client.GetAsync("/storage-endpoints/nodes?take=3&skip=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var data1 = JsonSerializer.Deserialize<JsonElement>(content1);
        Assert.True(data1.TryGetProperty("nodes", out var nodes1));
        Assert.True(nodes1.GetArrayLength() <= 3);
        
        var content2 = await response2.Content.ReadAsStringAsync();
        var data2 = JsonSerializer.Deserialize<JsonElement>(content2);
        Assert.True(data2.TryGetProperty("nodes", out var nodes2));
        Assert.True(nodes2.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetEdges_WithBasicQuery_ReturnsEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?take=5");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(edges.GetArrayLength() <= 5);
    }

    [Fact]
    public async Task GetEdges_WithRoleFilter_ReturnsFilteredEdges()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges?role=instance-of&take=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edges", out var edges));
        Assert.True(data.TryGetProperty("totalCount", out var totalCount));
        
        Assert.True(success.GetBoolean());
        Assert.True(totalCount.GetInt32() > 0);
        Assert.True(edges.GetArrayLength() <= 3);
    }

    [Fact]
    public async Task GetEdges_WithNodeIdFilter_ReturnsRelatedEdges()
    {
        // First get a node ID
        var nodeResponse = await _client.GetAsync("/storage-endpoints/nodes?take=1");
        var nodeContent = await nodeResponse.Content.ReadAsStringAsync();
        var nodeData = JsonSerializer.Deserialize<JsonElement>(nodeContent);
        
        if (nodeData.TryGetProperty("nodes", out var nodes) && nodes.GetArrayLength() > 0)
        {
            var nodeId = nodes[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/storage-endpoints/edges?nodeId={nodeId}&take=3");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("edges", out var edges));
            Assert.True(success.GetBoolean());
            Assert.True(edges.GetArrayLength() <= 3);
        }
    }

    [Fact]
    public async Task GetNodeEdges_WithValidNodeId_ReturnsNodeEdges()
    {
        // First get a node ID
        var nodeResponse = await _client.GetAsync("/storage-endpoints/nodes?take=1");
        var nodeContent = await nodeResponse.Content.ReadAsStringAsync();
        var nodeData = JsonSerializer.Deserialize<JsonElement>(nodeContent);
        
        if (nodeData.TryGetProperty("nodes", out var nodes) && nodes.GetArrayLength() > 0)
        {
            var nodeId = nodes[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/nodes/{nodeId}/edges");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("outgoing", out var outgoing));
            Assert.True(data.TryGetProperty("incoming", out var incoming));
            
            Assert.True(success.GetBoolean());
            Assert.True(outgoing.GetArrayLength() >= 0);
            Assert.True(incoming.GetArrayLength() >= 0);
        }
    }

    [Fact]
    public async Task GetNodeEdges_WithTypeFilter_ReturnsFilteredEdges()
    {
        // First get a node ID
        var nodeResponse = await _client.GetAsync("/storage-endpoints/nodes?take=1");
        var nodeContent = await nodeResponse.Content.ReadAsStringAsync();
        var nodeData = JsonSerializer.Deserialize<JsonElement>(nodeContent);
        
        if (nodeData.TryGetProperty("nodes", out var nodes) && nodes.GetArrayLength() > 0)
        {
            var nodeId = nodes[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/nodes/{nodeId}/edges?type=instance-of");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }
    }

    [Fact]
    public async Task GetNodeEdges_WithInvalidNodeId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/nodes/invalid-node-id-12345/edges");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetNode_WithValidId_ReturnsNode()
    {
        // First get a node ID
        var nodeResponse = await _client.GetAsync("/storage-endpoints/nodes?take=1");
        var nodeContent = await nodeResponse.Content.ReadAsStringAsync();
        var nodeData = JsonSerializer.Deserialize<JsonElement>(nodeContent);
        
        if (nodeData.TryGetProperty("nodes", out var nodes) && nodes.GetArrayLength() > 0)
        {
            var nodeId = nodes[0].GetProperty("id").GetString();
            
            // Act
            var response = await _client.GetAsync($"/storage-endpoints/nodes/{nodeId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("node", out var node));
            
            Assert.True(success.GetBoolean());
            Assert.True(node.TryGetProperty("id", out var id));
            Assert.Equal(nodeId, id.GetString());
        }
    }

    [Fact]
    public async Task GetNode_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/nodes/invalid-node-id-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEdge_WithValidIds_ReturnsEdge()
    {
        // First get an edge
        var edgeResponse = await _client.GetAsync("/storage-endpoints/edges?take=1");
        var edgeContent = await edgeResponse.Content.ReadAsStringAsync();
        var edgeData = JsonSerializer.Deserialize<JsonElement>(edgeContent);
        
        if (edgeData.TryGetProperty("edges", out var edges) && edges.GetArrayLength() > 0)
        {
            var fromId = edges[0].GetProperty("fromId").GetString();
            var toId = edges[0].GetProperty("toId").GetString();
            
            // Act
            var response = await _client.GetAsync($"/storage-endpoints/edges/{fromId}/{toId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            Assert.True(data.TryGetProperty("success", out var success));
            Assert.True(data.TryGetProperty("edge", out var edge));
            
            Assert.True(success.GetBoolean());
            Assert.True(edge.TryGetProperty("fromId", out var edgeFromId));
            Assert.True(edge.TryGetProperty("toId", out var edgeToId));
            Assert.Equal(fromId, edgeFromId.GetString());
            Assert.Equal(toId, edgeToId.GetString());
        }
    }

    [Fact]
    public async Task GetEdge_WithInvalidIds_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/storage-endpoints/edges/invalid-from-id/invalid-to-id");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateNode_WithValidData_ReturnsCreatedNode()
    {
        // Arrange
        var createRequest = new
        {
            typeId = "codex.test.node",
            state = "ice",
            locale = "en-US",
            title = "Test Node",
            description = "A test node for graph testing",
            meta = new Dictionary<string, object>
            {
                ["testProperty"] = "testValue",
                ["createdAt"] = DateTime.UtcNow
            }
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/nodes", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("node", out var node));
        
        Assert.True(success.GetBoolean());
        Assert.True(node.TryGetProperty("id", out var id));
        Assert.True(node.TryGetProperty("typeId", out var typeId));
        Assert.Equal("codex.test.node", typeId.GetString());
    }

    [Fact]
    public async Task CreateEdge_WithValidData_ReturnsCreatedEdge()
    {
        // First create two nodes
        var node1Request = new
        {
            typeId = "codex.test.node",
            state = "ice",
            title = "Test Node 1"
        };
        var node1Json = JsonSerializer.Serialize(node1Request);
        var node1Content = new StringContent(node1Json, Encoding.UTF8, "application/json");
        var node1Response = await _client.PostAsync("/storage-endpoints/nodes", node1Content);
        var node1Data = JsonSerializer.Deserialize<JsonElement>(await node1Response.Content.ReadAsStringAsync());
        var node1Id = node1Data.GetProperty("node").GetProperty("id").GetString();

        var node2Request = new
        {
            typeId = "codex.test.node",
            state = "ice",
            title = "Test Node 2"
        };
        var node2Json = JsonSerializer.Serialize(node2Request);
        var node2Content = new StringContent(node2Json, Encoding.UTF8, "application/json");
        var node2Response = await _client.PostAsync("/storage-endpoints/nodes", node2Content);
        var node2Data = JsonSerializer.Deserialize<JsonElement>(await node2Response.Content.ReadAsStringAsync());
        var node2Id = node2Data.GetProperty("node").GetProperty("id").GetString();

        // Arrange
        var createRequest = new
        {
            fromId = node1Id,
            toId = node2Id,
            role = "test-relationship",
            weight = 1.0,
            meta = new Dictionary<string, object>
            {
                ["testProperty"] = "testValue"
            }
        };
        var json = JsonSerializer.Serialize(createRequest);
        var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/storage-endpoints/edges", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        Assert.True(data.TryGetProperty("success", out var success));
        Assert.True(data.TryGetProperty("edge", out var edge));
        
        Assert.True(success.GetBoolean());
        Assert.True(edge.TryGetProperty("fromId", out var fromId));
        Assert.True(edge.TryGetProperty("toId", out var toId));
        Assert.True(edge.TryGetProperty("role", out var role));
        Assert.Equal(node1Id, fromId.GetString());
        Assert.Equal(node2Id, toId.GetString());
        Assert.Equal("test-relationship", role.GetString());
    }
}






