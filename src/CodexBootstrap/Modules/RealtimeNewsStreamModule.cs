using System;
using System.Collections.Concurrent;
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

// Mock API Router for parameterless constructor
public class MockApiRouter : IApiRouter
{
    public void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler) { }
    public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler) 
    { 
        handler = null!; 
        return false; 
    }
    public NodeRegistry GetRegistry() => new NodeRegistry();
}

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
        private readonly IApiRouter _apiRouter;
        private readonly Timer _ingestionTimer;
        private readonly Timer _cleanupTimer;
        private readonly int _ingestionIntervalMinutes = 15;
        private readonly int _cleanupIntervalHours = 24;
        private readonly int _maxItemsPerSource = 50; // Increased from 10
        private readonly ConcurrentDictionary<string, bool> _processedNewsIds = new(); // Track processed news to prevent duplicates
        private readonly SemaphoreSlim _semaphore = new(1, 1); // Semaphore for async thread safety

        // Node type constants
        private const string NEWS_SOURCE_NODE_TYPE = "codex.news.source";
        private const string NEWS_ITEM_NODE_TYPE = "codex.news.item";
        private const string FRACTAL_NEWS_NODE_TYPE = "codex.news.fractal";
        private const string NEWS_SUBSCRIPTION_NODE_TYPE = "codex.news.subscription";

        public RealtimeNewsStreamModule(NodeRegistry registry, HttpClient? httpClient = null, Core.ConfigurationManager? configManager = null, IApiRouter? apiRouter = null)
        {
            _logger = new Log4NetLogger(typeof(RealtimeNewsStreamModule));
            _registry = registry;
            _httpClient = httpClient ?? new HttpClient();
            _configManager = configManager ?? new Core.ConfigurationManager(registry, new Log4NetLogger(typeof(Core.ConfigurationManager)));
            _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter), "IApiRouter is required for AI module integration");
            
            // Set up timers
            _ingestionTimer = new Timer(async _ => await IngestNewsFromSources(null), null, TimeSpan.Zero, TimeSpan.FromMinutes(_ingestionIntervalMinutes));
            _cleanupTimer = new Timer(CleanupOldNews, null, TimeSpan.FromHours(1), TimeSpan.FromHours(_cleanupIntervalHours));
        }

        // Parameterless constructor for module loader
        public RealtimeNewsStreamModule() : this(new NodeRegistry(), new HttpClient(), new Core.ConfigurationManager(new NodeRegistry(), new Log4NetLogger(typeof(Core.ConfigurationManager))), new MockApiRouter())
        {
        }

        public string Name => "Real-Time News Stream";
        public string Description => "Ingests external news sources and transforms them through fractal analysis";
        public string Version => "1.0.0";

        public void Initialize()
        {
            _logger.Info("Initializing Real-Time News Stream Module");
            InitializeNewsSources();
        }

        public Node GetModuleNode()
        {
            return NodeStorage.CreateModuleNode(
                id: "realtime-news-stream-module",
                name: "Real-Time News Stream Module",
                version: Version,
                description: "Ingests external news sources and transforms them through fractal analysis",
                capabilities: new[] { "rss-ingestion", "api-ingestion", "fractal-analysis", "real-time-streaming" },
                tags: new[] { "news", "streaming", "realtime", "fractal" },
                specReference: "codex.spec.realtime-news-stream"
            );
        }

        public void Register(NodeRegistry registry)
        {
            _logger.Info("Registering Real-Time News Stream Module with NodeRegistry");
            Initialize();
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
            _semaphore?.Dispose();
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

        private async Task IngestNewsFromSourceAsync(NewsSource source)
        {
            try
            {
                switch (source.Type.ToLower())
                {
                    case "rss":
                        await IngestRssFeed(source);
                        break;
                    case "api":
                        await IngestApiFeed(source);
                        break;
                    case "hackernews":
                        await IngestHackerNews(source);
                        break;
                    default:
                        _logger.Warn($"Unknown source type: {source.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting from source {source.Name}: {ex.Message}", ex);
            }
        }

        private async Task IngestNewsFromSources(object? state)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.Info("Starting news ingestion from all sources");

                // Get all active news source nodes - create a copy to avoid collection modification issues
                var allSourceNodes = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE);
                var sourceNodes = allSourceNodes
                    .ToArray() // Create a copy to prevent collection modification during iteration
                    .Where(n => n.Meta?.ContainsKey("isActive") == true && (bool)n.Meta["isActive"])
                    .ToList();

                // Process sources sequentially to avoid collection modification issues
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("Collection was modified"))
            {
                _logger.Warn($"Collection modification detected during news ingestion, retrying: {ex.Message}");
                // Retry once after a short delay
                await Task.Delay(100);
                try
                {
                    var allSourceNodes = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE);
                    var sourceNodes = allSourceNodes
                        .ToArray()
                        .Where(n => n.Meta?.ContainsKey("isActive") == true && (bool)n.Meta["isActive"])
                        .ToList();
                    
                    foreach (var sourceNode in sourceNodes)
                    {
                        try
                        {
                            var source = JsonSerializer.Deserialize<NewsSource>(sourceNode.Content?.InlineJson ?? "{}");
                            if (source != null)
                            {
                                await IngestNewsFromSourceAsync(source);
                            }
                        }
                        catch (Exception innerEx)
                        {
                            _logger.Error($"Error processing source {sourceNode.Id}: {innerEx.Message}");
                        }
                    }
                }
                catch (Exception retryEx)
                {
                    _logger.Error($"Error during retry news ingestion: {retryEx.Message}", retryEx);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during news ingestion: {ex.Message}", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task IngestRssFeed(NewsSource source)
        {
            try
            {
                using var reader = XmlReader.Create(source.Url);
                var feed = SyndicationFeed.Load(reader);

                foreach (var item in feed.Items.Take(_maxItemsPerSource).ToList())
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
                // Check for duplicates (ConcurrentDictionary is thread-safe)
                if (_processedNewsIds.ContainsKey(newsItem.Id))
                {
                    _logger.Debug($"Skipping duplicate news item: {newsItem.Id}");
                    return;
                }
                
                // Mark as processed before processing
                _processedNewsIds.TryAdd(newsItem.Id, true);

                // Check if news item already exists in registry - create a copy to avoid collection modification issues
                var existingNewsNode = _registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
                    .ToArray() // Create a copy to prevent collection modification during iteration
                    .FirstOrDefault(n => n.Meta?.ContainsKey("newsId") == true && n.Meta["newsId"].ToString() == newsItem.Id);

                if (existingNewsNode != null)
                {
                    _logger.Debug($"News item already exists in registry: {newsItem.Id}");
                    return;
                }

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
                    BeliefSystemTranslation = fractalTransformation.BeliefSystemTranslation,
                    Summary = fractalTransformation.Summary,
                    ImpactAreas = fractalTransformation.ImpactAreas.ToList(),
                    AmplificationFactors = fractalTransformation.AmplificationFactors.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToList(),
                    ResonanceData = new ResonanceData(),
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
                _logger.Debug($"Attempting AI concept extraction for news item: {newsItem.Title}");

                // Call AI module via router API
                var request = new
                {
                    title = newsItem.Title,
                    content = newsItem.Content,
                    categories = newsItem.Tags ?? Array.Empty<string>(),
                    source = newsItem.Source,
                    url = newsItem.Url
                };

                var result = _apiRouter.TryGetHandler("ai", "extract-concepts", out var handler);
                if (!result)
                {
                    _logger.Warn($"AI handler 'ai.extract-concepts' not found, falling back to local extraction");
                    return await FallbackConceptExtraction(newsItem);
                }

                if (handler == null)
                {
                    _logger.Warn($"AI handler 'ai.extract-concepts' is null, falling back to local extraction");
                    return await FallbackConceptExtraction(newsItem);
                }

                _logger.Debug($"Calling AI module for concept extraction: {newsItem.Title}");
                var response = await handler(JsonSerializer.SerializeToElement(request));
                
                if (response == null)
                {
                    _logger.Warn($"AI module returned null response for concept extraction: {newsItem.Title}");
                    return await FallbackConceptExtraction(newsItem);
                }

                if (response is JsonElement jsonResponse)
                {
                    if (jsonResponse.TryGetProperty("success", out var success) && success.GetBoolean())
                    {
                        if (jsonResponse.TryGetProperty("concepts", out var concepts) && 
                            jsonResponse.TryGetProperty("confidence", out var confidence) &&
                            jsonResponse.TryGetProperty("ontologyLevels", out var ontologyLevels))
                        {
                            _logger.Info($"AI concept extraction successful for: {newsItem.Title} (confidence: {confidence.GetDouble()})");
                            
                            // Create ConceptAnalysis from response
                            return new ConceptAnalysis
                            {
                                Id = $"concept-{Guid.NewGuid():N}",
                                NewsItemId = newsItem.Id,
                                Concepts = concepts.EnumerateArray().Select(c => c.GetString() ?? "").ToList(),
                                Confidence = confidence.GetDouble(),
                                OntologyLevels = ontologyLevels.EnumerateArray().Select(o => o.GetString() ?? "").ToList(),
                                ExtractedAt = DateTimeOffset.UtcNow,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["source"] = "ai-module",
                                    ["originalTitle"] = newsItem.Title,
                                    ["processingTime"] = DateTimeOffset.UtcNow
                                }
                            };
                        }
                        else
                        {
                            _logger.Warn($"AI module response missing required fields for concept extraction: {newsItem.Title}");
                        }
                    }
                    else
                    {
                        _logger.Warn($"AI module returned error for concept extraction: {newsItem.Title}");
                        if (jsonResponse.TryGetProperty("error", out var error))
                        {
                            _logger.Warn($"AI module error: {error.GetString()}");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"AI module returned unexpected response type for concept extraction: {newsItem.Title}");
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON serialization/deserialization error in AI concept extraction for {newsItem.Title}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error calling AI module for concept extraction {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local concept extraction for: {newsItem.Title}");
            return await FallbackConceptExtraction(newsItem);
        }

        private async Task<ScoringAnalysis> PerformAIScoringAnalysis(ConceptAnalysis conceptAnalysis, NewsItem newsItem)
        {
            try
            {
                _logger.Debug($"Attempting AI scoring analysis for news item: {newsItem.Title}");

                // Call AI module via router API
                var request = new
                {
                    conceptAnalysis = conceptAnalysis,
                    content = new
                    {
                        title = newsItem.Title,
                        content = newsItem.Content,
                        categories = newsItem.Tags ?? Array.Empty<string>(),
                        source = newsItem.Source,
                        url = newsItem.Url
                    }
                };

                var result = _apiRouter.TryGetHandler("ai", "score-analysis", out var handler);
                if (!result)
                {
                    _logger.Warn($"AI handler 'ai.score-analysis' not found, falling back to local analysis");
                    return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
                }

                if (handler == null)
                {
                    _logger.Warn($"AI handler 'ai.score-analysis' is null, falling back to local analysis");
                    return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
                }

                _logger.Debug($"Calling AI module for scoring analysis: {newsItem.Title}");
                var response = await handler(JsonSerializer.SerializeToElement(request));
                
                if (response == null)
                {
                    _logger.Warn($"AI module returned null response for scoring analysis: {newsItem.Title}");
                    return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
                }

                if (response is JsonElement jsonResponse)
                {
                    if (jsonResponse.TryGetProperty("success", out var success) && success.GetBoolean())
                    {
                        if (jsonResponse.TryGetProperty("abundanceScore", out var abundanceScore) &&
                            jsonResponse.TryGetProperty("consciousnessScore", out var consciousnessScore) &&
                            jsonResponse.TryGetProperty("unityScore", out var unityScore) &&
                            jsonResponse.TryGetProperty("overallScore", out var overallScore))
                        {
                            _logger.Info($"AI scoring analysis successful for: {newsItem.Title} (overall: {overallScore.GetDouble()})");
                            
                            // Create ScoringAnalysis from response
                            return new ScoringAnalysis
                            {
                                Id = $"scoring-{Guid.NewGuid():N}",
                                NewsItemId = newsItem.Id,
                                ConceptAnalysisId = conceptAnalysis.Id,
                                AbundanceScore = abundanceScore.GetDouble(),
                                ConsciousnessScore = consciousnessScore.GetDouble(),
                                UnityScore = unityScore.GetDouble(),
                                OverallScore = overallScore.GetDouble(),
                                ScoredAt = DateTimeOffset.UtcNow,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["source"] = "ai-module",
                                    ["originalTitle"] = newsItem.Title,
                                    ["processingTime"] = DateTimeOffset.UtcNow
                                }
                            };
                        }
                        else
                        {
                            _logger.Warn($"AI module response missing required fields for scoring analysis: {newsItem.Title}");
                        }
                    }
                    else
                    {
                        _logger.Warn($"AI module returned error for scoring analysis: {newsItem.Title}");
                        if (jsonResponse.TryGetProperty("error", out var error))
                        {
                            _logger.Warn($"AI module error: {error.GetString()}");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"AI module returned unexpected response type for scoring analysis: {newsItem.Title}");
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON serialization/deserialization error in AI scoring analysis for {newsItem.Title}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error calling AI module for scoring analysis {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local scoring analysis for: {newsItem.Title}");
            return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
        }

        private async Task<FractalTransformation> PerformAIFractalTransformation(NewsItem newsItem, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            try
            {
                _logger.Debug($"Attempting AI fractal transformation for news item: {newsItem.Title}");

                // Call AI module via router API
                var request = new
                {
                    content = new
                    {
                        title = newsItem.Title,
                        content = newsItem.Content,
                        categories = newsItem.Tags ?? Array.Empty<string>(),
                        source = newsItem.Source,
                        url = newsItem.Url
                    },
                    conceptAnalysis = conceptAnalysis,
                    scoringAnalysis = scoringAnalysis
                };

                var result = _apiRouter.TryGetHandler("ai", "fractal-transform", out var handler);
                if (!result)
                {
                    _logger.Warn($"AI handler 'ai.fractal-transform' not found, falling back to local transformation");
                    return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
                }

                if (handler == null)
                {
                    _logger.Warn($"AI handler 'ai.fractal-transform' is null, falling back to local transformation");
                    return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
                }

                _logger.Debug($"Calling AI module for fractal transformation: {newsItem.Title}");
                var response = await handler(JsonSerializer.SerializeToElement(request));
                
                if (response == null)
                {
                    _logger.Warn($"AI module returned null response for fractal transformation: {newsItem.Title}");
                    return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
                }

                if (response is JsonElement jsonResponse)
                {
                    if (jsonResponse.TryGetProperty("success", out var success) && success.GetBoolean())
                    {
                        if (jsonResponse.TryGetProperty("headline", out var headline) &&
                            jsonResponse.TryGetProperty("beliefTranslation", out var beliefTranslation) &&
                            jsonResponse.TryGetProperty("summary", out var summary) &&
                            jsonResponse.TryGetProperty("impactAreas", out var impactAreas))
                        {
                            _logger.Info($"AI fractal transformation successful for: {newsItem.Title}");
                            
                            // Create FractalTransformation from response
                            return new FractalTransformation
                            {
                                Id = $"fractal-{Guid.NewGuid():N}",
                                NewsItemId = newsItem.Id,
                                Headline = headline.GetString() ?? newsItem.Title,
                                BeliefSystemTranslation = beliefTranslation.GetString() ?? newsItem.Content,
                                Summary = summary.GetString() ?? newsItem.Content,
                                ImpactAreas = impactAreas.EnumerateArray().Select(a => a.GetString() ?? "").ToArray(),
                                AmplificationFactors = new Dictionary<string, double> { { "resonance", 0.8 } },
                                ResonanceData = new Dictionary<string, object>(),
                                TransformationType = "consciousness-expansion",
                                ConsciousnessLevel = "L5",
                                TransformedAt = DateTimeOffset.UtcNow,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["source"] = "ai-module",
                                    ["originalTitle"] = newsItem.Title,
                                    ["processingTime"] = DateTimeOffset.UtcNow
                                }
                            };
                        }
                        else
                        {
                            _logger.Warn($"AI module response missing required fields for fractal transformation: {newsItem.Title}");
                        }
                    }
                    else
                    {
                        _logger.Warn($"AI module returned error for fractal transformation: {newsItem.Title}");
                        if (jsonResponse.TryGetProperty("error", out var error))
                        {
                            _logger.Warn($"AI module error: {error.GetString()}");
                        }
                    }
                }
                else
                {
                    _logger.Warn($"AI module returned unexpected response type for fractal transformation: {newsItem.Title}");
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON serialization/deserialization error in AI fractal transformation for {newsItem.Title}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error calling AI module for fractal transformation {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local fractal transformation for: {newsItem.Title}");
            return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
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
                    .ToArray() // Create a copy to prevent collection modification during iteration
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
                var sourceNodes = _registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE).ToArray(); // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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
                    .ToArray() // Create a copy to prevent collection modification
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

        // Fallback methods for when AI module is not available
        private async Task<ConceptAnalysis> FallbackConceptExtraction(NewsItem newsItem)
        {
            _logger.Info($"Using fallback concept extraction for: {newsItem.Title}");
            
            var concepts = ExtractTagsFromContent(newsItem.Content).ToList();
            var tags = newsItem.Tags?.ToList() ?? new List<string>();
            var entities = ExtractEntitiesFromContent(newsItem.Content);
            var themes = ExtractThemesFromContent(newsItem.Content);
            
            return new ConceptAnalysis
            {
                Id = $"concept-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                Concepts = concepts,
                Confidence = 0.7,
                OntologyLevels = OntologyLevelHelper.DetermineOntologyLevelsFromCategories(tags).ToList(),
                ExtractedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["extractionMethod"] = "fallback",
                    ["source"] = "local",
                    ["processedAt"] = DateTimeOffset.UtcNow,
                    ["tags"] = tags,
                    ["entities"] = entities,
                    ["themes"] = themes
                }
            };
        }

        private async Task<ScoringAnalysis> FallbackScoringAnalysis(ConceptAnalysis conceptAnalysis, NewsItem newsItem)
        {
            _logger.Info($"Using fallback scoring analysis for: {newsItem.Title}");
            
            var abundanceScore = CalculateAbundanceScore(conceptAnalysis.Concepts, newsItem);
            var consciousnessScore = CalculateConsciousnessScore(conceptAnalysis.Concepts, newsItem);
            var unityScore = CalculateUnityScore(conceptAnalysis.Concepts, newsItem);
            var innovationScore = CalculateInnovationScore(conceptAnalysis.Concepts, newsItem);
            var impactScore = CalculateImpactScore(conceptAnalysis.Concepts, newsItem);
            
            return new ScoringAnalysis
            {
                Id = $"scoring-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                ConceptAnalysisId = conceptAnalysis.Id,
                AbundanceScore = abundanceScore,
                ConsciousnessScore = consciousnessScore,
                UnityScore = unityScore,
                OverallScore = (abundanceScore + consciousnessScore + unityScore + innovationScore + impactScore) / 5.0,
                ScoredAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["scoringMethod"] = "fallback",
                    ["source"] = "local",
                    ["processedAt"] = DateTimeOffset.UtcNow
                }
            };
        }

        private async Task<FractalTransformation> FallbackFractalTransformation(NewsItem newsItem, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            _logger.Info($"Using fallback fractal transformation for: {newsItem.Title}");
            
            return new FractalTransformation
            {
                Id = $"fractal-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                Headline = TransformHeadline(newsItem.Title, conceptAnalysis.Concepts),
                BeliefSystemTranslation = TransformToBeliefSystem(newsItem.Content, conceptAnalysis.Concepts),
                Summary = TransformSummary(newsItem.Content, conceptAnalysis.Concepts),
                ImpactAreas = DetermineImpactAreas(conceptAnalysis.Concepts),
                AmplificationFactors = CalculateAmplificationFactors(scoringAnalysis),
                ResonanceData = new Dictionary<string, object>
                {
                    ["resonanceScore"] = scoringAnalysis.OverallScore,
                    ["consciousnessLevel"] = DetermineConsciousnessLevel(scoringAnalysis.OverallScore),
                    ["transformationType"] = "consciousness-expansion"
                },
                TransformationType = "consciousness-expansion",
                ConsciousnessLevel = DetermineConsciousnessLevel(scoringAnalysis.OverallScore),
                TransformedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["transformationMethod"] = "fallback",
                    ["source"] = "local",
                    ["processedAt"] = DateTimeOffset.UtcNow
                }
            };
        }

        // Helper methods for fallback implementations
        private List<string> ExtractEntitiesFromContent(string content)
        {
            // Simple entity extraction - look for capitalized words
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Where(w => w.Length > 2 && char.IsUpper(w[0]) && w.All(c => char.IsLetter(c)))
                       .Distinct()
                       .Take(10)
                       .ToList();
        }

        private List<string> ExtractThemesFromContent(string content)
        {
            var themes = new List<string>();
            var lowerContent = content.ToLower();
            
            var themeKeywords = new Dictionary<string, string[]>
            {
                ["technology"] = new[] { "tech", "digital", "ai", "software", "computer", "internet" },
                ["science"] = new[] { "research", "study", "discovery", "experiment", "data" },
                ["consciousness"] = new[] { "mind", "awareness", "consciousness", "mindfulness", "meditation" },
                ["unity"] = new[] { "together", "collaboration", "community", "global", "united" },
                ["abundance"] = new[] { "growth", "prosperity", "wealth", "success", "opportunity" }
            };

            foreach (var theme in themeKeywords)
            {
                if (theme.Value.Any(keyword => lowerContent.Contains(keyword)))
                {
                    themes.Add(theme.Key);
                }
            }

            return themes;
        }

        private string TransformHeadline(string originalTitle, List<string> concepts)
        {
            // Simple headline transformation based on concepts
            if (concepts.Contains("consciousness"))
                return $"Consciousness Expansion: {originalTitle}";
            if (concepts.Contains("unity"))
                return $"Unity in Action: {originalTitle}";
            if (concepts.Contains("abundance"))
                return $"Abundance Through: {originalTitle}";
            
            return $"Transformative Insight: {originalTitle}";
        }

        private string TransformToBeliefSystem(string content, List<string> concepts)
        {
            // Simple belief system translation
            var beliefPrefix = "From a consciousness-expanded perspective: ";
            return beliefPrefix + content;
        }

        private string TransformSummary(string content, List<string> concepts)
        {
            // Simple summary transformation
            var summary = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
            return $"This transformative insight reveals: {summary}";
        }

        private string[] DetermineImpactAreas(List<string> concepts)
        {
            var impactAreas = new List<string>();
            
            if (concepts.Contains("consciousness")) impactAreas.Add("consciousness");
            if (concepts.Contains("unity")) impactAreas.Add("unity");
            if (concepts.Contains("abundance")) impactAreas.Add("abundance");
            if (concepts.Contains("technology")) impactAreas.Add("technology");
            if (concepts.Contains("science")) impactAreas.Add("science");
            
            return impactAreas.Count > 0 ? impactAreas.ToArray() : new[] { "consciousness" };
        }

        private Dictionary<string, double> CalculateAmplificationFactors(ScoringAnalysis scoring)
        {
            return new Dictionary<string, double>
            {
                ["resonance"] = scoring.OverallScore,
                ["consciousness"] = scoring.ConsciousnessScore,
                ["unity"] = scoring.UnityScore,
                ["abundance"] = scoring.AbundanceScore
            };
        }

        private string DetermineConsciousnessLevel(double overallScore)
        {
            if (overallScore >= 0.8) return "L5";
            if (overallScore >= 0.6) return "L4";
            if (overallScore >= 0.4) return "L3";
            if (overallScore >= 0.2) return "L2";
            return "L1";
        }

        private double CalculateInnovationScore(List<string> concepts, NewsItem newsItem)
        {
            var innovationKeywords = new[] { "innovation", "breakthrough", "discovery", "new", "revolutionary", "cutting-edge" };
            var innovationCount = concepts.Count(c => innovationKeywords.Contains(c));
            return Math.Min(1.0, innovationCount / 2.0);
        }

        private double CalculateImpactScore(List<string> concepts, NewsItem newsItem)
        {
            var impactKeywords = new[] { "impact", "change", "transformation", "breakthrough", "revolutionary", "significant" };
            var impactCount = concepts.Count(c => impactKeywords.Contains(c));
            return Math.Min(1.0, impactCount / 2.0);
        }

        private double CalculateConsciousnessScore(List<string> concepts, NewsItem newsItem)
        {
            var consciousnessKeywords = new[] { "consciousness", "awareness", "mindfulness", "meditation", "wisdom", "enlightenment", "spiritual", "transcendence" };
            var consciousnessCount = concepts.Count(c => consciousnessKeywords.Contains(c));
            return Math.Min(1.0, consciousnessCount / 3.0);
        }

        private double CalculateUnityScore(List<string> concepts, NewsItem newsItem)
        {
            var unityKeywords = new[] { "unity", "collaboration", "collective", "together", "community", "global", "international", "cooperation" };
            var unityCount = concepts.Count(c => unityKeywords.Contains(c));
            return Math.Min(1.0, unityCount / 3.0);
        }
    }

    // Data Transfer Objects
    [MetaNodeAttribute("codex.news.news-source", "codex.meta/type", "NewsSource", "News source configuration")]
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

    [MetaNodeAttribute("codex.news.news-item", "codex.meta/type", "NewsItem", "News item data structure")]
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

    // AI Analysis Data Structures are now defined in AIModule.cs

    [MetaNodeAttribute("codex.news.fractal-news-item", "codex.meta/type", "FractalNewsItem", "Fractal analysis of news item")]
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

    [MetaNodeAttribute("codex.news.resonance-data", "codex.meta/type", "ResonanceData", "Resonance analysis data")]
    public class ResonanceData
    {
        public double ResonanceScore { get; set; }
        public double AmplificationPotential { get; set; }
        public string CollectiveImpact { get; set; } = "";
        public List<string> ResonanceFactors { get; set; } = new();
    }

    [MetaNodeAttribute("codex.news.news-subscription", "codex.meta/type", "NewsSubscription", "News subscription configuration")]
    public class NewsSubscription
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string[] InterestAreas { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    [RequestType("codex.news.news-subscription-request", "NewsSubscriptionRequest", "Request for news subscription")]
    public class NewsSubscriptionRequest
    {
        public string UserId { get; set; } = "";
        public string[] InterestAreas { get; set; } = Array.Empty<string>();
    }

    [MetaNodeAttribute("codex.news.hacker-news-story", "codex.meta/type", "HackerNewsStory", "Hacker News story data")]
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