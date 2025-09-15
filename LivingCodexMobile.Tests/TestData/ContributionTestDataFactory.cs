using LivingCodexMobile.Models;

namespace LivingCodexMobile.Tests.TestData;

/// <summary>
/// Factory class for creating test contribution data
/// </summary>
public static class ContributionTestDataFactory
{
    /// <summary>
    /// Creates a test contribution
    /// </summary>
    public static Contribution CreateTestContribution(string? id = null, string? userId = null, string? type = null)
    {
        return new Contribution
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserId = userId ?? "test-user-123",
            Type = type ?? "concept_creation",
            Description = "Test contribution",
            Energy = 25.0,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    /// <summary>
    /// Creates a list of test contributions
    /// </summary>
    public static List<Contribution> CreateTestContributions(int count = 5)
    {
        var contributions = new List<Contribution>();
        for (int i = 0; i < count; i++)
        {
            contributions.Add(CreateTestContribution(
                id: $"test-contribution-{i + 1}",
                userId: "test-user-123",
                type: GetContributionType(i)
            ));
        }
        return contributions;
    }

    /// <summary>
    /// Gets a contribution type based on index
    /// </summary>
    private static string GetContributionType(int index)
    {
        var types = new[]
        {
            "concept_creation",
            "concept_update",
            "concept_interest",
            "news_contribution",
            "node_creation",
            "edge_creation",
            "content_contribution",
            "feedback_contribution"
        };
        return types[index % types.Length];
    }

    /// <summary>
    /// Creates a test contribution with specific energy
    /// </summary>
    public static Contribution CreateTestContributionWithEnergy(double energy, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user-123",
            Type = type ?? "concept_creation",
            Description = $"Test contribution with {energy} energy",
            Energy = energy,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    /// <summary>
    /// Creates a test contribution for a specific user
    /// </summary>
    public static Contribution CreateTestContributionForUser(string userId, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Type = type ?? "concept_creation",
            Description = $"Test contribution for user {userId}",
            Energy = 25.0,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    /// <summary>
    /// Creates a test contribution with specific timestamp
    /// </summary>
    public static Contribution CreateTestContributionWithTimestamp(DateTime timestamp, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user-123",
            Type = type ?? "concept_creation",
            Description = $"Test contribution at {timestamp:yyyy-MM-dd HH:mm:ss}",
            Energy = 25.0,
            CreatedAt = timestamp
        };
    }

    /// <summary>
    /// Creates a test contribution with specific description
    /// </summary>
    public static Contribution CreateTestContributionWithDescription(string description, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user-123",
            Type = type ?? "concept_creation",
            Description = description,
            Energy = 25.0,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    /// <summary>
    /// Creates a test contribution with specific energy and timestamp
    /// </summary>
    public static Contribution CreateTestContributionWithEnergyAndTimestamp(double energy, DateTime timestamp, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "test-user-123",
            Type = type ?? "concept_creation",
            Description = $"Test contribution with {energy} energy at {timestamp:yyyy-MM-dd HH:mm:ss}",
            Energy = energy,
            CreatedAt = timestamp
        };
    }

    /// <summary>
    /// Creates a test contribution with all parameters
    /// </summary>
    public static Contribution CreateTestContributionWithAllParameters(
        string id,
        string userId,
        string type,
        string description,
        double energy,
        DateTime createdAt)
    {
        return new Contribution
        {
            Id = id,
            UserId = userId,
            Type = type,
            Description = description,
            Energy = energy,
            CreatedAt = createdAt
        };
    }
}
