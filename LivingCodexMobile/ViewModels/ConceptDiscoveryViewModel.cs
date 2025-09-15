using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels;

public class ConceptDiscoveryViewModel : INotifyPropertyChanged
{
    private readonly IConceptService _conceptService;
    private readonly ILoggingService _loggingService;
    private readonly IAuthenticationService _authService;

    private bool _isLoading;
    private string _searchQuery = string.Empty;
    private Concept? _selectedConcept;
    private ConceptFilter _filter = new();
    private ObservableCollection<ConceptCard> _concepts = new();
    private ObservableCollection<ConceptCard> _trendingConcepts = new();
    private ObservableCollection<ConceptCard> _recommendedConcepts = new();
    private ObservableCollection<ConceptCard> _interestedConcepts = new();
    private string _currentView = "discover"; // "discover", "trending", "recommended", "interested"

    public ConceptDiscoveryViewModel(
        IConceptService conceptService,
        ILoggingService loggingService,
        IAuthenticationService authService)
    {
        _conceptService = conceptService;
        _loggingService = loggingService;
        _authService = authService;

        LoadConceptsCommand = new Command(async () => await LoadConceptsAsync());
        SearchConceptsCommand = new Command(async () => await SearchConceptsAsync());
        LoadTrendingCommand = new Command(async () => await LoadTrendingConceptsAsync());
        LoadRecommendedCommand = new Command(async () => await LoadRecommendedConceptsAsync());
        LoadInterestedCommand = new Command(async () => await LoadInterestedConceptsAsync());
        SelectConceptCommand = new Command<ConceptCard>(async (concept) => await SelectConceptAsync(concept));
        ToggleInterestCommand = new Command<ConceptCard>(async (concept) => await ToggleInterestAsync(concept));
        RefreshCommand = new Command(async () => await RefreshAsync());
        ApplyFilterCommand = new Command(async () => await ApplyFilterAsync());
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

    public Concept? SelectedConcept
    {
        get => _selectedConcept;
        set => SetProperty(ref _selectedConcept, value);
    }

    public ConceptFilter Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public ObservableCollection<ConceptCard> Concepts
    {
        get => _concepts;
        set => SetProperty(ref _concepts, value);
    }

    public ObservableCollection<ConceptCard> TrendingConcepts
    {
        get => _trendingConcepts;
        set => SetProperty(ref _trendingConcepts, value);
    }

    public ObservableCollection<ConceptCard> RecommendedConcepts
    {
        get => _recommendedConcepts;
        set => SetProperty(ref _recommendedConcepts, value);
    }

    public ObservableCollection<ConceptCard> InterestedConcepts
    {
        get => _interestedConcepts;
        set => SetProperty(ref _interestedConcepts, value);
    }

    public string CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand LoadConceptsCommand { get; }
    public ICommand SearchConceptsCommand { get; }
    public ICommand LoadTrendingCommand { get; }
    public ICommand LoadRecommendedCommand { get; }
    public ICommand LoadInterestedCommand { get; }
    public ICommand SelectConceptCommand { get; }
    public ICommand ToggleInterestCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ApplyFilterCommand { get; }

    public async Task LoadConceptsAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "discover";
            
            _loggingService.LogInfo("Loading concepts...");
            var concepts = await _conceptService.GetConceptsAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Concepts.Clear();
                foreach (var concept in concepts)
                {
                    Concepts.Add(CreateConceptCard(concept));
                }
            });
            
            _loggingService.LogInfo($"Loaded {concepts.Count} concepts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load concepts", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SearchConceptsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        try
        {
            IsLoading = true;
            
            _loggingService.LogInfo($"Searching concepts: {SearchQuery}");
            var request = new ConceptSearchRequest
            {
                SearchTerm = SearchQuery,
                Take = 50
            };
            var concepts = await _conceptService.SearchConceptsAsync(request);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Concepts.Clear();
                foreach (var concept in concepts)
                {
                    Concepts.Add(CreateConceptCard(concept));
                }
            });
            
            _loggingService.LogInfo($"Found {concepts.Count} concepts");
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

    public async Task LoadTrendingConceptsAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "trending";
            
            _loggingService.LogInfo("Loading trending concepts...");
            var concepts = await _conceptService.GetTrendingConceptsAsync(20);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TrendingConcepts.Clear();
                foreach (var concept in concepts)
                {
                    TrendingConcepts.Add(CreateConceptCard(concept));
                }
            });
            
            _loggingService.LogInfo($"Loaded {concepts.Count} trending concepts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load trending concepts", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadRecommendedConceptsAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "recommended";
            
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _loggingService.LogWarning("No current user for recommendations");
                return;
            }
            
            _loggingService.LogInfo("Loading recommended concepts...");
            var concepts = await _conceptService.GetRecommendedConceptsAsync(currentUser.Id, 20);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecommendedConcepts.Clear();
                foreach (var concept in concepts)
                {
                    RecommendedConcepts.Add(CreateConceptCard(concept));
                }
            });
            
            _loggingService.LogInfo($"Loaded {concepts.Count} recommended concepts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load recommended concepts", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadInterestedConceptsAsync()
    {
        try
        {
            IsLoading = true;
            CurrentView = "interested";
            
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _loggingService.LogWarning("No current user for interested concepts");
                return;
            }
            
            _loggingService.LogInfo("Loading interested concepts...");
            var concepts = await _conceptService.GetConceptsByInterestAsync(currentUser.Id);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                InterestedConcepts.Clear();
                foreach (var concept in concepts)
                {
                    InterestedConcepts.Add(CreateConceptCard(concept));
                }
            });
            
            _loggingService.LogInfo($"Loaded {concepts.Count} interested concepts");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load interested concepts", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SelectConceptAsync(ConceptCard conceptCard)
    {
        try
        {
            SelectedConcept = conceptCard.Concept;
            _loggingService.LogInfo($"Selected concept: {conceptCard.Concept.Name}");
            
            // Load related concepts
            var relatedConcepts = await _conceptService.GetRelatedConceptsAsync(conceptCard.Concept.Id);
            conceptCard.Concept.Relationships = relatedConcepts.Select(c => new ConceptRelationship
            {
                Id = Guid.NewGuid().ToString(),
                FromConceptId = conceptCard.Concept.Id,
                ToConceptId = c.Id,
                RelationshipType = "related",
                Weight = 1.0,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to select concept", ex);
        }
    }

    public async Task ToggleInterestAsync(ConceptCard conceptCard)
    {
        try
        {
            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _loggingService.LogWarning("No current user for interest toggle");
                return;
            }

            var newInterestState = !conceptCard.Concept.IsInterested;
            var success = await _conceptService.MarkConceptInterestAsync(currentUser.Id, conceptCard.Concept.Id, newInterestState);
            
            if (success)
            {
                conceptCard.Concept.IsInterested = newInterestState;
                conceptCard.Concept.InterestCount += newInterestState ? 1 : -1;
                
                _loggingService.LogInfo($"Toggled interest for concept {conceptCard.Concept.Name}: {newInterestState}");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to toggle interest", ex);
        }
    }

    public async Task RefreshAsync()
    {
        switch (CurrentView)
        {
            case "discover":
                await LoadConceptsAsync();
                break;
            case "trending":
                await LoadTrendingConceptsAsync();
                break;
            case "recommended":
                await LoadRecommendedConceptsAsync();
                break;
            case "interested":
                await LoadInterestedConceptsAsync();
                break;
        }
    }

    public async Task ApplyFilterAsync()
    {
        try
        {
            IsLoading = true;
            
            var request = new ConceptSearchRequest
            {
                SearchTerm = Filter.SearchTerm,
                Domains = !string.IsNullOrEmpty(Filter.Domain) ? new[] { Filter.Domain } : null,
                Complexities = GetComplexityRange(),
                Tags = Filter.Tags.Any() ? Filter.Tags.ToArray() : null,
                SortBy = Filter.SortBy,
                SortDescending = Filter.SortDescending,
                Take = 100
            };
            
            var concepts = await _conceptService.SearchConceptsAsync(request);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Concepts.Clear();
                foreach (var concept in concepts)
                {
                    Concepts.Add(CreateConceptCard(concept));
                }
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to apply filter", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ConceptCard CreateConceptCard(Concept concept)
    {
        return new ConceptCard
        {
            Concept = concept,
            DisplayImage = concept.ImageUrl ?? "default_concept.png",
            ShortDescription = concept.Description.Length > 100 
                ? concept.Description.Substring(0, 100) + "..." 
                : concept.Description,
            DisplayTags = concept.Tags.Take(3).ToList()
        };
    }

    private int[]? GetComplexityRange()
    {
        if (Filter.MinComplexity.HasValue || Filter.MaxComplexity.HasValue)
        {
            var min = Filter.MinComplexity ?? 1;
            var max = Filter.MaxComplexity ?? 10;
            return Enumerable.Range(min, max - min + 1).ToArray();
        }
        return null;
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
