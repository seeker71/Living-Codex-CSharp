using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// News Feed Module - Real news feed based on user interests from actual news data
/// Provides personalized news feeds based on user interests, location, and concept relationships
/// </summary>
/// <remarks>
/// Without external ingestion the module serves cached samples; live feeds require Real-time News Stream and storage backends.
/// </remarks>
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
        _logger.Info("NewsFeedModule constructor called");
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
    [RequireAuth]
    [ApiRoute("GET", "/news/feed/{userId}", "Get User News Feed", "Get personalized news feed for a user based on their interests", "codex.news-feed")]
    public async Task<object> GetUserNewsFeed(
        [ApiParameter("userId", "User ID to get news feed for", Required = true, Location = "path")] string userId,
        [ApiParameter("limit", "Number of news items to return", Required = false, Location = "query")] int limit = 50,
        [ApiParameter("skip", "Number of items to skip for pagination", Required = false, Location = "query")] int skip = 0,
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

            // Build full candidate list then page server-side
            var allItems = await BuildNewsItems(userInterests, userLocation, userContributions, hoursBack);
            var totalCount = allItems.Count;
            var paged = allItems.Skip(Math.Max(0, skip)).Take(Math.Max(1, Math.Min(limit, 500))).ToList();

            return new NewsFeedResponse(paged, totalCount, "Personalized news feed");
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
            var interests = request.Interests ?? new List<string>();
            var contributions = request.Contributions ?? new List<string>();
            var limit = request.Limit ?? 20;
            var hoursBack = request.HoursBack ?? 24;
            var skip = request.Skip ?? 0;

            var allItems = await BuildNewsItems(interests, request.Location, contributions, hoursBack);
            var totalCount = allItems.Count;
            var paged = allItems.Skip(Math.Max(0, skip)).Take(Math.Max(1, Math.Min(limit, 500))).ToList();

            return new NewsFeedResponse(paged, totalCount, "News search results");
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
        [ApiParameter("query", "Number of trending topics to return", Required = false, Location = "query")] int limit = 20,
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

    // Get news item summary
    [ApiRoute("GET", "/news/summary/{id}", "Get News Summary", "Get summary content for a news item", "codex.news-feed")]
    public async Task<object> GetNewsSummary([ApiParameter("id", "News item ID", Required = true, Location = "path")] string id)
    {
        try
        {
            var node = Registry.GetNode(id);
            if (node?.TypeId != "codex.news.item")
            {
                return new ErrorResponse("News item not found");
            }

            // First try to get summary from linked summary node
            var summaryNodeId = node.Meta?.GetValueOrDefault("summaryNodeId")?.ToString();
            if (!string.IsNullOrWhiteSpace(summaryNodeId))
            {
                var summaryNode = Registry.GetNode(summaryNodeId);
                if (summaryNode?.TypeId == "codex.content.summary" && summaryNode.Content?.InlineJson != null)
                {
                    return new NewsSummaryResponse(id, summaryNode.Content.InlineJson, "available");
                }
            }

            // Fallback to explicit summary field in metadata
            var summary = node.Meta?.GetValueOrDefault("summary")?.ToString();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                return new NewsSummaryResponse(id, summary, "available");
            }

            // Check if summary is being generated
            var isGenerating = node.Meta?.GetValueOrDefault("summaryGenerating")?.ToString() == "true";
            if (isGenerating)
            {
                return new NewsSummaryResponse(id, "", "generating");
            }
            
            // No summary available and not generating
            return new NewsSummaryResponse(id, "", "none");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news summary for {id}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting news summary: {ex.Message}");
        }
    }

    // Get news item concepts
    [ApiRoute("GET", "/news/concepts/{id}", "Get News Concepts", "Get extracted concepts for a news item", "codex.news-feed")]
    public async Task<object> GetNewsConcepts([ApiParameter("id", "News item ID", Required = true, Location = "path")] string id)
    {
        try
        {
            var node = Registry.GetNode(id);
            if (node?.TypeId != "codex.news.item")
            {
                return new ErrorResponse("News item not found");
            }

            var concepts = new List<object>();

            // Get concept node IDs from metadata
            var conceptNodeIds = node.Meta?.GetValueOrDefault("conceptNodeIds") as string[];
            if (conceptNodeIds != null && conceptNodeIds.Length > 0)
            {
                foreach (var conceptNodeId in conceptNodeIds)
                {
                    var conceptNode = Registry.GetNode(conceptNodeId);
                    if (conceptNode != null)
                    {
                        // Get edges to find relationship data
                        var edges = Registry.GetEdges()
                            .Where(e => e.FromId == id && e.ToId == conceptNodeId && e.Role == "relates-to")
                            .ToList();

                        var edge = edges.FirstOrDefault();
                        var extractedAt = edge?.Meta?.GetValueOrDefault("extractedAt")?.ToString();
                        var concept = edge?.Meta?.GetValueOrDefault("concept")?.ToString();

                        concepts.Add(new
                        {
                            id = conceptNode.Id,
                            name = conceptNode.Title ?? concept ?? "Unknown Concept",
                            description = conceptNode.Description,
                            weight = 1.0, // Default weight
                            resonance = 0.5, // Default resonance
                            confidence = 0.8, // Default confidence
                            extractedAt = extractedAt,
                            conceptType = conceptNode.TypeId,
                            axes = new string[0], // Could be populated from U-Core mapping
                            meta = conceptNode.Meta
                        });
                    }
                }
            }

            return new
            {
                success = true,
                concepts = concepts.OrderByDescending(c => ((dynamic)c).weight).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting news concepts for {id}: {ex.Message}", ex);
            return new ErrorResponse($"Error getting news concepts: {ex.Message}");
        }
    }

    // Get related news items
    [ApiRoute("GET", "/news/related/{id}", "Get Related News", "Get related news items", "codex.news-feed")]
    public async Task<object> GetRelatedNews(
        [ApiParameter("id", "News item ID", Required = true, Location = "path")] string id,
        [ApiParameter("query", "Number of related items to return", Required = false, Location = "query")] int limit = 20)
    {
        try
        {
            var sourceNode = Registry.GetNode(id);
            if (sourceNode?.TypeId != "codex.news.item")
            {
                return new ErrorResponse("Source news item not found");
            }

            // Get all nodes and filter by type
            // Use efficient type-specific query
            var allNewsNodes = Registry.GetNodesByType("codex.news.item").ToList();
            if (!allNewsNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.item");
                allNewsNodes = fromStorage.ToList();
            }
            
            // Filter out the current item and calculate similarity
            var relatedNewsNodes = allNewsNodes
                .Where(n => n.Id != id)
                .OrderByDescending(n => CalculateNewsSimilarity(sourceNode, n))
                .Take(limit)
                .ToList();

            var relatedItems = relatedNewsNodes
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
                Id: $"codex.news.read.{request.UserId}.{request.NewsId}.{Guid.NewGuid():N}",
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
        [ApiParameter("query", "Number of items to return", Required = false, Location = "query")] int limit = 50)
    {
        try
        {
            // Get all nodes and filter by type
            // Use efficient type-specific query for read news
            var readNodes = Registry.GetNodesByType("codex.news.read").ToList();
            if (!readNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.read");
                readNodes = fromStorage.ToList();
            }
            
            // Filter by user
            readNodes = readNodes
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
    [RequireAuth]
    [ApiRoute("GET", "/news/unread/{userId}", "Get Unread News", "Get unread news items for user", "codex.news-feed")]
    public async Task<object> GetUnreadNews(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("query", "Number of items to return", Required = false, Location = "query")] int limit = 50)
    {
        try
        {
            // Get all nodes and filter by type
            // Use efficient type-specific queries
            var allNewsNodes = Registry.GetNodesByType("codex.news.item").ToList();
            if (!allNewsNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.item");
                allNewsNodes = fromStorage.ToList();
            }
            
            var readNodes = Registry.GetNodesByType("codex.news.read").ToList();
            if (!readNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.read");
                readNodes = fromStorage.ToList();
            }

            // Get read news IDs for this user
            var readNewsIds = readNodes
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

    // Get latest news items (utility endpoint for UI/testing)
    [ApiRoute("GET", "/news/latest", "Get Latest News", "Get latest news items across all sources", "codex.news-feed")]
    public async Task<object> GetLatestNews(
        [ApiParameter("limit", "Number of items to return", Required = false, Location = "query")] int limit = 20,
        [ApiParameter("skip", "Number of items to skip", Required = false, Location = "query")] int skip = 0)
    {
        try
        {
            // Use efficient type-specific query instead of AllNodes + filtering
            var newsNodes = _registry.GetNodesByType("codex.news.item").ToList();
            if (!newsNodes.Any())
            {
                // Fallback to async if in-memory cache is empty
                var fromStorage = await _registry.GetNodesByTypeAsync("codex.news.item");
                newsNodes = fromStorage.ToList();
            }
            
            _logger.Info($"Found {newsNodes.Count} news nodes in registry");
            
            var totalCount = newsNodes.Count;
            
            // Robust publishedAt parsing (DateTime, DateTimeOffset, string ISO)
            DateTime? GetPublishedAt(Node n)
            {
                try
                {
                    var raw = n.Meta?.GetValueOrDefault("publishedAt");
                    if (raw is DateTime dt) return dt.ToUniversalTime();
                    if (raw is DateTimeOffset dto) return dto.UtcDateTime;
                    if (raw is string s && DateTime.TryParse(s, out var parsed))
                    {
                        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                    }
                }
                catch { }
                return null;
            }
            
            var items = newsNodes
                .OrderByDescending(n => GetPublishedAt(n) ?? DateTime.MinValue)
                .Skip(skip)
                .Take(Math.Max(1, Math.Min(limit, 500)))
                .Select(MapNodeToNewsFeedItem)
                .Where(i => i != null)
                .Cast<NewsFeedItem>()
                .ToList();
                
            _logger.Info($"Successfully mapped {items.Count} news items");

            return new NewsFeedResponse(items, totalCount, "Latest news items");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting latest news: {ex.Message}", ex);
            return new ErrorResponse($"Error getting latest news: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/news/stats", "Get News Stats", "Get news totals and source breakdown with optional filters", "codex.news-feed")]
    public async Task<object> GetNewsStats(
        [ApiParameter("hoursBack", "Number of hours to look back for news", Required = false, Location = "query")] int hoursBack = 24,
        [ApiParameter("search", "Search term to filter topics", Required = false, Location = "query")] string? search = null)
    {
        try
        {
            var interests = string.IsNullOrWhiteSpace(search) ? new List<string>() : new List<string> { search! };
            var allItems = await BuildNewsItems(interests, null, new List<string>(), hoursBack);
            var totalCount = allItems.Count;
            var sources = allItems
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Source) ? "Unknown" : i.Source)
                .OrderByDescending(g => g.Count())
                .ToDictionary(g => g.Key, g => g.Count());

            return new
            {
                success = true,
                message = "News stats computed",
                totalCount,
                hoursBack,
                sources
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error computing news stats: {ex.Message}", ex);
            return new ErrorResponse($"Error computing news stats: {ex.Message}");
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
        var all = await BuildNewsItems(interests, location, contributions, hoursBack);
        return all.Take(limit).ToList();
    }

    private async Task<List<NewsFeedItem>> BuildNewsItems(
        List<string> interests,
        string? location,
        List<string> contributions,
        int hoursBack)
    {
        var newsItems = new List<NewsFeedItem>();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-hoursBack);

            // Use prefix to include any news variants (e.g., codex.news.item, codex.news.something)
            var allNewsNodes = Registry.GetNodesByTypePrefix("codex.news").ToList();
            if (!allNewsNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.item");
                allNewsNodes = fromStorage.ToList();
            }

            // Robust publishedAt parsing (DateTime, DateTimeOffset, string ISO)
            DateTime? GetPublishedAt(Node n)
            {
                try
                {
                    var raw = n.Meta?.GetValueOrDefault("publishedAt");
                    if (raw is DateTime dt) return dt.ToUniversalTime();
                    if (raw is DateTimeOffset dto) return dto.UtcDateTime;
                    if (raw is string s && DateTime.TryParse(s, out var parsed))
                    {
                        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                    }
                }
                catch { }
                return null;
            }

            var newsNodes = allNewsNodes
                .Where(n =>
                {
                    var pub = GetPublishedAt(n);
                    // If missing publishedAt, include conservatively so items are not dropped
                    return pub == null || pub.Value >= cutoffDate;
                })
                .OrderByDescending(n => GetPublishedAt(n) ?? DateTime.MinValue)
                .ToList();

            foreach (var node in newsNodes)
            {
                var newsItem = MapNodeToNewsFeedItem(node);
                if (newsItem != null)
                {
                    newsItems.Add(newsItem);
                }
            }

            if (interests.Any())
            {
                newsItems = newsItems
                    .Where(item => interests.Any(interest =>
                        item.Title.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                        item.Description.Contains(interest, StringComparison.OrdinalIgnoreCase) ||
                        (item.Content?.Contains(interest, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .ToList();
            }

            newsItems = newsItems
                .OrderByDescending(item => CalculateRelevanceScore(item, interests, contributions))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.Error($"Error building news items: {ex.Message}", ex);
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
                            Id: $"newsapi-{Guid.NewGuid():N}",
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
                            Id: $"nytimes-{Guid.NewGuid():N}",
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
                            Id: $"guardian-{Guid.NewGuid():N}",
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
            // Get all nodes and filter by type
            // Use efficient type-specific query
            var newsNodes = Registry.GetNodesByType("codex.news.item").ToList();
            if (!newsNodes.Any())
            {
                var fromStorage = await Registry.GetNodesByTypeAsync("codex.news.item");
                newsNodes = fromStorage.ToList();
            }
            
            // Filter by time range
            var cutoffDate = DateTime.UtcNow.AddHours(-hoursBack);
            newsNodes = newsNodes
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
            DateTime published;
            var raw = node.Meta?.GetValueOrDefault("publishedAt");
            if (raw is DateTime dt)
            {
                published = dt.ToUniversalTime();
            }
            else if (raw is DateTimeOffset dto)
            {
                published = dto.UtcDateTime;
            }
            else if (raw is string s && DateTime.TryParse(s, out var parsed))
            {
                published = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }
            else
            {
                published = DateTime.UtcNow;
            }

            // Resolve URL from common metadata keys
            string ResolveUrl(Node n)
            {
                var keys = new[] { "url", "link", "sourceUrl", "originUrl" };
                foreach (var k in keys)
                {
                    var val = n.Meta?.GetValueOrDefault(k)?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val!;
                }
                return "";
            }

            var url = ResolveUrl(node);
            
            // Try to get source from serialized NewsItem content first
            string? source = null;
            if (node.Content?.InlineJson != null)
            {
                try
                {
                    var newsItem = JsonSerializer.Deserialize<NewsItem>(node.Content.InlineJson);
                    source = newsItem?.Source;
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to deserialize NewsItem content: {ex.Message}");
                }
            }
            
            // Fallback to metadata
            if (string.IsNullOrWhiteSpace(source))
            {
                source = node.Meta?.GetValueOrDefault("source")?.ToString();
            }
            
            var normalizedSource = NormalizeSource(source, url);
            if (string.Equals(normalizedSource, "Unknown", StringComparison.OrdinalIgnoreCase))
            {
                // Try alternate meta keys to derive a better source name
                var alt = node.Meta?.GetValueOrDefault("siteName")?.ToString()
                          ?? node.Meta?.GetValueOrDefault("publisher")?.ToString()
                          ?? node.Meta?.GetValueOrDefault("sourceName")?.ToString();
                if (!string.IsNullOrWhiteSpace(alt))
                    normalizedSource = alt!;
            }

            return new NewsFeedItem(
                Id: node.Id,
                Title: node.Title ?? "",
                Description: node.Description ?? "",
                Url: url,
                PublishedAt: published,
                Source: normalizedSource,
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

    private string NormalizeSource(string? source, string? url)
    {
        try
        {
            var s = (source ?? "").Trim();
            if (string.Equals(s, "unknown", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s, "ai", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(s))
            {
                // Derive from URL host
                if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host.ToLowerInvariant();
                    // Strip common subdomains
                    foreach (var sub in new[] { "www.", "m.", "amp.", "feedproxy." })
                    {
                        if (host.StartsWith(sub)) host = host.Substring(sub.Length);
                    }

                    // Known mappings
                    if (host.Contains("ycombinator") || host.Contains("hackernews")) return "Hacker News";
                    if (host.Contains("nytimes")) return "New York Times";
                    if (host.Contains("guardian")) return "The Guardian";
                    if (host.Contains("bbc")) return "BBC";
                    if (host.Contains("reuters")) return "Reuters";
                    if (host.Contains("bloomberg")) return "Bloomberg";
                    if (host.Contains("washingtonpost")) return "The Washington Post";
                    if (host.Contains("wsj")) return "Wall Street Journal";
                    if (host.Contains("apnews") || host.Contains("associatedpress")) return "AP News";
                    if (host.Contains("theverge")) return "The Verge";
                    if (host.Contains("techcrunch")) return "TechCrunch";

                    // Convert host to brand name (take second-level domain, capitalize words)
                    var parts = host.Split('.');
                    var core = parts.Length >= 2 ? parts[parts.Length - 2] : host;
                    core = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(core.Replace('-', ' '));
                    return core;
                }
                return "Unknown";
            }

            // Clean simple lowercase tokens
            if (s.Length <= 3) // too short, likely not a brand
            {
                if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri2))
                {
                    var host2 = uri2.Host.ToLowerInvariant();
                    var parts2 = host2.Split('.');
                    var core2 = parts2.Length >= 2 ? parts2[parts2.Length - 2] : host2;
                    core2 = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(core2.Replace('-', ' '));
                    return core2;
                }
                return "Unknown";
            }

            return s;
        }
        catch
        {
            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source;
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
    int? HoursBack = 24,
    int? Skip = 0
);

[MetaNode(Id = "codex.news-feed.news-item", Name = "News Item", Description = "A news item from external sources")]
public record NewsFeedItem(
    string Id,
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

[MetaNode(Id = "codex.news-feed.news-summary-response", Name = "News Summary Response", Description = "Response containing news summary content")]
public record NewsSummaryResponse(
    string NewsId,
    string Summary,
    string Status = "available" // "available", "generating", "none", "error"
);
