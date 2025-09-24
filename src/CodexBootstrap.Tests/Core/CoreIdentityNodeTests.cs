using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using System.Text.Json;
using System.Net.Http;

namespace CodexBootstrap.Tests.Core;

/// <summary>
/// Comprehensive tests for the core identity node system and node graph traversal
/// This test validates that all nodes can walk back to the core identity node
/// </summary>
public class CoreIdentityNodeTests
{
    private readonly INodeRegistry _registry;
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;

    public CoreIdentityNodeTests()
    {
        _registry = TestInfrastructure.CreateTestNodeRegistry();
        _logger = TestInfrastructure.CreateTestLogger();
        _httpClient = new HttpClient();
        _registry.InitializeAsync().Wait();
    }

    /// <summary>
    /// Test that validates the complete node graph traversal from any node back to the core identity
    /// This test creates a news item and traces its path back through all related nodes to the core identity
    /// </summary>
    [Fact]
    public async Task NodeGraphTraversal_Should_WalkBackToCoreIdentity()
    {
        // Arrange - Create the core identity node first
        var coreIdentityNode = CreateCoreIdentityNode();
        _registry.Upsert(coreIdentityNode);

        // Create U-CORE ontology root
        var ucoreRootNode = CreateUCoreOntologyRootNode();
        _registry.Upsert(ucoreRootNode);

        // Create edge from U-CORE root to core identity
        var ucoreToIdentityEdge = NodeHelpers.CreateEdge(
            ucoreRootNode.Id, 
            coreIdentityNode.Id, 
            "belongs_to", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "ontology-root-to-identity" }
        );
        _registry.Upsert(ucoreToIdentityEdge);

        // Create a news source
        var newsSourceNode = CreateNewsSourceNode();
        _registry.Upsert(newsSourceNode);

