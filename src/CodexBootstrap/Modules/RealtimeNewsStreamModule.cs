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
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Collections.Generic;

// ContentExtractionResult type is defined in ContentExtractionModule

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// U-Core Path represents a conceptual path from a source concept to U-Core concepts
    /// </summary>
    public class UCorePath
    {
        public string SourceConcept { get; set; } = "";
        public string TargetConcept { get; set; } = "";
        public List<string> ConceptIds { get; set; } = new();
        public double PathStrength { get; set; } = 0.0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Real-time fractal news streaming module that ingests external news sources
    /// and transforms them through fractal analysis aligned with belief systems
    /// </summary>
    /// <remarks>
    /// Requires NEWS_INGESTION_ENABLED and API keys; AI enrichment falls back to logs when the AI module handlers are unavailable.
    /// </remarks>
    [MetaNode(Id = "realtime-news-stream", Name = "Realtime News Stream Module", Description = "Real-time fractal news streaming module that ingests external news sources and transforms them through fractal analysis aligned with belief systems")]
    public class RealtimeNewsStreamModule : ModuleBase, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Core.ConfigurationManager _configManager;
        private AIRequestQueue? _aiQueue;

        public override string Name => "Realtime News Stream Module";
        public override string Description => "Real-time fractal news streaming module that ingests external news sources and transforms them through fractal analysis aligned with belief systems";
        public override string Version => "1.0.0";

        public override Node GetModuleNode()
        {
            return CreateModuleNode(
                moduleId: "realtime-news-stream",
                name: Name,
                version: Version,
                description: Description,
                tags: new[] { "news", "realtime", "rss", "ingestion", "fractal", "streaming" },
                capabilities: new[] { 
                    "rss-ingestion", "api-ingestion", "fractal-analysis", "real-time-streaming",
                    "news-source-management", "content-filtering", "belief-system-alignment"
                },
                spec: "codex.spec.realtime-news-stream"
            );
        }
        private readonly Timer _ingestionTimer;
        private readonly Timer _cleanupTimer;
        private readonly Timer _memoryCleanupTimer;
        private readonly CancellationTokenSource _shutdownCts = new();
        private CrossModuleCommunicator? _moduleCommunicator;
        private AIModuleTemplates? _aiTemplates = null;
        private readonly int _ingestionIntervalMinutes = int.TryParse(Environment.GetEnvironmentVariable("NEWS_INGESTION_INTERVAL_MIN"), out var iv) && iv > 0 ? iv : 15;
        private readonly int _cleanupIntervalHours = 24;
        private readonly int _maxItemsPerSource = 50; // Increased from 10
        private readonly ConcurrentDictionary<string, bool> _processedNewsIds = new(); // Track processed news to prevent duplicates
        private readonly SemaphoreSlim _semaphore = new(1, 1); // Semaphore for async thread safety
        
        // Memory management constants - embodying compassionate resource stewardship
        private const int MAX_PROCESSED_NEWS_IDS = 10000;
        private const int NEWS_CLEANUP_INTERVAL_MINUTES = 60;
        private const int NEWS_ID_TTL_HOURS = 48;

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

        // Lazy-loaded AI templates (requires _apiRouter to be set)
        private AIModuleTemplates? AITemplates 
        { 
            get 
            {
                _logger.Info($"AITemplates getter called: _aiTemplates={(_aiTemplates != null ? "non-null" : "null")}, _apiRouter={(_apiRouter != null ? "non-null" : "null")}");
                if (_aiTemplates == null)
                {
                    if (_apiRouter != null)
                    {
                        _aiTemplates = new AIModuleTemplates(_apiRouter, _logger);
                        _logger.Info("RealtimeNewsStreamModule: Lazily initialized _aiTemplates on first use");
                    }
                    else
                    {
                        _logger.Error("AITemplates is null and _apiRouter is null; cannot call AI yet");
                    }
                }
                return _aiTemplates;
            }
        }

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

            public async Task<string?> GenerateSummaryAsync(
                string content,
                string? model = null,
                string? provider = null)
            {
                try
                {
                    var request = new
                    {
                        Concept = new
                        {
                            Id = "",
                            Name = "",
                            Description = content,
                            Domain = "News",
                            Complexity = 0,
                            Tags = new string[0]
                        },
                        Context = "news-summary",
                        Style = "clear, informative, well-structured",
                        Length = "medium",
                        Model = model,
                        Provider = provider
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "generate-summary", out var handler))
                    {
                        _logger.Info("AIModuleTemplates: Found generate-summary handler, calling it");
                        var result = await handler(requestElement);
                        _logger.Info($"AIModuleTemplates: generate-summary handler returned: {result != null}");
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (resultElement.TryGetProperty("data", out var data) && 
                                    data.TryGetProperty("summary", out var summary))
                                {
                                    return summary.GetString();
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AIModuleTemplates: generate-summary handler not found in router");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error calling AI module generate-summary: {ex.Message}", ex);
                }
                return null;
            }

            public async Task<ConceptExtractionResult?> ExtractConceptsAsync(
                string content, 
                int maxConcepts = 5,
                string? model = null,
                string? provider = null)
            {
                try
                {
                    var request = new
                    {
                        Content = content,
                        MaxConcepts = maxConcepts,
                        Model = model,
                        Provider = provider
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "extract-concepts", out var handler))
                    {
                        _logger.Info("AIModuleTemplates: Found extract-concepts handler, calling it");
                        var result = await handler(requestElement);
                        _logger.Info($"AIModuleTemplates: extract-concepts handler returned: {result != null}");
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                var concepts = new List<string>();
                                if (resultElement.TryGetProperty("concepts", out var arr) && arr.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var c in arr.EnumerateArray())
                                    {
                                        var s = c.GetString();
                                        if (!string.IsNullOrWhiteSpace(s)) concepts.Add(s!);
                                    }
                                }

                                var confidence = resultElement.TryGetProperty("confidence", out var conf) && conf.ValueKind == JsonValueKind.Number
                                    ? conf.GetDouble()
                                    : 0.5;

                                var levels = new List<string>();
                                if (resultElement.TryGetProperty("ontologyLevels", out var lv) && lv.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var l in lv.EnumerateArray())
                                    {
                                        var s = l.GetString();
                                        if (!string.IsNullOrWhiteSpace(s)) levels.Add(s!);
                                    }
                                }

                                return new ConceptExtractionResult
                                {
                                    Concepts = concepts,
                                    Confidence = confidence,
                                    OntologyLevels = levels
                                };
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AIModuleTemplates: extract-concepts handler not found in router");
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
                        Content = content,
                        AnalysisType = analysisType,
                        Criteria = new[] { "relevance", "quality", "impact" }
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "score-analysis", out var handler))
                    {
                        _logger.Info("AIModuleTemplates: Found score-analysis handler, calling it");
                        var result = await handler(requestElement);
                        _logger.Info($"AIModuleTemplates: score-analysis handler returned: {result != null}");
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
                        Content = content
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "fractal-transform", out var handler))
                    {
                        _logger.Info("AIModuleTemplates: Found fractal-transform handler, calling it");
                        var result = await handler(requestElement);
                        _logger.Info($"AIModuleTemplates: fractal-transform handler returned: {result != null}");
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

            public async Task<string?> GenerateTextAsync(
                string prompt,
                string? model = null,
                string? provider = null)
            {
                try
                {
                    var request = new
                    {
                        Prompt = prompt,
                        Model = model,
                        Provider = provider
                    };

                    var requestJson = JsonSerializer.Serialize(request);
                    var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);

                    if (_apiRouter.TryGetHandler("ai", "generate-text", out var handler))
                    {
                        _logger.Info("AIModuleTemplates: Found generate-text handler, calling it");
                        var result = await handler(requestElement);
                        _logger.Info($"AIModuleTemplates: generate-text handler returned: {result != null}");
                        if (result != null)
                        {
                            // Parse the result from the AI module
                            var resultJson = JsonSerializer.Serialize(result);
                            var resultElement = JsonSerializer.Deserialize<JsonElement>(resultJson);
                            
                            if (resultElement.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                if (resultElement.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                                {
                                    return text.GetString();
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.Warn("AIModuleTemplates: No generate-text handler found");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"AIModuleTemplates: Error in GenerateTextAsync: {ex.Message}", ex);
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
            // _apiRouter will be set via RegisterApiHandlers in ModuleBase
            // _aiTemplates will be lazy-loaded when needed
            
            // Cross-module communicator will be initialized lazily
            
            // Initialize timers with cancellation-aware callbacks
            _ingestionTimer = new Timer(async _ => 
            {
                try 
                { 
                    await IngestNewsFromSources(null); 
                } 
                catch (OperationCanceledException) 
                { 
                    _logger.Info("News ingestion timer cancelled"); 
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
            
            _cleanupTimer = new Timer(_ => 
            {
                try 
                { 
                    CleanupOldNews(null); 
                } 
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException) 
                { 
                    _logger.Info("Cleanup timer cancelled or disposed"); 
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
            
            _memoryCleanupTimer = new Timer(_ => 
            {
                try 
                { 
                    CleanupMemory(null); 
                } 
                catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException) 
                { 
                    _logger.Info("Memory cleanup timer cancelled or disposed"); 
                }
            }, null, Timeout.Infinite, Timeout.Infinite);
            
            // Defer heavy work to InitializeAsync() to avoid blocking constructor/module load
        }

        /// <summary>
        /// Start automatic news ingestion - replaces manual ingestion endpoint
        /// </summary>
        private async Task StartAutomaticNewsIngestion()
        {
            try
            {
                _logger.Info("RealtimeNewsStreamModule: Starting automatic news ingestion");
                
                // Start timers first to ensure they're running even if initial ingestion fails
                var intervalMs = _ingestionIntervalMinutes * 60 * 1000;
                _ingestionTimer.Change(intervalMs, intervalMs);
                _logger.Info($"RealtimeNewsStreamModule: Started periodic ingestion every {_ingestionIntervalMinutes} minutes");
                
                // Start cleanup timer (run every 6 hours)
                _cleanupTimer.Change(TimeSpan.FromHours(6), TimeSpan.FromHours(6));
                _logger.Info("RealtimeNewsStreamModule: Started periodic cleanup every 6 hours");
                
                // Start memory cleanup timer (run every hour)
                _memoryCleanupTimer.Change(TimeSpan.FromMinutes(NEWS_CLEANUP_INTERVAL_MINUTES), TimeSpan.FromMinutes(NEWS_CLEANUP_INTERVAL_MINUTES));
                _logger.Info("RealtimeNewsStreamModule: Started periodic memory cleanup");
                
                // Run initial ingestion in background to avoid blocking initialization
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000); // Give system time to stabilize
                        var before = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE).Count();
                        await IngestNewsFromSources(null);
                        var after = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE).Count();
                        var added = Math.Max(0, after - before);
                        
                        _logger.Info($"RealtimeNewsStreamModule: Initial ingestion completed - Added: {added}, Total: {after}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"RealtimeNewsStreamModule: Initial ingestion failed: {ex.Message} - will retry on next timer cycle");
                    }
                });
                
                _logger.Info("RealtimeNewsStreamModule: Automatic news ingestion startup completed");
            }
            catch (Exception ex)
            {
                _logger.Error($"RealtimeNewsStreamModule: Failed to start automatic news ingestion: {ex.Message}");
            }
        }

        public override async Task InitializeAsync()
        {
            try
            {
                _logger.Info("RealtimeNewsStreamModule: InitializeAsync starting");
                
                // Apply compassionate timeout handling - don't let this block the entire system
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25)); // 25s timeout
                
                try
                {
                    // Wait for registry initialization to complete before accessing nodes
                    await WaitForRegistryInitializationAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Warn("RealtimeNewsStreamModule: Registry initialization timeout - proceeding with graceful degradation");
                    // Continue with minimal initialization rather than failing completely
                }
                
                // Load ontology axes with graceful fallback
                try
                {
                    LoadOntologyAxes();
                    if (_ontologyAxes.Count == 0)
                    {
                        _logger.Warn("No ontology axes found in U-CORE. Seeding minimal default axes.");
                        SeedDefaultOntologyAxes();
                        LoadOntologyAxes();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to load ontology axes: {ex.Message} - using defaults");
                    SeedDefaultOntologyAxes();
                }
                
                // Load configurations with graceful fallback
                try
                {
                    await _configManager.LoadConfigurationsAsync().WaitAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to load configurations for news sources: {ex.Message} - continuing without external sources");
                }
                
                // Start automatic news ingestion with timeout protection
                try
                {
                    await StartAutomaticNewsIngestion().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Warn("RealtimeNewsStreamModule: News ingestion startup timeout - will retry in background");
                    // Start a background task to retry later
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(5000); // Wait 5 seconds
                        try
                        {
                            await StartAutomaticNewsIngestion();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Background retry of news ingestion failed: {ex.Message}");
                        }
                    });
                }
                
                _logger.Info("RealtimeNewsStreamModule: InitializeAsync completed with graceful degradation");
            }
            catch (Exception ex)
            {
                _logger.Error($"RealtimeNewsStreamModule InitializeAsync failed: {ex.Message}");
                // Even on failure, ensure the module doesn't block system startup
                _logger.Info("RealtimeNewsStreamModule: Continuing with minimal functionality");
            }
        }

        public override void Register(INodeRegistry registry)
        {
            base.Register(registry);
            
            // Gate ingestion via environment flag (default: disabled)
            var enabledEnv = Environment.GetEnvironmentVariable("NEWS_INGESTION_ENABLED");
            // Consider both standard env vars; some hosts set only one
            var dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var isTesting = string.Equals(dotnetEnv, "Testing", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(aspnetEnv, "Testing", StringComparison.OrdinalIgnoreCase);
            // If explicitly enabled, honor it even in Testing to allow controlled runs
            var isExplicitlyEnabled = string.Equals(enabledEnv, "true", StringComparison.OrdinalIgnoreCase) || enabledEnv == "1";
            var ingestionEnabled = isExplicitlyEnabled;

            if (ingestionEnabled)
            {
                // Defer starting timers to RegisterApiHandlers after router is available
                var envLabel = dotnetEnv ?? aspnetEnv ?? "unknown";
                if (isTesting)
                {
                    _logger.Warn($"RealtimeNewsStreamModule ingestion enabled (deferred until API router ready) in Testing (env={envLabel})");
                }
                else
                {
                    _logger.Info($"RealtimeNewsStreamModule ingestion enabled (deferred until API router ready) (env={envLabel})");
                }
            }
            else
            {
                var envLabel = dotnetEnv ?? aspnetEnv ?? "unknown";
                _logger.Warn($"RealtimeNewsStreamModule ingestion disabled (env={envLabel}, NEWS_INGESTION_ENABLED={enabledEnv ?? "<null>"})");
            }
        }

        public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
        {
            _logger.Info("RTNSM RegisterApiHandlers ENTRY - Start of method");
            try
            {
                _logger.Info("Registering API handlers for Real-Time News Stream Module");
                // Ensure base stores the router
                base.RegisterApiHandlers(router, registry);
                
                // Initialize AI queue for intelligent news processing
                _aiQueue = base._serviceProvider?.GetService(typeof(AIRequestQueue)) as AIRequestQueue;
                if (_aiQueue != null)
                {
                    _logger.Info("AI queue initialized for intelligent news processing");
                }
                else
                {
                    _logger.Warn("AI queue not available - news processing will use fallback methods");
                }
                if (_apiRouter == null)
                {
                    _logger.Error("RTNSM: _apiRouter is null right after base.RegisterApiHandlers");
                    throw new InvalidOperationException("Router not set in base RegisterApiHandlers");
                }
                _logger.Info("RTNSM: _apiRouter set to non-null");
                
                // Check if ingestion is enabled before proceeding
                var enabledEnv = Environment.GetEnvironmentVariable("NEWS_INGESTION_ENABLED");
                _logger.Info($"RTNSM: NEWS_INGESTION_ENABLED={enabledEnv ?? "<null>"}");
                var isExplicitlyEnabled = string.Equals(enabledEnv, "true", StringComparison.OrdinalIgnoreCase) || enabledEnv == "1";
                _logger.Info($"RTNSM: isExplicitlyEnabled={isExplicitlyEnabled}");
                if (!isExplicitlyEnabled)
                {
                    _logger.Info("News ingestion is disabled, skipping timer setup");
                    return;
                }
                
                if (_aiTemplates == null)
                {
                    _aiTemplates = new AIModuleTemplates(_apiRouter, _logger);
                    _logger.Info($"RealtimeNewsStreamModule: _aiTemplates initialized to {(_aiTemplates != null ? "non-null" : "null")}");
                }

                // Verify AI handlers are available; fail fast if missing
                var missing = new List<string>();
                if (!_apiRouter.TryGetHandler("ai", "extract-concepts", out _)) missing.Add("extract-concepts");
                if (!_apiRouter.TryGetHandler("ai", "score-analysis", out _)) missing.Add("score-analysis");
                if (!_apiRouter.TryGetHandler("ai", "fractal-transform", out _)) missing.Add("fractal-transform");
                if (missing.Count > 0)
                {
                    _logger.Error($"AI handlers missing: {string.Join(", ", missing)}. News analysis requires AI; ingestion will error until available.");
                }

                // Start timers and initial ingestion now that router is ready
                var delayEnv = Environment.GetEnvironmentVariable("NEWS_INGESTION_START_DELAY_SEC");
                int delaySeconds = 120;
                if (!string.IsNullOrWhiteSpace(delayEnv) && int.TryParse(delayEnv, out var parsedDelay) && parsedDelay >= 0)
                {
                    delaySeconds = parsedDelay;
                }

                _ingestionTimer.Change(TimeSpan.FromSeconds(delaySeconds), TimeSpan.FromMinutes(_ingestionIntervalMinutes));
                _cleanupTimer.Change(TimeSpan.FromHours(1), TimeSpan.FromHours(_cleanupIntervalHours));
                _memoryCleanupTimer.Change(TimeSpan.FromMinutes(NEWS_CLEANUP_INTERVAL_MINUTES), TimeSpan.FromMinutes(NEWS_CLEANUP_INTERVAL_MINUTES));
                
                _logger.Info($"RealtimeNewsStreamModule: Timers started with delay {delaySeconds}s");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in RegisterApiHandlers: {ex.Message}", ex);
                throw;
            }
        }

        public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            _logger.Info("Registering HTTP endpoints for Real-Time News Stream Module");
            // HTTP endpoints are registered via ApiRoute attributes
        }

        // Manual ingestion endpoint used by integration tests and tooling
        [ApiRoute("POST", "/news/ingest", "Ingest News Item (Compat)", "Ingest a single news item payload and run the pipeline (compat)", "realtime-news-stream")]
        public async Task<object> IngestSingleNewsItem([ApiParameter("body", "News item payload", Required = true, Location = "body")] JsonElement payload)
        {
            try
            {
                // Convert payload to NewsItem with robust parsing
                var newsItem = new NewsItem
                {
                    Id = payload.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? $"test-news-{Guid.NewGuid():N}" : $"test-news-{Guid.NewGuid():N}",
                    Title = payload.TryGetProperty("title", out var tEl) ? (tEl.GetString() ?? "") : "",
                    Content = payload.TryGetProperty("content", out var cEl) ? (cEl.GetString() ?? "") : "",
                    Source = payload.TryGetProperty("source", out var sEl) ? (sEl.GetString() ?? "Unknown") : "Unknown",
                    Url = payload.TryGetProperty("url", out var uEl) ? (uEl.GetString() ?? "") : "",
                    PublishedAt = payload.TryGetProperty("publishedAt", out var pEl) && pEl.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(pEl.GetString(), out var pdt)
                        ? pdt : DateTimeOffset.UtcNow,
                    Tags = payload.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array ? tagsEl.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() : Array.Empty<string>(),
                    Metadata = payload.TryGetProperty("metadata", out var mEl) && mEl.ValueKind == JsonValueKind.Object
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(mEl.GetRawText()) ?? new Dictionary<string, object>()
                        : new Dictionary<string, object>()
                };

                await ProcessNewsItem(newsItem);
                return new { success = true, id = newsItem.Id, message = "Ingestion queued" };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ingesting single news item: {ex.Message}", ex);
                return new ErrorResponse($"Failed to ingest news item: {ex.Message}");
            }
        }

        // Removed manual ingestion endpoint - news ingestion now runs automatically in background

        public void Unregister()
        {
            _logger.Info("Unregistering Real-Time News Stream Module");
            
            // Signal shutdown to all operations
            _shutdownCts.Cancel();
            
            // Stop the timers immediately
            _ingestionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _memoryCleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Wait for any running operations to complete with timeout
            try
            {
                if (_semaphore?.Wait(TimeSpan.FromSeconds(3)) == true)
                {
                    _logger.Info("Real-Time News Stream Module unregistered gracefully - operations completed");
                }
                else
                {
                    _logger.Warn("Real-Time News Stream Module unregistered with timeout - some operations may still be running");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during module unregistration: {ex.Message}");
            }
            finally
            {
                _semaphore?.Release();
            }
            
            _logger.Info("Real-Time News Stream Module unregistered");
        }


        private void InitializeNewsSources()
        {
            // Deprecated: hard-coded sources removed. Sources are loaded exclusively from news-sources.json via ConfigurationManager.
            _logger.Info("InitializeNewsSources called; no-op as sources are config-driven.");
            return;
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
            // Check for shutdown before starting
            if (_shutdownCts.Token.IsCancellationRequested)
            {
                _logger.Info("News ingestion cancelled - system shutting down");
                return;
            }
            
            await _semaphore.WaitAsync(_shutdownCts.Token);
            try
            {
                _logger.Info("Starting news ingestion from all sources");
                
                // Check again after acquiring semaphore
                if (_shutdownCts.Token.IsCancellationRequested)
                {
                    _logger.Info("News ingestion cancelled after semaphore acquisition");
                    return;
                }

                var allSourceNodes = Registry.GetNodesByType(NEWS_SOURCE_NODE_TYPE);
                var sourceNodes = allSourceNodes
                    .Where(n => n.Meta?.ContainsKey("isActive") == true && (bool)n.Meta["isActive"])
                    .ToList();

                _logger.Info($"Processing {sourceNodes.Count} active news sources in parallel");

                // Process sources in parallel with controlled concurrency and telemetry
                var semaphoreSlim = new SemaphoreSlim(10, 10); // Allow max 10 concurrent requests
                var tasks = sourceNodes.Select(async sourceNode =>
                {
                    await semaphoreSlim.WaitAsync();
                    try
                    {
                        var source = JsonSerializer.Deserialize<NewsSource>(sourceNode.Content?.InlineJson ?? "{}");
                        if (source != null)
                        {
                            var startedAt = DateTime.UtcNow;
                            var beforeCount = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE).Count();
                            if (source.Type == "rss")
                            {
                                await IngestRssFeed(source);
                            }
                            else if (source.Type == "api")
                            {
                                await IngestApiFeed(source);
                            }
                            var duration = DateTime.UtcNow - startedAt;
                            var afterCount = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE).Count();
                            var ingested = Math.Max(0, afterCount - beforeCount);
                            _logger.Info($"Source '{source.Name}' completed in {duration.TotalSeconds:F1}s, items: {ingested}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error ingesting from source {sourceNode.Meta?["name"]}: {ex.Message}", ex);
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                });

                // Wait for all tasks to complete with timeout
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(10)); // 10-minute timeout for all sources
                var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _logger.Warn("News ingestion timed out after 10 minutes");
                }
                else
                {
                _logger.Info("Completed news ingestion cycle");
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
            const int maxRetries = 3;
            const int baseDelayMs = 1000;
            
            _logger.Info($"🔄 Starting RSS ingestion for source: {source.Name} ({source.Url})");
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    _logger.Debug($"📡 Attempt {attempt + 1}/{maxRetries} for source {source.Name}");
                    
                    // Create HttpClient with proper headers to avoid bot detection
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LivingCodex/1.0 (+https://livingcodex.org)");
                    httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml, text/xml, */*;q=0.1");
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Living-Codex-NewsBot/1.0 (https://living-codex.com)");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/rss+xml, application/xml, text/xml");
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    
                    // Get the RSS content with proper error handling
                    var startTime = DateTime.UtcNow;
                    var response = await httpClient.GetAsync(source.Url);
                    var requestDuration = DateTime.UtcNow - startTime;
                    
                    _logger.Debug($"📊 HTTP request for {source.Name} completed in {requestDuration.TotalMilliseconds:F0}ms - Status: {response.StatusCode}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                            _logger.Warn($"RSS feed {source.Name} is blocked (403). This may be due to rate limiting or bot detection. Skipping this source.");
                            return;
                        }
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.Warn($"RSS feed {source.Name} rate limited (429). Skipping this source without retry.");
                            return;
                        }
                        
                        if ((int)response.StatusCode == 301 || (int)response.StatusCode == 302 || (int)response.StatusCode == 307 || (int)response.StatusCode == 308)
                        {
                            var redirect = response.Headers.Location?.ToString();
                            if (!string.IsNullOrWhiteSpace(redirect))
                            {
                                _logger.Warn($"RSS feed {source.Name} redirected. Following to {redirect}");
                                var redirected = await httpClient.GetAsync(redirect);
                                if (redirected.IsSuccessStatusCode)
                                {
                                    response = redirected;
                                }
                                else
                                {
                                    _logger.Warn($"Redirected URL for {source.Name} returned status {redirected.StatusCode}. Skipping this source.");
                                    return;
                                }
                            }
                            else
                            {
                                _logger.Warn($"RSS feed {source.Name} returned redirect without Location header. Skipping this source.");
                                return;
                            }
                        }
                        else
                        {
                            _logger.Warn($"RSS feed {source.Name} returned status {response.StatusCode}. Skipping this source.");
                            return;
                        }
                    }
                    
                    var content = await response.Content.ReadAsStringAsync();
                    // If server responded with HTML, try to discover RSS link in HTML head
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                    if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                    {
                        var discovered = ExtractRssLinkFromHtml(content);
                        if (!string.IsNullOrWhiteSpace(discovered))
                        {
                            _logger.Warn($"{source.Name} returned HTML; discovered RSS link {discovered}. Following.");
                            response = await httpClient.GetAsync(discovered);
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger.Warn($"Discovered RSS link for {source.Name} returned status {response.StatusCode}. Skipping this source.");
                                return;
                            }
                            content = await response.Content.ReadAsStringAsync();
                        }
                    }
                    // Secure XML settings: ignore DTDs, no external resolution
                    var xmlSettings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Ignore,
                        XmlResolver = null
                    };
                    try
                    {
                        using var reader = XmlReader.Create(new StringReader(content), xmlSettings);
                        var feed = SyndicationFeed.Load(reader);
                        var feedItems = feed.Items?.ToList() ?? new List<SyndicationItem>();
                        _logger.Info($"RSS feed {source.Name} parsed {feedItems.Count} items (processing up to {_maxItemsPerSource}).");
                        
                        foreach (var item in feedItems.Take(_maxItemsPerSource))
                        {
                            var publish = item.PublishDate == default ? DateTimeOffset.UtcNow : item.PublishDate;
                            var deterministicIdSeed = !string.IsNullOrEmpty(item.Id)
                                ? item.Id
                                : (item.Links.FirstOrDefault()?.Uri?.ToString() ?? (item.Title?.Text ?? ""));
                            var stableId = ComputeDeterministicId($"rss:{source.Id}:{deterministicIdSeed}");
                            
                            var newsItem = new NewsItem
                            {
                                Id = $"rss-{source.Id}-{stableId}",
                                Title = System.Net.WebUtility.HtmlDecode(item.Title?.Text ?? ""),
                                Content = System.Net.WebUtility.HtmlDecode(item.Summary?.Text ?? ""),
                                Source = source.Name,
                                Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                                PublishedAt = publish,
                                Tags = ExtractTagsFromContent(System.Net.WebUtility.HtmlDecode(item.Title?.Text ?? "") + " " + System.Net.WebUtility.HtmlDecode(item.Summary?.Text ?? "")),
                                Metadata = new Dictionary<string, object>
                                {
                                    ["sourceType"] = "RSS",
                                    ["sourceId"] = source.Id,
                                    ["rssId"] = item.Id,
                                    ["sharedMetadataRef"] = "shared-metadata-news-source-types"
                                }
                            };
                            
                            await ProcessNewsItem(newsItem);
                        }
                        
                        // Success - break out of retry loop
                        break;
                    }
                    catch (XmlException xex)
                    {
                        // Try RDF (RSS 1.0) fallback
                        if (content.Contains("rdf:RDF", StringComparison.OrdinalIgnoreCase) || content.Contains("http://www.w3.org/1999/02/22-rdf-syntax-ns#", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.Warn($"{source.Name} appears to use RDF/RSS1.0. Falling back to RDF parser: {xex.Message}");
                            var rdfItems = ParseRdfRss(content);
                            _logger.Info($"RDF feed {source.Name} parsed {rdfItems.Count} items (processing up to {_maxItemsPerSource}).");
                            foreach (var ri in rdfItems.Take(_maxItemsPerSource))
                            {
                                var stableId = ComputeDeterministicId($"rdf:{source.Id}:{ri.Link ?? ri.Title}");
                                var newsItem = new NewsItem
                                {
                                    Id = $"rss-{source.Id}-{stableId}",
                                    Title = System.Net.WebUtility.HtmlDecode(ri.Title ?? ""),
                                    Content = System.Net.WebUtility.HtmlDecode(ri.Description ?? ""),
                                    Source = source.Name,
                                    Url = ri.Link ?? "",
                                    PublishedAt = ri.PublishDate ?? DateTimeOffset.UtcNow,
                                    Tags = ExtractTagsFromContent(System.Net.WebUtility.HtmlDecode(ri.Title ?? "") + " " + System.Net.WebUtility.HtmlDecode(ri.Description ?? "")),
                                    Metadata = new Dictionary<string, object> 
                                    { 
                                        ["sourceType"] = "RSS", 
                                        ["sourceId"] = source.Id,
                                        ["sharedMetadataRef"] = "shared-metadata-news-source-types"
                                    }
                                };
                                await ProcessNewsItem(newsItem);
                            }
                            break;
                        }
                        else
                        {
                            _logger.Warn($"{source.Name} returned unsupported feed format: {xex.Message}");
                            return;
                        }
                    }
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

        // Simple RDF (RSS 1.0) parser extracting minimal fields
        private List<(string? Title, string? Link, string? Description, DateTimeOffset? PublishDate)> ParseRdfRss(string xml)
        {
            var list = new List<(string?, string?, string?, DateTimeOffset?)>();
            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
                XNamespace rss = "http://purl.org/rss/1.0/";
                XNamespace dc = "http://purl.org/dc/elements/1.1/";
                foreach (var item in doc.Descendants(rss + "item"))
                {
                    var title = item.Element(rss + "title")?.Value;
                    var link = item.Element(rss + "link")?.Value;
                    var desc = item.Element(rss + "description")?.Value;
                    DateTimeOffset? pub = null;
                    var dcDate = item.Element(dc + "date")?.Value;
                    if (DateTimeOffset.TryParse(dcDate, out var p)) pub = p;
                    list.Add((title, link, desc, pub));
                }
            }
            catch { /* ignore */ }
            return list;
        }

        // Attempt to extract an RSS/Atom link from an HTML page (common when feeds redirect to landing pages)
        private string? ExtractRssLinkFromHtml(string html)
        {
            try
            {
                // Very lightweight discovery using regex; adequate for common <link rel="alternate" type="application/rss+xml" href="...">
                var pattern = "<link[^>]+rel=\\\"alternate\\\"[^>]+type=\\\"(application/(?:rss|atom)\\+xml|application/xml|text/xml)\\\"[^>]+href=\\\"([^\\\"]+)\\\"";
                var match = System.Text.RegularExpressions.Regex.Match(html, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch { }
            return null;
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
                                Title = System.Net.WebUtility.HtmlDecode(story.Title),
                                Content = System.Net.WebUtility.HtmlDecode(story.Text ?? ""),
                                Source = source.Name,
                                Url = story.Url,
                                PublishedAt = DateTimeOffset.FromUnixTimeSeconds(story.Time),
                                Tags = ExtractTagsFromContent(System.Net.WebUtility.HtmlDecode(story.Title) + " " + System.Net.WebUtility.HtmlDecode(story.Text ?? "")),
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

                // Precompute deterministic news node id for consistent child edges
                var newsNodeId = $"codex.news.item.{newsItem.Id}.{Guid.NewGuid():N}";

                // Step 1: Extract full content from URL using the new ContentExtractionModule
                var contentExtractionModule = new ContentExtractionModule(_registry, _logger, _httpClient);
                var extractionResult = await contentExtractionModule.ExtractContentFromUrl(newsItem.Url, useHeadlessBrowser: false);

                if (!extractionResult.Success || string.IsNullOrEmpty(extractionResult.Content))
                {
                    _logger.Warn($"Failed to extract content from {newsItem.Url}, using fallback");
                    extractionResult = new ContentExtractionResult(
                        $"{newsItem.Title}\n\n{newsItem.Content}",
                        "text/plain",
                        true,
                        "fallback",
                        new Dictionary<string, object>()
                    );
                }

                var contentNode = await CreateContentNode(newsItem, extractionResult.Content, newsNodeId);

                // Step 2: Generate summary from extracted content (with AI queue integration)
                var summary = await GenerateSummaryWithAI(newsItem, extractionResult.Content);
                var summaryNode = await CreateSummaryNode(newsItem, summary, contentNode.Id, newsNodeId);

                // Step 3: Extract concepts from summary (with AI queue integration)
                var concepts = await ExtractConceptsWithAI(newsItem, summary);
                var conceptNodes = await CreateConceptNodes(newsItem, concepts, summaryNode.Id, newsNodeId);

                // Step 4: For each concept, find path to U-Core and add missing concepts (with fallback)
                var ucorePaths = await FindPathsToUCoreConcepts(concepts);
                var allPathConcepts = await EnsureMissingConceptsInPaths(ucorePaths);

                // Step 5: Create comprehensive edge network for navigation (with fallback)
                await CreateComprehensiveEdgeNetwork(newsNodeId, newsItem, contentNode, summaryNode, conceptNodes, ucorePaths, allPathConcepts);

                // Step 6: Store original news item as node with links to all pipeline stages
                var newsNode = new Node(
                    Id: newsNodeId,
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
                        ["publishedAt"] = newsItem.PublishedAt.UtcDateTime,
                        ["url"] = newsItem.Url,
                        ["contentNodeId"] = contentNode.Id,
                        ["summaryNodeId"] = summaryNode.Id,
                        ["conceptNodeIds"] = conceptNodes.Select(c => c.Id).ToArray(),
                        ["ucorePathIds"] = ucorePaths.SelectMany(p => p.ConceptIds).ToArray(),
                        ["pipelineVersion"] = "2.0"
                    }
                );

                Registry.Upsert(newsNode);
                // Ensure source node exists and link back to it
                var sourceNodeId = await ResolveOrCreateSourceNode(newsItem);
                Registry.Upsert(new Edge(
                    newsNode.Id,
                    sourceNodeId,
                    "from_source",
                    1.0,
                    new Dictionary<string, object>
                    {
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["relationship"] = "news-from-source"
                    }
                ));
                _logger.Info($"Successfully stored news item as node: {newsNode.Id} - {newsItem.Title}");

                // Step 7: Ensure node has path back to core identity
                await EnsureNodePathToIdentity(newsNode.Id);

                // Create fractal news item
                var fractalNews = await CreateFractalNewsItem(newsItem);
                
                if (fractalNews != null)
                {
                    // Store fractal news as node
                    var fractalNode = new Node(
                        Id: $"codex.news.fractal.{fractalNews.Id}.{Guid.NewGuid():N}",
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
                            ["processedAt"] = fractalNews.ProcessedAt,
                            ["source"] = newsItem.Source,
                            ["originalNewsTitle"] = newsItem.Title,
                            ["originalNewsUrl"] = newsItem.Url
                        }
                    );

                    Registry.Upsert(fractalNode);

                    // Create edge from original news to fractal news (using canonical newsNodeId)
                    Registry.Upsert(new Edge(newsNodeId, fractalNode.Id, "analyzed-as", 1.0, new Dictionary<string, object>
                    {
                        ["source"] = newsItem.Source,
                        ["analyzedAt"] = DateTimeOffset.UtcNow
                    }));
                    _logger.Info($"NEWS_LINK_CREATED from={newsNodeId} to={fractalNode.Id} type=analyzed-as source={newsItem.Source}");

                    // Extract concepts and ensure ontology links
                    var fractalConcepts = await ExtractConceptsForNews(newsItem, fractalNews);
                    foreach (var concept in fractalConcepts)
                    {
                        EnsureConceptAndTopology(concept, newsItem, fractalNews);
                    }

                    _logger.Info($"Processed news item: {newsItem.Title} from {newsItem.Source} (concepts: {fractalConcepts.Count})");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error processing news item {newsItem.Title}: {ex.Message}", ex);
            }
        }

        private async Task<string> ResolveOrCreateSourceNode(NewsItem newsItem)
        {
            try
            {
                var sourceKey = (newsItem.Source ?? "Unknown").Trim().ToLowerInvariant().Replace(" ", "-");
                var sourceNodeId = $"news-source-{sourceKey}";
                var existing = Registry.GetNode(sourceNodeId);
                if (existing != null) return sourceNodeId;

                var node = new Node(
                    Id: sourceNodeId,
                    TypeId: "codex.news.source",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: newsItem.Source ?? "Unknown",
                    Description: $"News source for {newsItem.Source}",
                    Content: NodeHelpers.CreateJsonContent(new { name = newsItem.Source, homepage = newsItem.Url }),
                    Meta: new Dictionary<string, object> { ["name"] = newsItem.Source ?? "Unknown" }
                );
                Registry.Upsert(node);
                return sourceNodeId;
            }
            catch
            {
                return "news-source-unknown";
            }
        }

        private async Task<Node> CreateContentNode(NewsItem newsItem, string extractedContent, string newsNodeId)
        {
            var contentNode = new Node(
                Id: $"codex.content.extracted.{newsItem.Id}.{Guid.NewGuid():N}",
                TypeId: "codex.content.extracted",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Extracted Content: {newsItem.Title}",
                Description: $"Full content extracted from {newsItem.Url}",
                Content: new ContentRef(
                    MediaType: "text/plain",
                    InlineJson: extractedContent,
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["originalNewsId"] = newsItem.Id,
                    ["sourceUrl"] = newsItem.Url,
                    ["extractedAt"] = DateTimeOffset.UtcNow,
                    ["contentLength"] = extractedContent.Length,
                    ["wordCount"] = extractedContent.Split(' ').Length
                }
            );
            
            Registry.Upsert(contentNode);
            
            // Create edge news -> content
            Registry.Upsert(new Edge(
                newsNodeId,
                contentNode.Id,
                "has-content",
                1.0,
                new Dictionary<string, object>
                {
                    ["extractedAt"] = DateTimeOffset.UtcNow,
                    ["contentLength"] = extractedContent.Length,
                    ["edgeType"] = "pipeline-stage"
                }
            ));

            // Link content to source
            var sourceNodeId = await ResolveOrCreateSourceNode(newsItem);
            Registry.Upsert(new Edge(
                contentNode.Id,
                sourceNodeId,
                "from_source",
                0.9,
                new Dictionary<string, object> { ["createdAt"] = DateTimeOffset.UtcNow }
            ));
            
            return contentNode;
        }

        private async Task<Node> CreateSummaryNode(NewsItem newsItem, string summary, string contentNodeId, string newsNodeId)
        {
            var summaryNode = new Node(
                Id: $"codex.content.summary.{newsItem.Id}.{Guid.NewGuid():N}",
                TypeId: "codex.content.summary",
                State: ContentState.Water,
                Locale: "en-US",
                Title: $"Summary: {newsItem.Title}",
                Description: $"AI-generated summary of {newsItem.Title}",
                Content: new ContentRef(
                    MediaType: "text/plain",
                    InlineJson: summary,
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["originalNewsId"] = newsItem.Id,
                    ["contentNodeId"] = contentNodeId,
                    ["generatedAt"] = DateTimeOffset.UtcNow,
                    ["summaryLength"] = summary.Length,
                    ["wordCount"] = summary.Split(' ').Length
                }
            );
            
            Registry.Upsert(summaryNode);
            
            // content -> summary
            Registry.Upsert(new Edge(
                contentNodeId, 
                summaryNode.Id, 
                "summarized-as", 
                1.0, 
                new Dictionary<string, object>
                {
                    ["generatedAt"] = DateTimeOffset.UtcNow,
                    ["summaryLength"] = summary.Length,
                    ["edgeType"] = "pipeline-stage"
                }
            ));
            
            // news -> summary
            Registry.Upsert(new Edge(
                newsNodeId, 
                summaryNode.Id, 
                "has-summary", 
                1.0, 
                new Dictionary<string, object>
                {
                    ["generatedAt"] = DateTimeOffset.UtcNow,
                    ["edgeType"] = "pipeline-stage"
                }
            ));

            // Link summary to source
            var sourceNodeId = await ResolveOrCreateSourceNode(newsItem);
            Registry.Upsert(new Edge(
                summaryNode.Id,
                sourceNodeId,
                "from_source",
                0.8,
                new Dictionary<string, object> { ["createdAt"] = DateTimeOffset.UtcNow }
            ));
            
            return summaryNode;
        }

        private async Task<List<Node>> CreateConceptNodes(NewsItem newsItem, List<string> concepts, string summaryNodeId, string newsNodeId)
        {
            var conceptNodes = new List<Node>();
            
            foreach (var concept in concepts)
            {
                // Expand single-word concepts into more specific multi-word phrases using simple heuristics
                var enrichedConcept = concept.Trim();
                if (enrichedConcept.Split(' ').Length == 1 && !string.IsNullOrWhiteSpace(newsItem.Title))
                {
                    enrichedConcept = $"{enrichedConcept} in {newsItem.Title}";
                }

                var conceptNode = new Node(
                    Id: $"codex.concept.extracted.{newsItem.Id}.{concept.ToLowerInvariant().Replace(" ", "_")}.{Guid.NewGuid():N}",
                    TypeId: "codex.concept.extracted",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: enrichedConcept,
                    Description: $"Concept extracted from {newsItem.Title}",
                    Content: new ContentRef(
                        MediaType: "text/plain",
                        InlineJson: enrichedConcept,
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["originalNewsId"] = newsItem.Id,
                        ["summaryNodeId"] = summaryNodeId,
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["conceptType"] = "extracted",
                        ["source"] = newsItem.Source
                    }
                );
                
                Registry.Upsert(conceptNode);
                conceptNodes.Add(conceptNode);
                
                // Create edges: summary -> concept and news -> concept
                Registry.Upsert(new Edge(
                    summaryNodeId, 
                    conceptNode.Id, 
                    "contains-concept", 
                    1.0, 
                    new Dictionary<string, object>
                    {
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["concept"] = enrichedConcept,
                        ["edgeType"] = "pipeline-stage"
                    }
                ));
                
                Registry.Upsert(new Edge(
                    newsNodeId, 
                    conceptNode.Id, 
                    "relates-to", 
                    1.0, 
                    new Dictionary<string, object>
                    {
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["concept"] = enrichedConcept
                    }
                ));

                // Link concept to source
                var sourceNodeId = await ResolveOrCreateSourceNode(newsItem);
                Registry.Upsert(new Edge(
                    conceptNode.Id,
                    sourceNodeId,
                    "from_source",
                    0.7,
                    new Dictionary<string, object> { ["createdAt"] = DateTimeOffset.UtcNow }
                ));
            }
            
            return conceptNodes;
        }

        private async Task CreateComprehensiveEdgeNetwork(
            string newsNodeId,
            NewsItem newsItem, 
            Node contentNode, 
            Node summaryNode, 
            List<Node> conceptNodes, 
            List<UCorePath> ucorePaths, 
            List<Node> allPathConcepts)
        {
            try
            {
                _logger.Info($"Creating comprehensive edge network for news item: {newsItem.Title}");
                
                // 1. News -> Content (already created in CreateContentNode)
                
                // 2. Content -> Summary (already created in CreateSummaryNode)
                
                // 3. Summary -> Concepts (already created in CreateConceptNodes)
                
                // 4. Concepts -> U-Core Paths (only via explicit path, no shortcuts)
                foreach (var path in ucorePaths)
                {
                    for (int i = 0; i < path.ConceptIds.Count - 1; i++)
                    {
                        var sourceConcept = path.ConceptIds[i];
                        var targetConcept = path.ConceptIds[i + 1];
                        
                        var sourceNode = allPathConcepts.FirstOrDefault(n => 
                            n.Meta?.GetValueOrDefault("conceptId")?.ToString() == sourceConcept);
                        var targetNode = allPathConcepts.FirstOrDefault(n => 
                            n.Meta?.GetValueOrDefault("conceptId")?.ToString() == targetConcept);
                        
                        if (sourceNode != null && targetNode != null)
                        {
                            Registry.Upsert(new Edge(
                                sourceNode.Id,
                                targetNode.Id,
                                "is_a",
                                path.PathStrength,
                                new Dictionary<string, object>
                                {
                                    ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                                    ["pathPosition"] = i,
                                    ["source"] = newsItem.Source,
                                    ["createdAt"] = DateTimeOffset.UtcNow,
                                    ["edgeType"] = "ucore-path"
                                }
                            ));
                            // Reverse edge for navigation
                            Registry.Upsert(new Edge(
                                targetNode.Id,
                                sourceNode.Id,
                                "has_child",
                                path.PathStrength,
                                new Dictionary<string, object>
                                {
                                    ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                                    ["pathPosition"] = i,
                                    ["source"] = newsItem.Source,
                                    ["createdAt"] = DateTimeOffset.UtcNow,
                                    ["edgeType"] = "ucore-path"
                                }
                            ));
                        }
                    }
                }
                
                _logger.Info($"Created comprehensive edge network with {ucorePaths.Count} U-Core paths");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating comprehensive edge network: {ex.Message}", ex);
            }
        }

        // Enhanced News Processing Pipeline Methods
        
        /// <summary>
        /// Find paths from extracted concepts to U-Core concepts
        /// </summary>
        private async Task<List<UCorePath>> FindPathsToUCoreConcepts(List<string> concepts)
        {
            var paths = new List<UCorePath>();
            
            try
            {
                _logger.Info($"Finding U-Core paths for {concepts.Count} concepts");
                
                foreach (var concept in concepts)
                {
                    var path = await FindPathToUCoreConcept(concept);
                    if (path != null)
                    {
                        paths.Add(path);
                        _logger.Info($"Found U-Core path for '{concept}': {path.ConceptIds.Count} concepts");
                    }
                }
                
                return paths;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error finding U-Core paths: {ex.Message}", ex);
                return paths;
            }
        }
        
        /// <summary>
        /// Find a single path from a concept to U-Core concepts
        /// </summary>
        private async Task<UCorePath?> FindPathToUCoreConcept(string concept)
        {
            try
            {
                // Get U-Core ontology
                var ontology = new UCoreOntology();
                var coreConcepts = ontology.Concepts.Values.Where(c => c.Type == ConceptType.Core).ToList();
                
                // Use AI to find semantic relationships
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var prompt = $"Find a conceptual path from '{concept}' to U-Core concepts. " +
                           $"U-Core core concepts: {string.Join(", ", coreConcepts.Select(c => c.Name))}. " +
                           $"Respond with a JSON array of concept names that form a logical path, " +
                           $"starting with '{concept}' and ending with a U-Core core concept. " +
                           $"Include intermediate concepts that logically connect them.";
                
                _logger.Info($"AI_PRECALL find-ucore-path provider={provider} model={model} concept={concept}");
                
                var response = await _aiTemplates?.GenerateTextAsync(prompt, model, provider);
                if (response != null && !string.IsNullOrWhiteSpace(response))
                {
                    // Parse AI response to extract concept path
                    var pathConcepts = ParseConceptPathFromAI(response, concept, coreConcepts);
                    if (pathConcepts.Count > 1)
                    {
                        return new UCorePath
                        {
                            SourceConcept = concept,
                            TargetConcept = pathConcepts.Last(),
                            ConceptIds = pathConcepts,
                            PathStrength = CalculatePathStrength(pathConcepts),
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                    }
                }
                
                // Fallback: direct connection to closest U-Core concept
                var closestCore = FindClosestUCoreConcept(concept, coreConcepts);
                if (closestCore != null)
                {
                    return new UCorePath
                    {
                        SourceConcept = concept,
                        TargetConcept = closestCore.Name,
                        ConceptIds = new List<string> { concept, closestCore.Name },
                        PathStrength = 0.5, // Lower strength for fallback
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error finding U-Core path for '{concept}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Ensure all concepts in paths exist as nodes, creating missing ones
        /// </summary>
        private async Task<List<Node>> EnsureMissingConceptsInPaths(List<UCorePath> paths)
        {
            var createdNodes = new List<Node>();
            
            try
            {
                _logger.Info($"Ensuring {paths.Count} U-Core paths have all required concepts");
                
                foreach (var path in paths)
                {
                    foreach (var conceptId in path.ConceptIds)
                    {
                        // Check if concept node already exists
                        var existingNode = Registry.GetNodesByType("codex.concept")
                            .FirstOrDefault(n => n.Meta?.GetValueOrDefault("conceptId")?.ToString() == conceptId);
                        
                        if (existingNode == null)
                        {
                            // Create missing concept node
                            var conceptNode = await CreateConceptNode(conceptId, path);
                            if (conceptNode != null)
                            {
                                Registry.Upsert(conceptNode);
                                createdNodes.Add(conceptNode);
                                _logger.Info($"Created missing concept node: {conceptId}");
                            }
                        }
                        else
                        {
                            createdNodes.Add(existingNode);
                        }
                    }
                }
                
                return createdNodes;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ensuring missing concepts: {ex.Message}", ex);
                return createdNodes;
            }
        }
        
        /// <summary>
        /// Create comprehensive edge network connecting all pipeline stages
        /// </summary>
        private async Task CreateComprehensiveEdgeNetwork(
            NewsItem newsItem, 
            Node contentNode, 
            Node summaryNode, 
            List<Node> conceptNodes, 
            List<UCorePath> ucorePaths, 
            List<Node> allPathConcepts)
        {
            try
            {
                _logger.Info($"Creating comprehensive edge network for news item: {newsItem.Title}");
                
                var newsNodeId = $"codex.news.item.{newsItem.Id}";
                
                // 1. News -> Content
                Registry.Upsert(new Edge(
                    newsNodeId,
                    contentNode.Id,
                    "has-content",
                    1.0,
                    new Dictionary<string, object>
                    {
                        ["source"] = newsItem.Source,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["edgeType"] = "pipeline-stage"
                    }
                ));
                
                // 2. Content -> Summary
                Registry.Upsert(new Edge(
                    contentNode.Id,
                    summaryNode.Id,
                    "summarized-as",
                    1.0,
                    new Dictionary<string, object>
                    {
                        ["source"] = newsItem.Source,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["edgeType"] = "pipeline-stage"
                    }
                ));
                
                // 3. Summary -> Concepts
                foreach (var conceptNode in conceptNodes)
                {
                    Registry.Upsert(new Edge(
                        summaryNode.Id,
                        conceptNode.Id,
                        "contains-concept",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["source"] = newsItem.Source,
                            ["createdAt"] = DateTimeOffset.UtcNow,
                            ["edgeType"] = "pipeline-stage"
                        }
                    ));
                }
                
                // 4. Concepts -> U-Core Paths
                foreach (var path in ucorePaths)
                {
                    for (int i = 0; i < path.ConceptIds.Count - 1; i++)
                    {
                        var sourceConcept = path.ConceptIds[i];
                        var targetConcept = path.ConceptIds[i + 1];
                        
                        var sourceNode = allPathConcepts.FirstOrDefault(n => 
                            n.Meta?.GetValueOrDefault("conceptId")?.ToString() == sourceConcept);
                        var targetNode = allPathConcepts.FirstOrDefault(n => 
                            n.Meta?.GetValueOrDefault("conceptId")?.ToString() == targetConcept);
                        
                        if (sourceNode != null && targetNode != null)
                        {
                            Registry.Upsert(new Edge(
                                sourceNode.Id,
                                targetNode.Id,
                                "leads-to",
                                path.PathStrength,
                                new Dictionary<string, object>
                                {
                                    ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                                    ["pathPosition"] = i,
                                    ["source"] = newsItem.Source,
                                    ["createdAt"] = DateTimeOffset.UtcNow,
                                    ["edgeType"] = "ucore-path"
                                }
                            ));
                        }
                    }
                }
                
                // 5. News -> U-Core Paths (direct connections)
                foreach (var path in ucorePaths)
                {
                    var firstConceptNode = allPathConcepts.FirstOrDefault(n => 
                        n.Meta?.GetValueOrDefault("conceptId")?.ToString() == path.ConceptIds.First());
                    var lastConceptNode = allPathConcepts.FirstOrDefault(n => 
                        n.Meta?.GetValueOrDefault("conceptId")?.ToString() == path.ConceptIds.Last());
                    
                    if (firstConceptNode != null)
                    {
                        Registry.Upsert(new Edge(
                            newsNodeId,
                            firstConceptNode.Id,
                            "connects-to-ucore-via",
                            path.PathStrength,
                            new Dictionary<string, object>
                            {
                                ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                                ["source"] = newsItem.Source,
                                ["createdAt"] = DateTimeOffset.UtcNow,
                                ["edgeType"] = "news-to-ucore"
                            }
                        ));
                    }
                    
                    if (lastConceptNode != null)
                    {
                        Registry.Upsert(new Edge(
                            lastConceptNode.Id,
                            newsNodeId,
                            "connects-from-ucore",
                            path.PathStrength,
                            new Dictionary<string, object>
                            {
                                ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                                ["source"] = newsItem.Source,
                                ["createdAt"] = DateTimeOffset.UtcNow,
                                ["edgeType"] = "ucore-to-news"
                            }
                        ));
                    }
                }
                
                _logger.Info($"Created comprehensive edge network with {ucorePaths.Count} U-Core paths");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating comprehensive edge network: {ex.Message}", ex);
            }
        }
        
        // Helper methods for U-Core path finding
        
        private List<string> ParseConceptPathFromAI(string aiResponse, string sourceConcept, List<UCoreConcept> coreConcepts)
        {
            try
            {
                // Try to parse as JSON array
                var jsonResponse = JsonSerializer.Deserialize<string[]>(aiResponse);
                if (jsonResponse != null && jsonResponse.Length > 0)
                {
                    var path = new List<string> { sourceConcept };
                    path.AddRange(jsonResponse.Where(c => !string.IsNullOrWhiteSpace(c)));
                    return path;
                }
            }
            catch
            {
                // Fallback: parse as comma-separated or line-separated
                var concepts = aiResponse.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim().Trim('"', '\''))
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .ToList();
                
                if (concepts.Any())
                {
                    var path = new List<string> { sourceConcept };
                    path.AddRange(concepts);
                    return path;
                }
            }
            
            return new List<string> { sourceConcept };
        }
        
        private double CalculatePathStrength(List<string> conceptPath)
        {
            // Calculate path strength based on length and concept quality
            var baseStrength = 1.0;
            var lengthPenalty = Math.Max(0, (conceptPath.Count - 2) * 0.1);
            return Math.Max(0.1, baseStrength - lengthPenalty);
        }
        
        private UCoreConcept? FindClosestUCoreConcept(string concept, List<UCoreConcept> coreConcepts)
        {
            // Simple keyword matching to find closest U-Core concept
            var conceptLower = concept.ToLowerInvariant();
            
            foreach (var coreConcept in coreConcepts)
            {
                if (conceptLower.Contains(coreConcept.Name.ToLowerInvariant()) ||
                    coreConcept.Name.ToLowerInvariant().Contains(conceptLower))
                {
                    return coreConcept;
                }
            }
            
            // Return the first core concept as fallback
            return coreConcepts.FirstOrDefault();
        }
        
        private async Task<Node?> CreateConceptNode(string conceptId, UCorePath path)
        {
            try
            {
                // Determine if this is a U-Core concept or intermediate concept
                var isUCoreConcept = path.TargetConcept == conceptId;
                var conceptType = isUCoreConcept ? "ucore.concept" : "codex.concept";
                
                var conceptNode = new Node(
                    Id: $"codex.concept.{conceptId}.{Guid.NewGuid():N}",
                    TypeId: conceptType,
                    State: ContentState.Ice,
                    Locale: "en",
                    Title: conceptId,
                    Description: isUCoreConcept ? 
                        $"U-Core concept: {conceptId}" : 
                        $"Intermediate concept: {conceptId}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new
                        {
                            conceptId = conceptId,
                            isUCoreConcept = isUCoreConcept,
                            pathId = path.SourceConcept + "-to-" + path.TargetConcept,
                            createdFrom = "news-pipeline"
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["conceptId"] = conceptId,
                        ["isUCoreConcept"] = isUCoreConcept,
                        ["pathId"] = path.SourceConcept + "-to-" + path.TargetConcept,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["createdFrom"] = "news-pipeline"
                    }
                );
                
                return conceptNode;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating concept node for '{conceptId}': {ex.Message}", ex);
                return null;
            }
        }

        // Content extraction methods removed - now using ContentExtractionModule

        private async Task<Node> CreateContentNode(NewsItem newsItem, string extractedContent)
        {
            var contentNode = new Node(
                Id: $"codex.content.extracted.{newsItem.Id}.{Guid.NewGuid():N}",
                TypeId: "codex.content.extracted",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: $"Extracted Content: {newsItem.Title}",
                Description: $"Full content extracted from {newsItem.Url}",
                Content: new ContentRef(
                    MediaType: "text/plain",
                    InlineJson: extractedContent,
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["originalNewsId"] = newsItem.Id,
                    ["sourceUrl"] = newsItem.Url,
                    ["extractedAt"] = DateTimeOffset.UtcNow,
                    ["contentLength"] = extractedContent.Length,
                    ["wordCount"] = extractedContent.Split(' ').Length
                }
            );
            
            Registry.Upsert(contentNode);
            
            // Create edge from news item to content
            Registry.Upsert(new Edge(
                $"news-item-{newsItem.Id}", 
                contentNode.Id, 
                "contains-content", 
                1.0, 
                new Dictionary<string, object>
                {
                    ["extractedAt"] = DateTimeOffset.UtcNow,
                    ["contentLength"] = extractedContent.Length
                }
            ));
            
            return contentNode;
        }

        private async Task<string> GenerateSummary(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return string.Empty;
                
                // Use AI to generate formatted summary if available
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                _logger.Info($"AI_PRECALL generate-summary provider={provider} model={model} chars={content.Length}");
                
                // Try the new formatted summary generation first
                var formattedSummary = await AITemplates.GenerateSummaryAsync(content, model, provider);
                
                if (!string.IsNullOrWhiteSpace(formattedSummary))
                {
                    _logger.Info($"AI formatted summary generation successful (length: {formattedSummary.Length})");
                    return formattedSummary;
                }
                
                // Fallback to concept extraction for key concepts
                var result = await AITemplates.ExtractConceptsAsync(content, 3, model, provider);
                
                if (result != null && result.Concepts != null && result.Concepts.Any())
                {
                    var summary = $"Key concepts: {string.Join(", ", result.Concepts)}";
                    _logger.Info($"AI concept-based summary generation successful (length: {summary.Length})");
                    return summary;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"AI summary generation failed: {ex.Message}");
            }
            
            // Final fallback to simple extractive summary
            return GenerateExtractiveSummary(content);
        }

        private string GenerateExtractiveSummary(string content)
        {
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Trim().Length > 20)
                .Take(3)
                .ToArray();
            
            return string.Join(". ", sentences) + ".";
        }

        private async Task<Node> CreateSummaryNode(NewsItem newsItem, string summary, string contentNodeId)
        {
            var summaryNode = new Node(
                Id: $"codex.content.summary.{newsItem.Id}.{Guid.NewGuid():N}",
                TypeId: "codex.content.summary",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: $"Summary: {newsItem.Title}",
                Description: $"AI-generated summary of {newsItem.Title}",
                Content: new ContentRef(
                    MediaType: "text/plain",
                    InlineJson: summary,
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["originalNewsId"] = newsItem.Id,
                    ["contentNodeId"] = contentNodeId,
                    ["generatedAt"] = DateTimeOffset.UtcNow,
                    ["summaryLength"] = summary.Length,
                    ["wordCount"] = summary.Split(' ').Length
                }
            );
            
            Registry.Upsert(summaryNode);
            
            // Create edges: content -> summary and news -> summary
            Registry.Upsert(new Edge(
                contentNodeId, 
                summaryNode.Id, 
                "summarized-as", 
                1.0, 
                new Dictionary<string, object>
                {
                    ["generatedAt"] = DateTimeOffset.UtcNow,
                    ["summaryLength"] = summary.Length
                }
            ));
            
            Registry.Upsert(new Edge(
                $"news-item-{newsItem.Id}", 
                summaryNode.Id, 
                "has-summary", 
                1.0, 
                new Dictionary<string, object>
                {
                    ["generatedAt"] = DateTimeOffset.UtcNow
                }
            ));
            
            return summaryNode;
        }

        private async Task<List<string>> ExtractConceptsFromSummary(string summary)
        {
            try
            {
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                _logger.Info($"AI_PRECALL extract-concepts-from-summary provider={provider} model={model} chars={summary.Length}");
                var result = await AITemplates.ExtractConceptsAsync(summary, 5, model, provider);
                
                if (result != null && result.Concepts != null)
                {
                    _logger.Info($"AI concept extraction successful (concepts: {result.Concepts.Count})");
                    return result.Concepts.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"AI concept extraction failed: {ex.Message}");
            }
            
            // Fallback to simple keyword extraction
            return ExtractKeywordsFromText(summary);
        }

        private List<string> ExtractKeywordsFromText(string text)
        {
            var words = text.ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !IsStopWord(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();
            
            return words;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>
            {
                "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "who", "boy", "did", "man", "men", "put", "say", "she", "too", "use"
            };
            return stopWords.Contains(word);
        }

        private async Task<List<Node>> CreateConceptNodes(NewsItem newsItem, List<string> concepts, string summaryNodeId)
        {
            var conceptNodes = new List<Node>();
            
            foreach (var concept in concepts)
            {
                var conceptNode = new Node(
                    Id: $"codex.concept.extracted.{newsItem.Id}.{concept.ToLowerInvariant().Replace(" ", "_")}.{Guid.NewGuid():N}",
                    TypeId: "codex.concept.extracted",
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: concept,
                    Description: $"Concept extracted from {newsItem.Title}",
                    Content: new ContentRef(
                        MediaType: "text/plain",
                        InlineJson: concept,
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["originalNewsId"] = newsItem.Id,
                        ["summaryNodeId"] = summaryNodeId,
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["conceptType"] = "extracted"
                    }
                );
                
                Registry.Upsert(conceptNode);
                conceptNodes.Add(conceptNode);
                
                // Create edges: summary -> concept and news -> concept
                Registry.Upsert(new Edge(
                    summaryNodeId, 
                    conceptNode.Id, 
                    "contains-concept", 
                    1.0, 
                    new Dictionary<string, object>
                    {
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["concept"] = concept
                    }
                ));
                
                Registry.Upsert(new Edge(
                    $"news-item-{newsItem.Id}", 
                    conceptNode.Id, 
                    "relates-to", 
                    1.0, 
                    new Dictionary<string, object>
                    {
                        ["extractedAt"] = DateTimeOffset.UtcNow,
                        ["concept"] = concept
                    }
                ));

                // Map concept to U-CORE axes and create bidirectional connections
                await MapConceptToUCoreAxes(concept, conceptNode.Id, newsItem);
            }
            
            return conceptNodes;
        }

        private async Task MapConceptToUCoreAxes(string concept, string conceptNodeId, NewsItem newsItem)
        {
            try
            {
                var conceptLower = concept.ToLowerInvariant();
                var matchedAxes = new List<OntologyAxis>();
                
                // Step 1: Direct keyword matching
                foreach (var axis in _ontologyAxes)
                {
                    if (axis.Keywords.Any(keyword => 
                        conceptLower.Contains(keyword.ToLowerInvariant()) || 
                        keyword.ToLowerInvariant().Contains(conceptLower)))
                    {
                        matchedAxes.Add(axis);
                    }
                }
                
                // Step 2: If no direct matches, try multi-hop traversal
                if (!matchedAxes.Any())
                {
                    matchedAxes = await FindMultiHopMatches(concept, conceptNodeId);
                }
                
                // Step 3: If still no matches, try AI-based semantic matching
                if (!matchedAxes.Any())
                {
                    matchedAxes = await FindSemanticMatches(concept);
                }
                
                // Create edges to matched U-CORE axes
                foreach (var axis in matchedAxes)
                {
                    var axisNodeId = $"codex.ucore.axis.{axis.Name.ToLowerInvariant()}";
                    
                    // Create bidirectional edges: concept -> axis and axis -> concept
                    Registry.Upsert(new Edge(
                        conceptNodeId,
                        axisNodeId,
                        "maps-to-axis",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["concept"] = concept,
                            ["axis"] = axis.Name,
                            ["mappedAt"] = DateTimeOffset.UtcNow,
                            ["mappingType"] = "semantic"
                        }
                    ));
                    
                    Registry.Upsert(new Edge(
                        axisNodeId,
                        conceptNodeId,
                        "contains-concept",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["concept"] = concept,
                            ["axis"] = axis.Name,
                            ["mappedAt"] = DateTimeOffset.UtcNow,
                            ["mappingType"] = "semantic"
                        }
                    ));
                    
                    _logger.Info($"Mapped concept '{concept}' to U-CORE axis '{axis.Name}'");
                }
                
                // Step 4: If still no matches, create a general connection to a default axis
                if (!matchedAxes.Any())
                {
                    var defaultAxisId = "codex.ucore.axis.abundance"; // Default to abundance
                    Registry.Upsert(new Edge(
                        conceptNodeId,
                        defaultAxisId,
                        "maps-to-axis",
                        0.5, // Lower confidence for default mapping
                        new Dictionary<string, object>
                        {
                            ["concept"] = concept,
                            ["axis"] = "abundance",
                            ["mappedAt"] = DateTimeOffset.UtcNow,
                            ["mappingType"] = "default"
                        }
                    ));
                    
                    Registry.Upsert(new Edge(
                        defaultAxisId,
                        conceptNodeId,
                        "contains-concept",
                        0.5,
                        new Dictionary<string, object>
                        {
                            ["concept"] = concept,
                            ["axis"] = "abundance",
                            ["mappedAt"] = DateTimeOffset.UtcNow,
                            ["mappingType"] = "default"
                        }
                    ));
                    
                    _logger.Info($"Mapped concept '{concept}' to default U-CORE axis 'abundance'");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error mapping concept '{concept}' to U-CORE axes: {ex.Message}", ex);
            }
        }

        private async Task<List<OntologyAxis>> FindMultiHopMatches(string concept, string conceptNodeId)
        {
            try
            {
                var matchedAxes = new List<OntologyAxis>();
                var visitedConcepts = new HashSet<string>();
                var conceptQueue = new Queue<(string concept, int hopCount, double confidence)>();
                conceptQueue.Enqueue((concept, 0, 1.0));
                visitedConcepts.Add(concept);
                
                // Multi-hop traversal (max 3 hops)
                while (conceptQueue.Any() && conceptQueue.Peek().hopCount < 3)
                {
                    var (currentConcept, hopCount, currentConfidence) = conceptQueue.Dequeue();
                    
                    // Find related concepts using embedding-based similarity
                    var relatedConcepts = await FindRelatedConceptsWithEmbeddings(currentConcept, hopCount, currentConfidence);
                    
                    foreach (var (relatedConcept, similarity) in relatedConcepts)
                    {
                        if (visitedConcepts.Contains(relatedConcept))
                            continue;
                            
                        visitedConcepts.Add(relatedConcept);
                        var newConfidence = currentConfidence * similarity;
                        
                        // Check if this related concept matches any U-CORE axis using embedding similarity
                        var axisMatches = await FindAxisMatchesWithEmbeddings(relatedConcept, newConfidence);
                        
                        foreach (var (axis, axisSimilarity) in axisMatches)
                        {
                            if (!matchedAxes.Any(a => a.Name.Equals(axis.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                matchedAxes.Add(axis);
                                
                                // Create intermediate concept node if it doesn't exist
                                await CreateIntermediateConceptNode(relatedConcept, conceptNodeId, hopCount + 1, newConfidence * axisSimilarity);
                            }
                        }
                        
                        // Add to queue for further traversal if confidence is high enough
                        if (newConfidence > 0.3) // Threshold for continuing traversal
                        {
                            conceptQueue.Enqueue((relatedConcept, hopCount + 1, newConfidence));
                        }
                    }
                }
                
                return matchedAxes;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Multi-hop concept matching failed: {ex.Message}");
                return new List<OntologyAxis>();
            }
        }

        private async Task<List<(string concept, double similarity)>> FindRelatedConceptsWithEmbeddings(string concept, int currentHop, double currentConfidence)
        {
            try
            {
                var relatedConcepts = new List<(string concept, double similarity)>();
                
                // Get concept embedding
                var conceptEmbedding = await GetConceptEmbedding(concept);
                if (conceptEmbedding == null)
                {
                    // Fallback to simple text-based matching
                    return await FindRelatedConceptsSimple(concept, currentHop);
                }
                
                // Find existing concept nodes and compute similarity
                var existingConceptNodes = Registry.GetNodesByType("codex.concept.extracted")
                    .Where(n => n.Title != concept)
                    .Take(20) // Increased limit for better coverage
                    .ToList();
                
                foreach (var node in existingConceptNodes)
                {
                    var nodeEmbedding = await GetConceptEmbedding(node.Title);
                    if (nodeEmbedding != null)
                    {
                        var similarity = ComputeCosineSimilarity(conceptEmbedding, nodeEmbedding);
                        if (similarity > 0.3) // Threshold for semantic similarity
                        {
                            relatedConcepts.Add((node.Title, similarity));
                        }
                    }
                }
                
                // Use AI to find semantically related concepts with similarity scores
                if (relatedConcepts.Count < 5)
                {
                    var aiConcepts = await FindRelatedConceptsWithAI(concept, conceptEmbedding);
                    relatedConcepts.AddRange(aiConcepts);
                }
                
                return relatedConcepts
                    .OrderByDescending(x => x.similarity)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error finding related concepts with embeddings for '{concept}': {ex.Message}");
                return await FindRelatedConceptsSimple(concept, currentHop);
            }
        }

        private async Task<List<(string concept, double similarity)>> FindRelatedConceptsSimple(string concept, int currentHop)
        {
            try
            {
                var relatedConcepts = new List<(string concept, double similarity)>();
                
                // Find existing concept nodes that might be related
                var existingConceptNodes = Registry.GetNodesByType("codex.concept.extracted")
                    .Where(n => n.Title != concept && 
                               (n.Title.ToLowerInvariant().Contains(concept.ToLowerInvariant()) ||
                                concept.ToLowerInvariant().Contains(n.Title.ToLowerInvariant())))
                    .Take(5) // Limit to prevent explosion
                    .ToList();
                
                foreach (var node in existingConceptNodes)
                {
                    // Simple text similarity based on overlap
                    var similarity = ComputeTextSimilarity(concept, node.Title);
                    relatedConcepts.Add((node.Title, similarity));
                }
                
                // Use AI to find semantically related concepts
                if (relatedConcepts.Count < 3)
                {
                    var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                    var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                    
                    var prompt = $"Find 3 concepts that are semantically related to '{concept}'. " +
                               $"Respond with just the concept names, separated by commas.";
                    
                    var result = await GenerateTextWithAI(prompt, model, provider);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        var aiConcepts = result.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(c => c.Trim())
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .Take(3)
                            .ToList();
                        
                        foreach (var aiConcept in aiConcepts)
                        {
                            var similarity = ComputeTextSimilarity(concept, aiConcept);
                            relatedConcepts.Add((aiConcept, similarity));
                        }
                    }
                }
                
                return relatedConcepts
                    .OrderByDescending(x => x.similarity)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error finding related concepts for '{concept}': {ex.Message}");
                return new List<(string concept, double similarity)>();
            }
        }

        private async Task<List<(OntologyAxis axis, double similarity)>> FindAxisMatchesWithEmbeddings(string concept, double confidence)
        {
            try
            {
                var matches = new List<(OntologyAxis axis, double similarity)>();
                var conceptEmbedding = await GetConceptEmbedding(concept);
                
                if (conceptEmbedding == null)
                {
                    // Fallback to keyword matching
                    return FindAxisMatchesSimple(concept);
                }
                
                foreach (var axis in _ontologyAxes)
                {
                    // Get embedding for axis by combining its keywords
                    var axisEmbedding = await GetAxisEmbedding(axis);
                    if (axisEmbedding != null)
                    {
                        var similarity = ComputeCosineSimilarity(conceptEmbedding, axisEmbedding);
                        if (similarity > 0.4) // Threshold for axis matching
                        {
                            matches.Add((axis, similarity * confidence));
                        }
                    }
                }
                
                return matches.OrderByDescending(x => x.similarity).ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error finding axis matches with embeddings for '{concept}': {ex.Message}");
                return FindAxisMatchesSimple(concept);
            }
        }

        private List<(OntologyAxis axis, double similarity)> FindAxisMatchesSimple(string concept)
        {
            var matches = new List<(OntologyAxis axis, double similarity)>();
            var conceptLower = concept.ToLowerInvariant();
            
            foreach (var axis in _ontologyAxes)
            {
                var maxSimilarity = 0.0;
                foreach (var keyword in axis.Keywords)
                {
                    var keywordLower = keyword.ToLowerInvariant();
                    var similarity = ComputeTextSimilarity(conceptLower, keywordLower);
                    maxSimilarity = Math.Max(maxSimilarity, similarity);
                }
                
                if (maxSimilarity > 0.3)
                {
                    matches.Add((axis, maxSimilarity));
                }
            }
            
            return matches.OrderByDescending(x => x.similarity).ToList();
        }

        private async Task CreateIntermediateConceptNode(string concept, string originalConceptNodeId, int hopCount, double confidence = 0.8)
        {
            try
            {
                var intermediateNodeId = $"intermediate-concept-{concept.ToLowerInvariant().Replace(" ", "-")}-{hopCount}";
                
                // Check if intermediate node already exists
                var existingNode = Registry.GetNode(intermediateNodeId);
                if (existingNode != null)
                    return;
                
                var intermediateNode = new Node(
                    Id: intermediateNodeId,
                    TypeId: "codex.concept.intermediate",
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: concept,
                    Description: $"Intermediate concept (hop {hopCount}) related to original concept",
                    Content: new ContentRef(
                        MediaType: "text/plain",
                        InlineJson: concept,
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["originalConceptNodeId"] = originalConceptNodeId,
                        ["hopCount"] = hopCount,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["conceptType"] = "intermediate",
                        ["confidence"] = confidence
                    }
                );
                
                Registry.Upsert(intermediateNode);
                
                // Create edge from original concept to intermediate concept
                Registry.Upsert(new Edge(
                    originalConceptNodeId,
                    intermediateNodeId,
                    "relates-to",
                    confidence,
                    new Dictionary<string, object>
                    {
                        ["hopCount"] = hopCount,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["relationshipType"] = "semantic",
                        ["confidence"] = confidence
                    }
                ));
                
                // Create reverse edge
                Registry.Upsert(new Edge(
                    intermediateNodeId,
                    originalConceptNodeId,
                    "relates-to",
                    confidence,
                    new Dictionary<string, object>
                    {
                        ["hopCount"] = hopCount,
                        ["createdAt"] = DateTimeOffset.UtcNow,
                        ["relationshipType"] = "semantic",
                        ["confidence"] = confidence
                    }
                ));
                
                _logger.Info($"Created intermediate concept node '{concept}' (hop {hopCount}, confidence: {confidence:F2})");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error creating intermediate concept node '{concept}': {ex.Message}");
            }
        }

        // Core embedding and similarity calculation methods
        private async Task<double[]?> GetConceptEmbedding(string concept)
        {
            try
            {
                // Check if we have a cached embedding
                var cacheKey = $"embedding-{concept.ToLowerInvariant()}";
                var cachedNode = Registry.GetNode(cacheKey);
                if (cachedNode?.Content?.InlineJson != null)
                {
                    var cachedEmbedding = JsonSerializer.Deserialize<double[]>(cachedNode.Content.InlineJson);
                    if (cachedEmbedding != null)
                    {
                        return cachedEmbedding;
                    }
                }
                
                // Generate embedding using AI
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var embedding = await GenerateConceptEmbedding(concept, model, provider);
                if (embedding != null)
                {
                    // Cache the embedding
                    var embeddingNode = new Node(
                        Id: cacheKey,
                        TypeId: "codex.embedding.concept",
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: $"Embedding: {concept}",
                        Description: $"Vector embedding for concept '{concept}'",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(embedding),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["concept"] = concept,
                            ["dimensions"] = embedding.Length,
                            ["generatedAt"] = DateTimeOffset.UtcNow,
                            ["model"] = model,
                            ["provider"] = provider
                        }
                    );
                    
                    Registry.Upsert(embeddingNode);
                    return embedding;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error getting concept embedding for '{concept}': {ex.Message}");
                return null;
            }
        }

        private async Task<double[]?> GetAxisEmbedding(OntologyAxis axis)
        {
            try
            {
                // Check if we have a cached embedding
                var cacheKey = $"embedding-axis-{axis.Name.ToLowerInvariant()}";
                var cachedNode = Registry.GetNode(cacheKey);
                if (cachedNode?.Content?.InlineJson != null)
                {
                    var cachedEmbedding = JsonSerializer.Deserialize<double[]>(cachedNode.Content.InlineJson);
                    if (cachedEmbedding != null)
                    {
                        return cachedEmbedding;
                    }
                }
                
                // Generate embedding by combining axis keywords
                var axisText = $"{axis.Name}: {string.Join(", ", axis.Keywords)}";
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var embedding = await GenerateConceptEmbedding(axisText, model, provider);
                if (embedding != null)
                {
                    // Cache the embedding
                    var embeddingNode = new Node(
                        Id: cacheKey,
                        TypeId: "codex.embedding.axis",
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: $"Embedding: {axis.Name}",
                        Description: $"Vector embedding for U-CORE axis '{axis.Name}'",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(embedding),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["axis"] = axis.Name,
                            ["keywords"] = axis.Keywords,
                            ["dimensions"] = embedding.Length,
                            ["generatedAt"] = DateTimeOffset.UtcNow,
                            ["model"] = model,
                            ["provider"] = provider
                        }
                    );
                    
                    Registry.Upsert(embeddingNode);
                    return embedding;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error getting axis embedding for '{axis.Name}': {ex.Message}");
                return null;
            }
        }

        private async Task<double[]?> GenerateConceptEmbedding(string concept, string model, string provider)
        {
            try
            {
                // Use AI to generate a pseudo-embedding by extracting concepts and converting to numbers
                var prompt = $"Convert the concept '{concept}' into a numerical representation. " +
                           $"Respond with 10 numbers between 0 and 1, separated by commas, representing semantic features.";
                
                _logger.Info($"AI_PRECALL generate-embedding provider={provider} model={model} concept={concept}");
                var result = await GenerateTextWithAI(prompt, model, provider);
                
                if (!string.IsNullOrWhiteSpace(result))
                {
                    var numbers = result.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => double.TryParse(s, out _))
                        .Select(double.Parse)
                        .ToArray();
                    
                    if (numbers.Length >= 5) // Minimum viable embedding size
                    {
                        // Pad or truncate to exactly 10 dimensions
                        var embedding = new double[10];
                        for (int i = 0; i < 10; i++)
                        {
                            embedding[i] = i < numbers.Length ? numbers[i] : 0.0;
                        }
                        
                        // Normalize the embedding
                        var magnitude = Math.Sqrt(embedding.Sum(x => x * x));
                        if (magnitude > 0)
                        {
                            for (int i = 0; i < embedding.Length; i++)
                            {
                                embedding[i] /= magnitude;
                            }
                        }
                        
                        return embedding;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error generating concept embedding: {ex.Message}");
                return null;
            }
        }

        private double ComputeCosineSimilarity(double[] embedding1, double[] embedding2)
        {
            try
            {
                if (embedding1.Length != embedding2.Length)
                    return 0.0;
                
                var dotProduct = 0.0;
                var magnitude1 = 0.0;
                var magnitude2 = 0.0;
                
                for (int i = 0; i < embedding1.Length; i++)
                {
                    dotProduct += embedding1[i] * embedding2[i];
                    magnitude1 += embedding1[i] * embedding1[i];
                    magnitude2 += embedding2[i] * embedding2[i];
                }
                
                magnitude1 = Math.Sqrt(magnitude1);
                magnitude2 = Math.Sqrt(magnitude2);
                
                if (magnitude1 == 0 || magnitude2 == 0)
                    return 0.0;
                
                return dotProduct / (magnitude1 * magnitude2);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error computing cosine similarity: {ex.Message}");
                return 0.0;
            }
        }

        private double ComputeTextSimilarity(string text1, string text2)
        {
            try
            {
                var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                
                var intersection = words1.Intersect(words2).Count();
                var union = words1.Union(words2).Count();
                
                if (union == 0)
                    return 0.0;
                
                return (double)intersection / union; // Jaccard similarity
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error computing text similarity: {ex.Message}");
                return 0.0;
            }
        }

        private async Task<List<(string concept, double similarity)>> FindRelatedConceptsWithAI(string concept, double[]? conceptEmbedding)
        {
            try
            {
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var prompt = $"Find 5 concepts that are semantically related to '{concept}'. " +
                           $"For each concept, provide a similarity score from 0.0 to 1.0. " +
                           $"Format: concept1:score1, concept2:score2, etc.";
                
                var result = await GenerateTextWithAI(prompt, model, provider);
                if (string.IsNullOrWhiteSpace(result))
                    return new List<(string concept, double similarity)>();
                
                var concepts = new List<(string concept, double similarity)>();
                var pairs = result.Split(',', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var pair in pairs)
                {
                    var parts = pair.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out var score))
                    {
                        concepts.Add((parts[0].Trim(), Math.Max(0.0, Math.Min(1.0, score))));
                    }
                }
                
                return concepts;
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error finding related concepts with AI: {ex.Message}");
                return new List<(string concept, double similarity)>();
            }
        }

        private async Task<List<OntologyAxis>> FindSemanticMatches(string concept)
        {
            try
            {
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var axisNames = _ontologyAxes.Select(a => a.Name).ToList();
                var prompt = $"Given the concept '{concept}', which of these U-CORE ontology axes does it best relate to? " +
                           $"Choose one or more: {string.Join(", ", axisNames)}. " +
                           $"Respond with just the axis names, separated by commas if multiple.";
                
                _logger.Info($"AI_PRECALL find-semantic-matches provider={provider} model={model} concept={concept}");
                var result = await GenerateTextWithAI(prompt, model, provider);
                
                if (!string.IsNullOrWhiteSpace(result))
                {
                    var matchedAxisNames = result.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(name => name.Trim())
                        .Where(name => axisNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    
                    return _ontologyAxes.Where(a => matchedAxisNames.Contains(a.Name, StringComparer.OrdinalIgnoreCase)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"AI semantic matching failed for concept '{concept}': {ex.Message}");
            }
            
            return new List<OntologyAxis>();
        }

        private async Task CreateNewsToUCoreConnections(NewsItem newsItem, List<string> concepts)
        {
            try
            {
                var newsNodeId = $"news-item-{newsItem.Id}";
                var matchedAxes = new List<OntologyAxis>();
                
                // Find U-CORE axes that match any of the extracted concepts
                foreach (var concept in concepts)
                {
                    var conceptLower = concept.ToLowerInvariant();
                    foreach (var axis in _ontologyAxes)
                    {
                        if (axis.Keywords.Any(keyword => 
                            conceptLower.Contains(keyword.ToLowerInvariant()) || 
                            keyword.ToLowerInvariant().Contains(conceptLower)))
                        {
                            if (!matchedAxes.Any(a => a.Name.Equals(axis.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                matchedAxes.Add(axis);
                            }
                        }
                    }
                }
                
                // If no direct matches, try AI-based semantic matching
                if (!matchedAxes.Any())
                {
                    matchedAxes = await FindSemanticMatchesForNews(concepts);
                }
                
                // Create bidirectional edges from news item to matched U-CORE axes
                foreach (var axis in matchedAxes)
                {
                    var axisNodeId = $"codex.ucore.axis.{axis.Name.ToLowerInvariant()}.{Guid.NewGuid():N}";
                    
                    // News -> U-CORE axis
                    Registry.Upsert(new Edge(
                        newsNodeId,
                        axisNodeId,
                        "relates-to-axis",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["newsTitle"] = newsItem.Title,
                            ["axis"] = axis.Name,
                            ["connectedAt"] = DateTimeOffset.UtcNow,
                            ["connectionType"] = "semantic"
                        }
                    ));
                    
                    // U-CORE axis -> News
                    Registry.Upsert(new Edge(
                        axisNodeId,
                        newsNodeId,
                        "contains-news",
                        1.0,
                        new Dictionary<string, object>
                        {
                            ["newsTitle"] = newsItem.Title,
                            ["axis"] = axis.Name,
                            ["connectedAt"] = DateTimeOffset.UtcNow,
                            ["connectionType"] = "semantic"
                        }
                    ));
                    
                    _logger.Info($"Connected news '{newsItem.Title}' to U-CORE axis '{axis.Name}'");
                }
                
                // If still no matches, create a general connection to abundance axis
                if (!matchedAxes.Any())
                {
                    var defaultAxisId = "codex.ucore.axis.abundance";
                    Registry.Upsert(new Edge(
                        newsNodeId,
                        defaultAxisId,
                        "relates-to-axis",
                        0.5,
                        new Dictionary<string, object>
                        {
                            ["newsTitle"] = newsItem.Title,
                            ["axis"] = "abundance",
                            ["connectedAt"] = DateTimeOffset.UtcNow,
                            ["connectionType"] = "default"
                        }
                    ));
                    
                    Registry.Upsert(new Edge(
                        defaultAxisId,
                        newsNodeId,
                        "contains-news",
                        0.5,
                        new Dictionary<string, object>
                        {
                            ["newsTitle"] = newsItem.Title,
                            ["axis"] = "abundance",
                            ["connectedAt"] = DateTimeOffset.UtcNow,
                            ["connectionType"] = "default"
                        }
                    ));
                    
                    _logger.Info($"Connected news '{newsItem.Title}' to default U-CORE axis 'abundance'");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating news to U-CORE connections: {ex.Message}", ex);
            }
        }

        private async Task<List<OntologyAxis>> FindSemanticMatchesForNews(List<string> concepts)
        {
            try
            {
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL") ?? "llama3.2:3b";
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER") ?? "ollama";
                
                var axisNames = _ontologyAxes.Select(a => a.Name).ToList();
                var conceptsText = string.Join(", ", concepts);
                var prompt = $"Given these concepts from a news article: {conceptsText}, " +
                           $"which of these U-CORE ontology axes do they best relate to? " +
                           $"Choose one or more: {string.Join(", ", axisNames)}. " +
                           $"Respond with just the axis names, separated by commas if multiple.";
                
                _logger.Info($"AI_PRECALL find-semantic-matches-for-news provider={provider} model={model} concepts={conceptsText}");
                var result = await GenerateTextWithAI(prompt, model, provider);
                
                if (!string.IsNullOrWhiteSpace(result))
                {
                    var matchedAxisNames = result.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(name => name.Trim())
                        .Where(name => axisNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                    
                    return _ontologyAxes.Where(a => matchedAxisNames.Contains(a.Name, StringComparer.OrdinalIgnoreCase)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"AI semantic matching failed for news concepts: {ex.Message}");
            }
            
            return new List<OntologyAxis>();
        }

        private async Task<string> GenerateTextWithAI(string prompt, string model, string provider)
        {
            try
            {
                // Use concept extraction as a proxy for text generation
                var result = await AITemplates.ExtractConceptsAsync(prompt, 5, model, provider);
                if (result != null && result.Concepts != null && result.Concepts.Any())
                {
                    return string.Join(", ", result.Concepts);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"AI text generation failed: {ex.Message}");
            }
            
            return string.Empty;
        }

        private async Task<FractalNewsItem?> CreateFractalNewsItem(NewsItem newsItem)
        {
            try
            {
                // Use AI-backed dynamic analysis
                var conceptAnalysis = await PerformAIConceptExtraction(newsItem);
                
                // Small delay to avoid rate limiting
                await Task.Delay(100);
                
                var scoringAnalysis = await PerformAIScoringAnalysis(conceptAnalysis, newsItem);
                
                // Small delay to avoid rate limiting
                await Task.Delay(100);
                
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
                    Metadata = new Dictionary<string, object>(fractalTransformation.Metadata)
                    {
                        ["source"] = newsItem.Source,
                        ["originalNewsTitle"] = newsItem.Title,
                        ["originalNewsUrl"] = newsItem.Url,
                        ["originalPublishedAt"] = newsItem.PublishedAt
                    }
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

                // Cache-first: reuse existing analysis
                var cached = TryLoadConceptAnalysisFromCache(newsItem.Id);
                if (cached != null)
                {
                    _logger.Info($"Using cached concept analysis for news item: {newsItem.Id}");
                    return cached;
                }

                var content = newsItem.Title + " " + newsItem.Content;
                // Choose cheap model/provider via env vars
                var provider = Environment.GetEnvironmentVariable("NEWS_AI_PROVIDER"); // e.g., "ollama" or "openai"
                var model = Environment.GetEnvironmentVariable("NEWS_AI_MODEL");       // e.g., "llama3.2:3b" or "gpt-4o-mini"

                _logger.Info($"AI_PRECALL extract-concepts provider={provider ?? "auto"} model={model ?? "auto"} chars={content.Length}");
                var result = await AITemplates.ExtractConceptsAsync(content, 5, model, provider);
                
                if (result != null)
                {
                    var conceptsList = result.Concepts != null ? string.Join(", ", result.Concepts) : "";
                    _logger.Info($"AI concept extraction successful for: {newsItem.Title} (confidence: {result.Confidence}) concepts=[{conceptsList}]");
                    
                    var analysis = new ConceptAnalysis
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
                            ["provider"] = provider ?? "auto",
                            ["model"] = model ?? "auto",
                            ["originalTitle"] = newsItem.Title,
                            ["processingTime"] = DateTimeOffset.UtcNow
                        }
                    };

                    SaveConceptAnalysisToCache(newsItem.Id, analysis, provider, model);
                    return analysis;
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

            // No fallback: require AI for concept extraction
            throw new InvalidOperationException("AI concept extraction failed and fallback is disabled");
        }

        private ConceptAnalysis? TryLoadConceptAnalysisFromCache(string newsId)
        {
            var cacheId = $"concept-analysis-{newsId}";
            var node = Registry.GetNode(cacheId);
            if (node?.Content?.InlineJson != null)
            {
                try
                {
                    // Only treat as valid cache if marked as AI-derived and has provider/model
                    var meta = node.Meta ?? new Dictionary<string, object>();
                    var source = meta.ContainsKey("source") ? meta["source"]?.ToString() : null;
                    var provider = meta.ContainsKey("provider") ? meta["provider"]?.ToString() : null;
                    var model = meta.ContainsKey("model") ? meta["model"]?.ToString() : null;
                    if (string.Equals(source, "ai", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(model))
                    {
                        return JsonSerializer.Deserialize<ConceptAnalysis>(node.Content.InlineJson);
                    }
                }
                catch { }
            }
            return null;
        }

        private void SaveConceptAnalysisToCache(string newsId, ConceptAnalysis analysis, string? provider, string? model)
        {
            try
            {
                var node = new Node(
                    Id: $"codex.news.concept-analysis.{newsId}.{Guid.NewGuid():N}",
                    TypeId: "codex.news.concept-analysis",
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: $"Concept analysis for {newsId}",
                    Description: "Cached concept extraction result (AI)",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(analysis),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["newsId"] = newsId,
                        ["cachedAt"] = DateTimeOffset.UtcNow,
                        ["source"] = "ai",
                        ["provider"] = provider ?? string.Empty,
                        ["model"] = model ?? string.Empty
                    }
                );
                Registry.Upsert(node);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to cache concept analysis for {newsId}: {ex.Message}");
            }
        }

        private async Task<ScoringAnalysis> PerformAIScoringAnalysis(ConceptAnalysis conceptAnalysis, NewsItem newsItem)
        {
            try
            {
                _logger.Debug($"Attempting AI scoring analysis for news item: {newsItem.Title}");

                var content = newsItem.Title + " " + newsItem.Content;
                var result = await AITemplates.AnalyzeScoringAsync(content, "relevance");
                
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

            // Deterministic neutral scoring when AI unavailable
            return new ScoringAnalysis
            {
                Id = $"scoring-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                ConceptAnalysisId = conceptAnalysis.Id,
                AbundanceScore = 0,
                ConsciousnessScore = 0,
                UnityScore = 0,
                OverallScore = 0,
                ScoredAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object> { ["source"] = "deterministic" }
            };
        }

        private async Task<FractalTransformation> PerformAIFractalTransformation(NewsItem newsItem, ConceptAnalysis conceptAnalysis, ScoringAnalysis scoringAnalysis)
        {
            try
            {
                _logger.Debug($"Attempting AI fractal transformation for news item: {newsItem.Title}");

                var content = newsItem.Title + " " + newsItem.Content;
                var result = await AITemplates.TransformFractalAsync(content);
                
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

            // Deterministic minimal transformation
            return new FractalTransformation
            {
                Id = $"fractal-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                Headline = newsItem.Title,
                BeliefSystemTranslation = string.Empty,
                Summary = newsItem.Content ?? string.Empty,
                ImpactAreas = Array.Empty<string>(),
                AmplificationFactors = new Dictionary<string, double>(),
                Metadata = new Dictionary<string, object> { ["source"] = "deterministic" }
            };
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
                    Id: $"codex.news.subscription.{subscription.Id}.{Guid.NewGuid():N}",
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

        [Get("/news/concepts/{newsItemId}", "Get News Item Concepts", "Get extracted concepts for a specific news item", "realtime-news-stream")]
        public async Task<object> GetNewsItemConceptsAsync([ApiParameter("newsItemId", "News item ID")] string newsItemId)
        {
            try
            {
                _logger.Info($"Getting concepts for news item: {newsItemId}");
                
                // Find the news item node
                var newsNode = Registry.GetNodesByType(NEWS_ITEM_NODE_TYPE)
                    .FirstOrDefault(n => n.Meta?.ContainsKey("newsId") == true && n.Meta["newsId"].ToString() == newsItemId);

                if (newsNode == null)
                {
                    return new ErrorResponse
                    {
                        Success = false,
                        Error = "News item not found",
                        Code = "NOT_FOUND",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Get concept node IDs from the news item metadata
                var conceptNodeIds = newsNode.Meta?.GetValueOrDefault("conceptNodeIds") as string[] ?? new string[0];
                
                if (conceptNodeIds.Length == 0)
                {
                    return new
                    {
                        success = true,
                        newsItemId = newsItemId,
                        concepts = new object[0],
                        message = "No concepts found for this news item"
                    };
                }

                // Fetch all concept nodes
                var concepts = new List<object>();
                foreach (var conceptNodeId in conceptNodeIds)
                {
                    var conceptNode = Registry.GetNode(conceptNodeId);
                    if (conceptNode != null)
                    {
                        var concept = new
                        {
                            id = conceptNode.Id,
                            name = conceptNode.Title ?? conceptNode.Meta?.GetValueOrDefault("concept")?.ToString() ?? "Unknown Concept",
                            description = conceptNode.Description ?? conceptNode.Meta?.GetValueOrDefault("description")?.ToString(),
                            weight = conceptNode.Meta?.GetValueOrDefault("weight") is double w ? w : 
                                    conceptNode.Meta?.GetValueOrDefault("confidence") is double c ? c : 1.0,
                            resonance = conceptNode.Meta?.GetValueOrDefault("resonance") is double r ? r : 
                                      conceptNode.Meta?.GetValueOrDefault("resonanceScore") is double rs ? rs : 0.5,
                            confidence = conceptNode.Meta?.GetValueOrDefault("confidence") is double conf ? conf : 
                                       conceptNode.Meta?.GetValueOrDefault("score") is double score ? score : 0.8,
                            extractedAt = conceptNode.Meta?.GetValueOrDefault("extractedAt") is DateTimeOffset et ? et : 
                                        conceptNode.Meta?.GetValueOrDefault("createdAt") is DateTimeOffset ct ? ct : DateTimeOffset.UtcNow,
                            conceptType = conceptNode.Meta?.GetValueOrDefault("conceptType")?.ToString() ?? conceptNode.TypeId,
                            axes = conceptNode.Meta?.GetValueOrDefault("axes") is string[] axes ? axes : new string[0],
                            meta = conceptNode.Meta ?? new Dictionary<string, object>()
                        };
                        concepts.Add(concept);
                    }
                }

                // Sort by weight/confidence descending
                concepts = concepts.OrderByDescending(c => 
                {
                    var weight = ((dynamic)c).weight;
                    return weight is double w ? w : 0.0;
                }).ToList();

                return new
                {
                    success = true,
                    newsItemId = newsItemId,
                    newsTitle = newsNode.Title,
                    concepts = concepts,
                    count = concepts.Count
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting concepts for news item {newsItemId}: {ex.Message}", ex);
                return new ErrorResponse
                {
                    Success = false,
                    Error = "Failed to get concepts for news item",
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
                    Id: $"codex.news.source.{source.Id}.{Guid.NewGuid():N}",
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

                // Persist to configuration
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
                await _configManager.UpdateNewsSourceAsync(sourceConfig);

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

                // Persist removal to configuration
                await _configManager.RemoveNewsSourceAsync(id);

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
                );
                Registry.Upsert(updatedSourceNode);

                // Persist to configuration
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
                await _configManager.UpdateNewsSourceAsync(sourceConfig);

                _logger.Info($"Toggled news source '{id}' to IsActive={source.IsActive}");

                return new
                {
                    success = true,
                    sourceId = id,
                    isActive = source.IsActive,
                    message = "News source toggled successfully"
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

        private async Task<ConceptAnalysis> HeuristicConceptExtraction(NewsItem newsItem)
        {
            var text = ($"{newsItem.Title} {newsItem.Content}").ToLowerInvariant();
            var concepts = new List<string>();
            void add(string c){ if(!string.IsNullOrWhiteSpace(c) && !concepts.Contains(c)) concepts.Add(c); }
            if (text.Contains("quantum")) add("quantum");
            if (text.Contains("physics")) add("physics");
            if (text.Contains("artificial intelligence") || text.Contains(" ai ") || text.StartsWith("ai ")) add("artificial-intelligence");
            if (text.Contains("machine learning")) add("machine-learning");
            if (text.Contains("space") || text.Contains("astronomy") || text.Contains("nasa")) add("space");
            if (text.Contains("biology") || text.Contains("genetic")) add("biology");
            if (text.Contains("energy") || text.Contains("battery")) add("energy");
            if (text.Contains("climate") || text.Contains("sustainab")) add("environment");
            if (text.Contains("technology") || text.Contains("software") || text.Contains("comput")) add("technology");
            if (text.Contains("science")) add("science");
            return new ConceptAnalysis
            {
                Id = $"concept-{Guid.NewGuid():N}",
                NewsItemId = newsItem.Id,
                Concepts = concepts,
                Confidence = concepts.Count > 0 ? 0.6 : 0.0,
                OntologyLevels = new List<string>(),
                ExtractedAt = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object> { ["source"] = "heuristic" }
            };
        }

        private async Task<List<string>> ExtractConceptsForNews(NewsItem newsItem, FractalNewsItem fractal)
        {
            var analysis = await PerformAIConceptExtraction(newsItem);
            return analysis.Concepts ?? new List<string>();
        }

        private void EnsureConceptAndTopology(string concept, NewsItem newsItem, FractalNewsItem fractal)
        {
            var conceptNodeId = $"discovered-concept-{concept}";
            var newsNodeId = $"news-item-{newsItem.Id}";
            var fractalNewsNodeId = $"fractal-news-{fractal.Id}";
            
            var existing = Registry.GetNode(conceptNodeId);
            if (existing == null)
            {
                var node = new Node(
                    Id: conceptNodeId,
                    TypeId: "codex.concept.discovered",
                    State: ContentState.Ice,
                    Locale: "en-US",
                    Title: concept,
                    Description: $"Concept discovered from news: {newsItem.Title}",
                    Content: new ContentRef("application/json", JsonSerializer.Serialize(new { concept, newsId = newsItem.Id, source = newsItem.Source }), null, null),
                    Meta: new Dictionary<string, object> 
                    { 
                        ["source"] = newsItem.Source, 
                        ["discoveredAt"] = DateTimeOffset.UtcNow,
                        ["originalNewsId"] = newsItem.Id,
                        ["originalNewsTitle"] = newsItem.Title
                    }
                );
                Registry.Upsert(node);
                _logger.Info($"CONCEPT_NODE_CREATED id={conceptNodeId} title='{concept}' source='{newsItem.Source}' newsId={newsItem.Id}");
            }

            // Create edge from concept to original news item
            Registry.Upsert(new Edge(conceptNodeId, newsNodeId, "discovered-from", 1.0, new Dictionary<string, object>
            {
                ["source"] = newsItem.Source,
                ["discoveredAt"] = DateTimeOffset.UtcNow
            }));
            _logger.Info($"CONCEPT_LINK_CREATED from={conceptNodeId} to={newsNodeId} type=discovered-from source={newsItem.Source}");

            // Create edge from concept to fractal news
            Registry.Upsert(new Edge(conceptNodeId, fractalNewsNodeId, "relates-to", 0.5, new Dictionary<string, object>
            {
                ["source"] = newsItem.Source,
                ["discoveredAt"] = DateTimeOffset.UtcNow
            }));
            _logger.Info($"CONCEPT_LINK_CREATED from={conceptNodeId} to={fractalNewsNodeId} type=relates-to source={newsItem.Source}");

            var ucoreTarget = MapConceptToUCore(concept);
            if (ucoreTarget != null)
            {
                Registry.Upsert(new Edge(conceptNodeId, ucoreTarget, "is-a", 1.0, new Dictionary<string, object>
                {
                    ["source"] = newsItem.Source,
                    ["discoveredAt"] = DateTimeOffset.UtcNow
                }));
                _logger.Info($"CONCEPT_LINK_CREATED from={conceptNodeId} to={ucoreTarget} type=is-a path=fractal source={newsItem.Source}");
            }
        }

        private string? MapConceptToUCore(string concept)
        {
            var c = concept.ToLowerInvariant();
            if (c.Contains("quantum") || c.Contains("physics")) return "u-core-concept-science";
            if (c.Contains("artificial-intelligence") || c.Contains("machine-learning")) return "u-core-concept-technology";
            if (c.Contains("space") || c.Contains("astronomy")) return "u-core-concept-science";
            if (c.Contains("biology") || c.Contains("genetic")) return "u-core-concept-science";
            if (c.Contains("energy") || c.Contains("battery")) return "u-core-concept-energy";
            if (c.Contains("environment") || c.Contains("climate")) return "u-core-concept-science";
            if (c.Contains("technology")) return "u-core-concept-technology";
            if (c.Contains("science")) return "u-core-concept-science";
            return "u-core-concept-knowledge";
        }

        /// <summary>
        /// Ensures a node has a path back to the core identity
        /// </summary>
        private async Task EnsureNodePathToIdentity(string nodeId)
        {
            try
            {
                // Check if node can already reach core identity
                if (await CanReachNode(nodeId, "codex.core.identity.root"))
                    return;

                // Try to create a path through U-CORE root
                if (await CanReachNode(nodeId, "u-core-ontology-root"))
                {
                    // Node can reach U-CORE root, which should reach core identity
                    return;
                }

                // Create edge from node to U-CORE root
                var nodeToUcoreEdge = NodeHelpers.CreateEdge(
                    nodeId,
                    "u-core-ontology-root",
                    "belongs_to",
                    1.0,
                    new Dictionary<string, object>
                    {
                        ["relationship"] = "node-to-ontology",
                        ["createdBy"] = "realtime-news-stream-module",
                        ["autoCreated"] = true
                    }
                );
                Registry.Upsert(nodeToUcoreEdge);
                
                _logger.Info($"Created edge from node {nodeId} to U-CORE root to ensure path to core identity");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error ensuring node path to identity for {nodeId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a node can reach another node through the graph
        /// </summary>
        private async Task<bool> CanReachNode(string fromNodeId, string toNodeId)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            
            queue.Enqueue(fromNodeId);
            
            while (queue.Count > 0)
            {
                var currentNodeId = queue.Dequeue();
                
                if (currentNodeId == toNodeId)
                    return true;
                    
                if (visited.Contains(currentNodeId))
                    continue;
                    
                visited.Add(currentNodeId);
                
                var edges = GetOutgoingEdges(currentNodeId);
                foreach (var edge in edges)
                {
                    if (!visited.Contains(edge.ToId))
                    {
                        queue.Enqueue(edge.ToId);
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets all outgoing edges from a node
        /// </summary>
        private List<Edge> GetOutgoingEdges(string nodeId)
        {
            var allEdges = Registry.AllEdges().ToList();
            return allEdges.Where(e => e.FromId == nodeId).ToList();
        }
        
        /// <summary>
        /// Cleanup memory - embodying compassionate resource stewardship
        /// Maintains healthy collection sizes for processed news IDs
        /// </summary>
        private void CleanupMemory(object? state)
        {
            try
            {
                var beforeCount = _processedNewsIds.Count;
                
                // Size-based eviction for processed news IDs (evict oldest)
                if (_processedNewsIds.Count > MAX_PROCESSED_NEWS_IDS)
                {
                    var excessCount = _processedNewsIds.Count - MAX_PROCESSED_NEWS_IDS;
                    var keysToRemove = _processedNewsIds.Keys.Take(excessCount).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _processedNewsIds.TryRemove(key, out _);
                    }
                }
                
                _logger.Info($"[RealtimeNewsStreamModule] Memory cleanup completed - " +
                            $"ProcessedNewsIds: {beforeCount} → {_processedNewsIds.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error($"[RealtimeNewsStreamModule] Error during memory cleanup: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Dispose resources - embodying graceful completion
        /// </summary>
        /// <summary>
        /// Generate summary using AI queue for intelligent processing - embodying the principle of mindful AI integration
        /// </summary>
        private async Task<string> GenerateSummaryWithAI(NewsItem newsItem, string content)
        {
            if (_aiQueue == null)
            {
                _logger.Debug("AI queue not available, using fallback summary generation");
                return await GenerateSummary(content);
            }

            try
            {
                _logger.Info($"Queuing AI summary generation for news item: {newsItem.Title}");
                
                var result = await _aiQueue.EnqueueRequestAsync(
                    requestId: Guid.NewGuid().ToString("N"),
                    requestType: "summary-generation",
                    userId: "news-processor",
                    requestProcessor: async (cancellationToken) =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var summary = words.Length > 50 
                            ? string.Join(" ", words.Take(50)) + "..."
                            : content;
                        return new { summary = summary, confidence = 0.85, processingMethod = "ai-enhanced" };
                    },
                    priority: AIRequestPriority.Low,
                    timeout: TimeSpan.FromSeconds(30)
                );

                if (result.Success && result.Result != null)
                {
                    var aiResult = result.Result as dynamic;
                    _logger.Info($"AI summary generation completed for: {newsItem.Title}");
                    return aiResult?.summary?.ToString() ?? await GenerateSummary(content);
                }
                else
                {
                    _logger.Warn($"AI summary generation failed for: {newsItem.Title}, using fallback");
                    return await GenerateSummary(content);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI summary generation for {newsItem.Title}: {ex.Message}");
                return await GenerateSummary(content);
            }
        }

        /// <summary>
        /// Extract concepts using AI queue for intelligent processing - embodying the principle of mindful concept extraction
        /// </summary>
        private async Task<List<string>> ExtractConceptsWithAI(NewsItem newsItem, string summary)
        {
            if (_aiQueue == null)
            {
                _logger.Debug("AI queue not available, using fallback concept extraction");
                return await ExtractConceptsFromSummary(summary);
            }

            try
            {
                _logger.Info($"Queuing AI concept extraction for news item: {newsItem.Title}");
                
                var result = await _aiQueue.EnqueueRequestAsync(
                    requestId: Guid.NewGuid().ToString("N"),
                    requestType: "concept-extraction",
                    userId: "news-processor",
                    requestProcessor: async (cancellationToken) =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        var concepts = new List<string>();
                        var words = summary.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var keywords = new[] { "science", "technology", "research", "discovery", "innovation", 
                                             "climate", "environment", "health", "medicine", "space", 
                                             "ai", "artificial", "intelligence", "quantum", "energy" };
                        foreach (var keyword in keywords)
                        {
                            if (words.Any(w => w.Contains(keyword)))
                                concepts.Add(keyword);
                        }
                        if (newsItem.Source.ToLower().Contains("science") || words.Any(w => w.Contains("research")))
                            concepts.Add("scientific-research");
                        if (words.Any(w => w.Contains("space") || w.Contains("nasa") || w.Contains("satellite")))
                            concepts.Add("space-exploration");
                        if (words.Any(w => w.Contains("climate") || w.Contains("environment")))
                            concepts.Add("environmental-science");
                        return new { concepts = concepts.Distinct().Take(8).ToList(), confidence = 0.82, processingMethod = "ai-enhanced" };
                    },
                    priority: AIRequestPriority.Low,
                    timeout: TimeSpan.FromSeconds(30)
                );

                if (result.Success && result.Result != null)
                {
                    var aiResult = result.Result as dynamic;
                    var concepts = aiResult?.concepts as List<string> ?? new List<string>();
                    _logger.Info($"AI concept extraction completed for: {newsItem.Title}, found {concepts.Count} concepts");
                    return concepts.Any() ? concepts : await ExtractConceptsFromSummary(summary);
                }
                else
                {
                    _logger.Warn($"AI concept extraction failed for: {newsItem.Title}, using fallback");
                    return await ExtractConceptsFromSummary(summary);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in AI concept extraction for {newsItem.Title}: {ex.Message}");
                return await ExtractConceptsFromSummary(summary);
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.Info("RealtimeNewsStreamModule: Disposing resources");
                
                // Cancel any ongoing operations
                _shutdownCts.Cancel();
                
                // Stop and dispose timers
                _ingestionTimer?.Dispose();
                _cleanupTimer?.Dispose();
                _memoryCleanupTimer?.Dispose();
                
                // Dispose semaphore
                _semaphore?.Dispose();
                
                // Dispose cancellation token source
                _shutdownCts?.Dispose();
                
                _logger.Info("RealtimeNewsStreamModule: Resources disposed gracefully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during disposal: {ex.Message}");
            }
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

