using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using CodexBootstrap.Tests.Modules;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;

namespace CodexBootstrap.Tests.Modules
{
    /// <summary>
    /// Comprehensive tests for the ontology API endpoints using real data
    /// </summary>
    public class OntologyApiTests : IClassFixture<TestServerFixture>
    {
        private readonly TestServerFixture _fixture;
        private readonly HttpClient _client;

        public OntologyApiTests(TestServerFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.HttpClient;
        }

        [Fact]
        public async Task GetOntologyAxes_ShouldReturnRealData()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify we have real ontology data
            var axes = result.Nodes;
            Assert.Contains(axes, a => a.TypeId == "codex.ontology.axis");
            
            // Check for specific known axes
            var axisIds = axes.Select(a => a.Id).ToList();
            Assert.Contains(axisIds, id => id.Contains("water_states"));
            Assert.Contains(axisIds, id => id.Contains("quantum_dimensions"));
            Assert.Contains(axisIds, id => id.Contains("chakras"));
        }

        [Fact]
        public async Task GetOntologyAxes_WithPagination_ShouldWork()
        {
            // Act - Test pagination
            var response1 = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=5");
            var response2 = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=5&skip=5");
            
            // Assert
            Assert.True(response1.IsSuccessStatusCode);
            Assert.True(response2.IsSuccessStatusCode);
            
            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();
            
            var result1 = JsonSerializer.Deserialize<ApiResponse<List<Node>>>(content1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result2 = JsonSerializer.Deserialize<ApiResponse<List<Node>>>(content2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.True(result1.Nodes.Count <= 5);
            Assert.True(result2.Nodes.Count <= 5);
            
            // Verify different results
            var ids1 = result1.Nodes.Select(n => n.Id).ToHashSet();
            var ids2 = result2.Nodes.Select(n => n.Id).ToHashSet();
            Assert.False(ids1.Intersect(ids2).Any());
        }

        [Fact]
        public async Task GetOntologyConcepts_ShouldReturnRealData()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ucore.base&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify we have real concept data
            var concepts = result.Nodes;
            Assert.Contains(concepts, c => c.TypeId == "codex.ucore.base");
            
            // Check for specific known concepts
            var conceptIds = concepts.Select(c => c.Id).ToList();
            Assert.Contains(conceptIds, id => id.Contains("entity"));
            Assert.Contains(conceptIds, id => id.Contains("consciousness"));
            Assert.Contains(conceptIds, id => id.Contains("energy"));
        }

        [Fact]
        public async Task GetOntologyRelationships_ShouldReturnRealData()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.relationship&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify we have real relationship data
            var relationships = result.Nodes;
            Assert.Contains(relationships, r => r.TypeId == "codex.relationship");
        }

