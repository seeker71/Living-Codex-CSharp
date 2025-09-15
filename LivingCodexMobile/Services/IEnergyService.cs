using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

/// <summary>
/// Service for managing energy and contribution data
/// </summary>
public interface IEnergyService
{
    /// <summary>
    /// Get collective energy value
    /// </summary>
    Task<double> GetCollectiveEnergyAsync();

    /// <summary>
    /// Get contributor energy for a specific user
    /// </summary>
    Task<double> GetContributorEnergyAsync(string userId);

    /// <summary>
    /// Get recent contributions for a user
    /// </summary>
    Task<List<Contribution>> GetRecentContributionsAsync(string userId, int limit = 5);

    /// <summary>
    /// Get contribution statistics
    /// </summary>
    Task<ContributionStats> GetContributionStatsAsync(string userId);

    /// <summary>
    /// Record a new contribution
    /// </summary>
    Task<Contribution> RecordContributionAsync(ContributionRequest request);
}

/// <summary>
/// Contribution statistics model
/// </summary>
public class ContributionStats
{
    public string UserId { get; set; } = string.Empty;
    public int TotalContributions { get; set; }
    public double TotalEnergy { get; set; }
    public double AverageResonance { get; set; }
    public DateTime LastContribution { get; set; }
    public Dictionary<string, int> ContributionsByType { get; set; } = new();
}

/// <summary>
/// Contribution request model
/// </summary>
public class ContributionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NodeId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
