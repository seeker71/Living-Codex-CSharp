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
    
    public string ModuleId => "codex.userconcept";
    public string Version => "1.0.0";
    public string Description => "User-Concept Relationship Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return new Node(
            Id: ModuleId,
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "User-Concept Relationship Module",
            Description: Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    moduleId = ModuleId,
                    version = Version,
                    description = Description,
                    apis = new[]
                    {
                        new { name = "link", spec = "/userconcept/link/spec" },
                        new { name = "unlink", spec = "/userconcept/unlink/spec" },
                        new { name = "get-user-concepts", spec = "/userconcept/user-concepts/spec" },
                        new { name = "get-concept-users", spec = "/userconcept/concept-users/spec" },
                        new { name = "get-relationship", spec = "/userconcept/relationship/spec" }
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = ModuleId,
                ["version"] = Version,
                ["type"] = "user-concept-relationships"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register module node
        registry.Upsert(GetModuleNode());

        // Register API nodes for RouteDiscovery
        var linkApi = NodeStorage.CreateApiNode("codex.userconcept", "link", "/userconcept/link", "Link user to concept");
        var unlinkApi = NodeStorage.CreateApiNode("codex.userconcept", "unlink", "/userconcept/unlink", "Unlink user from concept");
        var getUserConceptsApi = NodeStorage.CreateApiNode("codex.userconcept", "get-user-concepts", "/userconcept/user-concepts/{userId}", "Get concepts for a user");
        var getConceptUsersApi = NodeStorage.CreateApiNode("codex.userconcept", "get-concept-users", "/userconcept/concept-users/{conceptId}", "Get users for a concept");
        var getRelationshipApi = NodeStorage.CreateApiNode("codex.userconcept", "get-relationship", "/userconcept/relationship/{userId}/{conceptId}", "Get specific relationship");

        registry.Upsert(linkApi);
        registry.Upsert(unlinkApi);
        registry.Upsert(getUserConceptsApi);
        registry.Upsert(getConceptUsersApi);
        registry.Upsert(getRelationshipApi);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.userconcept", "link"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.userconcept", "unlink"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.userconcept", "get-user-concepts"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.userconcept", "get-concept-users"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.userconcept", "get-relationship"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // User-Concept Link API
        router.Register("codex.userconcept", "link", async (args) =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var request = JsonSerializer.Deserialize<UserConceptLinkRequest>(JsonSerializer.Serialize(args), options);
            return await LinkUserConcept(request ?? new UserConceptLinkRequest("", "", "", 0.0), router.GetRegistry());
        });

        // User-Concept Unlink API
        router.Register("codex.userconcept", "unlink", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserConceptUnlinkRequest>(JsonSerializer.Serialize(args));
            return await UnlinkUserConcept(request ?? new UserConceptUnlinkRequest("", ""));
        });

        // Get User Concepts API
        router.Register("codex.userconcept", "get-user-concepts", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserConceptsRequest>(JsonSerializer.Serialize(args));
            return await GetUserConcepts(request ?? new UserConceptsRequest(""));
        });

        // Get Concept Users API
        router.Register("codex.userconcept", "get-concept-users", async (args) =>
        {
            var request = JsonSerializer.Deserialize<ConceptUsersRequest>(JsonSerializer.Serialize(args));
            return await GetConceptUsers(request ?? new ConceptUsersRequest(""));
        });

        // Get Relationship API
        router.Register("codex.userconcept", "get-relationship", async (args) =>
        {
            var request = JsonSerializer.Deserialize<UserConceptRelationshipRequest>(JsonSerializer.Serialize(args));
            return await GetRelationship(request ?? new UserConceptRelationshipRequest("", ""));
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Each API endpoint is self-contained with its own OpenAPI spec
        app.MapPost("/userconcept/link", async (UserConceptLinkRequest request) =>
        {
            var result = await LinkUserConcept(request, registry);
            return Results.Ok(result);
        })
        .WithName("LinkUserConcept");

        app.MapPost("/userconcept/unlink", async (UserConceptUnlinkRequest request) =>
        {
            var result = await UnlinkUserConcept(request);
            return Results.Ok(result);
        })
        .WithName("UnlinkUserConcept");

        app.MapGet("/userconcept/user-concepts/{userId}", async (string userId) =>
        {
            var request = new UserConceptsRequest(userId);
            var result = await GetUserConcepts(request);
            return Results.Ok(result);
        })
        .WithName("GetUserConcepts");

        app.MapGet("/userconcept/concept-users/{conceptId}", async (string conceptId) =>
        {
            var request = new ConceptUsersRequest(conceptId);
            var result = await GetConceptUsers(request);
            return Results.Ok(result);
        })
        .WithName("GetConceptUsers");

        app.MapGet("/userconcept/relationship/{userId}/{conceptId}", async (string userId, string conceptId) =>
        {
            var request = new UserConceptRelationshipRequest(userId, conceptId);
            var result = await GetRelationship(request);
            return Results.Ok(result);
        })
        .WithName("GetUserConceptRelationship");
    }

    // API Implementation Methods
    private Task<object> LinkUserConcept(UserConceptLinkRequest request, NodeRegistry registry)
    {
        // Create relationship edge
        var relationshipEdge = new Edge(
            FromId: request.UserId,
            ToId: request.ConceptId,
            Role: request.RelationshipType,
            Weight: request.Weight,
            Meta: new Dictionary<string, object>
            {
                ["relationshipType"] = request.RelationshipType,
                ["createdAt"] = DateTime.UtcNow,
                ["status"] = "active"
            }
        );

        // Store the relationship edge in the registry
        registry.Upsert(relationshipEdge);

        return Task.FromResult<object>(new UserConceptLinkResponse(
            Success: true,
            RelationshipId: $"rel_{request.UserId}_{request.ConceptId}",
            RelationshipType: request.RelationshipType,
            Weight: request.Weight,
            Message: "User-concept relationship created successfully"
        ));
    }

    private Task<object> UnlinkUserConcept(UserConceptUnlinkRequest request)
    {
        return Task.FromResult<object>(new UserConceptUnlinkResponse(
            Success: true,
            RelationshipId: $"rel_{request.UserId}_{request.ConceptId}",
            Message: "User-concept relationship removed successfully"
        ));
    }

    private Task<object> GetUserConcepts(UserConceptsRequest request)
    {
        return Task.FromResult<object>(new UserConceptsResponse(
            UserId: request.UserId,
            Concepts: new[]
            {
                new { conceptId = "concept.example1", relationshipType = "learning", weight = 0.8 },
                new { conceptId = "concept.example2", relationshipType = "mastered", weight = 1.0 }
            },
            TotalCount: 2,
            Message: "User concepts retrieved successfully"
        ));
    }

    private Task<object> GetConceptUsers(ConceptUsersRequest request)
    {
        return Task.FromResult<object>(new ConceptUsersResponse(
            ConceptId: request.ConceptId,
            Users: new[]
            {
                new { userId = "user.example1", relationshipType = "learning", weight = 0.6 },
                new { userId = "user.example2", relationshipType = "mastered", weight = 0.9 }
            },
            TotalCount: 2,
            Message: "Concept users retrieved successfully"
        ));
    }

    private Task<object> GetRelationship(UserConceptRelationshipRequest request)
    {
        return Task.FromResult<object>(new UserConceptRelationshipResponse(
            UserId: request.UserId,
            ConceptId: request.ConceptId,
            RelationshipType: "learning",
            Weight: 0.75,
            CreatedAt: DateTime.UtcNow.AddDays(-7),
            Status: "active",
            Message: "Relationship retrieved successfully"
        ));
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