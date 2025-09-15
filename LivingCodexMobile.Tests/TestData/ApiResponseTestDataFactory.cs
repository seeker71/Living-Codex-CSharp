using LivingCodexMobile.Models;

namespace LivingCodexMobile.Tests.TestData;

/// <summary>
/// Factory class for creating test API response data
/// </summary>
public static class ApiResponseTestDataFactory
{
    /// <summary>
    /// Creates a successful API response
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponse<T>(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed API response
    /// </summary>
    public static ApiResponse<T> CreateErrorResponse<T>(string message, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default(T),
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful API response with default data
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponseWithDefault<T>(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = default(T),
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful API response with empty data
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponseWithEmpty<T>(string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = default(T),
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful API response with specific timestamp
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponseWithTimestamp<T>(T data, DateTime timestamp, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Creates a failed API response with specific timestamp
    /// </summary>
    public static ApiResponse<T> CreateErrorResponseWithTimestamp<T>(string message, DateTime timestamp, int? statusCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default(T),
            Message = message,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Creates a successful API response with status code
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponseWithStatusCode<T>(T data, int statusCode, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed API response with status code
    /// </summary>
    public static ApiResponse<T> CreateErrorResponseWithStatusCode<T>(string message, int statusCode)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default(T),
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful API response with all parameters
    /// </summary>
    public static ApiResponse<T> CreateSuccessResponseWithAllParameters<T>(
        T data,
        string? message,
        int? statusCode,
        DateTime timestamp)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Creates a failed API response with all parameters
    /// </summary>
    public static ApiResponse<T> CreateErrorResponseWithAllParameters<T>(
        string message,
        int? statusCode,
        DateTime timestamp)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default(T),
            Message = message,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Creates a successful API response for user data
    /// </summary>
    public static ApiResponse<User> CreateUserSuccessResponse(string? userId = null, string? message = null)
    {
        var user = TestDataFactory.CreateTestUser(userId);
        return CreateSuccessResponse(user, message);
    }

    /// <summary>
    /// Creates a successful API response for concept data
    /// </summary>
    public static ApiResponse<Concept> CreateConceptSuccessResponse(string? conceptId = null, string? message = null)
    {
        var concept = TestDataFactory.CreateTestConcept(conceptId);
        return CreateSuccessResponse(concept, message);
    }

    /// <summary>
    /// Creates a successful API response for news item data
    /// </summary>
    public static ApiResponse<NewsItem> CreateNewsItemSuccessResponse(string? newsId = null, string? message = null)
    {
        var newsItem = TestDataFactory.CreateTestNewsItem(newsId);
        return CreateSuccessResponse(newsItem, message);
    }

    /// <summary>
    /// Creates a successful API response for list of concepts
    /// </summary>
    public static ApiResponse<List<Concept>> CreateConceptListSuccessResponse(int count = 5, string? message = null)
    {
        var concepts = TestDataFactory.CreateTestConcepts(count);
        return CreateSuccessResponse(concepts, message);
    }

    /// <summary>
    /// Creates a successful API response for list of news items
    /// </summary>
    public static ApiResponse<List<NewsItem>> CreateNewsItemListSuccessResponse(int count = 5, string? message = null)
    {
        var newsItems = TestDataFactory.CreateTestNewsItems(count);
        return CreateSuccessResponse(newsItems, message);
    }

    /// <summary>
    /// Creates a successful API response for list of trending topics
    /// </summary>
    public static ApiResponse<List<TrendingTopic>> CreateTrendingTopicListSuccessResponse(int count = 5, string? message = null)
    {
        var topics = TestDataFactory.CreateTestTrendingTopics(count);
        return CreateSuccessResponse(topics, message);
    }

    /// <summary>
    /// Creates a successful API response for list of contributions
    /// </summary>
    public static ApiResponse<List<Contribution>> CreateContributionListSuccessResponse(int count = 5, string? message = null)
    {
        var contributions = ContributionTestDataFactory.CreateTestContributions(count);
        return CreateSuccessResponse(contributions, message);
    }

    /// <summary>
    /// Creates a successful API response for energy data
    /// </summary>
    public static ApiResponse<double> CreateEnergySuccessResponse(double energy, string? message = null)
    {
        return CreateSuccessResponse(energy, message);
    }

    /// <summary>
    /// Creates a successful API response for boolean data
    /// </summary>
    public static ApiResponse<bool> CreateBooleanSuccessResponse(bool value, string? message = null)
    {
        return CreateSuccessResponse(value, message);
    }

    /// <summary>
    /// Creates a successful API response for string data
    /// </summary>
    public static ApiResponse<string> CreateStringSuccessResponse(string value, string? message = null)
    {
        return CreateSuccessResponse(value, message);
    }

    /// <summary>
    /// Creates a successful API response for integer data
    /// </summary>
    public static ApiResponse<int> CreateIntegerSuccessResponse(int value, string? message = null)
    {
        return CreateSuccessResponse(value, message);
    }

    /// <summary>
    /// Creates a successful API response for double data
    /// </summary>
    public static ApiResponse<double> CreateDoubleSuccessResponse(double value, string? message = null)
    {
        return CreateSuccessResponse(value, message);
    }
}
