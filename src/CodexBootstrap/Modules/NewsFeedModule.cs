using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// News Feed Module - Real news feed based on user interests from actual news data
/// Provides personalized news feeds based on user interests, location, and concept relationships
/// </summary>
[MetaNode(Id = "codex.news-feed", Name = "News Feed Module", Description = "Real news feed system based on user interests and actual news data")]
public sealed class NewsFeedModule : IModule, IRegistryModule
{
    private readonly NodeRegistry _localRegistry;
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;
    private NodeRegistry? _globalRegistry;

    public NewsFeedModule(NodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
    {
        _localRegistry = registry;
        _logger = logger;
        _httpClient = httpClient;
    }

    // Parameterless constructor for module loader
    public NewsFeedModule() : this(new NodeRegistry(), new Log4NetLogger(typeof(NewsFeedModule)), new HttpClient())
    {
    }

    /// <summary>
    /// Gets the registry to use - global registry if set, otherwise local registry
    /// This ensures the module uses the global registry when available
    /// </summary>
    private NodeRegistry Registry => _globalRegistry ?? _localRegistry;

    /// <summary>
    /// Sets the global registry for this module
    /// This ensures the module uses the global registry instead of a local one
    /// </summary>
    public void SetGlobalRegistry(NodeRegistry registry)
    {
        _globalRegistry = registry;
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.news-feed",
            name: "News Feed Module",
            version: "1.0.0",
            description: "Real news feed system based on user interests and actual news data",
            capabilities: new[] { 
                "news-feed", "personalization", "interest-matching", "news-aggregation", 
                "content-filtering", "real-time-updates", "user-preferences" 
            },
            tags: new[] { "news", "feed", "personalization", "content", "interests", "real-time" },
            specReference: "codex.spec.news-feed"
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Set the global registry if this is the first call
        if (_globalRegistry == null)
        {
            SetGlobalRegistry(registry);
        }
        
        Registry.Upsert(GetModuleNode());
        _logger.Info("News Feed Module registered");
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attribute-based routing
        _logger.Info("News Feed Module API handlers registered");
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attribute-based routing
        _logger.Info("News Feed Module HTTP endpoints registered");
    }

    // Get personalized news feed for user
    [ApiRoute("GET", "/news/feed/{userId}", "Get User News Feed", "Get personalized news feed for a user based on their interests", "codex.news-feed")]
    public async Task<object> GetUserNewsFeed(
        [ApiParameter("path", "User ID to get news feed for", Required = true, Location = "path")] string userId,
        [ApiParameter("query", "Number of news items to return", Required = false, Location = "query")] int limit = 20,
        [ApiParameter("query", "Number of hours to look back for news", Required = false, Location = "query")] int hoursBack = 24)
    {
        try
        {
            // Get user profile and interests
            if (!Registry.TryGet(userId, out var userNode))
            {
                return new ErrorResponse($"User {userId} not found");
            }

            var userInterests = ParseStringList(userNode.Meta?.GetValueOrDefault("interests")?.ToString());
            var userLocation = userNode.Meta?.GetValueOrDefault("location")?.ToString();
            var userContributions = ParseStringList(userNode.Meta?.GetValueOrDefault("contributions")?.ToString());

            if (!userInterests.Any())
            {
                return new NewsFeedResponse(new List<NewsFeedItem>(), 0, "No interests found for user");
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
            // Get news from multiple sources
            var newsSources = new[]
            {
                "https://newsapi.org/v2/everything",
                "https://api.nytimes.com/svc/search/v2/articlesearch.json",
                "https://content.guardianapis.com/search"
            };

            var tasks = newsSources.Select(source => GetNewsFromSource(source, interests, location, limit / newsSources.Length, hoursBack));
            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                newsItems.AddRange(result);
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
            // Get recent news items
            var newsItems = await GetNewsFeedItemsForInterests(
                new List<string> { "technology", "science", "business", "politics", "health" },
                null,
                new List<string>(),
                100, // Get more items for trend analysis
                hoursBack
            );

            // Extract keywords and count frequency
            var keywordCounts = new Dictionary<string, int>();
            
            foreach (var item in newsItems)
            {
                var keywords = ExtractKeywords($"{item.Title} {item.Description}");
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
                .Select(kvp => new TrendingTopic(kvp.Key, kvp.Value, CalculateTrendScore(kvp.Value, newsItems.Count)))
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
