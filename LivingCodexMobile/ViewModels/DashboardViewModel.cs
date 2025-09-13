using System.Collections.ObjectModel;
using System.Windows.Input;
using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly ISignalRService _signalRService;
        private readonly IAuthenticationService _authService;
        private bool _isConnected;
        private User? _currentUser;
        private double _collectiveEnergy;
        private double _contributorEnergy;
        private ObservableCollection<Concept> _recentConcepts = new();
        private ObservableCollection<Contribution> _recentContributions = new();

        public DashboardViewModel(IApiService apiService, ISignalRService signalRService, IAuthenticationService authService)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            _authService = authService;
            Title = "Dashboard";

            RefreshCommand = new Command(async () => await RefreshDataAsync());
            ViewConceptCommand = new Command<Concept>(async (concept) => await ViewConceptAsync(concept));
            ViewContributionCommand = new Command<Contribution>(async (contribution) => await ViewContributionAsync(contribution));
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Subscribe to SignalR events
            _signalRService.CollectiveEnergyUpdated += OnCollectiveEnergyUpdated;
            _signalRService.ContributionAdded += OnContributionAdded;
            _signalRService.ConceptUpdated += OnConceptUpdated;
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public double CollectiveEnergy
        {
            get => _collectiveEnergy;
            set => SetProperty(ref _collectiveEnergy, value);
        }

        public double ContributorEnergy
        {
            get => _contributorEnergy;
            set => SetProperty(ref _contributorEnergy, value);
        }

        public ObservableCollection<Concept> RecentConcepts
        {
            get => _recentConcepts;
            set => SetProperty(ref _recentConcepts, value);
        }

        public ObservableCollection<Contribution> RecentContributions
        {
            get => _recentContributions;
            set => SetProperty(ref _recentContributions, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ViewConceptCommand { get; }
        public ICommand ViewContributionCommand { get; }
        public ICommand LogoutCommand { get; }

        public async Task InitializeAsync()
        {
            // Get current user from authentication service
            CurrentUser = await _authService.GetCurrentUserAsync();
            
            // Connect to SignalR and subscribe to real-time events
            await _signalRService.ConnectAsync();
            await SetupRealtimeSubscriptions();
            
            await RefreshDataAsync();
        }

        private async Task SetupRealtimeSubscriptions()
        {
            if (CurrentUser != null)
            {
                // Subscribe to user-specific events
                await _signalRService.SubscribeToEventTypeAsync("contribution_recorded");
                await _signalRService.SubscribeToEventTypeAsync("contribution_amplified");
                await _signalRService.SubscribeToEventTypeAsync("abundance_event");
                await _signalRService.SubscribeToEventTypeAsync("system_event");
                
                // Subscribe to user's concepts
                foreach (var concept in RecentConcepts)
                {
                    await _signalRService.SubscribeToNodeAsync(concept.Id);
                }
            }

            // Set up event handlers
            _signalRService.ContributionEventReceived += OnContributionEventReceived;
            _signalRService.AbundanceEventReceived += OnAbundanceEventReceived;
            _signalRService.SystemEventReceived += OnSystemEventReceived;
            _signalRService.NodeEventReceived += OnNodeEventReceived;
            _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Update connection status
            IsConnected = _signalRService.IsConnected;
        }

        private async Task RefreshDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;

            try
            {
                // Load collective energy
                var collectiveEnergyResponse = await _apiService.GetCollectiveEnergyAsync();
                if (collectiveEnergyResponse.Success)
                {
                    CollectiveEnergy = collectiveEnergyResponse.Data;
                }

                // Load contributor energy if user is logged in
                if (CurrentUser != null)
                {
                    var contributorEnergyResponse = await _apiService.GetContributorEnergyAsync(CurrentUser.Id);
                    if (contributorEnergyResponse.Success)
                    {
                        ContributorEnergy = contributorEnergyResponse.Data;
                    }
                }

                // Load recent concepts
                var conceptsResponse = await _apiService.GetConceptsAsync();
                if (conceptsResponse.Success && conceptsResponse.Data != null)
                {
                    RecentConcepts.Clear();
                    foreach (var concept in conceptsResponse.Data.Take(5))
                    {
                        RecentConcepts.Add(concept);
                    }
                }

                // Load recent contributions
                if (CurrentUser != null)
                {
                    var contributionsResponse = await _apiService.GetUserContributionsAsync(CurrentUser.Id);
                    if (contributionsResponse.Success && contributionsResponse.Data != null)
                    {
                        RecentContributions.Clear();
                        foreach (var contribution in contributionsResponse.Data.Take(5))
                        {
                            RecentContributions.Add(contribution);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error
                await Application.Current!.MainPage!.DisplayAlert("Error", $"Failed to refresh data: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ViewConceptAsync(Concept concept)
        {
            // Navigate to concept detail page
            await Shell.Current.GoToAsync($"concept/{concept.Id}");
        }

        private async Task ViewContributionAsync(Contribution contribution)
        {
            // Navigate to contribution detail page
            await Shell.Current.GoToAsync($"contribution/{contribution.Id}");
        }

        private void OnCollectiveEnergyUpdated(object? sender, double energy)
        {
            CollectiveEnergy = energy;
        }

        private void OnContributionAdded(object? sender, Contribution contribution)
        {
            RecentContributions.Insert(0, contribution);
            if (RecentContributions.Count > 5)
            {
                RecentContributions.RemoveAt(RecentContributions.Count - 1);
            }
        }

        private void OnConceptUpdated(object? sender, Concept concept)
        {
            var existingConcept = RecentConcepts.FirstOrDefault(c => c.Id == concept.Id);
            if (existingConcept != null)
            {
                var index = RecentConcepts.IndexOf(existingConcept);
                RecentConcepts[index] = concept;
            }
            else
            {
                RecentConcepts.Insert(0, concept);
                if (RecentConcepts.Count > 5)
                {
                    RecentConcepts.RemoveAt(RecentConcepts.Count - 1);
                }
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        private async void OnContributionEventReceived(object? sender, ContributionEvent contributionEvent)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Refresh contributions when a new one is recorded
                if (contributionEvent.EventType == "contribution_recorded")
                {
                    await RefreshDataAsync();
                }
            });
        }

        private void OnAbundanceEventReceived(object? sender, AbundanceEvent abundanceEvent)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update collective energy display
                if (abundanceEvent.EventType == "contribution_amplified")
                {
                    // You could add a CollectiveEnergy property to display this
                    System.Diagnostics.Debug.WriteLine($"Abundance event: {abundanceEvent.CollectiveValue}");
                }
            });
        }

        private void OnSystemEventReceived(object? sender, SystemEvent systemEvent)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Handle system events (notifications, updates, etc.)
                System.Diagnostics.Debug.WriteLine($"System event: {systemEvent.EventType}");
            });
        }

        private async void OnNodeEventReceived(object? sender, RealtimeEvent nodeEvent)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Handle node-specific events (concept updates, etc.)
                if (nodeEvent.NodeType == "concept")
                {
                    // Refresh concepts when they're updated
                    await RefreshDataAsync();
                }
            });
        }

        private void OnConnectionStatusChanged(object? sender, string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsConnected = _signalRService.IsConnected;
                System.Diagnostics.Debug.WriteLine($"Connection status: {status}");
            });
        }
    }
}
