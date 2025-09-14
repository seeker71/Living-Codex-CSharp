using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace CodexBootstrap.Tests.Core
{
    public class NodeRegistryTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly NodeRegistry _registry;

        public NodeRegistryTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
            
            _registry = new NodeRegistry(_mockLoggerFactory.Object);
        }

        [Fact]
        public void RegisterNode_WithValidNode_ShouldRegisterSuccessfully()
        {
            // Arrange
            var nodeId = "test-node";
            var node = new Node
            {
                Id = nodeId,
                Type = "test",
                Data = new Dictionary<string, object> { { "key", "value" } },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = _registry.RegisterNode(node);

            // Assert
            result.Should().BeTrue();
            _registry.GetNode(nodeId).Should().NotBeNull();
            _registry.GetNode(nodeId).Id.Should().Be(nodeId);
        }

        [Fact]
        public void RegisterNode_WithDuplicateId_ShouldReturnFalse()
        {
            // Arrange
            var nodeId = "duplicate-node";
            var node1 = new Node { Id = nodeId, Type = "test1", CreatedAt = DateTime.UtcNow };
            var node2 = new Node { Id = nodeId, Type = "test2", CreatedAt = DateTime.UtcNow };

            // Act
            var result1 = _registry.RegisterNode(node1);
            var result2 = _registry.RegisterNode(node2);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeFalse();
        }

        [Fact]
        public void RegisterNode_WithNullNode_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _registry.RegisterNode(null));
        }

        [Fact]
        public void GetNode_WithExistingId_ShouldReturnNode()
        {
            // Arrange
            var nodeId = "existing-node";
            var node = new Node { Id = nodeId, Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node);

            // Act
            var result = _registry.GetNode(nodeId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(nodeId);
        }

        [Fact]
        public void GetNode_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var nodeId = "non-existent-node";

            // Act
            var result = _registry.GetNode(nodeId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetAllNodes_ShouldReturnAllRegisteredNodes()
        {
            // Arrange
            var node1 = new Node { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow };
            var node2 = new Node { Id = "node2", Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node1);
            _registry.RegisterNode(node2);

            // Act
            var result = _registry.GetAllNodes();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(node1);
            result.Should().Contain(node2);
        }

        [Fact]
        public void GetNodesByType_WithExistingType_ShouldReturnMatchingNodes()
        {
            // Arrange
            var node1 = new Node { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow };
            var node2 = new Node { Id = "node2", Type = "other", CreatedAt = DateTime.UtcNow };
            var node3 = new Node { Id = "node3", Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node1);
            _registry.RegisterNode(node2);
            _registry.RegisterNode(node3);

            // Act
            var result = _registry.GetNodesByType("test");

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(node1);
            result.Should().Contain(node3);
            result.Should().NotContain(node2);
        }

        [Fact]
        public void GetNodesByType_WithNonExistentType_ShouldReturnEmptyList()
        {
            // Arrange
            var node = new Node { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node);

            // Act
            var result = _registry.GetNodesByType("non-existent");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UpdateNode_WithExistingNode_ShouldUpdateSuccessfully()
        {
            // Arrange
            var nodeId = "update-node";
            var originalNode = new Node 
            { 
                Id = nodeId, 
                Type = "test", 
                Data = new Dictionary<string, object> { { "key", "original" } },
                CreatedAt = DateTime.UtcNow 
            };
            _registry.RegisterNode(originalNode);

            var updatedNode = new Node 
            { 
                Id = nodeId, 
                Type = "test", 
                Data = new Dictionary<string, object> { { "key", "updated" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act
            var result = _registry.UpdateNode(updatedNode);

            // Assert
            result.Should().BeTrue();
            var retrievedNode = _registry.GetNode(nodeId);
            retrievedNode.Data["key"].Should().Be("updated");
        }

        [Fact]
        public void UpdateNode_WithNonExistentNode_ShouldReturnFalse()
        {
            // Arrange
            var node = new Node { Id = "non-existent", Type = "test", CreatedAt = DateTime.UtcNow };

            // Act
            var result = _registry.UpdateNode(node);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void DeleteNode_WithExistingNode_ShouldDeleteSuccessfully()
        {
            // Arrange
            var nodeId = "delete-node";
            var node = new Node { Id = nodeId, Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node);

            // Act
            var result = _registry.DeleteNode(nodeId);

            // Assert
            result.Should().BeTrue();
            _registry.GetNode(nodeId).Should().BeNull();
        }

        [Fact]
        public void DeleteNode_WithNonExistentNode_ShouldReturnFalse()
        {
            // Arrange
            var nodeId = "non-existent";

            // Act
            var result = _registry.DeleteNode(nodeId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void SearchNodes_WithMatchingQuery_ShouldReturnMatchingNodes()
        {
            // Arrange
            var node1 = new Node 
            { 
                Id = "node1", 
                Type = "test", 
                Data = new Dictionary<string, object> { { "name", "test node" } },
                CreatedAt = DateTime.UtcNow 
            };
            var node2 = new Node 
            { 
                Id = "node2", 
                Type = "test", 
                Data = new Dictionary<string, object> { { "name", "other node" } },
                CreatedAt = DateTime.UtcNow 
            };
            _registry.RegisterNode(node1);
            _registry.RegisterNode(node2);

            // Act
            var result = _registry.SearchNodes("test node");

            // Assert
            result.Should().HaveCount(1);
            result.Should().Contain(node1);
        }

        [Fact]
        public void SearchNodes_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var node = new Node 
            { 
                Id = "node1", 
                Type = "test", 
                Data = new Dictionary<string, object> { { "name", "test node" } },
                CreatedAt = DateTime.UtcNow 
            };
            _registry.RegisterNode(node);

            // Act
            var result = _registry.SearchNodes("no match");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void GetNodeCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var node1 = new Node { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow };
            var node2 = new Node { Id = "node2", Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node1);
            _registry.RegisterNode(node2);

            // Act
            var count = _registry.GetNodeCount();

            // Assert
            count.Should().Be(2);
        }

        [Fact]
        public void Clear_ShouldRemoveAllNodes()
        {
            // Arrange
            var node1 = new Node { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow };
            var node2 = new Node { Id = "node2", Type = "test", CreatedAt = DateTime.UtcNow };
            _registry.RegisterNode(node1);
            _registry.RegisterNode(node2);

            // Act
            _registry.Clear();

            // Assert
            _registry.GetNodeCount().Should().Be(0);
            _registry.GetAllNodes().Should().BeEmpty();
        }
    }
}
