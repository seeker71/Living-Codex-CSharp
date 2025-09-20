using CodexBootstrap.Core;
using CodexBootstrap.Core.Storage;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Tests.Modules;

/// <summary>
/// Test infrastructure for module testing
/// </summary>
public static class TestInfrastructure
{
    /// <summary>
    /// Creates a test NodeRegistry with mock storage backends
    /// </summary>
    public static NodeRegistry CreateTestNodeRegistry()
    {
        var iceStorage = new TestIceStorageBackend();
        var waterStorage = new TestWaterStorageBackend();
        var logger = CreateTestLogger();
        return new NodeRegistry(iceStorage, waterStorage, logger);
    }

    /// <summary>
    /// Creates a test logger
    /// </summary>
    public static TestLogger CreateTestLogger()
    {
        return new TestLogger();
    }
}

/// <summary>
/// Test Ice storage backend
/// </summary>
public class TestIceStorageBackend : IIceStorageBackend
{
    private readonly Dictionary<string, Node> _nodes = new();
    private readonly Dictionary<string, Edge> _edges = new();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task<Node?> GetIceNodeAsync(string id) => Task.FromResult(_nodes.TryGetValue(id, out var node) ? node : null);
    public Task StoreIceNodeAsync(Node node)
    {
        _nodes[node.Id] = node;
        return Task.CompletedTask;
    }
    public Task DeleteIceNodeAsync(string id)
    {
        _nodes.Remove(id);
        return Task.CompletedTask;
    }
    public Task<IEnumerable<Node>> GetAllIceNodesAsync() => Task.FromResult(_nodes.Values.AsEnumerable());
    public Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId) => 
        Task.FromResult(_nodes.Values.Where(n => n.TypeId == typeId));
    public Task StoreEdgeAsync(Edge edge)
    {
        var key = $"{edge.FromId}-{edge.ToId}-{edge.Role}";
        _edges[key] = edge;
        return Task.CompletedTask;
    }
    public Task<IEnumerable<Edge>> GetAllEdgesAsync() => Task.FromResult(_edges.Values.AsEnumerable());
    public Task<IEnumerable<Edge>> GetEdgesFromAsync(string nodeId) => 
        Task.FromResult(_edges.Values.Where(e => e.FromId == nodeId));
    public Task<IEnumerable<Edge>> GetEdgesToAsync(string nodeId) => 
        Task.FromResult(_edges.Values.Where(e => e.ToId == nodeId));
    public Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        var key = $"{fromId}-{toId}-{role}";
        _edges.Remove(key);
        return Task.CompletedTask;
    }
    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
    public Task<IceStorageStats> GetStatsAsync() => Task.FromResult(new IceStorageStats(
        IceNodeCount: _nodes.Count,
        EdgeCount: _edges.Count,
        TotalSizeBytes: 0,
        LastUpdated: DateTime.UtcNow,
        BackendType: "Test",
        BackendStats: new Dictionary<string, object>()
    ));
    public Task BatchStoreIceNodesAsync(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
        {
            _nodes[node.Id] = node;
        }
        return Task.CompletedTask;
    }
    public Task BatchStoreEdgesAsync(IEnumerable<Edge> edges)
    {
        foreach (var edge in edges)
        {
            var key = $"{edge.FromId}-{edge.ToId}-{edge.Role}";
            _edges[key] = edge;
        }
        return Task.CompletedTask;
    }
    public Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100) => 
        Task.FromResult(_nodes.Values.Where(n => n.Title?.Contains(query) == true || n.Description?.Contains(query) == true).Take(limit));
    public Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100) => 
        Task.FromResult(_nodes.Values.Where(n => n.Meta?.ContainsKey(key) == true && n.Meta[key]?.Equals(value) == true).Take(limit));

    // Additional methods for NodePersistenceTests
    public IEnumerable<Node> GetPersistentNodes() => _nodes.Values;
    public IEnumerable<Edge> GetPersistentEdges() => _edges.Values;
    public void SimulateServerRestart() { /* Ice storage persists across restarts, no action needed */ }
}

