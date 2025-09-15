using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class NodeExplorerPage : ContentPage
{
    private readonly NodeExplorerViewModel _viewModel;

    public NodeExplorerPage(NodeExplorerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnNodeSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Node selectedNode)
        {
            await _viewModel.SelectNodeAsync(selectedNode);
            await Navigation.PushAsync(new NodeDetailPage(selectedNode, _viewModel));
        }
    }

    private async void OnEdgeSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Edge selectedEdge)
        {
            await _viewModel.SelectEdgeAsync(selectedEdge);
            await Navigation.PushAsync(new EdgeDetailPage(selectedEdge, _viewModel));
        }
    }
}
