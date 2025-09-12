using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// Simplified Real-time fractal news streaming module with persistent configuration
    /// </summary>
    public class RealtimeNewsStreamModuleSimple : IModule, IHostedService
    {
        private readonly Core.ILogger _logger;
        private readonly NodeRegistry _registry;
        private readonly HttpClient _httpClient;
        private readonly Core.ConfigurationManager _configManager;
        private readonly Timer _ingestionTimer;
        private readonly Timer _cleanupTimer;
        private readonly int _ingestionIntervalMinutes = 15;
        private readonly int _cleanupIntervalHours = 24;
        private readonly int _maxItemsPerSource = 50;
        private readonly HashSet<string> _processedNewsIds = new();

        // Node type constants
        private const string NEWS_SOURCE_NODE_TYPE = "codex.news.source";
        private const string NEWS_ITEM_NODE_TYPE = "codex.news.item";
        private const string FRACTAL_NEWS_NODE_TYPE = "codex.news.fractal";
        private const string NEWS_SUBSCRIPTION_NODE_TYPE = "codex.news.subscription";

        public RealtimeNewsStreamModuleSimple(NodeRegistry registry, HttpClient? httpClient = null, Core.ConfigurationManager? configManager = null)
        {
            _logger = new Log4NetLogger(typeof(RealtimeNewsStreamModuleSimple));
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

        public Node GetModuleNode()
        {
            return new Node(
                Id: "realtime-news-stream-module",
                TypeId: "module",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: Name,
                Description: Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new { Name, Description, Version }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["version"] = Version,
                    ["type"] = "news-streaming"
                }
            );
        }

        public void Register(NodeRegistry registry)
        {
            registry.Upsert(GetModuleNode());
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via attributes
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
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

        private async Task IngestNewsFromSources(object? state)
        {
            try
            {
                _logger.Info("Starting news ingestion from all sources");

                var sources = _configManager.GetNewsSources();
                var activeSources = sources.Where(s => s.IsActive).ToList();

                _logger.Info($"Found {activeSources.Count} active news sources");

                foreach (var source in activeSources)
                {
                    try
                    {
                        _logger.Info($"Ingesting from source: {source.Name} ({source.Id})");

                        if (source.Type == "rss")
                        {
                            await IngestRssFeed(source);
                        }
                        else if (source.Type == "api")
                        {
                            await IngestApiFeed(source);
                        }

                        _logger.Info($"Successfully ingested from {source.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to ingest from {source.Name}: {ex.Message}", ex);
                    }
                }

                _logger.Info("News ingestion completed");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during news ingestion: {ex.Message}", ex);
            }
        }

        private async Task IngestRssFeed(NewsSourceConfig source)
        {
            try
            {
                using var response = await _httpClient.GetAsync(source.Url);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = XmlReader.Create(stream);
                var feed = SyndicationFeed.Load(reader);

                var items = feed.Items.Take(_maxItemsPerSource).ToList();
                _logger.Info($"Found {items.Count} items in RSS feed: {source.Name}");

                foreach (var item in items)
                {
                    await ProcessNewsItem(item, source);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting RSS feed {source.Name}: {ex.Message}", ex);
            }
        }

        private async Task IngestApiFeed(NewsSourceConfig source)
        {
            try
            {
                using var response = await _httpClient.GetAsync(source.Url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var stories = JsonSerializer.Deserialize<HackerNewsStory[]>(json);

                if (stories != null)
                {
                    var items = stories.Take(_maxItemsPerSource).ToList();
                    _logger.Info($"Found {items.Count} items in API feed: {source.Name}");

                    foreach (var story in items)
                    {
                        await ProcessApiNewsItem(story, source);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting API feed {source.Name}: {ex.Message}", ex);
            }
        }

        private async Task ProcessNewsItem(SyndicationItem item, NewsSourceConfig source)
        {
            try
            {
                var newsId = $"rss-{source.Id}-{item.Id?.GetHashCode() ?? item.Title?.GetHashCode() ?? Guid.NewGuid().GetHashCode():x}";

                // Check for duplicates
                if (_processedNewsIds.Contains(newsId) || _registry.GetNode($"news-item-{newsId}") != null)
                {
                    return;
                }

                var newsItem = new NewsItem
                {
                    Id = newsId,
                    Title = item.Title?.Text ?? "Untitled",
                    Summary = item.Summary?.Text ?? "",
                    Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                    PublishedAt = item.PublishDate,
                    SourceId = source.Id,
                    SourceName = source.Name,
                    Categories = source.Categories.ToList(),
                    Tags = ExtractTagsFromContent(item.Title?.Text + " " + item.Summary?.Text)
                };

                // Store news item as Water node
                var newsNode = new Node(
                    Id: $"news-item-{newsId}",
                    TypeId: NEWS_ITEM_NODE_TYPE,
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: newsItem.Title,
                    Description: newsItem.Summary,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(newsItem),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["newsId"] = newsItem.Id,
                        ["sourceId"] = newsItem.SourceId,
                        ["sourceName"] = newsItem.SourceName,
                        ["publishedAt"] = newsItem.PublishedAt,
                        ["url"] = newsItem.Url,
                        ["categories"] = newsItem.Categories,
                        ["tags"] = newsItem.Tags
                    }
                );

                _registry.Upsert(newsNode);
                _processedNewsIds.Add(newsId);

                // Create fractal transformation
                await CreateFractalNewsItem(newsItem);

                _logger.Debug($"Processed news item: {newsItem.Title}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing news item: {ex.Message}", ex);
            }
        }

        private async Task ProcessApiNewsItem(HackerNewsStory story, NewsSourceConfig source)
        {
            try
            {
                var newsId = $"api-{source.Id}-{story.Id}";

                // Check for duplicates
                if (_processedNewsIds.Contains(newsId) || _registry.GetNode($"news-item-{newsId}") != null)
                {
                    return;
                }

                var newsItem = new NewsItem
                {
                    Id = newsId,
                    Title = story.Title,
                    Summary = story.Text ?? "",
                    Url = story.Url,
                    PublishedAt = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                    SourceId = source.Id,
                    SourceName = source.Name,
                    Categories = source.Categories.ToList(),
                    Tags = ExtractTagsFromContent(story.Title + " " + story.Text)
                };

                // Store news item as Water node
                var newsNode = new Node(
                    Id: $"news-item-{newsId}",
                    TypeId: NEWS_ITEM_NODE_TYPE,
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: newsItem.Title,
                    Description: newsItem.Summary,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(newsItem),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["newsId"] = newsItem.Id,
                        ["sourceId"] = newsItem.SourceId,
                        ["sourceName"] = newsItem.SourceName,
                        ["publishedAt"] = newsItem.PublishedAt,
                        ["url"] = newsItem.Url,
                        ["categories"] = newsItem.Categories,
                        ["tags"] = newsItem.Tags
                    }
                );

                _registry.Upsert(newsNode);
                _processedNewsIds.Add(newsId);

                // Create fractal transformation
                await CreateFractalNewsItem(newsItem);

                _logger.Debug($"Processed API news item: {newsItem.Title}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing API news item: {ex.Message}", ex);
            }
        }

        private async Task CreateFractalNewsItem(NewsItem newsItem)
        {
            try
            {
                // Simple fractal transformation for now
                var fractalItem = new FractalNewsItem
                {
                    Id = $"fractal-{newsItem.Id}",
                    OriginalNewsId = newsItem.Id,
                    Headline = TransformHeadline(newsItem.Title),
                    BeliefSystemTranslation = TranslateToBeliefSystem(newsItem.Title, newsItem.Summary),
                    Summary = TransformSummary(newsItem.Summary),
                    ImpactAreas = DetermineImpactAreas(newsItem.Categories),
                    AmplificationFactors = CalculateAmplificationFactors(newsItem),
                    ResonanceData = CalculateResonanceData(newsItem),
                    ProcessedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["originalSource"] = newsItem.SourceName,
                        ["transformationType"] = "consciousness-expansion",
                        ["ontologyLevel"] = DetermineOntologyLevel(newsItem.Categories)
                    }
                };

                // Store fractal news item as Water node
                var fractalNode = new Node(
                    Id: $"fractal-{newsItem.Id}",
                    TypeId: FRACTAL_NEWS_NODE_TYPE,
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: fractalItem.Headline,
                    Description: fractalItem.Summary,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(fractalItem),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["fractalId"] = fractalItem.Id,
                        ["originalNewsId"] = fractalItem.OriginalNewsId,
                        ["processedAt"] = fractalItem.ProcessedAt,
                        ["impactAreas"] = fractalItem.ImpactAreas,
                        ["amplificationFactors"] = fractalItem.AmplificationFactors,
                        ["ontologyLevel"] = fractalItem.Metadata["ontologyLevel"]
                    }
                );

                _registry.Upsert(fractalNode);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating fractal news item: {ex.Message}", ex);
            }
        }

        private string TransformHeadline(string originalTitle)
        {
            // Simple transformation to make headlines more consciousness-expanding
            return originalTitle
                .Replace("breakthrough", "consciousness-expanding breakthrough")
                .Replace("discovery", "unity-revealing discovery")
                .Replace("innovation", "abundance-generating innovation")
                .Replace("crisis", "transformation opportunity")
                .Replace("problem", "growth invitation");
        }

        private string TranslateToBeliefSystem(string title, string summary)
        {
            // Simple belief system translation
            var content = $"{title} {summary}".ToLower();
            
            if (content.Contains("quantum") || content.Contains("consciousness"))
                return "This reveals the quantum nature of consciousness and our interconnected reality.";
            else if (content.Contains("sustainability") || content.Contains("environment"))
                return "This demonstrates our sacred responsibility to steward Earth's abundance.";
            else if (content.Contains("collaboration") || content.Contains("unity"))
                return "This shows the power of collective consciousness and unified action.";
            else
                return "This offers an opportunity for expanded awareness and growth.";
        }

        private string TransformSummary(string originalSummary)
        {
            // Add consciousness-expanding language to summaries
            return $"In the dance of universal consciousness, {originalSummary.ToLower()} This moment invites us to expand our awareness and embrace the infinite possibilities of our interconnected reality.";
        }

        private string[] DetermineImpactAreas(List<string> categories)
        {
            var impactAreas = new List<string>();
            
            foreach (var category in categories)
            {
                switch (category.ToLower())
                {
                    case "science":
                    case "technology":
                        impactAreas.Add("L0-L2: Scientific Foundation");
                        break;
                    case "consciousness":
                    case "spirituality":
                        impactAreas.Add("L3-L4: Consciousness Expansion");
                        break;
                    case "unity":
                    case "collaboration":
                        impactAreas.Add("L5-L6: Unity Consciousness");
                        break;
                    case "sustainability":
                    case "environment":
                        impactAreas.Add("L9-L10: Gaia Stewardship");
                        break;
                    case "cosmology":
                    case "quantum":
                        impactAreas.Add("L15-L16: Cosmic Awareness");
                        break;
                }
            }
            
            return impactAreas.Any() ? impactAreas.ToArray() : new[] { "L0-L2: General Awareness" };
        }

        private Dictionary<string, double> CalculateAmplificationFactors(NewsItem newsItem)
        {
            var factors = new Dictionary<string, double>
            {
                ["consciousness_expansion"] = 1.0,
                ["unity_resonance"] = 1.0,
                ["abundance_mindset"] = 1.0,
                ["collective_evolution"] = 1.0
            };

            // Adjust factors based on content
            var content = $"{newsItem.Title} {newsItem.Summary}".ToLower();
            
            if (content.Contains("quantum") || content.Contains("consciousness"))
                factors["consciousness_expansion"] = 2.0;
            
            if (content.Contains("unity") || content.Contains("collaboration"))
                factors["unity_resonance"] = 2.0;
            
            if (content.Contains("abundance") || content.Contains("prosperity"))
                factors["abundance_mindset"] = 2.0;

            return factors;
        }

        private Dictionary<string, object> CalculateResonanceData(NewsItem newsItem)
        {
            return new Dictionary<string, object>
            {
                ["frequency"] = "432Hz", // Universal harmony frequency
                ["chakra"] = "Heart Chakra", // Unity and love
                ["element"] = "Ether", // Consciousness and space
                ["archetype"] = "The Sage", // Wisdom and knowledge
                ["mantra"] = "Om Namah Shivaya" // Universal consciousness
            };
        }

        private string DetermineOntologyLevel(List<string> categories)
        {
            var levelMapping = new Dictionary<string, string>
            {
                ["technology"] = "L0-L2",
                ["science"] = "L0-L2",
                ["consciousness"] = "L3-L4",
                ["spirituality"] = "L3-L4",
                ["unity"] = "L5-L6",
                ["collaboration"] = "L5-L6",
                ["sustainability"] = "L9-L10",
                ["cosmology"] = "L15-L16",
                ["quantum"] = "L15-L16"
            };

            foreach (var category in categories)
            {
                if (levelMapping.TryGetValue(category.ToLower(), out var level))
                {
                    return level;
                }
            }

            return "L0-L2";
        }

        private List<string> ExtractTagsFromContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Select(w => w.ToLower().Trim(new char[] { '.', ',', '!', '?', ';', ':', '"', '(', ')', '[', ']', '{', '}' }))
                .Where(w => !string.IsNullOrEmpty(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            return words;
        }

        private void CleanupOldNews(object? state)
        {
            try
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-7);
                var newsNodes = _registry.GetNodesByType(NEWS_ITEM_NODE_TYPE);
                var nodesToRemove = new List<string>();

                foreach (var node in newsNodes)
                {
                    if (node.Meta?.TryGetValue("publishedAt", out var publishedAt) == true &&
                        publishedAt is DateTimeOffset date && date < cutoffDate)
                    {
                        nodesToRemove.Add(node.Id);
                    }
                }

                foreach (var nodeId in nodesToRemove)
                {
                    _registry.RemoveNode(nodeId);
                }

                _logger.Info($"Cleaned up {nodesToRemove.Count} old news items");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during cleanup: {ex.Message}", ex);
            }
        }
    }

    // Data structures are defined in the main RealtimeNewsStreamModule
}
