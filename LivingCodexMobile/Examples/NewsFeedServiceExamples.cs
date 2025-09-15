using LivingCodexMobile.Models;
using LivingCodexMobile.Services;

namespace LivingCodexMobile.Examples;

/// <summary>
/// Examples of how to use the news feed service for news discovery and concept exploration
/// </summary>
public class NewsFeedServiceExamples
{
    private readonly INewsFeedService _newsFeedService;
    private readonly IConceptService _conceptService;

    public NewsFeedServiceExamples(INewsFeedService newsFeedService, IConceptService conceptService)
    {
        _newsFeedService = newsFeedService;
        _conceptService = conceptService;
    }

    // Example 1: Get personalized news feed
    public async Task<List<NewsItem>> GetPersonalizedNewsFeedAsync(string userId)
    {
        return await _newsFeedService.GetNewsFeedAsync(userId, 50, 24);
    }

    // Example 2: Search news by interests
    public async Task<List<NewsItem>> SearchNewsByInterestsAsync(List<string> interests)
    {
        var request = new NewsSearchRequest
        {
            Interests = interests,
            Limit = 30
        };
        return await _newsFeedService.SearchNewsAsync(request);
    }

    // Example 3: Get trending topics
    public async Task<List<TrendingTopic>> GetTrendingTopicsAsync()
    {
        return await _newsFeedService.GetTrendingTopicsAsync(20, 24);
    }

    // Example 4: Get news by specific concept
    public async Task<List<NewsItem>> GetNewsByConceptAsync(string conceptId)
    {
        return await _newsFeedService.GetNewsByConceptAsync(conceptId, 25);
    }

    // Example 5: Extract concepts from news
    public async Task<List<Concept>> ExtractConceptsFromNewsAsync(string newsId)
    {
        return await _newsFeedService.ExtractConceptsFromNewsAsync(newsId);
    }

    // Example 6: Get related news
    public async Task<List<NewsItem>> GetRelatedNewsAsync(string newsId)
    {
        return await _newsFeedService.GetRelatedNewsAsync(newsId, 15);
    }

    // Example 7: Mark news as read
    public async Task<bool> MarkNewsAsReadAsync(string userId, string newsId)
    {
        return await _newsFeedService.MarkNewsAsReadAsync(userId, newsId);
    }

    // Example 8: Get unread news
    public async Task<List<NewsItem>> GetUnreadNewsAsync(string userId)
    {
        return await _newsFeedService.GetUnreadNewsAsync(userId, 50);
    }

