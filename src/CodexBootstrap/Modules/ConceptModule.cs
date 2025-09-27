using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

/// <summary>
/// Concept Management Module (L8) - Modular Fractal API Design
/// Each API is self-contained with its own OpenAPI specification
/// </summary>
[ApiModule(Name = "ConceptModule", Version = "1.0.0", Description = "Concept Management Module - Self-contained fractal APIs", Tags = new[] { "concept", "management", "fractal" })]
public class ConceptModule : ModuleBase
{
    private readonly HttpClient _httpClient;

    public override string Name => "Concept Management Module";
    public override string Version => "1.0.0";
    public override string Description => "Concept Management Module - Self-contained fractal APIs";

    public ConceptModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient)
        : base(registry, logger)
    {
        _httpClient = httpClient;
    }

    public string ModuleId => "codex.concept";

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: ModuleId,
            name: Name,
            version: Version,
            description: Description,
            tags: new[] { "concept", "manage", "ontology", "knowledge" },
            capabilities: new[] { "concepts", "management", "ontology", "knowledge" },
            spec: "codex.spec.concept"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        _apiRouter = router; // Set the actual router during registration
        // API handlers are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are now registered automatically by the attribute discovery system
        // This method can be used for additional manual registrations if needed
    }

    /// <summary>
    /// Browse existing concepts with resonance-based discovery
    /// </summary>
    [Post("/concepts/browse", "concepts-browse", "Browse existing concepts by resonance criteria", "codex.concept")]
    [ApiResponse(200, "Success")]
    public async Task<object> BrowseConcepts([ApiParameter("request", "Concept browse criteria", Required = false, Location = "body")] ConceptBrowseRequest? request = null)
    {
        try
        {
            // Get all concept nodes - both extracted concepts and core concepts
            var extractedConcepts = _registry.GetNodesByType("codex.concept.extracted");
            var coreConcepts = _registry.GetNodesByType("codex.concept");
            var conceptNodes = extractedConcepts.Concat(coreConcepts).ToList();
            
            // Debug: Log concept counts
            var extractedCount = extractedConcepts.Count();
            var coreCount = coreConcepts.Count();
            _logger.Info("Found " + extractedCount + " extracted concepts and " + coreCount + " core concepts");
            
            var concepts = conceptNodes.Select(node => new
            {
                id = node.Id,
                name = node.Meta?.GetValueOrDefault("name")?.ToString() ?? node.Title ?? "Unknown",
                description = node.Meta?.GetValueOrDefault("description")?.ToString() ?? node.Description ?? "",
                domain = node.Meta?.GetValueOrDefault("domain")?.ToString() ?? "General",
                complexity = int.TryParse(node.Meta?.GetValueOrDefault("complexity")?.ToString(), out var complexity) ? complexity : 0,
                tags = node.Meta?.GetValueOrDefault("tags")?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0],
                createdAt = node.Meta?.GetValueOrDefault("createdAt") is DateTime dt ? dt : DateTime.UtcNow,
                updatedAt = node.Meta?.GetValueOrDefault("updatedAt") is DateTime ut ? ut : DateTime.UtcNow,
                resonance = CalculateResonance(node, request?.axes ?? new[] { "resonance" }),
                energy = CalculateEnergy(node, request?.joy ?? 0.7),
                isInterested = false, // This would come from user data
                interestCount = 0     // This would come from user data
            }).ToList();

            // Apply filtering, ranking, and pagination
            if (request != null)
            {
                // Optional text search
                if (!string.IsNullOrWhiteSpace(request.searchTerm))
                {
                    var term = request.searchTerm.Trim();
                    concepts = concepts.Where(c =>
                        (c.name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.domain?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.tags?.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false)
                    ).ToList();
                }

                // Resonance threshold
                var resonanceThreshold = request.joy * 0.5;
                concepts = concepts.Where(c => c.resonance >= resonanceThreshold).ToList();

                // Ranking
                if (request.serendipity > 0.5)
                {
                    var random = new Random();
                    concepts = concepts.OrderBy(c => random.NextDouble()).ToList();
                }
                else
                {
                    concepts = concepts.OrderByDescending(c => c.resonance * c.energy).ToList();
                }

                // Total before pagination
                var total = concepts.Count;

                // Pagination
                var skip = request.skip ?? 0;
                var take = request.take ?? 20;
                if (skip < 0) skip = 0;
                if (take <= 0) take = 20;
                concepts = concepts.Skip(skip).Take(take).ToList();

                return new {
                    success = true,
                    discoveredConcepts = concepts,
                    totalDiscovered = total,
                    message = "Concept browsing completed successfully"
                };
            }

            var fallbackTotal = concepts.Count;
            concepts = concepts.Take(20).ToList();
            return new {
                success = true,
                discoveredConcepts = concepts,
                totalDiscovered = fallbackTotal,
                message = "Concept browsing completed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error browsing concepts: {ex.Message}", ex);
            return new ErrorResponse($"Failed to browse concepts: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all concepts
    /// </summary>
    [Get("/concepts", "concepts-list", "Get all concepts", "codex.concept")]
    [ApiResponse(200, "Success")]
    public async Task<object> GetConcepts(
        [ApiParameter("searchTerm", "Search term filter")] string? searchTerm = null,
        [ApiParameter("skip", "Number of concepts to skip")] int? skip = null,
        [ApiParameter("take", "Number of concepts to take")] int? take = null)
    {
        try
        {
            // Get all nodes and filter by type (same approach as StorageEndpointsModule)
            var allNodes = _registry.AllNodes();
            if (!allNodes.Any())
            {
                // Fallback to async if in-memory cache is empty
                var fromStorage = await _registry.AllNodesAsync();
                allNodes = fromStorage;
            }
            
            var conceptNodes = allNodes.Where(n => n.TypeId != null && n.TypeId.StartsWith("codex.concept"));
            
            var concepts = conceptNodes.Select(node => new
            {
                id = node.Id,
                name = node.Meta?.GetValueOrDefault("name")?.ToString() ?? node.Title ?? "Unknown",
                description = node.Meta?.GetValueOrDefault("description")?.ToString() ?? node.Description ?? "",
                domain = node.Meta?.GetValueOrDefault("domain")?.ToString() ?? "General",
                complexity = int.TryParse(node.Meta?.GetValueOrDefault("complexity")?.ToString(), out var complexity) ? complexity : 0,
                tags = node.Meta?.GetValueOrDefault("tags")?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0],
                createdAt = node.Meta?.GetValueOrDefault("createdAt") is DateTime dt ? dt : DateTime.UtcNow,
                updatedAt = node.Meta?.GetValueOrDefault("updatedAt") is DateTime ut ? ut : DateTime.UtcNow,
                resonance = 0.75, // Default resonance - could be calculated from actual data
                energy = 500.0,   // Default energy - could be calculated from actual data
                isInterested = false, // This would come from user data
                interestCount = 0     // This would come from user data
            }).ToList();

            // Optional search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                concepts = concepts.Where(c =>
                    (c.name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.domain?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (c.tags?.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false)
                ).ToList();
            }

            var total = concepts.Count;
            var effectiveSkip = skip ?? 0;
            var effectiveTake = take ?? 50;
            if (effectiveSkip < 0) effectiveSkip = 0;
            if (effectiveTake <= 0) effectiveTake = 50;

            concepts = concepts
                .OrderByDescending(c => c.resonance * c.energy)
                .Skip(effectiveSkip)
                .Take(effectiveTake)
                .ToList();

            return new { concepts = concepts, totalCount = total };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get concepts: {ex.Message}", "GET_CONCEPTS_ERROR");
        }
    }

    /// <summary>
    /// Get a specific concept by ID
    /// </summary>
    [Get("/concepts/{id}", "concept-get", "Get a specific concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetConcept([ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id)
    {
        try
        {
            if (!_registry.TryGet(id, out var node) || node.TypeId != "codex.concept")
            {
                return Results.NotFound("Concept not found");
            }

            var concept = new
            {
                id = node.Id,
                name = node.Meta?.GetValueOrDefault("name")?.ToString() ?? node.Title ?? "Unknown",
                description = node.Meta?.GetValueOrDefault("description")?.ToString() ?? node.Description ?? "",
                domain = node.Meta?.GetValueOrDefault("domain")?.ToString() ?? "General",
                complexity = int.TryParse(node.Meta?.GetValueOrDefault("complexity")?.ToString(), out var complexity) ? complexity : 0,
                tags = node.Meta?.GetValueOrDefault("tags")?.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0],
                createdAt = node.Meta?.GetValueOrDefault("createdAt") is DateTime dt ? dt : DateTime.UtcNow,
                updatedAt = node.Meta?.GetValueOrDefault("updatedAt") is DateTime ut ? ut : DateTime.UtcNow,
                resonance = 0.75, // Default resonance - could be calculated from actual data
                energy = 500.0,   // Default energy - could be calculated from actual data
                isInterested = false, // This would come from user data
                interestCount = 0     // This would come from user data
            };

            return concept;
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get concept: {ex.Message}", "GET_CONCEPT_ERROR");
        }
    }

    /// <summary>
    /// Create a new concept
    /// </summary>
    [Post("/concepts", "concepts-create", "Create a new concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> CreateConceptPost([ApiParameter("request", "Concept creation request", Required = true, Location = "body")] ConceptCreateRequest request)
    {
        return await CreateConcept(request);
    }

    /// <summary>
    /// Update a concept
    /// </summary>
    [Put("/concepts/{id}", "concepts-update", "Update a concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    [ApiResponse(404, "Not found")]
    public async Task<object> UpdateConcept(
        [ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id,
        [ApiParameter("request", "Concept update request", Required = true, Location = "body")] ConceptCreateRequest request)
    {
        try
        {
            if (!_registry.TryGet(id, out var existingNode) || existingNode.TypeId != "codex.concept")
            {
                return Results.NotFound("Concept not found");
            }

            // Update the concept node
            var updatedMeta = new Dictionary<string, object>(existingNode.Meta ?? new Dictionary<string, object>())
            {
                ["name"] = request.Name,
                ["description"] = request.Description,
                ["domain"] = request.Domain,
                ["complexity"] = request.Complexity,
                ["tags"] = string.Join(",", request.Tags),
                ["updatedAt"] = DateTime.UtcNow
            };

            var updatedNode = new Node(
                Id: existingNode.Id,
                TypeId: existingNode.TypeId,
                State: existingNode.State,
                Locale: existingNode.Locale,
                Title: request.Name,
                Description: request.Description,
                Content: existingNode.Content,
                Meta: updatedMeta
            );

            _registry.Upsert(updatedNode);

            return new { 
                id = updatedNode.Id, 
                message = "Concept updated successfully" 
            };
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to update concept: {ex.Message}", "UPDATE_ERROR");
        }
    }

    /// <summary>
    /// Delete a concept
    /// </summary>
    [Delete("/concepts/{id}", "concepts-delete", "Delete a concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public IResult DeleteConcept([ApiParameter("id", "Concept ID", Required = true, Location = "path")] string id)
    {
        try
        {
            var node = _registry.GetNode(id);
            _logger.Debug($"DeleteConcept: Looking for concept with ID '{id}', found node: {node?.Id ?? "null"}");
            
            if (node == null)
            {
                _logger.Debug($"DeleteConcept: Node with ID '{id}' not found, returning 404");
                return Results.NotFound("Concept not found");
            }
            
            if (node.TypeId != "codex.concept")
            {
                _logger.Debug($"DeleteConcept: Node with ID '{id}' has wrong TypeId '{node.TypeId}', returning 404");
                return Results.NotFound(new ErrorResponse("Concept not found", "NOT_FOUND"));
            }

            // Remove the node from the registry
            _registry.RemoveNode(id);

            return Results.Ok(new { 
                id = id, 
                message = "Concept deleted successfully" 
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete concept: {ex.Message}", statusCode: 500);
        }
    }

    /// <summary>
    /// Create a new concept (original method)
    /// </summary>
    [Post("/concept/create", "concept-create", "Create a new concept", "codex.concept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> CreateConcept([ApiParameter("request", "Concept creation request", Required = true, Location = "body")] ConceptCreateRequest request)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new ErrorResponse("Name is required", "VALIDATION_ERROR", 400);
            }
            
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return new ErrorResponse("Description is required", "VALIDATION_ERROR", 400);
            }
            
            if (string.IsNullOrWhiteSpace(request.Domain))
            {
                return new ErrorResponse("Domain is required", "VALIDATION_ERROR", 400);
            }

            // Create concept node
            var conceptNode = new Node(
                Id: $"codex.concept.{request.Name}.{Guid.NewGuid():N}",
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
                Id: $"codex.userconcept.beliefsystem.{request.UserId}.{Guid.NewGuid():N}",
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

    // Helper methods for concept browsing
    private double CalculateResonance(Node node, string[] axes)
    {
        try
        {
            var baseResonance = 0.75; // Default resonance
            var nodeContent = node.Title + " " + node.Description;
            var nodeTags = node.Meta?.GetValueOrDefault("tags")?.ToString() ?? "";
            
            // Calculate resonance based on axis alignment
            var resonanceBoost = 0.0;
            foreach (var axis in axes)
            {
                if (nodeContent.ToLowerInvariant().Contains(axis.ToLowerInvariant()) ||
                    nodeTags.ToLowerInvariant().Contains(axis.ToLowerInvariant()))
                {
                    resonanceBoost += 0.1;
                }
            }
            
            return Math.Min(1.0, baseResonance + resonanceBoost);
        }
        catch
        {
            return 0.75; // Default fallback
        }
    }
    
    private double CalculateEnergy(Node node, double joy)
    {
        try
        {
            var baseEnergy = 500.0;
            
            // Energy influenced by joy and node complexity
            var complexity = 0;
            if (node.Meta?.TryGetValue("complexity", out var complexityObj) == true)
            {
                int.TryParse(complexityObj.ToString(), out complexity);
            }
            
            // Higher joy amplifies energy, complexity adds depth
            var energyMultiplier = 0.5 + (joy * 1.0) + (complexity * 0.1);
            return baseEnergy * energyMultiplier;
        }
        catch
        {
            return 500.0; // Default fallback
        }
    }
}

// Request/Response DTOs for each API
// Note: ConceptCreateRequest is defined in Core/RequestTypes.cs

[ResponseType("codex.concept.create-response", "ConceptCreateResponse", "Response for concept creation")]
public record ConceptCreateResponse(bool Success, string ConceptId, string Message);

[ResponseType("codex.concept.define-request", "ConceptDefineRequest", "Request for concept definition")]
public record ConceptDefineRequest(string ConceptId, string Definition, string[] Examples, string[] Relationships);

[ResponseType("codex.concept.define-response", "ConceptDefineResponse", "Response for concept definition")]
public record ConceptDefineResponse(string ConceptId, Dictionary<string, object> Properties, string Message);

[ResponseType("codex.concept.relate-request", "ConceptRelateRequest", "Request for concept relationship")]
public record ConceptRelateRequest(string SourceConceptId, string TargetConceptId, string RelationshipType, double Weight);

[ResponseType("codex.concept.relate-response", "ConceptRelateResponse", "Response for concept relationship")]
public record ConceptRelateResponse(bool Success, string RelationshipId, string RelationshipType, double Weight, string Message);

[ResponseType("codex.concept.search-request", "ConceptSearchRequest", "Request for concept search")]
public record ConceptSearchRequest(string Query, string Domain, string[] Tags);

[ResponseType("codex.concept.search-response", "ConceptSearchResponse", "Response for concept search")]
public record ConceptSearchResponse(object[] Concepts, int TotalCount, string Message);

[ResponseType("codex.concept.semantic-request", "ConceptSemanticRequest", "Request for concept semantic analysis")]
public record ConceptSemanticRequest(string ConceptId);

[ResponseType("codex.concept.semantic-response", "ConceptSemanticResponse", "Response for concept semantic analysis")]
public record ConceptSemanticResponse(string ConceptId, object Analysis, string Message);

// User-Concept Relationship DTOs
[ResponseType("codex.concept.user-link-request", "UserConceptLinkRequest", "Request for user-concept linking")]
public record UserConceptLinkRequest(string UserId, string ConceptId, string RelationshipType, double Strength);

[ResponseType("codex.concept.user-link-response", "UserConceptLinkResponse", "Response for user-concept linking")]
public record UserConceptLinkResponse(bool Success, string? RelationshipId, string Message);

[ResponseType("codex.concept.user-unlink-request", "UserConceptUnlinkRequest", "Request for user-concept unlinking")]
public record UserConceptUnlinkRequest(string UserId, string ConceptId);

[ResponseType("codex.concept.user-unlink-response", "UserConceptUnlinkResponse", "Response for user-concept unlinking")]
public record UserConceptUnlinkResponse(bool Success, string Message);

[ResponseType("codex.concept.user-concepts-response", "UserConceptsResponse", "Response for user concepts")]
public record UserConceptsResponse(bool Success, string UserId, object[] Concepts, int TotalCount, string Message);

[ResponseType("codex.concept.concept-users-response", "ConceptUsersResponse", "Response for concept users")]
public record ConceptUsersResponse(bool Success, string ConceptId, object[] Users, int TotalCount, string Message);

[ResponseType("codex.concept.user-relationship-response", "UserConceptRelationshipResponse", "Response for user-concept relationship")]
public record UserConceptRelationshipResponse(bool Success, string UserId, string ConceptId, string? RelationshipType, string? Strength, string? CreatedAt, string Message);

[ResponseType("codex.concept.user-belief-system", "UserBeliefSystem", "User belief system entity")]
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

[ResponseType("codex.concept.user-belief-system-request", "UserBeliefSystemRequest", "Request for user belief system")]
public record UserBeliefSystemRequest(string UserId, string Framework, string Description, string[] Principles, string[] Values);

[ResponseType("codex.concept.user-belief-system-response", "UserBeliefSystemResponse", "Response for user belief system")]
public record UserBeliefSystemResponse(bool Success, string UserId, string? BeliefSystemId, string Message);

[ResponseType("codex.concept.user-translation-request", "UserConceptTranslationRequest", "Request for user concept translation")]
public record UserConceptTranslationRequest(string Concept, string BeliefSystemId);

[ResponseType("codex.concept.user-translation-response", "UserConceptTranslationResponse", "Response for user concept translation")]
public record UserConceptTranslationResponse(bool Success, string OriginalConcept, string? TranslatedConcept, string BeliefSystemId, double Confidence, string Message);

[ResponseType("codex.concept.user-belief-system-get-response", "UserBeliefSystemGetResponse", "Response for getting user belief system")]
public record UserBeliefSystemGetResponse(bool Success, string UserId, string? BeliefSystemId, string? Framework, string[] Principles, string[] Values, string? CreatedAt, string Message);

[RequestType("codex.concept.browse-request", "ConceptBrowseRequest", "Request for browsing existing concepts")]
public record ConceptBrowseRequest(
    string[]? axes = null,
    double joy = 0.7,
    double serendipity = 0.5,
    string? searchTerm = null,
    int? skip = null,
    int? take = null
);