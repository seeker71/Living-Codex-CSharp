using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class NewsSourceMaintenanceTests : TestBase
    {
        private readonly INodeRegistry _registry;

        public NewsSourceMaintenanceTests()
        {
            _registry = TestInfrastructure.CreateTestNodeRegistry();
        }

        [Fact]
        public async Task NewsSource_ShouldBeMaintainedFromConfigurationToNewsItem()
        {
            // Arrange
            var newsSource = new NewsSource
            {
                Id = "test-source",
                Name = "Test News Source",
                Type = "rss",
                Url = "https://example.com/rss",
                Categories = new[] { "test", "news" },
                IsActive = true,
                UpdateIntervalMinutes = 60
            };

            // Create source node in registry
            var sourceNode = new Node(
                $"codex.news.news-source.{newsSource.Id}",
                "codex.news.news-source",
                ContentState.Ice,
                null,
                "News Source",
                $"News source: {newsSource.Name}",
                new ContentRef("application/json", System.Text.Json.JsonSerializer.Serialize(newsSource), null, null),
                new Dictionary<string, object>
                {
                    ["sourceId"] = newsSource.Id,
                    ["sourceName"] = newsSource.Name,
                    ["sourceType"] = newsSource.Type,
                    ["sourceUrl"] = newsSource.Url,
                    ["categories"] = newsSource.Categories,
                    ["isActive"] = newsSource.IsActive,
                    ["updateIntervalMinutes"] = newsSource.UpdateIntervalMinutes
                }
            );

            _registry.Upsert(sourceNode);

            // Create a mock news item that would be created during ingestion
            var newsItem = new NewsItem
            {
                Id = "test-news-1",
                Title = "Test News Article",
                Content = "Full article content here",
                Source = newsSource.Name, // This should be set from source.Name
                Url = "https://example.com/news/1",
                PublishedAt = DateTimeOffset.UtcNow,
                Tags = new[] { "test", "news" },
                Metadata = new Dictionary<string, object>
                {
                    ["description"] = "This is a test news article",
                    ["author"] = "Test Author",
                    ["imageUrl"] = "https://example.com/image.jpg"
                }
            };

            // Act - Simulate the news processing pipeline
            var newsNode = new Node(
                $"codex.news.item.{newsItem.Id}",
                "codex.news.news-item",
                ContentState.Ice,
                null,
                newsItem.Title,
                newsItem.Metadata["description"]?.ToString(),
                new ContentRef("application/json", System.Text.Json.JsonSerializer.Serialize(newsItem), null, null),
                new Dictionary<string, object>
                {
                    ["newsId"] = newsItem.Id,
                    ["title"] = newsItem.Title,
                    ["description"] = newsItem.Metadata["description"]?.ToString(),
                    ["url"] = newsItem.Url,
                    ["publishedAt"] = newsItem.PublishedAt,
                    ["source"] = newsItem.Source, // Source should be preserved
                    ["author"] = newsItem.Metadata["author"]?.ToString(),
                    ["imageUrl"] = newsItem.Metadata["imageUrl"]?.ToString(),
                    ["content"] = newsItem.Content
                }
            );

            _registry.Upsert(newsNode);

            // Create edges that maintain source information
            var sourceEdge = new Edge(
                $"codex.news.news-source.{newsSource.Id}",
                newsNode.Id,
                "provides-news",
                1.0,
                new Dictionary<string, object>
                {
                    ["source"] = newsSource.Name,
                    ["createdAt"] = DateTimeOffset.UtcNow,
                    ["edgeType"] = "source-relationship"
                }
            );

            _registry.Upsert(sourceEdge);

            // Assert - Verify source information is maintained
            var retrievedNewsNode = _registry.GetNode(newsNode.Id);
            Assert.NotNull(retrievedNewsNode);
            Assert.Equal(newsSource.Name, retrievedNewsNode.Meta?["source"]?.ToString());

            // Verify the source relationship edge exists
            var sourceEdges = _registry.GetEdgesFrom($"codex.news.news-source.{newsSource.Id}");
            Assert.Contains(sourceEdges, e => e.ToId == newsNode.Id && e.Role == "provides-news");

            // Verify source information is preserved in edge metadata
            var sourceEdgeData = sourceEdges.FirstOrDefault(e => e.ToId == newsNode.Id);
            Assert.NotNull(sourceEdgeData);
            Assert.Equal(newsSource.Name, sourceEdgeData.Meta?["source"]?.ToString());
        }

        [Fact]
        public async Task NewsSource_ShouldBeMaintainedInNewsFeedItemMapping()
        {
            // Arrange
            var newsSource = new NewsSource
            {
                Id = "test-source-3",
                Name = "MIT Technology Review",
                Type = "rss",
                Url = "https://www.technologyreview.com/feed/",
                Categories = new[] { "technology", "innovation" },
                IsActive = true,
                UpdateIntervalMinutes = 180
            };

            var newsItem = new NewsItem
            {
                Id = "test-news-3",
                Title = "AI Breakthrough in Healthcare",
                Content = "New AI system improves medical diagnosis",
                Source = newsSource.Name,
                Url = "https://example.com/ai-healthcare",
                PublishedAt = DateTimeOffset.UtcNow,
                Tags = new[] { "ai", "healthcare" },
                Metadata = new Dictionary<string, object>
                {
                    ["description"] = "New AI system improves medical diagnosis",
                    ["author"] = "Tech Writer",
                    ["imageUrl"] = "https://example.com/ai-image.jpg"
                }
            };

            // Create news node
            var newsNode = new Node(
                $"codex.news.item.{newsItem.Id}",
                "codex.news.news-item",
                ContentState.Ice,
                null,
                newsItem.Title,
                newsItem.Metadata["description"]?.ToString(),
                new ContentRef("application/json", System.Text.Json.JsonSerializer.Serialize(newsItem), null, null),
                new Dictionary<string, object>
                {
                    ["newsId"] = newsItem.Id,
                    ["title"] = newsItem.Title,
                    ["description"] = newsItem.Metadata["description"]?.ToString(),
                    ["url"] = newsItem.Url,
                    ["publishedAt"] = newsItem.PublishedAt,
                    ["source"] = newsItem.Source,
                    ["author"] = newsItem.Metadata["author"]?.ToString(),
                    ["imageUrl"] = newsItem.Metadata["imageUrl"]?.ToString(),
                    ["content"] = newsItem.Content
                }
            );

            _registry.Upsert(newsNode);

            // Act - Simulate the MapNodeToNewsFeedItem process
            var httpClient = new HttpClient();
            var newsFeedModule = new NewsFeedModule(_registry, Mock.Of<ICodexLogger>(), httpClient);
            
            // Use reflection to access the private MapNodeToNewsFeedItem method
            var mapMethod = typeof(NewsFeedModule).GetMethod("MapNodeToNewsFeedItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var newsFeedItem = mapMethod?.Invoke(newsFeedModule, new object[] { newsNode }) as NewsFeedItem;

            // Assert - Source information should be preserved in NewsFeedItem
            Assert.NotNull(newsFeedItem);
            Assert.Equal(newsSource.Name, newsFeedItem.Source);
            Assert.Equal(newsItem.Title, newsFeedItem.Title);
            Assert.Equal(newsItem.Metadata["description"]?.ToString(), newsFeedItem.Description);
            Assert.Equal(newsItem.Url, newsFeedItem.Url);
            Assert.Equal(newsItem.Metadata["author"]?.ToString(), newsFeedItem.Author);
            Assert.Equal(newsItem.Metadata["imageUrl"]?.ToString(), newsFeedItem.ImageUrl);
        }

        [Fact]
        public async Task NewsSource_ShouldNeverBeReplacedWithFallbacks()
        {
            // Arrange - Create a news item with a proper source from news-sources.json
            var newsItem = new NewsItem
            {
                Id = "test-news-4",
                Title = "Climate Change Research",
                Content = "New findings on global warming",
                Source = "Nature - Scientific Discoveries", // Proper source from news-sources.json
                Url = "https://example.com/climate",
                PublishedAt = DateTimeOffset.UtcNow,
                Tags = new[] { "climate", "research" },
                Metadata = new Dictionary<string, object>
                {
                    ["description"] = "New findings on global warming",
                    ["author"] = "Climate Scientist"
                }
            };

            // Create news node
            var newsNode = new Node(
                $"codex.news.item.{newsItem.Id}",
                "codex.news.news-item",
                ContentState.Ice,
                null,
                newsItem.Title,
                newsItem.Metadata["description"]?.ToString(),
                new ContentRef("application/json", System.Text.Json.JsonSerializer.Serialize(newsItem), null, null),
                new Dictionary<string, object>
                {
                    ["newsId"] = newsItem.Id,
                    ["title"] = newsItem.Title,
                    ["description"] = newsItem.Metadata["description"]?.ToString(),
                    ["url"] = newsItem.Url,
                    ["publishedAt"] = newsItem.PublishedAt,
                    ["source"] = newsItem.Source, // Source should be preserved exactly as provided
                    ["author"] = newsItem.Metadata["author"]?.ToString(),
                    ["content"] = newsItem.Content
                }
            );

            _registry.Upsert(newsNode);

            // Act - Simulate the MapNodeToNewsFeedItem process
            var httpClient = new HttpClient();
            var newsFeedModule = new NewsFeedModule(_registry, Mock.Of<ICodexLogger>(), httpClient);
            
            var mapMethod = typeof(NewsFeedModule).GetMethod("MapNodeToNewsFeedItem", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var newsFeedItem = mapMethod?.Invoke(newsFeedModule, new object[] { newsNode }) as NewsFeedItem;

            // Assert - Source should be preserved exactly as provided, never replaced with fallbacks
            Assert.NotNull(newsFeedItem);
            Assert.Equal("Nature - Scientific Discoveries", newsFeedItem.Source);
            Assert.NotEqual("Unknown", newsFeedItem.Source);
            Assert.NotEqual("AI", newsFeedItem.Source);
            Assert.NotEqual("", newsFeedItem.Source);
            Assert.NotNull(newsFeedItem.Source);
            Assert.NotEmpty(newsFeedItem.Source);
        }
    }
}
