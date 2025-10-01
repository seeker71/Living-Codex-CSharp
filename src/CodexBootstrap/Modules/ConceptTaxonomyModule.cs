using System.Text.Json;
using System.Text.RegularExpressions;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Taxonomy Module - Manages concept hierarchy, deduplication, and AI enrichment
/// Ensures concepts are specific (max 3 words), linked to parent concepts, and enriched with descriptions
/// </summary>
public class ConceptTaxonomyModule : ModuleBase
{
    private readonly HttpClient _httpClient;
    
    public override string Name => "Concept Taxonomy Module";
    public override string Description => "Manages concept hierarchy, ensures max 3-word specificity, eliminates duplicates, and enriches descriptions via Wikipedia/LLM";
    public override string Version => "1.0.0";

    public ConceptTaxonomyModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _httpClient = httpClient;
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.concept-taxonomy",
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "taxonomy", "hierarchy", "concepts", "deduplication", "enrichment", "wikipedia", "ai" },
            capabilities: new[] { "concept_normalization", "hierarchy_management", "duplicate_detection", "ai_enrichment", "wikipedia_integration" },
            spec: "codex.spec.concept-taxonomy"
        );
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        _logger.Info("Concept Taxonomy Module HTTP endpoints registered");
    }

    // ==================== NORMALIZATION ====================

    /// <summary>
    /// Normalize a concept name to max 3 words, specific, and properly cased
    /// </summary>
    [ApiRoute("POST", "/taxonomy/normalize", "normalize-concept", "Normalize concept to max 3 words and proper format", "codex.concept-taxonomy")]
    public async Task<object> NormalizeConcept([ApiParameter("request", "Normalization request", Required = true, Location = "body")] NormalizeConceptRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ConceptName))
            {
                return new ErrorResponse("Concept name is required");
            }

            var normalized = NormalizeConceptName(request.ConceptName);
            var conceptId = GenerateConceptId(normalized);

            return new
            {
                success = true,
                original = request.ConceptName,
                normalized,
                conceptId,
                wordCount = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error normalizing concept: {ex.Message}", ex);
            return new ErrorResponse($"Failed to normalize concept: {ex.Message}");
        }
    }

    /// <summary>
    /// Find and report duplicate concepts
    /// </summary>
    [ApiRoute("GET", "/taxonomy/duplicates", "find-duplicates", "Find duplicate concepts in the taxonomy", "codex.concept-taxonomy")]
    public async Task<object> FindDuplicates()
    {
        try
        {
            var allConcepts = _registry.GetNodesByTypePrefix("codex.concept").ToList();
            
            // Group by normalized title
            var groups = allConcepts
                .Where(n => !string.IsNullOrEmpty(n.Title))
                .GroupBy(n => NormalizeForComparison(n.Title!))
                .Where(g => g.Count() > 1)
                .ToList();

            var duplicates = groups.Select(g => new
            {
                normalizedTitle = g.Key,
                count = g.Count(),
                concepts = g.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    typeId = n.TypeId,
                    state = n.State.ToString()
                }).ToList()
            }).ToList();

            return new
            {
                success = true,
                duplicateGroups = duplicates.Count,
                totalDuplicateConcepts = duplicates.Sum(d => d.count),
                duplicates
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error finding duplicates: {ex.Message}", ex);
            return new ErrorResponse($"Failed to find duplicates: {ex.Message}");
        }
    }

    // ==================== HIERARCHY ====================

    /// <summary>
    /// Get the full hierarchy path from a concept to its top-level topology
    /// </summary>
    [ApiRoute("GET", "/taxonomy/hierarchy/{conceptId}", "get-hierarchy", "Get concept hierarchy path to topology root", "codex.concept-taxonomy")]
    public async Task<object> GetHierarchy(
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            var path = new List<object>();
            var visited = new HashSet<string>();
            var currentId = conceptId;

            while (currentId != null && !visited.Contains(currentId))
            {
                visited.Add(currentId);

                if (!_registry.TryGet(currentId, out var node))
                {
                    break;
                }

                path.Add(new
                {
                    id = node.Id,
                    title = node.Title,
                    typeId = node.TypeId,
                    isTopology = node.TypeId?.Contains("u-core.axis") == true || node.TypeId?.Contains("u-core-axis") == true
                });

                // Find parent via is-a relationship
                var parentEdge = _registry.GetEdgesFrom(currentId)
                    .FirstOrDefault(e => e.Role == "is-a" || e.Role == "axis_has_dimension" || e.Role == "parent");

                currentId = parentEdge?.ToId;
            }

            var reachedTopology = path.Any(p => ((dynamic)p).isTopology);

            return new
            {
                success = true,
                conceptId,
                pathLength = path.Count,
                reachedTopology,
                path
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting hierarchy: {ex.Message}", ex);
            return new ErrorResponse($"Failed to get hierarchy: {ex.Message}");
        }
    }

    /// <summary>
    /// Create parent-child relationship between concepts
    /// </summary>
    [ApiRoute("POST", "/taxonomy/link-parent", "link-parent", "Create parent-child relationship between concepts", "codex.concept-taxonomy")]
    public async Task<object> LinkToParent([ApiParameter("request", "Link request", Required = true, Location = "body")] LinkParentRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ConceptId) || string.IsNullOrEmpty(request.ParentId))
            {
                return new ErrorResponse("ConceptId and ParentId are required");
            }

            // Ensure both nodes exist
            if (!_registry.TryGet(request.ConceptId, out var concept))
            {
                return new ErrorResponse($"Concept {request.ConceptId} not found");
            }

            if (!_registry.TryGet(request.ParentId, out var parent))
            {
                return new ErrorResponse($"Parent concept {request.ParentId} not found");
            }

            // Create is-a relationship
            var edge = new Edge(
                FromId: request.ConceptId,
                ToId: request.ParentId,
                Role: "is-a",
                Weight: 1.0,
                Meta: new Dictionary<string, object>
                {
                    ["createdAt"] = DateTime.UtcNow.ToString("o"),
                    ["createdBy"] = "ConceptTaxonomyModule",
                    ["descriptionStatus"] = "placeholder"
                }
            );

            _registry.Upsert(edge);

            return new
            {
                success = true,
                message = $"Linked {concept.Title} → {parent.Title}",
                edge = new { from = concept.Title, to = parent.Title, role = "is-a" }
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error linking to parent: {ex.Message}", ex);
            return new ErrorResponse($"Failed to link to parent: {ex.Message}");
        }
    }

    // ==================== ENRICHMENT ====================

    /// <summary>
    /// Enrich concept description using Wikipedia API
    /// </summary>
    [ApiRoute("POST", "/taxonomy/enrich/{conceptId}", "enrich-concept", "Enrich concept description using Wikipedia/AI", "codex.concept-taxonomy")]
    public async Task<object> EnrichConcept(
        [ApiParameter("conceptId", "Concept ID to enrich", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            if (!_registry.TryGet(conceptId, out var concept))
            {
                return new ErrorResponse($"Concept {conceptId} not found");
            }

            if (string.IsNullOrEmpty(concept.Title))
            {
                return new ErrorResponse("Concept must have a title to enrich");
            }

            var enrichmentResult = await EnrichFromWikipedia(concept.Title);

            if (enrichmentResult.success)
            {
                // Update concept with enriched description
                var updatedConcept = new Node(
                    Id: concept.Id,
                    TypeId: concept.TypeId,
                    State: concept.State,
                    Locale: concept.Locale,
                    Title: concept.Title,
                    Description: enrichmentResult.description,
                    Content: concept.Content,
                    Meta: new Dictionary<string, object>(concept.Meta ?? new Dictionary<string, object>())
                    {
                        ["enrichmentStatus"] = "enriched",
                        ["enrichmentSource"] = enrichmentResult.source,
                        ["enrichedAt"] = DateTime.UtcNow.ToString("o"),
                        ["wikipediaUrl"] = enrichmentResult.url ?? ""
                    }
                );

                _registry.Upsert(updatedConcept);

                return new
                {
                    success = true,
                    conceptId,
                    title = concept.Title,
                    description = enrichmentResult.description,
                    source = enrichmentResult.source,
                    message = "Concept enriched successfully"
                };
            }
            else
            {
                return new ErrorResponse($"Failed to enrich concept: {enrichmentResult.error}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error enriching concept: {ex.Message}", ex);
            return new ErrorResponse($"Failed to enrich concept: {ex.Message}");
        }
    }

    /// <summary>
    /// Batch enrich multiple concepts
    /// </summary>
    [ApiRoute("POST", "/taxonomy/enrich-batch", "enrich-batch", "Enrich multiple concepts in batch", "codex.concept-taxonomy")]
    public async Task<object> EnrichBatch([ApiParameter("request", "Batch enrichment request", Required = true, Location = "body")] EnrichBatchRequest request)
    {
        try
        {
            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            foreach (var conceptId in request.ConceptIds)
            {
                var result = await EnrichConcept(conceptId);
                var resultObj = result as dynamic;
                
                if (resultObj?.success == true)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                results.Add(new { conceptId, result });
                
                // Rate limiting for Wikipedia API
                await Task.Delay(200);
            }

            return new
            {
                success = true,
                total = request.ConceptIds.Count,
                enriched = successCount,
                failed = failCount,
                results
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in batch enrichment: {ex.Message}", ex);
            return new ErrorResponse($"Batch enrichment failed: {ex.Message}");
        }
    }

    // ==================== PRIVATE HELPERS ====================

    private string NormalizeConceptName(string name)
    {
        // Remove extra whitespace
        name = Regex.Replace(name, @"\s+", " ").Trim();
        
        // Split into words
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Take max 3 words
        if (words.Length > 3)
        {
            words = words.Take(3).ToArray();
        }
        
        // Title case each word
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        
        return string.Join(" ", words);
    }

    private string GenerateConceptId(string normalizedName)
    {
        var slug = normalizedName.ToLower().Replace(" ", "-");
        return $"concept-{slug}";
    }

    private string NormalizeForComparison(string title)
    {
        return title.ToLower().Trim().Replace(" ", "").Replace("-", "").Replace("_", "");
    }

    private async Task<(bool success, string description, string source, string? url, string? error)> EnrichFromWikipedia(string conceptTitle)
    {
        try
        {
            var searchTerm = Uri.EscapeDataString(conceptTitle);
            var url = $"https://en.wikipedia.org/api/rest_v1/page/summary/{searchTerm}";
            
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<WikipediaSummary>(content);
                
                if (data?.Extract != null && data.Extract.Length > 10)
                {
                    // Take first sentence or first 200 chars
                    var description = data.Extract;
                    var firstSentence = description.Split('.').FirstOrDefault();
                    if (firstSentence != null && firstSentence.Length < 300)
                    {
                        description = firstSentence + ".";
                    }
                    else if (description.Length > 300)
                    {
                        description = description.Substring(0, 297) + "...";
                    }
                    
                    return (true, description, "wikipedia", data.ContentUrls?.Desktop?.Page, null);
                }
            }
            
            // Fallback to placeholder
            return (true, $"[PLACEHOLDER] - Concept related to {conceptTitle}", "placeholder", null, null);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Wikipedia enrichment failed for '{conceptTitle}': {ex.Message}");
            return (false, "", "", null, ex.Message);
        }
    }

    // ==================== VALIDATION ====================

    /// <summary>
    /// Validate entire taxonomy for compliance
    /// </summary>
    [ApiRoute("GET", "/taxonomy/validate", "validate-taxonomy", "Validate concept taxonomy for compliance", "codex.concept-taxonomy")]
    public async Task<object> ValidateTaxonomy()
    {
        try
        {
            var allConcepts = _registry.GetNodesByTypePrefix("codex.concept").ToList();
            var issues = new List<object>();

            foreach (var concept in allConcepts)
            {
                // Check word count
                if (concept.Title != null && concept.Title.Split(' ').Length > 3)
                {
                    issues.Add(new { 
                        conceptId = concept.Id, 
                        title = concept.Title, 
                        issue = "exceeds_3_words", 
                        wordCount = concept.Title.Split(' ').Length 
                    });
                }

                // Check for placeholder description
                if (concept.Description?.Contains("[PLACEHOLDER]") == true)
                {
                    issues.Add(new { 
                        conceptId = concept.Id, 
                        title = concept.Title, 
                        issue = "placeholder_description" 
                    });
                }

                // Check if links to topology
                var hierarchy = await GetHierarchyPath(concept.Id);
                if (!hierarchy.reachedTopology)
                {
                    issues.Add(new { 
                        conceptId = concept.Id, 
                        title = concept.Title, 
                        issue = "no_topology_link",
                        pathLength = hierarchy.pathLength
                    });
                }
            }

            var byIssueType = issues.GroupBy(i => ((dynamic)i).issue)
                .Select(g => new { issueType = g.Key, count = g.Count() })
                .ToList();

            return new
            {
                success = true,
                totalConcepts = allConcepts.Count,
                totalIssues = issues.Count,
                issuesByType = byIssueType,
                details = issues.Take(50) // Limit to first 50 for performance
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error validating taxonomy: {ex.Message}", ex);
            return new ErrorResponse($"Failed to validate taxonomy: {ex.Message}");
        }
    }

    private async Task<(int pathLength, bool reachedTopology)> GetHierarchyPath(string conceptId)
    {
        var visited = new HashSet<string>();
        var currentId = conceptId;
        var length = 0;
        var reachedTopology = false;

        while (currentId != null && !visited.Contains(currentId) && length < 20)
        {
            visited.Add(currentId);
            length++;

            if (!_registry.TryGet(currentId, out var node))
            {
                break;
            }

            if (node.TypeId?.Contains("u-core.axis") == true || node.TypeId?.Contains("u-core-axis") == true)
            {
                reachedTopology = true;
                break;
            }

            var parentEdge = _registry.GetEdgesFrom(currentId)
                .FirstOrDefault(e => e.Role == "is-a" || e.Role == "axis_has_dimension" || e.Role == "parent");

            currentId = parentEdge?.ToId;
        }

        return (length, reachedTopology);
    }
}

// Request/Response Models
public record NormalizeConceptRequest(string ConceptName);
public record LinkParentRequest(string ConceptId, string ParentId, string? RelationshipType = "is-a");
public record EnrichBatchRequest(List<string> ConceptIds);

// Wikipedia API Response Model
public class WikipediaSummary
{
    public string? Title { get; set; }
    public string? Extract { get; set; }
    public WikipediaContentUrls? ContentUrls { get; set; }
}

public class WikipediaContentUrls
{
    public WikipediaDesktopUrl? Desktop { get; set; }
}

public class WikipediaDesktopUrl
{
    public string? Page { get; set; }
}

