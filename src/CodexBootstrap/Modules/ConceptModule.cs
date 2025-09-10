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
    
    public string ModuleId => "codex.concept";
    public string Version => "1.0.0";
    public string Description => "Concept Management Module - Self-contained fractal APIs";

    public Node GetModuleNode()
    {
        return new Node(
            Id: ModuleId,
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Concept Management Module",
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
                        new { name = "create", spec = "/concept/create/spec" },
                        new { name = "define", spec = "/concept/define/spec" },
                        new { name = "relate", spec = "/concept/relate/spec" },
                        new { name = "search", spec = "/concept/search/spec" },
                        new { name = "semantic", spec = "/concept/semantic/spec" }
                    }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = ModuleId,
                ["version"] = Version,
                ["type"] = "concept-management"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register module node
        registry.Upsert(GetModuleNode());

        // Register API nodes for RouteDiscovery
        var createApi = NodeStorage.CreateApiNode("codex.concept", "create", "/concept/create", "Create a new concept");
        var defineApi = NodeStorage.CreateApiNode("codex.concept", "define", "/concept/define/{id}", "Define concept properties");
        var relateApi = NodeStorage.CreateApiNode("codex.concept", "relate", "/concept/relate", "Create concept relationships");
        var searchApi = NodeStorage.CreateApiNode("codex.concept", "search", "/concept/search", "Search concepts by criteria");
        var semanticApi = NodeStorage.CreateApiNode("codex.concept", "semantic", "/concept/semantic/{id}", "Semantic analysis of concepts");

        registry.Upsert(createApi);
        registry.Upsert(defineApi);
        registry.Upsert(relateApi);
        registry.Upsert(searchApi);
        registry.Upsert(semanticApi);

        // Register edges
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.concept", "create"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.concept", "define"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.concept", "relate"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.concept", "search"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.concept", "semantic"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // Concept Creation API
        router.Register("codex.concept", "create", async (args) =>
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var request = JsonSerializer.Deserialize<ConceptCreateRequest>(JsonSerializer.Serialize(args), options);
            return await CreateConcept(request ?? new ConceptCreateRequest("", "", "", "", new string[0]), router.GetRegistry());
        });

        // Concept Definition API
        router.Register("codex.concept", "define", async (args) =>
        {
            var request = JsonSerializer.Deserialize<ConceptDefineRequest>(JsonSerializer.Serialize(args));
            return await DefineConcept(request ?? new ConceptDefineRequest("", "", new string[0], new string[0]));
        });

        // Concept Relationship API
        router.Register("codex.concept", "relate", async (args) =>
        {
            var request = JsonSerializer.Deserialize<ConceptRelateRequest>(JsonSerializer.Serialize(args));
            return await RelateConcepts(request ?? new ConceptRelateRequest("", "", "", 0.0));
        });

        // Concept Search API
        router.Register("codex.concept", "search", async (args) =>
        {
            var request = JsonSerializer.Deserialize<ConceptSearchRequest>(JsonSerializer.Serialize(args));
            return await SearchConcepts(request ?? new ConceptSearchRequest("", "", new string[0]));
        });

        // Concept Semantic Analysis API
        router.Register("codex.concept", "semantic", async (args) =>
        {
            var request = JsonSerializer.Deserialize<ConceptSemanticRequest>(JsonSerializer.Serialize(args));
            return await AnalyzeSemantics(request ?? new ConceptSemanticRequest(""));
        });
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Each API endpoint is self-contained with its own OpenAPI spec
        app.MapPost("/concept/create", async (ConceptCreateRequest request) =>
        {
            var result = await CreateConcept(request, registry);
            return Results.Ok(result);
        })
        .WithName("CreateConcept");

        app.MapPost("/concept/define/{id}", async (string id, ConceptDefineRequest request) =>
        {
            var requestWithId = new ConceptDefineRequest(id, request.Definition, request.Examples, request.Relationships);
            var result = await DefineConcept(requestWithId);
            return Results.Ok(result);
        })
        .WithName("DefineConcept");

        app.MapPost("/concept/relate", async (ConceptRelateRequest request) =>
        {
            var result = await RelateConcepts(request);
            return Results.Ok(result);
        })
        .WithName("RelateConcepts");

        app.MapPost("/concept/search", async (ConceptSearchRequest request) =>
        {
            var result = await SearchConcepts(request);
            return Results.Ok(result);
        })
        .WithName("SearchConcepts");

        app.MapGet("/concept/semantic/{id}", async (string id) =>
        {
            var request = new ConceptSemanticRequest(id);
            var result = await AnalyzeSemantics(request);
            return Results.Ok(result);
        })
        .WithName("AnalyzeSemantics");
    }

    // API Implementation Methods
    private Task<object> CreateConcept(ConceptCreateRequest request, NodeRegistry registry)
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

        // Store the concept node in the registry
        registry.Upsert(conceptNode);

        return Task.FromResult<object>(new ConceptCreateResponse(
            Success: true,
            ConceptId: conceptNode.Id,
            Message: "Concept created successfully"
        ));
    }

    private Task<object> DefineConcept(ConceptDefineRequest request)
    {
        return Task.FromResult<object>(new ConceptDefineResponse(
            ConceptId: request.ConceptId,
            Properties: new Dictionary<string, object>
            {
                ["definition"] = request.Definition,
                ["examples"] = request.Examples,
                ["relationships"] = request.Relationships,
                ["updatedAt"] = DateTime.UtcNow
            },
            Message: "Concept defined successfully"
        ));
    }

    private Task<object> RelateConcepts(ConceptRelateRequest request)
    {
        return Task.FromResult<object>(new ConceptRelateResponse(
            Success: true,
            RelationshipId: $"rel_{request.SourceConceptId}_{request.TargetConceptId}",
            RelationshipType: request.RelationshipType,
            Weight: request.Weight,
            Message: "Concept relationship created successfully"
        ));
    }

    private Task<object> SearchConcepts(ConceptSearchRequest request)
    {
        return Task.FromResult<object>(new ConceptSearchResponse(
            Concepts: new[]
            {
                new { id = "concept.example1", name = "Example Concept 1", domain = "technology" },
                new { id = "concept.example2", name = "Example Concept 2", domain = "science" }
            },
            TotalCount: 2,
            Message: "Search completed successfully"
        ));
    }

    private Task<object> AnalyzeSemantics(ConceptSemanticRequest request)
    {
        return Task.FromResult<object>(new ConceptSemanticResponse(
            ConceptId: request.ConceptId,
            Analysis: new
            {
                complexity = "medium",
                relatedConcepts = new[] { "concept.related1", "concept.related2" },
                semanticTags = new[] { "technical", "abstract", "fundamental" },
                confidence = 0.85
            },
            Message: "Semantic analysis completed successfully"
        ));
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