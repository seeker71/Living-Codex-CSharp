using System.Text.Json;

namespace CodexBootstrap.Core;

/// <summary>
/// Unified storage backend that combines Ice and Water storage backends
/// </summary>
public class UnifiedStorageBackend : IStorageBackend
{
    private readonly Storage.IIceStorageBackend _iceStorage;
    private readonly Storage.IWaterStorageBackend _waterStorage;
    private readonly ICodexLogger _logger;

    public UnifiedStorageBackend(Storage.IIceStorageBackend iceStorage, Storage.IWaterStorageBackend waterStorage, ICodexLogger logger)
    {
        _iceStorage = iceStorage ?? throw new ArgumentNullException(nameof(iceStorage));
        _waterStorage = waterStorage ?? throw new ArgumentNullException(nameof(waterStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync()
    {
        await _iceStorage.InitializeAsync();
        await _waterStorage.InitializeAsync();
        _logger.Info("UnifiedStorageBackend initialized");
    }

    public async Task StoreNodeAsync(Node node)
    {
        switch (node.State)
        {
            case ContentState.Ice:
                await _iceStorage.StoreIceNodeAsync(node);
                break;
            case ContentState.Water:
                await _waterStorage.StoreWaterNodeAsync(node);
                break;
            case ContentState.Gas:
                // Gas nodes are not persisted
                _logger.Debug($"Gas node {node.Id} not persisted");
                break;
        }
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        // Try Ice storage first
        var iceNode = await _iceStorage.GetIceNodeAsync(id);
        if (iceNode != null) return iceNode;

        // Try Water storage
        var waterNode = await _waterStorage.GetWaterNodeAsync(id);
        if (waterNode != null) return waterNode;

        return null;
    }

    public async Task<IEnumerable<Node>> GetAllNodesAsync()
    {
        var iceNodes = await _iceStorage.GetAllIceNodesAsync();
        var waterNodes = await _waterStorage.GetAllWaterNodesAsync();
        return iceNodes.Concat(waterNodes);
    }

    public async Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId)
    {
        var iceNodes = await _iceStorage.GetIceNodesByTypeAsync(typeId);
        var waterNodes = await _waterStorage.GetWaterNodesByTypeAsync(typeId);
        return iceNodes.Concat(waterNodes);
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        await _iceStorage.StoreEdgeAsync(edge);
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        return await _iceStorage.GetAllEdgesAsync();
    }

    public async Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
    {
        return await _iceStorage.GetEdgesFromAsync(fromId);
    }

    public async Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
    {
        return await _iceStorage.GetEdgesToAsync(toId);
    }

    public async Task DeleteNodeAsync(string id)
    {
        // Try to delete from both storages
        await _iceStorage.DeleteIceNodeAsync(id);
        await _waterStorage.DeleteWaterNodeAsync(id);
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        await _iceStorage.DeleteEdgeAsync(fromId, toId, role);
    }

    public async Task<bool> IsAvailableAsync()
    {
        var iceAvailable = await _iceStorage.IsAvailableAsync();
        var waterAvailable = await _waterStorage.IsAvailableAsync();
        return iceAvailable && waterAvailable;
    }

    public async Task<StorageStats> GetStatsAsync()
    {
        var iceStats = await _iceStorage.GetStatsAsync();
        var waterStats = await _waterStorage.GetStatsAsync();
        
        return new StorageStats(
            NodeCount: iceStats.IceNodeCount + waterStats.WaterNodeCount,
            EdgeCount: iceStats.EdgeCount,
            TotalSizeBytes: iceStats.TotalSizeBytes + waterStats.TotalSizeBytes,
            LastUpdated: DateTime.UtcNow
        );
    }
}
