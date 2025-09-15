using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class NewsDetailPage : ContentPage
{
    private readonly NewsItem _newsItem;
    private readonly NewsFeedViewModel _viewModel;

    public NewsDetailPage(NewsItem newsItem, NewsFeedViewModel viewModel)
    {
        InitializeComponent();
        _newsItem = newsItem;
        _viewModel = viewModel;
        
        BindingContext = _newsItem;
    }

    private async void OnExtractConceptsClicked(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.ExtractConceptsAsync(_newsItem);
            
            // Show concepts section
            ConceptsSection.IsVisible = true;
            ConceptsCollectionView.ItemsSource = _viewModel.ExtractedConcepts;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to extract concepts: {ex.Message}", "OK");
        }
    }

    private async void OnRelatedNewsClicked(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.LoadRelatedNewsAsync(_newsItem);
            
            // Show related news section
            RelatedNewsSection.IsVisible = true;
            RelatedNewsCollectionView.ItemsSource = _viewModel.RelatedNews;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load related news: {ex.Message}", "OK");
        }
    }

    private async void OnMarkAsReadClicked(object? sender, EventArgs e)
    {
        try
        {
            await _viewModel.MarkAsReadAsync(_newsItem);
            await DisplayAlert("Success", "News item marked as read", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to mark as read: {ex.Message}", "OK");
        }
    }

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        try
        {
            // Implement news sharing
            await DisplayAlert("Share", "News sharing not yet implemented", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to share news: {ex.Message}", "OK");
        }
    }
}
