using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class PersonalNewsMatchingTests
    {
        [Fact]
        public async Task GetUserNewsFeed_ShouldPrioritizeItemsMatchingUserInterests()
        {
            // Arrange
            var registry = TestInfrastructure.CreateTestNodeRegistry();
            var logger = TestInfrastructure.CreateTestLogger();
            var httpClient = new HttpClient();
            var module = new NewsFeedModule(registry, logger, httpClient);

            var now = DateTime.UtcNow;

            // Seed user with interests
            var userId = "user.alice";
            var userNode = new Node(
                Id: userId,
                TypeId: "codex.user",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: "Alice",
                Description: "Test user",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new { name = "Alice" }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["interests"] = "quantum, computing, ai"
                }
            );
            registry.Upsert(userNode);

            // Helper to create news node
            Node CreateNews(string id, string title, string description, DateTime published, string source = "test")
            {
                return new Node(
                    Id: id,
                    TypeId: "codex.news.item",
                    State: ContentState.Water,
                    Locale: "en-US",
                    Title: title,
                    Description: description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new { title, summary = description }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["source"] = source,
                        ["publishedAt"] = published,
                        ["content"] = description
                    }
                );
            }

            // Seed matching items (quantum/AI)
            registry.Upsert(CreateNews(
                id: "news-1",
                title: "Quantum computing breakthrough at MIT",
                description: "A new quantum algorithm improves AI performance.",
                published: now.AddHours(-1)
            ));
            registry.Upsert(CreateNews(
                id: "news-2",
                title: "AI system accelerates quantum simulations",
                description: "Researchers combine AI with quantum methods.",
                published: now.AddHours(-2)
            ));

            // Seed non-matching items (sports)
            registry.Upsert(CreateNews(
                id: "news-3",
                title: "Local sports team wins championship",
                description: "A big win in regional sports.",
                published: now.AddHours(-1)
            ));
            registry.Upsert(CreateNews(
                id: "news-4",
                title: "Football transfer rumors heat up",
                description: "Speculation in the football world.",
                published: now.AddHours(-3)
            ));

            // Act
            var resultObj = await module.GetUserNewsFeed(userId, limit: 10, hoursBack: 24);

            // Assert
            Assert.NotNull(resultObj);
            var result = Assert.IsType<NewsFeedResponse>(resultObj);
            Assert.True(result.TotalCount > 0, "Expected at least one news item returned");

            // Ensure matching items are present and prioritized
            var titles = result.Items.Select(i => i.Title.ToLowerInvariant()).ToList();
            Assert.Contains(titles, t => t.Contains("quantum") || t.Contains("ai"));

            // Top item should be one of the matching topics due to relevance scoring
            var topTitle = result.Items.First().Title.ToLowerInvariant();
            Assert.True(topTitle.Contains("quantum") || topTitle.Contains("ai"),
                $"Top item '{result.Items.First().Title}' should match user interests");
        }
    }
}


