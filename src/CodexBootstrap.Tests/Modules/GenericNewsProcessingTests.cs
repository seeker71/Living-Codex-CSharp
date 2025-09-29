using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Modules;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    /// <summary>
    /// Generic news processing tests that follow the Living Codex specification architecture.
    /// Tests the complete flow using generic storage endpoints and node-based architecture.
    /// </summary>
    public class GenericNewsProcessingTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:5002";

        public GenericNewsProcessingTests()
        {
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task NewsProcessingPipeline_ShouldFollowGenericNodeArchitecture()
        {
            // Test follows "Everything is a Node" principle using generic storage endpoints
            
            // 1. Get latest news items using generic news endpoint
            var newsResponse = await _httpClient.GetAsync($"{_baseUrl}/news/latest");
            newsResponse.IsSuccessStatusCode.Should().BeTrue();

            var newsJson = await newsResponse.Content.ReadAsStringAsync();
            var newsData = JsonSerializer.Deserialize<NewsLatestResponse>(newsJson);
            newsData.Should().NotBeNull();
            newsData.Items.Should().NotBeEmpty();

            var newsItem = newsData.Items[0];
            newsItem.Id.Should().NotBeNullOrEmpty();

            // 2. Get the news item node using generic storage endpoint
            var nodeResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{newsItem.Id}");
            nodeResponse.IsSuccessStatusCode.Should().BeTrue();

            var nodeJson = await nodeResponse.Content.ReadAsStringAsync();
            var newsNode = JsonSerializer.Deserialize<GenericNode>(nodeJson);
            newsNode.Should().NotBeNull();
            newsNode.TypeId.Should().Be("codex.news.item");

            // 3. Verify node follows specification structure
            newsNode.State.Should().BeOneOf("ice", "water", "gas");
            newsNode.Meta.Should().NotBeNull();
            newsNode.Content.Should().NotBeNull();

            // 4. Get related nodes using generic storage endpoints
            var edgesResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/edges?nodeId={newsItem.Id}");
            edgesResponse.IsSuccessStatusCode.Should().BeTrue();

            var edgesJson = await edgesResponse.Content.ReadAsStringAsync();
            var edges = JsonSerializer.Deserialize<EdgeListResponse>(edgesJson);
            edges.Should().NotBeNull();

            // 5. Verify content and summary nodes exist and are accessible
            var contentNodeId = GetNodeIdFromMeta(newsNode.Meta, "contentNodeId");
            var summaryNodeId = GetNodeIdFromMeta(newsNode.Meta, "summaryNodeId");

            contentNodeId.Should().NotBeNullOrEmpty();
            summaryNodeId.Should().NotBeNullOrEmpty();

            // 6. Verify content node using generic storage endpoint
            var contentResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{contentNodeId}");
            contentResponse.IsSuccessStatusCode.Should().BeTrue();

            var contentJson = await contentResponse.Content.ReadAsStringAsync();
            var contentNode = JsonSerializer.Deserialize<GenericNode>(contentJson);
            contentNode.Should().NotBeNull();
            contentNode.TypeId.Should().Be("codex.content.extracted");

            // 7. Verify summary node using generic storage endpoint
            var summaryResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{summaryNodeId}");
            summaryResponse.IsSuccessStatusCode.Should().BeTrue();

            var summaryJson = await summaryResponse.Content.ReadAsStringAsync();
            var summaryNode = JsonSerializer.Deserialize<GenericNode>(summaryJson);
            summaryNode.Should().NotBeNull();
            summaryNode.TypeId.Should().Be("codex.content.summary");

            // 8. Verify nodes follow the specification's node lifecycle
            contentNode.State.Should().BeOneOf("ice", "water", "gas");
            summaryNode.State.Should().BeOneOf("ice", "water", "gas");

            // 9. Verify content quality
            var contentData = JsonSerializer.Deserialize<ContentData>(contentNode.Content.InlineJson);
            var summaryData = summaryNode.Content.InlineJson;

            contentData.Should().NotBeNull();
            contentData.Content.Should().NotBeNullOrEmpty();
            summaryData.Length.Should().BeLessThan(contentData.Content.Length);
        }

        [Fact]
        public async Task NewsProcessingPipeline_ShouldHaveValidNodeRelationships()
        {
            // Test node relationships using generic storage endpoints
            
            // 1. Get a news item
            var newsResponse = await _httpClient.GetAsync($"{_baseUrl}/news/latest");
            var newsJson = await newsResponse.Content.ReadAsStringAsync();
            var newsData = JsonSerializer.Deserialize<NewsLatestResponse>(newsJson);
            var newsItem = newsData.Items[0];

            // 2. Get the news node
            var nodeResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{newsItem.Id}");
            var nodeJson = await nodeResponse.Content.ReadAsStringAsync();
            var newsNode = JsonSerializer.Deserialize<GenericNode>(nodeJson);

            // 3. Get edges from this node
            var edgesFromResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/edges?fromId={newsItem.Id}");
            edgesFromResponse.IsSuccessStatusCode.Should().BeTrue();

            var edgesFromJson = await edgesFromResponse.Content.ReadAsStringAsync();
            var edgesFrom = JsonSerializer.Deserialize<EdgeListResponse>(edgesFromJson);

            // 4. Get edges to this node
            var edgesToResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/edges?toId={newsItem.Id}");
            edgesToResponse.IsSuccessStatusCode.Should().BeTrue();

            var edgesToJson = await edgesToResponse.Content.ReadAsStringAsync();
            var edgesTo = JsonSerializer.Deserialize<EdgeListResponse>(edgesToJson);

            // 5. Verify edge structure follows specification
            if (edgesFrom.Edges.Count > 0)
            {
                var edge = edgesFrom.Edges[0];
                edge.FromId.Should().Be(newsItem.Id);
                edge.ToId.Should().NotBeNullOrEmpty();
                edge.Role.Should().NotBeNullOrEmpty();
            }

            if (edgesTo.Edges.Count > 0)
            {
                var edge = edgesTo.Edges[0];
                edge.ToId.Should().Be(newsItem.Id);
                edge.FromId.Should().NotBeNullOrEmpty();
                edge.Role.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task NewsProcessingPipeline_ShouldSupportConceptResonanceAndTranslation()
        {
            // Test concept extraction, resonance matching, and translation for greater good
            
            // 1. Get a news item with content
            var newsResponse = await _httpClient.GetAsync($"{_baseUrl}/news/latest");
            var newsJson = await newsResponse.Content.ReadAsStringAsync();
            var newsData = JsonSerializer.Deserialize<NewsLatestResponse>(newsJson);
            var newsItem = newsData.Items[0];

            // 2. Get the news node and its content
            var nodeResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{newsItem.Id}");
            var nodeJson = await nodeResponse.Content.ReadAsStringAsync();
            var newsNode = JsonSerializer.Deserialize<GenericNode>(nodeJson);

            var contentNodeId = GetNodeIdFromMeta(newsNode.Meta, "contentNodeId");
            var contentResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes/{contentNodeId}");
            var contentJson = await contentResponse.Content.ReadAsStringAsync();
            var contentNode = JsonSerializer.Deserialize<GenericNode>(contentJson);

            var contentData = JsonSerializer.Deserialize<ContentData>(contentNode.Content.InlineJson);

            // 3. Extract concepts using generic AI endpoint
            var conceptRequest = new
            {
                content = contentData.Content,
                title = newsItem.Title
            };

            var conceptResponse = await _httpClient.PostAsync($"{_baseUrl}/ai/extract-concepts",
                new StringContent(JsonSerializer.Serialize(conceptRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            conceptResponse.IsSuccessStatusCode.Should().BeTrue();

            var conceptJson = await conceptResponse.Content.ReadAsStringAsync();
            var conceptResult = JsonSerializer.Deserialize<ConceptExtractionResponse>(conceptJson);
            conceptResult.Should().NotBeNull();
            conceptResult.Success.Should().BeTrue();
            conceptResult.Data.Concepts.Should().NotBeEmpty();

            // 4. Test concept resonance matching with user concepts
            var resonanceRequest = new
            {
                extractedConcepts = conceptResult.Data.Concepts,
                userConcepts = new[] { "sustainability", "innovation", "consciousness", "technology", "environment" },
                userId = "test-user-123"
            };

            var resonanceResponse = await _httpClient.PostAsync($"{_baseUrl}/concepts/resonance/compare",
                new StringContent(JsonSerializer.Serialize(resonanceRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            resonanceResponse.IsSuccessStatusCode.Should().BeTrue();

            var resonanceJson = await resonanceResponse.Content.ReadAsStringAsync();
            var resonanceResult = JsonSerializer.Deserialize<ConceptResonanceResponse>(resonanceJson);
            resonanceResult.Should().NotBeNull();
            resonanceResult.Success.Should().BeTrue();
            resonanceResult.ResonanceScores.Should().NotBeEmpty();

            // 5. Verify resonance scores show varying levels of match
            var maxResonance = resonanceResult.ResonanceScores.Max(r => r.Score);
            var minResonance = resonanceResult.ResonanceScores.Min(r => r.Score);
            maxResonance.Should().BeGreaterThan(minResonance);
            maxResonance.Should().BeInRange(0.0, 1.0);

            // 6. Test translation for greater good and human evolution
            var translationRequest = new
            {
                originalSummary = contentData.Content,
                extractedConcepts = conceptResult.Data.Concepts,
                resonanceScores = resonanceResult.ResonanceScores,
                userInterests = new[] { "consciousness", "evolution", "collective good", "innovation" },
                translationPurpose = "greater_good_human_evolution"
            };

            var translationResponse = await _httpClient.PostAsync($"{_baseUrl}/ai/translate-for-purpose",
                new StringContent(JsonSerializer.Serialize(translationRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            translationResponse.IsSuccessStatusCode.Should().BeTrue();

            var translationJson = await translationResponse.Content.ReadAsStringAsync();
            var translationResult = JsonSerializer.Deserialize<TranslationForGoodResponse>(translationJson);
            translationResult.Should().NotBeNull();
            translationResult.Success.Should().BeTrue();
            translationResult.TranslatedSummary.Should().NotBeNullOrEmpty();

            // 7. Verify the translated summary addresses human evolution and greater good
            var translatedText = translationResult.TranslatedSummary.ToLower();
            var evolutionKeywords = new[] { "evolution", "growth", "development", "progress", "advancement", "consciousness", "awareness", "collective", "humanity", "benefit", "good" };
            var hasEvolutionContext = evolutionKeywords.Any(keyword => translatedText.Contains(keyword));
            hasEvolutionContext.Should().BeTrue();

            // 8. Test U-CORE ontology integration for concept placement
            var ontologyRequest = new
            {
                concepts = conceptResult.Data.Concepts,
                resonanceScores = resonanceResult.ResonanceScores
            };

            var ontologyResponse = await _httpClient.PostAsync($"{_baseUrl}/ucore/align",
                new StringContent(JsonSerializer.Serialize(ontologyRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            ontologyResponse.IsSuccessStatusCode.Should().BeTrue();

            var ontologyJson = await ontologyResponse.Content.ReadAsStringAsync();
            var ontologyResult = JsonSerializer.Deserialize<UcoreAlignmentResponse>(ontologyJson);
            ontologyResult.Should().NotBeNull();
            ontologyResult.Success.Should().BeTrue();
            ontologyResult.AlignedConcepts.Should().NotBeEmpty();

            // 9. Verify concepts are aligned to U-CORE axes
            foreach (var alignedConcept in ontologyResult.AlignedConcepts)
            {
                alignedConcept.Axis.Should().NotBeNullOrEmpty();
                alignedConcept.Frequency.Should().BeGreaterThan(0);
                alignedConcept.SacredFrequency.Should().NotBeNullOrEmpty();
            }

            // 10. Generate final user-facing summary
            var userSummaryRequest = new
            {
                newsTitle = newsItem.Title,
                originalContent = contentData.Content,
                translatedSummary = translationResult.TranslatedSummary,
                topResonantConcepts = resonanceResult.ResonanceScores
                    .OrderByDescending(r => r.Score)
                    .Take(3)
                    .Select(r => r.Concept),
                userInterests = new[] { "consciousness", "evolution", "collective good" },
                format = "user_friendly"
            };

            var userSummaryResponse = await _httpClient.PostAsync($"{_baseUrl}/ai/generate-user-summary",
                new StringContent(JsonSerializer.Serialize(userSummaryRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            userSummaryResponse.IsSuccessStatusCode.Should().BeTrue();

            var userSummaryJson = await userSummaryResponse.Content.ReadAsStringAsync();
            var userSummaryResult = JsonSerializer.Deserialize<UserSummaryResponse>(userSummaryJson);
            userSummaryResult.Should().NotBeNull();
            userSummaryResult.Success.Should().BeTrue();
            userSummaryResult.UserFacingSummary.Should().NotBeNullOrEmpty();
            userSummaryResult.EvolutionImpact.Should().NotBeNullOrEmpty();
            userSummaryResult.CollectiveBenefit.Should().NotBeNullOrEmpty();

            // 11. Verify user summary is actionable and inspiring
            var userSummary = userSummaryResult.UserFacingSummary.ToLower();
            var actionableKeywords = new[] { "can", "will", "helps", "supports", "enables", "empowers", "benefits", "contributes" };
            var hasActionableLanguage = actionableKeywords.Any(keyword => userSummary.Contains(keyword));
            hasActionableLanguage.Should().BeTrue();
        }

        [Fact]
        public async Task NewsProcessingPipeline_ShouldFollowUCoreOntology()
        {
            // Test U-CORE ontology integration using generic storage endpoints
            
            // 1. Get U-CORE ontology axes
            var axesResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes?typeId=codex.ontology.axis");
            axesResponse.IsSuccessStatusCode.Should().BeTrue();

            var axesJson = await axesResponse.Content.ReadAsStringAsync();
            var axesElement = JsonSerializer.Deserialize<JsonElement>(axesJson);
            axesElement.Should().NotBeNull();
            
            // Check if the response has nodes array
            if (axesElement.TryGetProperty("nodes", out var nodesArray))
            {
                nodesArray.GetArrayLength().Should().BeGreaterThan(0);
                
                // 2. Verify U-CORE axes exist
                var axisNames = new HashSet<string>();
                foreach (var axisElement in nodesArray.EnumerateArray())
                {
                    if (axisElement.TryGetProperty("typeId", out var typeId))
                    {
                        typeId.GetString().Should().Be("codex.ontology.axis");
                    }
                    
                    if (axisElement.TryGetProperty("state", out var state))
                    {
                        state.GetString().Should().BeOneOf("ice", "water", "gas");
                    }
                    
                    if (axisElement.TryGetProperty("meta", out var meta) && 
                        meta.TryGetProperty("name", out var name))
                    {
                        axisNames.Add(name.GetString() ?? "");
                    }
                }

                // 3. Verify we have a good number of U-CORE axes
                Assert.True(axisNames.Count >= 20, $"Should have at least 20 U-CORE axes, but found {axisNames.Count}");

                // Verify some core axes exist (using the actual names from the ontology)
                Assert.Contains(axisNames, name => name.Contains("Consciousness"));
                Assert.Contains(axisNames, name => name.Contains("Quantum"));
                Assert.Contains(axisNames, name => name.Contains("Academic"));
            }

            // 4. Get concepts and verify they can be linked to U-CORE axes
            var conceptsResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes?typeId=codex.concept");
            conceptsResponse.IsSuccessStatusCode.Should().BeTrue();

            var conceptsJson = await conceptsResponse.Content.ReadAsStringAsync();
            var conceptsElement = JsonSerializer.Deserialize<JsonElement>(conceptsJson);
            
            if (conceptsElement.TryGetProperty("nodes", out var conceptsArray))
            {
                if (conceptsArray.GetArrayLength() > 0)
                {
                    var conceptElement = conceptsArray.EnumerateArray().First();
                    if (conceptElement.TryGetProperty("typeId", out var conceptTypeId))
                    {
                        conceptTypeId.GetString().Should().Be("codex.concept");
                    }
                    if (conceptElement.TryGetProperty("state", out var conceptState))
                    {
                        conceptState.GetString().Should().BeOneOf("ice", "water", "gas");
                    }
                }
            }
        }

        [Fact]
        public async Task NewsProcessingPipeline_ShouldSupportGenericNodeOperations()
        {
            // Test generic node operations following the specification
            
            // 1. Get all nodes using generic storage endpoint
            var allNodesResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes");
            allNodesResponse.IsSuccessStatusCode.Should().BeTrue();

            var allNodesJson = await allNodesResponse.Content.ReadAsStringAsync();
            var allNodesElement = JsonSerializer.Deserialize<JsonElement>(allNodesJson);
            allNodesElement.Should().NotBeNull();
            
            if (allNodesElement.TryGetProperty("nodes", out var allNodesArray))
            {
                allNodesArray.GetArrayLength().Should().BeGreaterThan(0);

                // 2. Verify node structure follows specification
                var nodeCount = 0;
                foreach (var nodeElement in allNodesArray.EnumerateArray())
                {
                    if (nodeCount >= 5) break; // Check first 5 nodes
                    
                    if (nodeElement.TryGetProperty("id", out var nodeId))
                    {
                        nodeId.GetString().Should().NotBeNullOrEmpty();
                    }
                    if (nodeElement.TryGetProperty("typeId", out var nodeTypeId))
                    {
                        nodeTypeId.GetString().Should().NotBeNullOrEmpty();
                    }
                    if (nodeElement.TryGetProperty("state", out var nodeState))
                    {
                        nodeState.GetString().Should().BeOneOf("ice", "water", "gas");
                    }
                    nodeElement.TryGetProperty("meta", out var nodeMeta);
                    nodeMeta.ValueKind.Should().NotBe(JsonValueKind.Undefined);
                    nodeElement.TryGetProperty("content", out var nodeContent);
                    nodeContent.ValueKind.Should().NotBe(JsonValueKind.Undefined);
                    
                    nodeCount++;
                }
            }

            // 3. Test node type filtering
            var newsNodesResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/nodes?typeId=codex.news.item");
            newsNodesResponse.IsSuccessStatusCode.Should().BeTrue();

            var newsNodesJson = await newsNodesResponse.Content.ReadAsStringAsync();
            var newsNodesElement = JsonSerializer.Deserialize<JsonElement>(newsNodesJson);
            
            if (newsNodesElement.TryGetProperty("nodes", out var newsNodesArray))
            {
                if (newsNodesArray.GetArrayLength() > 0)
                {
                    foreach (var nodeElement in newsNodesArray.EnumerateArray())
                    {
                        if (nodeElement.TryGetProperty("typeId", out var nodeTypeId))
                        {
                            nodeTypeId.GetString().Should().Be("codex.news.item");
                        }
                    }
                }
            }

            // 4. Test edge operations
            var edgesResponse = await _httpClient.GetAsync($"{_baseUrl}/storage-endpoints/edges");
            edgesResponse.IsSuccessStatusCode.Should().BeTrue();

            var edgesJson = await edgesResponse.Content.ReadAsStringAsync();
            var edges = JsonSerializer.Deserialize<EdgeListResponse>(edgesJson);
            edges.Should().NotBeNull();

            if (edges.Edges.Count > 0)
            {
                var edge = edges.Edges[0];
                edge.FromId.Should().NotBeNullOrEmpty();
                edge.ToId.Should().NotBeNullOrEmpty();
                edge.Role.Should().NotBeNullOrEmpty();
            }
        }

        private string GetNodeIdFromMeta(Dictionary<string, object> meta, string key)
        {
            if (meta.ContainsKey(key) && meta[key] is string nodeId)
            {
                return nodeId;
            }
            return string.Empty;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Generic data models following the specification
    public class NewsLatestResponse
    {
        public List<NewsItem> Items { get; set; } = new();
    }


    public class GenericNode
    {
        public string Id { get; set; } = "";
        public string TypeId { get; set; } = "";
        public string State { get; set; } = "";
        public string Locale { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public NodeContent Content { get; set; } = new();
        public Dictionary<string, object> Meta { get; set; } = new();
    }

    public class NodeContent
    {
        public string MediaType { get; set; } = "";
        public string InlineJson { get; set; } = "";
    }


    public class EdgeListResponse
    {
        public List<GenericEdge> Edges { get; set; } = new();
    }

    public class GenericEdge
    {
        public string FromId { get; set; } = "";
        public string ToId { get; set; } = "";
        public string Role { get; set; } = "";
        public Dictionary<string, object> Meta { get; set; } = new();
    }

    public class ContentData
    {
        public string Content { get; set; } = "";
        public string Title { get; set; } = "";
        public string Source { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class ConceptExtractionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public ConceptData Data { get; set; } = new();
    }

    public class ConceptData
    {
        public List<string> Concepts { get; set; } = new();
        public double Confidence { get; set; }
        public string Model { get; set; } = "";
    }

    public class ConceptResonanceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<ResonanceScore> ResonanceScores { get; set; } = new();
    }

    public class ResonanceScore
    {
        public string Concept { get; set; } = "";
        public double Score { get; set; }
        public string UserConcept { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    public class TranslationForGoodResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string TranslatedSummary { get; set; } = "";
        public string EvolutionContext { get; set; } = "";
        public string CollectiveImpact { get; set; } = "";
    }

    public class UcoreAlignmentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<AlignedConcept> AlignedConcepts { get; set; } = new();
    }

    public class AlignedConcept
    {
        public string Concept { get; set; } = "";
        public string Axis { get; set; } = "";
        public double Frequency { get; set; }
        public string SacredFrequency { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class UserSummaryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string UserFacingSummary { get; set; } = "";
        public string EvolutionImpact { get; set; } = "";
        public string CollectiveBenefit { get; set; } = "";
        public List<string> ActionableInsights { get; set; } = new();
        public string ResonanceExplanation { get; set; } = "";
    }
}