/// <summary>
/// Test Water storage backend
/// </summary>
public class TestWaterStorageBackend : IWaterStorageBackend
{
    private readonly Dictionary<string, Node> _nodes = new();
    private readonly Dictionary<string, Edge> _edges = new();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task<Node?> GetWaterNodeAsync(string id) => Task.FromResult(_nodes.TryGetValue(id, out var node) ? node : null);
    public Task StoreWaterNodeAsync(Node node, TimeSpan? expiry = null)
    {
        _nodes[node.Id] = node;
        return Task.CompletedTask;
    }
    public Task DeleteWaterNodeAsync(string id)
    {
        _nodes.Remove(id);
        return Task.CompletedTask;
    }
    public Task CleanupExpiredNodesAsync()
    {
        // Test implementation - no cleanup needed
        return Task.CompletedTask;
    }
    public Task<IEnumerable<Node>> GetAllWaterNodesAsync() => Task.FromResult(_nodes.Values.AsEnumerable());
    public Task<IEnumerable<Node>> GetWaterNodesByTypeAsync(string typeId) => 
        Task.FromResult(_nodes.Values.Where(n => n.TypeId == typeId));
    public Task<bool> IsAvailableAsync() => Task.FromResult(true);
    public Task<WaterStorageStats> GetStatsAsync() => Task.FromResult(new WaterStorageStats(
        WaterNodeCount: _nodes.Count,
        ExpiredNodeCount: 0,
        TotalSizeBytes: 0,
        LastUpdated: DateTime.UtcNow,
        AverageExpiry: TimeSpan.Zero,
        BackendStats: new Dictionary<string, object>()
    ));
    public Task BatchStoreWaterNodesAsync(IEnumerable<Node> nodes, TimeSpan? expiry = null)
    {
        foreach (var node in nodes)
        {
            _nodes[node.Id] = node;
        }
        return Task.CompletedTask;
    }
    public Task<IEnumerable<Node>> SearchWaterNodesAsync(string query, int limit = 100) => 
        Task.FromResult(_nodes.Values.Where(n => n.Title?.Contains(query) == true || n.Description?.Contains(query) == true).Take(limit));
    public Task<IEnumerable<Node>> GetWaterNodesByMetaAsync(string key, object value, int limit = 100) => 
        Task.FromResult(_nodes.Values.Where(n => n.Meta?.ContainsKey(key) == true && n.Meta[key]?.Equals(value) == true).Take(limit));

    // Edge operations for Water storage
    public Task StoreWaterEdgeAsync(Edge edge, TimeSpan? expiry = null)
    {
        var key = $"{edge.FromId}-{edge.ToId}-{edge.Role}";
        _edges[key] = edge;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Edge>> GetAllWaterEdgesAsync() => Task.FromResult(_edges.Values.AsEnumerable());

    public Task<IEnumerable<Edge>> GetWaterEdgesFromAsync(string fromId) => 
        Task.FromResult(_edges.Values.Where(e => e.FromId == fromId));

    public Task<IEnumerable<Edge>> GetWaterEdgesToAsync(string toId) => 
        Task.FromResult(_edges.Values.Where(e => e.ToId == toId));

    public Task DeleteWaterEdgeAsync(string fromId, string toId, string role)
    {
        var key = $"{fromId}-{toId}-{role}";
        _edges.Remove(key);
        return Task.CompletedTask;
    }

    // Additional methods for NodePersistenceTests
    public IEnumerable<Node> GetPersistentNodes() => _nodes.Values;
    public IEnumerable<Edge> GetPersistentEdges() => _edges.Values;
    public void SimulateServerRestart() { _nodes.Clear(); _edges.Clear(); /* Water storage is volatile, cleared on restart */ }
}

/// <summary>
/// Test logger implementation
/// </summary>
public class TestLogger : ICodexLogger
{
    public void Debug(string message) { }
    public void Debug(string message, Exception ex) { }
    public void Info(string message) { }
    public void Info(string message, Exception ex) { }
    public void Warn(string message) { }
    public void Warn(string message, Exception ex) { }
    public void Error(string message) { }
    public void Error(string message, Exception? ex = null) { }
    public void Fatal(string message) { }
    public void Fatal(string message, Exception ex) { }
}
