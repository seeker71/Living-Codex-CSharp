#if DISABLED_COMPLEX_STORAGE_TESTS
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;
using CodexBootstrap.Tests.Modules;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Core;

/// <summary>
/// Tests for proper node and edge state transitions, ensuring edges persist in the more fluid backend
/// when endpoints are in different states (Gas > Water > Ice).
/// These tests are currently disabled as they rely on internal storage backend APIs that have been removed.
/// TODO: Rewrite these tests to use public storage-endpoints APIs instead of internal APIs.
/// </summary>
public class StateTransitionTests
{
        private readonly InMemoryIceStorageBackend _iceStorage;
        private readonly InMemoryWaterStorageBackend _waterStorage;
    private readonly ICodexLogger _logger;
    private readonly NodeRegistry _registry;

    public StateTransitionTests()
    {
        _iceStorage = new InMemoryIceStorageBackend();
        _waterStorage = new InMemoryWaterStorageBackend();
        _logger = TestInfrastructure.CreateTestLogger();
        _registry = new NodeRegistry(_iceStorage, _waterStorage, _logger);
        
        // Initialize the registry
        _registry.InitializeAsync().Wait();
    }

    #region Edge Persistence in More Fluid Backend Tests

    [Fact]
    public async Task Edge_BetweenTwoIceNodes_ShouldPersistInIceStorage()
    {
        // Arrange - Create two Ice nodes
        var iceNode1 = new Node("ice-node-1", "test-type", ContentState.Ice, null, "Ice Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice1\"}", InlineBytes: null, ExternalUri: null), null);
        var iceNode2 = new Node("ice-node-2", "test-type", ContentState.Ice, null, "Ice Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(iceNode1);
        _registry.Upsert(iceNode2);
        await Task.Delay(100); // Allow async persistence

        // Act - Create edge between Ice nodes
        var edge = new Edge("ice-node-1", "ice-node-2", "connects", 1.0, new Dictionary<string, object> { ["test"] = "ice-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Assert - Edge should be persisted in Ice storage (both endpoints are Ice)
        var iceEdges = await _iceStorage.GetAllEdgesAsync();
        Assert.True(iceEdges.Any(e => e.FromId == "ice-node-1" && e.ToId == "ice-node-2" && e.Role == "connects"), "Edge should be in Ice storage");
        // Water storage should not contain the edge
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        Assert.False(waterEdges.Any(e => e.FromId == "ice-node-1" && e.ToId == "ice-node-2" && e.Role == "connects"), "Edge should not be in Water storage");
    }

    [Fact]
    public async Task Edge_BetweenTwoWaterNodes_ShouldPersistInWaterStorage()
    {
        // Arrange - Create two Water nodes
        var waterNode1 = new Node("water-node-1", "test-type", ContentState.Water, null, "Water Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water1\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode2 = new Node("water-node-2", "test-type", ContentState.Water, null, "Water Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(waterNode1);
        _registry.Upsert(waterNode2);
        await Task.Delay(100); // Allow async persistence

        // Act - Create edge between Water nodes
        var edge = new Edge("water-node-1", "water-node-2", "flows-to", 0.8, new Dictionary<string, object> { ["test"] = "water-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Assert - Edge should be persisted in Water storage (both endpoints are Water)
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        Assert.True(waterEdges.Any(e => e.FromId == "water-node-1" && e.ToId == "water-node-2" && e.Role == "flows-to"), "Edge should be in Water storage");

        var iceEdges = await _iceStorage.GetAllEdgesAsync();
        Assert.False(iceEdges.Any(e => e.FromId == "water-node-1" && e.ToId == "water-node-2" && e.Role == "flows-to"), "Edge should not be in Ice storage");
    }

    [Fact]
    public async Task Edge_BetweenIceAndWaterNodes_ShouldPersistInWaterStorage()
    {
        // Arrange - Create one Ice node and one Water node
        var iceNode = new Node("ice-node", "test-type", ContentState.Ice, null, "Ice Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode = new Node("water-node", "test-type", ContentState.Water, null, "Water Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(iceNode);
        _registry.Upsert(waterNode);
        await Task.Delay(100); // Allow async persistence

        // Act - Create edge from Ice to Water node (more fluid backend = Water)
        var edge = new Edge("ice-node", "water-node", "connects-to", 0.5, new Dictionary<string, object> { ["test"] = "mixed-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Assert - Edge should be persisted in Water storage (more fluid state)
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        Assert.True(waterEdges.Any(e => e.FromId == "ice-node" && e.ToId == "water-node" && e.Role == "connects-to"), "Edge should be in Water storage");

        var iceEdges = await _iceStorage.GetAllEdgesAsync();
        Assert.False(iceEdges.Any(e => e.FromId == "ice-node" && e.ToId == "water-node" && e.Role == "connects-to"), "Edge should not be in Ice storage");
    }

    [Fact]
    public async Task Edge_BetweenWaterAndGasNodes_ShouldNotPersist()
    {
        // Arrange - Create one Water node and one Gas node
        var waterNode = new Node("water-node", "test-type", ContentState.Water, null, "Water Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water\"}", InlineBytes: null, ExternalUri: null), null);
        var gasNode = new Node("gas-node", "test-type", ContentState.Gas, null, "Gas Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"gas\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(waterNode);
        _registry.Upsert(gasNode);
        await Task.Delay(100); // Allow async persistence

        // Act - Create edge from Water to Gas node (most fluid state = Gas, not persisted)
        var edge = new Edge("water-node", "gas-node", "evaporates-to", 0.3, new Dictionary<string, object> { ["test"] = "gas-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Assert - Edge should NOT be persisted in any storage (Gas state = in-memory only)
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        waterEdges.Should().NotContain(e => e.FromId == "water-node" && e.ToId == "gas-node" && e.Role == "evaporates-to");

        var iceEdges = _iceStorage.GetAllEdgesAsync();
        iceEdges.Should().NotContain(e => e.FromId == "water-node" && e.ToId == "gas-node" && e.Role == "evaporates-to");

        // But edge should be retrievable from registry (in-memory)
        var retrievedEdge = _registry.GetEdge("water-node", "gas-node");
        retrievedEdge.Should().NotBeNull();
        retrievedEdge!.Role.Should().Be("evaporates-to");
    }

    [Fact]
    public async Task Edge_BetweenIceAndGasNodes_ShouldNotPersist()
    {
        // Arrange - Create one Ice node and one Gas node
        var iceNode = new Node("ice-node", "test-type", ContentState.Ice, null, "Ice Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice\"}", InlineBytes: null, ExternalUri: null), null);
        var gasNode = new Node("gas-node", "test-type", ContentState.Gas, null, "Gas Node", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"gas\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(iceNode);
        _registry.Upsert(gasNode);
        await Task.Delay(100); // Allow async persistence

        // Act - Create edge from Ice to Gas node (most fluid state = Gas, not persisted)
        var edge = new Edge("ice-node", "gas-node", "sublimates-to", 0.1, new Dictionary<string, object> { ["test"] = "sublimation-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Assert - Edge should NOT be persisted in any storage (Gas state = in-memory only)
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        waterEdges.Should().NotContain(e => e.FromId == "ice-node" && e.ToId == "gas-node" && e.Role == "sublimates-to");

        var iceEdges = _iceStorage.GetAllEdgesAsync();
        iceEdges.Should().NotContain(e => e.FromId == "ice-node" && e.ToId == "gas-node" && e.Role == "sublimates-to");

        // But edge should be retrievable from registry (in-memory)
        var retrievedEdge = _registry.GetEdge("ice-node", "gas-node");
        retrievedEdge.Should().NotBeNull();
        retrievedEdge!.Role.Should().Be("sublimates-to");
    }

    #endregion

    #region Node State Transition and Edge Migration Tests

    [Fact]
    public async Task NodeStateTransition_FromWaterToIce_ShouldMigrateEdgesToIce()
    {
        // Arrange - Create two Water nodes with an edge
        var waterNode1 = new Node("transition-node-1", "test-type", ContentState.Water, null, "Water Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water1\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode2 = new Node("transition-node-2", "test-type", ContentState.Water, null, "Water Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(waterNode1);
        _registry.Upsert(waterNode2);
        
        var edge = new Edge("transition-node-1", "transition-node-2", "connects", 1.0, new Dictionary<string, object> { ["test"] = "transition-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Verify edge is initially in Water storage
        var initialWaterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        initialWaterEdges.Should().Contain(e => e.FromId == "transition-node-1" && e.ToId == "transition-node-2" && e.Role == "connects");

        // Act - Transition both nodes to Ice state
        var iceNode1 = waterNode1 with { State = ContentState.Ice };
        var iceNode2 = waterNode2 with { State = ContentState.Ice };
        
        _registry.Upsert(iceNode1);
        _registry.Upsert(iceNode2);
        await Task.Delay(200); // Allow async persistence and edge migration

        // Assert - Edge should now be in Ice storage
        var iceEdges = _iceStorage.GetAllEdgesAsync();
        iceEdges.Should().Contain(e => e.FromId == "transition-node-1" && e.ToId == "transition-node-2" && e.Role == "connects");

        var finalWaterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        finalWaterEdges.Should().NotContain(e => e.FromId == "transition-node-1" && e.ToId == "transition-node-2" && e.Role == "connects");
    }

    [Fact]
    public async Task NodeStateTransition_FromIceToWater_ShouldMigrateEdgesToWater()
    {
        // Arrange - Create two Ice nodes with an edge
        var iceNode1 = new Node("melt-node-1", "test-type", ContentState.Ice, null, "Ice Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice1\"}", InlineBytes: null, ExternalUri: null), null);
        var iceNode2 = new Node("melt-node-2", "test-type", ContentState.Ice, null, "Ice Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(iceNode1);
        _registry.Upsert(iceNode2);
        
        var edge = new Edge("melt-node-1", "melt-node-2", "connects", 1.0, new Dictionary<string, object> { ["test"] = "melt-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Verify edge is initially in Ice storage
        var initialIceEdges = _iceStorage.GetAllEdgesAsync();
        initialIceEdges.Should().Contain(e => e.FromId == "melt-node-1" && e.ToId == "melt-node-2" && e.Role == "connects");

        // Act - Transition both nodes to Water state
        var waterNode1 = iceNode1 with { State = ContentState.Water };
        var waterNode2 = iceNode2 with { State = ContentState.Water };
        
        _registry.Upsert(waterNode1);
        _registry.Upsert(waterNode2);
        await Task.Delay(200); // Allow async persistence and edge migration

        // Assert - Edge should now be in Water storage
        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        waterEdges.Should().Contain(e => e.FromId == "melt-node-1" && e.ToId == "melt-node-2" && e.Role == "connects");

        var finalIceEdges = _iceStorage.GetAllEdgesAsync();
        finalIceEdges.Should().NotContain(e => e.FromId == "melt-node-1" && e.ToId == "melt-node-2" && e.Role == "connects");
    }

    [Fact]
    public async Task NodeStateTransition_OneNodeToGas_ShouldMakeEdgeGas()
    {
        // Arrange - Create two Water nodes with an edge
        var waterNode1 = new Node("evaporate-node-1", "test-type", ContentState.Water, null, "Water Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water1\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode2 = new Node("evaporate-node-2", "test-type", ContentState.Water, null, "Water Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(waterNode1);
        _registry.Upsert(waterNode2);
        
        var edge = new Edge("evaporate-node-1", "evaporate-node-2", "connects", 1.0, new Dictionary<string, object> { ["test"] = "evaporate-edge" });
        _registry.Upsert(edge);
        await Task.Delay(100); // Allow async persistence

        // Verify edge is initially in Water storage
        var initialWaterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        initialWaterEdges.Should().Contain(e => e.FromId == "evaporate-node-1" && e.ToId == "evaporate-node-2" && e.Role == "connects");

        // Act - Transition one node to Gas state
        var gasNode1 = waterNode1 with { State = ContentState.Gas };
        _registry.Upsert(gasNode1);
        await Task.Delay(200); // Allow async persistence and edge migration

        // Assert - Edge should no longer be in persistent storage (becomes Gas)
        var finalWaterEdges = await _waterStorage.GetAllWaterEdgesAsync();
        finalWaterEdges.Should().NotContain(e => e.FromId == "evaporate-node-1" && e.ToId == "evaporate-node-2" && e.Role == "connects");

        var iceEdges = _iceStorage.GetAllEdgesAsync();
        iceEdges.Should().NotContain(e => e.FromId == "evaporate-node-1" && e.ToId == "evaporate-node-2" && e.Role == "connects");

        // But edge should still be retrievable from registry (in-memory)
        var retrievedEdge = _registry.GetEdge("evaporate-node-1", "evaporate-node-2");
        retrievedEdge.Should().NotBeNull();
        retrievedEdge!.Role.Should().Be("connects");
    }

    #endregion

    #region Server Restart and Persistence Tests

    [Fact]
    public async Task ServerRestart_ShouldMaintainEdgePersistenceInCorrectBackends()
    {
        // Arrange - Create nodes in different states with edges
        var iceNode1 = new Node("restart-ice-1", "test-type", ContentState.Ice, null, "Ice Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice1\"}", InlineBytes: null, ExternalUri: null), null);
        var iceNode2 = new Node("restart-ice-2", "test-type", ContentState.Ice, null, "Ice Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"ice2\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode1 = new Node("restart-water-1", "test-type", ContentState.Water, null, "Water Node 1", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water1\"}", InlineBytes: null, ExternalUri: null), null);
        var waterNode2 = new Node("restart-water-2", "test-type", ContentState.Water, null, "Water Node 2", "Description", 
            new ContentRef(MediaType: "application/json", InlineJson: "{\"data\": \"water2\"}", InlineBytes: null, ExternalUri: null), null);

        _registry.Upsert(iceNode1);
        _registry.Upsert(iceNode2);
        _registry.Upsert(waterNode1);
        _registry.Upsert(waterNode2);

        var iceEdge = new Edge("restart-ice-1", "restart-ice-2", "ice-connects", 1.0, new Dictionary<string, object> { ["type"] = "ice" });
        var waterEdge = new Edge("restart-water-1", "restart-water-2", "water-flows", 0.8, new Dictionary<string, object> { ["type"] = "water" });
        var mixedEdge = new Edge("restart-ice-1", "restart-water-1", "ice-to-water", 0.5, new Dictionary<string, object> { ["type"] = "mixed" });

        _registry.Upsert(iceEdge);
        _registry.Upsert(waterEdge);
        _registry.Upsert(mixedEdge);
        await Task.Delay(200); // Allow async persistence

        // Verify initial state
        var iceEdges = _iceStorage.GetAllEdgesAsync().ToList();
        iceEdges.Should().Contain(e => e != null && e.FromId == "restart-ice-1" && e.ToId == "restart-ice-2" && e.Role == "ice-connects");
        iceEdges.Should().NotContain(e => e != null && e.FromId == "restart-ice-1" && e.ToId == "restart-water-1" && e.Role == "ice-to-water");

        var waterEdges = await _waterStorage.GetAllWaterEdgesAsync().ToList();
        waterEdges.Should().Contain(e => e != null && e.FromId == "restart-water-1" && e.ToId == "restart-water-2" && e.Role == "water-flows");
        waterEdges.Should().Contain(e => e != null && e.FromId == "restart-ice-1" && e.ToId == "restart-water-1" && e.Role == "ice-to-water");

        // Act - Simulate server restart
        // Simulate server restart by clearing any potential cache
        // (In real scenario, this would clear the cache but data would remain in storage)
        // Ice storage persists across restarts

        // Create new registry to simulate restart
        var newRegistry = new NodeRegistry(_iceStorage, _waterStorage, _logger);
        await newRegistry.InitializeAsync();

        // Assert - Only Ice edges should survive restart
        var postRestartIceEdges = _iceStorage.GetAllEdgesAsync().ToList();
        Assert.True(postRestartIceEdges.Any(e => e != null && e.FromId == "restart-ice-1" && e.ToId == "restart-ice-2" && e.Role == "ice-connects"), "Ice edge should survive restart");

        var postRestartWaterEdges = await _waterStorage.GetAllWaterEdgesAsync().ToList();
        Assert.Empty(postRestartWaterEdges); // Water storage cleared on restart

        // Mixed edge should be gone (was in Water storage)
        Assert.False(postRestartWaterEdges.Any(e => e != null && e.FromId == "restart-ice-1" && e.ToId == "restart-water-1" && e.Role == "ice-to-water"), "Mixed edge should be gone from Water storage after restart");
    }

    #endregion
}
#endif