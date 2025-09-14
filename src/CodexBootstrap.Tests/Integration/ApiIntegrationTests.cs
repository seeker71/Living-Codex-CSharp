using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Tests;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<TestBase>
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiIntegrationTests(TestBase testBase)
        {
            _httpClient = testBase.HttpClient;
            _jsonOptions = testBase.JsonOptions;
        }

        [Fact]
        public async Task HealthEndpoint_ShouldReturnHealthyStatus()
        {
            // Act
            var response = await _httpClient.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("healthy");
        }

        [Fact]
        public async Task HelloEndpoint_ShouldReturnGreeting()
        {
            // Act
            var response = await _httpClient.GetAsync("/hello");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Hello");
        }

        [Fact]
        public async Task HelloHotReloadTestEndpoint_ShouldReturnTestMessage()
        {
            // Act
            var response = await _httpClient.GetAsync("/hello/hot-reload-test");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Hot-reload test endpoint");
        }

        [Fact]
        public async Task ApiDiscoveryEndpoint_ShouldReturnAvailableEndpoints()
        {
            // Act
            var response = await _httpClient.GetAsync("/api/discovery");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var discovery = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            discovery.TryGetProperty("endpoints", out var endpoints).Should().BeTrue();
            endpoints.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ModulesEndpoint_ShouldReturnLoadedModules()
        {
            // Act
            var response = await _httpClient.GetAsync("/api/modules");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var modules = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            modules.TryGetProperty("modules", out var modulesArray).Should().BeTrue();
            modulesArray.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task SpecRoutesAllEndpoint_ShouldReturnAllRoutes()
        {
            // Act
            var response = await _httpClient.GetAsync("/spec/routes/all");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var routes = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            routes.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task NewsSourcesEndpoint_ShouldReturnNewsSources()
        {
            // Act
            var response = await _httpClient.GetAsync("/news/sources");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var sources = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            sources.TryGetProperty("sources", out var sourcesArray).Should().BeTrue();
            sourcesArray.GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task PerformanceMetricsEndpoint_ShouldReturnMetrics()
        {
            // Act
            var response = await _httpClient.GetAsync("/performance/metrics");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            var metrics = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
            metrics.TryGetProperty("metrics", out var metricsObj).Should().BeTrue();
        }

        [Fact]
        public async Task AIExtractConceptsEndpoint_WithValidRequest_ShouldReturnConcepts()
        {
            // Arrange
            var request = new
            {
                text = "Artificial intelligence is transforming the world",
                maxConcepts = 5
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/extract-concepts", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            // Note: This might return fallback data if LLM is not available
        }

        [Fact]
        public async Task AIScoreAnalysisEndpoint_WithValidRequest_ShouldReturnScores()
        {
            // Arrange
            var request = new
            {
                concepts = new[]
                {
                    new { name = "AI", score = 0.9, category = "Technology" },
                    new { name = "Innovation", score = 0.8, category = "Process" }
                }
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/score-analysis", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            // Note: This might return fallback data if LLM is not available
        }

        [Fact]
        public async Task AIFractalTransformEndpoint_WithValidRequest_ShouldReturnTransformation()
        {
            // Arrange
            var request = new
            {
                content = "Test content for transformation",
                transformationType = "abundance",
                depth = 3
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/fractal-transform", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            // Note: This might return fallback data if LLM is not available
        }

        [Fact]
        public async Task AIExtractConceptsEndpoint_WithEmptyText_ShouldReturnError()
        {
            // Arrange
            var request = new
            {
                text = "",
                maxConcepts = 5
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/extract-concepts", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue(); // API returns 200 with error in body
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            success.GetBoolean().Should().BeFalse();
            result.TryGetProperty("error", out var error).Should().BeTrue();
        }

        [Fact]
        public async Task AIScoreAnalysisEndpoint_WithEmptyConcepts_ShouldReturnError()
        {
            // Arrange
            var request = new
            {
                concepts = new object[0]
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/score-analysis", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue(); // API returns 200 with error in body
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            success.GetBoolean().Should().BeFalse();
            result.TryGetProperty("error", out var error).Should().BeTrue();
        }

        [Fact]
        public async Task AIFractalTransformEndpoint_WithInvalidDepth_ShouldReturnError()
        {
            // Arrange
            var request = new
            {
                content = "Test content",
                transformationType = "abundance",
                depth = 0
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/fractal-transform", content);

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue(); // API returns 200 with error in body
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
            result.TryGetProperty("success", out var success).Should().BeTrue();
            success.GetBoolean().Should().BeFalse();
            result.TryGetProperty("error", out var error).Should().BeTrue();
        }

        [Fact]
        public async Task NonExistentEndpoint_ShouldReturn404()
        {
            // Act
            var response = await _httpClient.GetAsync("/non-existent-endpoint");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task InvalidJsonRequest_ShouldReturn400()
        {
            // Arrange
            var invalidJson = "{ invalid json }";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/ai/extract-concepts", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}
