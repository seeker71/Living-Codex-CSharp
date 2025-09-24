using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CodexBootstrap.Tests
{
    public class UCoreTopologyTests
    {
        private readonly HttpClient _client;

        public UCoreTopologyTests()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5002");
        }

        [Fact]
        public async Task TestWithExistingServer()
        {
            // Test health endpoint
            var healthResponse = await _client.GetAsync("/health");
            Assert.True(healthResponse.IsSuccessStatusCode);
            
            // Test kw-matter node
            var nodeResponse = await _client.GetAsync("/storage-endpoints/nodes/u-core-concept-kw-matter");
            Assert.True(nodeResponse.IsSuccessStatusCode);
            
            var nodeContent = await nodeResponse.Content.ReadAsStringAsync();
            var nodeData = JsonSerializer.Deserialize<JsonElement>(nodeContent);
            Assert.True(nodeData.GetProperty("success").GetBoolean());
            
            // Test edges
            var edgesResponse = await _client.GetAsync("/storage-endpoints/edges?FromId=u-core-concept-kw-matter");
            Assert.True(edgesResponse.IsSuccessStatusCode);
            
            var edgesContent = await edgesResponse.Content.ReadAsStringAsync();
            var edgesData = JsonSerializer.Deserialize<JsonElement>(edgesContent);
            Assert.True(edgesData.GetProperty("success").GetBoolean());
            
            var edges = edgesData.GetProperty("edges").EnumerateArray().ToList();
            Assert.True(edges.Count > 0, $"Expected edges for kw-matter, but found {edges.Count}");
            
            Console.WriteLine($"Found {edges.Count} edges for kw-matter:");
            foreach (var edge in edges)
            {
                Console.WriteLine($"  {edge.GetProperty("fromId").GetString()} -> {edge.GetProperty("toId").GetString()} ({edge.GetProperty("role").GetString()})");
            }
        }

        [Fact]
        public async Task DeepChildConcept_ShouldWalkToIdentity_WithExpectedEdges()
        {
            // First, verify the identity concept exists
            var identityNode = await GetNode("u-core-meta-identity");
            Assert.NotNull(identityNode);
            Console.WriteLine("Identity concept exists and is accessible");

            // Start from a deep child concept: u-core-concept-kw-matter
            var startNodeId = "u-core-concept-kw-matter";
            
            // Get the starting node
            var startNode = await GetNode(startNodeId);
            Assert.NotNull(startNode);
            Assert.Equal(startNodeId, startNode.Value.GetProperty("node").GetProperty("id").GetString());

            // Walk the topology to find the identity concept
            var visitedNodes = new HashSet<string>();
            var pathToIdentity = new List<string>();
            var allEdges = new List<EdgeInfo>();

            var foundIdentity = await WalkToIdentity(startNodeId, visitedNodes, pathToIdentity, allEdges, maxDepth: 10);
            
            Assert.True(foundIdentity, $"Could not walk from {startNodeId} to identity concept");
            Assert.Contains("u-core-meta-identity", pathToIdentity);

            // Print the path for debugging
            Console.WriteLine($"Path to identity: {string.Join(" -> ", pathToIdentity)}");

            // Verify expected edge types exist in the walk
            var edgeTypes = allEdges.Select(e => e.Role).Distinct().ToList();
            
                // Core relationship types that should be present
                var expectedEdgeTypes = new[]
                {
                    "concept_on_axis", 
                    "axis_has_keyword",
                    "axis_has_dimension",
                    "is_a",
                    "instance-of",
                    "has_content_type"
                };

            foreach (var expectedType in expectedEdgeTypes)
            {
                Assert.Contains(expectedType, edgeTypes);
            }

            // Verify the path includes expected intermediate concepts
            var pathConcepts = pathToIdentity.ToHashSet();
            Assert.Contains("u-core-concept-kw-matter", pathConcepts);
            Assert.Contains("u-core-meta-identity", pathConcepts);
        }

        [Fact]
        public async Task AllAxisKeywords_ShouldHaveConcepts()
        {
            // Get all axis nodes
            var axes = await GetNodesByType("codex.axis.universal");
            
            foreach (var axis in axes)
            {
                var axisId = axis.GetProperty("id").GetString();
                var keywords = axis.GetProperty("content").Deserialize<JsonElement>()
                    .GetProperty("keywords").EnumerateArray()
                    .Select(k => k.GetString())
                    .ToList();

                foreach (var keyword in keywords)
                {
                    var conceptId = $"u-core-concept-kw-{keyword}";
                    var concept = await GetNode(conceptId);
                    Assert.NotNull(concept);
                    Assert.Equal(conceptId, concept.Value.GetProperty("node").GetProperty("id").GetString());
                }
            }
        }

        [Fact]
        public async Task AllAxisDimensions_ShouldHaveConcepts()
        {
            // Get all axis nodes
            var axes = await GetNodesByType("codex.axis.universal");
            
            foreach (var axis in axes)
            {
                var axisId = axis.GetProperty("id").GetString();
                var dimensions = axis.GetProperty("content").Deserialize<JsonElement>()
                    .GetProperty("dimensions").EnumerateArray()
                    .Select(d => d.GetString())
                    .ToList();

                foreach (var dimension in dimensions)
                {
                    var conceptId = $"u-core-concept-{dimension}";
                    var concept = await GetNode(conceptId);
                    Assert.NotNull(concept);
                    Assert.Equal(conceptId, concept.Value.GetProperty("node").GetProperty("id").GetString());
                }
            }
        }

        private async Task<JsonElement?> GetNode(string nodeId)
        {
            try
            {
                var response = await _client.GetAsync($"/storage-endpoints/nodes/{nodeId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<JsonElement>(content);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<List<JsonElement>> GetNodesByType(string typeId)
        {
            try
            {
                var response = await _client.GetAsync($"/storage-endpoints/nodes?typeId={typeId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    return result.GetProperty("nodes").EnumerateArray().ToList();
                }
                return new List<JsonElement>();
            }
            catch
            {
                return new List<JsonElement>();
            }
        }

            private async Task<bool> WalkToIdentity(string nodeId, HashSet<string> visited, List<string> path, List<EdgeInfo> edges, int maxDepth = 10, int currentDepth = 0)
            {
                if (visited.Contains(nodeId) || currentDepth >= maxDepth)
                    return false;
                    
                visited.Add(nodeId);
                path.Add(nodeId);

                // Check if we found the identity
                if (nodeId == "u-core-meta-identity")
                {
                    Console.WriteLine($"Found identity at depth {currentDepth}!");
                    return true;
                }

                // Get all edges for this node
                var nodeEdges = await GetNodeEdges(nodeId);
                
                // Debug logging
                Console.WriteLine($"Node {nodeId} has {nodeEdges.Count} edges (depth {currentDepth}):");
                foreach (var edge in nodeEdges)
                {
                    Console.WriteLine($"  {edge.SourceId} -> {edge.TargetId} ({edge.Role})");
                }
                
                // Prioritize edges that move us toward the identity concept
                // 1. First try "is_a" relationships (hierarchy up)
                // 2. Then try "has_child" relationships (hierarchy up from child perspective)
                // 3. Then try axis relationships that might lead to higher-level concepts
                // 4. Finally try other relationships
                var prioritizedEdges = nodeEdges.OrderBy(e => GetEdgePriority(e.Role)).ToList();
                
                foreach (var edge in prioritizedEdges)
                {
                    edges.Add(edge);
                    
                    // Follow the target direction (this will be "up" the hierarchy for is_a relationships)
                    var targetId = edge.TargetId;
                    if (!visited.Contains(targetId))
                    {
                        if (await WalkToIdentity(targetId, visited, path, edges, maxDepth, currentDepth + 1))
                            return true;
                    }
                }

                path.RemoveAt(path.Count - 1);
                return false;
            }

            private int GetEdgePriority(string role)
            {
                return role switch
                {
                    "is_a" => 1,  // Highest priority - moves up hierarchy
                    "has_child" => 2,  // High priority - parent-child relationship
                    "concept_on_axis" => 3,  // Medium priority - might lead to higher concepts
                    "axis_has_keyword" => 4,  // Medium priority - axis relationships
                    "axis_has_dimension" => 4,  // Medium priority - axis relationships
                    "belongs_to_axis" => 5,  // Lower priority - axis membership
                    "instance-of" => 6,  // Lower priority - type relationships
                    "defines" => 7,  // Lower priority - definition relationships
                    "has_content" => 8,  // Lower priority - content relationships
                    "has_content_type" => 8,  // Lower priority - content relationships
                    "maps_to_axis" => 9,  // Lower priority - mapping relationships
                    "maps_to_concept" => 9,  // Lower priority - mapping relationships
                    "identity" => 10,  // Lowest priority - self-referential
                    _ => 5  // Default priority for unknown roles
                };
            }

        private async Task<List<EdgeInfo>> GetNodeEdges(string nodeId)
        {
            try
            {
                var response = await _client.GetAsync($"/storage-endpoints/edges?FromId={nodeId}");
                var edges = new List<EdgeInfo>();
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    foreach (var edge in result.GetProperty("edges").EnumerateArray())
                    {
                        edges.Add(new EdgeInfo
                        {
                            SourceId = edge.GetProperty("fromId").GetString(),
                            TargetId = edge.GetProperty("toId").GetString(),
                            Role = edge.GetProperty("role").GetString()
                        });
                    }
                }

                // Also get incoming edges
                response = await _client.GetAsync($"/storage-endpoints/edges?ToId={nodeId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    
                    foreach (var edge in result.GetProperty("edges").EnumerateArray())
                    {
                        edges.Add(new EdgeInfo
                        {
                            SourceId = edge.GetProperty("fromId").GetString(),
                            TargetId = edge.GetProperty("toId").GetString(),
                            Role = edge.GetProperty("role").GetString()
                        });
                    }
                }

                return edges;
            }
            catch
            {
                return new List<EdgeInfo>();
            }
        }

        private class EdgeInfo
        {
            public string SourceId { get; set; } = string.Empty;
            public string TargetId { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
