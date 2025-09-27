using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Module responsible for ensuring all related nodes have proper edges in the system
/// </summary>
[MetaNode(Id = "codex.edge-ensurance", Name = "Edge Ensurance Module", Description = "Ensures all related nodes have proper edges")]
public sealed class EdgeEnsuranceModule : ModuleBase
{
    public override string Name => "Edge Ensurance Module";
    public override string Description => "Ensures all related nodes have proper edges in the system";
    public override string Version => "1.0.0";

    public EdgeEnsuranceModule(INodeRegistry registry, ICodexLogger logger) : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.edge-ensurance",
            name: "Edge Ensurance Module",
            version: "1.0.0",
            description: "Ensures all related nodes have proper edges in the system",
            tags: new[] { "edges", "relationships", "graph", "connectivity" },
            capabilities: new[] { "edge-creation", "relationship-mapping", "graph-connectivity" },
            spec: "codex.spec.edge-ensurance"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        router.Register("codex.edge-ensurance", "ensure-all-edges", async args =>
        {
            return await EnsureAllEdgesAsync();
        });

        router.Register("codex.edge-ensurance", "ensure-module-edges", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing module ID");
            var moduleId = args.Value.GetString();
            if (string.IsNullOrEmpty(moduleId)) return new ErrorResponse("Invalid module ID");
            return await EnsureModuleEdgesAsync(moduleId);
        });

        router.Register("codex.edge-ensurance", "ensure-meta-edges", async args =>
        {
            return await EnsureMetaNodeEdgesAsync();
        });

        router.Register("codex.edge-ensurance", "ensure-content-edges", async args =>
        {
            return await EnsureContentEdgesAsync();
        });

        router.Register("codex.edge-ensurance", "ensure-concept-edges", async args =>
        {
            return await EnsureConceptEdgesAsync();
        });

        router.Register("codex.edge-ensurance", "ensure-ucore-connections", async args =>
        {
            return await EnsureUCoreConnectionsAsync();
        });

        router.Register("codex.edge-ensurance", "validate-edge-roles", async args =>
        {
            return await ValidateEdgeRolesAsync();
        });
    }

    [ApiRoute("POST", "/edges/ensure-all", "Ensure All Edges", "Ensures all related nodes have proper edges", "codex.edge-ensurance")]
    public async Task<object> EnsureAllEdgesAsync()
    {
        try
        {
            var results = new List<string>();
            
            // No generic backfills here; edges must be created at source time
            
            // Ensure meta-node edges
            var metaResult = await EnsureMetaNodeEdgesAsync();
            if (metaResult is SuccessResponse metaSuccess && metaSuccess.Data is IEnumerable<string> metaData)
                results.AddRange(metaData);
            
            // Ensure module edges
            var modules = _registry.GetNodesByType("codex.meta/module");
            foreach (var module in modules)
            {
                var moduleResult = await EnsureModuleEdgesAsync(module.Id);
                if (moduleResult is SuccessResponse moduleSuccess && moduleSuccess.Data is IEnumerable<string> moduleData)
                    results.AddRange(moduleData);
            }
            
            // Ensure content edges
            var contentResult = await EnsureContentEdgesAsync();
            if (contentResult is SuccessResponse contentSuccess && contentSuccess.Data is IEnumerable<string> contentData)
                results.AddRange(contentData);
            
            // Ensure shared metadata edges (normalize sharable meta into meta-nodes)
            var sharedMetaResult = await EnsureSharedMetadataEdgesAsync();
            if (sharedMetaResult is SuccessResponse sharedMetaSuccess && sharedMetaSuccess.Data is IEnumerable<string> sharedMetaData)
                results.AddRange(sharedMetaData);

            // Ensure concept edges
            var conceptResult = await EnsureConceptEdgesAsync();
            if (conceptResult is SuccessResponse conceptSuccess && conceptSuccess.Data is IEnumerable<string> conceptData)
                results.AddRange(conceptData);
            
            // Ensure news edges
            var newsResult = await EnsureNewsEdgesAsync();
            if (newsResult is SuccessResponse newsSuccess && newsSuccess.Data is IEnumerable<string> newsData)
                results.AddRange(newsData);
            
            // Ensure user edges
            var userResult = await EnsureUserEdgesAsync();
            if (userResult is SuccessResponse userSuccess && userSuccess.Data is IEnumerable<string> userData)
                results.AddRange(userData);
            
            // Ensure global U-CORE connections
            var ucoreResult = await EnsureUCoreConnectionsAsync();
            if (ucoreResult is SuccessResponse ucoreSuccess && ucoreSuccess.Data is IEnumerable<string> ucoreData)
                results.AddRange(ucoreData);

            // Validate roleId references
            var validateResult = await ValidateEdgeRolesAsync();
            if (validateResult is SuccessResponse validateSuccess && validateSuccess.Data is IEnumerable<string> validateData)
                results.AddRange(validateData);
            
            return new SuccessResponse($"Ensured edges for all node types. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring all edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring all edges: {ex.Message}");
        }
    }

    // Note: No backfill endpoints; enforcement occurs at node creation in NodeRegistry.Upsert

    [ApiRoute("POST", "/edges/validate-role-ids", "Validate Edge RoleIds", "Ensures Edge.roleId references codex.relationship.core", "codex.edge-ensurance")]
    public async Task<object> ValidateEdgeRolesAsync()
    {
        try
        {
            var issues = new List<string>();
            var relationshipType = "codex.relationship.core";
            var relationshipNodes = _registry.GetNodesByType(relationshipType).ToDictionary(n => n.Id, n => n, StringComparer.OrdinalIgnoreCase);

            foreach (var edge in _registry.GetEdges())
            {
                var roleId = edge.RoleId;
                if (string.IsNullOrWhiteSpace(roleId))
                {
                    // Best-effort: if meta carries roleId, hydrate
                    if (edge.Meta != null && edge.Meta.TryGetValue("roleId", out var ridObj))
                    {
                        var rid = ridObj?.ToString();
                        if (!string.IsNullOrWhiteSpace(rid))
                        {
                            var updated = edge with { RoleId = rid };
                            _registry.Upsert(updated);
                            continue;
                        }
                    }
                    issues.Add($"Missing roleId for edge {edge.FromId} -> {edge.ToId} ({edge.Role})");
                    continue;
                }

                if (!relationshipNodes.ContainsKey(roleId))
                {
                    issues.Add($"Invalid roleId '{roleId}' for edge {edge.FromId} -> {edge.ToId}");
                }
            }

            return new SuccessResponse(issues.Count == 0 ? "All edge roleIds are valid" : $"Edge roleId validation found {issues.Count} issue(s)", issues);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error validating edge roleIds: {ex.Message}", ex);
            return new ErrorResponse($"Error validating edge roleIds: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-shared-metadata", "Ensure Shared Metadata Edges", "Normalizes sharable metadata into meta-nodes and links", "codex.edge-ensurance")]
    public async Task<object> EnsureSharedMetadataEdgesAsync()
    {
        try
        {
            var results = new List<string>();
            var shared = new SharedMetadataModule(_registry, _logger);

            // Keys to normalize: category->key resolvers
            var knownKeys = new (string Category, string Key, string MetaKey)[]
            {
                ("news", "source", "source"),
                ("content", "media-type", "mediaType"),
                ("ai", "model", "aiModel"),
                ("i18n", "language", "language"),
                ("license", "id", "license"),
            };

            foreach (var node in _registry.AllNodes())
            {
                if (node.Meta == null || node.Meta.Count == 0) continue;

                foreach (var (category, key, metaKey) in knownKeys)
                {
                    if (!node.Meta.ContainsKey(metaKey)) continue;
                    var raw = node.Meta[metaKey]?.ToString();
                    if (string.IsNullOrWhiteSpace(raw)) continue;

                    // Create/get shared metadata node for this value
                    var createResp = await shared.CreateSharedMetadataAsync(new CreateSharedMetadataRequest(
                        Category: category,
                        Key: raw!,
                        Description: $"Shared metadata for {metaKey}",
                        DataType: "string",
                        Data: new { value = raw }
                    ));

                    string metadataId;
                    if (createResp is SuccessResponse ok && ok.Data is not null)
                    {
                        var json = JsonSerializer.Serialize(ok.Data);
                        var doc = JsonDocument.Parse(json);
                        metadataId = doc.RootElement.GetProperty("metadataId").GetString() ?? "";
                    }
                    else
                    {
                        // If already exists or error, attempt to compute deterministic id pattern used by CreateSharedMetadataAsync
                        // Note: CreateSharedMetadataAsync uses GUID suffix; since we cannot know it deterministically here,
                        // fallback: create a lightweight metadata node id by hashing
                        var hash = Math.Abs((category + ":" + key + ":" + raw).GetHashCode()).ToString("X8");
                        metadataId = $"codex.shared-metadata.{category}.{raw}.{hash}";
                        if (_registry.GetNode(metadataId) == null)
                        {
                            var metaNode = new Node(
                                Id: metadataId,
                                TypeId: "codex.meta/shared-metadata",
                                State: ContentState.Ice,
                                Locale: "en",
                                Title: $"Shared Metadata: {category}.{raw}",
                                Description: $"Normalized {metaKey}={raw}",
                                Content: new ContentRef("application/json", JsonSerializer.Serialize(new { value = raw }), null, null),
                                Meta: new Dictionary<string, object> { ["category"] = category, ["key"] = raw! }
                            );
                            _registry.Upsert(metaNode);
                        }
                    }

                    if (!string.IsNullOrEmpty(metadataId))
                    {
                        var edge = NodeHelpers.CreateEdge(
                            node.Id,
                            metadataId,
                            "references",
                            1.0,
                            new Dictionary<string, object> { ["relationship"] = "node-references-shared-metadata", ["metaKey"] = metaKey }
                        );
                        _registry.Upsert(edge);
                        results.Add($"Linked {node.Id} -> {metadataId} ({metaKey})");
                    }
                }
            }

            return new SuccessResponse($"Ensured shared metadata edges. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring shared metadata edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring shared metadata edges: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-ucore", "Ensure U-CORE Connections", "Connect all nodes and meta-nodes to U-CORE", "codex.edge-ensurance")]
    public async Task<object> EnsureUCoreConnectionsAsync()
    {
        try
        {
            var results = new List<string>();

            // Discover U-CORE axes and concept defaults
            var axes = GetUCoreAxes();
            var defaultAxisId = "codex.ucore.axis.abundance.00000000-0000-0000-0000-000000000001";
            var defaultConceptId = "codex.ucore.concept.knowledge.00000000-0000-0000-0000-000000000001";

            // Ensure default targets exist (best-effort)
            EnsurePlaceholderIfMissing(defaultAxisId, "codex.ontology.axis", "Abundance", "U-CORE ontology axis: Abundance");
            EnsurePlaceholderIfMissing(defaultConceptId, "u-core.concept", "Knowledge", "U-CORE concept: Knowledge");

            // Connect all regular nodes
            foreach (var node in _registry.AllNodes())
            {
                if (node.Id.StartsWith("u-core-")) continue; // skip U-CORE nodes themselves
                // Deprecated: do not auto-map generic nodes to axes. Respect explicit ontology only.
            }

            // Connect all meta-nodes
            var metaNodes = _registry.GetNodesByType("codex.meta/type")
                .Concat(_registry.GetNodesByType("codex.meta/api"))
                .Concat(_registry.GetNodesByType("codex.meta/module"))
                .Concat(_registry.GetNodesByType("codex.meta/response"))
                .Concat(_registry.GetNodesByType("codex.meta/node"))
                .ToList();

            // Deprecated: do not auto-map meta-nodes to a default concept.

            return new SuccessResponse($"Ensured U-CORE connections. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring U-CORE connections: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring U-CORE connections: {ex.Message}");
        }
    }

    private string? MatchAxis(Node node, List<(string AxisId, string Name, string[] Keywords)> axes)
    {
        var text = ($"{node.Title} {node.Description}").ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text)) return null;

        foreach (var axis in axes)
        {
            foreach (var k in axis.Keywords)
            {
                var kk = k.ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(kk) && text.Contains(kk))
                {
                    return axis.AxisId;
                }
            }
        }
        return null;
    }

    private List<(string AxisId, string Name, string[] Keywords)> GetUCoreAxes()
    {
        var results = new List<(string, string, string[])>();
        // Try known axis type ids
        foreach (var typeId in new[] { "codex.ontology.axis", "codex.meta/axis", "u-core.axis" })
        {
            foreach (var axisNode in _registry.GetNodesByType(typeId))
            {
                var axisId = axisNode.Id;
                var name = axisNode.Title ?? axisNode.Meta?.GetValueOrDefault("name")?.ToString() ?? axisId;
                string[] keywords = Array.Empty<string>();
                try
                {
                    if (axisNode.Content?.InlineJson != null)
                    {
                        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(axisNode.Content.InlineJson);
                        if (json != null && json.TryGetValue("keywords", out var kws))
                        {
                            if (kws is JsonElement je && je.ValueKind == JsonValueKind.Array)
                            {
                                keywords = je.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                            }
                        }
                    }
                }
                catch { }

                results.Add((axisId, name ?? axisId, keywords));
            }
        }
        return results.Distinct().ToList();
    }

    private void EnsurePlaceholderIfMissing(string id, string typeId, string title, string description)
    {
        if (_registry.GetNode(id) != null) return;
        var node = new Node(
            Id: id,
            TypeId: typeId,
            State: ContentState.Ice,
            Locale: "en-US",
            Title: title,
            Description: description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new { name = title, keywords = Array.Empty<string>() }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object> { ["seeded"] = true }
        );
        _registry.Upsert(node);
    }

    [ApiRoute("POST", "/edges/ensure-module/{moduleId}", "Ensure Module Edges", "Ensures edges for a specific module", "codex.edge-ensurance")]
    public async Task<object> EnsureModuleEdgesAsync([ApiParameter("moduleId", "Module identifier", Required = true, Location = "path")] string moduleId)
    {
        try
        {
            var results = new List<string>();
            var module = _registry.GetNode(moduleId);
            if (module == null)
            {
                return new ErrorResponse($"Module {moduleId} not found");
            }

            // Connect module to its meta-node
            var moduleMetaEdge = NodeHelpers.CreateEdge(
                moduleId, 
                "codex.meta/type/module", 
                "instance-of", 
                1.0,
                new Dictionary<string, object> { ["relationship"] = "module-instance-of-type" },
                NodeHelpers.TryResolveRoleId(_registry, "instance-of")
            );
            _registry.Upsert(moduleMetaEdge);
            results.Add($"Connected module {moduleId} to its meta-type");

            // Connect module to its defined types
            var moduleTypes = _registry.GetNodesByType("codex.meta/type")
                .Where(n => n.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId);
            
            foreach (var typeNode in moduleTypes)
            {
                var typeEdge = NodeHelpers.CreateEdge(
                    moduleId,
                    typeNode.Id,
                    "defines",
                    1.0,
                    new Dictionary<string, object> { ["relationship"] = "module-defines-type" },
                    NodeHelpers.TryResolveRoleId(_registry, "defines")
                );
                _registry.Upsert(typeEdge);
                results.Add($"Connected module {moduleId} to type {typeNode.Id}");
            }

            // Connect module to its APIs
            var moduleApis = _registry.GetNodesByType("codex.meta/api")
                .Where(n => n.Meta?.GetValueOrDefault("moduleId")?.ToString() == moduleId);
            
            foreach (var apiNode in moduleApis)
            {
                var apiEdge = NodeHelpers.CreateEdge(
                    moduleId,
                    apiNode.Id,
                    "exposes",
                    1.0,
                    new Dictionary<string, object> { ["relationship"] = "module-exposes-api" },
                    NodeHelpers.TryResolveRoleId(_registry, "exposes")
                );
                _registry.Upsert(apiEdge);
                results.Add($"Connected module {moduleId} to API {apiNode.Id}");
            }

            return new SuccessResponse($"Ensured edges for module {moduleId}. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring module edges for {moduleId}: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring module edges: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-meta", "Ensure Meta Edges", "Ensures edges between meta-nodes", "codex.edge-ensurance")]
    public async Task<object> EnsureMetaNodeEdgesAsync()
    {
        try
        {
            var results = new List<string>();

            // Connect all meta-nodes to the core meta-type
            var metaNodes = _registry.GetNodesByType("codex.meta/type");
            foreach (var metaNode in metaNodes)
            {
                var metaEdge = NodeHelpers.CreateEdge(
                    metaNode.Id,
                    "codex.meta/type/meta-node",
                    "instance-of",
                    1.0,
                    new Dictionary<string, object> { ["relationship"] = "meta-node-instance-of-type" },
                    NodeHelpers.TryResolveRoleId(_registry, "instance-of")
                );
                _registry.Upsert(metaEdge);
                results.Add($"Connected meta-node {metaNode.Id} to meta-type");
            }

            // Connect type nodes to their fields
            foreach (var typeNode in metaNodes)
            {
                var fields = typeNode.Content?.InlineJson != null 
                    ? JsonSerializer.Deserialize<dynamic>(typeNode.Content.InlineJson) 
                    : null;
                
                if (fields != null)
                {
                    // Create field nodes and connect them to the type
                    var fieldSpecs = typeNode.Meta?.GetValueOrDefault("fields") as IEnumerable<FieldSpec>;
                    if (fieldSpecs != null)
                    {
                        foreach (var field in fieldSpecs)
                        {
                            var fieldNodeId = $"{typeNode.Id}.field.{field.Name}";
                            var fieldNode = new Node(
                                Id: fieldNodeId,
                                TypeId: "codex.meta/field",
                                State: ContentState.Ice,
                                Locale: "en",
                                Title: $"{field.Name} Field",
                                Description: field.Description ?? $"Field {field.Name}",
                                Content: new ContentRef(
                                    MediaType: "application/json",
                                    InlineJson: JsonSerializer.Serialize(field),
                                    InlineBytes: null,
                                    ExternalUri: null
                                ),
                                Meta: new Dictionary<string, object>
                                {
                                    ["fieldName"] = field.Name,
                                    ["fieldType"] = field.Type,
                                    ["required"] = field.Required,
                                    ["parentType"] = typeNode.Id
                                }
                            );
                            _registry.Upsert(fieldNode);

                            var fieldEdge = NodeHelpers.CreateEdge(
                                typeNode.Id,
                                fieldNodeId,
                                "has_field",
                                1.0,
                                new Dictionary<string, object> { ["relationship"] = "type-has-field" },
                                NodeHelpers.TryResolveRoleId(_registry, "has_field")
                            );
                            _registry.Upsert(fieldEdge);
                            results.Add($"Connected type {typeNode.Id} to field {field.Name}");
                        }
                    }
                }
            }

            return new SuccessResponse($"Ensured meta-node edges. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring meta-node edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring meta-node edges: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-content", "Ensure Content Edges", "Ensures edges for content nodes", "codex.edge-ensurance")]
    public async Task<object> EnsureContentEdgesAsync()
    {
        try
        {
            var results = new List<string>();

            // Connect content nodes to their content types
            var contentNodes = _registry.AllNodes()
                .Where(n => n.Content != null && !string.IsNullOrEmpty(n.Content.MediaType))
                .ToList();

            foreach (var contentNode in contentNodes)
            {
                var mediaType = contentNode.Content.MediaType;
                var contentTypeId = GetContentTypeId(mediaType);
                
                if (!string.IsNullOrEmpty(contentTypeId))
                {
                    var contentEdge = NodeHelpers.CreateEdge(
                        contentNode.Id,
                        contentTypeId,
                        "has_content_type",
                        1.0,
                        new Dictionary<string, object> 
                        { 
                            ["relationship"] = "node-has-content-type",
                            ["mediaType"] = mediaType
                        },
                        NodeHelpers.TryResolveRoleId(_registry, "has_content_type")
                    );
                    _registry.Upsert(contentEdge);
                    results.Add($"Connected content node {contentNode.Id} to content type {contentTypeId}");
                }

                // Connect to parent node if it's a derived content node
                if (contentNode.Meta?.ContainsKey("parentNodeId") == true)
                {
                    var parentNodeId = contentNode.Meta["parentNodeId"].ToString();
                    if (!string.IsNullOrEmpty(parentNodeId))
                    {
                        var parentEdge = NodeHelpers.CreateEdge(
                            parentNodeId,
                            contentNode.Id,
                            "has_content",
                            1.0,
                            new Dictionary<string, object> { ["relationship"] = "node-has-content" },
                            NodeHelpers.TryResolveRoleId(_registry, "has_content")
                        );
                        _registry.Upsert(parentEdge);
                        results.Add($"Connected parent node {parentNodeId} to content node {contentNode.Id}");
                    }
                }
            }

            return new SuccessResponse($"Ensured content edges. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring content edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring content edges: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-concepts", "Ensure Concept Edges", "Ensures edges for concept nodes", "codex.edge-ensurance")]
    public async Task<object> EnsureConceptEdgesAsync()
    {
        try
        {
            var results = new List<string>();

            // Connect concept nodes to U-CORE axes
            var conceptNodes = _registry.GetNodesByType("codex.concept.extracted")
                .Concat(_registry.GetNodesByType("codex.concept.intermediate"))
                .Concat(_registry.GetNodesByType("codex.concept.fractal"))
                .ToList();

            foreach (var conceptNode in conceptNodes)
            {
                // Connect to concept meta-type
                var conceptTypeEdge = NodeHelpers.CreateEdge(
                    conceptNode.Id,
                    "codex.meta/type/concept",
                    "instance-of",
                    1.0,
                    new Dictionary<string, object> { ["relationship"] = "concept-instance-of-type" },
                    NodeHelpers.TryResolveRoleId(_registry, "instance-of")
                );
                _registry.Upsert(conceptTypeEdge);
                results.Add($"Connected concept {conceptNode.Id} to concept meta-type");

                // Connect to U-CORE axes if specified in metadata
                if (conceptNode.Meta?.ContainsKey("ucoreAxes") == true)
                {
                    var ucoreAxes = conceptNode.Meta["ucoreAxes"] as IEnumerable<string>;
                    if (ucoreAxes != null)
                    {
                        foreach (var axis in ucoreAxes)
                        {
                            var axisNodeId = $"ucore-axis-{axis.ToLowerInvariant()}";
                            var axisEdge = NodeHelpers.CreateEdge(
                                conceptNode.Id,
                                axisNodeId,
                                "belongs_to_axis",
                                1.0,
                                new Dictionary<string, object> 
                                { 
                                    ["relationship"] = "concept-belongs-to-axis",
                                    ["axis"] = axis
                                },
                                NodeHelpers.TryResolveRoleId(_registry, "belongs_to_axis")
                            );
                            _registry.Upsert(axisEdge);
                            results.Add($"Connected concept {conceptNode.Id} to U-CORE axis {axis}");
                        }
                    }
                }
            }

            return new SuccessResponse($"Ensured concept edges. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring concept edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring concept edges: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-news", "Ensure News Edges", "Ensures edges for news nodes", "codex.edge-ensurance")]
    public async Task<object> EnsureNewsEdgesAsync()
    {
        try
        {
            // News-specific edge wiring is handled in RealtimeNewsStreamModule.
            // This endpoint is intentionally a no-op to keep responsibilities aligned.
            return new SuccessResponse("News edge wiring is handled by RealtimeNewsStreamModule. No action taken.", Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in ensure-news no-op: {ex.Message}", ex);
            return new ErrorResponse($"Error in ensure-news: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/edges/ensure-users", "Ensure User Edges", "Ensures edges for user nodes", "codex.edge-ensurance")]
    public async Task<object> EnsureUserEdgesAsync()
    {
        try
        {
            var results = new List<string>();

            // Connect user nodes to their meta-type and related concepts
            var userNodes = _registry.GetNodesByType("codex.user")
                .Concat(_registry.GetNodesByType("codex.user.profile"))
                .ToList();

            foreach (var userNode in userNodes)
            {
                // Connect to user meta-type
                var userTypeEdge = NodeHelpers.CreateEdge(
                    userNode.Id,
                    "codex.meta/type/user",
                    "instance-of",
                    1.0,
                    new Dictionary<string, object> { ["relationship"] = "user-instance-of-type" }
                );
                _registry.Upsert(userTypeEdge);
                results.Add($"Connected user node {userNode.Id} to user meta-type");

                // Connect to contributed concepts if specified
                if (userNode.Meta?.ContainsKey("contributedConcepts") == true)
                {
                    var contributedConcepts = userNode.Meta["contributedConcepts"] as IEnumerable<string>;
                    if (contributedConcepts != null)
                    {
                        foreach (var conceptId in contributedConcepts)
                        {
                            var contributionEdge = NodeHelpers.CreateEdge(
                                userNode.Id,
                                conceptId,
                                "contributed",
                                1.0,
                                new Dictionary<string, object> { ["relationship"] = "user-contributed-concept" }
                            );
                            _registry.Upsert(contributionEdge);
                            results.Add($"Connected user {userNode.Id} to contributed concept {conceptId}");
                        }
                    }
                }
            }

            return new SuccessResponse($"Ensured user edges. Created/updated {results.Count} edges.", results);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error ensuring user edges: {ex.Message}", ex);
            return new ErrorResponse($"Error ensuring user edges: {ex.Message}");
        }
    }

    private string GetContentTypeId(string mediaType)
    {
        return mediaType switch
        {
            "text/plain" => "codex.meta/type/text",
            "text/markdown" => "codex.meta/type/markdown",
            "application/json" => "codex.meta/type/json",
            "text/html" => "codex.meta/type/html",
            "image/png" => "codex.meta/type/image",
            "image/jpeg" => "codex.meta/type/image",
            "image/svg+xml" => "codex.meta/type/svg",
            "video/mp4" => "codex.meta/type/video",
            "audio/mp3" => "codex.meta/type/audio",
            _ => "codex.meta/type/content"
        };
    }
}
