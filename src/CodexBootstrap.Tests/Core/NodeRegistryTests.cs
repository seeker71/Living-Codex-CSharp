using System;
using System.Collections.Generic;
using System.Linq;
using CodexBootstrap.Core;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Core
{
    public class NodeRegistryTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly NodeRegistry _registry;

        public NodeRegistryTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _registry = new NodeRegistry();
        }

        [Fact]
        public void UpsertNode_WithValidNode_ShouldRegisterSuccessfully()
        {
            // Arrange
            var nodeId = "test-node";
            var node = new Node(
                Id: nodeId,
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Test Node",
                Description: "A test node",
                Content: null,
                Meta: new Dictionary<string, object> { { "key", "value" } }
            );

            // Act
            _registry.Upsert(node);

            // Assert
            _registry.TryGet(nodeId, out var retrievedNode).Should().BeTrue();
            retrievedNode.Should().NotBeNull();
            retrievedNode.Id.Should().Be(nodeId);
        }

        [Fact]
        public void UpsertNode_WithDuplicateId_ShouldOverwrite()
        {
            // Arrange
            var nodeId = "duplicate-node";
            var node1 = new Node(
                Id: nodeId,
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Test Node 1",
                Description: "First test node",
                Content: null,
                Meta: new Dictionary<string, object> { { "key1", "value1" } }
            );
            var node2 = new Node(
                Id: nodeId,
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Test Node 2",
                Description: "Second test node",
                Content: null,
                Meta: new Dictionary<string, object> { { "key2", "value2" } }
            );

            // Act
            _registry.Upsert(node1);
            _registry.Upsert(node2);

            // Assert
            _registry.TryGet(nodeId, out var retrievedNode).Should().BeTrue();
            retrievedNode.Meta["key2"].Should().Be("value2");
        }

        [Fact]
        public void TryGet_WithExistingId_ShouldReturnNode()
        {
            // Arrange
            var nodeId = "existing-node";
            var node = new Node(
                Id: nodeId,
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Existing Node",
                Description: "An existing test node",
                Content: null,
                Meta: null
            );
            _registry.Upsert(node);

            // Act
            var result = _registry.TryGet(nodeId, out var retrievedNode);

            // Assert
            result.Should().BeTrue();
            retrievedNode.Should().NotBeNull();
            retrievedNode.Id.Should().Be(nodeId);
        }

        [Fact]
        public void TryGet_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var nodeId = "non-existent-node";

            // Act
            var result = _registry.TryGet(nodeId, out var retrievedNode);

            // Assert
            result.Should().BeFalse();
            retrievedNode.Should().BeNull();
        }

        [Fact]
        public void AllNodes_ShouldReturnAllRegisteredNodes()
        {
            // Arrange
            var node1 = new Node(
                Id: "node1",
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node 1",
                Description: "First node",
                Content: null,
                Meta: null
            );
            var node2 = new Node(
                Id: "node2",
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node 2",
                Description: "Second node",
                Content: null,
                Meta: null
            );

            _registry.Upsert(node1);
            _registry.Upsert(node2);

            // Act
            var allNodes = _registry.AllNodes().ToList();

            // Assert
            allNodes.Should().HaveCount(2);
            allNodes.Should().Contain(n => n.Id == "node1");
            allNodes.Should().Contain(n => n.Id == "node2");
        }

        [Fact]
        public void GetNodesByType_ShouldReturnNodesOfSpecificType()
        {
            // Arrange
            var node1 = new Node(
                Id: "node1",
                TypeId: "type1",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node 1",
                Description: "First node",
                Content: null,
                Meta: null
            );
            var node2 = new Node(
                Id: "node2",
                TypeId: "type2",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node 2",
                Description: "Second node",
                Content: null,
                Meta: null
            );

            _registry.Upsert(node1);
            _registry.Upsert(node2);

            // Act
            var type1Nodes = _registry.GetNodesByType("type1").ToList();

            // Assert
            type1Nodes.Should().HaveCount(1);
            type1Nodes.First().Id.Should().Be("node1");
        }

        [Fact]
        public void UpsertEdge_ShouldRegisterEdgeSuccessfully()
        {
            // Arrange
            var edge = new Edge(
                FromId: "node1",
                ToId: "node2",
                Role: "connects",
                Weight: 1.0,
                Meta: new Dictionary<string, object> { { "strength", "strong" } }
            );

            // Act
            _registry.Upsert(edge);

            // Assert
            var allEdges = _registry.AllEdges().ToList();
            allEdges.Should().HaveCount(1);
            allEdges.First().FromId.Should().Be("node1");
            allEdges.First().ToId.Should().Be("node2");
        }

        [Fact]
        public void GetEdgesFrom_ShouldReturnEdgesFromSpecificNode()
        {
            // Arrange
            var edge1 = new Edge("node1", "node2", "connects", 1.0, null);
            var edge2 = new Edge("node1", "node3", "relates", 0.5, null);
            var edge3 = new Edge("node2", "node3", "links", 0.8, null);

            _registry.Upsert(edge1);
            _registry.Upsert(edge2);
            _registry.Upsert(edge3);

            // Act
            var edgesFromNode1 = _registry.GetEdgesFrom("node1").ToList();

            // Assert
            edgesFromNode1.Should().HaveCount(2);
            edgesFromNode1.Should().Contain(e => e.ToId == "node2");
            edgesFromNode1.Should().Contain(e => e.ToId == "node3");
        }
    }
}