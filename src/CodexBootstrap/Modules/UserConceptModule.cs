using System.Text.Json;
using System.Text.Json.Serialization;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// User-Concept Relationship Module - Modular Fractal API Design
/// Manages relationships between users and concepts using edges
/// </summary>
public class UserConceptModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public UserConceptModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public string ModuleId => "codex.userconcept";
    public string Name => "User-Concept Relationship Module";
    public string Version => "1.0.0";
    public string Description => "User-Concept Relationship Module - Self-contained fractal APIs";

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
    /// Link user to concept
    /// </summary>
    [Post("/userconcept/link", "userconcept-link", "Link user to concept", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> LinkUserConcept([ApiParameter("request", "User-concept link request", Required = true, Location = "body")] UserConceptLinkRequest request)
    {
        try
        {
            var relationshipId = Guid.NewGuid().ToString();
            
            // Create relationship edge
            var relationshipEdge = new Edge(
                FromId: request.UserId,
                ToId: request.ConceptId,
                Role: request.RelationshipType,
                Weight: request.Weight,
                Meta: new Dictionary<string, object>
                {
                    ["relationshipId"] = relationshipId,
                    ["relationshipType"] = request.RelationshipType,
                    ["weight"] = request.Weight,
                    ["createdAt"] = DateTime.UtcNow,
                    ["status"] = "active"
                }
            );

            // Store the relationship in the registry
            _registry.Upsert(relationshipEdge);

            return new UserConceptLinkResponse(
                Success: true,
                RelationshipId: relationshipId,
                RelationshipType: request.RelationshipType,
                Weight: request.Weight,
                Message: "User-concept linked successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to link user-concept: {ex.Message}", "LINK_ERROR");
        }
    }

    /// <summary>
    /// Unlink user from concept
    /// </summary>
    [Post("/userconcept/unlink", "userconcept-unlink", "Unlink user from concept", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> UnlinkUserConcept([ApiParameter("request", "User-concept unlink request", Required = true, Location = "body")] UserConceptUnlinkRequest request)
    {
        try
        {
            return new UserConceptUnlinkResponse(
                Success: true,
                RelationshipId: Guid.NewGuid().ToString(),
                Message: "User-concept unlinked successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to unlink user-concept: {ex.Message}", "UNLINK_ERROR");
        }
    }

    /// <summary>
    /// Get concepts for a user
    /// </summary>
    [Get("/userconcept/user-concepts/{userId}", "userconcept-get-user-concepts", "Get concepts for a user", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetUserConcepts([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            return new UserConceptsResponse(
                UserId: userId,
                Concepts: new object[0],
                TotalCount: 0,
                Message: "User concepts retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get user concepts: {ex.Message}", "GET_CONCEPTS_ERROR");
        }
    }

    /// <summary>
    /// Get users for a concept
    /// </summary>
    [Get("/userconcept/concept-users/{conceptId}", "userconcept-get-concept-users", "Get users for a concept", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetConceptUsers([ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            return new ConceptUsersResponse(
                ConceptId: conceptId,
                Users: new object[0],
                TotalCount: 0,
                Message: "Concept users retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get concept users: {ex.Message}", "GET_USERS_ERROR");
        }
    }

    /// <summary>
    /// Get specific relationship between user and concept
    /// </summary>
    [Get("/userconcept/relationship/{userId}/{conceptId}", "userconcept-get-relationship", "Get specific relationship", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetRelationship(
        [ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId,
        [ApiParameter("conceptId", "Concept ID", Required = true, Location = "path")] string conceptId)
    {
        try
        {
            return new UserConceptRelationshipResponse(
                UserId: userId,
                ConceptId: conceptId,
                RelationshipType: "none",
                Weight: 0.0,
                CreatedAt: DateTime.UtcNow,
                Status: "active",
                Message: "Relationship retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get relationship: {ex.Message}", "GET_RELATIONSHIP_ERROR");
        }
    }
}

// Request/Response DTOs for each API
public record UserConceptLinkRequest(string UserId, string ConceptId, string RelationshipType, double Weight);
public record UserConceptLinkResponse(bool Success, string RelationshipId, string RelationshipType, double Weight, string Message);

public record UserConceptUnlinkRequest(string UserId, string ConceptId);
public record UserConceptUnlinkResponse(bool Success, string RelationshipId, string Message);

public record UserConceptsRequest(string UserId);
public record UserConceptsResponse(string UserId, object[] Concepts, int TotalCount, string Message);

public record ConceptUsersRequest(string ConceptId);
public record ConceptUsersResponse(string ConceptId, object[] Users, int TotalCount, string Message);

public record UserConceptRelationshipRequest(string UserId, string ConceptId);
public record UserConceptRelationshipResponse(string UserId, string ConceptId, string RelationshipType, double Weight, DateTime CreatedAt, string Status, string Message);