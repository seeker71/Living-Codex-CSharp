using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Manages persistent configuration using seed nodes
    /// </summary>
    public class ConfigurationManager
    {
        private readonly NodeRegistry _registry;
        private readonly ICodexLogger _logger;
        private readonly string _configPath;
        private const string CONFIG_NODE_TYPE = "codex.config.seed";
        private const string NEWS_SOURCES_CONFIG_ID = "news-sources-config";
        private const string LLM_CONFIG_ID = "llm-config";
        private const string SYSTEM_CONFIG_ID = "system-config";

        public ConfigurationManager(NodeRegistry registry, ICodexLogger logger, string configPath = "config")
        {
            _registry = registry;
            _logger = logger;
            _configPath = Path.Combine(Directory.GetCurrentDirectory(), configPath);
            Directory.CreateDirectory(_configPath);
        }

        /// <summary>
        /// Load all configurations from seed nodes on startup
        /// </summary>
        public async Task LoadConfigurationsAsync()
        {
            try
            {
                _logger.Info("Loading configurations from seed nodes...");

                // Load news sources configuration
                await LoadNewsSourcesConfigAsync();

                // Load LLM configuration
                await LoadLLMConfigAsync();

                // Load system configuration
                await LoadSystemConfigAsync();

                _logger.Info("All configurations loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading configurations: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Save news sources configuration
        /// </summary>
        public async Task SaveNewsSourcesConfigAsync(List<NewsSourceConfig> sources)
        {
            try
            {
                var configNode = new Node(
                    Id: NEWS_SOURCES_CONFIG_ID,
                    TypeId: CONFIG_NODE_TYPE,
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: "News Sources Configuration",
                    Description: "Persistent configuration for all news sources",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(sources, new JsonSerializerOptions { WriteIndented = true }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["configType"] = "news-sources",
                        ["version"] = "1.0.0",
                        ["lastUpdated"] = DateTimeOffset.UtcNow,
                        ["sourceCount"] = sources.Count
                    }
                );

                _registry.Upsert(configNode);

                // Also save to file for backup
                var filePath = Path.Combine(_configPath, "news-sources.json");
                await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(sources, new JsonSerializerOptions { WriteIndented = true }));

                _logger.Info($"Saved {sources.Count} news sources configuration");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving news sources configuration: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Load news sources configuration
        /// </summary>
        private async Task LoadNewsSourcesConfigAsync()
        {
            try
            {
                // First try to load from registry
                var configNode = _registry.GetNode(NEWS_SOURCES_CONFIG_ID);
                if (configNode != null && configNode.Content?.InlineJson != null)
                {
                    var sources = JsonSerializer.Deserialize<List<NewsSourceConfig>>(configNode.Content.InlineJson);
                    if (sources != null)
                    {
                        await CreateNewsSourceNodesAsync(sources);
                        _logger.Info($"Loaded {sources.Count} news sources from registry");
                        return;
                    }
                }

                // Fallback to file
                var filePath = Path.Combine(_configPath, "news-sources.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var sources = JsonSerializer.Deserialize<List<NewsSourceConfig>>(json);
                    if (sources != null)
                    {
                        await CreateNewsSourceNodesAsync(sources);
                        await SaveNewsSourcesConfigAsync(sources); // Save to registry
                        _logger.Info($"Loaded {sources.Count} news sources from file");
                        return;
                    }
                }

                // Create default configuration
                var defaultSources = CreateDefaultNewsSources();
                await CreateNewsSourceNodesAsync(defaultSources);
                await SaveNewsSourcesConfigAsync(defaultSources);
                _logger.Info($"Created default news sources configuration with {defaultSources.Count} sources");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading news sources configuration: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Create news source nodes from configuration
        /// </summary>
        private async Task CreateNewsSourceNodesAsync(List<NewsSourceConfig> sources)
        {
            foreach (var source in sources)
            {
                var sourceNode = new Node(
                    Id: $"news-source-{source.Id}",
                    TypeId: "codex.news.source",
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
                        ["updateIntervalMinutes"] = source.UpdateIntervalMinutes,
                        ["categories"] = source.Categories,
                        ["ontologyLevels"] = source.OntologyLevels,
                        ["priority"] = source.Priority,
                        ["lastIngested"] = source.LastIngested
                    }
                );

                _registry.Upsert(sourceNode);
            }
        }

        /// <summary>
        /// Create comprehensive news sources configuration covering all major categories
        /// </summary>
        private List<NewsSourceConfig> CreateDefaultNewsSources()
        {
            return new List<NewsSourceConfig>
            {
                // ===== SCIENCE & RESEARCH (L0-L2) =====
                new NewsSourceConfig
                {
                    Id = "nature-science",
                    Name = "Nature - Scientific Discoveries",
                    Type = "rss",
                    Url = "https://www.nature.com/nature.rss",
                    Categories = new[] { "science", "research", "discovery", "consciousness" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "sciam-science",
                    Name = "Scientific American",
                    Type = "rss",
                    Url = "https://rss.sciam.com/ScientificAmerican-Global",
                    Categories = new[] { "science", "research", "consciousness", "discovery" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "quanta-magazine",
                    Name = "Quanta Magazine",
                    Type = "rss",
                    Url = "https://api.quantamagazine.org/feed/",
                    Categories = new[] { "science", "mathematics", "physics", "consciousness" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "science-daily",
                    Name = "Science Daily",
                    Type = "rss",
                    Url = "https://www.sciencedaily.com/rss/all.xml",
                    Categories = new[] { "science", "research", "technology", "discovery" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 90,
                    Priority = 4
                },
                new NewsSourceConfig
                {
                    Id = "new-scientist",
                    Name = "New Scientist",
                    Type = "rss",
                    Url = "https://www.newscientist.com/feed/home/",
                    Categories = new[] { "science", "technology", "research", "innovation" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 5
                },
                new NewsSourceConfig
                {
                    Id = "mit-technology-review",
                    Name = "MIT Technology Review",
                    Type = "rss",
                    Url = "https://www.technologyreview.com/feed/",
                    Categories = new[] { "technology", "innovation", "research", "consciousness" },
                    OntologyLevels = new[] { "L0-L2" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 6
                },

                // ===== ENGINEERING & TECHNOLOGY (L1-L3) =====
                new NewsSourceConfig
                {
                    Id = "ieee-spectrum",
                    Name = "IEEE Spectrum",
                    Type = "rss",
                    Url = "https://spectrum.ieee.org/rss/fulltext",
                    Categories = new[] { "engineering", "technology", "innovation", "consciousness" },
                    OntologyLevels = new[] { "L1-L3" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "arstechnica-tech",
                    Name = "Ars Technica",
                    Type = "rss",
                    Url = "https://feeds.arstechnica.com/arstechnica/index/",
                    Categories = new[] { "technology", "engineering", "innovation", "consciousness" },
                    OntologyLevels = new[] { "L1-L3" },
                    IsActive = true,
                    UpdateIntervalMinutes = 90,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "hacker-news",
                    Name = "Hacker News",
                    Type = "api",
                    Url = "https://hacker-news.firebaseio.com/v0/",
                    Categories = new[] { "technology", "programming", "innovation", "consciousness" },
                    OntologyLevels = new[] { "L1-L3" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "techcrunch",
                    Name = "TechCrunch",
                    Type = "rss",
                    Url = "https://techcrunch.com/feed/",
                    Categories = new[] { "technology", "startups", "innovation", "consciousness" },
                    OntologyLevels = new[] { "L1-L3" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 4
                },
                new NewsSourceConfig
                {
                    Id = "wired-tech",
                    Name = "Wired Technology",
                    Type = "rss",
                    Url = "https://www.wired.com/feed/rss",
                    Categories = new[] { "technology", "innovation", "culture", "consciousness" },
                    OntologyLevels = new[] { "L1-L3" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 5
                },

                // ===== SPIRITUALITY & CONSCIOUSNESS (L3-L4) =====
                new NewsSourceConfig
                {
                    Id = "lions-roar-spirituality",
                    Name = "Lions Roar - Buddhist Wisdom",
                    Type = "rss",
                    Url = "https://www.lionsroar.com/feed/",
                    Categories = new[] { "spirituality", "mindfulness", "consciousness", "wisdom" },
                    OntologyLevels = new[] { "L3-L4" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "tricycle-buddhism",
                    Name = "Tricycle - Buddhist Teachings",
                    Type = "rss",
                    Url = "https://tricycle.org/feed/",
                    Categories = new[] { "spirituality", "buddhism", "mindfulness", "consciousness" },
                    OntologyLevels = new[] { "L3-L4" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "mindful-org",
                    Name = "Mindful.org",
                    Type = "rss",
                    Url = "https://www.mindful.org/feed/",
                    Categories = new[] { "mindfulness", "meditation", "consciousness", "wellness" },
                    OntologyLevels = new[] { "L3-L4" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "consciousness-research",
                    Name = "Consciousness Research Network",
                    Type = "rss",
                    Url = "https://consciousness-research.org/feed/",
                    Categories = new[] { "consciousness", "research", "spirituality", "science" },
                    OntologyLevels = new[] { "L3-L4" },
                    IsActive = true,
                    UpdateIntervalMinutes = 360,
                    Priority = 4
                },
                new NewsSourceConfig
                {
                    Id = "integral-life",
                    Name = "Integral Life",
                    Type = "rss",
                    Url = "https://integrallife.com/feed/",
                    Categories = new[] { "consciousness", "spirituality", "evolution", "wisdom" },
                    OntologyLevels = new[] { "L3-L4" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 5
                },

                // ===== UNITY CONSCIOUSNESS & GLOBAL COLLABORATION (L5-L6) =====
                new NewsSourceConfig
                {
                    Id = "global-citizen-unity",
                    Name = "Global Citizen - Unity Consciousness",
                    Type = "rss",
                    Url = "https://www.globalcitizen.org/en/feed/",
                    Categories = new[] { "unity", "global", "consciousness", "collaboration" },
                    OntologyLevels = new[] { "L5-L6" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "un-news-unity",
                    Name = "UN News - International Cooperation",
                    Type = "rss",
                    Url = "https://news.un.org/en/rss",
                    Categories = new[] { "unity", "international", "cooperation", "consciousness" },
                    OntologyLevels = new[] { "L5-L6" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "world-economic-forum",
                    Name = "World Economic Forum",
                    Type = "rss",
                    Url = "https://www.weforum.org/feeds/",
                    Categories = new[] { "global", "collaboration", "consciousness", "unity" },
                    OntologyLevels = new[] { "L5-L6" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "global-issues",
                    Name = "Global Issues",
                    Type = "rss",
                    Url = "https://www.globalissues.org/news/rss",
                    Categories = new[] { "global", "unity", "consciousness", "collaboration" },
                    OntologyLevels = new[] { "L5-L6" },
                    IsActive = true,
                    UpdateIntervalMinutes = 360,
                    Priority = 4
                },

                // ===== WORLD NEWS & INTERNATIONAL (L7-L8) =====
                new NewsSourceConfig
                {
                    Id = "bbc-world",
                    Name = "BBC World News",
                    Type = "rss",
                    Url = "http://feeds.bbci.co.uk/news/world/rss.xml",
                    Categories = new[] { "world", "international", "consciousness", "unity" },
                    OntologyLevels = new[] { "L7-L8" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "reuters-world",
                    Name = "Reuters World News",
                    Type = "rss",
                    Url = "https://feeds.reuters.com/Reuters/worldNews",
                    Categories = new[] { "world", "international", "consciousness", "unity" },
                    OntologyLevels = new[] { "L7-L8" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "guardian-world",
                    Name = "The Guardian World",
                    Type = "rss",
                    Url = "https://www.theguardian.com/world/rss",
                    Categories = new[] { "world", "international", "consciousness", "unity" },
                    OntologyLevels = new[] { "L7-L8" },
                    IsActive = true,
                    UpdateIntervalMinutes = 45,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "al-jazeera-world",
                    Name = "Al Jazeera World",
                    Type = "rss",
                    Url = "https://www.aljazeera.com/xml/rss/all.xml",
                    Categories = new[] { "world", "international", "consciousness", "unity" },
                    OntologyLevels = new[] { "L7-L8" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 4
                },

                // ===== FINANCIAL & ECONOMIC (L9-L10) =====
                new NewsSourceConfig
                {
                    Id = "bloomberg-finance",
                    Name = "Bloomberg Finance",
                    Type = "rss",
                    Url = "https://feeds.bloomberg.com/markets/news.rss",
                    Categories = new[] { "finance", "economics", "consciousness", "abundance" },
                    OntologyLevels = new[] { "L9-L10" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "reuters-business",
                    Name = "Reuters Business",
                    Type = "rss",
                    Url = "https://feeds.reuters.com/reuters/businessNews",
                    Categories = new[] { "finance", "business", "consciousness", "abundance" },
                    OntologyLevels = new[] { "L9-L10" },
                    IsActive = true,
                    UpdateIntervalMinutes = 30,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "financial-times",
                    Name = "Financial Times",
                    Type = "rss",
                    Url = "https://www.ft.com/rss/home",
                    Categories = new[] { "finance", "economics", "consciousness", "abundance" },
                    OntologyLevels = new[] { "L9-L10" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "wall-street-journal",
                    Name = "Wall Street Journal",
                    Type = "rss",
                    Url = "https://feeds.a.dj.com/rss/RSSMarketsMain.xml",
                    Categories = new[] { "finance", "markets", "consciousness", "abundance" },
                    OntologyLevels = new[] { "L9-L10" },
                    IsActive = true,
                    UpdateIntervalMinutes = 60,
                    Priority = 4
                },

                // ===== HEALTH & WELLNESS (L11-L12) =====
                new NewsSourceConfig
                {
                    Id = "webmd-health",
                    Name = "WebMD Health News",
                    Type = "rss",
                    Url = "https://rssfeeds.webmd.com/rss/rss.aspx?RSSSource=RSS_PUBLIC",
                    Categories = new[] { "health", "wellness", "consciousness", "healing" },
                    OntologyLevels = new[] { "L11-L12" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "medical-news-today",
                    Name = "Medical News Today",
                    Type = "rss",
                    Url = "https://www.medicalnewstoday.com/rss",
                    Categories = new[] { "health", "medicine", "consciousness", "healing" },
                    OntologyLevels = new[] { "L11-L12" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "mayo-clinic-health",
                    Name = "Mayo Clinic Health",
                    Type = "rss",
                    Url = "https://www.mayoclinic.org/rss/all-mayo-clinic-news",
                    Categories = new[] { "health", "wellness", "consciousness", "healing" },
                    OntologyLevels = new[] { "L11-L12" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 3
                },

                // ===== ENVIRONMENT & SUSTAINABILITY (L13-L14) =====
                new NewsSourceConfig
                {
                    Id = "environmental-news-network",
                    Name = "Environmental News Network",
                    Type = "rss",
                    Url = "https://www.enn.com/rss.xml",
                    Categories = new[] { "environment", "sustainability", "consciousness", "unity" },
                    OntologyLevels = new[] { "L13-L14" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "treehugger-environment",
                    Name = "TreeHugger Environment",
                    Type = "rss",
                    Url = "https://www.treehugger.com/feeds/all.rss",
                    Categories = new[] { "environment", "sustainability", "consciousness", "unity" },
                    OntologyLevels = new[] { "L13-L14" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "national-geographic-environment",
                    Name = "National Geographic Environment",
                    Type = "rss",
                    Url = "https://feeds.nationalgeographic.com/ng/News/News_Environment",
                    Categories = new[] { "environment", "nature", "consciousness", "unity" },
                    OntologyLevels = new[] { "L13-L14" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 3
                },

                // ===== COSMOLOGY & QUANTUM (L15-L16) =====
                new NewsSourceConfig
                {
                    Id = "space-cosmic-consciousness",
                    Name = "Space.com - Cosmic Consciousness",
                    Type = "rss",
                    Url = "https://www.space.com/feeds/all",
                    Categories = new[] { "cosmology", "consciousness", "universe", "exploration" },
                    OntologyLevels = new[] { "L15-L16" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "phys-org-cosmology",
                    Name = "Phys.org - Physics & Cosmology",
                    Type = "rss",
                    Url = "https://phys.org/rss-feed/",
                    Categories = new[] { "cosmology", "physics", "science", "consciousness" },
                    OntologyLevels = new[] { "L15-L16" },
                    IsActive = true,
                    UpdateIntervalMinutes = 120,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "nasa-space-news",
                    Name = "NASA Space News",
                    Type = "rss",
                    Url = "https://www.nasa.gov/rss/dyn/breaking_news.rss",
                    Categories = new[] { "space", "cosmology", "consciousness", "exploration" },
                    OntologyLevels = new[] { "L15-L16" },
                    IsActive = true,
                    UpdateIntervalMinutes = 180,
                    Priority = 3
                },
                new NewsSourceConfig
                {
                    Id = "quantum-magazine",
                    Name = "Quantum Magazine",
                    Type = "rss",
                    Url = "https://www.quantamagazine.org/feed/",
                    Categories = new[] { "quantum", "physics", "consciousness", "cosmology" },
                    OntologyLevels = new[] { "L15-L16" },
                    IsActive = true,
                    UpdateIntervalMinutes = 240,
                    Priority = 4
                },

                // ===== ART & CULTURE (L17-L18) =====
                new NewsSourceConfig
                {
                    Id = "artsy-art-news",
                    Name = "Artsy Art News",
                    Type = "rss",
                    Url = "https://www.artsy.net/rss/news",
                    Categories = new[] { "art", "culture", "consciousness", "creativity" },
                    OntologyLevels = new[] { "L17-L18" },
                    IsActive = true,
                    UpdateIntervalMinutes = 360,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "hyperallergic-art",
                    Name = "Hyperallergic Art",
                    Type = "rss",
                    Url = "https://hyperallergic.com/feed/",
                    Categories = new[] { "art", "culture", "consciousness", "creativity" },
                    OntologyLevels = new[] { "L17-L18" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "artnews-culture",
                    Name = "ARTnews Culture",
                    Type = "rss",
                    Url = "https://www.artnews.com/feed/",
                    Categories = new[] { "art", "culture", "consciousness", "creativity" },
                    OntologyLevels = new[] { "L17-L18" },
                    IsActive = true,
                    UpdateIntervalMinutes = 360,
                    Priority = 3
                },

                // ===== PHILOSOPHY & WISDOM (L19-L20) =====
                new NewsSourceConfig
                {
                    Id = "philosophy-now",
                    Name = "Philosophy Now",
                    Type = "rss",
                    Url = "https://philosophynow.org/rss",
                    Categories = new[] { "philosophy", "wisdom", "consciousness", "truth" },
                    OntologyLevels = new[] { "L19-L20" },
                    IsActive = true,
                    UpdateIntervalMinutes = 480,
                    Priority = 1
                },
                new NewsSourceConfig
                {
                    Id = "ieet-future-consciousness",
                    Name = "IEET Future Consciousness",
                    Type = "rss",
                    Url = "https://ieet.org/index.php/IEET2/rss",
                    Categories = new[] { "philosophy", "future", "consciousness", "wisdom" },
                    OntologyLevels = new[] { "L19-L20" },
                    IsActive = true,
                    UpdateIntervalMinutes = 360,
                    Priority = 2
                },
                new NewsSourceConfig
                {
                    Id = "big-think-wisdom",
                    Name = "Big Think Wisdom",
                    Type = "rss",
                    Url = "https://bigthink.com/feed/",
                    Categories = new[] { "philosophy", "wisdom", "consciousness", "truth" },
                    OntologyLevels = new[] { "L19-L20" },
                    IsActive = true,
                    UpdateIntervalMinutes = 300,
                    Priority = 3
                }
            };
        }

        /// <summary>
        /// Load LLM configuration
        /// </summary>
        private async Task LoadLLMConfigAsync()
        {
            try
            {
                var configNode = _registry.GetNode(LLM_CONFIG_ID);
                if (configNode == null)
                {
                    // Create default LLM configuration
                    var llmConfig = new LLMConfiguration
                    {
                        Id = "default-llm-config",
                        Name = "Default LLM Configuration",
                        Provider = "openai",
                        Model = "gpt-4",
                        ApiKey = "", // Should be set via environment variable
                        MaxTokens = 4000,
                        Temperature = 0.7,
                        TopP = 1.0,
                        FrequencyPenalty = 0.0,
                        PresencePenalty = 0.0,
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    var llmNode = new Node(
                        Id: LLM_CONFIG_ID,
                        TypeId: CONFIG_NODE_TYPE,
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: "LLM Configuration",
                        Description: "Configuration for Large Language Model integration",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(llmConfig, new JsonSerializerOptions { WriteIndented = true }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["configType"] = "llm",
                            ["version"] = "1.0.0",
                            ["lastUpdated"] = DateTimeOffset.UtcNow
                        }
                    );

                    _registry.Upsert(llmNode);
                    _logger.Info("Created default LLM configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading LLM configuration: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Load system configuration
        /// </summary>
        private async Task LoadSystemConfigAsync()
        {
            try
            {
                var configNode = _registry.GetNode(SYSTEM_CONFIG_ID);
                if (configNode == null)
                {
                    // Create default system configuration
                    var systemConfig = new SystemConfiguration
                    {
                        Id = "default-system-config",
                        Name = "Living Codex System Configuration",
                        MaxNewsItemsPerSource = 50,
                        IngestionIntervalMinutes = 15,
                        CleanupIntervalHours = 24,
                        FractalAnalysisEnabled = true,
                        DuplicateDetectionEnabled = true,
                        CacheEnabled = true,
                        CacheExpirationHours = 24,
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    var systemNode = new Node(
                        Id: SYSTEM_CONFIG_ID,
                        TypeId: CONFIG_NODE_TYPE,
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: "System Configuration",
                        Description: "Core system configuration parameters",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(systemConfig, new JsonSerializerOptions { WriteIndented = true }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["configType"] = "system",
                            ["version"] = "1.0.0",
                            ["lastUpdated"] = DateTimeOffset.UtcNow
                        }
                    );

                    _registry.Upsert(systemNode);
                    _logger.Info("Created default system configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading system configuration: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all news sources from configuration
        /// </summary>
        public List<NewsSourceConfig> GetNewsSources()
        {
            var sourceNodes = _registry.GetNodesByType("codex.news.source");
            var sources = new List<NewsSourceConfig>();

            foreach (var node in sourceNodes)
            {
                if (node.Content?.InlineJson != null)
                {
                    var source = JsonSerializer.Deserialize<NewsSourceConfig>(node.Content.InlineJson);
                    if (source != null)
                    {
                        sources.Add(source);
                    }
                }
            }

            return sources;
        }

        /// <summary>
        /// Update news source configuration
        /// </summary>
        public async Task UpdateNewsSourceAsync(NewsSourceConfig source)
        {
            var sources = GetNewsSources();
            var existingIndex = sources.FindIndex(s => s.Id == source.Id);
            
            if (existingIndex >= 0)
            {
                sources[existingIndex] = source;
            }
            else
            {
                sources.Add(source);
            }

            await SaveNewsSourcesConfigAsync(sources);
        }

        /// <summary>
        /// Remove news source configuration
        /// </summary>
        public async Task RemoveNewsSourceAsync(string sourceId)
        {
            var sources = GetNewsSources();
            sources.RemoveAll(s => s.Id == sourceId);
            await SaveNewsSourcesConfigAsync(sources);
        }
    }

    // Configuration Data Structures
    public class NewsSourceConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string[] OntologyLevels { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; } = true;
        public int UpdateIntervalMinutes { get; set; } = 30;
        public int Priority { get; set; } = 1;
        public DateTimeOffset? LastIngested { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class LLMConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 4000;
        public double Temperature { get; set; } = 0.7;
        public double TopP { get; set; } = 1.0;
        public double FrequencyPenalty { get; set; } = 0.0;
        public double PresencePenalty { get; set; } = 0.0;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class SystemConfiguration
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int MaxNewsItemsPerSource { get; set; } = 50;
        public int IngestionIntervalMinutes { get; set; } = 15;
        public int CleanupIntervalHours { get; set; } = 24;
        public bool FractalAnalysisEnabled { get; set; } = true;
        public bool DuplicateDetectionEnabled { get; set; } = true;
        public bool CacheEnabled { get; set; } = true;
        public int CacheExpirationHours { get; set; } = 24;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
