using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Loads seed data from JSONL format into the node registry
    /// </summary>
    public class SeedDataLoader
    {
        private readonly INodeRegistry _registry;
        private readonly ICodexLogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SeedDataLoader(INodeRegistry registry, ICodexLogger logger)
        {
            _registry = registry;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Load seed data from a JSONL file
        /// </summary>
        public async Task LoadSeedDataAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.Warn($"Seed data file not found: {filePath}");
                return;
            }

            try
            {
                _logger.Info($"Loading seed data from: {filePath}");
                
                var lines = await File.ReadAllLinesAsync(filePath);
                var nodes = new List<Node>();
                var edges = new List<Edge>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var jsonDoc = JsonDocument.Parse(line);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("subj", out _) && root.TryGetProperty("pred", out _) && root.TryGetProperty("obj", out _))
                        {
                            // This is an edge
                            var edge = ParseEdge(root);
                            if (edge != null)
                            {
                                edges.Add(edge);
                            }
                        }
                        else
                        {
                            // This is a node
                            var node = ParseNode(root);
                            if (node != null)
                            {
                                nodes.Add(node);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to parse seed data line: {line.Substring(0, Math.Min(100, line.Length))}... Error: {ex.Message}");
                    }
                }

                // Insert nodes first
                foreach (var node in nodes)
                {
                    try
                    {
                        _registry.Upsert(node);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to insert node {node.Id}: {ex.Message}");
                    }
                }

                // Then insert edges
                foreach (var edge in edges)
                {
                    try
                    {
                        _registry.Upsert(edge);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to insert edge {edge.FromId} -> {edge.ToId}: {ex.Message}");
                    }
                }

                _logger.Info($"Successfully loaded {nodes.Count} nodes and {edges.Count} edges from seed data");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to load seed data from {filePath}: {ex.Message}", ex);
                throw;
            }
        }

        private Node? ParseNode(JsonElement element)
        {
            try
            {
                var id = element.GetProperty("id").GetString();
                var type = element.GetProperty("type").GetString();
                var name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(type))
                {
                    return null;
                }

                // Parse metadata
                var meta = new Dictionary<string, object>();
                if (element.TryGetProperty("meta", out var metaElement))
                {
                    foreach (var prop in metaElement.EnumerateObject())
                    {
                        meta[prop.Name] = ParseJsonValue(prop.Value);
                    }
                }

                // Determine node type based on the kind or original type
                var nodeTypeId = DetermineNodeType(type, meta);

                // Parse content
                ContentRef? contentRef = null;
                if (element.TryGetProperty("content", out var contentElement))
                {
                    var contentJson = JsonSerializer.Serialize(ParseJsonValue(contentElement), _jsonOptions);
                    contentRef = new ContentRef(
                        MediaType: "application/json",
                        InlineJson: contentJson,
                        InlineBytes: null,
                        ExternalUri: null
                    );
                }

                // Parse structure and add to meta
                if (element.TryGetProperty("structure", out var structureElement))
                {
                    meta["structure"] = ParseJsonValue(structureElement);
                }

                // Add original type to meta for reference
                meta["originalType"] = type;
                if (!string.IsNullOrEmpty(name))
                {
                    meta["originalName"] = name;
                }

                return new Node(
                    Id: id,
                    TypeId: nodeTypeId,
                    State: ContentState.Ice, // All seed data is persistent
                    Locale: "en-US",
                    Title: name ?? id,
                    Description: GenerateDescription(id, type, meta),
                    Content: contentRef,
                    Meta: meta
                );
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to parse node: {ex.Message}");
                return null;
            }
        }

        private Edge? ParseEdge(JsonElement element)
        {
            try
            {
                var id = element.GetProperty("id").GetString();
                var subj = element.GetProperty("subj").GetString();
                var pred = element.GetProperty("pred").GetString();
                var obj = element.GetProperty("obj").GetString();

                if (string.IsNullOrEmpty(subj) || string.IsNullOrEmpty(pred) || string.IsNullOrEmpty(obj))
                {
                    return null;
                }

                var weight = element.TryGetProperty("weight", out var weightElement) 
                    ? (double?)weightElement.GetDouble() 
                    : null;

                var meta = new Dictionary<string, object>();
                if (element.TryGetProperty("meta", out var metaElement))
                {
                    foreach (var prop in metaElement.EnumerateObject())
                    {
                        meta[prop.Name] = ParseJsonValue(prop.Value);
                    }
                }

                // Add original ID to meta
                if (!string.IsNullOrEmpty(id))
                {
                    meta["originalId"] = id;
                }

                return new Edge(
                    FromId: subj,
                    ToId: obj,
                    Role: pred,
                    Weight: weight,
                    Meta: meta
                );
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to parse edge: {ex.Message}");
                return null;
            }
        }

        private object ParseJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonValue).ToArray(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                    prop => prop.Name,
                    prop => ParseJsonValue(prop.Value)
                ),
                _ => element.ToString()
            };
        }

        private string DetermineNodeType(string originalType, Dictionary<string, object> meta)
        {
            // Map original types and kinds to our type system
            if (meta.TryGetValue("kind", out var kindObj) && kindObj is string kind)
            {
                return kind switch
                {
                    "ucore" => "codex.ontology.ucore",
                    "axis" => "codex.ontology.axis",
                    "axis_part" => "codex.ontology.axis.part",
                    "concept" => "codex.concept",
                    "objective" => "codex.objective",
                    "policy" => "codex.policy",
                    "selector" => "codex.selector",
                    "schema" => "codex.schema",
                    "intent" => "codex.intent",
                    "role" => "codex.role",
                    "container" => "codex.container",
                    _ => $"codex.{kind}"
                };
            }

            // Fallback based on original type
            return originalType switch
            {
                "GenericNode" => "codex.generic",
                _ => $"codex.{originalType.ToLowerInvariant()}"
            };
        }

        private string GenerateDescription(string id, string type, Dictionary<string, object> meta)
        {
            var parts = new List<string>();

            if (meta.TryGetValue("kind", out var kindObj) && kindObj is string kind)
            {
                parts.Add($"{char.ToUpper(kind[0])}{kind.Substring(1)} node");
            }
            else
            {
                parts.Add($"{type} node");
            }

            if (meta.TryGetValue("epistemic_label", out var labelObj) && labelObj is string label)
            {
                parts.Add($"from {label} domain");
            }

            if (id.StartsWith("axis:"))
            {
                parts.Add("- part of U-CORE ontological framework");
            }
            else if (id.StartsWith("node:uc:"))
            {
                parts.Add("- fundamental U-CORE concept");
            }

            return string.Join(" ", parts);
        }
    }
}



