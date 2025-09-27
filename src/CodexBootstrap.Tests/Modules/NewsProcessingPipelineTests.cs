using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Comprehensive tests for the enhanced news processing pipeline
/// Validates the complete flow: news item → content → summary → concepts → U-Core paths
/// </summary>
public class NewsProcessingPipelineTests : IClassFixture<TestServerFixture>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public NewsProcessingPipelineTests(TestServerFixture fixture)
    {
        _client = fixture.HttpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task NewsProcessingPipeline_ShouldCreateCompleteEdgeNetwork()
    {
        // Arrange - Create a test news item
        var testNewsItem = new
        {
            id = $"test-news-{Guid.NewGuid():N}",
            title = "Breakthrough in Quantum Computing Research",
            content = "Scientists have made significant progress in quantum computing, achieving new milestones in quantum error correction and quantum supremacy. This breakthrough could revolutionize computing as we know it.",
            source = "Test News Source",
            url = "https://example.com/quantum-breakthrough",
            publishedAt = DateTimeOffset.UtcNow,
            tags = new[] { "quantum", "computing", "technology", "research" },
            metadata = new Dictionary<string, object>
            {
                ["sourceType"] = "Test",
                ["testRun"] = true
            }
        };

        // Act - Process the news item through the pipeline
        var ingestResponse = await _client.PostAsync("/news/ingest", 
            new StringContent(JsonSerializer.Serialize(testNewsItem), Encoding.UTF8, "application/json"));

        // Assert - Verify the news item was processed successfully
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ingestResult = await ingestResponse.Content.ReadAsStringAsync();
        var ingestData = JsonSerializer.Deserialize<Dictionary<string, object>>(ingestResult, _jsonOptions);
        ingestData.Should().NotBeNull();
        ingestData["success"].ToString().Should().Be("True");

        // Wait a moment for async processing to complete
        await Task.Delay(2000);

        // Act - Get the processed news item to verify pipeline stages
        var newsResponse = await _client.GetAsync($"/news/item/{testNewsItem.id}");
        newsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newsResult = await newsResponse.Content.ReadAsStringAsync();
        var newsData = JsonSerializer.Deserialize<Dictionary<string, object>>(newsResult, _jsonOptions);

        // Assert - Verify all pipeline stages exist
        newsData.Should().NotBeNull();
        newsData.Should().ContainKey("contentNodeId");
        newsData.Should().ContainKey("summaryNodeId");
        newsData.Should().ContainKey("conceptNodeIds");
        newsData.Should().ContainKey("ucorePathIds");
        newsData.Should().ContainKey("pipelineVersion");

        var contentNodeId = newsData["contentNodeId"].ToString();
        var summaryNodeId = newsData["summaryNodeId"].ToString();
        var conceptNodeIds = JsonSerializer.Deserialize<string[]>(newsData["conceptNodeIds"].ToString()!, _jsonOptions);
        var ucorePathIds = JsonSerializer.Deserialize<string[]>(newsData["ucorePathIds"].ToString()!, _jsonOptions);

        // Verify pipeline version
        newsData["pipelineVersion"].ToString().Should().Be("2.0");

        // Act - Get content node and verify it exists
        var contentResponse = await _client.GetAsync($"/nodes/{contentNodeId}");
        contentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentResult = await contentResponse.Content.ReadAsStringAsync();
        var contentData = JsonSerializer.Deserialize<Dictionary<string, object>>(contentResult, _jsonOptions);

        // Assert - Verify content node properties
        contentData.Should().NotBeNull();
        contentData["typeId"].ToString().Should().Be("codex.news.content");
        contentData.Should().ContainKey("content");

        // Act - Get summary node and verify it exists
        var summaryResponse = await _client.GetAsync($"/nodes/{summaryNodeId}");
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaryResult = await summaryResponse.Content.ReadAsStringAsync();
        var summaryData = JsonSerializer.Deserialize<Dictionary<string, object>>(summaryResult, _jsonOptions);

        // Assert - Verify summary node properties
        summaryData.Should().NotBeNull();
        summaryData["typeId"].ToString().Should().Be("codex.news.summary");
        summaryData.Should().ContainKey("content");

        // Act - Get concept nodes and verify they exist
        conceptNodeIds.Should().NotBeEmpty("Concepts should be extracted from the summary");
        
        foreach (var conceptNodeId in conceptNodeIds)
        {
            var conceptResponse = await _client.GetAsync($"/nodes/{conceptNodeId}");
            conceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var conceptResult = await conceptResponse.Content.ReadAsStringAsync();
            var conceptData = JsonSerializer.Deserialize<Dictionary<string, object>>(conceptResult, _jsonOptions);

            // Assert - Verify concept node properties
            conceptData.Should().NotBeNull();
            conceptData["typeId"].ToString().Should().StartWith("codex.concept");
            conceptData.Should().ContainKey("conceptId");
        }

        // Act - Get U-Core path nodes and verify they exist
        ucorePathIds.Should().NotBeEmpty("U-Core paths should be created for concepts");
        
        foreach (var ucorePathId in ucorePathIds)
        {
            var ucoreResponse = await _client.GetAsync($"/nodes/{ucorePathId}");
            ucoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var ucoreResult = await ucoreResponse.Content.ReadAsStringAsync();
            var ucoreData = JsonSerializer.Deserialize<Dictionary<string, object>>(ucoreResult, _jsonOptions);

            // Assert - Verify U-Core path node properties
            ucoreData.Should().NotBeNull();
            ucoreData["typeId"].ToString().Should().StartWith("codex.concept");
            ucoreData.Should().ContainKey("conceptId");
        }

        // Act - Verify edge network by getting edges from news item
        var edgesResponse = await _client.GetAsync($"/nodes/{testNewsItem.id}/edges");
        edgesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var edgesResult = await edgesResponse.Content.ReadAsStringAsync();
        var edgesData = JsonSerializer.Deserialize<Dictionary<string, object>>(edgesResult, _jsonOptions);

        // Assert - Verify edge network structure
        edgesData.Should().NotBeNull();
        edgesData.Should().ContainKey("outgoing");
        edgesData.Should().ContainKey("incoming");

        var outgoingEdges = JsonSerializer.Deserialize<Dictionary<string, object>[]>(edgesData["outgoing"].ToString()!, _jsonOptions);
        var incomingEdges = JsonSerializer.Deserialize<Dictionary<string, object>[]>(edgesData["incoming"].ToString()!, _jsonOptions);

        // Verify specific edge types exist
        var edgeTypes = outgoingEdges.Select(e => e["type"].ToString()).ToList();
        edgeTypes.Should().Contain("has-content", "News should have edge to content");
        edgeTypes.Should().Contain("connects-to-ucore-via", "News should have edge to U-Core paths");

        // Verify content to summary edge
        var contentEdgesResponse = await _client.GetAsync($"/nodes/{contentNodeId}/edges");
        contentEdgesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contentEdgesResult = await contentEdgesResponse.Content.ReadAsStringAsync();
        var contentEdgesData = JsonSerializer.Deserialize<Dictionary<string, object>>(contentEdgesResult, _jsonOptions);
        var contentOutgoingEdges = JsonSerializer.Deserialize<Dictionary<string, object>[]>(contentEdgesData["outgoing"].ToString()!, _jsonOptions);
        var contentEdgeTypes = contentOutgoingEdges.Select(e => e["type"].ToString()).ToList();
        contentEdgeTypes.Should().Contain("summarized-as", "Content should have edge to summary");

        // Verify summary to concepts edges
        var summaryEdgesResponse = await _client.GetAsync($"/nodes/{summaryNodeId}/edges");
        summaryEdgesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaryEdgesResult = await summaryEdgesResponse.Content.ReadAsStringAsync();
        var summaryEdgesData = JsonSerializer.Deserialize<Dictionary<string, object>>(summaryEdgesResult, _jsonOptions);
        var summaryOutgoingEdges = JsonSerializer.Deserialize<Dictionary<string, object>[]>(summaryEdgesData["outgoing"].ToString()!, _jsonOptions);
        var summaryEdgeTypes = summaryOutgoingEdges.Select(e => e["type"].ToString()).ToList();
        summaryEdgeTypes.Should().Contain("contains-concept", "Summary should have edges to concepts");

        // Verify U-Core path edges exist
        foreach (var ucorePathId in ucorePathIds)
        {
            var ucoreEdgesResponse = await _client.GetAsync($"/nodes/{ucorePathId}/edges");
            ucoreEdgesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var ucoreEdgesResult = await ucoreEdgesResponse.Content.ReadAsStringAsync();
            var ucoreEdgesData = JsonSerializer.Deserialize<Dictionary<string, object>>(ucoreEdgesResult, _jsonOptions);
            var ucoreOutgoingEdges = JsonSerializer.Deserialize<Dictionary<string, object>[]>(ucoreEdgesData["outgoing"].ToString()!, _jsonOptions);
            var ucoreEdgeTypes = ucoreOutgoingEdges.Select(e => e["type"].ToString()).ToList();
            
            // Should have either 'leads-to' edges (for intermediate concepts) or be a U-Core concept
            ucoreEdgeTypes.Should().NotBeEmpty("U-Core path nodes should have outgoing edges");
        }

        // Act - Verify we can navigate the complete pipeline
        await VerifyPipelineNavigation(testNewsItem.id, contentNodeId, summaryNodeId, conceptNodeIds, ucorePathIds);
    }

    [Fact]
    public async Task NewsProcessingPipeline_ShouldHandleMultipleConcepts()
    {
        // Arrange - Create a news item with multiple concepts
        var testNewsItem = new
        {
            id = $"test-news-multi-{Guid.NewGuid():N}",
            title = "AI and Machine Learning Revolutionize Healthcare",
            content = "Artificial intelligence and machine learning technologies are transforming healthcare through advanced diagnostics, personalized medicine, and predictive analytics. These innovations are improving patient outcomes and reducing costs.",
            source = "Test News Source",
            url = "https://example.com/ai-healthcare",
            publishedAt = DateTimeOffset.UtcNow,
            tags = new[] { "ai", "machine-learning", "healthcare", "technology", "medicine" },
            metadata = new Dictionary<string, object>
            {
                ["sourceType"] = "Test",
                ["testRun"] = true
            }
        };

        // Act - Process the news item
        var ingestResponse = await _client.PostAsync("/news/ingest", 
            new StringContent(JsonSerializer.Serialize(testNewsItem), Encoding.UTF8, "application/json"));

        // Assert - Verify processing was successful
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Wait for processing
        await Task.Delay(2000);

        // Act - Get the processed news item
        var newsResponse = await _client.GetAsync($"/news/item/{testNewsItem.id}");
        newsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newsResult = await newsResponse.Content.ReadAsStringAsync();
        var newsData = JsonSerializer.Deserialize<Dictionary<string, object>>(newsResult, _jsonOptions);

        // Assert - Verify multiple concepts were extracted
        var conceptNodeIds = JsonSerializer.Deserialize<string[]>(newsData["conceptNodeIds"].ToString()!, _jsonOptions);
        conceptNodeIds.Length.Should().BeGreaterThan(1, "Multiple concepts should be extracted from complex content");

        // Assert - Verify U-Core paths were created for each concept
        var ucorePathIds = JsonSerializer.Deserialize<string[]>(newsData["ucorePathIds"].ToString()!, _jsonOptions);
        ucorePathIds.Length.Should().BeGreaterThan(0, "U-Core paths should be created for extracted concepts");
    }

    [Fact]
    public async Task NewsProcessingPipeline_ShouldCreateMissingConcepts()
    {
        // Arrange - Create a news item with unique concepts that likely don't exist
        var testNewsItem = new
        {
            id = $"test-news-unique-{Guid.NewGuid():N}",
            title = "Revolutionary Quantum Holographic Computing Breakthrough",
            content = "Scientists have developed a new quantum holographic computing system that uses quantum entanglement and holographic principles to process information at unprecedented speeds. This breakthrough combines quantum mechanics with holographic storage techniques.",
            source = "Test News Source",
            url = "https://example.com/quantum-holographic",
            publishedAt = DateTimeOffset.UtcNow,
            tags = new[] { "quantum-holographic", "computing", "breakthrough", "entanglement" },
            metadata = new Dictionary<string, object>
            {
                ["sourceType"] = "Test",
                ["testRun"] = true
            }
        };

        // Act - Process the news item
        var ingestResponse = await _client.PostAsync("/news/ingest", 
            new StringContent(JsonSerializer.Serialize(testNewsItem), Encoding.UTF8, "application/json"));

        // Assert - Verify processing was successful
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Wait for processing
        await Task.Delay(3000);

        // Act - Get the processed news item
        var newsResponse = await _client.GetAsync($"/news/item/{testNewsItem.id}");
        newsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newsResult = await newsResponse.Content.ReadAsStringAsync();
        var newsData = JsonSerializer.Deserialize<Dictionary<string, object>>(newsResult, _jsonOptions);

        // Assert - Verify concepts were extracted and U-Core paths created
        var conceptNodeIds = JsonSerializer.Deserialize<string[]>(newsData["conceptNodeIds"].ToString()!, _jsonOptions);
        var ucorePathIds = JsonSerializer.Deserialize<string[]>(newsData["ucorePathIds"].ToString()!, _jsonOptions);

        conceptNodeIds.Should().NotBeEmpty("Concepts should be extracted even for unique content");
        ucorePathIds.Should().NotBeEmpty("U-Core paths should be created even for unique concepts");

        // Verify that missing concepts were created
        foreach (var ucorePathId in ucorePathIds)
        {
            var ucoreResponse = await _client.GetAsync($"/nodes/{ucorePathId}");
            ucoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var ucoreResult = await ucoreResponse.Content.ReadAsStringAsync();
            var ucoreData = JsonSerializer.Deserialize<Dictionary<string, object>>(ucoreResult, _jsonOptions);

            // Verify the concept was created with proper metadata
            ucoreData.Should().NotBeNull();
            ucoreData.Should().ContainKey("conceptId");
            ucoreData.Should().ContainKey("createdFrom");
            ucoreData["createdFrom"].ToString().Should().Be("news-pipeline");
        }
    }

    private async Task VerifyPipelineNavigation(string newsId, string contentNodeId, string summaryNodeId, string[] conceptNodeIds, string[] ucorePathIds)
    {
        // Test navigation from news item to content
        var newsToContentResponse = await _client.GetAsync($"/nodes/{newsId}/edges?type=has-content");
        newsToContentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Test navigation from content to summary
        var contentToSummaryResponse = await _client.GetAsync($"/nodes/{contentNodeId}/edges?type=summarized-as");
        contentToSummaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Test navigation from summary to concepts
        var summaryToConceptsResponse = await _client.GetAsync($"/nodes/{summaryNodeId}/edges?type=contains-concept");
        summaryToConceptsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Test navigation through U-Core paths
        foreach (var ucorePathId in ucorePathIds)
        {
            var ucorePathResponse = await _client.GetAsync($"/nodes/{ucorePathId}/edges?type=leads-to");
            ucorePathResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Test reverse navigation from U-Core concepts back to news
        var ucoreToNewsResponse = await _client.GetAsync($"/nodes/{ucorePathIds.First()}/edges?type=connects-from-ucore");
        ucoreToNewsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
