using System.Collections.Concurrent;

namespace CodexBootstrap.Core.Storage
{
    public class InMemoryIceStorageBackend : IIceStorageBackend
    {
        private readonly ConcurrentDictionary<string, Node> _nodes = new();
        private readonly ConcurrentDictionary<string, Edge> _edges = new();

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task StoreIceNodeAsync(Node node)
        {
            _nodes[node.Id] = node;
            return Task.CompletedTask;
        }

        public Task<Node?> GetIceNodeAsync(string id)
        {
            _nodes.TryGetValue(id, out var node);
            return Task.FromResult(node);
        }

        public Task<IEnumerable<Node>> GetAllIceNodesAsync()
        {
            return Task.FromResult(_nodes.Values.AsEnumerable());
        }

        public Task<IEnumerable<Node>> GetIceNodesByTypeAsync(string typeId)
        {
            var nodes = _nodes.Values.Where(n => n.TypeId == typeId);
            return Task.FromResult(nodes);
        }

        public Task StoreEdgeAsync(Edge edge)
        {
            var edgeKey = $"{edge.FromId}->{edge.ToId}:{edge.Role}";
            _edges[edgeKey] = edge;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Edge>> GetAllEdgesAsync()
        {
            return Task.FromResult(_edges.Values.AsEnumerable());
        }

        public Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
        {
            var edges = _edges.Values.Where(e => e.FromId == fromId);
            return Task.FromResult(edges);
        }

        public Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
        {
            var edges = _edges.Values.Where(e => e.ToId == toId);
            return Task.FromResult(edges);
        }

        public Task DeleteIceNodeAsync(string id)
        {
            _nodes.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task DeleteEdgeAsync(string fromId, string toId, string role)
        {
            var edgeKey = $"{fromId}->{toId}:{role}";
            _edges.TryRemove(edgeKey, out _);
            return Task.CompletedTask;
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
        }

        public Task<IceStorageStats> GetStatsAsync()
        {
            var stats = new IceStorageStats(
                IceNodeCount: _nodes.Count,
                EdgeCount: _edges.Count,
                TotalSizeBytes: 0, // In-memory, no size tracking
                LastUpdated: DateTime.UtcNow,
                BackendType: "InMemory",
                BackendStats: new Dictionary<string, object>()
            );
            return Task.FromResult(stats);
        }

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
                var edgeKey = $"{edge.FromId}->{edge.ToId}:{edge.Role}";
                _edges[edgeKey] = edge;
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Node>> SearchIceNodesAsync(string query, int limit = 100)
        {
            var results = _nodes.Values
                .Where(n => n.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                           n.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                .Take(limit);
            return Task.FromResult(results);
        }

        public Task<IEnumerable<Node>> GetIceNodesByMetaAsync(string key, object value, int limit = 100)
        {
            var results = _nodes.Values
                .Where(n => n.Meta.ContainsKey(key) && n.Meta[key].Equals(value))
                .Take(limit);
            return Task.FromResult(results);
        }

        public void Dispose()
        {
            // Nothing to dispose for in-memory storage
        }
    }
}
