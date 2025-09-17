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
using System.Security.Cryptography;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using Microsoft.AspNetCore.Builder;

namespace CodexBootstrap.Modules
{
    // Mock API Router for parameterless constructor
    public class MockApiRouter : IApiRouter
    {
        public void Register(string moduleId, string api, Func<JsonElement?, Task<object>> handler) { }
        public bool TryGetHandler(string moduleId, string api, out Func<JsonElement?, Task<object>> handler) 
        { 
            handler = null!; 
            return false; 
        }
        public INodeRegistry GetRegistry() => throw new NotImplementedException("MockApiRouter should not be used in production");
    }
    /// <summary>
    /// Real-time fractal news streaming module that ingests external news sources
    /// and transforms them through fractal analysis aligned with belief systems
    /// </summary>
    /// <remarks>
    /// Requires NEWS_INGESTION_ENABLED and API keys; AI enrichment falls back to logs when the AI module handlers are unavailable.
    /// </remarks>
    public class RealtimeNewsStreamModule : ModuleBase
    {
        private readonly HttpClient _httpClient;
        private readonly Core.ConfigurationManager _configManager;
        private readonly IApiRouter _apiRouter;

        public override string Name => "Realtime News Stream Module";
        public override string Description => "Real-time fractal news streaming module that ingests external news sources and transforms them through fractal analysis aligned with belief systems";
        public override string Version => "1.0.0";
        private readonly Timer _ingestionTimer;
        private readonly Timer _cleanupTimer;
        private CrossModuleCommunicator? _moduleCommunicator;
        private readonly AIModuleTemplates _aiTemplates;
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

        /// <summary>
        /// Gets the registry to use - now always the unified registry
        /// </summary>
        private INodeRegistry Registry => _registry;

        // Lazy-loaded cross-module communicator
        private CrossModuleCommunicator ModuleCommunicator => _moduleCommunicator ??= new CrossModuleCommunicator(_logger);

        // AI Module call templates
        private class AIModuleTemplates
        {
            private readonly IApiRouter _apiRouter;
            private readonly Core.ICodexLogger _logger;

            public AIModuleTemplates(IApiRouter apiRouter, Core.ICodexLogger logger)
            {
                _apiRouter = apiRouter;
                _logger = logger;
            }

