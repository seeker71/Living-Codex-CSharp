using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using Microsoft.AspNetCore.Builder;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Real-time fractal news streaming module that ingests external news sources
    /// and transforms them through fractal analysis aligned with belief systems
    /// </summary>
    public class RealtimeNewsStreamModule : IModule, IHostedService
    {
        private readonly Core.ILogger _logger;
        private readonly NodeRegistry _registry;
        private readonly HttpClient _httpClient;
        private readonly Core.ConfigurationManager _configManager;
        private readonly Timer _ingestionTimer;
        private readonly Timer _cleanupTimer;
        private readonly int _ingestionIntervalMinutes = 15;
        private readonly int _cleanupIntervalHours = 24;
        private readonly int _maxItemsPerSource = 50; // Increased from 10
        private readonly HashSet<string> _processedNewsIds = new(); // Track processed news to prevent duplicates

        // Node type constants
        private const string NEWS_SOURCE_NODE_TYPE = "codex.news.source";
        private const string NEWS_ITEM_NODE_TYPE = "codex.news.item";
        private const string FRACTAL_NEWS_NODE_TYPE = "codex.news.fractal";
        private const string NEWS_SUBSCRIPTION_NODE_TYPE = "codex.news.subscription";

        public RealtimeNewsStreamModule(NodeRegistry registry, HttpClient? httpClient = null, Core.ConfigurationManager? configManager = null)
        {
            _logger = new Log4NetLogger(typeof(RealtimeNewsStreamModule));
            _registry = registry;
            _httpClient = httpClient ?? new HttpClient();
            _configManager = configManager ?? new Core.ConfigurationManager(registry, new Log4NetLogger(typeof(Core.ConfigurationManager)));
            
            // Set up timers
            _ingestionTimer = new Timer(async _ => await IngestNewsFromSources(null), null, TimeSpan.Zero, TimeSpan.FromMinutes(_ingestionIntervalMinutes));
            _cleanupTimer = new Timer(CleanupOldNews, null, TimeSpan.FromHours(1), TimeSpan.FromHours(_cleanupIntervalHours));
        }

        public string Name => "Real-Time News Stream";
        public string Description => "Ingests external news sources and transforms them through fractal analysis";
        public string Version => "1.0.0";

        public void Initialize()
        {
            _logger.Info("Initializing Real-Time News Stream Module");
        }

        public Node GetModuleNode()
        {
            return new Node(
                Id: "realtime-news-stream-module",
                TypeId: "codex.module.news.stream",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: "Real-Time News Stream Module",
                Description: "Ingests external news sources and transforms them through fractal analysis",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new { Name, Description, Version }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["version"] = Version,
                    ["moduleType"] = "news-streaming",
                    ["capabilities"] = new[] { "rss-ingestion", "api-ingestion", "fractal-analysis", "real-time-streaming" }
                }
            );
        }

        public void Register(NodeRegistry registry)
        {
            _logger.Info("Registering Real-Time News Stream Module with NodeRegistry");
            // Module registration is handled in Initialize()
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            _logger.Info("Registering API handlers for Real-Time News Stream Module");
            // API handlers are registered via ApiRoute attributes
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            _logger.Info("Registering HTTP endpoints for Real-Time News Stream Module");
            // HTTP endpoints are registered via ApiRoute attributes
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Starting Real-Time News Stream Module");
            
            // Load configurations from seed nodes
            await _configManager.LoadConfigurationsAsync();
            
            await IngestNewsFromSources(null);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Stopping Real-Time News Stream Module");
            _ingestionTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _httpClient?.Dispose();
        }

        private void InitializeNewsSources()
        {
            var newsSources = new[]
            {
                new NewsSource
                {
                    Id = "wired",
                    Name = "Wired",
                    Type = "rss",
                    Url = "https://www.wired.com/feed/rss",
                    Categories = new[] { "technology", "science", "culture", "business" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30
                },
                new NewsSource
                {
                    Id = "hackernews",
                    Name = "Hacker News",
                    Type = "api",
                    Url = "https://hacker-news.firebaseio.com/v0/topstories.json",
                    Categories = new[] { "technology", "programming", "startup", "science" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30
                },
                new NewsSource
                {
                    Id = "arstechnica",
                    Name = "Ars Technica",
                    Type = "rss",
                    Url = "https://feeds.arstechnica.com/arstechnica/index/",
                    Categories = new[] { "technology", "science", "policy", "gaming" },
                    IsActive = true,
                    UpdateIntervalMinutes = 25
                },
                new NewsSource
                {
                    Id = "techcrunch",
                    Name = "TechCrunch",
                    Type = "rss",
                    Url = "https://techcrunch.com/feed/",
                    Categories = new[] { "technology", "startup", "AI", "innovation" },
                    IsActive = true,
                    UpdateIntervalMinutes = 15
                },
                new NewsSource
                {
                    Id = "reddit-tech",
                    Name = "Reddit Technology",
                    Type = "rss",
                    Url = "https://www.reddit.com/r/technology/.rss",
                    Categories = new[] { "technology", "discussion", "community" },
                    IsActive = true,
                    UpdateIntervalMinutes = 20
                }
            };

            foreach (var source in newsSources)
            {
                var sourceNode = new Node(
                    Id: $"news-source-{source.Id}",
                    TypeId: NEWS_SOURCE_NODE_TYPE,
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: source.Name,
                    Description: $"News source: {source.Name}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(source),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["sourceId"] = source.Id,
                        ["name"] = source.Name,
                        ["type"] = source.Type,
                        ["url"] = source.Url,
                        ["isActive"] = source.IsActive,
                        ["updateIntervalMinutes"] = source.UpdateIntervalMinutes
                    }
                );

                _registry.Upsert(sourceNode);
            }

            _logger.Info($"Initialized {newsSources.Length} news sources as nodes");
        }

        private async Task IngestNewsFromSources(object? state)
        {
            try
            {
                _logger.Info("Starting news ingestion from all sources");

                // Get all active news source nodes
                var sourceNodes = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
                    .Where(n => n.Meta?.ContainsKey("isActive") == true && (bool)n.Meta["isActive"]);

                foreach (var sourceNode in sourceNodes)
                {
                    try
                    {
                        var source = JsonSerializer.Deserialize<NewsSource>(sourceNode.Content?.InlineJson ?? "{}");
                        if (source != null)
                        {
                            if (source.Type == "rss")
                            {
                                await IngestRssFeed(source);
                            }
                            else if (source.Type == "api")
                            {
                                await IngestApiFeed(source);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error ingesting from source {sourceNode.Meta?["name"]}: {ex.Message}", ex);
                    }
                }

                _logger.Info("Completed news ingestion cycle");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during news ingestion: {ex.Message}", ex);
            }
        }

        private async Task IngestRssFeed(NewsSource source)
        {
            try
            {
                using var reader = XmlReader.Create(source.Url);
                var feed = SyndicationFeed.Load(reader);

                foreach (var item in feed.Items.Take(_maxItemsPerSource))
                {
                    var newsItem = new NewsItem
                    {
                        Id = $"rss-{source.Id}-{Guid.NewGuid().ToString("N")[..8]}",
                        Title = item.Title?.Text ?? "",
                        Content = item.Summary?.Text ?? "",
                        Source = source.Name,
                        Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                        PublishedAt = item.PublishDate,
                        Tags = ExtractTagsFromContent(item.Title?.Text + " " + item.Summary?.Text),
                        Metadata = new Dictionary<string, object>
                        {
                            ["category"] = "technology",
                            ["sourceType"] = "RSS",
                            ["sourceId"] = source.Id,
                            ["rssId"] = item.Id
                        }
                    };

                    await ProcessNewsItem(newsItem);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting RSS feed {source.Name}: {ex.Message}", ex);
            }
        }

        private async Task IngestApiFeed(NewsSource source)
        {
            try
            {
                if (source.Id == "hackernews")
                {
                    await IngestHackerNews(source);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting API feed {source.Name}: {ex.Message}", ex);
            }
        }

        private async Task IngestHackerNews(NewsSource source)
        {
            try
            {
                // Get top story IDs
                var topStoriesResponse = await _httpClient.GetStringAsync(source.Url);
                var storyIds = JsonSerializer.Deserialize<int[]>(topStoriesResponse)?.Take(_maxItemsPerSource).ToArray() ?? new int[0];

                foreach (var storyId in storyIds)
                {
                    try
                    {
                        var storyResponse = await _httpClient.GetStringAsync($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json");
                        var story = JsonSerializer.Deserialize<HackerNewsStory>(storyResponse);
                        
                        if (story?.Title != null && story.Type == "story")
                        {
                            var newsItem = new NewsItem
                            {
                                Id = $"hn-{storyId}",
                                Title = story.Title,
                                Content = story.Text ?? "",
                                Source = source.Name,
                                Url = story.Url,
                                PublishedAt = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                                Tags = ExtractTagsFromContent(story.Title + " " + (story.Text ?? "")),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["category"] = "technology",
                                    ["sourceType"] = "API",
                                    ["sourceId"] = source.Id,
                                    ["score"] = story.Score,
                                    ["by"] = story.By
                                }
                            };

                            await ProcessNewsItem(newsItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error processing Hacker News story {storyId}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting Hacker News: {ex.Message}", ex);
            }
        }

        private async Task ProcessNewsItem(NewsItem newsItem)
        {
            try
            {
                // Check for duplicates
                if (_processedNewsIds.Contains(newsItem.Id))
                {
                    _logger.Debug($"Skipping duplicate news item: {newsItem.Id}");
                    return;
                }

                // Check if news item already exists in registry
                var existingNewsNode = _registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("newsId") == true && n.Meta["newsId"].ToString() == newsItem.Id);

                if (existingNewsNode != null)
                {
                    _logger.Debug($"News item already exists in registry: {newsItem.Id}");
                    return;
                }

                // Mark as processed
                _processedNewsIds.Add(newsItem.Id);

                // Store news item as node
                var newsNode = new Node(
                    Id: $"news-item-{newsItem.Id}",
                    TypeId: NEWS_ITEM_NODE_TYPE,
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: newsItem.Title,
                    Description: $"News item from {newsItem.Source}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(newsItem),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["newsId"] = newsItem.Id,
                        ["title"] = newsItem.Title,
                        ["source"] = newsItem.Source,
                        ["publishedAt"] = newsItem.PublishedAt,
                        ["url"] = newsItem.Url
                    }
                );

                _registry.Upsert(newsNode);

                // Create fractal news item
                var fractalNews = await CreateFractalNewsItem(newsItem);
                
                if (fractalNews != null)
                {
                    // Store fractal news as node
                    var fractalNode = new Node(
                        Id: $"fractal-news-{fractalNews.Id}",
                        TypeId: FRACTAL_NEWS_NODE_TYPE,
                        State: ContentState.Water,
                        Locale: "en-US",
                        Title: fractalNews.Headline,
                        Description: $"Fractal analysis of {newsItem.Title}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(fractalNews),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["fractalId"] = fractalNews.Id,
                            ["originalNewsId"] = fractalNews.OriginalNewsId,
                            ["headline"] = fractalNews.Headline,
                            ["abundanceScore"] = fractalNews.ResonanceData.AmplificationPotential,
                            ["resonanceScore"] = fractalNews.ResonanceData.ResonanceScore,
                            ["processedAt"] = fractalNews.ProcessedAt
                        }
                    );

                    _registry.Upsert(fractalNode);

                    _logger.Info($"Processed news item: {newsItem.Title} from {newsItem.Source}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing news item {newsItem.Title}: {ex.Message}", ex);
            }
        }

        private async Task<FractalNewsItem?> CreateFractalNewsItem(NewsItem newsItem)
        {
            try
            {
                // Use AI-backed dynamic analysis
                var conceptAnalysis = await PerformAIConceptExtraction(newsItem);
                var scoringAnalysis = await PerformAIScoringAnalysis(conceptAnalysis, newsItem);
                var fractalTransformation = await PerformAIFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);

                return new FractalNewsItem
                {
                    Id = $"fractal-{newsItem.Id}",
                    OriginalNewsId = newsItem.Id,
                    Headline = fractalTransformation.Headline,
                    BeliefSystemTranslation = fractalTransformation.BeliefTranslation,
                    Summary = fractalTransformation.Summary,
                    ImpactAreas = fractalTransformation.ImpactAreas,
                    AmplificationFactors = fractalTransformation.AmplificationFactors,
                    ResonanceData = fractalTransformation.ResonanceData,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Metadata = fractalTransformation.Metadata
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating fractal news item for {newsItem.Title}: {ex.Message}", ex);
                return null;
            }
        }

        // AI-Backed Dynamic Analysis System
        private async Task<ConceptAnalysis> PerformAIConceptExtraction(NewsItem newsItem)
        {
            try
            {
                // Check for cached analysis first
                var cacheKey = $"concept_analysis_{newsItem.Id}";
                var cachedAnalysis = await GetCachedAnalysis<ConceptAnalysis>(cacheKey);
                if (cachedAnalysis != null)
                {
                    _logger.Debug($"Using cached concept analysis for {newsItem.Id}");
                    return cachedAnalysis;
                }

                // Generate AI analysis code dynamically
                var analysisCode = await GenerateConceptExtractionCode(newsItem);
                var analysis = await ExecuteAnalysisCode(analysisCode, newsItem);

                // Cache the result
                await CacheAnalysis(cacheKey, analysis, TimeSpan.FromHours(24));

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI concept extraction: {ex.Message}", ex);
                return await FallbackConceptExtraction(newsItem);
            }
        }

        private async Task<ScoringAnalysis> PerformAIScoringAnalysis(ConceptAnalysis conceptAnalysis, NewsItem newsItem)
        {
            try
            {
                var cacheKey = $"scoring_analysis_{newsItem.Id}";
                var cachedAnalysis = await GetCachedAnalysis<ScoringAnalysis>(cacheKey);
                if (cachedAnalysis != null)
                {
                    return cachedAnalysis;
                }

                var scoringCode = await GenerateScoringAnalysisCode(conceptAnalysis, newsItem);
                var analysis = await ExecuteScoringCode(scoringCode, conceptAnalysis, newsItem);

                await CacheAnalysis(cacheKey, analysis, TimeSpan.FromHours(12));
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI scoring analysis: {ex.Message}", ex);
                return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
            }
        }

        private async Task<FractalTransformation> PerformAIFractalTransformation(NewsItem newsItem, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            try
            {
                var cacheKey = $"fractal_transformation_{newsItem.Id}";
                var cachedTransformation = await GetCachedAnalysis<FractalTransformation>(cacheKey);
                if (cachedTransformation != null)
                {
                    return cachedTransformation;
                }

                var transformationCode = await GenerateFractalTransformationCode(newsItem, conceptAnalysis, scoringAnalysis);
                var transformation = await ExecuteTransformationCode(transformationCode, newsItem, conceptAnalysis, scoringAnalysis);

                await CacheAnalysis(cacheKey, transformation, TimeSpan.FromHours(6));
                return transformation;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI fractal transformation: {ex.Message}", ex);
                return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
            }
        }

        private double CalculateAbundanceScore(List<string> concepts, NewsItem newsItem)
        {
            var abundanceKeywords = new[] { "abundance", "amplification", "collective", "community", "collaboration", "growth", "innovation" };
            var abundanceCount = concepts.Count(c => abundanceKeywords.Contains(c));
            var totalConcepts = Math.Max(concepts.Count, 1);
            
            return Math.Min(abundanceCount / (double)totalConcepts * 2.0, 1.0);
        }

        private double CalculateResonanceScore(List<string> concepts)
        {
            var resonanceKeywords = new[] { "love", "joy", "consciousness", "unity", "harmony", "peace", "wisdom" };
            var resonanceCount = concepts.Count(c => resonanceKeywords.Contains(c));
            var totalConcepts = Math.Max(concepts.Count, 1);
            
            return Math.Min(resonanceCount / (double)totalConcepts * 1.5, 1.0);
        }

        private string CreateBeliefSystemTranslation(NewsItem newsItem, List<string> concepts)
        {
            var abundanceKeywords = concepts.Where(c => new[] { "abundance", "amplification", "collective" }.Contains(c)).ToList();
            
            if (abundanceKeywords.Any())
            {
                return $"This development represents a significant step forward in our collective journey toward abundance. The concepts of {string.Join(", ", abundanceKeywords)} align with our core belief in amplifying individual contributions through collective resonance.";
            }
            
            return $"This news item has the potential to contribute to collective understanding and growth, offering opportunities for amplification and abundance through thoughtful engagement.";
        }

        private string CreateAbundanceHeadline(NewsItem newsItem, List<string> concepts)
        {
            var abundanceKeywords = concepts.Where(c => new[] { "abundance", "amplification", "collective" }.Contains(c)).ToList();
            
            if (abundanceKeywords.Any())
            {
                return $"🌟 {newsItem.Title} - Amplifying Collective Abundance Through {string.Join(" & ", abundanceKeywords).ToUpperInvariant()}";
            }
            
            return $"📰 {newsItem.Title} - Potential for Collective Growth and Understanding";
        }

        private List<string> DetermineImpactAreas(List<string> concepts)
        {
            var impactAreas = new List<string>();
            
            if (concepts.Contains("ai") || concepts.Contains("artificial intelligence"))
                impactAreas.Add("AI & Technology");
            if (concepts.Contains("abundance") || concepts.Contains("amplification"))
                impactAreas.Add("Collective Abundance");
            if (concepts.Contains("sustainability") || concepts.Contains("climate"))
                impactAreas.Add("Environmental Impact");
            if (concepts.Contains("startup") || concepts.Contains("funding"))
                impactAreas.Add("Economic Innovation");
            
            return impactAreas.Any() ? impactAreas : new List<string> { "General Technology" };
        }

        private List<string> DetermineAmplificationFactors(List<string> concepts, double abundanceScore)
        {
            var factors = new List<string>();
            
            if (abundanceScore > 0.7)
                factors.Add("High Collective Impact Potential");
            if (concepts.Contains("community") || concepts.Contains("collaboration"))
                factors.Add("Community Engagement");
            if (concepts.Contains("innovation") || concepts.Contains("breakthrough"))
                factors.Add("Innovation Catalyst");
            if (concepts.Contains("research") || concepts.Contains("science"))
                factors.Add("Scientific Advancement");
            
            return factors.Any() ? factors : new List<string> { "General Interest" };
        }

        private string CreateFractalSummary(NewsItem newsItem, List<string> concepts)
        {
            var summary = $"This news item from {newsItem.Source} presents opportunities for collective growth and understanding. ";
            
            if (concepts.Contains("abundance"))
                summary += "The content aligns with abundance principles and collective amplification. ";
            
            if (concepts.Contains("innovation"))
                summary += "Innovation and breakthrough potential are evident. ";
            
            summary += "Through thoughtful engagement, this information can contribute to our shared journey of growth and understanding.";
            
            return summary;
        }

        private string[] ExtractTagsFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return Array.Empty<string>();

            var tags = new List<string>();
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var word in words)
            {
                var cleanWord = word.ToLower().Trim(new char[] { '.', ',', '!', '?', ';', ':', '"', '(', ')', '[', ']', '{', '}' });
                if (cleanWord.Length > 3 && !tags.Contains(cleanWord))
                {
                    tags.Add(cleanWord);
                }
            }
            
            return tags.Take(10).ToArray(); // Limit to 10 tags
        }

        private void CleanupOldNews(object? state)
        {
            try
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
                var oldNewsNodes = _registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
                    .Where(n => n.Meta?.ContainsKey("publishedAt") == true && 
                               DateTime.TryParse(n.Meta["publishedAt"].ToString(), out var publishedAt) && 
                               publishedAt < cutoffDate.DateTime)
                    .ToList();

                foreach (var node in oldNewsNodes)
                {
                    _registry.RemoveNode(node.Id);
                }

                _logger.Info($"Cleaned up {oldNewsNodes.Count} old news items");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during cleanup: {ex.Message}", ex);
            }
        }

        // API Endpoints

        [Get("/news/stream/sources", "Get News Sources", "Retrieve all configured news sources", "realtime-news-stream")]
        public async Task<object> GetNewsSourcesAsync()
        {
            try
            {
                var sourceNodes = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE);
                var sources = sourceNodes.Select(n => JsonSerializer.Deserialize<NewsSource>(n.Content?.InlineJson ?? "{}")).Where(s => s != null).ToList();

                return new
                {
                    success = true,
                    sources = sources,
                    totalCount = sources.Count,
                    activeCount = sources.Count(s => s.IsActive)
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting news sources: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to retrieve news sources",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Post("/news/stream/subscribe", "Subscribe to News Feed", "Subscribe to personalized news feed", "realtime-news-stream")]
        public async Task<object> SubscribeToNewsFeedAsync([ApiParameter("request", "Subscription request")] NewsSubscriptionRequest request)
        {
            try
            {
                var subscription = new NewsSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    InterestAreas = request.InterestAreas,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                // Store subscription as node
                var subscriptionNode = new Node(
                    Id: $"news-subscription-{subscription.Id}",
                    TypeId: NEWS_SUBSCRIPTION_NODE_TYPE,
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: $"News subscription for {subscription.UserId}",
                    Description: $"Personalized news feed subscription",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(subscription),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["subscriptionId"] = subscription.Id,
                        ["userId"] = subscription.UserId,
                        ["isActive"] = subscription.IsActive,
                        ["interestAreas"] = subscription.InterestAreas
                    }
                );

                _registry.Upsert(subscriptionNode);

                _logger.Info($"User {request.UserId} subscribed to news feed");

                return new
                {
                    success = true,
                    subscriptionId = subscription.Id,
                    message = "Successfully subscribed to news feed"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error subscribing to news feed: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to subscribe to news feed",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Get("/news/stream/feed/{userId}", "Get Personalized News Feed", "Retrieve personalized news feed for user", "realtime-news-stream")]
        public async Task<object> GetPersonalizedNewsFeedAsync([ApiParameter("userId", "User ID")] string userId, [ApiParameter("maxItems", "Maximum number of items")] int maxItems = 10)
        {
            try
            {
                // Get user subscriptions from nodes
                var subscriptionNodes = _registry.GetNodesByType(NEWS_SUBSCRIPTION_NODE_TYPE)
                    .Where(n => n.Meta?.ContainsKey("userId") == true && n.Meta["userId"].ToString() == userId)
                    .Where(n => n.Meta?.ContainsKey("isActive") == true && (bool)n.Meta["isActive"]);

                if (!subscriptionNodes.Any())
                {
                    return new
                    {
                        success = true,
                        newsItems = new List<FractalNewsItem>(),
                        totalCount = 0,
                        message = "No active subscriptions found"
                    };
                }

                // Get fractal news items from nodes
                var fractalNodes = _registry.GetNodesByType(FRACTAL_NEWS_NODE_TYPE)
                    .OrderByDescending(n => n.Meta?.ContainsKey("processedAt") == true ? 
                        DateTimeOffset.TryParse(n.Meta["processedAt"].ToString(), out var processedAt) ? processedAt : DateTimeOffset.MinValue : DateTimeOffset.MinValue)
                    .Take(maxItems);

                var newsItems = fractalNodes
                    .Select(n => JsonSerializer.Deserialize<FractalNewsItem>(n.Content?.InlineJson ?? "{}"))
                    .Where(n => n != null)
                    .ToList();

                return new
                {
                    success = true,
                    newsItems = newsItems,
                    totalCount = newsItems.Count,
                    message = $"Retrieved {newsItems.Count} news items"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting personalized news feed: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to retrieve news feed",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Get("/news/stream/fractal/{newsId}", "Get Fractal Analysis", "Retrieve fractal analysis for news item", "realtime-news-stream")]
        public async Task<object> GetFractalAnalysisAsync([ApiParameter("newsId", "News item ID")] string newsId)
        {
            try
            {
                var fractalId = $"fractal-{newsId}";
                var fractalNode = _registry.GetNodesByType(FRACTAL_NEWS_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("fractalId") == true && n.Meta["fractalId"].ToString() == fractalId);

                if (fractalNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News item not found",
                        Code = "Invalid news ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var fractalNews = JsonSerializer.Deserialize<FractalNewsItem>(fractalNode.Content?.InlineJson ?? "{}");

                return new
                {
                    success = true,
                    fractalNews = fractalNews
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting fractal analysis: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to retrieve fractal analysis",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Post("/news/stream/ingest", "Ingest News Item", "Manually ingest a news item", "realtime-news-stream")]
        public async Task<object> IngestNewsItemAsync([ApiParameter("newsItem", "News item to ingest")] NewsItem newsItem)
        {
            try
            {
                await ProcessNewsItem(newsItem);

                return new
                {
                    success = true,
                    newsId = newsItem.Id,
                    message = "News item ingested successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting news item: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to ingest news item",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Delete("/news/stream/unsubscribe/{id}", "Unsubscribe from News Feed", "Unsubscribe from news feed", "realtime-news-stream")]
        public async Task<object> UnsubscribeFromNewsFeedAsync([ApiParameter("id", "Subscription ID")] string id)
        {
            try
            {
                var subscriptionNode = _registry.GetNodesByType(NEWS_SUBSCRIPTION_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("subscriptionId") == true && n.Meta["subscriptionId"].ToString() == id);

                if (subscriptionNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "Subscription not found",
                        Code = "Invalid subscription ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                _registry.RemoveNode(subscriptionNode.Id);

                return new
                {
                    success = true,
                    message = "Successfully unsubscribed from news feed"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error unsubscribing from news feed: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to unsubscribe from news feed",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Post("/news/stream/sources", "Add News Source", "Add a new news source configuration", "realtime-news-stream")]
        public async Task<object> AddNewsSourceAsync([ApiParameter("source", "News source configuration")] NewsSource source)
        {
            try
            {
                // Convert to NewsSourceConfig
                var sourceConfig = new NewsSourceConfig
                {
                    Id = source.Id,
                    Name = source.Name,
                    Type = source.Type,
                    Url = source.Url,
                    Categories = source.Categories?.ToArray() ?? Array.Empty<string>(),
                    OntologyLevels = OntologyLevelHelper.DetermineOntologyLevelsFromCategories(source.Categories?.ToList() ?? new List<string>()),
                    IsActive = source.IsActive,
                    UpdateIntervalMinutes = source.UpdateIntervalMinutes,
                    Priority = 1,
                    LastIngested = null,
                    Metadata = new Dictionary<string, object>()
                };

                // Update configuration
                await _configManager.UpdateNewsSourceAsync(sourceConfig);

                _logger.Info($"Added new news source: {source.Name} ({source.Id})");

                return new
                {
                    success = true,
                    sourceId = source.Id,
                    message = "News source added successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding news source: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to add news source",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Put("/news/stream/sources/{id}", "Update News Source", "Update an existing news source configuration", "realtime-news-stream")]
        public async Task<object> UpdateNewsSourceAsync([ApiParameter("id", "Source ID")] string id, [ApiParameter("source", "Updated news source configuration")] NewsSource source)
        {
            try
            {
                // Find existing source
                var existingSource = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("sourceId") == true && n.Meta["sourceId"].ToString() == id);

                if (existingSource == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News source not found",
                        Code = "Invalid source ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Update the source
                source.Id = id; // Ensure ID matches
                var updatedSourceNode = new Node(
                    Id: $"news-source-{source.Id}",
                    TypeId: NEWS_SOURCE_NODE_TYPE,
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: source.Name,
                    Description: $"News source: {source.Name}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(source),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["sourceId"] = source.Id,
                        ["name"] = source.Name,
                        ["type"] = source.Type,
                        ["url"] = source.Url,
                        ["isActive"] = source.IsActive,
                        ["updateIntervalMinutes"] = source.UpdateIntervalMinutes
                    }
                );

                _registry.Upsert(updatedSourceNode);

                _logger.Info($"Updated news source: {source.Name} ({source.Id})");

                return new
                {
                    success = true,
                    sourceId = source.Id,
                    message = "News source updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating news source: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to update news source",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Delete("/news/stream/sources/{id}", "Remove News Source", "Remove a news source configuration", "realtime-news-stream")]
        public async Task<object> RemoveNewsSourceAsync([ApiParameter("id", "Source ID")] string id)
        {
            try
            {
                var sourceNode = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("sourceId") == true && n.Meta["sourceId"].ToString() == id);

                if (sourceNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News source not found",
                        Code = "Invalid source ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                _registry.RemoveNode(sourceNode.Id);

                _logger.Info($"Removed news source: {id}");

                return new
                {
                    success = true,
                    message = "News source removed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error removing news source: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to remove news source",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Post("/news/stream/sources/{id}/toggle", "Toggle News Source", "Enable or disable a news source", "realtime-news-stream")]
        public async Task<object> ToggleNewsSourceAsync([ApiParameter("id", "Source ID")] string id)
        {
            try
            {
                var sourceNode = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("sourceId") == true && n.Meta["sourceId"].ToString() == id);

                if (sourceNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News source not found",
                        Code = "Invalid source ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Get current source data
                var source = JsonSerializer.Deserialize<NewsSource>(sourceNode.Content?.InlineJson ?? "{}");
                if (source == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "Invalid source data",
                        Code = "Data corruption",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Toggle active state
                source.IsActive = !source.IsActive;

                // Update the source node
                var updatedSourceNode = new Node(
                    Id: sourceNode.Id,
                    TypeId: sourceNode.TypeId,
                    State: sourceNode.State,
                    Locale: sourceNode.Locale,
                    Title: sourceNode.Title,
                    Description: sourceNode.Description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(source),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>(sourceNode.Meta ?? new Dictionary<string, object>())
                    {
                        ["isActive"] = source.IsActive
                    }
                );

                _registry.Upsert(updatedSourceNode);

                _logger.Info($"Toggled news source {id}: {(source.IsActive ? "enabled" : "disabled")}");

                return new
                {
                    success = true,
                    sourceId = id,
                    isActive = source.IsActive,
                    message = $"News source {(source.IsActive ? "enabled" : "disabled")} successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error toggling news source: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to toggle news source",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        [Post("/news/stream/ingest/source/{id}", "Ingest from Specific Source", "Manually trigger ingestion from a specific news source", "realtime-news-stream")]
        public async Task<object> IngestFromSourceAsync([ApiParameter("id", "Source ID")] string id)
        {
            try
            {
                var sourceNode = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("sourceId") == true && n.Meta["sourceId"].ToString() == id);

                if (sourceNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News source not found",
                        Code = "Invalid source ID",
                        Timestamp = DateTime.UtcNow
                    };
                }

                var source = JsonSerializer.Deserialize<NewsSource>(sourceNode.Content?.InlineJson ?? "{}");
                if (source == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "Invalid source data",
                        Code = "Data corruption",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Ingest from the specific source
                if (source.Type == "rss")
                {
                    await IngestRssFeed(source);
                }
                else if (source.Type == "api")
                {
                    await IngestApiFeed(source);
                }

                _logger.Info($"Manually ingested from source: {source.Name} ({source.Id})");

                return new
                {
                    success = true,
                    sourceId = id,
                    sourceName = source.Name,
                    message = $"Successfully ingested from {source.Name}"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting from source {id}: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to ingest from source",
                    Code = "Internal Server Error",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
    }

    // Data Transfer Objects
    public class NewsSource
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Url { get; set; } = "";
        public string[] Categories { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; }
        public int UpdateIntervalMinutes { get; set; }
    }

    public class NewsItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Source { get; set; } = "";
        public string Url { get; set; } = "";
        public DateTimeOffset PublishedAt { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    // AI Analysis Data Structures
    public class ConceptAnalysis
    {
        public List<string> Concepts { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public List<string> Entities { get; set; } = new();
        public List<string> Themes { get; set; } = new();
        public Dictionary<string, double> ConceptWeights { get; set; } = new();
        public Dictionary<string, string> ConceptRelationships { get; set; } = new();
        public List<string> OntologyLevels { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ScoringAnalysis
    {
        public double AbundanceScore { get; set; }
        public double ConsciousnessLevel { get; set; }
        public double ResonanceScore { get; set; }
        public double InnovationScore { get; set; }
        public double ImpactScore { get; set; }
        public double UnityScore { get; set; }
        public Dictionary<string, double> DetailedScores { get; set; } = new();
        public List<string> ScoringFactors { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FractalTransformation
    {
        public string Headline { get; set; } = string.Empty;
        public string BeliefTranslation { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> ImpactAreas { get; set; } = new();
        public List<string> AmplificationFactors { get; set; } = new();
        public ResonanceData ResonanceData { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class FractalNewsItem
    {
        public string Id { get; set; } = "";
        public string OriginalNewsId { get; set; } = "";
        public string Headline { get; set; } = "";
        public string BeliefSystemTranslation { get; set; } = "";
        public string Summary { get; set; } = "";
        public List<string> ImpactAreas { get; set; } = new();
        public List<string> AmplificationFactors { get; set; } = new();
        public ResonanceData ResonanceData { get; set; } = new();
        public DateTimeOffset ProcessedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ResonanceData
    {
        public double ResonanceScore { get; set; }
        public double AmplificationPotential { get; set; }
        public string CollectiveImpact { get; set; } = "";
        public List<string> ResonanceFactors { get; set; } = new();
    }

    public class NewsSubscription
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string[] InterestAreas { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class NewsSubscriptionRequest
    {
        public string UserId { get; set; } = "";
        public string[] InterestAreas { get; set; } = Array.Empty<string>();
    }

    public class HackerNewsStory
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public string Url { get; set; } = "";
        public int Score { get; set; }
        public string By { get; set; } = "";
        public int Time { get; set; }
        public string Type { get; set; } = "";
    }

    // Helper method to determine ontology levels from categories
    public static class OntologyLevelHelper
    {
        public static string[] DetermineOntologyLevelsFromCategories(List<string> categories)
        {
            var levelMapping = new Dictionary<string, string>
            {
                ["technology"] = "L0-L2",
                ["science"] = "L0-L2",
                ["research"] = "L0-L2",
                ["consciousness"] = "L3-L4",
                ["spirituality"] = "L3-L4",
                ["mindfulness"] = "L3-L4",
                ["unity"] = "L5-L6",
                ["global"] = "L5-L6",
                ["collaboration"] = "L5-L6",
                ["cosmology"] = "L15-L16",
                ["quantum"] = "L15-L16",
                ["universe"] = "L15-L16",
                ["sustainability"] = "L9-L10",
                ["environment"] = "L9-L10",
                ["creativity"] = "L17-L18",
                ["philosophy"] = "L17-L18"
            };

            var levels = new HashSet<string>();
            foreach (var category in categories)
            {
                if (levelMapping.TryGetValue(category.ToLower(), out var level))
                {
                    levels.Add(level);
                }
            }

            return levels.Any() ? levels.ToArray() : new[] { "L0-L2" };
        }
    }
}