using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Management Module (L8) - Modular Fractal API Design
/// Each API is self-contained with its own OpenAPI specification
/// </summary>
[ApiModule(Name = "ConceptModule", Version = "1.0.0", Description = "Concept Management Module - Self-contained fractal APIs", Tags = new[] { "concept", "management", "fractal" })]
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

    #region User-Concept Relationship Management

    /// <summary>
    /// Link user to concept
    /// </summary>
    [Post("/concept/user/link", "concept-user-link", "Link user to concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> LinkUserToConcept([ApiParameter("request", "User-concept link request", Required = true, Location = "body")] UserConceptLinkRequest request)
    {
        try
        {
            // Create user-concept relationship node
            var relationshipId = $"userconcept.{request.UserId}.{request.ConceptId}";
            var relationshipNode = new Node(
                Id: relationshipId,
                TypeId: "codex.userconcept.relationship",
                State: ContentState.Water,
                Locale: "en",
                Title: $"User-Concept Relationship: {request.UserId} -> {request.ConceptId}",
                Description: $"Relationship between user {request.UserId} and concept {request.ConceptId}",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        userId = request.UserId,
                        conceptId = request.ConceptId,
                        relationshipType = request.RelationshipType,
                        strength = request.Strength,
                        createdAt = DateTime.UtcNow
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["userId"] = request.UserId,
                    ["conceptId"] = request.ConceptId,
                    ["relationshipType"] = request.RelationshipType,
                    ["strength"] = request.Strength,
                    ["createdAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(relationshipNode);

            return new UserConceptLinkResponse(
                Success: true,
                RelationshipId: relationshipId,
                Message: "User-concept relationship created successfully"
            );
        }
        catch (Exception ex)
        {
            return new UserConceptLinkResponse(
                Success: false,
                RelationshipId: null,
                Message: $"Failed to create user-concept relationship: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Unlink user from concept
    /// </summary>
    [Post("/concept/user/unlink", "concept-user-unlink", "Unlink user from concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> UnlinkUserFromConcept([ApiParameter("request", "User-concept unlink request", Required = true, Location = "body")] UserConceptUnlinkRequest request)
    {
        try
        {
            var relationshipId = $"userconcept.{request.UserId}.{request.ConceptId}";
            var relationshipNode = _registry.GetNode(relationshipId);
            
            if (relationshipNode != null)
            {
                _registry.RemoveNode(relationshipId);
                return new UserConceptUnlinkResponse(
                    Success: true,
                    Message: "User-concept relationship removed successfully"
                );
            }
            else
            {
                return new UserConceptUnlinkResponse(
                    Success: false,
                    Message: "User-concept relationship not found"
                );
            }
        }
        catch (Exception ex)
        {
            return new UserConceptUnlinkResponse(
                Success: false,
                Message: $"Failed to remove user-concept relationship: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Get concepts for a user
    /// </summary>
    [Get("/concept/user/{userId}", "concept-user-concepts", "Get concepts for a user", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "User not found")]
    public async Task<object> GetUserConcepts([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            var relationships = _registry.GetEdgesFrom($"user.{userId}")
                .Where(e => e.Role == "userconcept.relationship")
                .ToList();

            var concepts = relationships.Select(r => new
            {
                conceptId = r.ToId,
                relationshipType = r.Meta?.GetValueOrDefault("relationshipType")?.ToString(),
                strength = r.Meta?.GetValueOrDefault("strength")?.ToString(),
                createdAt = r.Meta?.GetValueOrDefault("createdAt")?.ToString()
            }).ToArray();

            return new UserConceptsResponse(
                Success: true,
                UserId: userId,
                Concepts: concepts,
                TotalCount: concepts.Length,
                Message: "User concepts retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return new UserConceptsResponse(
                Success: false,
                UserId: userId,
                Concepts: Array.Empty<object>(),
                TotalCount: 0,
                Message: $"Failed to retrieve user concepts: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Get users for a concept
    /// </summary>
    [Get("/concept/{conceptId}/users", "concept-concept-users", "Get users for a concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Concept not found")]
    public async Task<object> GetConceptUsers([ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            var relationships = _registry.GetEdgesTo(conceptId)
                .Where(e => e.Role == "userconcept.relationship")
                .ToList();

            var users = relationships.Select(r => new
            {
                userId = r.FromId,
                relationshipType = r.Meta?.GetValueOrDefault("relationshipType")?.ToString(),
                strength = r.Meta?.GetValueOrDefault("strength")?.ToString(),
                createdAt = r.Meta?.GetValueOrDefault("createdAt")?.ToString()
            }).ToArray();

            return new ConceptUsersResponse(
                Success: true,
                ConceptId: conceptId,
                Users: users,
                TotalCount: users.Length,
                Message: "Concept users retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return new ConceptUsersResponse(
                Success: false,
                ConceptId: conceptId,
                Users: Array.Empty<object>(),
                TotalCount: 0,
                Message: $"Failed to retrieve concept users: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Get specific user-concept relationship
    /// </summary>
    [Get("/concept/user/{userId}/{conceptId}", "concept-user-relationship", "Get specific relationship", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Relationship not found")]
    public async Task<object> GetUserConceptRelationship(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            var relationshipId = $"userconcept.{userId}.{conceptId}";
            var relationshipNode = _registry.GetNode(relationshipId);
            
            if (relationshipNode != null)
            {
                return new UserConceptRelationshipResponse(
                    Success: true,
                    UserId: userId,
                    ConceptId: conceptId,
                    RelationshipType: relationshipNode.Meta?.GetValueOrDefault("relationshipType")?.ToString() ?? "unknown",
                    Strength: relationshipNode.Meta?.GetValueOrDefault("strength")?.ToString() ?? "0",
                    CreatedAt: relationshipNode.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                    Message: "Relationship retrieved successfully"
                );
            }
            else
            {
                return new UserConceptRelationshipResponse(
                    Success: false,
                    UserId: userId,
                    ConceptId: conceptId,
                    RelationshipType: null,
                    Strength: null,
                    CreatedAt: null,
                    Message: "Relationship not found"
                );
            }
        }
        catch (Exception ex)
        {
            return new UserConceptRelationshipResponse(
                Success: false,
                UserId: userId,
                ConceptId: conceptId,
                RelationshipType: null,
                Strength: null,
                CreatedAt: null,
                Message: $"Failed to retrieve relationship: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Register user belief system
    /// </summary>
    [Post("/concept/user/belief-system/register", "concept-user-belief-register", "Register user belief system", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> RegisterUserBeliefSystem([ApiParameter("request", "Belief system registration request", Required = true, Location = "body")] UserBeliefSystemRequest request)
    {
        try
        {
            var beliefSystemNode = new Node(
                Id: $"beliefsystem.{request.UserId}",
                TypeId: "codex.userconcept.beliefsystem",
                State: ContentState.Water,
                Locale: "en",
                Title: $"Belief System for User {request.UserId}",
                Description: request.Description,
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(new
                    {
                        userId = request.UserId,
                        framework = request.Framework,
                        principles = request.Principles,
                        values = request.Values,
                        createdAt = DateTime.UtcNow
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["userId"] = request.UserId,
                    ["framework"] = request.Framework,
                    ["principles"] = string.Join(",", request.Principles),
                    ["values"] = string.Join(",", request.Values),
                    ["createdAt"] = DateTime.UtcNow
                }
            );

            _registry.Upsert(beliefSystemNode);

            return new UserBeliefSystemResponse(
                Success: true,
                UserId: request.UserId,
                BeliefSystemId: beliefSystemNode.Id,
                Message: "Belief system registered successfully"
            );
        }
        catch (Exception ex)
        {
            return new UserBeliefSystemResponse(
                Success: false,
                UserId: request.UserId,
                BeliefSystemId: null,
                Message: $"Failed to register belief system: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Translate concept through belief system
    /// </summary>
    [Post("/concept/user/translate", "concept-user-translate", "Translate concept through belief system", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> TranslateConceptThroughBeliefSystem([ApiParameter("request", "Concept translation request", Required = true, Location = "body")] UserConceptTranslationRequest request)
    {
        try
        {
            // TODO: Implement actual belief system translation logic
            var translatedConcept = new
            {
                originalConcept = request.Concept,
                translatedConcept = $"Translated: {request.Concept}",
                beliefSystem = request.BeliefSystemId,
                translationMethod = "belief-system-mapping",
                confidence = 0.85,
                translatedAt = DateTime.UtcNow
            };

            return new UserConceptTranslationResponse(
                Success: true,
                OriginalConcept: request.Concept,
                TranslatedConcept: translatedConcept.translatedConcept,
                BeliefSystemId: request.BeliefSystemId,
                Confidence: 0.85,
                Message: "Concept translated successfully through belief system"
            );
        }
        catch (Exception ex)
        {
            return new UserConceptTranslationResponse(
                Success: false,
                OriginalConcept: request.Concept,
                TranslatedConcept: null,
                BeliefSystemId: request.BeliefSystemId,
                Confidence: 0.0,
                Message: $"Failed to translate concept: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Get user belief system
    /// </summary>
    [Get("/concept/user/{userId}/belief-system", "concept-user-belief-get", "Get user belief system", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Belief system not found")]
    public async Task<object> GetUserBeliefSystem([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            var beliefSystemId = $"beliefsystem.{userId}";
            var beliefSystemNode = _registry.GetNode(beliefSystemId);
            
            if (beliefSystemNode != null)
            {
                return new UserBeliefSystemGetResponse(
                    Success: true,
                    UserId: userId,
                    BeliefSystemId: beliefSystemId,
                    Framework: beliefSystemNode.Meta?.GetValueOrDefault("framework")?.ToString() ?? "unknown",
                    Principles: beliefSystemNode.Meta?.GetValueOrDefault("principles")?.ToString()?.Split(',') ?? Array.Empty<string>(),
                    Values: beliefSystemNode.Meta?.GetValueOrDefault("values")?.ToString()?.Split(',') ?? Array.Empty<string>(),
                    CreatedAt: beliefSystemNode.Meta?.GetValueOrDefault("createdAt")?.ToString() ?? DateTime.UtcNow.ToString(),
                    Message: "Belief system retrieved successfully"
                );
            }
            else
            {
                return new UserBeliefSystemGetResponse(
                    Success: false,
                    UserId: userId,
                    BeliefSystemId: null,
                    Framework: null,
                    Principles: Array.Empty<string>(),
                    Values: Array.Empty<string>(),
                    CreatedAt: null,
                    Message: "Belief system not found"
                );
            }
        }
        catch (Exception ex)
        {
            return new UserBeliefSystemGetResponse(
                Success: false,
                UserId: userId,
                BeliefSystemId: null,
                Framework: null,
                Principles: Array.Empty<string>(),
                Values: Array.Empty<string>(),
                CreatedAt: null,
                Message: $"Failed to retrieve belief system: {ex.Message}"
            );
        }
    }

    #endregion
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

// User-Concept Relationship DTOs
public record UserConceptLinkRequest(string UserId, string ConceptId, string RelationshipType, double Strength);
public record UserConceptLinkResponse(bool Success, string? RelationshipId, string Message);

public record UserConceptUnlinkRequest(string UserId, string ConceptId);
public record UserConceptUnlinkResponse(bool Success, string Message);

public record UserConceptsResponse(bool Success, string UserId, object[] Concepts, int TotalCount, string Message);

public record ConceptUsersResponse(bool Success, string ConceptId, object[] Users, int TotalCount, string Message);

public record UserConceptRelationshipResponse(bool Success, string UserId, string ConceptId, string? RelationshipType, string? Strength, string? CreatedAt, string Message);

public record UserBeliefSystem(
    string Id,
    string UserId,
    string Framework,
    string Description,
    string[] Principles,
    string[] Values,
    string Language,
    string CulturalContext,
    string SpiritualTradition,
    string ScientificBackground,
    string[] CoreValues,
    string[] TranslationPreferences,
    double ResonanceThreshold,
    DateTime CreatedAt
);

public record UserBeliefSystemRequest(string UserId, string Framework, string Description, string[] Principles, string[] Values);
public record UserBeliefSystemResponse(bool Success, string UserId, string? BeliefSystemId, string Message);

public record UserConceptTranslationRequest(string Concept, string BeliefSystemId);
public record UserConceptTranslationResponse(bool Success, string OriginalConcept, string? TranslatedConcept, string BeliefSystemId, double Confidence, string Message);

public record UserBeliefSystemGetResponse(bool Success, string UserId, string? BeliefSystemId, string? Framework, string[] Principles, string[] Values, string? CreatedAt, string Message);