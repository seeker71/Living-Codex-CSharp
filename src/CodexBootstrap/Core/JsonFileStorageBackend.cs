using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexBootstrap.Core;

/// <summary>
/// JSON file-based storage backend
/// </summary>
public class JsonFileStorageBackend : IStorageBackend
{
    private readonly string _dataDirectory;
    private readonly string _nodesFile;
    private readonly string _edgesFile;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonFileStorageBackend(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        _nodesFile = Path.Combine(_dataDirectory, "nodes.json");
        _edgesFile = Path.Combine(_dataDirectory, "edges.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task InitializeAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            // Ensure data directory exists
            Directory.CreateDirectory(_dataDirectory);

            // Initialize empty files if they don't exist
            if (!File.Exists(_nodesFile))
            {
                await File.WriteAllTextAsync(_nodesFile, "[]");
            }

            if (!File.Exists(_edgesFile))
            {
                await File.WriteAllTextAsync(_edgesFile, "[]");
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task StoreNodeAsync(Node node)
    {
        await _fileLock.WaitAsync();
        try
        {
            var nodes = await LoadNodesAsync();
            nodes[node.Id] = node;
            await SaveNodesAsync(nodes);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Node?> GetNodeAsync(string id)
    {
        await _fileLock.WaitAsync();
        try
        {
            var nodes = await LoadNodesAsync();
            return nodes.TryGetValue(id, out var node) ? node : null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<Node>> GetAllNodesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var nodes = await LoadNodesAsync();
            return nodes.Values;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId)
    {
        var allNodes = await GetAllNodesAsync();
        return allNodes.Where(n => n.TypeId == typeId);
    }

    public async Task StoreEdgeAsync(Edge edge)
    {
        await _fileLock.WaitAsync();
        try
        {
            var edges = await LoadEdgesAsync();
            edges.Add(edge);
            await SaveEdgesAsync(edges);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<Edge>> GetAllEdgesAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            return await LoadEdgesAsync();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IEnumerable<Edge>> GetEdgesFromAsync(string fromId)
    {
        var allEdges = await GetAllEdgesAsync();
        return allEdges.Where(e => e.FromId == fromId);
    }

    public async Task<IEnumerable<Edge>> GetEdgesToAsync(string toId)
    {
        var allEdges = await GetAllEdgesAsync();
        return allEdges.Where(e => e.ToId == toId);
    }

    public async Task DeleteNodeAsync(string id)
    {
        await _fileLock.WaitAsync();
        try
        {
            var nodes = await LoadNodesAsync();
            if (nodes.Remove(id))
            {
                await SaveNodesAsync(nodes);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteEdgeAsync(string fromId, string toId, string role)
    {
        await _fileLock.WaitAsync();
        try
        {
            var edges = await LoadEdgesAsync();
            var edgeToRemove = edges.FirstOrDefault(e => e.FromId == fromId && e.ToId == toId && e.Role == role);
            if (edgeToRemove != null)
            {
                edges.Remove(edgeToRemove);
                await SaveEdgesAsync(edges);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return Directory.Exists(_dataDirectory) && 
                   File.Exists(_nodesFile) && 
                   File.Exists(_edgesFile);
        }
        catch
        {
            return false;
        }
    }

    public async Task<StorageStats> GetStatsAsync()
    {
        var nodes = await GetAllNodesAsync();
        var edges = await GetAllEdgesAsync();
        
        var totalSize = 0L;
        if (File.Exists(_nodesFile))
        {
            totalSize += new FileInfo(_nodesFile).Length;
        }
        if (File.Exists(_edgesFile))
        {
            totalSize += new FileInfo(_edgesFile).Length;
        }

        return new StorageStats(
            NodeCount: nodes.Count(),
            EdgeCount: edges.Count(),
            TotalSizeBytes: totalSize,
            LastUpdated: DateTime.UtcNow
        );
    }

    private async Task<Dictionary<string, Node>> LoadNodesAsync()
    {
        if (!File.Exists(_nodesFile))
        {
            return new Dictionary<string, Node>();
        }

        var json = await File.ReadAllTextAsync(_nodesFile);
        var nodes = JsonSerializer.Deserialize<List<Node>>(json, _jsonOptions) ?? new List<Node>();
        return nodes.ToDictionary(n => n.Id, n => n);
    }

    private async Task<List<Edge>> LoadEdgesAsync()
    {
        if (!File.Exists(_edgesFile))
        {
            return new List<Edge>();
        }

        var json = await File.ReadAllTextAsync(_edgesFile);
        return JsonSerializer.Deserialize<List<Edge>>(json, _jsonOptions) ?? new List<Edge>();
    }

    private async Task SaveNodesAsync(Dictionary<string, Node> nodes)
    {
        var json = JsonSerializer.Serialize(nodes.Values, _jsonOptions);
        await File.WriteAllTextAsync(_nodesFile, json);
    }

    private async Task SaveEdgesAsync(List<Edge> edges)
    {
        var json = JsonSerializer.Serialize(edges, _jsonOptions);
        await File.WriteAllTextAsync(_edgesFile, json);
    }

    public void Dispose()
    {
        _fileLock?.Dispose();
    }
}
