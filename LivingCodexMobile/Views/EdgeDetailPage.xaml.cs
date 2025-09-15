using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class EdgeDetailPage : ContentPage
{
    private readonly Edge _edge;
    private readonly NodeExplorerViewModel _viewModel;

    public EdgeDetailPage(Edge edge, NodeExplorerViewModel viewModel)
    {
        InitializeComponent();
        _edge = edge;
        _viewModel = viewModel;
        
        BindingContext = _edge;
        LoadMetadata();
    }

    private void LoadMetadata()
    {
        try
        {
            if (_edge.Meta != null)
            {
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(_edge.Meta, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                MetadataLabel.Text = metadataJson;
            }
            else
            {
                MetadataLabel.Text = "No metadata available";
            }
        }
        catch (Exception ex)
        {
            MetadataLabel.Text = $"Error loading metadata: {ex.Message}";
        }
    }

    private async void OnViewFromNodeClicked(object? sender, EventArgs e)
    {
        try
        {
            var fromNode = await _viewModel.GetNodeAsync(_edge.FromId);
            if (fromNode != null)
            {
                await Navigation.PushAsync(new NodeDetailPage(fromNode, _viewModel));
            }
            else
            {
                await DisplayAlert("Error", "Could not load from node", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load from node: {ex.Message}", "OK");
        }
    }

    private async void OnViewToNodeClicked(object? sender, EventArgs e)
    {
        try
        {
            var toNode = await _viewModel.GetNodeAsync(_edge.ToId);
            if (toNode != null)
            {
                await Navigation.PushAsync(new NodeDetailPage(toNode, _viewModel));
            }
            else
            {
                await DisplayAlert("Error", "Could not load to node", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load to node: {ex.Message}", "OK");
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        // TODO: Implement edge editing
        await DisplayAlert("Edit", "Edge editing not yet implemented", "OK");
    }
}
