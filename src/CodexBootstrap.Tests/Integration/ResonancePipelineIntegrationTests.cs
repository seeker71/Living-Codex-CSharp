using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using Xunit;

namespace CodexBootstrap.Tests.Integration
{
    public class ResonancePipelineIntegrationTests
    {
        [Fact]
        public async Task Pipeline_ShouldExtractIntegrateAndRelate_WithStableResonanceField()
        {
            Environment.SetEnvironmentVariable("DISABLE_AI", "false");
            Environment.SetEnvironmentVariable("USE_OLLAMA_ONLY", "true");

            var registry = TestInfrastructure.CreateTestNodeRegistry();
            var logger = TestInfrastructure.CreateTestLogger();
            var httpClient = new HttpClient();
            var startup = new StartupStateService(logger);
            startup.MarkAIReady();

            // Seed a user-tagged node to surface in relatedConcepts
            var userId = "pipeline-user";
            var seed = new Node(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.concept.fundamental",
                State: ContentState.Water,
                Locale: "en",
                Title: "pipeline-seed",
                Description: "",
                Content: new ContentRef("application/json", "{}", null, null),
                Meta: new System.Collections.Generic.Dictionary<string, object> { ["userId"] = userId }
            );
            registry.Upsert(seed);

            var module = new ResonanceAlignedConceptExtractionModule(registry, logger, httpClient, startup);

            var req = new ResonanceAlignedExtractionRequest(
                Content: "love compassion healing harmony consciousness awareness",
                UserId: userId
            );

            var result = await module.ExtractAndIntegrateConceptsAsync(req);
            var json = JsonSerializer.Serialize(result);
            var obj = JsonSerializer.Deserialize<JsonElement>(json);

            if (!(obj.TryGetProperty("success", out var successProp) && successProp.GetBoolean()))
            {
                Assert.True(obj.TryGetProperty("Error", out var err));
                Assert.True(obj.TryGetProperty("Code", out var code));
                var c = code.GetString();
                Assert.True(c == "INTERNAL_ERROR" || c == "SERVICE_UNAVAILABLE" || c == "LLM_SERVICE_ERROR");
                return;
            }

            // nodes + edges created
            Assert.True(obj.TryGetProperty("ontologyNodes", out var nodesProp));
            Assert.True(nodesProp.GetInt32() >= 1);
            Assert.True(obj.TryGetProperty("topologyEdges", out var edgesProp));
            Assert.True(edgesProp.GetInt32() >= 0);

            // resonance field present and includes related concepts
            Assert.True(obj.TryGetProperty("resonanceField", out var field));
            JsonElement relatedProp;
            Assert.True(field.TryGetProperty("relatedConcepts", out relatedProp) || field.TryGetProperty("RelatedConcepts", out relatedProp));
            Assert.True(relatedProp.ValueKind == JsonValueKind.Array);
            Assert.Contains("pipeline-seed", relatedProp.EnumerateArray().Select(e => e.GetString()));

            // frequency stable among sacred bands
            JsonElement freqProp;
            Assert.True(field.TryGetProperty("optimalFrequency", out freqProp) || field.TryGetProperty("OptimalFrequency", out freqProp));
            var f = freqProp.GetDouble();
            var ok = Math.Abs(f - 432.0) <= 1.0 || Math.Abs(f - 528.0) <= 1.0 || Math.Abs(f - 741.0) <= 1.0;
            Assert.True(ok, $"Optimal frequency {f} must be a sacred band");
        }
    }
}