            public async Task<ConceptExtractionResult?> ExtractConceptsAsync(
                string content, 
                int maxConcepts = 5)
            {
                try
                {
                    var request = new
                    {
                        content = content,
                        maxConcepts = maxConcepts
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "extract-concepts", out var handler))
                    {
                        var result = await handler(requestElement);
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (resultElement.TryGetProperty("data", out var data))
                                {
                                    return JsonSerializer.Deserialize<ConceptExtractionResult>(data.GetRawText());
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AI module extract-concepts handler not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling AI module extract-concepts: {ex.Message}", ex);
                }
                return null;
            }

            public async Task<ScoringAnalysisResult?> AnalyzeScoringAsync(
                string content, 
                string analysisType = "relevance")
            {
                try
                {
                    var request = new
                    {
                        content = content,
                        analysisType = analysisType,
                        criteria = new[] { "relevance", "quality", "impact" }
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "score-analysis", out var handler))
                    {
                        var result = await handler(requestElement);
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (resultElement.TryGetProperty("data", out var data))
                                {
                                    return JsonSerializer.Deserialize<ScoringAnalysisResult>(data.GetRawText());
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AI module score-analysis handler not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling AI module score-analysis: {ex.Message}", ex);
                }
                return null;
            }

            public async Task<FractalTransformResult?> TransformFractalAsync(
                string content)
            {
                try
                {
                    var request = new
                    {
                        content = content
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "fractal-transform", out var handler))
                    {
                        var result = await handler(requestElement);
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (resultElement.TryGetProperty("data", out var data))
                                {
                                    return JsonSerializer.Deserialize<FractalTransformResult>(data.GetRawText());
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AI module fractal-transform handler not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling AI module fractal-transform: {ex.Message}", ex);
                }
                return null;
            }
        }

        // AI Module response types
        private class ConceptExtractionResult
        {
            public List<string> Concepts { get; set; } = new();
            public double Confidence { get; set; }
            public List<string> OntologyLevels { get; set; } = new();
        }

        private class ScoringAnalysisResult
        {
            public double AbundanceScore { get; set; }
            public double ConsciousnessScore { get; set; }
            public double UnityScore { get; set; }
            public double OverallScore { get; set; }
        }

        private class FractalTransformResult
        {
            public string Headline { get; set; } = "";
            public string BeliefTranslation { get; set; } = "";
            public string Summary { get; set; } = "";
            public List<string> ImpactAreas { get; set; } = new();
        }

        // Ontology axis loaded from U-CORE
        private record OntologyAxis(string Name, List<string> Keywords);
        private List<OntologyAxis> _ontologyAxes = new();

        public RealtimeNewsStreamModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
            : base(registry, logger)
        {
            _httpClient = httpClient;
            _configManager = new Core.ConfigurationManager(_registry, logger);
            _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter), "ApiRouter must be provided - MockApiRouter fallback removed");
            _aiTemplates = new AIModuleTemplates(_apiRouter, _logger);
            
            // Cross-module communicator will be initialized lazily
            
            // Initialize timers but don't start them yet - they will be started in StartAsync
            _ingestionTimer = new Timer(async _ => await IngestNewsFromSources(null), null, Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer = new Timer(CleanupOldNews, null, Timeout.Infinite, Timeout.Infinite);
        }

        // Parameterless constructor for module loader

        public void Initialize()
        {
            _logger.Info("Initializing Real-Time News Stream Module");
            LoadOntologyAxes();
            if (_ontologyAxes.Count == 0)
            {
                _logger.Warn("No ontology axes found in U-CORE. Seeding minimal default axes.");
                SeedDefaultOntologyAxes();
                LoadOntologyAxes();
            }
            InitializeNewsSources();
        }

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "realtime-news-stream-module",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "news", "streaming", "realtime", "fractal" },
                capabilities: new[] { "rss-ingestion", "api-ingestion", "fractal-analysis", "real-time-streaming" },
                spec: "codex.spec.realtime-news-stream"
            );
        }

        public override void Register(INodeRegistry registry)
        {
            base.Register(registry);
            
            Initialize();
            
            // Gate ingestion via environment flag (default: enabled)
            var enabledEnv = Environment.GetEnvironmentVariable("NEWS_INGESTION_ENABLED");
            var ingestionEnabled = string.IsNullOrWhiteSpace(enabledEnv) || enabledEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || enabledEnv == "1";

            if (ingestionEnabled)
            {
                // Start the timers when module is registered
                _ingestionTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(_ingestionIntervalMinutes));
                _cleanupTimer.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(_cleanupIntervalHours));
                
