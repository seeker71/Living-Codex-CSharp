using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class NodeExplorerViewModel : INotifyPropertyChanged
{
    private readonly INodeExplorerService _nodeExplorerService;
    private readonly IMediaRendererService _mediaRendererService;
    private readonly ILoggingService _loggingService;

    private bool _isLoading;
    private string _searchQuery = string.Empty;
    private Node? _selectedNode;
    private Edge? _selectedEdge;
    private ObservableCollection<Node> _nodes = new();
    private ObservableCollection<Edge> _edges = new();
    private ObservableCollection<GraphQueryResult> _searchResults = new();
    private string _currentView = "nodes"; // "nodes", "edges", "search"

    public NodeExplorerViewModel(
        INodeExplorerService nodeExplorerService,
        IMediaRendererService mediaRendererService,
        ILoggingService loggingService)
    {
        _nodeExplorerService = nodeExplorerService;
        _mediaRendererService = mediaRendererService;
        _loggingService = loggingService;

        LoadNodesCommand = new Command(async () => await LoadNodesAsync());
        LoadEdgesCommand = new Command(async () => await LoadEdgesAsync());
        SearchCommand = new Command(async () => await SearchAsync());
        SelectNodeCommand = new Command<Node>(async (node) => await SelectNodeAsync(node));
        SelectEdgeCommand = new Command<Edge>(async (edge) => await SelectEdgeAsync(edge));
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    public Node? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    public Edge? SelectedEdge
    {
        get => _selectedEdge;
        set => SetProperty(ref _selectedEdge, value);
    }

    public ObservableCollection<Node> Nodes
    {
        get => _nodes;
        set => SetProperty(ref _nodes, value);
    }

    public ObservableCollection<Edge> Edges
    {
        get => _edges;
        set => SetProperty(ref _edges, value);
    }

    public ObservableCollection<GraphQueryResult> SearchResults
    {
        get => _searchResults;
        set => SetProperty(ref _searchResults, value);
    }

    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand LoadNodesCommand { get; }
    public ICommand LoadEdgesCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand SelectNodeCommand { get; }
    public ICommand SelectEdgeCommand { get; }
    public ICommand RefreshCommand { get; }

    public async Task LoadNodesAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "nodes";
            
            _loggingService.LogInfo("Loading nodes...");
            var nodes = await _nodeExplorerService.GetNodesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Nodes.Clear();
                foreach (var node in nodes)
                {
                    Nodes.Add(node);
                }
            });
            
            _loggingService.LogInfo($"Loaded {nodes.Count} nodes");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load nodes", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadEdgesAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "edges";
            
            _loggingService.LogInfo("Loading edges...");
            var edges = await _nodeExplorerService.GetEdgesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Edges.Clear();
                foreach (var edge in edges)
                {
                    Edges.Add(edge);
                }
            });
            
            _loggingService.LogInfo($"Loaded {edges.Count} edges");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load edges", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        try
        {
            IsLoading = true;
            CurrentView = "search";
            
            _loggingService.LogInfo($"Searching for: {SearchQuery}");
            var results = await _nodeExplorerService.SearchGraphAsync(SearchQuery);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SearchResults.Clear();
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            });
            
            _loggingService.LogInfo($"Found {results.Count} search results");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Search failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SelectNodeAsync(Node node)
    {
        try
        {
            SelectedNode = node;
            _loggingService.LogInfo($"Selected node: {node.Id}");
            
            // Load edges for this node
            var edgesFrom = await _nodeExplorerService.GetEdgesFromNodeAsync(node.Id);
            var edgesTo = await _nodeExplorerService.GetEdgesToNodeAsync(node.Id);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Edges.Clear();
                foreach (var edge in edgesFrom.Concat(edgesTo))
                {
                    Edges.Add(edge);
                }
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to select node", ex);
        }
    }

    public async Task SelectEdgeAsync(Edge edge)
    {
        try
        {
            SelectedEdge = edge;
            _loggingService.LogInfo($"Selected edge: {edge.FromId} -> {edge.ToId}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to select edge", ex);
        }
    }

    public async Task RefreshAsync()
    {
        switch (CurrentView)
        {
            case "nodes":
                await LoadNodesAsync();
                break;
            case "edges":
                await LoadEdgesAsync();
                break;
            case "search":
                await SearchAsync();
                break;
        }
    }

    public async Task<Node?> GetNodeAsync(string nodeId)
    {
        try
        {
            return await _nodeExplorerService.GetNodeAsync(nodeId);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get node", ex);
            return null;
        }
    }

    public async Task<MediaRenderResult> RenderNodeContentAsync(Node node)
    {
        if (node.Content == null)
            return new MediaRenderResult { IsSuccess = false, ErrorMessage = "No content available" };

        try
        {
            return await _mediaRendererService.RenderContentAsync(node.Content);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to render node content", ex);
            return new MediaRenderResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