    // Example 9: News discovery workflow
    public async Task<NewsDiscoveryResult> DiscoverNewsWorkflowAsync(string userId, List<string> interests)
    {
        var result = new NewsDiscoveryResult { IsLoading = true };

        try
        {
            // Get personalized news feed
            var newsFeed = await _newsFeedService.GetNewsFeedAsync(userId, 30);
            result.NewsItems = newsFeed;

            // Get trending topics
            var trendingTopics = await _newsFeedService.GetTrendingTopicsAsync(10);
            result.TrendingTopics = trendingTopics;

            // Get news by interests
            var interestNews = await _newsFeedService.GetNewsByInterestsAsync(interests, 20);
            result.InterestBasedNews = interestNews;

            // Extract concepts from top news items
            var extractedConcepts = new List<Concept>();
            foreach (var newsItem in newsFeed.Take(5))
            {
                var concepts = await _newsFeedService.ExtractConceptsFromNewsAsync(newsItem.Id);
                extractedConcepts.AddRange(concepts);
            }
            result.ExtractedConcepts = extractedConcepts.DistinctBy(c => c.Id).ToList();

            result.IsLoading = false;
        }
        catch (Exception ex)
        {
            result.IsLoading = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // Example 10: Concept-based news exploration
    public async Task<ConceptNewsExploration> ExploreConceptNewsAsync(string conceptId, string userId)
    {
        var exploration = new ConceptNewsExploration
        {
            ConceptId = conceptId,
            UserId = userId,
            IsLoading = true
        };

        try
        {
            // Get news related to the concept
            var conceptNews = await _newsFeedService.GetNewsByConceptAsync(conceptId, 30);
            exploration.ConceptNews = conceptNews;

            // Extract related concepts from the news
            var relatedConcepts = new List<Concept>();
            foreach (var newsItem in conceptNews.Take(10))
            {
                var concepts = await _newsFeedService.ExtractConceptsFromNewsAsync(newsItem.Id);
                relatedConcepts.AddRange(concepts);
            }
            exploration.RelatedConcepts = relatedConcepts.DistinctBy(c => c.Id).ToList();

            // Get news for each related concept
            var relatedConceptNews = new List<NewsItem>();
            foreach (var concept in exploration.RelatedConcepts.Take(5))
            {
                var news = await _newsFeedService.GetNewsByConceptAsync(concept.Id, 10);
                relatedConceptNews.AddRange(news);
            }
            exploration.RelatedConceptNews = relatedConceptNews;

            exploration.IsLoading = false;
        }
        catch (Exception ex)
        {
            exploration.IsLoading = false;
            exploration.ErrorMessage = ex.Message;
        }

        return exploration;
    }

    // Example 11: News reading history management
    public async Task<NewsReadingHistory> ManageNewsReadingHistoryAsync(string userId)
    {
        var history = new NewsReadingHistory
        {
            UserId = userId,
            IsLoading = true
        };

        try
        {
            // Get read and unread news
            var readNews = await _newsFeedService.GetReadNewsAsync(userId, 100);
            var unreadNews = await _newsFeedService.GetUnreadNewsAsync(userId, 100);

            history.ReadNews = readNews;
            history.UnreadNews = unreadNews;
            history.TotalRead = readNews.Count;
            history.TotalUnread = unreadNews.Count;

            // Calculate reading patterns
            history.AverageReadPerDay = CalculateAverageReadPerDay(readNews);
            history.MostReadCategories = CalculateMostReadCategories(readNews);
            history.ReadingStreak = CalculateReadingStreak(readNews);

            history.IsLoading = false;
        }
        catch (Exception ex)
        {
            history.IsLoading = false;
            history.ErrorMessage = ex.Message;
        }

        return history;
    }

    // Example 12: News recommendation system
    public async Task<List<NewsItem>> GetRecommendedNewsAsync(string userId, List<string> interests)
    {
        try
        {
            // Get user's interested concepts
            var interestedConcepts = await _conceptService.GetConceptsByInterestAsync(userId);
            var conceptIds = interestedConcepts.Select(c => c.Id).ToList();

            // Get news for each concept
            var recommendedNews = new List<NewsItem>();
            foreach (var conceptId in conceptIds.Take(10))
            {
                var news = await _newsFeedService.GetNewsByConceptAsync(conceptId, 5);
                recommendedNews.AddRange(news);
            }

            // Get trending news
            var trendingTopics = await _newsFeedService.GetTrendingTopicsAsync(10);
            var trendingNews = new List<NewsItem>();
            foreach (var topic in trendingTopics.Take(5))
            {
                var news = await _newsFeedService.SearchNewsAsync(new NewsSearchRequest
                {
                    Interests = new List<string> { topic.Topic },
                    Limit = 3
                });
                trendingNews.AddRange(news);
            }

            // Combine and deduplicate
            var allNews = recommendedNews.Concat(trendingNews)
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .OrderByDescending(n => n.PublishedAt)
                .Take(50)
                .ToList();

            return allNews;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get recommended news error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    // Helper methods
    private double CalculateAverageReadPerDay(List<NewsItem> readNews)
    {
        if (!readNews.Any()) return 0;

        var days = readNews.Select(n => n.PublishedAt.Date).Distinct().Count();
        return days > 0 ? (double)readNews.Count / days : 0;
    }

    private List<string> CalculateMostReadCategories(List<NewsItem> readNews)
    {
        return readNews
            .SelectMany(n => n.Categories)
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();
    }

    private int CalculateReadingStreak(List<NewsItem> readNews)
    {
        if (!readNews.Any()) return 0;

        var sortedDates = readNews
            .Select(n => n.PublishedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        var currentDate = DateTime.Today;

        foreach (var date in sortedDates)
        {
            if (date == currentDate || date == currentDate.AddDays(-1))
            {
                streak++;
                currentDate = date.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}

// Helper classes
public class NewsDiscoveryResult
{
    public List<NewsItem> NewsItems { get; set; } = new();
    public List<TrendingTopic> TrendingTopics { get; set; } = new();
    public List<NewsItem> InterestBasedNews { get; set; } = new();
    public List<Concept> ExtractedConcepts { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConceptNewsExploration
{
    public string ConceptId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<NewsItem> ConceptNews { get; set; } = new();
    public List<Concept> RelatedConcepts { get; set; } = new();
    public List<NewsItem> RelatedConceptNews { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}

public class NewsReadingHistory
{
    public string UserId { get; set; } = string.Empty;
    public List<NewsItem> ReadNews { get; set; } = new();
    public List<NewsItem> UnreadNews { get; set; } = new();
    public int TotalRead { get; set; }
    public int TotalUnread { get; set; }
    public double AverageReadPerDay { get; set; }
    public List<string> MostReadCategories { get; set; } = new();
    public int ReadingStreak { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}
