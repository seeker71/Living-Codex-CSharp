using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using FluentAssertions;
using Moq;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class VisualValidationModuleTests
    {
        private readonly Mock<ICodexLogger> _loggerMock = new();

        [Fact]
        public async Task RenderComponentToImage_ShouldStoreNodeAndReturnSuccess()
        {
            var registry = new TestNodeRegistry();
            var routerMock = new Mock<IApiRouter>();
            var module = CreateModule(registry, routerMock, (_, _, _, _, _) => Task.FromResult(new byte[] { 0x01, 0x02, 0x03 }));

            var request = new RenderComponentRequest(
                ComponentId: "button-primary",
                ComponentCode: "<button class='btn-primary'>Click me</button>",
                Width: 800,
                Height: 600,
                Viewport: "desktop");

            var response = await module.RenderComponentToImage(request);
            var json = ToJson(response);

            json.GetProperty("success").GetBoolean().Should().BeTrue();
            json.GetProperty("data").GetProperty("imageNodeId").GetString().Should().Be("rendered-image.button-primary");

            registry.TryGet("rendered-image.button-primary", out var storedNode).Should().BeTrue();
            storedNode.Content.Should().NotBeNull();
            storedNode.Content!.InlineJson.Should().NotBeNull();
        }

        [Fact]
        public async Task AnalyzeRenderedImage_ShouldReturnAnalysisUsingRouter()
        {
            var registry = new TestNodeRegistry();
            var routerMock = new Mock<IApiRouter>();
            var module = CreateModule(registry, routerMock, (_, _, _, _, _) => Task.FromResult(Array.Empty<byte>()));

            var base64 = Convert.ToBase64String(new byte[] { 0x10, 0x20 });
            var renderNode = new Node(
                Id: "rendered-image.button-primary",
                TypeId: "codex.ui.rendered-image",
                State: ContentState.Water,
                Locale: "en",
                Title: "Rendered",
                Description: "",
                Content: new ContentRef("image/png", JsonSerializer.Serialize(new { base64Image = base64 }), null, null),
                Meta: new Dictionary<string, object>());
            registry.Upsert(renderNode);

            var analysisPayload = JsonSerializer.Serialize(new
            {
                resonanceScore = 0.92,
                joyScore = 0.85,
                unityScore = 0.9,
                clarityScore = 0.88,
                technicalQualityScore = 0.95,
                overallScore = 0.9,
                feedback = new[] { "Polished visuals" },
                issues = Array.Empty<string>(),
                recommendations = new[] { "Ship it" }
            });

            Func<JsonElement?, Task<object>> handler = _ =>
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    success = true,
                    data = new
                    {
                        response = analysisPayload
                    }
                }));
                return Task.FromResult<object>(doc.RootElement.Clone());
            };

            routerMock
                .Setup(r => r.TryGetHandler("ai", "process", out handler))
                .Returns(true);

            var request = new AnalyzeImageRequest(
                ImageNodeId: "rendered-image.button-primary",
                ComponentId: "button-primary",
                SpecVision: "Test vision",
                AnalysisType: null,
                Requirements: "Follow spec",
                Provider: "test",
                Model: "model");

            var response = await module.AnalyzeRenderedImage(request);
            var json = ToJson(response);

            GetProperty(json, "success").GetBoolean().Should().BeTrue();
            var analysis = GetProperty(GetProperty(json, "data"), "analysis");
            GetProperty(analysis, "overallScore").GetDouble().Should().Be(0.9);

            registry.TryGet("visual-analysis.rendered-image.button-primary", out var analysisNode).Should().BeTrue();
            analysisNode.TypeId.Should().Be("codex.ui.visual-analysis");
        }

        [Fact]
        public async Task ValidateComponentAgainstSpec_ShouldReturnValidationResult()
        {
            var registry = new TestNodeRegistry();
            var routerMock = new Mock<IApiRouter>();
            var module = CreateModule(registry, routerMock, (_, _, _, _, _) => Task.FromResult(Array.Empty<byte>()));

            var analysisResult = new VisualAnalysisResult(
                ComponentId: "button-primary",
                ImageNodeId: "rendered-image.button-primary",
                ResonanceScore: 0.9,
                JoyScore: 0.85,
                UnityScore: 0.88,
                ClarityScore: 0.9,
                TechnicalQualityScore: 0.95,
                OverallScore: 0.9,
                Feedback: new List<string> { "Looks great" },
                Issues: new List<string>(),
                Recommendations: new List<string> { "Ship it" },
                AnalyzedAt: DateTimeOffset.UtcNow.AddMinutes(-5));

            var analysisNode = new Node(
                Id: "visual-analysis.rendered-image.button-primary",
                TypeId: "codex.ui.visual-analysis",
                State: ContentState.Water,
                Locale: "en",
                Title: "Analysis",
                Description: "",
                Content: new ContentRef("application/json", JsonSerializer.Serialize(analysisResult), null, null),
                Meta: new Dictionary<string, object>
                {
                    ["componentId"] = "button-primary",
                    ["imageNodeId"] = "rendered-image.button-primary",
                    ["overallScore"] = analysisResult.OverallScore,
                    ["analyzedAt"] = analysisResult.AnalyzedAt
                });
            registry.Upsert(analysisNode);

            var request = new ValidateComponentRequest(
                ComponentId: "button-primary",
                MinimumScore: 0.7);

            var response = await module.ValidateComponentAgainstSpec(request);
            var json = ToJson(response);

            GetProperty(json, "success").GetBoolean().Should().BeTrue();
            var validation = GetProperty(GetProperty(json, "data"), "validation");
            GetProperty(validation, "passed").GetBoolean().Should().BeTrue();

            registry.TryGet("validation.button-primary", out var validationNode).Should().BeTrue();
            validationNode.TypeId.Should().Be("codex.ui.validation-result");
        }

        [Fact]
        public async Task ExecuteVisualValidationPipeline_ShouldReturnSuccessAndThreeSteps()
        {
            var registry = new TestNodeRegistry();
            var routerMock = new Mock<IApiRouter>();
            var module = CreateModule(registry, routerMock, (_, _, _, _, _) => Task.FromResult(new byte[] { 0x01 }));

            var analysisPayload = JsonSerializer.Serialize(new
            {
                resonanceScore = 0.92,
                joyScore = 0.88,
                unityScore = 0.9,
                clarityScore = 0.9,
                technicalQualityScore = 0.94,
                overallScore = 0.9,
                feedback = new[] { "Balanced" },
                issues = Array.Empty<string>(),
                recommendations = new[] { "Proceed" }
            });

            Func<JsonElement?, Task<object>> handler = _ =>
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    success = true,
                    data = new
                    {
                        response = analysisPayload
                    }
                }));
                return Task.FromResult<object>(doc.RootElement.Clone());
            };

            routerMock
                .Setup(r => r.TryGetHandler("ai", "process", out handler))
                .Returns(true);

            var request = new VisualValidationPipelineRequest(
                ComponentId: "card-component",
                ComponentCode: "<div class='card'>Content</div>",
                SpecVision: "Ensure clarity",
                Requirements: "Follow spec",
                Width: 1024,
                Height: 768,
                Viewport: "desktop",
                MinimumScore: 0.6,
                Provider: "test",
                Model: "model");

            var response = await module.ExecuteVisualValidationPipeline(request);
            var json = ToJson(response);

            GetProperty(json, "success").GetBoolean().Should().BeTrue();
            var steps = GetProperty(GetProperty(json, "data"), "steps");
            steps.ValueKind.Should().Be(JsonValueKind.Array);
            steps.GetArrayLength().Should().Be(3);
            steps.EnumerateArray()
                .Select(step => GetProperty(step, "status").GetString())
                .Should()
                .OnlyContain(status => status == "Success");

            registry.TryGet("validation.card-component", out var validationNode).Should().BeTrue();
            validationNode.TypeId.Should().Be("codex.ui.validation-result");
        }

        private VisualValidationModule CreateModule(
            TestNodeRegistry registry,
            Mock<IApiRouter> routerMock,
            Func<string, string, int?, int?, string?, Task<byte[]>> captureScreenshot)
        {
            return new VisualValidationModule(
                registry,
                _loggerMock.Object,
                new HttpClient(),
                routerMock.Object,
                captureScreenshot);
        }

        private static JsonElement ToJson(object result)
        {
            if (result is JsonElement element)
            {
                return element.Clone();
            }

            var json = JsonSerializer.Serialize(result);
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        private static JsonElement GetProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Element is not an object.");
            }

            if (element.TryGetProperty(propertyName, out var direct))
            {
                return direct;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value;
                }
            }

            throw new KeyNotFoundException($"Property '{propertyName}' not found.");
        }

        private sealed class TestNodeRegistry : INodeRegistry
        {
            private readonly Dictionary<string, Node> _nodes = new(StringComparer.OrdinalIgnoreCase);

            public Task InitializeAsync() => Task.CompletedTask;

            public void Upsert(Node node) => _nodes[node.Id] = node;

            public void Upsert(Edge edge)
            {
                // Edges not needed for these tests
            }

            public bool TryGet(string id, out Node node) => _nodes.TryGetValue(id, out node!);

            public Task<Node?> GetNodeAsync(string id)
            {
                _nodes.TryGetValue(id, out var node);
                return Task.FromResult(node);
            }

            public IEnumerable<Node> AllNodes() => _nodes.Values;

            public Task<IEnumerable<Node>> AllNodesAsync() => Task.FromResult<IEnumerable<Node>>(_nodes.Values);

            public IEnumerable<Node> GetNodesByType(string typeId) =>
                _nodes.Values.Where(n => string.Equals(n.TypeId, typeId, StringComparison.OrdinalIgnoreCase));

            public Task<IEnumerable<Node>> GetNodesByTypeAsync(string typeId) =>
                Task.FromResult(GetNodesByType(typeId));

            public IEnumerable<Node> GetNodesByState(ContentState state) =>
                _nodes.Values.Where(n => n.State == state);

            public Task<IEnumerable<Node>> GetNodesByStateAsync(ContentState state) =>
                Task.FromResult(GetNodesByState(state));

            public IEnumerable<Edge> AllEdges() => Array.Empty<Edge>();

            public Task<IEnumerable<Edge>> AllEdgesAsync() => Task.FromResult<IEnumerable<Edge>>(Array.Empty<Edge>());

            public Node? GetNode(string id)
            {
                _nodes.TryGetValue(id, out var node);
                return node;
            }

            public void RemoveNode(string nodeId) => _nodes.Remove(nodeId);

            public Task<UnifiedStorageStats> GetStatsAsync()
            {
                var ice = new CodexBootstrap.Core.Storage.IceStorageStats(0, 0, 0, DateTime.UtcNow, "test", new Dictionary<string, object>());
                var water = new CodexBootstrap.Core.Storage.WaterStorageStats(0, 0, 0, DateTime.UtcNow, TimeSpan.Zero, new Dictionary<string, object>());
                return Task.FromResult(new UnifiedStorageStats(ice, water, 0, 0, DateTime.UtcNow));
            }

            public Task CleanupExpiredWaterNodesAsync() => Task.CompletedTask;

            public IEnumerable<Edge> GetEdgesFrom(string fromId) => Array.Empty<Edge>();

            public IEnumerable<Edge> GetEdgesTo(string toId) => Array.Empty<Edge>();

            public IEnumerable<Edge> GetEdges() => Array.Empty<Edge>();

            public Edge? GetEdge(string edgeId) => null;

            public Edge? GetEdge(string fromId, string toId) => null;

            public void RemoveEdge(string edgeId)
            {
            }

            public void RemoveEdge(string fromId, string toId)
            {
            }

            public void Clear() => _nodes.Clear();
        }
    }
}
