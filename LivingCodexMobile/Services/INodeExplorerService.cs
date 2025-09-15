using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface INodeExplorerService
{
    Task<Node?> GetNodeAsync(string nodeId);
    Task<List<Node>> GetNodesAsync(NodeListQuery? query = null);
    Task<List<Node>> SearchNodesAsync(NodeSearchRequest request);
    Task<Edge?> GetEdgeAsync(string fromId, string toId);
    Task<List<Edge>> GetEdgesAsync(EdgeListQuery? query = null);
    Task<List<Edge>> GetEdgesFromNodeAsync(string nodeId);
    Task<List<Edge>> GetEdgesToNodeAsync(string nodeId);
    Task<List<GraphQueryResult>> SearchGraphAsync(string query, Dictionary<string, object>? filters = null);
}

public class NodeExplorerService : INodeExplorerService
{
    private readonly IApiService _apiService;

    public NodeExplorerService(IApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<Node?> GetNodeAsync(string nodeId)
    {
        try
        {
            var response = await _apiService.GetAsync<NodeResponse>($"/storage-endpoints/nodes/{nodeId}");
            return response?.Node;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get node error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Node>> GetNodesAsync(NodeListQuery? query = null)
    {
        try
        {
            var queryParams = query != null ? BuildQueryString(query) : "";
            var response = await _apiService.GetAsync<NodeListResponse>($"/storage-endpoints/nodes{queryParams}");
            return response?.Nodes ?? new List<Node>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get nodes error: {ex.Message}");
            return new List<Node>();
        }
    }

    public async Task<List<Node>> SearchNodesAsync(NodeSearchRequest request)
    {
        try
        {
            var response = await _apiService.PostAsync<NodeSearchRequest, NodeSearchResponse>("/storage-endpoints/nodes/search", request);
            return response?.Nodes ?? new List<Node>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search nodes error: {ex.Message}");
            return new List<Node>();
        }
    }

    public async Task<Edge?> GetEdgeAsync(string fromId, string toId)
    {
        try
        {
            var response = await _apiService.GetAsync<EdgeResponse>($"/storage-endpoints/edges/{fromId}/{toId}");
            return response?.Edge;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get edge error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Edge>> GetEdgesAsync(EdgeListQuery? query = null)
    {
        try
        {
            var queryParams = query != null ? BuildEdgeQueryString(query) : "";
            var response = await _apiService.GetAsync<EdgeListResponse>($"/storage-endpoints/edges{queryParams}");
            return response?.Edges ?? new List<Edge>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get edges error: {ex.Message}");
            return new List<Edge>();
        }
    }

    public async Task<List<Edge>> GetEdgesFromNodeAsync(string nodeId)
    {
        try
        {
            var query = new EdgeListQuery { FromId = nodeId };
            return await GetEdgesAsync(query);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get edges from node error: {ex.Message}");
            return new List<Edge>();
        }
    }

    public async Task<List<Edge>> GetEdgesToNodeAsync(string nodeId)
    {
        try
        {
            var query = new EdgeListQuery { ToId = nodeId };
            return await GetEdgesAsync(query);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get edges to node error: {ex.Message}");
            return new List<Edge>();
        }
    }

    public async Task<List<GraphQueryResult>> SearchGraphAsync(string query, Dictionary<string, object>? filters = null)
    {
        try
        {
            var request = new GraphSearchRequest { Query = query, Filters = filters };
            var response = await _apiService.PostAsync<GraphSearchRequest, GraphSearchResponse>("/graph/search", request);
            return response?.Results ?? new List<GraphQueryResult>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Graph search error: {ex.Message}");
            return new List<GraphQueryResult>();
        }
    }

    private string BuildQueryString(NodeListQuery query)
    {
        var parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(query.TypeId))
            parameters.Add($"typeId={Uri.EscapeDataString(query.TypeId)}");
        if (query.State.HasValue)
            parameters.Add($"state={query.State.Value}");
        if (!string.IsNullOrEmpty(query.Locale))
            parameters.Add($"locale={Uri.EscapeDataString(query.Locale)}");
        if (!string.IsNullOrEmpty(query.SearchTerm))
            parameters.Add($"searchTerm={Uri.EscapeDataString(query.SearchTerm)}");
        if (query.Skip.HasValue)
            parameters.Add($"skip={query.Skip.Value}");
        if (query.Take.HasValue)
            parameters.Add($"take={query.Take.Value}");

        return parameters.Any() ? "?" + string.Join("&", parameters) : "";
    }

    private string BuildEdgeQueryString(EdgeListQuery query)
    {
        var parameters = new List<string>();
        
        if (!string.IsNullOrEmpty(query.FromId))
            parameters.Add($"fromId={Uri.EscapeDataString(query.FromId)}");
        if (!string.IsNullOrEmpty(query.ToId))
            parameters.Add($"toId={Uri.EscapeDataString(query.ToId)}");
        if (!string.IsNullOrEmpty(query.Role))
            parameters.Add($"role={Uri.EscapeDataString(query.Role)}");
        if (query.MinWeight.HasValue)
            parameters.Add($"minWeight={query.MinWeight.Value}");
        if (query.MaxWeight.HasValue)
            parameters.Add($"maxWeight={query.MaxWeight.Value}");
        if (query.Skip.HasValue)
            parameters.Add($"skip={query.Skip.Value}");
        if (query.Take.HasValue)
            parameters.Add($"take={query.Take.Value}");

        return parameters.Any() ? "?" + string.Join("&", parameters) : "";
    }
}
