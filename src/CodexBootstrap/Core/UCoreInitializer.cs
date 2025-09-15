using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CodexBootstrap.Core
{
    public static class UCoreInitializer
    {
        public static void SeedIfMissing(NodeRegistry registry, ICodexLogger logger)
        {
            try
            {
                // Seed ontology root if missing
                var rootId = "u-core-ontology-root";
                var hasRoot = registry.GetNodesByType("codex.ontology/root").Any();
                if (!hasRoot)
                {
                    var rootNode = new Node(
                        Id: rootId,
                        TypeId: "codex.ontology/root",
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: "U-CORE Ontology Root",
                        Description: "Root node for U-CORE ontology",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new { id = rootId, name = "u-core" }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object>
                        {
                            ["name"] = "u-core",
                            ["version"] = "1.0.0"
                        }
                    );
                    registry.Upsert(rootNode);
                }

                // Seed axes if none exist
                var axisType = "codex.ontology.axis";
                var hasAxes = registry.GetNodesByType(axisType).Any();
                if (!hasAxes)
                {
                    var defaults = new List<(string name, string[] keywords)>
                    {
                        ("abundance", new [] { "abundance", "amplification", "growth", "prosperity", "opportunity" }),
                        ("unity", new [] { "unity", "collaboration", "collective", "community", "global" }),
                        ("resonance", new [] { "resonance", "harmony", "coherence", "joy", "love", "peace", "wisdom" }),
                        ("innovation", new [] { "innovation", "breakthrough", "cutting-edge", "new", "discovery" }),
                        ("science", new [] { "science", "research", "study", "experiment", "data" }),
                        ("consciousness", new [] { "consciousness", "awareness", "mindfulness", "enlightenment", "awakening", "presence" }),
                        ("impact", new [] { "impact", "influence", "change", "transformation", "effect", "consequence" })
                    };

                    foreach (var (name, keywords) in defaults)
                    {
                        var node = new Node(
                            Id: $"u-core-axis-{name.ToLowerInvariant()}",
                            TypeId: axisType,
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
                        registry.Upsert(node);
                    }
                }

                logger.Info("U-CORE initialized (root and axes ensured).");
            }
            catch (Exception ex)
            {
                logger.Warn($"U-CORE initialization warning: {ex.Message}");
            }
        }
    }
}


