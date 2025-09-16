using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// News Feed Module - Real news feed based on user interests from actual news data
/// Provides personalized news feeds based on user interests, location, and concept relationships
/// </summary>
[MetaNode(Id = "codex.news-feed", Name = "News Feed Module", Description = "Real news feed system based on user interests and actual news data")]
public sealed class NewsFeedModule : ModuleBase
{
    private readonly HttpClient _httpClient;

    public override string Name => "News Feed Module";
    public override string Description => "Real news feed system based on user interests and actual news data";
    public override string Version => "1.0.0";

    public NewsFeedModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the registry to use - now always the unified registry
    /// </summary>
    private INodeRegistry Registry => _registry;

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.news-feed",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "news", "feed", "personalization", "content", "interests", "real-time" },
            capabilities: new[] { 
                "news-feed", "personalization", "interest-matching", "news-aggregation", 
                "content-filtering", "real-time-updates", "user-preferences" 
            },
            spec: "codex.spec.news-feed"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("News Feed Module API handlers registered");
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attribute-based routing
        _logger.Info("News Feed Module HTTP endpoints registered");
    }

    // Get personalized news feed for user
    [ApiRoute("GET", "/news/feed/{userId}", "Get User News Feed", "Get personalized news feed for a user based on their interests", "codex.news-feed")]
    public async Task<object> GetUserNewsFeed(
        [ApiParameter("userId", "User ID to get news feed for", Required = true, Location = "path")] string userId,
        [ApiParameter("limit", "Number of news items to return", Required = false, Location = "query")] int limit = 20,
        [ApiParameter("hoursBack", "Number of hours to look back for news", Required = false, Location = "query")] int hoursBack = 24)
    {
        try
        {
            // Validate userId parameter
            if (string.IsNullOrEmpty(userId))
            {
                return new ErrorResponse("User ID is required");
            }

            // Get user profile and interests
            List<string> userInterests = new();
            string? userLocation = null;
            List<string> userContributions = new();

            if (Registry.TryGet(userId, out var userNode))
            {
                userInterests = ParseStringList(userNode.Meta?.GetValueOrDefault("interests")?.ToString());
                userLocation = userNode.Meta?.GetValueOrDefault("location")?.ToString();
                userContributions = ParseStringList(userNode.Meta?.GetValueOrDefault("contributions")?.ToString());
            }
            else
            {
                // If user doesn't exist, use default interests
                userInterests = new List<string> { "technology", "science", "business", "politics", "health" };
            }

            // Get news items based on user interests
            var newsItems = await GetNewsFeedItemsForInterests(userInterests, userLocation, userContributions, limit, hoursBack);

            return new NewsFeedResponse(newsItems, newsItems.Count, "Personalized news feed");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news feed for user {userId}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting news feed: {ex.Message}");
        }
    }

    // Get news items for specific interests
    [ApiRoute("POST", "/news/search", "Search News", "Search news items by interests, location, or concepts", "codex.news-feed")]
    public async Task<object> SearchNews([ApiParameter("body", "News search request", Required = true, Location = "body")] NewsSearchRequest request)
    {
        try
        {
            var newsItems = await GetNewsFeedItemsForInterests(
                request.Interests ?? new List<string>(),
                request.Location,
                request.Contributions ?? new List<string>(),
                request.Limit ?? 20,
                request.HoursBack ?? 24
            );

            return new NewsFeedResponse(newsItems, newsItems.Count, "News search results");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error searching news: {ex.Message}", ex);
            return new ErrorResponse($"Error searching news: {ex.Message}");
        }
    }

    // Get trending topics based on recent news
    [ApiRoute("GET", "/news/trending", "Get Trending Topics", "Get trending topics from recent news", "codex.news-feed")]
    public async Task<object> GetTrendingTopics(
        [ApiParameter("query", "Number of trending topics to return", Required = false, Location = "query")] int limit = 10,
        [ApiParameter("query", "Number of hours to analyze for trends", Required = false, Location = "query")] int hoursBack = 24)
    {
        try
        {
            var trendingTopics = await GetTrendingTopicsFromNews(limit, hoursBack);
            return new TrendingTopicsResponse(trendingTopics, trendingTopics.Count);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting trending topics: {ex.Message}", ex);
            return new ErrorResponse($"Error getting trending topics: {ex.Message}");
        }
    }

    // Get specific news item by ID
    [ApiRoute("GET", "/news/item/{id}", "Get News Item", "Get specific news item by ID", "codex.news-feed")]
    public async Task<object> GetNewsItem([ApiParameter("id", "News item ID", Required = true, Location = "path")] string id)
    {
        try
        {
            var node = Registry.GetNode(id);
            if (node?.TypeId != "codex.news.item")
            {
                return new ErrorResponse("News item not found");
            }

            var newsItem = MapNodeToNewsFeedItem(node);
            if (newsItem == null)
            {
                return new ErrorResponse("Failed to parse news item");
            }

            return new NewsItemResponse(newsItem);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news item {id}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting news item: {ex.Message}");
        }
    }

    // Get related news items
    [ApiRoute("GET", "/news/related/{id}", "Get Related News", "Get related news items", "codex.news-feed")]
    public async Task<object> GetRelatedNews(
        [ApiParameter("id", "News item ID", Required = true, Location = "path")] string id,
        [ApiParameter("query", "Number of related items to return", Required = false, Location = "query")] int limit = 10)
    {
        try
        {
            var sourceNode = Registry.GetNode(id);
            if (sourceNode?.TypeId != "codex.news.item")
            {
                return new ErrorResponse("Source news item not found");
            }

            // Get other news items and calculate similarity
            var allNewsNodes = Registry.GetNodesByType("codex.news.item")
                .Where(n => n.Id != id)
                .OrderByDescending(n => CalculateNewsSimilarity(sourceNode, n))
                .Take(limit)
                .ToList();

            var relatedItems = allNewsNodes
                .Select(MapNodeToNewsFeedItem)
                .Where(item => item != null)
                .Cast<NewsFeedItem>()
                .ToList();

            return new NewsFeedResponse(relatedItems, relatedItems.Count, "Related news items");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting related news for {id}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting related news: {ex.Message}");
        }
    }

    // Mark news as read
    [ApiRoute("POST", "/news/read", "Mark News as Read", "Mark news item as read by user", "codex.news-feed")]
    public async Task<object> MarkNewsAsRead([ApiParameter("request", "Read request", Required = true, Location = "body")] NewsReadRequest request)
    {
        try
        {
            // Create a read tracking node
            var readNode = new Node(
                Id: $"read-{request.UserId}-{request.NewsId}",
                TypeId: "codex.news.read",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Read: {request.NewsId}",
                Description: null,
                Content: null,
                Meta: new Dictionary<string, object>
                {
                    ["userId"] = request.UserId,
                    ["newsId"] = request.NewsId,
                    ["readAt"] = DateTime.UtcNow
                }
            );

            Registry.Upsert(readNode);
            return new { success = true, message = "News marked as read" };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error marking news as read: {ex.Message}", ex);
            return new ErrorResponse($"Error marking news as read: {ex.Message}");
        }
    }

    // Get read news for user
    [ApiRoute("GET", "/news/read/{userId}", "Get Read News", "Get read news items for user", "codex.news-feed")]
    public async Task<object> GetReadNews(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("query", "Number of items to return", Required = false, Location = "query")] int limit = 20)
    {
        try
        {
            var readNodes = Registry.GetNodesByType("codex.news.read")
                .Where(n => n.Meta?.GetValueOrDefault("userId")?.ToString() == userId)
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("readAt"))
                .Take(limit)
                .ToList();

            var readNewsItems = new List<NewsFeedItem>();
            foreach (var readNode in readNodes)
            {
                var newsId = readNode.Meta?.GetValueOrDefault("newsId")?.ToString();
                if (!string.IsNullOrEmpty(newsId))
                {
                    var newsNode = Registry.GetNode(newsId);
                    if (newsNode?.TypeId == "codex.news.item")
                    {
                        var newsItem = MapNodeToNewsFeedItem(newsNode);
                        if (newsItem != null)
                        {
                            readNewsItems.Add(newsItem);
                        }
                    }
                }
            }

            return new NewsFeedResponse(readNewsItems, readNewsItems.Count, "Read news items");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting read news for user {userId}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting read news: {ex.Message}");
        }
    }

    // Get unread news for user
    [ApiRoute("GET", "/news/unread/{userId}", "Get Unread News", "Get unread news items for user", "codex.news-feed")]
    public async Task<object> GetUnreadNews(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("query", "Number of items to return", Required = false, Location = "query")] int limit = 20)
    {
        try
        {
            // Get all news items
            var allNewsNodes = Registry.GetNodesByType("codex.news.item")
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("publishedAt"))
                .Take(limit * 2)
                .ToList();

            // Get read news IDs
            var readNewsIds = Registry.GetNodesByType("codex.news.read")
                .Where(n => n.Meta?.GetValueOrDefault("userId")?.ToString() == userId)
                .Select(n => n.Meta?.GetValueOrDefault("newsId")?.ToString())
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            // Filter out read news
            var unreadNewsItems = allNewsNodes
                .Where(n => !readNewsIds.Contains(n.Id))
                .Take(limit)
                .Select(MapNodeToNewsFeedItem)
                .Where(item => item != null)
                .Cast<NewsFeedItem>()
                .ToList();

            return new NewsFeedResponse(unreadNewsItems, unreadNewsItems.Count, "Unread news items");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting unread news for user {userId}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting unread news: {ex.Message}");
        }
    }

    // Get news items for specific interests
    private async Task<List<NewsFeedItem>> GetNewsFeedItemsForInterests(
        List<string> interests, 
        string? location, 
        List<string> contributions, 
        int limit, 
        int hoursBack)
    {
        var newsItems = new List<NewsFeedItem>();
        
        try
        {
            // Get news from internal news nodes instead of external APIs
            var cutoffDate = DateTime.UtcNow.AddHours(-hoursBack);
            
            // First, let's get all news nodes to see what we have
            var allNewsNodes = Registry.GetNodesByType("codex.news.item").ToList();
            _logger.Info($"Found {allNewsNodes.Count} news nodes in registry");
            
            // Debug: Let's check if we can find any nodes with common types
            var conceptNodes = Registry.GetNodesByType("codex.concept").ToList();
            var moduleNodes = Registry.GetNodesByType("codex.module").ToList();
            _logger.Info($"Found {conceptNodes.Count} concept nodes and {moduleNodes.Count} module nodes in registry");
            
            var newsNodes = allNewsNodes
                .Where(n => 
                {
                    try
                    {
                        var publishedAt = n.Meta?.GetValueOrDefault("publishedAt");
                        if (publishedAt is DateTime publishedDate)
                        {
                            return publishedDate >= cutoffDate;
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error checking publishedAt for node {n.Id}: {ex.Message}");
                        return false;
                    }
                })
                .OrderByDescending(n => 
                {
                    try
                    {
                        return n.Meta?.GetValueOrDefault("publishedAt") as DateTime? ?? DateTime.MinValue;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error ordering node {n.Id}: {ex.Message}");
                        return DateTime.MinValue;
                    }
                })
                .Take(limit * 2) // Get more to account for filtering
                .ToList();

            _logger.Info($"Filtered to {newsNodes.Count} recent news nodes");

            // Convert nodes to NewsFeedItem objects
            foreach (var node in newsNodes)
            {
                try
                {
                    var newsItem = MapNodeToNewsFeedItem(node);
                    if (newsItem != null)
                    {
                        newsItems.Add(newsItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error mapping node {node.Id} to NewsFeedItem: {ex.Message}");
                }
            }

            // Filter by interests if provided
            if (interests.Any())
            {
                newsItems = newsItems
                    .Where(item => interests.Any(interest => 
                        item.Title.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                        item.Description.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                        (item.Content?.Contains(interest, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .ToList();
            }

            // Sort by relevance score and take top items
            newsItems = newsItems
                .OrderByDescending(item => CalculateRelevanceScore(item, interests, contributions))
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news items for interests: {ex.Message}", ex);
        }

        return newsItems;
    }

    // Get news from a specific source
    private async Task<List<NewsFeedItem>> GetNewsFromSource(string source, List<string> interests, string? location, int limit, int hoursBack)
    {
        var newsItems = new List<NewsFeedItem>();
        
        try
        {
            // Build query based on interests
            var query = string.Join(" OR ", interests.Select(i => $"\"{i}\""));
            var fromDate = DateTime.UtcNow.AddHours(-hoursBack).ToString("yyyy-MM-dd");
            
            var url = source switch
            {
                var s when s.Contains("newsapi.org") => BuildNewsApiUrl(s, query, fromDate, limit),
                var s when s.Contains("nytimes.com") => BuildNYTimesUrl(s, query, fromDate, limit),
                var s when s.Contains("guardianapis.com") => BuildGuardianUrl(s, query, fromDate, limit),
                _ => null
            };

            if (url == null) return newsItems;

            var response = await _httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            
            newsItems = ParseNewsFromJson(json, source);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news from source {source}: {ex.Message}", ex);
        }

        return newsItems;
    }

    // Build NewsAPI URL
    private string BuildNewsApiUrl(string baseUrl, string query, string fromDate, int limit)
    {
        var apiKey = Environment.GetEnvironmentVariable("NEWS_API_KEY");
        if (string.IsNullOrEmpty(apiKey)) return string.Empty;
        
        return $"{baseUrl}?q={Uri.EscapeDataString(query)}&from={fromDate}&sortBy=relevancy&pageSize={limit}&apiKey={apiKey}";
    }

    // Build NY Times URL
    private string BuildNYTimesUrl(string baseUrl, string query, string fromDate, int limit)
    {
        var apiKey = Environment.GetEnvironmentVariable("NYTIMES_API_KEY");
        if (string.IsNullOrEmpty(apiKey)) return string.Empty;
        
        return $"{baseUrl}?q={Uri.EscapeDataString(query)}&begin_date={fromDate.Replace("-", "")}&sort=relevance&page=0&api-key={apiKey}";
    }

    // Build Guardian URL
    private string BuildGuardianUrl(string baseUrl, string query, string fromDate, int limit)
    {
        var apiKey = Environment.GetEnvironmentVariable("GUARDIAN_API_KEY");
        if (string.IsNullOrEmpty(apiKey)) return string.Empty;
        
        return $"{baseUrl}?q={Uri.EscapeDataString(query)}&from-date={fromDate}&page-size={limit}&api-key={apiKey}";
    }

    // Parse news items from JSON response
    private List<NewsFeedItem> ParseNewsFromJson(JsonDocument json, string source)
    {
        var newsItems = new List<NewsFeedItem>();
        
        try
        {
            if (source.Contains("newsapi.org"))
            {
                if (json.RootElement.TryGetProperty("articles", out var articles))
                {
                    foreach (var article in articles.EnumerateArray())
                    {
                        newsItems.Add(new NewsFeedItem(
                            Title: article.GetProperty("title").GetString() ?? "",
                            Description: article.GetProperty("description").GetString() ?? "",
                            Url: article.GetProperty("url").GetString() ?? "",
                            PublishedAt: article.TryGetProperty("publishedAt", out var publishedAt) ? 
                                DateTime.Parse(publishedAt.GetString() ?? "") : DateTime.UtcNow,
                            Source: article.GetProperty("source").GetProperty("name").GetString() ?? "Unknown",
                            Author: article.TryGetProperty("author", out var author) ? author.GetString() : null,
                            ImageUrl: article.TryGetProperty("urlToImage", out var imageUrl) ? imageUrl.GetString() : null,
                            Content: article.TryGetProperty("content", out var content) ? content.GetString() : null
                        ));
                    }
                }
            }
            else if (source.Contains("nytimes.com"))
            {
                if (json.RootElement.TryGetProperty("response", out var response) &&
                    response.TryGetProperty("docs", out var docs))
                {
                    foreach (var doc in docs.EnumerateArray())
                    {
                        newsItems.Add(new NewsFeedItem(
                            Title: doc.GetProperty("headline").GetProperty("main").GetString() ?? "",
                            Description: doc.TryGetProperty("abstract", out var abstractText) ? abstractText.GetString() : "",
                            Url: doc.GetProperty("web_url").GetString() ?? "",
                            PublishedAt: DateTime.Parse(doc.GetProperty("pub_date").GetString() ?? ""),
                            Source: "New York Times",
                            Author: doc.TryGetProperty("byline", out var byline) ? byline.GetProperty("original").GetString() : null,
                            ImageUrl: null,
                            Content: doc.TryGetProperty("lead_paragraph", out var lead) ? lead.GetString() : null
                        ));
                    }
                }
            }
            else if (source.Contains("guardianapis.com"))
            {
                if (json.RootElement.TryGetProperty("response", out var response) &&
                    response.TryGetProperty("results", out var results))
                {
                    foreach (var result in results.EnumerateArray())
                    {
                        newsItems.Add(new NewsFeedItem(
                            Title: result.GetProperty("webTitle").GetString() ?? "",
                            Description: result.TryGetProperty("fields", out var fields) && 
                                       fields.TryGetProperty("trailText", out var trailText) ? trailText.GetString() : "",
                            Url: result.GetProperty("webUrl").GetString() ?? "",
                            PublishedAt: DateTime.Parse(result.GetProperty("webPublicationDate").GetString() ?? ""),
                            Source: "The Guardian",
                            Author: result.TryGetProperty("fields", out var authorFields) && 
                                   authorFields.TryGetProperty("byline", out var byline) ? byline.GetString() : null,
                            ImageUrl: result.TryGetProperty("fields", out var imageFields) && 
                                     imageFields.TryGetProperty("thumbnail", out var thumbnail) ? thumbnail.GetString() : null,
                            Content: result.TryGetProperty("fields", out var contentFields) && 
                                    contentFields.TryGetProperty("body", out var body) ? body.GetString() : null
                        ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error parsing news from {source}: {ex.Message}", ex);
        }

        return newsItems;
    }

    // Calculate relevance score for a news item
    private double CalculateRelevanceScore(NewsFeedItem item, List<string> interests, List<string> contributions)
    {
        double score = 0.0;
        var text = $"{item.Title} {item.Description} {item.Content}".ToLowerInvariant();
        
        // Score based on interest matches
        foreach (var interest in interests)
        {
            if (text.Contains(interest.ToLowerInvariant()))
            {
                score += 1.0;
            }
        }
        
        // Score based on contribution matches
        foreach (var contribution in contributions)
        {
            if (text.Contains(contribution.ToLowerInvariant()))
            {
                score += 0.5;
            }
        }
        
        // Boost score for recent items
        var hoursOld = (DateTime.UtcNow - item.PublishedAt).TotalHours;
        if (hoursOld < 1) score += 2.0;
        else if (hoursOld < 6) score += 1.0;
        else if (hoursOld < 24) score += 0.5;
        
        return score;
    }

    // Get trending topics from recent news
    private async Task<List<TrendingTopic>> GetTrendingTopicsFromNews(int limit, int hoursBack)
    {
        var trendingTopics = new List<TrendingTopic>();
        
        try
        {
            // Get recent news items from internal nodes
            var cutoffDate = DateTime.UtcNow.AddHours(-hoursBack);
            var newsNodes = Registry.GetNodesByType("codex.news.item")
                .Where(n => n.Meta?.GetValueOrDefault("publishedAt") is DateTime publishedAt && publishedAt >= cutoffDate)
                .OrderByDescending(n => n.Meta?.GetValueOrDefault("publishedAt"))
                .Take(100) // Get more items for trend analysis
                .ToList();

            // Extract keywords and count frequency
            var keywordCounts = new Dictionary<string, int>();
            
            foreach (var node in newsNodes)
            {
                var title = node.Title ?? "";
                var description = node.Description ?? "";
                var content = node.Meta?.GetValueOrDefault("content")?.ToString() ?? "";
                var text = $"{title} {description} {content}";
                
                var keywords = ExtractKeywords(text);
                foreach (var keyword in keywords)
                {
                    if (keywordCounts.ContainsKey(keyword))
                        keywordCounts[keyword]++;
                    else
                        keywordCounts[keyword] = 1;
                }
            }

            // Get top trending topics
            trendingTopics = keywordCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(limit)
                .Select(kvp => new TrendingTopic(kvp.Key, kvp.Value, CalculateTrendScore(kvp.Value, newsNodes.Count)))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting trending topics: {ex.Message}", ex);
        }

        return trendingTopics;
    }

    // Extract keywords from text
    private List<string> ExtractKeywords(string text)
    {
        // Simple keyword extraction - in production, use more sophisticated NLP
        var words = text.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !IsStopWord(w))
            .GroupBy(w => w)
            .Where(g => g.Count() > 1) // Only words that appear multiple times
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    // Check if word is a stop word
    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string>
        {
            "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "who", "boy", "did", "she", "use", "way", "will", "with", "this", "that", "they", "have", "from", "been", "were", "said", "each", "which", "their", "time", "will", "about", "there", "could", "other", "after", "first", "well", "also", "where", "much", "some", "very", "when", "come", "here", "just", "like", "long", "make", "many", "over", "such", "take", "than", "them", "these", "think", "want", "what", "year", "your", "work", "know", "look", "good", "great", "little", "right", "small", "still", "those", "under", "while", "world", "years", "being", "every", "going", "might", "never", "place", "same", "seems", "told", "tried", "trying", "water", "young"
        };
        
        return stopWords.Contains(word);
    }

    // Calculate trend score
    private double CalculateTrendScore(int count, int totalItems)
    {
        return (double)count / totalItems;
    }

    // Helper methods
    private List<string> ParseStringList(string? value)
    {
        if (string.IsNullOrEmpty(value)) return new List<string>();
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    // Map Node to NewsFeedItem
    private NewsFeedItem? MapNodeToNewsFeedItem(Node node)
    {
        try
        {
            return new NewsFeedItem(
                Title: node.Title ?? "",
                Description: node.Description ?? "",
                Url: node.Meta?.GetValueOrDefault("url")?.ToString() ?? "",
                PublishedAt: node.Meta?.GetValueOrDefault("publishedAt") is DateTime publishedAt ? publishedAt : DateTime.UtcNow,
                Source: node.Meta?.GetValueOrDefault("source")?.ToString() ?? "Unknown",
                Author: node.Meta?.GetValueOrDefault("author")?.ToString(),
                ImageUrl: node.Meta?.GetValueOrDefault("imageUrl")?.ToString(),
                Content: node.Meta?.GetValueOrDefault("content")?.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"Error mapping node to NewsFeedItem: {ex.Message}", ex);
            return null;
        }
    }

    // Calculate similarity between two news items
    private double CalculateNewsSimilarity(Node news1, Node news2)
    {
        var text1 = $"{news1.Title} {news1.Description} {news1.Meta?.GetValueOrDefault("content")}".ToLowerInvariant();
        var text2 = $"{news2.Title} {news2.Description} {news2.Meta?.GetValueOrDefault("content")}".ToLowerInvariant();
        
        var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union > 0 ? (double)intersection / union : 0.0;
    }
}

// Response types for news feed
[MetaNode(Id = "codex.news-feed.search-request", Name = "News Search Request", Description = "Request for searching news items")]
public record NewsSearchRequest(
    List<string>? Interests = null,
    string? Location = null,
    List<string>? Contributions = null,
    int? Limit = 20,
    int? HoursBack = 24
);

[MetaNode(Id = "codex.news-feed.news-item", Name = "News Item", Description = "A news item from external sources")]
public record NewsFeedItem(
    string Title,
    string Description,
    string Url,
    DateTime PublishedAt,
    string Source,
    string? Author,
    string? ImageUrl,
    string? Content
);

[MetaNode(Id = "codex.news-feed.news-feed-response", Name = "News Feed Response", Description = "Response containing news items")]
public record NewsFeedResponse(
    List<NewsFeedItem> Items,
    int TotalCount,
    string Message
);

[MetaNode(Id = "codex.news-feed.trending-topic", Name = "Trending Topic", Description = "A trending topic from recent news")]
public record TrendingTopic(
    string Topic,
    int MentionCount,
    double TrendScore
);

[MetaNode(Id = "codex.news-feed.trending-topics-response", Name = "Trending Topics Response", Description = "Response containing trending topics")]
public record TrendingTopicsResponse(
    List<TrendingTopic> Topics,
    int TotalCount
);

[MetaNode(Id = "codex.news-feed.news-read-request", Name = "News Read Request", Description = "Request to mark news as read")]
public record NewsReadRequest(
    string UserId,
    string NewsId
);

[MetaNode(Id = "codex.news-feed.news-item-response", Name = "News Item Response", Description = "Response containing a single news item")]
public record NewsItemResponse(
    NewsFeedItem? Item
);
