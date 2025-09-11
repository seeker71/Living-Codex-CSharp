using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Future Knowledge Data Types

[MetaNodeAttribute("codex.future.knowledge", "codex.meta/type", "FutureKnowledge", "Knowledge retrieved from future states")]
public record FutureKnowledge(
    string Id,
    object Content,
    DateTime Timestamp,
    double Confidence,
    string Source
);

[MetaNodeAttribute("codex.future.delta", "codex.meta/type", "FutureDelta", "Delta representing future changes")]
public record FutureDelta(
    string Id,
    string TargetNodeId,
    object Changes,
    int Priority,
    Dictionary<string, object> Metadata
);

/// <summary>
/// Future Knowledge Module - Retrieves and applies knowledge from future states
/// </summary>
public class FutureKnowledgeModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;
    private CoreApiService? _coreApiService;

    public FutureKnowledgeModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.future",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Future Knowledge Module",
            Description: "Retrieves and applies knowledge from future states",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.future",
                    Name = "Future Knowledge Module",
                    Description = "Retrieves and applies knowledge from future states",
                    Version = "1.0.0"
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.future",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes, so this is empty
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Store CoreApiService reference for cross-service communication
        _coreApiService = coreApi;
        // HTTP endpoints are registered via attributes, so this is empty
    }

    [ApiRoute("POST", "/future/retrieve", "future-retrieve", "Retrieve knowledge from future", "codex.future")]
    public async Task<object> RetrieveFutureKnowledge([ApiParameter("request", "Future knowledge request", Required = true, Location = "body")] FutureKnowledgeRequest request)
    {
        try
        {
            // Simulate future knowledge retrieval
            var futureKnowledge = await RetrieveFromFuture(request.Query, request.Context);
            
            // Store as a node in the existing registry
            var knowledgeNode = new Node(
                Id: Guid.NewGuid().ToString(),
                TypeId: "codex.future.knowledge",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Future Knowledge",
                Description: "Knowledge retrieved from future",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(futureKnowledge),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["timestamp"] = DateTime.UtcNow,
                    ["confidence"] = futureKnowledge.Confidence,
                    ["source"] = futureKnowledge.Source,
                    ["query"] = request.Query,
                    ["context"] = request.Context
                }
            );
            
            _registry.Upsert(knowledgeNode);
            
            return new FutureKnowledgeResponse(true, "Future knowledge retrieved and stored", knowledgeNode.Id, futureKnowledge);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to retrieve future knowledge: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/future/apply-delta", "future-apply-delta", "Apply future delta to existing node", "codex.future")]
    public async Task<object> ApplyFutureDelta([ApiParameter("request", "Delta application request", Required = true, Location = "body")] ApplyDeltaRequest request)
    {
        try
        {
            // Get the target node
            if (!_registry.TryGet(request.TargetNodeId, out var targetNode))
            {
                return new ErrorResponse("Target node not found");
            }

            // Get the future delta
            if (!_registry.TryGet(request.DeltaId, out var deltaNode))
            {
                return new ErrorResponse("Future delta not found");
            }

            // Apply the future delta using the existing diff engine
            var updatedNode = ApplyDeltaToNode(targetNode, deltaNode);
            
            // Store the updated node
            _registry.Upsert(updatedNode);
            
            return new SuccessResponse("Future delta applied successfully");
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to apply future delta: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/future/knowledge/{id}", "future-get-knowledge", "Get stored future knowledge", "codex.future")]
    public async Task<object> GetFutureKnowledge([ApiParameter("id", "Knowledge ID", Required = true, Location = "path")] string id)
    {
        try
        {
            if (!_registry.TryGet(id, out var knowledgeNode))
            {
                return new ErrorResponse("Future knowledge not found");
            }

            return new FutureKnowledgeResponse(true, "Future knowledge retrieved", id, knowledgeNode.Content?.InlineJson);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get future knowledge: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/future/merge", "future-merge", "Merge future knowledge with existing nodes", "codex.future")]
    public async Task<object> MergeFutureKnowledge([ApiParameter("request", "Merge request", Required = true, Location = "body")] MergeRequest request)
    {
        try
        {
            var results = new List<MergeResult>();
            
            foreach (var targetId in request.TargetNodeIds)
            {
                if (!_registry.TryGet(targetId, out var targetNode))
                {
                    results.Add(new MergeResult(targetId, false, "Target node not found"));
                    continue;
                }

                if (!_registry.TryGet(request.KnowledgeId, out var knowledgeNode))
                {
                    results.Add(new MergeResult(targetId, false, "Future knowledge not found"));
                    continue;
                }

                // Merge future knowledge with target node
                var mergedNode = MergeWithFutureKnowledge(targetNode, knowledgeNode);
                _registry.Upsert(mergedNode);
                
                results.Add(new MergeResult(targetId, true, "Successfully merged"));
            }
            
            return new MergeResponse(true, "Merge operation completed", results);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to merge future knowledge: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/future/search", "future-search", "Search for future knowledge", "codex.future")]
    public async Task<object> SearchFutureKnowledge([ApiParameter("query", "Search query", Required = false, Location = "query")] string? query = null)
    {
        try
        {
            var allNodes = _registry.AllNodes();
            var futureKnowledgeNodes = allNodes
                .Where(n => n.TypeId == "codex.future.knowledge")
                .ToList();

            if (!string.IsNullOrEmpty(query))
            {
                futureKnowledgeNodes = futureKnowledgeNodes
                    .Where(n => n.Meta?.ContainsKey("query") == true && 
                               n.Meta["query"].ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            var results = futureKnowledgeNodes.Select(n => new FutureKnowledgeSummary(
                n.Id,
                n.Meta?.GetValueOrDefault("query", "").ToString() ?? "",
                Convert.ToDouble(n.Meta?.GetValueOrDefault("confidence", 0.0)),
                Convert.ToDateTime(n.Meta?.GetValueOrDefault("timestamp", DateTime.MinValue))
            )).ToList();

            return new SearchResponse(true, $"Found {results.Count} future knowledge items", results);
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to search future knowledge: {ex.Message}");
        }
    }

    private async Task<FutureKnowledge> RetrieveFromFuture(string query, string context)
    {
        // Simulate future knowledge retrieval
        // In a real implementation, this would connect to your future knowledge source
        await Task.Delay(100); // Simulate async operation
        
        return new FutureKnowledge(
            Id: Guid.NewGuid().ToString(),
            Content: new
            {
                Query = query,
                Context = context,
                PredictedOutcome = $"Future prediction for: {query}",
                Confidence = 0.85,
                Timestamp = DateTime.UtcNow.AddDays(1) // Simulate future timestamp
            },
            Timestamp: DateTime.UtcNow,
            Confidence: 0.85,
            Source: "future-knowledge-engine"
        );
    }

    private Node ApplyDeltaToNode(Node targetNode, Node deltaNode)
    {
        // Simple delta application - in a real implementation, this would use your diff engine
        var deltaContent = deltaNode.Content?.InlineJson;
        var targetContent = targetNode.Content?.InlineJson;
        
        // Merge the content (simplified)
        var mergedContent = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(targetContent))
        {
            var targetDict = JsonSerializer.Deserialize<Dictionary<string, object>>(targetContent);
            if (targetDict != null)
            {
                foreach (var kvp in targetDict)
                {
                    mergedContent[kvp.Key] = kvp.Value;
                }
            }
        }
        
        if (!string.IsNullOrEmpty(deltaContent))
        {
            var deltaDict = JsonSerializer.Deserialize<Dictionary<string, object>>(deltaContent);
            if (deltaDict != null)
            {
                foreach (var kvp in deltaDict)
                {
                    mergedContent[kvp.Key] = kvp.Value;
                }
            }
        }

        return targetNode with
        {
            Content = new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(mergedContent),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta = new Dictionary<string, object>(targetNode.Meta ?? new Dictionary<string, object>())
            {
                ["lastModified"] = DateTime.UtcNow,
                ["appliedDelta"] = deltaNode.Id
            }
        };
    }

    private Node MergeWithFutureKnowledge(Node targetNode, Node knowledgeNode)
    {
        // Merge future knowledge with target node
        var knowledgeContent = knowledgeNode.Content?.InlineJson;
        var targetContent = targetNode.Content?.InlineJson;
        
        // Create merged content
        var mergedContent = new Dictionary<string, object>();
        
        if (!string.IsNullOrEmpty(targetContent))
        {
            var targetDict = JsonSerializer.Deserialize<Dictionary<string, object>>(targetContent);
            if (targetDict != null)
            {
                foreach (var kvp in targetDict)
                {
                    mergedContent[kvp.Key] = kvp.Value;
                }
            }
        }
        
        // Add future knowledge insights
        mergedContent["futureInsights"] = knowledgeContent;
        mergedContent["mergedAt"] = DateTime.UtcNow;
        mergedContent["confidence"] = knowledgeNode.Meta?.GetValueOrDefault("confidence", 0.0);

        return targetNode with
        {
            Content = new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(mergedContent),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta = new Dictionary<string, object>(targetNode.Meta ?? new Dictionary<string, object>())
            {
                ["lastModified"] = DateTime.UtcNow,
                ["mergedWithFutureKnowledge"] = knowledgeNode.Id
            }
        };
    }

    /// <summary>
    /// Import concepts from another service and analyze them for future knowledge
    /// </summary>
    [ApiRoute("POST", "/future/import-concepts", "future-import-concepts", "Import concepts from another service for future analysis", "codex.future")]
    public async Task<object> ImportConceptsFromService([ApiParameter("request", "Concept import request", Required = true, Location = "body")] ConceptImportRequest request)
    {
        try
        {
            if (_coreApiService == null)
            {
                return new ErrorResponse("CoreApiService not available for cross-service communication");
            }

            // Get concepts from the source service
            var concepts = await GetConceptsFromService(request.SourceServiceId, request.ConceptIds);
            if (!concepts.Any())
            {
                return new ErrorResponse($"No concepts found in service '{request.SourceServiceId}'");
            }

            var importedConcepts = new List<ImportedConcept>();
            var futureInsights = new List<FutureInsight>();

            foreach (var concept in concepts)
            {
                // Analyze the concept for future potential
                var futureAnalysis = await AnalyzeConceptForFuture(concept, request.AnalysisContext);
                
                // Translate the concept if needed
                var translatedConcept = await TranslateConceptIfNeeded(concept, request.TargetBeliefSystem);
                
                // Store the imported concept
                var importedConcept = new ImportedConcept(
                    OriginalConcept: concept,
                    TranslatedConcept: translatedConcept,
                    FutureAnalysis: futureAnalysis,
                    ImportTimestamp: DateTime.UtcNow,
                    SourceServiceId: request.SourceServiceId
                );

                importedConcepts.Add(importedConcept);

                // Generate future insights
                var insight = await GenerateFutureInsight(concept, futureAnalysis);
                if (insight != null)
                {
                    futureInsights.Add(insight);
                }

                // Store as node in registry
                var conceptNode = new Node(
                    Id: $"imported-concept-{concept.Id}-{Guid.NewGuid()}",
                    TypeId: "codex.future.imported-concept",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: concept.Name,
                    Description: $"Imported concept from service {request.SourceServiceId}",
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(importedConcept),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["sourceServiceId"] = request.SourceServiceId,
                        ["originalConceptId"] = concept.Id,
                        ["importTimestamp"] = DateTime.UtcNow,
                        ["futurePotential"] = futureAnalysis.FuturePotential,
                        ["confidence"] = futureAnalysis.Confidence
                    }
                );
                _registry.Upsert(conceptNode);
            }

            // Publish cross-service event about the import
            await PublishConceptImportEvent(request.SourceServiceId, importedConcepts, futureInsights);

            return new ConceptImportResponse(
                Success: true,
                ImportedConcepts: importedConcepts,
                FutureInsights: futureInsights,
                Message: $"Successfully imported {importedConcepts.Count} concepts from service '{request.SourceServiceId}'"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to import concepts: {ex.Message}");
        }
    }

    /// <summary>
    /// Get future insights from imported concepts
    /// </summary>
    [ApiRoute("GET", "/future/insights", "future-get-insights", "Get future insights from imported concepts", "codex.future")]
    public async Task<object> GetFutureInsights([ApiParameter("sourceServiceId", "Source service ID", Required = false, Location = "query")] string? sourceServiceId = null)
    {
        try
        {
            var insights = new List<FutureInsight>();
            
            // Get all imported concept nodes
            var importedConceptNodes = _registry.GetNodesByType("codex.future.imported-concept");
            
            foreach (var node in importedConceptNodes)
            {
                if (sourceServiceId != null && 
                    node.Meta?.ContainsKey("sourceServiceId") == true &&
                    node.Meta["sourceServiceId"].ToString() != sourceServiceId)
                {
                    continue;
                }

                if (node.Content?.InlineJson != null)
                {
                    var importedConcept = JsonSerializer.Deserialize<ImportedConcept>(node.Content.InlineJson);
                    if (importedConcept?.FutureAnalysis != null)
                    {
                        var insight = new FutureInsight(
                            ConceptId: importedConcept.OriginalConcept.Id,
                            ConceptName: importedConcept.OriginalConcept.Name,
                            FuturePotential: importedConcept.FutureAnalysis.FuturePotential,
                            Confidence: importedConcept.FutureAnalysis.Confidence,
                            Recommendations: importedConcept.FutureAnalysis.Recommendations,
                            SourceServiceId: importedConcept.SourceServiceId,
                            GeneratedAt: DateTime.UtcNow
                        );
                        insights.Add(insight);
                    }
                }
            }

            return new FutureInsightsResponse(
                Success: true,
                Insights: insights,
                Count: insights.Count,
                Message: $"Retrieved {insights.Count} future insights"
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get future insights: {ex.Message}");
        }
    }

    // Helper methods for cross-service concept import
    private async Task<List<ConceptNode>> GetConceptsFromService(string serviceId, List<string> conceptIds)
    {
        var concepts = new List<ConceptNode>();
        
        foreach (var conceptId in conceptIds)
        {
            try
            {
                var call = new DynamicCall(
                    ModuleId: serviceId,
                    Api: "concept-get",
                    Args: JsonSerializer.SerializeToElement(new { conceptId })
                );

                var result = await _coreApiService!.ExecuteDynamicCall(call);
                
                if (result is JsonElement jsonResult && jsonResult.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    var conceptData = jsonResult.GetProperty("concept");
                    var concept = JsonSerializer.Deserialize<ConceptNode>(conceptData.GetRawText());
                    if (concept != null)
                    {
                        concepts.Add(concept);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other concepts
                Console.WriteLine($"Error getting concept {conceptId} from service {serviceId}: {ex.Message}");
            }
        }

        return concepts;
    }

    private async Task<FutureAnalysis> AnalyzeConceptForFuture(ConceptNode concept, string analysisContext)
    {
        // Use existing LLM module for future analysis
        if (_coreApiService == null)
        {
            return new FutureAnalysis(0.5, 0.5, new List<string> { "Analysis not available" });
        }

        try
        {
            var call = new DynamicCall(
                ModuleId: "codex.llm.future",
                Api: "generate-future-knowledge",
                Args: JsonSerializer.SerializeToElement(new
                {
                    query = $"Analyze the future potential of concept '{concept.Name}': {concept.Description}",
                    context = analysisContext
                })
            );

            var result = await _coreApiService.ExecuteDynamicCall(call);
            
            if (result is JsonElement jsonResult && jsonResult.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                var futurePotential = jsonResult.TryGetProperty("futurePotential", out var fp) ? fp.GetDouble() : 0.5;
                var confidence = jsonResult.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5;
                var recommendations = jsonResult.TryGetProperty("recommendations", out var rec) 
                    ? JsonSerializer.Deserialize<List<string>>(rec.GetRawText()) ?? new List<string>()
                    : new List<string>();

                return new FutureAnalysis(futurePotential, confidence, recommendations);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing concept for future: {ex.Message}");
        }

        return new FutureAnalysis(0.5, 0.5, new List<string> { "Analysis failed" });
    }

    private async Task<ConceptNode?> TranslateConceptIfNeeded(ConceptNode concept, Dictionary<string, object>? targetBeliefSystem)
    {
        if (targetBeliefSystem == null)
        {
            return concept;
        }

        try
        {
            var call = new DynamicCall(
                ModuleId: "codex.llm.future",
                Api: "translate-concept",
                Args: JsonSerializer.SerializeToElement(new
                {
                    conceptId = concept.Id,
                    conceptName = concept.Name,
                    conceptDescription = concept.Description,
                    sourceFramework = "Universal",
                    targetFramework = targetBeliefSystem.GetValueOrDefault("framework", "Unknown").ToString(),
                    userBeliefSystem = targetBeliefSystem
                })
            );

            var result = await _coreApiService!.ExecuteDynamicCall(call);
            
            if (result is JsonElement jsonResult && jsonResult.TryGetProperty("success", out var success) && success.GetBoolean())
            {
                var translatedName = jsonResult.GetProperty("translatedConcept").GetString() ?? concept.Name;
                return concept with { Name = translatedName };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error translating concept: {ex.Message}");
        }

        return concept;
    }

    private async Task<FutureInsight?> GenerateFutureInsight(ConceptNode concept, FutureAnalysis analysis)
    {
        if (analysis.FuturePotential < 0.7)
        {
            return null; // Only generate insights for high-potential concepts
        }

        return new FutureInsight(
            ConceptId: concept.Id,
            ConceptName: concept.Name,
            FuturePotential: analysis.FuturePotential,
            Confidence: analysis.Confidence,
            Recommendations: analysis.Recommendations,
            SourceServiceId: "unknown",
            GeneratedAt: DateTime.UtcNow
        );
    }

    private async Task PublishConceptImportEvent(string sourceServiceId, List<ImportedConcept> concepts, List<FutureInsight> insights)
    {
        try
        {
            var call = new DynamicCall(
                ModuleId: "codex.event-streaming",
                Api: "publish-cross-service-event",
                Args: JsonSerializer.SerializeToElement(new
                {
                    eventType = "concept_imported",
                    entityType = "concept",
                    entityId = "future-knowledge",
                    data = new
                    {
                        sourceServiceId,
                        importedCount = concepts.Count,
                        insightsCount = insights.Count,
                        concepts = concepts.Select(c => new { c.OriginalConcept.Id, c.OriginalConcept.Name }),
                        insights = insights.Select(i => new { i.ConceptName, i.FuturePotential })
                    },
                    sourceServiceId = "codex.future",
                    targetServices = new List<string> { sourceServiceId }
                })
            );

            await _coreApiService!.ExecuteDynamicCall(call);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing concept import event: {ex.Message}");
        }
    }
}

// Request/Response Types

[ResponseType("codex.future.knowledge-response", "FutureKnowledgeResponse", "Future knowledge response")]
public record FutureKnowledgeResponse(bool Success, string Message, string? KnowledgeId = null, object? Knowledge = null);

[RequestType("codex.future.knowledge-request", "FutureKnowledgeRequest", "Future knowledge request")]
public record FutureKnowledgeRequest(string Query, string Context = "");

[RequestType("codex.future.apply-delta-request", "ApplyDeltaRequest", "Apply delta request")]
public record ApplyDeltaRequest(string TargetNodeId, string DeltaId);

[RequestType("codex.future.merge-request", "MergeRequest", "Merge request")]
public record MergeRequest(string KnowledgeId, List<string> TargetNodeIds);

[ResponseType("codex.future.merge-response", "MergeResponse", "Merge response")]
public record MergeResponse(bool Success, string Message, List<MergeResult> Results);

[ResponseType("codex.future.search-response", "SearchResponse", "Search response")]
public record SearchResponse(bool Success, string Message, List<FutureKnowledgeSummary> Results);

public record MergeResult(string NodeId, bool Success, string Message);

public record FutureKnowledgeSummary(string Id, string Query, double Confidence, DateTime Timestamp);

// Cross-service concept import data models
public record ConceptImportRequest(
    string SourceServiceId,
    List<string> ConceptIds,
    string AnalysisContext = "",
    Dictionary<string, object>? TargetBeliefSystem = null
);

public record ConceptImportResponse(
    bool Success,
    List<ImportedConcept> ImportedConcepts,
    List<FutureInsight> FutureInsights,
    string Message
);

public record FutureInsightsResponse(
    bool Success,
    List<FutureInsight> Insights,
    int Count,
    string Message
);

public record ImportedConcept(
    ConceptNode OriginalConcept,
    ConceptNode? TranslatedConcept,
    FutureAnalysis FutureAnalysis,
    DateTime ImportTimestamp,
    string SourceServiceId
);

public record FutureInsight(
    string ConceptId,
    string ConceptName,
    double FuturePotential,
    double Confidence,
    List<string> Recommendations,
    string SourceServiceId,
    DateTime GeneratedAt
);

public record ConceptNode(
    string Id,
    string Name,
    string Description,
    Dictionary<string, object>? Metadata = null
);

public record FutureAnalysis(
    double FuturePotential,
    double Confidence,
    List<string> Recommendations
);

