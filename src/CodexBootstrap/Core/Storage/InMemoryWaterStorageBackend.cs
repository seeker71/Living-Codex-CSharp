using System.Collections.Concurrent;

namespace CodexBootstrap.Core.Storage
{
    public class InMemoryWaterStorageBackend : IWaterStorageBackend
    {
        private readonly ConcurrentDictionary<string, (Node node, DateTime? expiry)> _nodes = new();
        private readonly ConcurrentDictionary<string, (Edge edge, DateTime? expiry)> _edges = new();

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task StoreWaterNodeAsync(Node node, TimeSpan? expiry = null)
        {
            var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
            _nodes[node.Id] = (node, expiryTime);
            return Task.CompletedTask;
        }

        public Task<Node?> GetWaterNodeAsync(string id)
        {
            if (_nodes.TryGetValue(id, out var nodeData))
            {
                // Check if expired
                if (nodeData.expiry.HasValue && DateTime.UtcNow > nodeData.expiry.Value)
                {
                    _nodes.TryRemove(id, out _);
                    return Task.FromResult<Node?>(null);
                }
                return Task.FromResult<Node?>(nodeData.node);
            }
            return Task.FromResult<Node?>(null);
        }

        public Task<IEnumerable<Node>> GetAllWaterNodesAsync()
        {
            var now = DateTime.UtcNow;
            var validNodes = _nodes.Values
                .Where(n => !n.expiry.HasValue || now <= n.expiry.Value)
                .Select(n => n.node);
            return Task.FromResult(validNodes);
        }

        public Task<IEnumerable<Node>> GetWaterNodesByTypeAsync(string typeId)
        {
            var now = DateTime.UtcNow;
            var validNodes = _nodes.Values
                .Where(n => (!n.expiry.HasValue || now <= n.expiry.Value) && n.node.TypeId == typeId)
                .Select(n => n.node);
            return Task.FromResult(validNodes);
        }

        public Task DeleteWaterNodeAsync(string id)
        {
            _nodes.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        public Task CleanupExpiredNodesAsync()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _nodes
                .Where(kvp => kvp.Value.expiry.HasValue && now > kvp.Value.expiry.Value)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _nodes.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
        }

        public Task<WaterStorageStats> GetStatsAsync()
        {
            var now = DateTime.UtcNow;
            var validNodes = _nodes.Values.Where(n => !n.expiry.HasValue || now <= n.expiry.Value).Count();
            var expiredNodes = _nodes.Values.Where(n => n.expiry.HasValue && now > n.expiry.Value).Count();

            var stats = new WaterStorageStats(
                WaterNodeCount: validNodes,
                ExpiredNodeCount: expiredNodes,
                TotalSizeBytes: 0, // In-memory, no size tracking
                LastUpdated: DateTime.UtcNow,
                AverageExpiry: TimeSpan.Zero, // Not tracking for in-memory
                BackendStats: new Dictionary<string, object>()
            );
            return Task.FromResult(stats);
        }

        public Task BatchStoreWaterNodesAsync(IEnumerable<Node> nodes, TimeSpan? expiry = null)
        {
            var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
            foreach (var node in nodes)
            {
                _nodes[node.Id] = (node, expiryTime);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Node>> SearchWaterNodesAsync(string query, int limit = 100)
        {
            var now = DateTime.UtcNow;
            var results = _nodes.Values
                .Where(n => (!n.expiry.HasValue || now <= n.expiry.Value) &&
                           (n.node.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                            n.node.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                .Select(n => n.node)
                .Take(limit);
            return Task.FromResult(results);
        }

        public Task StoreWaterEdgeAsync(Edge edge, TimeSpan? expiry = null)
        {
            var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
            var edgeKey = $"{edge.FromId}->{edge.ToId}:{edge.Role}";
            _edges[edgeKey] = (edge, expiryTime);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Edge>> GetAllWaterEdgesAsync()
        {
            var now = DateTime.UtcNow;
            var validEdges = _edges.Values
                .Where(e => !e.expiry.HasValue || now <= e.expiry.Value)
                .Select(e => e.edge);
            return Task.FromResult(validEdges);
        }

        public Task<IEnumerable<Edge>> GetWaterEdgesFromAsync(string fromId)
        {
            var now = DateTime.UtcNow;
            var validEdges = _edges.Values
                .Where(e => (!e.expiry.HasValue || now <= e.expiry.Value) && e.edge.FromId == fromId)
                .Select(e => e.edge);
            return Task.FromResult(validEdges);
        }

        public Task<IEnumerable<Edge>> GetWaterEdgesToAsync(string toId)
        {
            var now = DateTime.UtcNow;
            var validEdges = _edges.Values
                .Where(e => (!e.expiry.HasValue || now <= e.expiry.Value) && e.edge.ToId == toId)
                .Select(e => e.edge);
            return Task.FromResult(validEdges);
        }

        public Task DeleteWaterEdgeAsync(string fromId, string toId, string role)
        {
            var edgeKey = $"{fromId}->{toId}:{role}";
            _edges.TryRemove(edgeKey, out _);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // Nothing to dispose for in-memory storage
        }
    }
}
