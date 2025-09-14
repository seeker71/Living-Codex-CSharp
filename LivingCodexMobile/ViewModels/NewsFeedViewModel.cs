using System.Collections.ObjectModel;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class NewsFeedViewModel : BaseViewModel
{
    private readonly INewsFeedService _newsFeedService;
    private readonly IAuthenticationService _authService;
    private double _collectiveEnergy = 432.0;
    private NewsItem? _selectedNewsItem;

    public NewsFeedViewModel(INewsFeedService newsFeedService, IAuthenticationService authService)
    {
        _newsFeedService = newsFeedService;
        _authService = authService;
        
        NewsItems = new ObservableCollection<NewsItem>();
        ExtractedConcepts = new ObservableCollection<Concept>();
        
        RefreshCommand = new Command(async () => await LoadNewsFeedAsync());
        AnalyzeNewsCommand = new Command<NewsItem>(async (item) => await AnalyzeNewsItemAsync(item));
        ExtractConceptsCommand = new Command<NewsItem>(async (item) => await ExtractConceptsFromNewsAsync(item));
        
        // Load initial data
        _ = Task.Run(LoadNewsFeedAsync);
    }

    public ObservableCollection<NewsItem> NewsItems { get; }
    public ObservableCollection<Concept> ExtractedConcepts { get; }

    public double CollectiveEnergy
    {
        get => _collectiveEnergy;
        set => SetProperty(ref _collectiveEnergy, value);
    }

    public NewsItem? SelectedNewsItem
    {
        get => _selectedNewsItem;
        set => SetProperty(ref _selectedNewsItem, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand AnalyzeNewsCommand { get; }
    public ICommand ExtractConceptsCommand { get; }

    private async Task LoadNewsFeedAsync()
    {
        try
        {
            IsBusy = true;
            
            var currentUser = await _authService.GetCurrentUserAsync();
            var newsItems = currentUser != null 
                ? await _newsFeedService.GetPersonalizedNewsAsync(currentUser.Id)
                : await _newsFeedService.GetNewsFeedAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.Clear();
                foreach (var item in newsItems)
                {
                    NewsItems.Add(item);
                }
                
                // Simulate collective energy fluctuation
                CollectiveEnergy = 432.0 + (Random.Shared.NextDouble() - 0.5) * 10;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load news feed error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AnalyzeNewsItemAsync(NewsItem newsItem)
    {
        try
        {
            IsBusy = true;
            SelectedNewsItem = newsItem;
            
            var analysis = await _newsFeedService.AnalyzeNewsItemAsync(newsItem.Id);
            
            // In a real implementation, you'd show the analysis results
            await Application.Current.MainPage.DisplayAlert(
                "News Analysis", 
                $"Analyzed: {newsItem.Title}\n\nSentiment: {analysis.SentimentScore:F2}\nRelevance: {analysis.RelevanceScore:F2}\n\nKey Topics:\n{string.Join("\n", analysis.KeyTopics)}", 
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Analyze news error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to analyze news item", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExtractConceptsFromNewsAsync(NewsItem newsItem)
    {
        try
        {
            IsBusy = true;
            SelectedNewsItem = newsItem;
            
            var concepts = await _newsFeedService.ExtractConceptsAsync(newsItem.Content);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ExtractedConcepts.Clear();
                foreach (var concept in concepts)
                {
                    ExtractedConcepts.Add(concept);
                }
            });
            
            if (concepts.Any())
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Concepts Extracted", 
                    $"Found {concepts.Count} concepts from: {newsItem.Title}", 
                    "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    "No Concepts", 
                    "No concepts could be extracted from this news item.", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extract concepts error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to extract concepts", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

