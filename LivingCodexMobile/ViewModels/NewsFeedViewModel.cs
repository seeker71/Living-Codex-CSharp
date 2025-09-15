using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class NewsFeedViewModel : INotifyPropertyChanged
{
    private readonly INewsFeedService _newsFeedService;
    private readonly IConceptService _conceptService;
    private readonly ILoggingService _loggingService;
    private readonly IAuthenticationService _authService;

    private bool _isLoading;
    private string _searchQuery = string.Empty;
    private NewsItem? _selectedNewsItem;
    private ObservableCollection<NewsItem> _newsItems = new();
    private ObservableCollection<TrendingTopic> _trendingTopics = new();
    private ObservableCollection<Concept> _extractedConcepts = new();
    private ObservableCollection<NewsItem> _relatedNews = new();
    private string _currentView = "feed"; // "feed", "trending", "concepts", "related"
    private bool _showUnreadOnly = false;

    public NewsFeedViewModel(
        INewsFeedService newsFeedService,
        IConceptService conceptService,
        ILoggingService loggingService,
        IAuthenticationService authService)
    {
        _newsFeedService = newsFeedService;
        _conceptService = conceptService;
        _loggingService = loggingService;
        _authService = authService;

        LoadNewsFeedCommand = new Command(async () => await LoadNewsFeedAsync());
        SearchNewsCommand = new Command(async () => await SearchNewsAsync());
        LoadTrendingCommand = new Command(async () => await LoadTrendingTopicsAsync());
        SelectNewsItemCommand = new Command<NewsItem>(async (newsItem) => await SelectNewsItemAsync(newsItem));
        ExtractConceptsCommand = new Command<NewsItem>(async (newsItem) => await ExtractConceptsAsync(newsItem));
        LoadRelatedNewsCommand = new Command<NewsItem>(async (newsItem) => await LoadRelatedNewsAsync(newsItem));
        MarkAsReadCommand = new Command<NewsItem>(async (newsItem) => await MarkAsReadAsync(newsItem));
        RefreshCommand = new Command(async () => await RefreshAsync());
        ToggleUnreadFilterCommand = new Command(async () => await ToggleUnreadFilterAsync());
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

    public NewsItem? SelectedNewsItem
    {
        get => _selectedNewsItem;
        set => SetProperty(ref _selectedNewsItem, value);
    }

    public ObservableCollection<NewsItem> NewsItems
    {
        get => _newsItems;
        set => SetProperty(ref _newsItems, value);
    }

    public ObservableCollection<TrendingTopic> TrendingTopics
    {
        get => _trendingTopics;
        set => SetProperty(ref _trendingTopics, value);
    }

    public ObservableCollection<Concept> ExtractedConcepts
    {
        get => _extractedConcepts;
        set => SetProperty(ref _extractedConcepts, value);
    }

    public ObservableCollection<NewsItem> RelatedNews
    {
        get => _relatedNews;
        set => SetProperty(ref _relatedNews, value);
    }

    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public bool ShowUnreadOnly
    {
        get => _showUnreadOnly;
        set => SetProperty(ref _showUnreadOnly, value);
    }

    public ICommand LoadNewsFeedCommand { get; }
    public ICommand SearchNewsCommand { get; }
    public ICommand LoadTrendingCommand { get; }
    public ICommand SelectNewsItemCommand { get; }
    public ICommand ExtractConceptsCommand { get; }
    public ICommand LoadRelatedNewsCommand { get; }
    public ICommand MarkAsReadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ToggleUnreadFilterCommand { get; }

    public async Task LoadNewsFeedAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "feed";
            
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _loggingService.LogWarning("No current user for news feed");
                return;
            }
            
            _loggingService.LogInfo("Loading news feed...");
            var newsItems = ShowUnreadOnly 
                ? await _newsFeedService.GetUnreadNewsAsync(currentUser.Id, 50)
                : await _newsFeedService.GetNewsFeedAsync(currentUser.Id, 50);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.Clear();
                foreach (var newsItem in newsItems)
                {
                    NewsItems.Add(newsItem);
                }
            });
            
            _loggingService.LogInfo($"Loaded {newsItems.Count} news items");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load news feed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchNewsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        try
        {
            IsLoading = true;
            
            _loggingService.LogInfo($"Searching news: {SearchQuery}");
            var request = new NewsSearchRequest
            {
                Interests = new List<string> { SearchQuery },
                Limit = 50
            };
            var newsItems = await _newsFeedService.SearchNewsAsync(request);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.Clear();
                foreach (var newsItem in newsItems)
                {
                    NewsItems.Add(newsItem);
                }
            });
            
            _loggingService.LogInfo($"Found {newsItems.Count} news items");
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

    public async Task LoadTrendingTopicsAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "trending";
            
            _loggingService.LogInfo("Loading trending topics...");
            var trendingTopics = await _newsFeedService.GetTrendingTopicsAsync(20);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TrendingTopics.Clear();
                foreach (var topic in trendingTopics)
                {
                    TrendingTopics.Add(topic);
                }
            });
            
            _loggingService.LogInfo($"Loaded {trendingTopics.Count} trending topics");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load trending topics", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SelectNewsItemAsync(NewsItem newsItem)
    {
        try
        {
            SelectedNewsItem = newsItem;
            _loggingService.LogInfo($"Selected news item: {newsItem.Title}");
            
            // Mark as read
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser != null)
            {
                await MarkAsReadAsync(newsItem);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to select news item", ex);
        }
    }

    public async Task ExtractConceptsAsync(NewsItem newsItem)
    {
        try
        {
            IsLoading = true;
            CurrentView = "concepts";
            
            _loggingService.LogInfo($"Extracting concepts from news: {newsItem.Title}");
            var concepts = await _newsFeedService.ExtractConceptsFromNewsAsync(newsItem.Id);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ExtractedConcepts.Clear();
                foreach (var concept in concepts)
                {
                    ExtractedConcepts.Add(concept);
                }
            });
            
            _loggingService.LogInfo($"Extracted {concepts.Count} concepts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to extract concepts", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadRelatedNewsAsync(NewsItem newsItem)
    {
        try
        {
            IsLoading = true;
            CurrentView = "related";
            
            _loggingService.LogInfo($"Loading related news for: {newsItem.Title}");
            var relatedNews = await _newsFeedService.GetRelatedNewsAsync(newsItem.Id, 20);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RelatedNews.Clear();
                foreach (var news in relatedNews)
                {
                    RelatedNews.Add(news);
                }
            });
            
            _loggingService.LogInfo($"Loaded {relatedNews.Count} related news items");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load related news", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task MarkAsReadAsync(NewsItem newsItem)
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null) return;

            var success = await _newsFeedService.MarkNewsAsReadAsync(currentUser.Id, newsItem.Id);
            if (success)
            {
                _loggingService.LogInfo($"Marked news as read: {newsItem.Title}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to mark news as read", ex);
        }
    }

    public async Task RefreshAsync()
    {
        switch (CurrentView)
        {
            case "feed":
                await LoadNewsFeedAsync();
                break;
            case "trending":
                await LoadTrendingTopicsAsync();
                break;
            case "concepts":
                if (SelectedNewsItem != null)
                    await ExtractConceptsAsync(SelectedNewsItem);
                break;
            case "related":
                if (SelectedNewsItem != null)
                    await LoadRelatedNewsAsync(SelectedNewsItem);
                break;
        }
    }

    public async Task ToggleUnreadFilterAsync()
    {
        ShowUnreadOnly = !ShowUnreadOnly;
        await LoadNewsFeedAsync();
    }

    public async Task<NewsItem?> GetNewsItemAsync(string newsId)
    {
        try
        {
            return await _newsFeedService.GetNewsItemAsync(newsId);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to get news item", ex);
            return null;
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