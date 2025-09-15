using LivingCodexMobile.Models;
using LivingCodexMobile.ViewModels;

namespace LivingCodexMobile.Views;

public partial class NewsFeedPage : ContentPage
{
    private readonly NewsFeedViewModel _viewModel;

    public NewsFeedPage(NewsFeedViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnNewsItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is NewsItem selectedNewsItem)
        {
            await _viewModel.SelectNewsItemAsync(selectedNewsItem);
            await Navigation.PushAsync(new NewsDetailPage(selectedNewsItem, _viewModel));
        }
    }
}
