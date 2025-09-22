using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodexBootstrap.Core
{
    public static class UCoreInitializer
    {
        public static async Task SeedIfMissing(INodeRegistry registry, ICodexLogger logger)
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

                // Load axes from config file
                await LoadAxesFromConfig(registry, logger);
                
                // Load core concepts from config file
                await LoadConceptsFromConfig(registry, logger);
                
                // Load core relationships from config file
                await LoadRelationshipsFromConfig(registry, logger);

                logger.Info("U-CORE initialized (root and axes ensured).");
            }
            catch (Exception ex)
            {
                logger.Warn($"U-CORE initialization warning: {ex.Message}");
            }
        }

        private static async Task LoadAxesFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            var axisType = "codex.ontology.axis";
            var hasAxes = registry.GetNodesByType(axisType).Any();
            if (hasAxes)
            {
                logger.Info("U-CORE axes already exist, skipping config loading");
                return;
            }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-axes.json");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core axes config file not found: {configPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<AxesConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (config?.Axes != null)
                {
                    foreach (var axis in config.Axes)
                    {
                        var node = new Node(
                            Id: $"u-core-axis-{axis.Id}",
                            TypeId: axisType,
                            State: ContentState.Ice,
                            Locale: "en-US",
                            Title: axis.Name,
                            Description: axis.Description,
                            Content: new ContentRef(
                                MediaType: "application/json",
                                InlineJson: JsonSerializer.Serialize(axis),
                                InlineBytes: null,
                                ExternalUri: null
                            ),
                            Meta: new Dictionary<string, object>
                            {
                                ["name"] = axis.Name,
                                ["keywords"] = axis.Keywords,
                                ["level"] = axis.Level,
                                ["dimensions"] = axis.Dimensions,
                                ["parentAxes"] = axis.ParentAxes,
                                ["childAxes"] = axis.ChildAxes
                            }
                        );
                        registry.Upsert(node);
                    }
                    logger.Info($"Loaded {config.Axes.Count} axes from config");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading axes config: {ex.Message}", ex);
            }
        }

        private static async Task LoadConceptsFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            var conceptType = "codex.concept";
            var hasCoreConcepts = registry.GetNodesByType(conceptType).Any(n => n.Meta?.ContainsKey("level") == true && (int)n.Meta["level"] <= 1);
            if (hasCoreConcepts)
            {
                logger.Info("Core concepts already exist, skipping config loading");
                return;
            }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-concepts.json");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core concepts config file not found: {configPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<ConceptsConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (config?.Concepts != null)
                {
                    foreach (var concept in config.Concepts)
                    {
                        var node = new Node(
                            Id: $"u-core-concept-{concept.Id}",
                            TypeId: concept.TypeId,
                            State: ContentState.Ice,
                            Locale: "en-US",
                            Title: concept.Name,
                            Description: concept.Description,
                            Content: new ContentRef(
                                MediaType: "application/json",
                                InlineJson: JsonSerializer.Serialize(concept),
                                InlineBytes: null,
                                ExternalUri: null
                            ),
                            Meta: new Dictionary<string, object>
                            {
                                ["name"] = concept.Name,
                                ["keywords"] = concept.Keywords,
                                ["level"] = concept.Level,
                                ["parentConcepts"] = concept.ParentConcepts,
                                ["childConcepts"] = concept.ChildConcepts,
                                ["axes"] = concept.Axes
                            }
                        );
                        registry.Upsert(node);
                    }
                    logger.Info($"Loaded {config.Concepts.Count} core concepts from config");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading concepts config: {ex.Message}", ex);
            }
        }

        private static async Task LoadRelationshipsFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            // Use the core relationship type to align with tests and ontology spec
            var relationshipType = "codex.relationship.core";
            var hasRelationships = registry.GetNodesByType(relationshipType).Any();
            if (hasRelationships)
            {
                logger.Info("Core relationships already exist, skipping config loading");
                return;
            }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-relationships.json");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core relationships config file not found: {configPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<RelationshipsConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (config?.Relationships != null)
                {
                    foreach (var relationship in config.Relationships)
                    {
                        var node = new Node(
                            Id: $"u-core-relationship-{relationship.Id}",
                            TypeId: relationshipType,
                            State: ContentState.Ice,
                            Locale: "en-US",
                            Title: relationship.Name,
                            Description: relationship.Description,
                            Content: new ContentRef(
                                MediaType: "application/json",
                                InlineJson: JsonSerializer.Serialize(relationship),
                                InlineBytes: null,
                                ExternalUri: null
                            ),
                            Meta: new Dictionary<string, object>
                            {
                                ["name"] = relationship.Name,
                                ["fromConcept"] = relationship.FromConcept,
                                ["toConcept"] = relationship.ToConcept,
                                ["weight"] = relationship.Weight,
                                ["bidirectional"] = relationship.Bidirectional,
                                ["transitive"] = relationship.Transitive,
                                ["symmetric"] = relationship.Symmetric,
                                ["reflexive"] = relationship.Reflexive
                            }
                        );
                        registry.Upsert(node);
                    }
                    logger.Info($"Loaded {config.Relationships.Count} core relationships from config");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading relationships config: {ex.Message}", ex);
            }
        }

        // Config data structures
        private class AxesConfig
        {
            public List<AxisConfig> Axes { get; set; } = new();
        }

        private class AxisConfig
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string TypeId { get; set; } = "";
            public int Level { get; set; }
            public string[] Keywords { get; set; } = Array.Empty<string>();
            public string[] Dimensions { get; set; } = Array.Empty<string>();
            public string[] ParentAxes { get; set; } = Array.Empty<string>();
            public string[] ChildAxes { get; set; } = Array.Empty<string>();
        }

        private class ConceptsConfig
        {
            public List<ConceptConfig> Concepts { get; set; } = new();
        }

        private class ConceptConfig
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string TypeId { get; set; } = "";
            public int Level { get; set; }
            public string[] Keywords { get; set; } = Array.Empty<string>();
            public string[] ParentConcepts { get; set; } = Array.Empty<string>();
            public string[] ChildConcepts { get; set; } = Array.Empty<string>();
            public string[] Axes { get; set; } = Array.Empty<string>();
        }

        private class RelationshipsConfig
        {
            public List<RelationshipConfig> Relationships { get; set; } = new();
        }

        private class RelationshipConfig
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string FromConcept { get; set; } = "";
            public string ToConcept { get; set; } = "";
            public double Weight { get; set; }
            public bool Bidirectional { get; set; }
            public bool Transitive { get; set; }
            public bool Symmetric { get; set; }
            public bool Reflexive { get; set; }
        }
    }
}