        // Create edge from news source to U-CORE root
        var sourceToUcoreEdge = NodeHelpers.CreateEdge(
            newsSourceNode.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "news-source-to-ontology" }
        );
        _registry.Upsert(sourceToUcoreEdge);

        // Create a news item
        var newsItemNode = CreateNewsItemNode(newsSourceNode.Id);
        _registry.Upsert(newsItemNode);

        // Create edge from news item to news source
        var itemToSourceEdge = NodeHelpers.CreateEdge(
            newsItemNode.Id, 
            newsSourceNode.Id, 
            "published_by", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "news-item-to-source" }
        );
        _registry.Upsert(itemToSourceEdge);

        // Create extracted news content
        var newsContentNode = CreateNewsContentNode(newsItemNode.Id);
        _registry.Upsert(newsContentNode);

        // Create edge from content to news item
        var contentToItemEdge = NodeHelpers.CreateEdge(
            newsContentNode.Id, 
            newsItemNode.Id, 
            "extracted_from", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "content-to-news-item" }
        );
        _registry.Upsert(contentToItemEdge);

        // Create news summary
        var newsSummaryNode = CreateNewsSummaryNode(newsContentNode.Id);
        _registry.Upsert(newsSummaryNode);

        // Create edge from summary to content
        var summaryToContentEdge = NodeHelpers.CreateEdge(
            newsSummaryNode.Id, 
            newsContentNode.Id, 
            "summarizes", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "summary-to-content" }
        );
        _registry.Upsert(summaryToContentEdge);

        // Create extracted concepts
        var concept1Node = CreateConceptNode("technology", newsSummaryNode.Id);
        var concept2Node = CreateConceptNode("innovation", newsSummaryNode.Id);
        _registry.Upsert(concept1Node);
        _registry.Upsert(concept2Node);

        // Create edges from concepts to summary
        var concept1ToSummaryEdge = NodeHelpers.CreateEdge(
            concept1Node.Id, 
            newsSummaryNode.Id, 
            "extracted_from", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "concept-to-summary" }
        );
        var concept2ToSummaryEdge = NodeHelpers.CreateEdge(
            concept2Node.Id, 
            newsSummaryNode.Id, 
            "extracted_from", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "concept-to-summary" }
        );
        _registry.Upsert(concept1ToSummaryEdge);
        _registry.Upsert(concept2ToSummaryEdge);

        // Create edges from concepts to U-CORE root
        var concept1ToUcoreEdge = NodeHelpers.CreateEdge(
            concept1Node.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "concept-to-ontology" }
        );
        var concept2ToUcoreEdge = NodeHelpers.CreateEdge(
            concept2Node.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "concept-to-ontology" }
        );
        _registry.Upsert(concept1ToUcoreEdge);
        _registry.Upsert(concept2ToUcoreEdge);

        // Create generated image
        var generatedImageNode = CreateGeneratedImageNode(concept1Node.Id, concept2Node.Id);
        _registry.Upsert(generatedImageNode);

        // Create edges from image to concepts
        var imageToConcept1Edge = NodeHelpers.CreateEdge(
            generatedImageNode.Id, 
            concept1Node.Id, 
            "visualizes", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "image-to-concept" }
        );
        var imageToConcept2Edge = NodeHelpers.CreateEdge(
            generatedImageNode.Id, 
            concept2Node.Id, 
            "visualizes", 
            1.0, 
            new Dictionary<string, object> { ["relationship"] = "image-to-concept" }
        );
        _registry.Upsert(imageToConcept1Edge);
        _registry.Upsert(imageToConcept2Edge);

        // Act - Test traversal from generated image back to core identity
        var traversalPath = await TraverseNodeGraphToIdentity(generatedImageNode.Id);

        // Assert - Verify the complete traversal path
        Assert.NotNull(traversalPath);
        Assert.True(traversalPath.Count >= 5, $"Expected at least 5 nodes in traversal path, found {traversalPath.Count}");

        // Verify the path includes all expected nodes
        var nodeIds = traversalPath.Select(n => n.Id).ToList();
        Assert.Contains(generatedImageNode.Id, nodeIds);
        Assert.Contains(concept1Node.Id, nodeIds);
        Assert.Contains(concept2Node.Id, nodeIds);
        Assert.Contains(newsSummaryNode.Id, nodeIds);
        Assert.Contains(newsContentNode.Id, nodeIds);
        Assert.Contains(newsItemNode.Id, nodeIds);
        Assert.Contains(newsSourceNode.Id, nodeIds);
        Assert.Contains(ucoreRootNode.Id, nodeIds);
        Assert.Contains(coreIdentityNode.Id, nodeIds);

        // Verify the image node is valid
        var imageNode = _registry.GetNodesByType("codex.image.generated").FirstOrDefault(n => n.Id == generatedImageNode.Id);
        Assert.NotNull(imageNode);
        Assert.True(ValidateImageNode(imageNode), "Generated image node should be valid");

        // Verify all nodes in the path have proper edges
        foreach (var node in traversalPath)
        {
            var edges = GetOutgoingEdges(node.Id);
            Assert.True(edges.Any(), $"Node {node.Id} should have at least one outgoing edge");
        }

        _logger.Info($"Successfully traversed from image node {generatedImageNode.Id} back to core identity {coreIdentityNode.Id} through {traversalPath.Count} nodes");
    }

    /// <summary>
    /// Test that validates the core identity node is created first and is the root of all other nodes
    /// </summary>
    [Fact]
    public async Task CoreIdentityNode_Should_BeCreatedFirst()
    {
        // Arrange
        var coreIdentityNode = CreateCoreIdentityNode();

        // Act - Create core identity first
        _registry.Upsert(coreIdentityNode);

        // Create other nodes
        var ucoreRootNode = CreateUCoreOntologyRootNode();
        _registry.Upsert(ucoreRootNode);

        var newsSourceNode = CreateNewsSourceNode();
        _registry.Upsert(newsSourceNode);

        // Create edges to core identity
        var ucoreToIdentityEdge = NodeHelpers.CreateEdge(
            ucoreRootNode.Id, 
            coreIdentityNode.Id, 
            "belongs_to", 
            1.0
        );
        _registry.Upsert(ucoreToIdentityEdge);

        var sourceToUcoreEdge = NodeHelpers.CreateEdge(
            newsSourceNode.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0
        );
        _registry.Upsert(sourceToUcoreEdge);

        // Assert - Verify core identity exists and is the root
        var retrievedIdentity = _registry.GetNodeAsync(coreIdentityNode.Id).Result;
        Assert.NotNull(retrievedIdentity);
        Assert.Equal(coreIdentityNode.Id, retrievedIdentity.Id);
        Assert.Equal("codex.core.identity", retrievedIdentity.TypeId);

        // Verify all other nodes can reach the core identity
        var allNodes = _registry.AllNodes().ToList();
        foreach (var node in allNodes.Where(n => n.Id != coreIdentityNode.Id))
        {
            var canReachIdentity = await CanReachNode(node.Id, coreIdentityNode.Id);
            Assert.True(canReachIdentity, $"Node {node.Id} should be able to reach core identity");
        }
    }

    /// <summary>
    /// Test that validates all nodes have proper edges and can walk back to the identity node
    /// </summary>
    [Fact]
    public async Task AllNodes_Should_HaveEdgesToIdentity()
    {
        // Arrange - Create a complete node graph
        await CreateCompleteNodeGraph();

        // Act - Get all nodes and verify they can reach the core identity
        var allNodes = _registry.AllNodes().ToList();
        var coreIdentityNodes = allNodes.Where(n => n.TypeId == "codex.core.identity").ToList();
        
        Assert.True(coreIdentityNodes.Any(), "Should have at least one core identity node");
        var coreIdentity = coreIdentityNodes.First();

        // Assert - Verify all nodes can reach the core identity
        var nodesWithoutPath = new List<string>();
        
        foreach (var node in allNodes.Where(n => n.Id != coreIdentity.Id))
        {
            var canReachIdentity = await CanReachNode(node.Id, coreIdentity.Id);
            if (!canReachIdentity)
            {
                nodesWithoutPath.Add(node.Id);
            }
        }

        Assert.True(nodesWithoutPath.Count == 0, 
            $"Nodes without path to core identity: {string.Join(", ", nodesWithoutPath)}");

        _logger.Info($"All {allNodes.Count} nodes can reach the core identity node");
    }

    #region Helper Methods

    private Node CreateCoreIdentityNode()
    {
        return NodeHelpers.CreateNode(
            "codex.core.identity.root",
            "codex.core.identity",
            ContentState.Ice,
            "Core Identity Root",
            "The root identity node that all other nodes should link back to",
            NodeHelpers.CreateJsonContent(new
            {
                id = "codex.core.identity.root",
                name = "Core Identity",
                description = "Root identity node for the Living Codex system",
                created = DateTime.UtcNow,
                version = "1.0.0"
            }),
            new Dictionary<string, object>
            {
                ["isCoreIdentity"] = true,
                ["isRoot"] = true,
                ["systemVersion"] = "1.0.0"
            }
        );
    }

    private Node CreateUCoreOntologyRootNode()
    {
        return NodeHelpers.CreateNode(
            "u-core-ontology-root",
            "codex.ontology.root",
            ContentState.Ice,
            "U-CORE Ontology Root",
            "Root node for U-CORE ontology system",
            NodeHelpers.CreateJsonContent(new
            {
                id = "u-core-ontology-root",
                name = "u-core",
                version = "1.0.0"
            }),
            new Dictionary<string, object>
            {
                ["ontologyType"] = "root",
                ["version"] = "1.0.0"
            }
        );
    }

    private Node CreateNewsSourceNode()
    {
        return NodeHelpers.CreateNode(
            "news-source-techcrunch",
            "codex.news.source",
            ContentState.Ice,
            "TechCrunch News Source",
            "TechCrunch technology news source",
            NodeHelpers.CreateJsonContent(new
            {
                id = "techcrunch",
                name = "TechCrunch",
                url = "https://techcrunch.com/feed/",
                description = "Technology news and startup coverage",
                category = "technology"
            }),
            new Dictionary<string, object>
            {
                ["sourceType"] = "RSS",
                ["category"] = "technology",
                ["reliability"] = "high"
            }
        );
    }

    private Node CreateNewsItemNode(string sourceId)
    {
        return NodeHelpers.CreateNode(
            "news-item-tech-innovation",
            "codex.news.item",
            ContentState.Ice,
            "Tech Innovation News Item",
            "Latest technology innovation news item",
            NodeHelpers.CreateJsonContent(new
            {
                id = "news-item-tech-innovation",
                title = "Revolutionary AI Technology Breakthrough",
                content = "Scientists have developed a new AI system that can...",
                url = "https://techcrunch.com/2024/01/ai-breakthrough",
                publishedAt = DateTime.UtcNow,
                source = sourceId
            }),
            new Dictionary<string, object>
            {
                ["sourceId"] = sourceId,
                ["category"] = "technology",
                ["importance"] = "high"
            }
        );
    }

    private Node CreateNewsContentNode(string newsItemId)
    {
        return NodeHelpers.CreateNode(
            "news-content-extracted",
            "codex.news.content",
            ContentState.Water,
            "Extracted News Content",
            "Extracted and processed news content",
            NodeHelpers.CreateJsonContent(new
            {
                id = "news-content-extracted",
                extractedText = "Scientists have developed a new AI system that can process natural language with unprecedented accuracy...",
                entities = new[] { "AI", "technology", "innovation", "research" },
                sentiment = "positive",
                language = "en"
            }),
            new Dictionary<string, object>
            {
                ["newsItemId"] = newsItemId,
                ["extractionMethod"] = "automated",
                ["confidence"] = 0.95
            }
        );
    }

    private Node CreateNewsSummaryNode(string contentId)
    {
        return NodeHelpers.CreateNode(
            "news-summary-ai-breakthrough",
            "codex.news.summary",
            ContentState.Water,
            "AI Breakthrough Summary",
            "Summarized version of the AI breakthrough news",
            NodeHelpers.CreateJsonContent(new
            {
                id = "news-summary-ai-breakthrough",
                summary = "New AI system achieves breakthrough in natural language processing",
                keyPoints = new[] { "Unprecedented accuracy", "Natural language processing", "Research breakthrough" },
                wordCount = 25
            }),
            new Dictionary<string, object>
            {
                ["contentId"] = contentId,
                ["summaryType"] = "automated",
                ["wordCount"] = 25
            }
        );
    }

    private Node CreateConceptNode(string conceptName, string summaryId)
    {
        return NodeHelpers.CreateNode(
            $"concept-{conceptName}",
            "codex.concept",
            ContentState.Ice,
            $"Concept: {conceptName}",
            $"Extracted concept: {conceptName}",
            NodeHelpers.CreateJsonContent(new
            {
                id = $"concept-{conceptName}",
                name = conceptName,
                description = $"Concept extracted from news content: {conceptName}",
                frequency = 0.85,
                resonance = 0.92
            }),
            new Dictionary<string, object>
            {
                ["conceptName"] = conceptName,
                ["summaryId"] = summaryId,
                ["frequency"] = 0.85,
                ["resonance"] = 0.92
            }
        );
    }

    private Node CreateGeneratedImageNode(string concept1Id, string concept2Id)
    {
        return NodeHelpers.CreateNode(
            "generated-image-tech-innovation",
            "codex.image.generated",
            ContentState.Water,
            "Generated Tech Innovation Image",
            "AI-generated image visualizing technology and innovation concepts",
            NodeHelpers.CreateJsonContent(new
            {
                id = "generated-image-tech-innovation",
                prompt = "Technology innovation visualization",
                concepts = new[] { concept1Id, concept2Id },
                imageUrl = "https://example.com/generated/tech-innovation.png",
                dimensions = new { width = 1024, height = 768 },
                generatedAt = DateTime.UtcNow
            }),
            new Dictionary<string, object>
            {
                ["concept1Id"] = concept1Id,
                ["concept2Id"] = concept2Id,
                ["imageType"] = "generated",
                ["quality"] = "high"
            }
        );
    }

    private async Task<List<Node>> TraverseNodeGraphToIdentity(string startNodeId)
    {
        var visited = new HashSet<string>();
        var path = new List<Node>();
        var queue = new Queue<string>();
        
        queue.Enqueue(startNodeId);
        
        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            
            if (visited.Contains(currentNodeId))
                continue;
                
            visited.Add(currentNodeId);
            
            var currentNode = await _registry.GetNodeAsync(currentNodeId);
            if (currentNode == null)
                continue;
                
            path.Add(currentNode);
            
            // If we've reached the core identity, we're done
            if (currentNode.TypeId == "codex.core.identity")
                break;
            
            // Add all connected nodes to the queue
            var edges = GetOutgoingEdges(currentNodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.ToId))
                {
                    queue.Enqueue(edge.ToId);
                }
            }
        }
        
        return path;
    }

    private async Task<bool> CanReachNode(string fromNodeId, string toNodeId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        
        queue.Enqueue(fromNodeId);
        
        while (queue.Count > 0)
        {
            var currentNodeId = queue.Dequeue();
            
            if (currentNodeId == toNodeId)
                return true;
                
            if (visited.Contains(currentNodeId))
                continue;
                
            visited.Add(currentNodeId);
            
            var edges = GetOutgoingEdges(currentNodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.ToId))
                {
                    queue.Enqueue(edge.ToId);
                }
            }
        }
        
        return false;
    }

    private List<Edge> GetOutgoingEdges(string nodeId)
    {
        var allEdges = _registry.AllEdges().ToList();
        return allEdges.Where(e => e.FromId == nodeId).ToList();
    }

    private bool ValidateImageNode(Node imageNode)
    {
        if (imageNode.TypeId != "codex.image.generated")
            return false;
            
        var content = imageNode.Content?.InlineJson;
        if (string.IsNullOrEmpty(content))
            return false;
            
        try
        {
            var imageData = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            return imageData.ContainsKey("imageUrl") && 
                   imageData.ContainsKey("concepts") && 
                   imageData.ContainsKey("dimensions");
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateCompleteNodeGraph()
    {
        // Create core identity
        var coreIdentityNode = CreateCoreIdentityNode();
        _registry.Upsert(coreIdentityNode);

        // Create U-CORE root
        var ucoreRootNode = CreateUCoreOntologyRootNode();
        _registry.Upsert(ucoreRootNode);

        // Create edge from U-CORE to identity
        var ucoreToIdentityEdge = NodeHelpers.CreateEdge(
            ucoreRootNode.Id, 
            coreIdentityNode.Id, 
            "belongs_to", 
            1.0
        );
        _registry.Upsert(ucoreToIdentityEdge);

        // Create news source
        var newsSourceNode = CreateNewsSourceNode();
        _registry.Upsert(newsSourceNode);

        // Create edge from source to U-CORE
        var sourceToUcoreEdge = NodeHelpers.CreateEdge(
            newsSourceNode.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0
        );
        _registry.Upsert(sourceToUcoreEdge);

        // Create news item
        var newsItemNode = CreateNewsItemNode(newsSourceNode.Id);
        _registry.Upsert(newsItemNode);

        // Create edge from item to source
        var itemToSourceEdge = NodeHelpers.CreateEdge(
            newsItemNode.Id, 
            newsSourceNode.Id, 
            "published_by", 
            1.0
        );
        _registry.Upsert(itemToSourceEdge);

        // Create content
        var newsContentNode = CreateNewsContentNode(newsItemNode.Id);
        _registry.Upsert(newsContentNode);

        // Create edge from content to item
        var contentToItemEdge = NodeHelpers.CreateEdge(
            newsContentNode.Id, 
            newsItemNode.Id, 
            "extracted_from", 
            1.0
        );
        _registry.Upsert(contentToItemEdge);

        // Create summary
        var newsSummaryNode = CreateNewsSummaryNode(newsContentNode.Id);
        _registry.Upsert(newsSummaryNode);

        // Create edge from summary to content
        var summaryToContentEdge = NodeHelpers.CreateEdge(
            newsSummaryNode.Id, 
            newsContentNode.Id, 
            "summarizes", 
            1.0
        );
        _registry.Upsert(summaryToContentEdge);

        // Create concepts
        var concept1Node = CreateConceptNode("technology", newsSummaryNode.Id);
        var concept2Node = CreateConceptNode("innovation", newsSummaryNode.Id);
        _registry.Upsert(concept1Node);
        _registry.Upsert(concept2Node);

        // Create edges from concepts to summary
        var concept1ToSummaryEdge = NodeHelpers.CreateEdge(
            concept1Node.Id, 
            newsSummaryNode.Id, 
            "extracted_from", 
            1.0
        );
        var concept2ToSummaryEdge = NodeHelpers.CreateEdge(
            concept2Node.Id, 
            newsSummaryNode.Id, 
            "extracted_from", 
            1.0
        );
        _registry.Upsert(concept1ToSummaryEdge);
        _registry.Upsert(concept2ToSummaryEdge);

        // Create edges from concepts to U-CORE
        var concept1ToUcoreEdge = NodeHelpers.CreateEdge(
            concept1Node.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0
        );
        var concept2ToUcoreEdge = NodeHelpers.CreateEdge(
            concept2Node.Id, 
            ucoreRootNode.Id, 
            "belongs_to", 
            1.0
        );
        _registry.Upsert(concept1ToUcoreEdge);
        _registry.Upsert(concept2ToUcoreEdge);

        // Create generated image
        var generatedImageNode = CreateGeneratedImageNode(concept1Node.Id, concept2Node.Id);
        _registry.Upsert(generatedImageNode);

        // Create edges from image to concepts
        var imageToConcept1Edge = NodeHelpers.CreateEdge(
            generatedImageNode.Id, 
            concept1Node.Id, 
            "visualizes", 
            1.0
        );
        var imageToConcept2Edge = NodeHelpers.CreateEdge(
            generatedImageNode.Id, 
            concept2Node.Id, 
            "visualizes", 
            1.0
        );
        _registry.Upsert(imageToConcept1Edge);
        _registry.Upsert(imageToConcept2Edge);
    }

    #endregion
}
