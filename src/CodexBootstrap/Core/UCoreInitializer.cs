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
                logger.Info("=== UCoreInitializer.SeedIfMissing called ===");
                Console.WriteLine("=== UCoreInitializer.SeedIfMissing called ===");
                
                // Stage 1: Seed ontology root if missing
                await SeedOntologyRoot(registry, logger);
                
                // Stage 2: Load all concepts first
                await LoadConceptsFromConfig(registry, logger);
                
                // Stage 3: Load all axes
                await LoadAxesFromConfig(registry, logger);
                
                // Stage 4: Load all relationships
                await LoadRelationshipsFromConfig(registry, logger);
                
                // Stage 5: Create implicit edges from concept properties
                await CreateImplicitEdges(registry, logger);
                
                // Stage 6: Validate all referenced concepts exist
                await ValidateReferencedConcepts(registry, logger);

                logger.Info("U-CORE ontology seeding completed successfully");
                Console.WriteLine("U-CORE ontology seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.Error($"Error during U-CORE seeding: {ex.Message}", ex);
                Console.WriteLine($"Error during U-CORE seeding: {ex.Message}");
                throw;
            }
        }

        private static async Task SeedOntologyRoot(INodeRegistry registry, ICodexLogger logger)
        {
            logger.Info("Stage 1: Seeding ontology root");
            Console.WriteLine("Stage 1: Seeding ontology root");
            
            var rootId = "u-core-ontology-root";
            var hasRoot = registry.GetNodesByType("codex.ontology/root").Any();
            Console.WriteLine($"Root node exists: {hasRoot}");
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
                logger.Info("Created U-CORE ontology root node");
                Console.WriteLine("Created U-CORE ontology root node");
            }
        }

        private static async Task CreateImplicitEdges(INodeRegistry registry, ICodexLogger logger)
        {
            logger.Info("Stage 5: Creating implicit edges from concept properties");
            Console.WriteLine("Stage 5: Creating implicit edges from concept properties");
            
            // Create edges derived from axes (dimensions, keywords, parent/child, type) and ensure missing nodes
            Console.WriteLine("About to call EnsureAxisDerivedEdgesAndConcepts");
            EnsureAxisDerivedEdgesAndConcepts(registry, logger);
            Console.WriteLine("EnsureAxisDerivedEdgesAndConcepts completed");

            // Create edges derived from concepts (parent/child hierarchy and axis membership)
            Console.WriteLine("About to call EnsureConceptDerivedEdges");
            EnsureConceptDerivedEdges(registry, logger);
            Console.WriteLine("EnsureConceptDerivedEdges completed");

            // Ensure baseline root → concept edges so every concept has at least one relationship
            Console.WriteLine("About to call EnsureBaselineConceptEdges");
            EnsureBaselineConceptEdges(registry, logger);
            Console.WriteLine("EnsureBaselineConceptEdges completed");
        }

        private static async Task ValidateReferencedConcepts(INodeRegistry registry, ICodexLogger logger)
        {
            logger.Info("Stage 6: Validating all referenced concepts exist");
            Console.WriteLine("Stage 6: Validating all referenced concepts exist");
            
            // Get all concept nodes
            var conceptNodes = new List<Node>();
            conceptNodes.AddRange(registry.GetNodesByTypePrefix("codex.ucore"));
            conceptNodes.AddRange(registry.GetNodesByTypePrefix("codex.concept"));
            conceptNodes = conceptNodes.Distinct().ToList();
            
            var missingConcepts = new List<string>();
            
            foreach (var concept in conceptNodes)
            {
                // Check parent concepts
                var parents = GetStringArray(concept.Meta, "parentConcepts");
                foreach (var parent in parents)
                {
                    var parentId = parent == "meta-identity" ? "u-core-meta-identity" : $"u-core-concept-{parent}";
                    if (!registry.TryGet(parentId, out _))
                    {
                        missingConcepts.Add($"Parent concept '{parentId}' referenced by '{concept.Id}'");
                    }
                }
                
                // Check child concepts
                var children = GetStringArray(concept.Meta, "childConcepts");
                foreach (var child in children)
                {
                    var childId = child == "meta-identity" ? "u-core-meta-identity" : $"u-core-concept-{child}";
                    if (!registry.TryGet(childId, out _))
                    {
                        missingConcepts.Add($"Child concept '{childId}' referenced by '{concept.Id}'");
                    }
                }
                
                // Check axes
                var axes = GetStringArray(concept.Meta, "axes");
                foreach (var axis in axes)
                {
                    var axisId = $"u-core-axis-{axis}";
                    if (!registry.TryGet(axisId, out _))
                    {
                        missingConcepts.Add($"Axis '{axisId}' referenced by '{concept.Id}'");
                    }
                }
            }
            
            if (missingConcepts.Any())
            {
                logger.Warn($"Found {missingConcepts.Count} missing referenced concepts:");
                Console.WriteLine($"Found {missingConcepts.Count} missing referenced concepts:");
                foreach (var missing in missingConcepts)
                {
                    logger.Warn($"  - {missing}");
                    Console.WriteLine($"  - {missing}");
                }
            }
            else
            {
                logger.Info("All referenced concepts exist");
                Console.WriteLine("All referenced concepts exist");
            }
        }

        private static async Task LoadAxesFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            Console.WriteLine("LoadAxesFromConfig called");
            var axisType = "codex.ontology.axis";
            var hasAxes = registry.GetNodesByType(axisType).Any();
            Console.WriteLine($"Axes already exist: {hasAxes}");
            if (hasAxes)
            {
                logger.Info("U-CORE axes already exist, skipping config loading");
                Console.WriteLine("U-CORE axes already exist, skipping config loading");
                return;
            }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-axes.json");
            Console.WriteLine($"Looking for config file at: {configPath}");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core axes config file not found: {configPath}");
                Console.WriteLine($"Config file not found: {configPath}");
                return;
            }

            Console.WriteLine("Config file found, reading...");
            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                Console.WriteLine($"Config file read, size: {json.Length} characters");
                var config = JsonSerializer.Deserialize<AxesConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"Config deserialized, axes count: {config?.Axes?.Count ?? 0}");
                
                if (config?.Axes != null)
                {
                    Console.WriteLine($"Processing {config.Axes.Count} axes...");
                    foreach (var axis in config.Axes)
                    {
                        Console.WriteLine($"Processing axis: {axis.Id}");
                        var axisNodeId = $"u-core-axis-{axis.Id}";
                        var axisNode = new Node(
                            Id: axisNodeId,
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
                        registry.Upsert(axisNode);

                        // Edges: axis_has_dimension (axis -> u-core-concept-{dim})
                        foreach (var dim in axis.Dimensions ?? Array.Empty<string>())
                        {
                            var dimConceptId = $"u-core-concept-{dim}";
                            var edge = NodeHelpers.CreateEdge(
                                axisNodeId,
                                dimConceptId,
                                role: "axis_has_dimension",
                                weight: 1.0,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-has-dimension", ["axisId"] = axis.Id, ["dimensionId"] = dim },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_has_dimension")
                            );
                            registry.Upsert(edge);
                        }

                        // Edges: axis_has_keyword (axis -> u-core-concept-kw-{kw})
                        foreach (var kw in axis.Keywords ?? Array.Empty<string>())
                        {
                            var kwConceptId = $"u-core-concept-kw-{kw}";
                            var edge = NodeHelpers.CreateEdge(
                                axisNodeId,
                                kwConceptId,
                                role: "axis_has_keyword",
                                weight: 0.6,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-has-keyword", ["axisId"] = axis.Id, ["keyword"] = kw },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_has_keyword")
                            );
                            registry.Upsert(edge);
                        }

                        // Edges: parent/child axes
                        foreach (var parent in axis.ParentAxes ?? Array.Empty<string>())
                        {
                            var parentAxisId = $"u-core-axis-{parent}";
                            var pEdge = NodeHelpers.CreateEdge(
                                parentAxisId,
                                axisNodeId,
                                role: "axis_parent_of",
                                weight: 1.0,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-parent-of", ["parent"] = parent, ["child"] = axis.Id },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_parent_of")
                            );
                            registry.Upsert(pEdge);

                            var cEdge = NodeHelpers.CreateEdge(
                                axisNodeId,
                                parentAxisId,
                                role: "axis_child_of",
                                weight: 1.0,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-child-of", ["child"] = axis.Id, ["parent"] = parent },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_child_of")
                            );
                            registry.Upsert(cEdge);
                        }

                        foreach (var child in axis.ChildAxes ?? Array.Empty<string>())
                        {
                            var childAxisId = $"u-core-axis-{child}";
                            var pEdge = NodeHelpers.CreateEdge(
                                axisNodeId,
                                childAxisId,
                                role: "axis_parent_of",
                                weight: 1.0,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-parent-of", ["parent"] = axis.Id, ["child"] = child },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_parent_of")
                            );
                            registry.Upsert(pEdge);

                            var cEdge = NodeHelpers.CreateEdge(
                                childAxisId,
                                axisNodeId,
                                role: "axis_child_of",
                                weight: 1.0,
                                meta: new Dictionary<string, object> { ["relationship"] = "axis-child-of", ["child"] = child, ["parent"] = axis.Id },
                                roleId: NodeHelpers.TryResolveRoleId(registry, "axis_child_of")
                            );
                            registry.Upsert(cEdge);
                        }
                    }
                    logger.Info($"Loaded {config.Axes.Count} axes from config (edges upserted)");
                    Console.WriteLine($"Loaded {config.Axes.Count} axes from config (edges upserted)");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading axes config: {ex.Message}", ex);
                Console.WriteLine($"Error loading axes config: {ex.Message}");
            }
            Console.WriteLine("LoadAxesFromConfig completed");
        }

        private static async Task LoadConceptsFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            Console.WriteLine("LoadConceptsFromConfig called");
            var conceptType = "codex.ucore.base";
            // Merge strategy: do not skip if some concepts exist; load/Upsert to ensure new/missing seeds are added

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-concepts.json");
            Console.WriteLine($"Looking for concepts config file at: {configPath}");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core concepts config file not found: {configPath}");
                Console.WriteLine($"Concepts config file not found: {configPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                logger.Info($"Loaded concepts config file, size: {json.Length} characters");
                Console.WriteLine($"Loaded concepts config file, size: {json.Length} characters");
                
                var config = JsonSerializer.Deserialize<ConceptsConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"Config deserialized, concepts count: {config?.Concepts?.Count ?? 0}");
                
                if (config == null)
                {
                    logger.Error("Failed to deserialize concepts config - config is null");
                    Console.WriteLine("Failed to deserialize concepts config - config is null");
                    return;
                }
                
                if (config.Concepts == null)
                {
                    logger.Error("Failed to deserialize concepts config - Concepts list is null");
                    Console.WriteLine("Failed to deserialize concepts config - Concepts list is null");
                    return;
                }
                
                logger.Info($"Deserialized concepts config with {config.Concepts.Count} concepts");
                Console.WriteLine($"Deserialized concepts config with {config.Concepts.Count} concepts");
                
                int upserts = 0;
                foreach (var concept in config.Concepts)
                {
                    // Special case for meta-identity - use u-core-meta-identity ID format
                    var nodeId = concept.Id == "meta-identity" ? "u-core-meta-identity" : $"u-core-concept-{concept.Id}";
                    
                    var node = new Node(
                        Id: nodeId,
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
                    upserts++;
                }
                logger.Info($"Merged {upserts} core concepts from config");
                Console.WriteLine($"Merged {upserts} core concepts from config");
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading concepts config: {ex.Message}", ex);
                Console.WriteLine($"Error loading concepts config: {ex.Message}");
            }
            Console.WriteLine("LoadConceptsFromConfig completed");
        }

        private static async Task LoadRelationshipsFromConfig(INodeRegistry registry, ICodexLogger logger)
        {
            Console.WriteLine("LoadRelationshipsFromConfig called");
            // Use the core relationship type to align with tests and ontology spec
            var relationshipType = "codex.relationship.core";
            var hasRelationships = registry.GetNodesByType(relationshipType).Any();
            Console.WriteLine($"Relationships already exist: {hasRelationships}");
            if (hasRelationships)
            {
                logger.Info("Core relationships already exist, skipping config loading");
                Console.WriteLine("Core relationships already exist, skipping config loading");
                return;
            }

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ontology", "core-relationships.json");
            Console.WriteLine($"Looking for relationships config file at: {configPath}");
            if (!File.Exists(configPath))
            {
                logger.Warn($"Core relationships config file not found: {configPath}");
                Console.WriteLine($"Relationships config file not found: {configPath}");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(configPath);
                Console.WriteLine($"Loaded relationships config file, size: {json.Length} characters");
                var config = JsonSerializer.Deserialize<RelationshipsConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"Config deserialized, relationships count: {config?.Relationships?.Count ?? 0}");
                
                if (config?.Relationships != null)
                {
                    int upserts = 0;
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
                        upserts++;
                    }
                    logger.Info($"Loaded {upserts} core relationships from config");
                    Console.WriteLine($"Loaded {upserts} core relationships from config");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading relationships config: {ex.Message}", ex);
                Console.WriteLine($"Error loading relationships config: {ex.Message}");
            }
            Console.WriteLine("LoadRelationshipsFromConfig completed");
        }

        private static void EnsureAxisDerivedEdgesAndConcepts(INodeRegistry registry, ICodexLogger logger)
        {
            try
            {
                var axisNodes = registry.GetNodesByType("codex.ontology.axis").ToList();
                if (axisNodes.Count == 0)
                {
                    logger.Info("No axes found to derive edges from");
                    return;
                }

                // Index axis nodes by short id (strip the 'u-core-axis-' prefix)
                var axisByShortId = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
                foreach (var axis in axisNodes)
                {
                    var shortId = axis.Id.StartsWith("u-core-axis-") ? axis.Id.Substring("u-core-axis-".Length) : axis.Id;
                    axisByShortId[shortId] = axis;
                }

                // Helper local functions
                string ToTitle(string id)
                {
                    if (string.IsNullOrWhiteSpace(id)) return id;
                    var parts = id.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return string.Join(" ", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
                }

                // Runtime generation of axis-derived concepts is disabled.
                // Concepts are pre-seeded from configuration in LoadAxesFromConfig.
                Node EnsureConceptNode(string conceptId, string typeId, string title, string description, IEnumerable<string>? keywords = null, IEnumerable<string>? axes = null, int level = 3)
                {
                    var fullId = $"u-core-concept-{conceptId}";
                    if (registry.TryGet(fullId, out var existing))
                    {
                        return existing;
                    }
                    logger.Warn($"Missing pre-seeded concept '{fullId}'. Skipping runtime generation per policy.");
                    // Return a lightweight placeholder reference to avoid null handling downstream
                    return new Node(
                        Id: fullId,
                        TypeId: typeId,
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: title,
                        Description: description,
                        Content: new ContentRef("application/json", JsonSerializer.Serialize(new { id = conceptId, name = title, description, typeId, level, keywords = keywords ?? Array.Empty<string>(), axes = axes ?? Array.Empty<string>() }), null, null),
                        Meta: new Dictionary<string, object> { ["name"] = title, ["level"] = level }
                    );
                }

                void UpsertEdge(string fromId, string toId, string role, double weight, Dictionary<string, object>? meta = null)
                {
                    var mergedMeta = meta != null ? new Dictionary<string, object>(meta) : new Dictionary<string, object>();
                    if (!mergedMeta.ContainsKey("origin")) mergedMeta["origin"] = "u-core-spec";
                    if (!mergedMeta.ContainsKey("persistent")) mergedMeta["persistent"] = true;
                    var edge = new Edge(
                        FromId: fromId,
                        ToId: toId,
                        Role: role,
                        Weight: weight,
                        Meta: mergedMeta
                    );
                    registry.Upsert(edge);
                }

                foreach (var axis in axisNodes)
                {
                    var axisId = axis.Id; // full node id
                    var axisShortId = axisByShortId.First(kv => kv.Value.Id == axisId).Key;

                    // Dimensions → ensure concept nodes and edges
                    var dimensions = GetStringArray(axis.Meta, "dimensions");
                    foreach (var dim in dimensions)
                    {
                        var dimNode = EnsureConceptNode(
                            conceptId: dim,
                            typeId: "codex.concept.dimension",
                            title: ToTitle(dim),
                            description: $"Dimension '{dim}' of axis '{axisShortId}'",
                            keywords: new[] { dim },
                            axes: new[] { axisShortId },
                            level: 3
                        );
                        UpsertEdge(axisId, dimNode.Id, "axis_has_dimension", 1.0, new Dictionary<string, object> { ["axisId"] = axisShortId, ["dimensionId"] = dim });
                    }

                    // Keywords → ensure concept nodes and edges
                    var keywords = GetStringArray(axis.Meta, "keywords");
                    foreach (var kw in keywords)
                    {
                        var kwNode = EnsureConceptNode(
                            conceptId: $"kw-{kw}",
                            typeId: "codex.concept.keyword",
                            title: ToTitle(kw),
                            description: $"Keyword '{kw}' related to axis '{axisShortId}'",
                            keywords: new[] { kw },
                            axes: new[] { axisShortId },
                            level: 3
                        );
                        UpsertEdge(axisId, kwNode.Id, "axis_has_keyword", 0.6, new Dictionary<string, object> { ["axisId"] = axisShortId, ["keyword"] = kw });
                    }

                    // Parent/Child axis edges
                    var parentAxes = GetStringArray(axis.Meta, "parentAxes");
                    foreach (var parent in parentAxes)
                    {
                        if (axisByShortId.TryGetValue(parent, out var parentAxisNode))
                        {
                            UpsertEdge(parentAxisNode.Id, axisId, "axis_parent_of", 1.0, new Dictionary<string, object> { ["parent"] = parent, ["child"] = axisShortId });
                            UpsertEdge(axisId, parentAxisNode.Id, "axis_child_of", 1.0, new Dictionary<string, object> { ["child"] = axisShortId, ["parent"] = parent });
                        }
                        else
                        {
                            // Missing parent axis → ensure a placeholder axis node
                            var ensuredParent = EnsureAxisNode(registry, parent, logger);
                            UpsertEdge(ensuredParent.Id, axisId, "axis_parent_of", 1.0, new Dictionary<string, object> { ["parent"] = parent, ["child"] = axisShortId });
                            UpsertEdge(axisId, ensuredParent.Id, "axis_child_of", 1.0, new Dictionary<string, object> { ["child"] = axisShortId, ["parent"] = parent });
                            axisByShortId[parent] = ensuredParent; // cache
                        }
                    }

                    var childAxes = GetStringArray(axis.Meta, "childAxes");
                    foreach (var child in childAxes)
                    {
                        if (axisByShortId.TryGetValue(child, out var childAxisNode))
                        {
                            UpsertEdge(axisId, childAxisNode.Id, "axis_parent_of", 1.0, new Dictionary<string, object> { ["parent"] = axisShortId, ["child"] = child });
                            UpsertEdge(childAxisNode.Id, axisId, "axis_child_of", 1.0, new Dictionary<string, object> { ["child"] = child, ["parent"] = axisShortId });
                        }
                        else
                        {
                            // Missing child axis → ensure a placeholder axis node
                            var ensuredChild = EnsureAxisNode(registry, child, logger);
                            UpsertEdge(axisId, ensuredChild.Id, "axis_parent_of", 1.0, new Dictionary<string, object> { ["parent"] = axisShortId, ["child"] = child });
                            UpsertEdge(ensuredChild.Id, axisId, "axis_child_of", 1.0, new Dictionary<string, object> { ["child"] = child, ["parent"] = axisShortId });
                            axisByShortId[child] = ensuredChild; // cache
                        }
                    }

                    // Axis type → ensure a concept node for the type and edge
                    var typeId = axis.Meta != null && axis.Meta.TryGetValue("typeId", out var t)
                        ? t?.ToString()
                        : null;
                    if (string.IsNullOrWhiteSpace(typeId))
                    {
                        // Fallback: try to parse from Content payload
                        try
                        {
                            if (axis.Content?.InlineJson != null)
                            {
                                using var doc = JsonDocument.Parse(axis.Content.InlineJson);
                                if (doc.RootElement.TryGetProperty("typeId", out var typeProp))
                                {
                                    typeId = typeProp.GetString();
                                }
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(typeId))
                    {
                        var typeConceptId = $"type-{typeId}";
                        var typeTitle = ToTitle(typeId.Split('.').Last());
                        var typeNode = EnsureConceptNode(
                            conceptId: typeConceptId,
                            typeId: "codex.concept.type",
                            title: typeTitle,
                            description: $"Axis type '{typeId}'",
                            keywords: new[] { typeId },
                            axes: new[] { axisShortId },
                            level: 2
                        );
                        UpsertEdge(axisId, typeNode.Id, "axis_has_type", 0.9, new Dictionary<string, object> { ["axisId"] = axisShortId, ["typeId"] = typeId });
                    }
                }

                logger.Info("Axis-derived nodes and edges ensured");
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to ensure axis-derived nodes/edges: {ex.Message}");
            }
        }

        private static void EnsureConceptDerivedEdges(INodeRegistry registry, ICodexLogger logger)
        {
            try
            {
                // Collect all concept nodes - look for all concept types
                var conceptNodes = new List<Node>();
                conceptNodes.AddRange(registry.GetNodesByTypePrefix("codex.ucore"));
                conceptNodes.AddRange(registry.GetNodesByTypePrefix("codex.concept"));
                conceptNodes.AddRange(registry.GetNodesByTypePrefix("codex.meta"));
                conceptNodes = conceptNodes.Distinct().ToList();
                
                logger.Info($"EnsureConceptDerivedEdges: Found {conceptNodes.Count} concept nodes with type prefixes 'codex.ucore', 'codex.concept', and 'codex.meta'");
                Console.WriteLine($"EnsureConceptDerivedEdges: Found {conceptNodes.Count} concept nodes with type prefixes 'codex.ucore', 'codex.concept', and 'codex.meta'");
                
                // Debug: List all concept type IDs
                var typeIds = conceptNodes.Select(n => n.TypeId).Distinct().ToList();
                Console.WriteLine($"Concept type IDs found: {string.Join(", ", typeIds)}");
                
                // Debug: Check if kw-matter is in the list
                var kwMatterNode = conceptNodes.FirstOrDefault(n => n.Id == "u-core-concept-kw-matter");
                if (kwMatterNode != null)
                {
                    Console.WriteLine($"Found kw-matter node with type: {kwMatterNode.TypeId}");
                }
                else
                {
                    Console.WriteLine("kw-matter node NOT found in concept nodes");
                }
                if (conceptNodes.Count == 0)
                {
                    logger.Info("No concepts found to derive edges from");
                    Console.WriteLine("No concepts found to derive edges from");
                    return;
                }

                // Build lookup by short id (strip 'u-core-concept-' or 'u-core-meta-')
                var conceptByShortId = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in conceptNodes)
                {
                    string shortId;
                    if (c.Id.StartsWith("u-core-concept-"))
                    {
                        shortId = c.Id.Substring("u-core-concept-".Length);
                    }
                    else if (c.Id.StartsWith("u-core-meta-"))
                    {
                        shortId = c.Id.Substring("u-core-meta-".Length);
                    }
                    else
                    {
                        shortId = c.Id;
                    }
                    conceptByShortId[shortId] = c;
                }

                // Helper to upsert edge
                void UpsertEdge(string fromId, string toId, string role, double weight, Dictionary<string, object>? meta = null)
                {
                    var mergedMeta = meta != null ? new Dictionary<string, object>(meta) : new Dictionary<string, object>();
                    if (!mergedMeta.ContainsKey("origin")) mergedMeta["origin"] = "u-core-spec";
                    if (!mergedMeta.ContainsKey("persistent")) mergedMeta["persistent"] = true;
                    var edge = new Edge(
                        FromId: fromId,
                        ToId: toId,
                        Role: role,
                        Weight: weight,
                        Meta: mergedMeta
                    );
                    registry.Upsert(edge);
                    Console.WriteLine($"    Upserted edge: {fromId} -> {toId} ({role})");
                    
                    // Special debug for kw-matter node
                    if (fromId == "u-core-concept-kw-matter" || toId == "u-core-concept-kw-matter")
                    {
                        Console.WriteLine($"    *** KW-MATTER EDGE: {fromId} -> {toId} ({role}) ***");
                    }
                }

                // Helper to detect missing concepts without generating new nodes at runtime
                Node EnsureConceptPlaceholder(string shortId)
                {
                    var fullId = $"u-core-concept-{shortId}";
                    if (registry.TryGet(fullId, out var existing))
                    {
                        return existing;
                    }
                    // Do not upsert placeholders. Return a non-persistent stub and log.
                    logger.Warn($"Missing concept '{fullId}' referenced by seed edges. Please add to core-concepts.json.");
                    var title = shortId.Replace('_', ' ').Replace('-', ' ');
                    title = string.Join(" ", title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
                    return new Node(
                        Id: fullId,
                        TypeId: "codex.concept",
                        State: ContentState.Ice,
                        Locale: "en-US",
                        Title: title,
                        Description: $"Stub for missing concept '{shortId}'",
                        Content: new ContentRef(
                            MediaType: "application/json",
                            InlineJson: JsonSerializer.Serialize(new { id = shortId, name = title, typeId = "codex.concept" }),
                            InlineBytes: null,
                            ExternalUri: null
                        ),
                        Meta: new Dictionary<string, object> { ["name"] = title, ["level"] = 3 }
                    );
                }

                foreach (var concept in conceptNodes)
                {
                    var conceptId = concept.Id; // full id
                    var shortId = conceptByShortId.First(kv => kv.Value.Id == conceptId).Key;
                    logger.Info($"Processing concept {conceptId} (short: {shortId})");
                    
                    // Special debug for kw-matter
                    if (conceptId == "u-core-concept-kw-matter")
                    {
                        Console.WriteLine($"    *** PROCESSING KW-MATTER CONCEPT ***");
                        Console.WriteLine($"    Keywords: {string.Join(", ", GetStringArray(concept.Meta, "keywords"))}");
                        Console.WriteLine($"    Axes: {string.Join(", ", GetStringArray(concept.Meta, "axes"))}");
                        Console.WriteLine($"    Parent concepts: {string.Join(", ", GetStringArray(concept.Meta, "parentConcepts"))}");
                        Console.WriteLine($"    Child concepts: {string.Join(", ", GetStringArray(concept.Meta, "childConcepts"))}");
                    }

                    // Parent concepts → is_a edge from this to parent; and optional reverse has_child
                    var parents = GetStringArray(concept.Meta, "parentConcepts");
                    logger.Info($"  Parent concepts: {string.Join(", ", parents)}");
                    foreach (var parentShort in parents)
                    {
                        var parentNode = conceptByShortId.TryGetValue(parentShort, out var p) ? p : EnsureConceptPlaceholder(parentShort);
                        conceptByShortId[parentShort] = parentNode; // cache
                        logger.Info($"    Creating edge: {conceptId} -> {parentNode.Id} (is_a)");
                        UpsertEdge(conceptId, parentNode.Id, "is_a", 1.0, new Dictionary<string, object> { ["child"] = shortId, ["parent"] = parentShort });
                        logger.Info($"    Creating edge: {parentNode.Id} -> {conceptId} (has_child)");
                        UpsertEdge(parentNode.Id, conceptId, "has_child", 0.95, new Dictionary<string, object> { ["parent"] = parentShort, ["child"] = shortId });
                    }

                    // Child concepts → has_child and reverse is_a
                    var children = GetStringArray(concept.Meta, "childConcepts");
                    foreach (var childShort in children)
                    {
                        var childNode = conceptByShortId.TryGetValue(childShort, out var cnode) ? cnode : EnsureConceptPlaceholder(childShort);
                        conceptByShortId[childShort] = childNode; // cache
                        UpsertEdge(conceptId, childNode.Id, "has_child", 0.95, new Dictionary<string, object> { ["parent"] = shortId, ["child"] = childShort });
                        UpsertEdge(childNode.Id, conceptId, "is_a", 1.0, new Dictionary<string, object> { ["child"] = childShort, ["parent"] = shortId });
                    }

                    // Axes membership → concept_on_axis (link to axis nodes)
                    var axes = GetStringArray(concept.Meta, "axes");
                    logger.Info($"  Axes: {string.Join(", ", axes)}");
                    foreach (var axisShort in axes)
                    {
                        // Ensure axis node exists
                        var axisNode = registry.TryGet($"u-core-axis-{axisShort}", out var existingAxis) ? existingAxis : EnsureAxisNode(registry, axisShort, logger);
                        logger.Info($"    Creating edge: {conceptId} -> {axisNode.Id} (concept_on_axis)");
                        UpsertEdge(conceptId, axisNode.Id, "concept_on_axis", 0.9, new Dictionary<string, object> { ["concept"] = shortId, ["axis"] = axisShort });
                    }
                }

                logger.Info("Concept-derived edges ensured");
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to ensure concept-derived edges: {ex.Message}");
            }
        }

        // Ensure a baseline edge from the U-CORE ontology root to each u-core-concept-* node
        private static void EnsureBaselineConceptEdges(INodeRegistry registry, ICodexLogger logger)
        {
            try
            {
                var rootId = "u-core-ontology-root";
                var concepts = registry.AllNodes()
                    .Where(n => n.Id.StartsWith("u-core-concept-", StringComparison.OrdinalIgnoreCase))
                    .Select(n => n.Id)
                    .ToList();

                foreach (var conceptId in concepts)
                {
                    var edge = new Edge(
                        FromId: rootId,
                        ToId: conceptId,
                        Role: "contains",
                        Weight: 1.0,
                        Meta: new Dictionary<string, object>
                        {
                            ["relationship"] = "ontology-contains-concept",
                            ["origin"] = "u-core-spec",
                            ["persistent"] = true
                        }
                    );
                    registry.Upsert(edge);
                }

                if (concepts.Count > 0)
                {
                    logger.Info($"Baseline ontology concept edges ensured for {concepts.Count} concepts");
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to ensure baseline concept edges: {ex.Message}");
            }
        }

        private static Node EnsureAxisNode(INodeRegistry registry, string shortId, ICodexLogger logger)
        {
            var fullId = $"u-core-axis-{shortId}";
            if (registry.TryGet(fullId, out var existing))
            {
                return existing;
            }

            var node = new Node(
                Id: fullId,
                TypeId: "codex.ontology.axis",
                State: ContentState.Ice,
                Locale: "en-US",
                Title: ToAxisTitle(shortId),
                Description: $"Placeholder axis for '{shortId}' (auto-created to satisfy references)",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        id = shortId,
                        name = ToAxisTitle(shortId),
                        description = "Auto-created axis placeholder",
                        typeId = "codex.ontology.axis",
                        level = 0,
                        keywords = Array.Empty<string>(),
                        dimensions = Array.Empty<string>(),
                        parentAxes = Array.Empty<string>(),
                        childAxes = Array.Empty<string>()
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = ToAxisTitle(shortId),
                    ["keywords"] = Array.Empty<string>(),
                    ["level"] = 0,
                    ["dimensions"] = Array.Empty<string>(),
                    ["parentAxes"] = Array.Empty<string>(),
                    ["childAxes"] = Array.Empty<string>(),
                    ["typeId"] = "codex.ontology.axis"
                }
            );
            registry.Upsert(node);
            logger.Info($"Created placeholder axis node: {fullId}");
            return node;
        }

        private static string ToAxisTitle(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;
            var parts = id.Replace('_', ' ').Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
        }

        private static string[] GetStringArray(Dictionary<string, object>? meta, string key)
        {
            if (meta == null || !meta.TryGetValue(key, out var val) || val == null)
            {
                return Array.Empty<string>();
            }

            if (val is string[] sa) return sa;
            if (val is IEnumerable<string> es) return es.ToArray();
            try
            {
                // Attempt to coerce from JsonElement[] or object[]
                if (val is object[] oa)
                {
                    return oa.Select(o => o?.ToString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                }

                if (val is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var e in je.EnumerateArray())
                        {
                            if (e.ValueKind == JsonValueKind.String)
                            {
                                list.Add(e.GetString()!);
                            }
                        }
                        return list.ToArray();
                    }
                }
            }
            catch
            {
                // ignore coercion errors
            }
            return Array.Empty<string>();
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


