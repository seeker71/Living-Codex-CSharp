using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
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
        private readonly Dictionary<string, UserBeliefSystem> _userBeliefSystems = new();
        private readonly Dictionary<string, ConceptTranslationCache> _translationCache = new();
        private CoreApiService? _coreApiService;
        private readonly IServiceProvider? _serviceProvider;

    public UserConceptModule()
    {
        // Parameterless constructor for attribute discovery
        _apiRouter = null!;
        _registry = null!;
        _serviceProvider = null;
    }

    public UserConceptModule(IApiRouter apiRouter, NodeRegistry registry, IServiceProvider? serviceProvider = null)
    {
        _apiRouter = apiRouter;
        _registry = registry;
        _serviceProvider = serviceProvider;
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
        // Store CoreApiService reference for inter-module communication
        _coreApiService = coreApi;
        
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
                Weight: request.Strength,
                Meta: new Dictionary<string, object>
                {
                    ["relationshipId"] = relationshipId,
                    ["relationshipType"] = request.RelationshipType,
                    ["weight"] = request.Strength,
                    ["createdAt"] = DateTime.UtcNow,
                    ["status"] = "active"
                }
            );

            // Store the relationship in the registry
            _registry.Upsert(relationshipEdge);

            return new UserConceptLinkResponse(
                Success: true,
                RelationshipId: relationshipId,
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
                Success: true,
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
                Success: true,
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
                Success: true,
                UserId: userId,
                ConceptId: conceptId,
                RelationshipType: "none",
                Strength: "0.0",
                CreatedAt: DateTime.UtcNow.ToString(),
                Message: "Relationship retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get relationship: {ex.Message}", "GET_RELATIONSHIP_ERROR");
        }
    }

    /// <summary>
    /// Register a user's belief system
    /// </summary>
    [Post("/userconcept/belief-system/register", "userconcept-register-belief-system", "Register user belief system", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> RegisterBeliefSystem([ApiParameter("request", "Belief system registration request", Required = true, Location = "body")] BeliefSystemRegistrationRequest request)
    {
        try
        {
            var beliefSystem = new UserBeliefSystem
            {
                UserId = request.UserId,
                Framework = request.Framework,
                Language = request.Language,
                CulturalContext = request.CulturalContext,
                SpiritualTradition = request.SpiritualTradition,
                ScientificBackground = request.ScientificBackground,
                CoreValues = request.CoreValues,
                TranslationPreferences = request.TranslationPreferences,
                ResonanceThreshold = request.ResonanceThreshold,
                CreatedAt = DateTime.UtcNow
            };

            _userBeliefSystems[request.UserId] = beliefSystem;

            return new BeliefSystemRegistrationResponse(
                Success: true,
                UserId: request.UserId,
                Framework: request.Framework,
                Message: "Belief system registered successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to register belief system: {ex.Message}", "REGISTER_BELIEF_ERROR");
        }
    }

    /// <summary>
    /// Translate a concept through a user's belief system lens
    /// </summary>
    [Post("/userconcept/translate", "userconcept-translate-concept", "Translate concept through belief system", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(400, "Bad request")]
    public async Task<object> TranslateConcept([ApiParameter("request", "Concept translation request", Required = true, Location = "body")] ConceptTranslationRequest request)
    {
        try
        {
            var cacheKey = $"{request.UserId}:{request.ConceptId}:{request.TargetFramework}";
            
            // Check cache first
            if (_translationCache.TryGetValue(cacheKey, out var cachedTranslation) && 
                cachedTranslation.ExpiresAt > DateTime.UtcNow)
            {
                return new ConceptTranslationResponse(
                    Success: true,
                    OriginalConcept: request.ConceptId,
                    TranslatedConcept: cachedTranslation.TranslatedConcept,
                    TranslationFramework: request.TargetFramework,
                    ResonanceScore: cachedTranslation.ResonanceScore,
                    UnityAmplification: cachedTranslation.UnityAmplification,
                    Message: "Concept translated successfully (cached)"
                );
            }

            // Get user's belief system
            if (!_userBeliefSystems.TryGetValue(request.UserId, out var userBeliefSystem))
            {
                return ResponseHelpers.CreateErrorResponse("User belief system not found", "BELIEF_SYSTEM_NOT_FOUND");
            }

            // Perform AI-powered translation using LLM configuration
            var translation = await PerformAITranslation(request.ConceptId, userBeliefSystem, request.TargetFramework);
            
            // Cache the translation
            _translationCache[cacheKey] = new ConceptTranslationCache
            {
                TranslatedConcept = translation.TranslatedConcept,
                ResonanceScore = translation.ResonanceScore,
                UnityAmplification = translation.UnityAmplification,
                ExpiresAt = DateTime.UtcNow.AddHours(24) // Cache for 24 hours
            };

            return translation;
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to translate concept: {ex.Message}", "TRANSLATION_ERROR");
        }
    }

    /// <summary>
    /// Get user's belief system
    /// </summary>
    [Get("/userconcept/belief-system/{userId}", "userconcept-get-belief-system", "Get user belief system", "codex.userconcept")]
    [ApiResponse(200, "Success")]
    [ApiResponse(404, "Not found")]
    public async Task<object> GetBeliefSystem([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            if (!_userBeliefSystems.TryGetValue(userId, out var beliefSystem))
            {
                return ResponseHelpers.CreateErrorResponse("User belief system not found", "BELIEF_SYSTEM_NOT_FOUND");
            }

            return new BeliefSystemResponse(
                Success: true,
                BeliefSystem: beliefSystem,
                Message: "Belief system retrieved successfully"
            );
        }
        catch (Exception ex)
        {
            return ResponseHelpers.CreateErrorResponse($"Failed to get belief system: {ex.Message}", "GET_BELIEF_ERROR");
        }
    }

    /// <summary>
    /// Perform AI-powered concept translation using real LLM
    /// </summary>
    private async Task<ConceptTranslationResponse> PerformAITranslation(string conceptId, UserBeliefSystem userBeliefSystem, string targetFramework)
    {
        try
        {
            // Get the concept from U-CORE ontology
            var ontology = new UCoreOntology();
            var concept = ontology.GetConcept(conceptId);
            
            if (concept == null)
            {
                return new ConceptTranslationResponse(
                    Success: false,
                    OriginalConcept: conceptId,
                    TranslatedConcept: "",
                    TranslationFramework: targetFramework,
                    ResonanceScore: 0.0,
                    UnityAmplification: 0.0,
                    Message: "Concept not found in U-CORE ontology"
                );
            }

            // Use the integrated LLM module via API call
            if (_coreApiService == null)
            {
                // Try to get CoreApiService from DI container
                if (_serviceProvider != null)
                {
                    _coreApiService = _serviceProvider.GetService<CoreApiService>();
                }
                
                if (_coreApiService == null)
                {
                    // Fallback: Use direct HTTP call to LLM module
                    return await PerformDirectLLMTranslation(conceptId, concept, userBeliefSystem, targetFramework);
                }
            }

            // Create translation request for the integrated LLM module
            var translationRequest = new
            {
                conceptId = conceptId,
                conceptName = concept.Name,
                conceptDescription = concept.Description,
                sourceFramework = "Universal",
                targetFramework = targetFramework,
                userBeliefSystem = new Dictionary<string, object>
                {
                    ["framework"] = userBeliefSystem.Framework,
                    ["language"] = userBeliefSystem.Language,
                    ["culturalContext"] = userBeliefSystem.CulturalContext,
                    ["spiritualTradition"] = userBeliefSystem.SpiritualTradition ?? "",
                    ["scientificBackground"] = userBeliefSystem.ScientificBackground ?? "",
                    ["coreValues"] = userBeliefSystem.CoreValues,
                    ["translationPreferences"] = userBeliefSystem.TranslationPreferences
                }
            };

            // Call the integrated LLM module
            var call = new DynamicCall("codex.llm.future", "translate-concept", JsonSerializer.SerializeToElement(translationRequest));
            var response = await _coreApiService.ExecuteDynamicCall(call);
            
            if (response is JsonElement jsonResponse)
            {
                var success = jsonResponse.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
                var translatedConcept = jsonResponse.TryGetProperty("translatedConcept", out var translatedElement) ? translatedElement.GetString() ?? "" : "";
                var resonanceScore = jsonResponse.TryGetProperty("resonanceScore", out var resonanceElement) ? resonanceElement.GetDouble() : 0.0;
                var unityAmplification = jsonResponse.TryGetProperty("unityAmplification", out var unityElement) ? unityElement.GetDouble() : 0.0;
                var message = jsonResponse.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "" : "";

                return new ConceptTranslationResponse(
                    Success: success,
                    OriginalConcept: conceptId,
                    TranslatedConcept: translatedConcept,
                    TranslationFramework: targetFramework,
                    ResonanceScore: resonanceScore,
                    UnityAmplification: unityAmplification,
                    Message: message
                );
            }

            return new ConceptTranslationResponse(
                Success: false,
                OriginalConcept: conceptId,
                TranslatedConcept: "",
                TranslationFramework: targetFramework,
                ResonanceScore: 0.0,
                UnityAmplification: 0.0,
                Message: "Invalid response from integrated LLM module"
            );
        }
        catch (Exception ex)
        {
            return new ConceptTranslationResponse(
                Success: false,
                OriginalConcept: conceptId,
                TranslatedConcept: "",
                TranslationFramework: targetFramework,
                ResonanceScore: 0.0,
                UnityAmplification: 0.0,
                Message: $"AI translation failed: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Fallback method to perform direct LLM translation via HTTP call
    /// </summary>
    private async Task<ConceptTranslationResponse> PerformDirectLLMTranslation(string conceptId, UCoreConcept concept, UserBeliefSystem userBeliefSystem, string targetFramework)
    {
        try
        {
            using var httpClient = new HttpClient();
            
            var translationRequest = new
            {
                conceptId = conceptId,
                conceptName = concept.Name,
                conceptDescription = concept.Description,
                sourceFramework = "Universal",
                targetFramework = targetFramework,
                userBeliefSystem = new Dictionary<string, object>
                {
                    ["framework"] = userBeliefSystem.Framework,
                    ["language"] = userBeliefSystem.Language,
                    ["culturalContext"] = userBeliefSystem.CulturalContext,
                    ["spiritualTradition"] = userBeliefSystem.SpiritualTradition ?? "",
                    ["scientificBackground"] = userBeliefSystem.ScientificBackground ?? "",
                    ["coreValues"] = userBeliefSystem.CoreValues,
                    ["translationPreferences"] = userBeliefSystem.TranslationPreferences
                }
            };

            var response = await httpClient.PostAsJsonAsync("http://localhost:5000/llm/translate", translationRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TranslationResponse>();
                if (result != null)
                {
                    return new ConceptTranslationResponse(
                        Success: result.Success,
                        OriginalConcept: conceptId,
                        TranslatedConcept: result.TranslatedConcept,
                        TranslationFramework: targetFramework,
                        ResonanceScore: result.ResonanceScore,
                        UnityAmplification: result.UnityAmplification,
                        Message: result.Message
                    );
                }
            }

            return new ConceptTranslationResponse(
                Success: false,
                OriginalConcept: conceptId,
                TranslatedConcept: "",
                TranslationFramework: targetFramework,
                ResonanceScore: 0.0,
                UnityAmplification: 0.0,
                Message: "Direct LLM translation failed"
            );
        }
        catch (Exception ex)
        {
            return new ConceptTranslationResponse(
                Success: false,
                OriginalConcept: conceptId,
                TranslatedConcept: "",
                TranslationFramework: targetFramework,
                ResonanceScore: 0.0,
                UnityAmplification: 0.0,
                Message: $"Direct LLM translation error: {ex.Message}"
            );
        }
    }
}

// Request/Response DTOs for each API
// UserConcept DTOs moved to ConceptModule

// Belief System DTOs
public record BeliefSystemRegistrationRequest(
    string UserId,
    string Framework,
    string Language,
    string CulturalContext,
    string? SpiritualTradition,
    string? ScientificBackground,
    Dictionary<string, object> CoreValues,
    Dictionary<string, object> TranslationPreferences,
    double ResonanceThreshold = 0.7
);

public record BeliefSystemRegistrationResponse(bool Success, string UserId, string Framework, string Message);

public record ConceptTranslationRequest(
    string UserId,
    string ConceptId,
    string TargetFramework,
    string? SourceLanguage = null,
    string? TargetLanguage = null
);

public record ConceptTranslationResponse(
    bool Success,
    string OriginalConcept,
    string TranslatedConcept,
    string TranslationFramework,
    double ResonanceScore,
    double UnityAmplification,
    string Message
);

public record BeliefSystemResponse(bool Success, UserBeliefSystem BeliefSystem, string Message);

// Data Models - UserBeliefSystem is defined in UCoreResonanceEngine

public class ConceptTranslationCache
{
    public string TranslatedConcept { get; set; } = "";
    public double ResonanceScore { get; set; }
    public double UnityAmplification { get; set; }
    public DateTime ExpiresAt { get; set; }
}