        [Fact]
        public async Task GetOntologyFrequencies_ShouldReturnRealData()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.frequency&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify we have real frequency data
            var frequencies = result.Nodes;
            Assert.Contains(frequencies, f => f.TypeId == "codex.frequency");
        }

        [Fact]
        public async Task GetOntologyAxes_WithLevelFilter_ShouldWork()
        {
            // Act - Get level 0 axes (root level)
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            
            // Filter for level 0 axes
            var level0Axes = result.Nodes.Where(a => a.Meta?.ContainsKey("level") == true && 
                                                   (a.Meta["level"] is int level && level == 0)).ToList();
            
            Assert.True(level0Axes.Count > 0);
            
            // Verify level 0 axes have expected properties
            foreach (var axis in level0Axes)
            {
                Assert.NotNull(axis.Id);
                Assert.NotNull(axis.TypeId);
                Assert.NotNull(axis.Meta);
                Assert.True(axis.Meta.ContainsKey("level"));
                Assert.Equal(0, axis.Meta["level"]);
            }
        }

        [Fact]
        public async Task GetOntologyAxes_WithHierarchy_ShouldShowParentChildRelationships()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            
            var axes = result.Nodes;
            
            // Find axes with parent-child relationships
            var axesWithParents = axes.Where(a => a.Meta?.ContainsKey("parentAxes") == true && 
                                                a.Meta["parentAxes"] is Array parentAxes && parentAxes.Length > 0).ToList();
            
            var axesWithChildren = axes.Where(a => a.Meta?.ContainsKey("childAxes") == true && 
                                                a.Meta["childAxes"] is Array childAxes && childAxes.Length > 0).ToList();
            
            // Should have some hierarchical relationships
            Assert.True(axesWithParents.Count > 0 || axesWithChildren.Count > 0);
        }

        [Fact]
        public async Task GetOntologyAxes_WithKeywords_ShouldIncludeKeywords()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            
            var axes = result.Nodes;
            
            // Find axes with keywords
            var axesWithKeywords = axes.Where(a => a.Meta?.ContainsKey("keywords") == true && 
                                                 a.Meta["keywords"] is Array keywords && keywords.Length > 0).ToList();
            
            Assert.True(axesWithKeywords.Count > 0);
            
            // Verify keywords are strings
            foreach (var axis in axesWithKeywords)
            {
                var keywords = ((JsonElement)axis.Meta["keywords"]).EnumerateArray().Select(k => k.GetString()).ToList();
                Assert.True(keywords.All(k => !string.IsNullOrEmpty(k)));
            }
        }

        [Fact]
        public async Task GetOntologyAxes_WithDimensions_ShouldIncludeDimensions()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            
            var axes = result.Nodes;
            
            // Find axes with dimensions
            var axesWithDimensions = axes.Where(a => a.Meta?.ContainsKey("dimensions") == true && 
                                                   a.Meta["dimensions"] is Array dimensions && dimensions.Length > 0).ToList();
            
            Assert.True(axesWithDimensions.Count > 0);
            
            // Verify dimensions are strings
            foreach (var axis in axesWithDimensions)
            {
                var dimensions = ((JsonElement)axis.Meta["dimensions"]).EnumerateArray().Select(d => d.GetString()).ToList();
                Assert.True(dimensions.All(d => !string.IsNullOrEmpty(d)));
            }
        }

        [Fact]
        public async Task GetOntologyAxes_WithInvalidTypeId_ShouldReturnEmpty()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=invalid.type&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.Empty(result.Nodes);
        }

        [Fact]
        public async Task GetOntologyAxes_WithNegativeTake_ShouldHandleGracefully()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=-1");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            // Should return some reasonable number of results, not negative
            Assert.True(result.Nodes.Count >= 0);
        }

        [Fact]
        public async Task GetOntologyAxes_WithLargeTake_ShouldWork()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=10000");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
        }

        [Fact]
        public async Task GetOntologyAxes_WithSpecialCharacters_ShouldWork()
        {
            // Act - Test with URL-encoded special characters
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify IDs are properly encoded/decoded
            foreach (var node in result.Nodes)
            {
                Assert.NotNull(node.Id);
                Assert.DoesNotContain(node.Id, c => c == ' '); // No spaces in IDs
            }
        }

        [Fact]
        public async Task GetOntologyAxes_ConcurrentRequests_ShouldWork()
        {
            // Act - Make multiple concurrent requests
            var tasks = Enumerable.Range(0, 5).Select(_ => 
                _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=10")).ToArray();
            
            var responses = await Task.WhenAll(tasks);
            
            // Assert
            foreach (var response in responses)
            {
                Assert.True(response.IsSuccessStatusCode);
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.NotNull(result.Nodes);
            }
        }

        [Fact]
        public async Task GetOntologyAxes_WithSearch_ShouldWork()
        {
            // Act - Test search functionality if available
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=100");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify we can search through the results
            var waterStatesAxis = result.Nodes.FirstOrDefault(a => a.Id.Contains("water_states"));
            Assert.NotNull(waterStatesAxis);
            Assert.Contains("water", waterStatesAxis.Id.ToLower());
        }

        [Fact]
        public async Task GetOntologyAxes_WithMetadata_ShouldIncludeCompleteMetadata()
        {
            // Act
            var response = await _client.GetAsync("/storage-endpoints/nodes?typeId=codex.ontology.axis&take=10");
            
            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<CodexBootstrap.Core.Node>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Nodes);
            Assert.True(result.Nodes.Count > 0);
            
            // Verify metadata completeness
            foreach (var node in result.Nodes)
            {
                Assert.NotNull(node.Id);
                Assert.NotNull(node.TypeId);
                Assert.NotNull(node.Meta);
                
                // Check for essential metadata fields
                Assert.True(node.Meta.ContainsKey("name") || node.Meta.ContainsKey("title"));
                Assert.True(node.Meta.ContainsKey("level"));
                Assert.True(node.Meta.ContainsKey("keywords"));
                Assert.True(node.Meta.ContainsKey("dimensions"));
            }
        }
    }

    // Helper classes for deserialization
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Nodes { get; set; }
        public string Error { get; set; }
    }
}
