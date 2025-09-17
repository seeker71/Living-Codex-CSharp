using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;
using System.Reflection;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Core
{
    public class NodeRegistryTests
    {
        private readonly Mock<CodexBootstrap.Core.ICodexLogger> _mockLogger;
        private readonly NodeRegistry _registry;
        private readonly InMemoryIceStorageBackend _iceStorage;
        private readonly InMemoryWaterStorageBackend _waterStorage;

        public NodeRegistryTests()
        {
            _mockLogger = new Mock<CodexBootstrap.Core.ICodexLogger>();
            _iceStorage = new InMemoryIceStorageBackend();
            _waterStorage = new InMemoryWaterStorageBackend();
            _registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            
            // Initialize the registry for testing
            _registry.InitializeAsync().Wait();
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

        [Fact]
        public void EdgeState_ShouldFollowNodePhaseTransitions()
        {
            var nodeA = new Node(
                Id: "nodeA",
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node A",
                Description: "",
                Content: null,
                Meta: null);

            var nodeB = new Node(
                Id: "nodeB",
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Node B",
                Description: "",
                Content: null,
                Meta: null);

            var edge = new Edge("nodeA", "nodeB", "relates", 1.0, null);

            _registry.Upsert(nodeA);
            _registry.Upsert(nodeB);
            _registry.Upsert(edge);

            GetEdgeState("nodeA", "nodeB", "relates").Should().Be(ContentState.Ice);

            var nodeAAsWater = nodeA with { State = ContentState.Water };
            _registry.Upsert(nodeAAsWater);
            GetEdgeState("nodeA", "nodeB", "relates").Should().Be(ContentState.Gas);

            var nodeBAsWater = nodeB with { State = ContentState.Water };
            _registry.Upsert(nodeBAsWater);
            GetEdgeState("nodeA", "nodeB", "relates").Should().Be(ContentState.Water);

            var nodeAAsGas = nodeAAsWater with { State = ContentState.Gas };
            _registry.Upsert(nodeAAsGas);
            GetEdgeState("nodeA", "nodeB", "relates").Should().Be(ContentState.Gas);
        }

        [Fact]
        public async Task InitializeAsync_HydratesNodesFromPersistentStorage()
        {
            // Arrange
            var iceStorage = new InMemoryIceStorageBackend();
            var waterStorage = new InMemoryWaterStorageBackend();
            var logger = new Mock<CodexBootstrap.Core.ICodexLogger>();

            var iceNode = new Node(
                Id: "hydrated-ice",
                TypeId: "test",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Hydrated Ice",
                Description: "Persisted ice node",
                Content: null,
                Meta: null);

            var waterNode = new Node(
                Id: "hydrated-water",
                TypeId: "test",
                State: ContentState.Water,
                Locale: "en",
                Title: "Hydrated Water",
                Description: "Persisted water node",
                Content: null,
                Meta: null);

            await iceStorage.StoreIceNodeAsync(iceNode);
            await waterStorage.StoreWaterNodeAsync(waterNode);

            var registry = new NodeRegistry(iceStorage, waterStorage, logger.Object);

            // Act
            await registry.InitializeAsync();

            // Assert
            registry.TryGet("hydrated-ice", out var hydratedIce).Should().BeTrue();
            hydratedIce.Should().NotBeNull();
            hydratedIce!.State.Should().Be(ContentState.Ice);

            registry.TryGet("hydrated-water", out var hydratedWater).Should().BeTrue();
            hydratedWater.Should().NotBeNull();
            hydratedWater!.State.Should().Be(ContentState.Water);

            registry.AllNodes().Select(n => n.Id).Should().Contain(new[] { "hydrated-ice", "hydrated-water" });
        }

        [Fact]
        public async Task Upsert_NodeStateTransitionUpdatesMemoryAndPersistence()
        {
            // Arrange
            var nodeId = "stateful-node";
            var waterNode = new Node(
                Id: nodeId,
                TypeId: "test",
                State: ContentState.Water,
                Locale: "en",
                Title: "Water Node",
                Description: "Node in water state",
                Content: null,
                Meta: null);

            var iceNode = waterNode with
            {
                State = ContentState.Ice,
                Title = "Ice Node",
                Description = "Node promoted to ice"
            };

            // Act - store as Water first
            _registry.Upsert(waterNode);

            var hydratedWater = await WaitForNodeAsync(() => _waterStorage.GetWaterNodeAsync(nodeId), expectPresent: true);
            hydratedWater.Should().NotBeNull();
            hydratedWater!.State.Should().Be(ContentState.Water);

            _registry.TryGet(nodeId, out var cachedWater).Should().BeTrue();
            cachedWater.State.Should().Be(ContentState.Water);

            // Promote to Ice
            _registry.Upsert(iceNode);

            var hydratedIce = await WaitForNodeAsync(() => _iceStorage.GetIceNodeAsync(nodeId), expectPresent: true);
            hydratedIce.Should().NotBeNull();
            hydratedIce!.State.Should().Be(ContentState.Ice);

            _registry.TryGet(nodeId, out var cachedIce).Should().BeTrue();
            cachedIce.State.Should().Be(ContentState.Ice);

            _registry.AllNodes().Count(n => n.Id == nodeId).Should().Be(1);
        }

        private static async Task<Node?> WaitForNodeAsync(Func<Task<Node?>> fetch, bool expectPresent, TimeSpan? timeout = null)
        {
            var expiry = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(1));
            Node? node = null;

            while (DateTime.UtcNow <= expiry)
            {
                node = await fetch();
                if ((node != null) == expectPresent)
                {
                    return node;
                }

                await Task.Delay(25);
            }

            return node;
        }

        private ContentState? GetEdgeState(string fromId, string toId, string role)
        {
            var edgeKey = $"{fromId}::{role}::{toId}".ToLowerInvariant();
            var edgeRecordsField = typeof(NodeRegistry).GetField("_edgeRecords", BindingFlags.NonPublic | BindingFlags.Instance);
            edgeRecordsField.Should().NotBeNull();

            var edgeRecords = edgeRecordsField!.GetValue(_registry);
            edgeRecords.Should().NotBeNull();

            var tryGetValueMethod = edgeRecords!.GetType().GetMethod("TryGetValue");
            tryGetValueMethod.Should().NotBeNull();

            var parameters = new object?[] { edgeKey, null };
            var found = (bool)tryGetValueMethod!.Invoke(edgeRecords, parameters)!;
            found.Should().BeTrue();

            var edgeRecord = parameters[1];
            edgeRecord.Should().NotBeNull();

            var stateProperty = edgeRecord!.GetType().GetProperty("State", BindingFlags.Public | BindingFlags.Instance);
            stateProperty.Should().NotBeNull();

            return (ContentState?)stateProperty!.GetValue(edgeRecord);
        }
    }
}
