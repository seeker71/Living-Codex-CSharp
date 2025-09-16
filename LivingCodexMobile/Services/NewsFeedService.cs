using System.Text.Json;
using LivingCodexMobile.Models;

namespace LivingCodexMobile.Services;

public class NewsFeedService : INewsFeedService
{
    private readonly IApiService _apiService;
    private readonly IConceptService _conceptService;

    public NewsFeedService(IApiService apiService, IConceptService conceptService)
    {
        _apiService = apiService;
        _conceptService = conceptService;
    }

    public async Task<List<NewsItem>> GetNewsFeedAsync(string userId, int limit = 20, int hoursBack = 24)
    {
        try
        {
            var response = await _apiService.GetAsync<NewsFeedResponse>($"/news/feed/{Uri.EscapeDataString(userId)}?limit={limit}&hoursBack={hoursBack}");
            return response?.Items ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news feed error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<List<NewsItem>> SearchNewsAsync(NewsSearchRequest request)
    {
        try
        {
            var response = await _apiService.PostAsync<NewsSearchRequest, NewsFeedResponse>("/news/search", request);
            return response?.Items ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search news error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<List<TrendingTopic>> GetTrendingTopicsAsync(int limit = 10, int hoursBack = 24)
    {
        try
        {
            var response = await _apiService.GetAsync<TrendingTopicsResponse>($"/news/trending?limit={limit}&hoursBack={hoursBack}");
            return response?.Topics ?? new List<TrendingTopic>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get trending topics error: {ex.Message}");
            return new List<TrendingTopic>();
        }
    }

    public async Task<List<NewsItem>> GetNewsByConceptAsync(string conceptId, int limit = 20)
    {
        try
        {
            var request = new NewsSearchRequest
            {
                Interests = new List<string> { conceptId },
                Limit = limit
            };
            return await SearchNewsAsync(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news by concept error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<List<NewsItem>> GetNewsByInterestsAsync(List<string> interests, int limit = 20)
    {
        try
        {
            var request = new NewsSearchRequest
            {
                Interests = interests,
                Limit = limit
            };
            return await SearchNewsAsync(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news by interests error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<NewsItem?> GetNewsItemAsync(string newsId)
    {
        try
        {
            // Prefer dedicated news item endpoint if available
            var direct = await _apiService.GetAsync<NewsItemResponse>($"/news/item/{newsId}");
            if (direct?.Item != null) return direct.Item;
            // Fallback to node endpoint
            var response = await _apiService.GetAsync<NodeResponse>($"/storage-endpoints/nodes/{newsId}");
            return response?.Node != null ? MapNodeToNewsItem(response.Node) : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get news item error: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Concept>> ExtractConceptsFromNewsAsync(string newsId)
    {
        try
        {
            var newsItem = await GetNewsItemAsync(newsId);
            if (newsItem == null) return new List<Concept>();

            var request = new ConceptDiscoveryRequest(
                newsItem.Content ?? newsItem.Description,
                "text/plain",
                null,
                10
            );
            return await _conceptService.DiscoverConceptsAsync(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extract concepts from news error: {ex.Message}");
            return new List<Concept>();
        }
    }

    public async Task<List<NewsItem>> GetRelatedNewsAsync(string newsId, int limit = 10)
    {
        try
        {
            // Prefer dedicated related news endpoint if available
            var direct = await _apiService.GetAsync<NewsFeedResponse>($"/news/related/{newsId}?limit={limit}");
            if (direct?.Items != null && direct.Items.Count > 0) return direct.Items;
            // Fallback to graph relationships endpoint
            var response = await _apiService.GetAsync<GraphRelationshipsResponse>($"/graph/relationships/{newsId}");
            return response?.RelatedNodes?.Where(n => n.TypeId == "codex.news.item").Take(limit).Select(n => MapNodeToNewsItem(n)).ToList() ?? new List<NewsItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get related news error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<bool> MarkNewsAsReadAsync(string userId, string newsId)
    {
        try
        {
            // Prefer dedicated read tracking endpoint if available
            var readRequest = new NewsReadRequest { UserId = userId, NewsId = newsId };
            var direct = await _apiService.PostAsync<NewsReadRequest, ApiResponse>("/news/read", readRequest);
            if (direct?.Success == true) return true;
            // Fallback to contribution recording
            var request = new ContributionRequest
            {
                UserId = userId,
                Title = $"Read news: {newsId}",
                Description = "User read a news item",
                Type = "news-read",
                Metadata = new Dictionary<string, object> { ["entityId"] = newsId, ["energy"] = 10.0 }
            };
            await _apiService.PostAsync<ContributionRequest, ContributionResponse>("/contributions/record", request);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Mark news as read error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<NewsItem>> GetReadNewsAsync(string userId, int limit = 20)
    {
        try
        {
            // Map to existing contributions endpoint for read news tracking
            // Prefer dedicated read list endpoint if available
            var direct = await _apiService.GetAsync<NewsFeedResponse>($"/news/read/{userId}?limit={limit}");
            if (direct?.Items != null && direct.Items.Count > 0) return direct.Items;
            // Fallback to contributions endpoint for read news tracking
            var response = await _apiService.GetAsync<ContributionsResponse>($"/contributions/user/{userId}?type=news-read&limit={limit}");
            var newsIds = response?.Contributions?
                .Select(c => c.Metadata != null && c.Metadata.TryGetValue("entityId", out var v) ? v?.ToString() : null)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .ToList() ?? new List<string>();
            var newsItems = new List<NewsItem>();
            
            foreach (var newsId in newsIds)
            {
                var newsItem = await GetNewsItemAsync(newsId);
                if (newsItem != null)
                    newsItems.Add(newsItem);
            }
            
            return newsItems;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get read news error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    public async Task<List<NewsItem>> GetUnreadNewsAsync(string userId, int limit = 20)
    {
        try
        {
            // Get all news feed and filter out read items
            var allNews = await GetNewsFeedAsync(userId, limit * 2); // Get more to account for filtering
            var readNews = await GetReadNewsAsync(userId, limit * 2);
            var readIds = readNews.Select(n => n.Id).ToHashSet();
            
            return allNews.Where(n => !readIds.Contains(n.Id)).Take(limit).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get unread news error: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    private NewsItem MapNodeToNewsItem(Node node)
    {
        return new NewsItem
        {
            Id = node.Id,
            Title = node.Title ?? "Unknown",
            Description = node.Description ?? "",
            Content = node.Content?.InlineJson ?? "",
            PublishedAt = node.Meta?.GetValueOrDefault("publishedAt") is DateTime published ? published : DateTime.UtcNow,
            Source = node.Meta?.GetValueOrDefault("source")?.ToString() ?? "Unknown",
            Url = node.Meta?.GetValueOrDefault("url")?.ToString() ?? "",
            ImageUrl = node.Meta?.GetValueOrDefault("imageUrl")?.ToString() ?? "",
            Tags = node.Meta?.GetValueOrDefault("tags")?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList() ?? new List<string>(),
            IsRead = false, // Default to unread
            RelevanceScore = 0.5 // Default relevance
        };
    }
}


