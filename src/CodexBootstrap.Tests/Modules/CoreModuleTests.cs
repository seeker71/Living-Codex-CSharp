using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace CodexBootstrap.Tests.Modules
{
    public class CoreModuleTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<CodexBootstrap.Core.NodeRegistry> _mockNodeRegistry;
        private readonly CodexBootstrap.Modules.CoreModule _coreModule;

        public CoreModuleTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockNodeRegistry = new Mock<CodexBootstrap.Core.NodeRegistry>();
            
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);
            
            _coreModule = new CodexBootstrap.Modules.CoreModule(_mockNodeRegistry.Object, _mockLoggerFactory.Object);
        }

        [Fact]
        public async Task GetSystemStatusAsync_ShouldReturnHealthyStatus()
        {
            // Arrange
            _mockNodeRegistry.Setup(x => x.GetNodeCount()).Returns(10);

            // Act
            var result = await _coreModule.GetSystemStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("healthy");
            result.NodeCount.Should().Be(10);
            result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetSystemStatusAsync_WithZeroNodes_ShouldReturnWarningStatus()
        {
            // Arrange
            _mockNodeRegistry.Setup(x => x.GetNodeCount()).Returns(0);

            // Act
            var result = await _coreModule.GetSystemStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("warning");
            result.NodeCount.Should().Be(0);
        }

        [Fact]
        public async Task GetSystemStatusAsync_WithRegistryException_ShouldReturnErrorStatus()
        {
            // Arrange
            _mockNodeRegistry.Setup(x => x.GetNodeCount())
                .Throws(new Exception("Registry unavailable"));

            // Act
            var result = await _coreModule.GetSystemStatusAsync();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be("error");
            result.Error.Should().Contain("Registry unavailable");
        }

        [Fact]
        public async Task GetNodeInfoAsync_WithExistingNode_ShouldReturnNodeInfo()
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

            _mockNodeRegistry.Setup(x => x.GetNode(nodeId)).Returns(node);

            // Act
            var result = await _coreModule.GetNodeInfoAsync(nodeId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(nodeId);
        }

        [Fact]
        public async Task GetNodeInfoAsync_WithNonExistentNode_ShouldReturnError()
        {
            // Arrange
            var nodeId = "non-existent";
            _mockNodeRegistry.Setup(x => x.GetNode(nodeId)).Returns((Node)null);

            // Act
            var result = await _coreModule.GetNodeInfoAsync(nodeId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Node not found");
        }

        [Fact]
        public async Task GetNodeInfoAsync_WithNullNodeId_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.GetNodeInfoAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Node ID cannot be null");
        }

        [Fact]
        public async Task GetNodeInfoAsync_WithEmptyNodeId_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.GetNodeInfoAsync("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Node ID cannot be empty");
        }

        [Fact]
        public async Task GetAllNodesAsync_ShouldReturnAllNodes()
        {
            // Arrange
            var nodes = new List<Node>
            {
                new() { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow },
                new() { Id = "node2", Type = "test", CreatedAt = DateTime.UtcNow }
            };

            _mockNodeRegistry.Setup(x => x.GetAllNodes()).Returns(nodes);

            // Act
            var result = await _coreModule.GetAllNodesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain(nodes[0]);
            result.Data.Should().Contain(nodes[1]);
        }

        [Fact]
        public async Task GetAllNodesAsync_WithEmptyRegistry_ShouldReturnEmptyList()
        {
            // Arrange
            _mockNodeRegistry.Setup(x => x.GetAllNodes()).Returns(new List<Node>());

            // Act
            var result = await _coreModule.GetAllNodesAsync();

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchNodesAsync_WithValidQuery_ShouldReturnMatchingNodes()
        {
            // Arrange
            var query = "test query";
            var nodes = new List<Node>
            {
                new() { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow }
            };

            _mockNodeRegistry.Setup(x => x.SearchNodes(query)).Returns(nodes);

            // Act
            var result = await _coreModule.SearchNodesAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.Should().Contain(nodes[0]);
        }

        [Fact]
        public async Task SearchNodesAsync_WithNullQuery_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.SearchNodesAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Query cannot be null");
        }

        [Fact]
        public async Task SearchNodesAsync_WithEmptyQuery_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.SearchNodesAsync("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Query cannot be empty");
        }

        [Fact]
        public async Task GetNodesByTypeAsync_WithValidType_ShouldReturnMatchingNodes()
        {
            // Arrange
            var nodeType = "test";
            var nodes = new List<Node>
            {
                new() { Id = "node1", Type = "test", CreatedAt = DateTime.UtcNow },
                new() { Id = "node2", Type = "test", CreatedAt = DateTime.UtcNow }
            };

            _mockNodeRegistry.Setup(x => x.GetNodesByType(nodeType)).Returns(nodes);

            // Act
            var result = await _coreModule.GetNodesByTypeAsync(nodeType);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain(nodes[0]);
            result.Data.Should().Contain(nodes[1]);
        }

        [Fact]
        public async Task GetNodesByTypeAsync_WithNullType_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.GetNodesByTypeAsync(null);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Type cannot be null");
        }

        [Fact]
        public async Task GetNodesByTypeAsync_WithEmptyType_ShouldReturnError()
        {
            // Act
            var result = await _coreModule.GetNodesByTypeAsync("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Error.Should().Contain("Type cannot be empty");
        }

        [Fact]
        public void GetModuleInfo_ShouldReturnCorrectInfo()
        {
            // Act
            var info = _coreModule.GetModuleInfo();

            // Assert
            info.Should().NotBeNull();
            info.Name.Should().Be("Core System Module");
            info.Version.Should().Be("0.1.0");
            info.Description.Should().Contain("Core system");
        }

        [Fact]
        public void GetApiEndpoints_ShouldReturnAllEndpoints()
        {
            // Act
            var endpoints = _coreModule.GetApiEndpoints();

            // Assert
            endpoints.Should().NotBeNull();
            endpoints.Should().HaveCountGreaterThan(0);
            endpoints.Should().Contain(e => e.Path.Contains("status"));
            endpoints.Should().Contain(e => e.Path.Contains("nodes"));
        }
    }
}
