using LivingCodexMobile.Models;

namespace LivingCodexMobile.Tests.TestData;

/// <summary>
/// Factory class for creating test data
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a test user
    /// </summary>
    public static User CreateTestUser(string? id = null, string? username = null, string? email = null)
    {
        return new User
        {
            Id = id ?? "test-user-123",
            Username = username ?? "testuser",
            Email = email ?? "test@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            LastActive = DateTime.UtcNow,
            Permissions = new List<string> { "read", "write", "contribute" }
        };
    }

    /// <summary>
    /// Creates a test concept
    /// </summary>
    public static Concept CreateTestConcept(string? id = null, string? name = null, string? description = null)
    {
        return new Concept
        {
            Id = id ?? "test-concept-123",
            Name = name ?? "Test Concept",
            Description = description ?? "A test concept for unit testing",
            Domain = "testing",
            Complexity = 5,
            Tags = new List<string> { "test", "example" },
            IsInterested = false,
            Resonance = 0.75,
            Energy = 100.0,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test news item
    /// </summary>
    public static NewsItem CreateTestNewsItem(string? id = null, string? title = null, string? content = null)
    {
        return new NewsItem
        {
            Id = id ?? "test-news-123",
            Title = title ?? "Test News Item",
            Content = content ?? "This is a test news item for unit testing",
            Source = "Test Source",
            PublishedAt = DateTime.UtcNow.AddHours(-1),
            Resonance = 0.80,
            Energy = 120.5,
            Tags = new List<string> { "test", "news" },
            IsRead = false
        };
    }

    /// <summary>
    /// Creates a test trending topic
    /// </summary>
    public static TrendingTopic CreateTestTrendingTopic(string? topic = null, int? count = null)
    {
        return new TrendingTopic
        {
            Id = Guid.NewGuid().ToString(),
            Topic = topic ?? "Test Topic",
            Name = topic ?? "Test Topic",
            Description = "Test trending topic description",
            MentionCount = count ?? 100,
            TrendScore = 0.85,
            RelatedTags = new List<string> { "test", "trending" },
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test contribution
    /// </summary>
    public static Contribution CreateTestContribution(string? userId = null, string? type = null)
    {
        return new Contribution
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId ?? "test-user-123",
            Type = type ?? "concept_creation",
            Description = "Test contribution",
            Energy = 25.0,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
    }

    /// <summary>
    /// Creates a test node
    /// </summary>
    public static Node CreateTestNode(string? id = null, string? title = null, string? typeId = null)
    {
        return new Node
        {
            Id = id ?? "test-node-123",
            Title = title ?? "Test Node",
            TypeId = typeId ?? "concept",
            State = ContentState.Water,
            Content = new ContentRef
            {
                InlineJson = "{\"content\": \"Test node content\"}",
                MediaType = "text/plain"
            },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test edge
    /// </summary>
    public static Edge CreateTestEdge(string? fromNodeId = null, string? toNodeId = null, string? role = null)
    {
        return new Edge
        {
            FromId = fromNodeId ?? "test-node-1",
            ToId = toNodeId ?? "test-node-2",
            Role = role ?? "related",
            Weight = 1.0,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    /// <summary>
    /// Creates a test API response
    /// </summary>
    public static ApiResponse<T> CreateTestApiResponse<T>(T data, bool success = true, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = success,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test API error response
    /// </summary>
    public static ApiErrorResponse CreateTestApiErrorResponse(string? message = null, int? statusCode = null)
    {
        return new ApiErrorResponse
        {
            Message = message ?? "Test error",
            ErrorCode = statusCode?.ToString() ?? "400",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a list of test concepts
    /// </summary>
    public static List<Concept> CreateTestConcepts(int count = 5)
    {
        var concepts = new List<Concept>();
        for (int i = 0; i < count; i++)
        {
            concepts.Add(CreateTestConcept(
                id: $"test-concept-{i + 1}",
                name: $"Test Concept {i + 1}",
                description: $"Description for test concept {i + 1}"
            ));
        }
        return concepts;
    }

    /// <summary>
    /// Creates a list of test news items
    /// </summary>
    public static List<NewsItem> CreateTestNewsItems(int count = 5)
    {
        var newsItems = new List<NewsItem>();
        for (int i = 0; i < count; i++)
        {
            newsItems.Add(CreateTestNewsItem(
                id: $"test-news-{i + 1}",
                title: $"Test News Item {i + 1}",
                content: $"Content for test news item {i + 1}"
            ));
        }
        return newsItems;
    }

    /// <summary>
    /// Creates a list of test trending topics
    /// </summary>
    public static List<TrendingTopic> CreateTestTrendingTopics(int count = 5)
    {
        var topics = new List<TrendingTopic>();
        for (int i = 0; i < count; i++)
        {
            topics.Add(CreateTestTrendingTopic(
                topic: $"Test Topic {i + 1}",
                count: 100 + (i * 10)
            ));
        }
        return topics;
    }
}
