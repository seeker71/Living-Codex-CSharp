using LivingCodexMobile.Models;
using LivingCodexMobile.Services;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class NodeDetailPage : ContentPage
{
    private readonly Node _node;
    private readonly NodeExplorerViewModel _viewModel;
    private readonly IMediaRendererService _mediaRendererService;

    public NodeDetailPage(Node node, NodeExplorerViewModel viewModel)
    {
        InitializeComponent();
        _node = node;
        _viewModel = viewModel;
        _mediaRendererService = viewModel.GetType().GetProperty("MediaRendererService")?.GetValue(viewModel) as IMediaRendererService 
            ?? throw new InvalidOperationException("MediaRendererService not available");
        
        BindingContext = _node;
        LoadContent();
    }

    private async void LoadContent()
    {
        try
        {
            // Load metadata
            if (_node.Meta != null)
            {
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(_node.Meta, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                MetadataLabel.Text = metadataJson;
            }

            // Render content based on media type
            if (_node.Content != null)
            {
                var renderResult = await _mediaRendererService.RenderContentAsync(_node.Content);
                
                if (renderResult.IsSuccess)
                {
                    await RenderContent(renderResult);
                }
                else
                {
                    ContentRenderer.Content = new Label 
                    { 
                        Text = $"Error rendering content: {renderResult.ErrorMessage}",
                        TextColor = Colors.Red
                    };
                }
            }
            else
            {
                ContentRenderer.Content = new Label 
                { 
                    Text = "No content available",
                    TextColor = Colors.Gray,
                    FontAttributes = FontAttributes.Italic
                };
            }
        }
        catch (Exception ex)
        {
            ContentRenderer.Content = new Label 
            { 
                Text = $"Error loading content: {ex.Message}",
                TextColor = Colors.Red
            };
        }
    }

    private async Task RenderContent(MediaRenderResult renderResult)
    {
        switch (renderResult.MediaType)
        {
            case "text/plain":
                ContentRenderer.Content = new Label 
                { 
                    Text = renderResult.RenderedContent,
                    FontFamily = "Courier"
                };
                break;

            case "text/markdown":
                // For now, display as plain text. In a real app, you'd use a markdown renderer
                ContentRenderer.Content = new ScrollView
                {
                    Content = new Label 
                    { 
                        Text = renderResult.RenderedContent,
                        FontFamily = "Courier"
                    }
                };
                break;

            case "text/html":
                // For now, display as plain text. In a real app, you'd use a web view
                ContentRenderer.Content = new ScrollView
                {
                    Content = new Label 
                    { 
                        Text = renderResult.RenderedContent,
                        FontFamily = "Courier"
                    }
                };
                break;

            case "application/json":
                ContentRenderer.Content = new ScrollView
                {
                    Content = new Label 
                    { 
                        Text = renderResult.RenderedContent,
                        FontFamily = "Courier"
                    }
                };
                break;

            case var type when type.StartsWith("image/"):
                ContentRenderer.Content = new Image 
                { 
                    Source = ImageSource.FromUri(new Uri(renderResult.RenderedContent)),
                    Aspect = Aspect.AspectFit
                };
                break;

            case var type when type.StartsWith("video/"):
                ContentRenderer.Content = new Label 
                { 
                    Text = $"Video content: {renderResult.RenderedContent}",
                    TextColor = Colors.Blue
                };
                break;

            case var type when type.StartsWith("audio/"):
                ContentRenderer.Content = new Label 
                { 
                    Text = $"Audio content: {renderResult.RenderedContent}",
                    TextColor = Colors.Blue
                };
                break;

            default:
                ContentRenderer.Content = new Label 
                { 
                    Text = renderResult.RenderedContent,
                    FontFamily = "Courier"
                };
                break;
        }
    }

    private async void OnViewEdgesClicked(object? sender, EventArgs e)
    {
        await _viewModel.SelectNodeAsync(_node);
        await Navigation.PopAsync();
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        // TODO: Implement node editing
        await DisplayAlert("Edit", "Node editing not yet implemented", "OK");
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        // TODO: Implement node sharing
        await DisplayAlert("Share", "Node sharing not yet implemented", "OK");
    }
}
