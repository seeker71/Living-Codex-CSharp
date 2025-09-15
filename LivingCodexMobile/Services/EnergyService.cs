using LivingCodexMobile.Models;
using System.Text.Json;

namespace LivingCodexMobile.Services;

/// <summary>
/// Service for managing energy and contribution data using real API calls
/// </summary>
public class EnergyService : IEnergyService
{
    private readonly IApiService _apiService;
    private readonly ILoggingService _loggingService;

    public EnergyService(IApiService apiService, ILoggingService loggingService)
    {
        _apiService = apiService;
        _loggingService = loggingService;
    }

    public async Task<double> GetCollectiveEnergyAsync()
    {
        try
        {
            _loggingService.Info("Fetching collective energy from API");
            
            // Call the server's collective energy endpoint
            var response = await _apiService.GetAsync<EnergyResponse>("/contributions/abundance/collective-energy");
            
            if (response?.Success == true)
            {
                _loggingService.Info($"Retrieved collective energy: {response.Energy}");
                return response.Energy;
            }
            
            _loggingService.Warn("Failed to retrieve collective energy, using fallback value");
            return 0.0;
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Error fetching collective energy: {ex.Message}");
            return 0.0;
        }
    }

    public async Task<double> GetContributorEnergyAsync(string userId)
    {
        try
        {
            _loggingService.Info($"Fetching contributor energy for user: {userId}");
            
            var response = await _apiService.GetAsync<EnergyResponse>($"/contributions/abundance/contributor-energy/{userId}");
            
            if (response?.Success == true)
            {
                _loggingService.Info($"Retrieved contributor energy: {response.Energy}");
                return response.Energy;
            }
            
            _loggingService.Warn($"Failed to retrieve contributor energy for user {userId}, using fallback value");
            return 0.0;
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Error fetching contributor energy: {ex.Message}");
            return 0.0;
        }
    }

    public async Task<List<Contribution>> GetRecentContributionsAsync(string userId, int limit = 5)
    {
        try
        {
            _loggingService.Info($"Fetching recent contributions for user: {userId}, limit: {limit}");
            
            var response = await _apiService.GetAsync<ContributionsResponse>($"/contributions/user/{userId}?limit={limit}");
            
            if (response?.Success == true)
            {
                _loggingService.Info($"Retrieved {response.Contributions.Count} contributions");
                return response.Contributions;
            }
            
            _loggingService.Warn($"Failed to retrieve contributions for user {userId}");
            return new List<Contribution>();
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Error fetching contributions: {ex.Message}");
            return new List<Contribution>();
        }
    }

    public async Task<ContributionStats> GetContributionStatsAsync(string userId)
    {
        try
        {
            _loggingService.Info($"Fetching contribution stats for user: {userId}");
            
            var response = await _apiService.GetAsync<ContributionStatsResponse>($"/contributions/insights/{userId}");
            
            if (response?.Success == true)
            {
                _loggingService.Info($"Retrieved contribution stats for user {userId}");
                return response.Stats;
            }
            
            _loggingService.Warn($"Failed to retrieve contribution stats for user {userId}");
            return new ContributionStats { UserId = userId };
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Error fetching contribution stats: {ex.Message}");
            return new ContributionStats { UserId = userId };
        }
    }

    public async Task<Contribution> RecordContributionAsync(ContributionRequest request)
    {
        try
        {
            _loggingService.Info($"Recording contribution: {request.Title}");
            
            var response = await _apiService.PostAsync<ContributionRequest, ContributionResponse>("/contributions/record", request);
            
            if (response?.Success == true)
            {
                _loggingService.Info($"Successfully recorded contribution: {response.Contribution.Id}");
                return response.Contribution;
            }
            
            throw new InvalidOperationException("Failed to record contribution");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Error recording contribution: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Energy response model
/// </summary>
public class EnergyResponse
{
    public bool Success { get; set; }
    public double Energy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Contributions response model
/// </summary>
public class ContributionsResponse
{
    public bool Success { get; set; }
    public List<Contribution> Contributions { get; set; } = new();
    public int TotalCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Contribution stats response model
/// </summary>
public class ContributionStatsResponse
{
    public bool Success { get; set; }
    public ContributionStats Stats { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Contribution response model
/// </summary>
public class ContributionResponse
{
    public bool Success { get; set; }
    public Contribution Contribution { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
