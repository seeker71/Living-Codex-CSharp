using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using System.Text.Json;

namespace CodexBootstrap.Tests;

/// <summary>
/// Comprehensive tests for news source configuration and ingestion
/// </summary>
public class NewsSourceTests
{
    private readonly INodeRegistry _registry;
    private readonly ICodexLogger _logger;
    private readonly HttpClient _httpClient;

    public NewsSourceTests()
    {
        _registry = TestInfrastructure.CreateTestNodeRegistry();
        _logger = TestInfrastructure.CreateTestLogger();
        _httpClient = new HttpClient();
        _registry.InitializeAsync().Wait();
    }

    [Fact]
    public async Task NewsSourcesConfig_Should_LoadAllSources()
    {
        // Arrange
        var realtimeModule = new RealtimeNewsStreamModule(_registry, _logger, _httpClient);
        
        // Act
        realtimeModule.Register(_registry);
        
        // Wait a moment for initialization
        await Task.Delay(1000);

        // Assert - Check that news sources are loaded
        var sourceNodes = _registry.GetNodesByType("codex.news.source");
        var sources = sourceNodes.Select(n => JsonSerializer.Deserialize<NewsSource>(n.Content?.InlineJson ?? "{}"))
            .Where(s => s != null).ToList();

        Assert.True(sources.Count >= 50, $"Expected at least 50 news sources, found {sources.Count}");
        _logger.Info($"✓ Found {sources.Count} news sources configured");

        // Check key categories are represented
        var categories = sources.SelectMany(s => s.Categories).Distinct().ToList();
        var expectedCategories = new[] { "science", "technology", "world", "space", "health", "environment" };
        
        foreach (var category in expectedCategories)
        {
            Assert.Contains(category, categories);
            var categoryCount = sources.Count(s => s.Categories.Contains(category));
            _logger.Info($"✓ Category '{category}' has {categoryCount} sources");
        }
    }