                // Start initial news ingestion
                _ = Task.Run(async () => await IngestNewsFromSources(null));
                _logger.Info("RealtimeNewsStreamModule ingestion enabled (NEWS_INGESTION_ENABLED=true)");
            }
            else
            {
                _logger.Warn("RealtimeNewsStreamModule ingestion disabled via NEWS_INGESTION_ENABLED=false");
            }
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            _logger.Info("Registering API handlers for Real-Time News Stream Module");
            // API handlers are registered via ApiRoute attributes
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            _logger.Info("Registering HTTP endpoints for Real-Time News Stream Module");
            // HTTP endpoints are registered via ApiRoute attributes
        }

        public void Unregister()
        {
            _logger.Info("Unregistering Real-Time News Stream Module");
            
            // Stop the timers
            _ingestionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Wait for any running operations to complete
            _semaphore?.Wait(TimeSpan.FromSeconds(5));
            try
            {
                _logger.Info("Real-Time News Stream Module unregistered gracefully");
            }
            finally
            {
                _semaphore?.Release();
            }
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
                    Categories = Array.Empty<string>(),
                    IsActive = true,
                    UpdateIntervalMinutes = 30
                },
                new NewsSource
                {
                    Id = "hackernews",
                    Name = "Hacker News",
                    Type = "api",
                    Url = "https://hacker-news.firebaseio.com/v0/topstories.json",
                    Categories = Array.Empty<string>(),
                    IsActive = true,
                    UpdateIntervalMinutes = 30
                },
                new NewsSource
                {
                    Id = "arstechnica",
                    Name = "Ars Technica",
                    Type = "rss",
                    Url = "https://feeds.arstechnica.com/arstechnica/index/",
                    Categories = Array.Empty<string>(),
                    IsActive = true,
                    UpdateIntervalMinutes = 25
                },
                new NewsSource
                {
                    Id = "techcrunch",
                    Name = "TechCrunch",
                    Type = "rss",
                    Url = "https://techcrunch.com/feed/",
                    Categories = Array.Empty<string>(),
                    IsActive = true,
                    UpdateIntervalMinutes = 15
                },
                new NewsSource
                {
                    Id = "hacker-news",
                    Name = "Hacker News",
                    Type = "rss",
                    Url = "https://hnrss.org/frontpage",
                    Categories = Array.Empty<string>(),
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

                Registry.Upsert(sourceNode);
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

                var allSourceNodes = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE);
                var sourceNodes = allSourceNodes
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
            const int maxRetries = 3;
            const int baseDelayMs = 1000;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Create HttpClient with proper headers to avoid bot detection
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Living-Codex-NewsBot/1.0 (https://living-codex.com)");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/xml, text/xml");
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    // Get the RSS content with proper error handling
                    var response = await httpClient.GetAsync(source.Url);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            _logger.Warn($"RSS feed {source.Name} is blocked (403). This may be due to rate limiting or bot detection. Skipping this source.");
                            return;
                        }
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.Warn($"RSS feed {source.Name} rate limited (429). Attempt {attempt + 1}/{maxRetries}");
                            if (attempt < maxRetries - 1)
                            {
                                await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
                                continue;
                            }
                        }
                        
                        _logger.Warn($"RSS feed {source.Name} returned status {response.StatusCode}. Skipping this source.");
                        return;
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    using var reader = XmlReader.Create(new StringReader(content));
                    var feed = SyndicationFeed.Load(reader);

                foreach (var item in feed.Items.Take(_maxItemsPerSource).ToList())
                {
                    var deterministicIdSeed = !string.IsNullOrEmpty(item.Id)
                        ? item.Id
                        : (item.Links.FirstOrDefault()?.Uri?.ToString() ?? (item.Title?.Text ?? ""));
                    var stableId = ComputeDeterministicId($"rss:{source.Id}:{deterministicIdSeed}");

                    var newsItem = new NewsItem
                    {
                        Id = $"rss-{source.Id}-{stableId}",
                        Title = item.Title?.Text ?? "",
                        Content = item.Summary?.Text ?? "",
                        Source = source.Name,
                        Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                        PublishedAt = item.PublishDate,
                        Tags = ExtractTagsFromContent(item.Title?.Text + " " + item.Summary?.Text),
                        Metadata = new Dictionary<string, object>
                        {
                            ["sourceType"] = "RSS",
                            ["sourceId"] = source.Id,
                            ["rssId"] = item.Id
                        }
                    };

                    await ProcessNewsItem(newsItem);
                }
                
                // Success - break out of retry loop
                break;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error ingesting RSS feed {source.Name} (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                    
                    if (attempt == maxRetries - 1)
                    {
                        _logger.Error($"Failed to ingest RSS feed {source.Name} after {maxRetries} attempts: {ex.Message}", ex);
                        return;
                    }
                    
                    // Wait before retry with exponential backoff
                    await Task.Delay(baseDelayMs * (int)Math.Pow(2, attempt));
                }
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
                _logger.Info($"Processing news item: {newsItem.Id} - {newsItem.Title}");
                
                // Check for duplicates (ConcurrentDictionary is thread-safe)
                if (_processedNewsIds.ContainsKey(newsItem.Id))
                {
                    _logger.Debug($"Skipping duplicate news item: {newsItem.Id}");
                    return;
                }
                
                // Mark as processed before processing
                _processedNewsIds.TryAdd(newsItem.Id, true);

                // Check if news item already exists in registry - create a copy to avoid collection modification issues
                var existingNewsNode = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
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

                Registry.Upsert(newsNode);
                _logger.Info($"Successfully stored news item as node: {newsNode.Id} - {newsItem.Title}");

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

                    Registry.Upsert(fractalNode);

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

                var content = newsItem.Title + " " + newsItem.Content;
                var result = await _aiTemplates.ExtractConceptsAsync(content, 5);
                
                if (result != null)
                {
                    _logger.Info($"AI concept extraction successful for: {newsItem.Title} (confidence: {result.Confidence})");
                    
                    return new ConceptAnalysis
                    {
                        Id = $"concept-{Guid.NewGuid():N}",
                        NewsItemId = newsItem.Id,
                        Concepts = result.Concepts,
                        Confidence = result.Confidence,
                        OntologyLevels = result.OntologyLevels,
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
                    _logger.Warn($"AI module returned null result for concept extraction: {newsItem.Title}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI concept extraction for {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local concept extraction for: {newsItem.Title}");
            return await FallbackConceptExtraction(newsItem);
        }

        private async Task<ScoringAnalysis> PerformAIScoringAnalysis(ConceptAnalysis conceptAnalysis, NewsItem newsItem)
        {
            try
            {
                _logger.Debug($"Attempting AI scoring analysis for news item: {newsItem.Title}");

                var content = newsItem.Title + " " + newsItem.Content;
                var result = await _aiTemplates.AnalyzeScoringAsync(content, "relevance");
                
                if (result != null)
                {
                    _logger.Info($"AI scoring analysis successful for: {newsItem.Title} (overall: {result.OverallScore})");
                    
                    return new ScoringAnalysis
                    {
                        Id = $"scoring-{Guid.NewGuid():N}",
                        NewsItemId = newsItem.Id,
                        ConceptAnalysisId = conceptAnalysis.Id,
                        AbundanceScore = result.AbundanceScore,
                        ConsciousnessScore = result.ConsciousnessScore,
                        UnityScore = result.UnityScore,
                        OverallScore = result.OverallScore,
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
                    _logger.Warn($"AI module returned null result for scoring analysis: {newsItem.Title}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI scoring analysis for {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local scoring analysis for: {newsItem.Title}");
            return await FallbackScoringAnalysis(conceptAnalysis, newsItem);
        }

        private async Task<FractalTransformation> PerformAIFractalTransformation(NewsItem newsItem, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            try
            {
                _logger.Debug($"Attempting AI fractal transformation for news item: {newsItem.Title}");

                var content = newsItem.Title + " " + newsItem.Content;
                var result = await _aiTemplates.TransformFractalAsync(content);
                
                if (result != null)
                {
                    _logger.Info($"AI fractal transformation successful for: {newsItem.Title}");
                    
                    return new FractalTransformation
                    {
                        Id = $"fractal-{Guid.NewGuid():N}",
                        NewsItemId = newsItem.Id,
                        Headline = result.Headline,
                        BeliefSystemTranslation = result.BeliefTranslation,
                        Summary = result.Summary,
                        ImpactAreas = result.ImpactAreas.ToArray(),
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
                    _logger.Warn($"AI module returned null result for fractal transformation: {newsItem.Title}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI fractal transformation for {newsItem.Title}: {ex.Message}", ex);
            }

            _logger.Info($"Falling back to local fractal transformation for: {newsItem.Title}");
            return await FallbackFractalTransformation(newsItem, conceptAnalysis, scoringAnalysis);
        }

        private double CalculateAbundanceScore(List<string> concepts, NewsItem newsItem)
        {
            var abundanceAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("abundance", StringComparison.OrdinalIgnoreCase));
            var abundanceKeywords = abundanceAxis?.Keywords ?? new List<string>();
            var abundanceCount = concepts.Count(c => abundanceKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            var totalConcepts = Math.Max(concepts.Count, 1);
            
            return Math.Min(abundanceCount / (double)totalConcepts * 2.0, 1.0);
        }

        private double CalculateResonanceScore(List<string> concepts)
        {
            var resonanceAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("resonance", StringComparison.OrdinalIgnoreCase));
            var resonanceKeywords = resonanceAxis?.Keywords ?? new List<string>();
            var resonanceCount = concepts.Count(c => resonanceKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            var totalConcepts = Math.Max(concepts.Count, 1);
            
            return Math.Min(resonanceCount / (double)totalConcepts * 1.5, 1.0);
        }

        private string CreateBeliefSystemTranslation(NewsItem newsItem, List<string> concepts)
        {
            var abundanceAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("abundance", StringComparison.OrdinalIgnoreCase));
            var abundanceKeywords = concepts.Where(c => (abundanceAxis?.Keywords ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            
            if (abundanceKeywords.Any())
            {
                return $"This development represents a significant step forward in our collective journey toward abundance. The concepts of {string.Join(", ", abundanceKeywords)} align with our core belief in amplifying individual contributions through collective resonance.";
            }
            
            return $"This news item has the potential to contribute to collective understanding and growth, offering opportunities for amplification and abundance through thoughtful engagement.";
        }

        private string CreateAbundanceHeadline(NewsItem newsItem, List<string> concepts)
        {
            var abundanceAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("abundance", StringComparison.OrdinalIgnoreCase));
            var abundanceKeywords = concepts.Where(c => (abundanceAxis?.Keywords ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            
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
            var unityAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("unity", StringComparison.OrdinalIgnoreCase));
            if (concepts.Any(c => (unityAxis?.Keywords ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase)))
                factors.Add("Community Engagement");
            var innovationAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("innovation", StringComparison.OrdinalIgnoreCase));
            if (concepts.Any(c => (innovationAxis?.Keywords ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase)))
                factors.Add("Innovation Catalyst");
            var scienceAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("science", StringComparison.OrdinalIgnoreCase));
            if (concepts.Any(c => (scienceAxis?.Keywords ?? new List<string>()).Contains(c, StringComparer.OrdinalIgnoreCase)))
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
                var oldNewsNodes = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
                    .ToArray() // Create a copy to prevent collection modification during iteration
                    .Where(n => n.Meta?.ContainsKey("publishedAt") == true && 
                               DateTime.TryParse(n.Meta["publishedAt"].ToString(), out var publishedAt) && 
                               publishedAt < cutoffDate.DateTime)
                    .ToList();

                foreach (var node in oldNewsNodes)
                {
                    Registry.RemoveNode(node.Id);
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
                var sourceNodes = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE).ToArray(); // Create a copy to prevent collection modification
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

                Registry.Upsert(subscriptionNode);

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
                var subscriptionNodes = Registry.GetNodesByType(NEWS_SUBSCRIPTION_NODE_TYPE)
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
                var fractalNodes = Registry.GetNodesByType(FRACTAL_NEWS_NODE_TYPE)
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
                var fractalNode = Registry.GetNodesByType(FRACTAL_NEWS_NODE_TYPE)
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
                var subscriptionNode = Registry.GetNodesByType(NEWS_SUBSCRIPTION_NODE_TYPE)
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

                Registry.RemoveNode(subscriptionNode.Id);

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
                var existingSource = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
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

                Registry.Upsert(updatedSourceNode);

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
                var sourceNode = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
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

                Registry.RemoveNode(sourceNode.Id);

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
                var sourceNode = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
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

                Registry.Upsert(updatedSourceNode);

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
                var sourceNode = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE)
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
            
            foreach (var axis in _ontologyAxes)
            {
                if (axis.Keywords.Any(keyword => lowerContent.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    themes.Add(axis.Name);
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
            
            foreach (var axis in _ontologyAxes)
            {
                if (concepts.Any(c => axis.Keywords.Contains(c, StringComparer.OrdinalIgnoreCase)))
                {
                    impactAreas.Add(axis.Name);
                }
            }
            
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
            var innovationAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("innovation", StringComparison.OrdinalIgnoreCase));
            var innovationKeywords = innovationAxis?.Keywords ?? new List<string>();
            var innovationCount = concepts.Count(c => innovationKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            return Math.Min(1.0, innovationCount / 2.0);
        }

        private double CalculateImpactScore(List<string> concepts, NewsItem newsItem)
        {
            var impactAxis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("impact", StringComparison.OrdinalIgnoreCase));
            var impactKeywords = impactAxis?.Keywords ?? new List<string>();
            var impactCount = concepts.Count(c => impactKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            return Math.Min(1.0, impactCount / 2.0);
        }

        private double CalculateConsciousnessScore(List<string> concepts, NewsItem newsItem)
        {
            var axis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("consciousness", StringComparison.OrdinalIgnoreCase));
            var consciousnessKeywords = axis?.Keywords ?? new List<string>();
            var consciousnessCount = concepts.Count(c => consciousnessKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            return Math.Min(1.0, consciousnessCount / 3.0);
        }

        private double CalculateUnityScore(List<string> concepts, NewsItem newsItem)
        {
            var axis = _ontologyAxes.FirstOrDefault(a => a.Name.Equals("unity", StringComparison.OrdinalIgnoreCase));
            var unityKeywords = axis?.Keywords ?? new List<string>();
            var unityCount = concepts.Count(c => unityKeywords.Contains(c, StringComparer.OrdinalIgnoreCase));
            return Math.Min(1.0, unityCount / 3.0);
        }

        // --- U-CORE Ontology integration helpers ---
        private void LoadOntologyAxes()
        {
            try
            {
                var axes = new List<OntologyAxis>();

                // Try multiple likely type ids for axes
                foreach (var typeId in new[] { "codex.ontology.axis", "codex.meta/axis", "u-core.axis" })
                {
                    var nodes = Registry.GetNodesByType(typeId).ToArray();
                    foreach (var n in nodes)
                    {
                        var name = n.Meta?.GetValueOrDefault("name")?.ToString() ?? n.Title;
                        var keywordsObj = n.Meta?.GetValueOrDefault("keywords");
                        List<string> keywords = new();
                        if (keywordsObj is IEnumerable<object> objs)
                        {
                            keywords = objs.Select(o => o?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        }
                        else if (!string.IsNullOrEmpty(n.Content?.InlineJson))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(n.Content.InlineJson);
                                if (doc.RootElement.TryGetProperty("keywords", out var kw) && kw.ValueKind == JsonValueKind.Array)
                                {
                                    keywords = kw.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                                }
                            }
                            catch { }
                        }

                        if (!string.IsNullOrWhiteSpace(name) && keywords.Any())
                        {
                            axes.Add(new OntologyAxis(name, keywords));
                        }
                    }
                }

                _ontologyAxes = axes.DistinctBy(a => a.Name.ToLowerInvariant()).ToList();
                _logger.Info($"Loaded {_ontologyAxes.Count} ontology axes from U-CORE");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to load ontology axes from U-CORE: {ex.Message}");
                _ontologyAxes = new List<OntologyAxis>();
            }
        }

        private void SeedDefaultOntologyAxes()
        {
            try
            {
                var defaults = new List<(string name, string[] keywords)>
                {
                    ("abundance", new [] { "abundance", "amplification", "growth", "prosperity", "opportunity" }),
                    ("unity", new [] { "unity", "collaboration", "collective", "community", "global" }),
                    ("resonance", new [] { "resonance", "harmony", "coherence", "joy", "love", "peace", "wisdom" }),
                    ("innovation", new [] { "innovation", "breakthrough", "cutting-edge", "new", "discovery" }),
                    ("science", new [] { "science", "research", "study", "experiment", "data" })
                };

                foreach (var (name, keywords) in defaults)
                {
                    // Skip if already exists
                    var exists = Registry.GetNodesByType("codex.ontology.axis")
                        .ToArray()
                        .Any(n => string.Equals(n.Meta?.GetValueOrDefault("name")?.ToString(), name, StringComparison.OrdinalIgnoreCase));
                    if (exists) continue;

                    var node = new Node(
                        Id: $"u-core-axis-{name.ToLowerInvariant()}",
                        TypeId: "codex.ontology.axis",
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: name,
                        Description: $"U-CORE ontology axis: {name}",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new { name, keywords }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["name"] = name,
                            ["keywords"] = keywords
                        }
                    );

                    Registry.Upsert(node);
                }

                _logger.Info("Seeded minimal U-CORE ontology axes.");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed seeding default ontology axes: {ex.Message}");
            }
        }

        private static string ComputeDeterministicId(string seed)
        {
            using var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(seed);
            var hash = sha1.ComputeHash(bytes);
            return Convert.ToHexString(hash)[..12].ToLowerInvariant();
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
