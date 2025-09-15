using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public interface INewsFeedService
{
    Task<List<NewsItem>> GetNewsFeedAsync(string userId, int limit = 20, int hoursBack = 24);
    Task<List<NewsItem>> SearchNewsAsync(NewsSearchRequest request);
    Task<List<TrendingTopic>> GetTrendingTopicsAsync(int limit = 10, int hoursBack = 24);
    Task<List<NewsItem>> GetNewsByConceptAsync(string conceptId, int limit = 20);
    Task<List<NewsItem>> GetNewsByInterestsAsync(List<string> interests, int limit = 20);
    Task<NewsItem?> GetNewsItemAsync(string newsId);
    Task<List<Concept>> ExtractConceptsFromNewsAsync(string newsId);
    Task<List<NewsItem>> GetRelatedNewsAsync(string newsId, int limit = 10);
    Task<bool> MarkNewsAsReadAsync(string userId, string newsId);
    Task<List<NewsItem>> GetReadNewsAsync(string userId, int limit = 20);
    Task<List<NewsItem>> GetUnreadNewsAsync(string userId, int limit = 20);
}