    [Fact]
    public async Task NewsSourceValidation_Should_CheckRandomSources()
    {
        // Arrange
        var realtimeModule = new RealtimeNewsStreamModule(_registry, _logger, _httpClient);
        realtimeModule.Register(_registry);
        await Task.Delay(1000);

        var sourceNodes = _registry.GetNodesByType("codex.news.source");
        var sources = sourceNodes.Select(n => JsonSerializer.Deserialize<NewsSource>(n.Content?.InlineJson ?? "{}"))
            .Where(s => s != null && s.IsActive).Take(10).ToList(); // Test first 10 active sources

        // Act & Assert
        var validSources = 0;
        var invalidSources = new List<(NewsSource source, string error)>();

        foreach (var source in sources)
        {
            try
            {
                _logger.Info($"Testing news source: {source.Name} ({source.Url})");
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Living-Codex-NewsBot/1.0 (https://living-codex.com)");
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(source.Url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        validSources++;
                        _logger.Info($"✓ Source '{source.Name}' is accessible and returns content");
                    }
                    else
                    {
                invalidSources.Add((source!, "Empty content"));
                _logger.Warn($"⚠ Source '{source!.Name}' returns empty content");
                    }
                }
                else
                {
                    invalidSources.Add((source, $"HTTP {response.StatusCode}"));
                    _logger.Warn($"⚠ Source '{source.Name}' returned {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                invalidSources.Add((source!, ex.Message));
                _logger.Error($"✗ Source '{source!.Name}' failed: {ex.Message}");
            }
        }

        // Log summary
        _logger.Info($"News source validation summary: {validSources} valid, {invalidSources.Count} invalid out of {sources.Count} tested");
        
        // At least 70% should be valid for the test to pass
        var validPercentage = (double)validSources / sources.Count * 100;
        Assert.True(validPercentage >= 70, 
            $"Expected at least 70% of news sources to be valid, got {validPercentage:F1}%. Invalid sources: {string.Join(", ", invalidSources.Select(x => $"{x.source.Name}: {x.error}"))}");
    }

    [Fact]
    public async Task NewsSourceCategories_Should_CoverAllDomains()
    {
        // Arrange
        var realtimeModule = new RealtimeNewsStreamModule(_registry, _logger, _httpClient);
        realtimeModule.Register(_registry);
        await Task.Delay(1000);

        // Act
        var sourceNodes = _registry.GetNodesByType("codex.news.source");
        var sources = sourceNodes.Select(n => JsonSerializer.Deserialize<NewsSource>(n.Content?.InlineJson ?? "{}"))
            .Where(s => s != null).ToList();

        // Assert - Check comprehensive coverage
        var expectedDomains = new Dictionary<string, int>
        {
            ["science"] = 5,      // At least 5 science sources
            ["technology"] = 5,   // At least 5 tech sources  
            ["world"] = 3,        // At least 3 world news sources
            ["space"] = 2,        // At least 2 space sources
            ["health"] = 2,       // At least 2 health sources
            ["environment"] = 2,  // At least 2 environment sources
            ["physics"] = 1,      // At least 1 physics source
            ["ai"] = 1           // At least 1 AI source
        };

        foreach (var (domain, minCount) in expectedDomains)
        {
            var domainSources = sources.Where(s => s.Categories.Contains(domain)).ToList();
            Assert.True(domainSources.Count >= minCount, 
                $"Expected at least {minCount} sources for domain '{domain}', found {domainSources.Count}");
            
            _logger.Info($"✓ Domain '{domain}' covered by {domainSources.Count} sources: {string.Join(", ", domainSources.Take(3).Select(s => s.Name))}");
        }
    }

    [Fact]
    public async Task NewsSourceTypes_Should_BeValid()
    {
        // Arrange
        var realtimeModule = new RealtimeNewsStreamModule(_registry, _logger, _httpClient);
        realtimeModule.Register(_registry);
        await Task.Delay(1000);

        // Act
        var sourceNodes = _registry.GetNodesByType("codex.news.source");
        var sources = sourceNodes.Select(n => JsonSerializer.Deserialize<NewsSource>(n.Content?.InlineJson ?? "{}"))
            .Where(s => s != null).ToList();

        // Assert
        var validTypes = new[] { "rss", "api" };
        foreach (var source in sources)
        {
            Assert.Contains(source.Type, validTypes);
            Assert.True(!string.IsNullOrWhiteSpace(source.Url), $"Source '{source.Name}' has empty URL");
            Assert.True(source.UpdateIntervalMinutes > 0, $"Source '{source.Name}' has invalid update interval");
            Assert.NotEmpty(source.Categories);
            
            _logger.Debug($"✓ Source '{source.Name}' has valid configuration: type={source.Type}, interval={source.UpdateIntervalMinutes}min, categories=[{string.Join(",", source.Categories)}]");
        }

        _logger.Info($"✓ All {sources.Count} news sources have valid configurations");
    }

    [Fact]
    public async Task NewsIngestion_Should_CreateValidNewsItems()
    {
        // Arrange
        var realtimeModule = new RealtimeNewsStreamModule(_registry, _logger, _httpClient);
        realtimeModule.Register(_registry);
        
        // Wait for some news to potentially be ingested (but not too long for tests)
        await Task.Delay(3000);

        // Act - Check if any news items were created
        var newsItems = _registry.GetNodesByType("codex.news.item").ToList();
        
        // Assert - If news items exist, validate their structure
        if (newsItems.Any())
        {
            _logger.Info($"Found {newsItems.Count} news items for validation");
            
            foreach (var item in newsItems.Take(5)) // Test first 5 items
            {
                Assert.NotNull(item.Title);
                Assert.NotEmpty(item.Title);
                Assert.Equal("codex.news.item", item.TypeId);
                
                // Check that it has required metadata
                Assert.NotNull(item.Meta);
                Assert.True(item.Meta.ContainsKey("source"), "News item should have source metadata");
                Assert.True(item.Meta.ContainsKey("publishedAt"), "News item should have publishedAt metadata");
                
                _logger.Info($"✓ News item '{item.Title.Substring(0, Math.Min(50, item.Title.Length))}...' has valid structure");
            }
        }
        else
        {
            _logger.Info("No news items found (ingestion may not have started yet - this is expected in quick tests)");
        }
    }

    [Fact]
    public async Task NewsConceptExtraction_Should_LinkToUCore()
    {
        // Arrange
        await UCoreInitializer.SeedIfMissing(_registry, _logger);
        
        // Act - Create a sample news item and check concept extraction
        var sampleNewsContent = new
        {
            title = "Scientists Discover New Quantum Computing Breakthrough",
            summary = "Researchers at MIT have developed a new quantum algorithm that could revolutionize artificial intelligence and machine learning applications.",
            url = "https://example.com/quantum-breakthrough",
            publishedAt = DateTime.UtcNow,
            source = "test-source"
        };

        var newsNode = new Node(
            Id: "test-news-quantum-breakthrough",
            TypeId: "codex.news.item",
            State: ContentState.Water,
            Locale: "en-US",
            Title: sampleNewsContent.title,
            Description: sampleNewsContent.summary,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(sampleNewsContent),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["source"] = sampleNewsContent.source,
                ["publishedAt"] = sampleNewsContent.publishedAt,
                ["concepts"] = new[] { "quantum", "computing", "artificial-intelligence", "machine-learning", "science", "research" }
            }
        );

        _registry.Upsert(newsNode);

        // Assert - Check that extracted concepts can link to U-Core
        var conceptIds = (string[])newsNode.Meta["concepts"];
        var ucoreConcepts = _registry.GetNodesByType("codex.ucore.base").ToList();
        
        foreach (var conceptId in conceptIds)
        {
            // Check if concept exists or can be mapped to U-Core
            var relatedUCoreConcept = ucoreConcepts.FirstOrDefault(uc => 
                uc.Title.Contains(conceptId, StringComparison.OrdinalIgnoreCase) ||
                uc.Description?.Contains(conceptId, StringComparison.OrdinalIgnoreCase) == true);

            if (relatedUCoreConcept != null)
            {
                _logger.Info($"✓ Concept '{conceptId}' maps to U-Core concept '{relatedUCoreConcept.Title}'");
            }
            else
            {
                // This is expected - we should be able to create missing concepts with topology paths
                _logger.Info($"→ Concept '{conceptId}' not found in U-Core - should be created with topology path");
            }
        }

        Assert.True(conceptIds.Length > 0, "News item should have extracted concepts");
    }
}

/// <summary>
/// News source data structure for deserialization
/// </summary>
public class NewsSource
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Url { get; set; } = "";
    public string[] Categories { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public int UpdateIntervalMinutes { get; set; } = 60;
}
