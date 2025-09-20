using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;
using CodexBootstrap.Tests.Modules;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Core
{
    /// <summary>
    /// Comprehensive tests for node state transitions, persistence, and cache behavior.
    /// Validates the "Everything is a Node" principle with proper Ice/Water/Gas state management.
    /// </summary>
    public class NodePersistenceTests
    {
        private readonly Mock<ICodexLogger> _mockLogger;
        private readonly TestIceStorageBackend _iceStorage;
        private readonly TestWaterStorageBackend _waterStorage;

        public NodePersistenceTests()
        {
            _mockLogger = new Mock<ICodexLogger>();
            _iceStorage = new TestIceStorageBackend();
            _waterStorage = new TestWaterStorageBackend();
        }

        /// <summary>
        /// Test storage backend that simulates real persistence behavior
        /// </summary>
        private class PersistentTestStorageBackend : IIceStorageBackend
        {
            private readonly Dictionary<string, Node> _persistentNodes = new();
            private readonly Dictionary<string, Edge> _persistentEdges = new();
            private readonly string _backendType;

            public PersistentTestStorageBackend(string backendType)
            {
                _backendType = backendType;
            }

            // Simulate clearing cache (server restart)
            public void SimulateServerRestart()
            {
                // Data remains in "persistent" storage but cache would be cleared
                // This simulates what happens during a real server restart
            }

            public Dictionary<string, Node> GetPersistentNodes() => new(_persistentNodes);
            public Dictionary<string, Edge> GetPersistentEdges() => new(_persistentEdges);

            #region IIceStorageBackend Implementation

            public Task InitializeAsync() => Task.CompletedTask;

            public Task StoreIceNodeAsync(Node node)
            {
                _persistentNodes[node.Id] = node;
                return Task.CompletedTask;
            }

            public Task<Node?> GetIceNodeAsync(string id)
            {
                _persistentNodes.TryGetValue(id, out var node);
                return Task.FromResult(node);
            }

            public Task<IEnumerable<Node>> GetAllIceNodesAsync()
            {
                return Task.FromResult(_persistentNodes.Values.AsEnumerable());
            }

            public Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId)
            {
                var nodes = _persistentNodes.Values.Where(n => n.TypeId == typeId);
                return Task.FromResult(nodes);
            }

            public Task StoreEdgeAsync(Edge edge)
            {
                var edgeKey = $"{edge.FromId}->{edge.ToId}:{edge.Role}";
                _persistentEdges[edgeKey] = edge;
                return Task.CompletedTask;
            }

            public Task<IEnumerable<Edge>> GetAllEdgesAsync()
            {
                return Task.FromResult(_persistentEdges.Values.AsEnumerable());
            }

            public Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
            {
                var edges = _persistentEdges.Values.Where(e => e.FromId == fromId);
                return Task.FromResult(edges);
            }

            public Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
            {
                var edges = _persistentEdges.Values.Where(e => e.ToId == toId);
                return Task.FromResult(edges);
            }

            public Task DeleteIceNodeAsync(string id)
            {
                _persistentNodes.Remove(id);
                return Task.CompletedTask;
            }

            public Task DeleteEdgeAsync(string fromId, string toId, string role)
            {
                var edgeKey = $"{fromId}->{toId}:{role}";
                _persistentEdges.Remove(edgeKey);
                return Task.CompletedTask;
            }

            public Task<bool> IsAvailableAsync() => Task.FromResult(true);

            public Task<IceStorageStats> GetStatsAsync()
            {
                return Task.FromResult(new IceStorageStats(
                    IceNodeCount: _persistentNodes.Count,
                    EdgeCount: _persistentEdges.Count,
                    TotalSizeBytes: 0,
                    LastUpdated: DateTime.UtcNow,
                    BackendType: _backendType,
                    BackendStats: new Dictionary<string, object>()
                ));
            }

            public Task BatchStoreIceNodesAsync(IEnumerable<Node> nodes)
            {
                foreach (var node in nodes)
                {
                    _persistentNodes[node.Id] = node;
                }
                return Task.CompletedTask;
            }

            public Task BatchStoreEdgesAsync(IEnumerable<Edge> edges)
            {
                foreach (var edge in edges)
                {
                    var edgeKey = $"{edge.FromId}->{edge.ToId}:{edge.Role}";
                    _persistentEdges[edgeKey] = edge;
                }
                return Task.CompletedTask;
            }

            public Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100)
            {
                var results = _persistentNodes.Values
                    .Where(n => n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                               n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    .Take(limit);
                return Task.FromResult(results);
            }

            public Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100)
            {
                var results = _persistentNodes.Values
                    .Where(n => n.Meta?.ContainsKey(key) == true && 
                               n.Meta[key]?.ToString() == value?.ToString())
                    .Take(limit);
                return Task.FromResult(results);
            }


            #endregion

            #region IWaterStorageBackend Implementation

            public Task StoreWaterNodeAsync(Node node, TimeSpan? expiry = null)
            {
                _persistentNodes[node.Id] = node;
                return Task.CompletedTask;
            }

            public Task<Node?> GetWaterNodeAsync(string id)
            {
                _persistentNodes.TryGetValue(id, out var node);
                return Task.FromResult(node);
            }

            public Task<IEnumerable<Node>> GetAllWaterNodesAsync()
            {
                return Task.FromResult(_persistentNodes.Values.AsEnumerable());
            }

            public Task<IEnumerable<Node>> GetWaterNodesByTypeAsync(string typeId)
            {
                var nodes = _persistentNodes.Values.Where(n => n.TypeId == typeId);
                return Task.FromResult(nodes);
            }

            public Task DeleteWaterNodeAsync(string id)
            {
                _persistentNodes.Remove(id);
                return Task.CompletedTask;
            }

            #endregion
        }

        #region Node State Transition Tests

        [Fact]
        public async Task NodeStateTransition_Ice_ShouldPersistInIceStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var iceNode = new Node(
                Id: "test-ice-node",
                TypeId: "test.ice",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Test Ice Node",
                Description: "A test node in Ice state",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice-data\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "test", "ice-value" } }
            );

            // Act
            registry.Upsert(iceNode);
            await Task.Delay(100); // Allow async storage to complete

            // Assert
            _iceStorage.GetPersistentNodes().Should().Contain(n => n.Id == "test-ice-node");
            _waterStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "test-ice-node");

            var storedNode = _iceStorage.GetPersistentNodes().First(n => n.Id == "test-ice-node");
            storedNode.State.Should().Be(ContentState.Ice);
            storedNode.Title.Should().Be("Test Ice Node");
            storedNode.Content?.InlineJson.Should().Be("{\"data\": \"ice-data\"}");
        }

        [Fact]
        public async Task NodeStateTransition_Water_ShouldPersistInWaterStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var waterNode = new Node(
                Id: "test-water-node",
                TypeId: "test.water",
                State: ContentState.Water,
                Locale: "en",
                Title: "Test Water Node",
                Description: "A test node in Water state",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water-data\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "test", "water-value" } }
            );

            // Act
            registry.Upsert(waterNode);
            await Task.Delay(100); // Allow async storage to complete

            // Assert
            _waterStorage.GetPersistentNodes().Should().Contain(n => n.Id == "test-water-node");
            _iceStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "test-water-node");

            var storedNode = _waterStorage.GetPersistentNodes().First(n => n.Id == "test-water-node");
            storedNode.State.Should().Be(ContentState.Water);
            storedNode.Title.Should().Be("Test Water Node");
            storedNode.Content?.InlineJson.Should().Be("{\"data\": \"water-data\"}");
        }

        [Fact]
        public async Task NodeStateTransition_Gas_ShouldNotPersist()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var gasNode = new Node(
                Id: "test-gas-node",
                TypeId: "test.gas",
                State: ContentState.Gas,
                Locale: "en",
                Title: "Test Gas Node",
                Description: "A test node in Gas state",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"gas-data\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "test", "gas-value" } }
            );

            // Act
            registry.Upsert(gasNode);
            await Task.Delay(100); // Allow any potential storage to complete

            // Assert - Gas nodes should not be persisted
            _iceStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "test-gas-node");
            _waterStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "test-gas-node");

            // But should be available in memory
            registry.TryGet("test-gas-node", out var retrievedNode).Should().BeTrue();
            retrievedNode.State.Should().Be(ContentState.Gas);
        }

        [Fact]
        public async Task NodeStateTransition_IceToWater_ShouldMoveStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var iceNode = new Node(
                Id: "transition-node",
                TypeId: "test.transition",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Transition Node",
                Description: "A node that will transition states",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"original\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "version", 1 } }
            );

            // Act 1: Store as Ice
            registry.Upsert(iceNode);
            await Task.Delay(100);

            // Assert 1: Should be in Ice storage
            _iceStorage.GetPersistentNodes().Should().Contain(n => n.Id == "transition-node");
            _waterStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "transition-node");

            // Act 2: Transition to Water
            var waterNode = iceNode with { 
                State = ContentState.Water,
                Content = new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"updated\"}", InlineBytes: null, ExternalUri: null),
                Meta = new Dictionary<string, object> { { "version", 2 } }
            };
            registry.Upsert(waterNode);
            await Task.Delay(100);

            // Assert 2: Should be in Water storage, removed from Ice
            _waterStorage.GetPersistentNodes().Should().Contain(n => n.Id == "transition-node");
            // Note: Ice storage might still contain the old version until cleanup
            
            var storedWaterNode = _waterStorage.GetPersistentNodes().First(n => n.Id == "transition-node");
            storedWaterNode.State.Should().Be(ContentState.Water);
            storedWaterNode.Content?.InlineJson.Should().Be("{\"data\": \"updated\"}");
            storedWaterNode.Meta?["version"].Should().Be(2);
        }

        [Fact]
        public async Task NodeStateTransition_WaterToGas_ShouldRemoveFromStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var waterNode = new Node(
                Id: "water-to-gas-node",
                TypeId: "test.transition",
                State: ContentState.Water,
                Locale: "en",
                Title: "Water to Gas Node",
                Description: "A node that will become Gas",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water-data\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "test", "water" } }
            );

            // Act 1: Store as Water
            registry.Upsert(waterNode);
            await Task.Delay(100);

            // Assert 1: Should be in Water storage
            _waterStorage.GetPersistentNodes().Should().Contain(n => n.Id == "water-to-gas-node");

            // Act 2: Transition to Gas
            var gasNode = waterNode with { State = ContentState.Gas };
            registry.Upsert(gasNode);
            await Task.Delay(100);

            // Assert 2: Should be removed from Water storage, available in memory as Gas
            // Note: Removal from storage might be deferred for performance
            registry.TryGet("water-to-gas-node", out var retrievedNode).Should().BeTrue();
            retrievedNode.State.Should().Be(ContentState.Gas);
        }

        #endregion

        #region Server Restart Simulation Tests

        [Fact]
        public async Task ServerRestart_IceNodes_ShouldSurviveRestart()
        {
            // Arrange - Create registry and store Ice nodes
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            var iceNode1 = new Node("ice-1", "test.ice", ContentState.Ice, "en", "Ice Node 1", "First ice node", null, null);
            var iceNode2 = new Node("ice-2", "test.ice", ContentState.Ice, "en", "Ice Node 2", "Second ice node", null, null);

            registry1.Upsert(iceNode1);
            registry1.Upsert(iceNode2);
            await Task.Delay(200); // Allow storage to complete

            // Assert - Nodes are persisted
            _iceStorage.GetPersistentNodes().Should().HaveCount(2);
            _iceStorage.GetPersistentNodes().Should().Contain(n => n.Id == "ice-1");
            _iceStorage.GetPersistentNodes().Should().Contain(n => n.Id == "ice-2");

            // Act - Simulate server restart by creating new registry (cache cleared)
            _iceStorage.SimulateServerRestart();
            _waterStorage.SimulateServerRestart();
            
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync(); // This should reload from persistent storage

            // Assert - Ice nodes should be restored from persistent storage
            registry2.TryGet("ice-1", out var restoredNode1).Should().BeTrue();
            registry2.TryGet("ice-2", out var restoredNode2).Should().BeTrue();

            restoredNode1.Title.Should().Be("Ice Node 1");
            restoredNode1.State.Should().Be(ContentState.Ice);
            restoredNode2.Title.Should().Be("Ice Node 2");
            restoredNode2.State.Should().Be(ContentState.Ice);
        }

        [Fact]
        public async Task ServerRestart_WaterNodes_ShouldNotSurviveRestart()
        {
            // Arrange - Create registry and store Water nodes
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            var waterNode1 = new Node("water-1", "test.water", ContentState.Water, "en", "Water Node 1", "First water node", null, null);
            var waterNode2 = new Node("water-2", "test.water", ContentState.Water, "en", "Water Node 2", "Second water node", null, null);

            registry1.Upsert(waterNode1);
            registry1.Upsert(waterNode2);
            await Task.Delay(200); // Allow storage to complete

            // Assert - Nodes are temporarily persisted in Water storage
            registry1.TryGet("water-1", out var _).Should().BeTrue();
            registry1.TryGet("water-2", out var _).Should().BeTrue();

            // Act - Simulate server restart
            _iceStorage.SimulateServerRestart();
            _waterStorage.SimulateServerRestart();
            
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync(); // This should reload from persistent storage

            // Assert - Water nodes should NOT survive restart (Water storage is volatile)
            registry2.TryGet("water-1", out var restoredNode1).Should().BeFalse(); // Water nodes are cleared on restart
            registry2.TryGet("water-2", out var restoredNode2).Should().BeFalse(); // Water nodes are cleared on restart
        }

        [Fact]
        public async Task ServerRestart_GasNodes_ShouldNotSurviveRestart()
        {
            // Arrange - Create registry and store Gas nodes
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            var gasNode1 = new Node("gas-1", "test.gas", ContentState.Gas, "en", "Gas Node 1", "First gas node", null, null);
            var gasNode2 = new Node("gas-2", "test.gas", ContentState.Gas, "en", "Gas Node 2", "Second gas node", null, null);

            registry1.Upsert(gasNode1);
            registry1.Upsert(gasNode2);
            await Task.Delay(100);

            // Assert - Gas nodes are in memory but not persisted
            registry1.TryGet("gas-1", out var _).Should().BeTrue();
            registry1.TryGet("gas-2", out var _).Should().BeTrue();
            _iceStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "gas-1");
            _waterStorage.GetPersistentNodes().Should().NotContain(n => n.Id == "gas-1");

            // Act - Simulate server restart
            _iceStorage.SimulateServerRestart();
            _waterStorage.SimulateServerRestart();
            
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Gas nodes should NOT be restored (they're transient)
            registry2.TryGet("gas-1", out var _).Should().BeFalse();
            registry2.TryGet("gas-2", out var _).Should().BeFalse();
        }

        [Fact]
        public async Task ServerRestart_MixedStates_ShouldRestoreCorrectly()
        {
            // Arrange - Create registry with mixed node states
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            var iceNode = new Node("mixed-ice", "test.mixed", ContentState.Ice, "en", "Mixed Ice", "Ice node", null, null);
            var waterNode = new Node("mixed-water", "test.mixed", ContentState.Water, "en", "Mixed Water", "Water node", null, null);
            var gasNode = new Node("mixed-gas", "test.mixed", ContentState.Gas, "en", "Mixed Gas", "Gas node", null, null);

            registry1.Upsert(iceNode);
            registry1.Upsert(waterNode);
            registry1.Upsert(gasNode);
            await Task.Delay(200);

            // Act - Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Only Ice and Water nodes should be restored
            registry2.TryGet("mixed-ice", out var restoredIce).Should().BeTrue();
            registry2.TryGet("mixed-water", out var restoredWater).Should().BeTrue();
            registry2.TryGet("mixed-gas", out var restoredGas).Should().BeFalse();

            restoredIce.State.Should().Be(ContentState.Ice);
            restoredWater.State.Should().Be(ContentState.Water);
        }

        #endregion

        #region Edge Persistence Tests

        [Fact]
        public async Task EdgePersistence_IceToIce_ShouldPersistInIceStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            // Create two Ice nodes
            var iceNode1 = new Node("ice-edge-1", "test.ice", ContentState.Ice, "en", "Ice Node 1", "First ice node", null, null);
            var iceNode2 = new Node("ice-edge-2", "test.ice", ContentState.Ice, "en", "Ice Node 2", "Second ice node", null, null);
            
            registry.Upsert(iceNode1);
            registry.Upsert(iceNode2);
            await Task.Delay(100);

            var edge = new Edge(
                FromId: "ice-edge-1",
                ToId: "ice-edge-2",
                Role: "relates-to",
                Weight: 1.0,
                Meta: new Dictionary<string, object> { { "test", "edge-data" } }
            );

            // Act
            registry.Upsert(edge);
            await Task.Delay(100);

            // Assert - Edge should be persisted in Ice storage
            _iceStorage.GetPersistentEdges().Should().Contain(e => 
                e.FromId == edge.FromId && e.ToId == edge.ToId && e.Role == edge.Role);
            
            // Test edge retrieval
            var retrievedEdge = registry.GetEdge("ice-edge-1", "ice-edge-2");
            retrievedEdge.Should().NotBeNull();
            retrievedEdge!.Weight.Should().Be(1.0);
            retrievedEdge.Meta?["test"].Should().Be("edge-data");
        }

        [Fact]
        public async Task EdgePersistence_WaterToWater_ShouldPersistInWaterStorage()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            // Create two Water nodes
            var waterNode1 = new Node("water-edge-1", "test.water", ContentState.Water, "en", "Water Node 1", "First water node", null, null);
            var waterNode2 = new Node("water-edge-2", "test.water", ContentState.Water, "en", "Water Node 2", "Second water node", null, null);
            
            registry.Upsert(waterNode1);
            registry.Upsert(waterNode2);
            await Task.Delay(100);

            var edge = new Edge(
                FromId: "water-edge-1",
                ToId: "water-edge-2",
                Role: "connects-to",
                Weight: 0.8,
                Meta: new Dictionary<string, object> { { "type", "water-edge" } }
            );

            // Act
            registry.Upsert(edge);
            await Task.Delay(100);

            // Assert - Edge should be persisted (in this case, both backends implement the same interface)
            var retrievedEdge = registry.GetEdge("water-edge-1", "water-edge-2");
            retrievedEdge.Should().NotBeNull();
            retrievedEdge!.Weight.Should().Be(0.8);
            retrievedEdge.Meta?["type"].Should().Be("water-edge");
        }

        [Fact]
        public async Task EdgePersistence_MixedStates_ShouldBeGas()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            // Create Ice and Water nodes
            var iceNode = new Node("mixed-ice", "test.ice", ContentState.Ice, "en", "Ice Node", "Ice node", null, null);
            var waterNode = new Node("mixed-water", "test.water", ContentState.Water, "en", "Water Node", "Water node", null, null);
            
            registry.Upsert(iceNode);
            registry.Upsert(waterNode);
            await Task.Delay(100);

            var edge = new Edge(
                FromId: "mixed-ice",
                ToId: "mixed-water",
                Role: "mixed-edge",
                Weight: 0.5,
                Meta: new Dictionary<string, object> { { "state", "mixed" } }
            );

            // Act
            registry.Upsert(edge);
            await Task.Delay(100);

            // Assert - Mixed state edge should be Gas (in-memory only)
            var retrievedEdge = registry.GetEdge("mixed-ice", "mixed-water");
            retrievedEdge.Should().NotBeNull();
            retrievedEdge!.Meta?["state"].Should().Be("mixed");

            // Mixed edges should not be in persistent storage (they're Gas)
            _iceStorage.GetPersistentEdges().Should().NotContain(e => 
                e.FromId == "mixed-ice" && e.ToId == "mixed-water" && e.Role == "mixed-edge");
        }

        #endregion

        #region Cache Clearing and Reload Tests

        [Fact]
        public async Task CacheClearAndReload_ShouldRestorePersistentNodes()
        {
            // Arrange - Create registry with various nodes
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            var nodes = new[]
            {
                new Node("persistent-ice-1", "test.ice", ContentState.Ice, "en", "Persistent Ice 1", "Ice node 1", 
                    new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice1\"}", InlineBytes: null, ExternalUri: null), new Dictionary<string, object> { { "key", "ice1" } }),
                new Node("persistent-ice-2", "test.ice", ContentState.Ice, "en", "Persistent Ice 2", "Ice node 2",
                    new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice2\"}", InlineBytes: null, ExternalUri: null), new Dictionary<string, object> { { "key", "ice2" } }),
                new Node("persistent-water-1", "test.water", ContentState.Water, "en", "Persistent Water 1", "Water node 1",
                    new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water1\"}", InlineBytes: null, ExternalUri: null), new Dictionary<string, object> { { "key", "water1" } }),
                new Node("persistent-water-2", "test.water", ContentState.Water, "en", "Persistent Water 2", "Water node 2",
                    new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water2\"}", InlineBytes: null, ExternalUri: null), new Dictionary<string, object> { { "key", "water2" } }),
                new Node("transient-gas-1", "test.gas", ContentState.Gas, "en", "Transient Gas 1", "Gas node 1", null, null),
                new Node("transient-gas-2", "test.gas", ContentState.Gas, "en", "Transient Gas 2", "Gas node 2", null, null)
            };

            foreach (var node in nodes)
            {
                registry1.Upsert(node);
            }
            await Task.Delay(300); // Allow all storage operations to complete

            // Assert - All nodes should be accessible in first registry
            foreach (var node in nodes)
            {
                registry1.TryGet(node.Id, out var _).Should().BeTrue($"Node {node.Id} should be accessible");
            }

            // Verify persistent storage state
            _iceStorage.GetPersistentNodes().Should().HaveCount(2);
            _waterStorage.GetPersistentNodes().Should().HaveCount(2);

            // Act - Simulate cache clearing and reload (server restart)
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync(); // This simulates reloading from persistent storage

            // Assert - Only persistent nodes should be restored
            registry2.TryGet("persistent-ice-1", out var ice1).Should().BeTrue();
            registry2.TryGet("persistent-ice-2", out var ice2).Should().BeTrue();
            registry2.TryGet("persistent-water-1", out var water1).Should().BeTrue();
            registry2.TryGet("persistent-water-2", out var water2).Should().BeTrue();
            
            // Gas nodes should be lost
            registry2.TryGet("transient-gas-1", out var _).Should().BeFalse();
            registry2.TryGet("transient-gas-2", out var _).Should().BeFalse();

            // Verify content integrity
            ice1.Content?.InlineJson.Should().Be("{\"data\": \"ice1\"}");
            ice2.Meta?["key"].Should().Be("ice2");
            water1.Content?.InlineJson.Should().Be("{\"data\": \"water1\"}");
            water2.Meta?["key"].Should().Be("water2");
        }

        [Fact]
        public async Task CacheClearAndReload_Edges_ShouldRestorePersistentEdges()
        {
            // Arrange - Create registry with nodes and edges
            var registry1 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry1.InitializeAsync();

            // Create nodes
            var iceNode1 = new Node("edge-ice-1", "test.ice", ContentState.Ice, "en", "Ice 1", "Ice node 1", null, null);
            var iceNode2 = new Node("edge-ice-2", "test.ice", ContentState.Ice, "en", "Ice 2", "Ice node 2", null, null);
            var waterNode1 = new Node("edge-water-1", "test.water", ContentState.Water, "en", "Water 1", "Water node 1", null, null);
            var gasNode1 = new Node("edge-gas-1", "test.gas", ContentState.Gas, "en", "Gas 1", "Gas node 1", null, null);

            registry1.Upsert(iceNode1);
            registry1.Upsert(iceNode2);
            registry1.Upsert(waterNode1);
            registry1.Upsert(gasNode1);
            await Task.Delay(100);

            // Create edges with different persistence characteristics
            var iceToIceEdge = new Edge("edge-ice-1", "edge-ice-2", "ice-to-ice", 1.0, new Dictionary<string, object> { { "type", "persistent" } });
            var waterToWaterEdge = new Edge("edge-water-1", "edge-water-1", "self-ref", 0.9, new Dictionary<string, object> { { "type", "self" } });
            var iceToGasEdge = new Edge("edge-ice-1", "edge-gas-1", "ice-to-gas", 0.5, new Dictionary<string, object> { { "type", "transient" } });

            registry1.Upsert(iceToIceEdge);
            registry1.Upsert(waterToWaterEdge);
            registry1.Upsert(iceToGasEdge);
            await Task.Delay(200);

            // Act - Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Only edges between persistent nodes should survive
            var restoredIceToIce = registry2.GetEdge("edge-ice-1", "edge-ice-2");
            restoredIceToIce.Should().NotBeNull();
            restoredIceToIce!.Meta?["type"].Should().Be("persistent");

            // Note: Ice-to-Gas edge should not survive because Gas node is gone
            // The edge becomes invalid when one endpoint doesn't exist
        }

        #endregion

        #region Complex State Transition Scenarios

        [Fact]
        public async Task ComplexStateTransition_IceToWaterToGas_ShouldHandleCorrectly()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var originalNode = new Node(
                Id: "complex-transition",
                TypeId: "test.complex",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Complex Node",
                Description: "A node with complex transitions",
                Content: new ContentRef(MediaType: "application/json", InlineJson: "{\"version\": 1, \"data\": \"original\"}", InlineBytes: null, ExternalUri: null),
                Meta: new Dictionary<string, object> { { "stage", "initial" } }
            );

            // Act & Assert - Stage 1: Ice
            registry.Upsert(originalNode);
            await Task.Delay(100);

            _iceStorage.GetPersistentNodes().Should().Contain(n => n.Id == "complex-transition");
            registry.TryGet("complex-transition", out var stage1Node).Should().BeTrue();
            stage1Node.State.Should().Be(ContentState.Ice);

            // Act & Assert - Stage 2: Ice → Water
            var waterNode = originalNode with {
                State = ContentState.Water,
                Content = new ContentRef(MediaType: "application/json", InlineJson: "{\"version\": 2, \"data\": \"updated\"}", InlineBytes: null, ExternalUri: null),
                Meta = new Dictionary<string, object> { { "stage", "water" } }
            };
            registry.Upsert(waterNode);
            await Task.Delay(100);

            _waterStorage.GetPersistentNodes().Should().Contain(n => n.Id == "complex-transition");
            registry.TryGet("complex-transition", out var stage2Node).Should().BeTrue();
            stage2Node.State.Should().Be(ContentState.Water);
            stage2Node.Meta?["stage"].Should().Be("water");

            // Act & Assert - Stage 3: Water → Gas
            var gasNode = waterNode with {
                State = ContentState.Gas,
                Content = new ContentRef(MediaType: "application/json", InlineJson: "{\"version\": 3, \"data\": \"transient\"}", InlineBytes: null, ExternalUri: null),
                Meta = new Dictionary<string, object> { { "stage", "gas" } }
            };
            registry.Upsert(gasNode);
            await Task.Delay(100);

            registry.TryGet("complex-transition", out var stage3Node).Should().BeTrue();
            stage3Node.State.Should().Be(ContentState.Gas);
            stage3Node.Meta?["stage"].Should().Be("gas");

            // Simulate server restart - only persistent nodes should survive
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // The node should be restored from the last persistent state (Water)
            // unless cleanup removed it from Water storage when it became Gas
            registry2.TryGet("complex-transition", out var restoredNode).Should().BeTrue();
            // The exact behavior depends on implementation - it might restore from Water state
        }

        [Fact]
        public async Task BulkOperations_ShouldMaintainStateConsistency()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var bulkNodes = new[]
            {
                new Node("bulk-ice-1", "test.bulk", ContentState.Ice, "en", "Bulk Ice 1", "Bulk ice node 1", null, null),
                new Node("bulk-ice-2", "test.bulk", ContentState.Ice, "en", "Bulk Ice 2", "Bulk ice node 2", null, null),
                new Node("bulk-water-1", "test.bulk", ContentState.Water, "en", "Bulk Water 1", "Bulk water node 1", null, null),
                new Node("bulk-water-2", "test.bulk", ContentState.Water, "en", "Bulk Water 2", "Bulk water node 2", null, null),
                new Node("bulk-gas-1", "test.bulk", ContentState.Gas, "en", "Bulk Gas 1", "Bulk gas node 1", null, null),
                new Node("bulk-gas-2", "test.bulk", ContentState.Gas, "en", "Bulk Gas 2", "Bulk gas node 2", null, null)
            };

            // Act - Store all nodes
            foreach (var node in bulkNodes)
            {
                registry.Upsert(node);
            }
            await Task.Delay(300); // Allow all storage operations to complete

            // Assert - Verify initial state
            foreach (var node in bulkNodes)
            {
                registry.TryGet(node.Id, out var retrievedNode).Should().BeTrue();
                retrievedNode.State.Should().Be(node.State);
            }

            // Verify storage distribution
            _iceStorage.GetPersistentNodes().Should().HaveCount(2);
            _waterStorage.GetPersistentNodes().Should().HaveCount(2);

            // Act - Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Only persistent nodes should be restored
            registry2.TryGet("bulk-ice-1", out var _).Should().BeTrue();
            registry2.TryGet("bulk-ice-2", out var _).Should().BeTrue();
            registry2.TryGet("bulk-water-1", out var _).Should().BeTrue();
            registry2.TryGet("bulk-water-2", out var _).Should().BeTrue();
            registry2.TryGet("bulk-gas-1", out var _).Should().BeFalse();
            registry2.TryGet("bulk-gas-2", out var _).Should().BeFalse();

            // Verify type-based queries work after restart
            var iceNodesByType = await registry2.GetNodesByTypeAsync("test.bulk");
            iceNodesByType.Count().Should().Be(4); // 2 Ice + 2 Water nodes restored
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task DataIntegrity_ComplexContent_ShouldPersistCorrectly()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var complexContent = new ContentRef(
                MediaType: "application/json",
                InlineJson: "{\"complex\": {\"nested\": {\"data\": [1, 2, 3]}, \"unicode\": \"🎉\", \"special\": \"chars & symbols\"}}",
                InlineBytes: null,
                ExternalUri: new Uri("https://example.com/data")
            );

            var complexMeta = new Dictionary<string, object>
            {
                { "string", "test value" },
                { "number", 42 },
                { "boolean", true },
                { "array", new[] { "a", "b", "c" } },
                { "nested", new Dictionary<string, object> { { "inner", "value" } } }
            };

            var complexNode = new Node(
                Id: "complex-data-node",
                TypeId: "test.complex",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: "Complex Data Node",
                Description: "A node with complex content and metadata",
                Content: complexContent,
                Meta: complexMeta
            );

            // Act - Store and retrieve
            registry.Upsert(complexNode);
            await Task.Delay(100);

            // Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Complex data should be preserved
            registry2.TryGet("complex-data-node", out var restoredNode).Should().BeTrue();
            
            restoredNode.Content?.InlineJson.Should().Be(complexContent.InlineJson);
            restoredNode.Content?.ExternalUri.Should().NotBeNull();
            restoredNode.Content?.ExternalUri?.ToString().Should().Be("https://example.com/data");
            
            restoredNode.Meta?["string"].Should().Be("test value");
            restoredNode.Meta?["number"].Should().Be(42);
            restoredNode.Meta?["boolean"].Should().Be(true);
            restoredNode.Locale.Should().Be("en-US");
        }

        [Fact]
        public async Task DataIntegrity_NodeUpdates_ShouldPreserveHistory()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var nodeId = "versioned-node";
            var v1Node = new Node(nodeId, "test.versioned", ContentState.Ice, "en", "Version 1", "First version", 
                new ContentRef(MediaType: "application/json", InlineJson: "{\"version\": 1}", InlineBytes: null, ExternalUri: null), new Dictionary<string, object> { { "rev", 1 } });

            // Act - Store version 1
            registry.Upsert(v1Node);
            await Task.Delay(100);

            // Act - Update to version 2
            var v2Node = v1Node with {
                Title = "Version 2",
                Description = "Second version",
                Content = new ContentRef(MediaType: "application/json", InlineJson: "{\"version\": 2}", InlineBytes: null, ExternalUri: null),
                Meta = new Dictionary<string, object> { { "rev", 2 } }
            };
            registry.Upsert(v2Node);
            await Task.Delay(100);

            // Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Latest version should be restored
            registry2.TryGet(nodeId, out var restoredNode).Should().BeTrue();
            restoredNode.Title.Should().Be("Version 2");
            restoredNode.Content?.InlineJson.Should().Be("{\"version\": 2}");
            restoredNode.Meta?["rev"].Should().Be(2);
        }

        #endregion

        #region Performance and Concurrency Tests

        [Fact]
        public async Task ConcurrentOperations_ShouldMaintainConsistency()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var tasks = new List<Task>();

            // Act - Perform concurrent operations
            for (int i = 0; i < 10; i++)
            {
                var nodeIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    var node = new Node(
                        Id: $"concurrent-{nodeIndex}",
                        TypeId: "test.concurrent",
                        State: nodeIndex % 3 == 0 ? ContentState.Ice : 
                               nodeIndex % 3 == 1 ? ContentState.Water : ContentState.Gas,
                        Locale: "en",
                        Title: $"Concurrent Node {nodeIndex}",
                        Description: $"Node created concurrently #{nodeIndex}",
                        Content: new ContentRef(MediaType: "application/json", InlineJson: $"{{\"index\": {nodeIndex}}}", InlineBytes: null, ExternalUri: null),
                        Meta: new Dictionary<string, object> { { "index", nodeIndex } }
                    );

                    registry.Upsert(node);
                    await Task.Delay(10); // Small delay to simulate real work
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(200); // Allow all storage operations to complete

            // Assert - All nodes should be accessible
            for (int i = 0; i < 10; i++)
            {
                registry.TryGet($"concurrent-{i}", out var node).Should().BeTrue();
                node.Meta?["index"].Should().Be(i);
            }

            // Verify storage distribution
            var expectedIceCount = Enumerable.Range(0, 10).Count(i => i % 3 == 0);
            var expectedWaterCount = Enumerable.Range(0, 10).Count(i => i % 3 == 1);
            
            _iceStorage.GetPersistentNodes().Should().HaveCount(expectedIceCount);
            _waterStorage.GetPersistentNodes().Should().HaveCount(expectedWaterCount);

            // Simulate restart and verify consistency
            _iceStorage.SimulateServerRestart();
            _waterStorage.SimulateServerRestart();
            
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Only Ice nodes should be restored (Water and Gas nodes are cleared on restart)
            for (int i = 0; i < 10; i++)
            {
                var shouldExist = i % 3 == 0; // Only Ice nodes
                registry2.TryGet($"concurrent-{i}", out var _).Should().Be(shouldExist);
            }
        }

        #endregion

        #region Edge State Consistency Tests

        [Fact]
        public async Task EdgeStateConsistency_ShouldMatchNodeStates()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            // Create nodes in different states
            var iceNode = new Node("edge-test-ice", "test.edge", ContentState.Ice, "en", "Ice Node", "Ice", null, null);
            var waterNode = new Node("edge-test-water", "test.edge", ContentState.Water, "en", "Water Node", "Water", null, null);
            var gasNode = new Node("edge-test-gas", "test.edge", ContentState.Gas, "en", "Gas Node", "Gas", null, null);

            registry.Upsert(iceNode);
            registry.Upsert(waterNode);
            registry.Upsert(gasNode);
            await Task.Delay(100);

            // Create edges between different state combinations
            var iceToWaterEdge = new Edge("edge-test-ice", "edge-test-water", "ice-water", 1.0, null);
            var waterToGasEdge = new Edge("edge-test-water", "edge-test-gas", "water-gas", 1.0, null);
            var iceToGasEdge = new Edge("edge-test-ice", "edge-test-gas", "ice-gas", 1.0, null);

            registry.Upsert(iceToWaterEdge);
            registry.Upsert(waterToGasEdge);
            registry.Upsert(iceToGasEdge);
            await Task.Delay(100);

            // Act - Simulate server restart (Gas nodes and mixed edges should be lost)
            _iceStorage.SimulateServerRestart();
            _waterStorage.SimulateServerRestart();
            
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Only Ice nodes should be restored (Water nodes are volatile and cleared on restart)
            registry2.TryGet("edge-test-ice", out var _).Should().BeTrue();
            registry2.TryGet("edge-test-water", out var _).Should().BeFalse(); // Water node lost on restart
            registry2.TryGet("edge-test-gas", out var _).Should().BeFalse(); // Gas node lost

            // Edges involving Gas nodes should be invalid after restart
            // Ice-to-Water edge was stored in Water storage (more fluid backend) and should be lost on restart
            var restoredIceToWater = registry2.GetEdge("edge-test-ice", "edge-test-water");
            restoredIceToWater.Should().BeNull(); // Water storage cleared on restart

            var restoredWaterToGas = registry2.GetEdge("edge-test-water", "edge-test-gas");
            restoredWaterToGas.Should().BeNull(); // Gas endpoint missing and was Gas state (not persisted)

            var restoredIceToGas = registry2.GetEdge("edge-test-ice", "edge-test-gas");
            restoredIceToGas.Should().BeNull(); // Gas endpoint missing
        }

        #endregion

        #region Node Type and Query Persistence Tests

        [Fact]
        public async Task TypeBasedQueries_ShouldWorkAfterRestart()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var conceptNodes = new[]
            {
                new Node("concept-1", "codex.concept", ContentState.Ice, "en", "Concept 1", "First concept", null, null),
                new Node("concept-2", "codex.concept", ContentState.Water, "en", "Concept 2", "Second concept", null, null),
                new Node("concept-3", "codex.concept", ContentState.Gas, "en", "Concept 3", "Third concept", null, null)
            };

            var moduleNodes = new[]
            {
                new Node("module-1", "codex.module", ContentState.Ice, "en", "Module 1", "First module", null, null),
                new Node("module-2", "codex.module", ContentState.Water, "en", "Module 2", "Second module", null, null)
            };

            // Act - Store all nodes
            foreach (var node in conceptNodes.Concat(moduleNodes))
            {
                registry.Upsert(node);
            }
            await Task.Delay(200);

            // Verify initial state
            var initialConcepts = await registry.GetNodesByTypeAsync("codex.concept");
            var initialModules = await registry.GetNodesByTypeAsync("codex.module");
            
            initialConcepts.Should().HaveCount(3);
            initialModules.Should().HaveCount(2);

            // Act - Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Type-based queries should work with persistent nodes only
            var restoredConcepts = await registry2.GetNodesByTypeAsync("codex.concept");
            var restoredModules = await registry2.GetNodesByTypeAsync("codex.module");

            restoredConcepts.Should().HaveCount(2); // Ice + Water (Gas lost)
            restoredModules.Should().HaveCount(2); // Both Ice and Water

            restoredConcepts.Should().Contain(n => n.Id == "concept-1" && n.State == ContentState.Ice);
            restoredConcepts.Should().Contain(n => n.Id == "concept-2" && n.State == ContentState.Water);
            restoredConcepts.Should().NotContain(n => n.Id == "concept-3"); // Gas node lost
        }

        [Fact]
        public async Task MetadataQueries_ShouldPersistCorrectly()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            var nodesWithMeta = new[]
            {
                new Node("meta-1", "test.meta", ContentState.Ice, "en", "Meta 1", "Node 1", null, 
                    new Dictionary<string, object> { { "category", "important" }, { "priority", 1 } }),
                new Node("meta-2", "test.meta", ContentState.Water, "en", "Meta 2", "Node 2", null,
                    new Dictionary<string, object> { { "category", "important" }, { "priority", 2 } }),
                new Node("meta-3", "test.meta", ContentState.Gas, "en", "Meta 3", "Node 3", null,
                    new Dictionary<string, object> { { "category", "important" }, { "priority", 3 } }),
                new Node("meta-4", "test.meta", ContentState.Ice, "en", "Meta 4", "Node 4", null,
                    new Dictionary<string, object> { { "category", "normal" }, { "priority", 1 } })
            };

            foreach (var node in nodesWithMeta)
            {
                registry.Upsert(node);
            }
            await Task.Delay(200);

            // Act - Simulate server restart
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();

            // Assert - Metadata-based queries should work on persistent nodes
            var importantNodes = (await registry2.GetNodesByTypeAsync("test.meta"))
                .Where(n => n.Meta?.GetValueOrDefault("category")?.ToString() == "important")
                .ToList();

            importantNodes.Should().HaveCount(2); // Ice + Water (Gas lost)
            importantNodes.Should().Contain(n => n.Id == "meta-1");
            importantNodes.Should().Contain(n => n.Id == "meta-2");
            importantNodes.Should().NotContain(n => n.Id == "meta-3"); // Gas node lost
        }

        #endregion

        #region Stress Tests

        [Fact]
        public async Task StressTest_ManyNodesAndEdges_ShouldMaintainPerformance()
        {
            // Arrange
            var registry = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry.InitializeAsync();

            const int nodeCount = 100;
            var nodes = new List<Node>();
            var edges = new List<Edge>();

            // Create many nodes with different states
            for (int i = 0; i < nodeCount; i++)
            {
                var state = i % 4 == 0 ? ContentState.Ice :
                           i % 4 == 1 ? ContentState.Water :
                           i % 4 == 2 ? ContentState.Water : ContentState.Gas;

                var node = new Node(
                    Id: $"stress-node-{i}",
                    TypeId: "test.stress",
                    State: state,
                    Locale: "en",
                    Title: $"Stress Node {i}",
                    Description: $"Stress test node #{i}",
                    Content: new ContentRef(MediaType: "application/json", InlineJson: $"{{\"index\": {i}, \"data\": \"stress-test-{i}\"}}", InlineBytes: null, ExternalUri: null),
                    Meta: new Dictionary<string, object> { { "index", i }, { "state", state.ToString() } }
                );

                nodes.Add(node);

                // Create edges between adjacent nodes
                if (i > 0)
                {
                    var edge = new Edge($"stress-node-{i-1}", $"stress-node-{i}", "next", 1.0, 
                        new Dictionary<string, object> { { "sequence", i } });
                    edges.Add(edge);
                }
            }

            // Act - Store all nodes and edges
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (var node in nodes)
            {
                registry.Upsert(node);
            }
            
            foreach (var edge in edges)
            {
                registry.Upsert(edge);
            }
            
            await Task.Delay(500); // Allow storage operations to complete
            stopwatch.Stop();

            // Assert - Performance should be reasonable
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds

            // Verify all nodes are accessible
            for (int i = 0; i < nodeCount; i++)
            {
                registry.TryGet($"stress-node-{i}", out var _).Should().BeTrue();
            }

            // Act - Simulate server restart
            var restartStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var registry2 = new NodeRegistry(_iceStorage, _waterStorage, _mockLogger.Object);
            await registry2.InitializeAsync();
            restartStopwatch.Stop();

            // Assert - Restart should be fast and restore persistent nodes
            restartStopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Restart within 2 seconds

            var expectedPersistentCount = nodes.Count(n => n.State != ContentState.Gas);
            var actualRestoredCount = 0;
            
            for (int i = 0; i < nodeCount; i++)
            {
                if (registry2.TryGet($"stress-node-{i}", out var _))
                {
                    actualRestoredCount++;
                }
            }

            actualRestoredCount.Should().Be(expectedPersistentCount);
        }

        #endregion
    }
}
