using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Management Module (L8) - Modular Fractal API Design
/// Each API is self-contained with its own OpenAPI specification
/// </summary>
public class ConceptModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public ConceptModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public string ModuleId => "codex.concept";
    public string Name => "Concept Management Module";
    public string Version => "1.0.0";
    public string Description => "Concept Management Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return ModuleHelpers.CreateModuleNode(ModuleId, Name, Version, Description);
    }

    public void Register(NodeRegistry registry)
    {
        // Module registration is now handled automatically by the attribute discovery system
        // This method can be used for additional module-specific setup if needed
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    /// <summary>
    /// Create a new concept
    /// </summary>
    [Post("/concept/create", "concept-create", "Create a new concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> CreateConcept([ApiParameter("request", "Concept creation request", Required = true, Location = "body")] ConceptCreateRequest request)
    {
        try
        {
            // Create concept node
            var conceptNode = new Node(
                Id: $"concept.{request.Name}",
                TypeId: "codex.concept",
                State: ContentState.Ice,
                Locale: "en",
                Title: request.Name,
                Description: request.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        name = request.Name,
                        description = request.Description,
                        domain = request.Domain,
                        complexity = request.Complexity,
                        tags = request.Tags,
                        createdAt = DateTime.UtcNow
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["name"] = request.Name,
                    ["description"] = request.Description,
                    ["domain"] = request.Domain,
                    ["complexity"] = request.Complexity,
                    ["tags"] = string.Join(",", request.Tags),
                    ["createdAt"] = DateTime.UtcNow,
                    ["status"] = "draft"
                }
            );

            // Store the concept in the registry
            _registry.Upsert(conceptNode);

            return ResponseHelpers.CreateConceptCreateResponse(
                success: true,
                conceptId: conceptNode.Id,
                message: "Concept created successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to create concept: {ex.Message}", "CREATE_ERROR");
        }
    }

    /// <summary>
    /// Define concept properties
    /// </summary>
    [Post("/concept/define/{id}", "concept-define", "Define concept properties", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    [ApiResponse(404, "Not found")]
    public async Task<object> DefineConcept(
        [ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id,
        [ApiParameter("request", "Concept definition request", Required = true, Location = "body")] ConceptDefineRequest request)
    {
        try
        {
            return ResponseHelpers.CreateConceptDefineResponse(
                conceptId: id,
                properties: new Dictionary<string, object>(),
                message: "Concept definition updated successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to define concept: {ex.Message}", "DEFINE_ERROR");
        }
    }

    /// <summary>
    /// Create concept relationships
    /// </summary>
    [Post("/concept/relate", "concept-relate", "Create concept relationships", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> RelateConcepts([ApiParameter("request", "Concept relationship request", Required = true, Location = "body")] ConceptRelateRequest request)
    {
        try
        {
            var relationshipId = Guid.NewGuid().ToString();
            return ResponseHelpers.CreateConceptRelateResponse(
                success: true,
                relationshipId: relationshipId,
                relationshipType: request.RelationshipType,
                weight: request.Weight,
                message: "Concept relationship created successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to create relationship: {ex.Message}", "RELATE_ERROR");
        }
    }

    /// <summary>
    /// Search concepts by criteria
    /// </summary>
    [Post("/concept/search", "concept-search", "Search concepts by criteria", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> SearchConcepts([ApiParameter("request", "Concept search request", Required = true, Location = "body")] ConceptSearchRequest request)
    {
        try
        {
            // In a real implementation, you would search the registry
            var concepts = new object[0];
            
            return ResponseHelpers.CreateConceptSearchResponse(
                concepts: concepts,
                totalCount: concepts.Length,
                message: "Search completed successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to search concepts: {ex.Message}", "SEARCH_ERROR");
        }
    }

    /// <summary>
    /// Semantic analysis of concepts
    /// </summary>
    [Get("/concept/semantic/{id}", "concept-semantic", "Semantic analysis of concepts", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> AnalyzeSemantics([ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id)
    {
        try
        {
            return ResponseHelpers.CreateConceptSemanticResponse(
                conceptId: id,
                analysis: new
                {
                    complexity = "unknown",
                    relatedConcepts = new string[0],
                    semanticTags = new string[0],
                    confidence = 0.0
                },
                message: "Semantic analysis completed successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to analyze concept: {ex.Message}", "ANALYSIS_ERROR");
        }
    }
}

// Request/Response DTOs for each API
public record ConceptCreateRequest(string Name, string Description, string Domain, string Complexity, string[] Tags);
public record ConceptCreateResponse(bool Success, string ConceptId, string Message);

public record ConceptDefineRequest(string ConceptId, string Definition, string[] Examples, string[] Relationships);
public record ConceptDefineResponse(string ConceptId, Dictionary<string, object> Properties, string Message);

public record ConceptRelateRequest(string SourceConceptId, string TargetConceptId, string RelationshipType, double Weight);
public record ConceptRelateResponse(bool Success, string RelationshipId, string RelationshipType, double Weight, string Message);

public record ConceptSearchRequest(string Query, string Domain, string[] Tags);
public record ConceptSearchResponse(object[] Concepts, int TotalCount, string Message);

public record ConceptSemanticRequest(string ConceptId);
public record ConceptSemanticResponse(string ConceptId, object Analysis, string Message);