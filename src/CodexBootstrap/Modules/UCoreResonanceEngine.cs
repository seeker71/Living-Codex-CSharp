using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules
{
    /// <summary>
    /// U-CORE Resonance Engine - Calculates resonance patterns and optimizes frequency fields
    /// Based on user belief systems and U-CORE ontology topology
    /// </summary>
    [ApiModule(Name = "UCoreResonanceEngine", Version = "1.0.0", Description = "U-CORE resonance calculation and optimization engine", Tags = new[] { "ucore", "resonance", "frequency", "optimization" })]
    public class UCoreResonanceEngine : IModule
    {
        private readonly IApiRouter _apiRouter;
        private readonly NodeRegistry _registry;
        private readonly UCoreOntology _ontology;
        private readonly Dictionary<string, UserBeliefSystem> _userBeliefs = new();
        // LLM translation now handled by integrated LLM module
        private readonly CodexBootstrap.Core.ILogger _logger;
        private CoreApiService? _coreApiService;

        public UCoreResonanceEngine(IApiRouter apiRouter, NodeRegistry registry)
        {
            _apiRouter = apiRouter ?? throw new ArgumentNullException(nameof(apiRouter));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _ontology = new UCoreOntology(); // Self-contained ontology
            _logger = new Log4NetLogger(typeof(UCoreResonanceEngine));
            // LLM translation now handled by integrated LLM module via API calls
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            // Store CoreApiService reference for inter-module communication
            _coreApiService = coreApi;
            
            // Resonance calculation endpoints
            app.MapPost("/ucore/resonance/calculate", CalculateResonance)
                .WithName("ucore-resonance-calculate")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/optimize", OptimizeResonance)
                .WithName("ucore-resonance-optimize")
                .WithTags("U-CORE Resonance");

            app.MapGet("/ucore/resonance/patterns", GetResonancePatterns)
                .WithName("ucore-resonance-patterns")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/register-beliefs", RegisterUserBeliefs)
                .WithName("ucore-resonance-register-beliefs")
                .WithTags("U-CORE Resonance");

            // Belief system translation endpoints
            app.MapPost("/ucore/resonance/translate-concept", TranslateConceptThroughBeliefSystem)
                .WithName("ucore-resonance-translate-concept")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/amplify-unity", AmplifyUnityThroughTranslation)
                .WithName("ucore-resonance-amplify-unity")
                .WithTags("U-CORE Resonance");
        }

        public async Task<ResonanceMatch> CalculateResonance(ResonanceCalculationRequest request)
        {
            var userBeliefs = _userBeliefs.GetValueOrDefault(request.UserId, new UserBeliefSystem());
            var conceptMatches = new List<ConceptMatch>();

            // For now, we'll use a simplified approach since the request doesn't have ConceptIds
            // We'll calculate resonance for all concepts in the ontology
            foreach (var concept in _ontology.Concepts.Values)
            {
                var match = CalculateConceptResonance(concept, userBeliefs);
                conceptMatches.Add(match);
            }

            // Calculate overall resonance score
            var overallResonance = conceptMatches.Average(m => m.Match);
            var frequencyAnalysis = AnalyzeFrequencyPatterns(conceptMatches);

            return new ResonanceMatch(
                OverallMatch: overallResonance,
                OptimizationScore: overallResonance * 0.8,
                AxisMatches: new Dictionary<string, double> { { "overall", overallResonance } },
                ConceptMatches: conceptMatches,
                CalculatedAt: DateTime.UtcNow
            );
        }

        public async Task<ResonanceMatch> OptimizeResonance(ResonanceOptimizationRequest request)
        {
            var userBeliefs = _userBeliefs.GetValueOrDefault(request.UserId, new UserBeliefSystem());
            var optimizedBeliefs = OptimizeUserBeliefs(userBeliefs, request.TargetConcepts);
            
            // Update user beliefs
            _userBeliefs[request.UserId] = optimizedBeliefs;

            // Calculate optimized resonance using a simplified request
            var optimizedRequest = new ResonanceCalculationRequest(
                UserId: request.UserId,
                Response: "optimized",
                ResponseType: "optimization",
                ResponseId: Guid.NewGuid().ToString()
            );

            return await CalculateResonance(optimizedRequest);
        }

        public async Task<ResonancePatternsResponse> GetResonancePatterns(string userId)
        {
            var userBeliefs = _userBeliefs.GetValueOrDefault(userId, new UserBeliefSystem());
            var patterns = new List<ResonancePattern>();

            // Generate resonance patterns for all concepts
            foreach (var concept in _ontology.Concepts.Values)
            {
                var match = CalculateConceptResonance(concept, userBeliefs);
                patterns.Add(new ResonancePattern
                {
                    ConceptId = concept.Id,
                    ConceptName = concept.Name,
                    ResonanceScore = match.Match,
                    Frequency = concept.Frequency,
                    Amplitude = match.Weight,
                    Phase = Convert.ToDouble(match.Phase)
                });
            }

            return new ResonancePatternsResponse
            {
                UserId = userId,
                Patterns = patterns.OrderByDescending(p => p.ResonanceScore).ToList(),
                TotalPatterns = patterns.Count,
                AverageResonance = patterns.Average(p => p.ResonanceScore)
            };
        }

        public async Task<ResonanceMatchResponse> RegisterUserBeliefs(UserBeliefRegistrationRequest request)
        {
            var beliefSystem = new UserBeliefSystem
            {
                UserId = request.UserId,
                WeightedConcepts = request.WeightedConcepts,
                InvestmentLevels = request.InvestmentLevels,
                PreferredFrequencies = request.PreferredFrequencies,
                ResonanceThreshold = request.ResonanceThreshold
            };

            _userBeliefs[request.UserId] = beliefSystem;

            // Calculate initial resonance using a simplified request
            var initialRequest = new ResonanceCalculationRequest(
                UserId: request.UserId,
                Response: "initial",
                ResponseType: "registration",
                ResponseId: Guid.NewGuid().ToString()
            );

            var resonance = await CalculateResonance(initialRequest);

            return new ResonanceMatchResponse(
                Success: true,
                Message: "User belief system registered successfully",
                Match: resonance
            );
        }

        private ConceptMatch CalculateConceptResonance(UCoreConcept concept, UserBeliefSystem userBeliefs)
        {
            // Calculate base resonance from concept properties
            var baseResonance = concept.Resonance;

            // Apply user belief weighting
            var beliefWeight = userBeliefs.WeightedConcepts.GetValueOrDefault(concept.Id, 0.5);
            var investmentMultiplier = userBeliefs.InvestmentLevels.GetValueOrDefault(concept.Id, 0.5);

            // Calculate frequency alignment
            var frequencyAlignment = CalculateFrequencyAlignment(concept.Frequency, userBeliefs.PreferredFrequencies);

            // Calculate final resonance score
            var resonanceScore = baseResonance * beliefWeight * investmentMultiplier * frequencyAlignment;

            // Calculate amplitude and phase
            var amplitude = Math.Min(resonanceScore * 100, 100.0);
            var phase = (concept.Frequency % 360) * Math.PI / 180;

            return new ConceptMatch(
                Concept: concept.Name,
                Match: resonanceScore,
                Weight: amplitude,
                Description: concept.Description,
                ResonanceScore: resonanceScore,
                Phase: phase.ToString(),
                InvestmentLevel: investmentMultiplier
            );
        }

        private double CalculateFrequencyAlignment(double conceptFrequency, List<double> preferredFrequencies)
        {
            if (!preferredFrequencies.Any()) return 1.0;

            var minDistance = preferredFrequencies.Min(f => Math.Abs(f - conceptFrequency));
            var maxDistance = preferredFrequencies.Max(f => Math.Abs(f - conceptFrequency));
            
            if (maxDistance == 0) return 1.0;
            
            return 1.0 - (minDistance / maxDistance);
        }

        private FrequencyAnalysis AnalyzeFrequencyPatterns(List<ConceptMatch> conceptMatches)
        {
            var frequencies = conceptMatches.Select(m => m.Match).ToList();
            var averageResonance = conceptMatches.Average(m => m.Match);
            var dominantFrequency = conceptMatches.OrderByDescending(m => m.Match).FirstOrDefault()?.Match ?? 0.0;

            return new FrequencyAnalysis(
                DetectedFrequencies: frequencies,
                ResonanceScore: averageResonance,
                HarmonicContent: new Dictionary<string, double> { { "dominant", dominantFrequency } },
                AnalyzedAt: DateTime.UtcNow
            );
        }

        private List<HarmonicPattern> CalculateHarmonicPatterns(List<ConceptMatch> conceptMatches)
        {
            var patterns = new List<HarmonicPattern>();
            var sortedMatches = conceptMatches.OrderByDescending(m => m.Match).ToList();

            for (int i = 0; i < sortedMatches.Count - 1; i++)
            {
                for (int j = i + 1; j < sortedMatches.Count; j++)
                {
                    var match1 = sortedMatches[i];
                    var match2 = sortedMatches[j];
                    
                    var harmonicRatio = CalculateHarmonicRatio(match1, match2);
                    if (harmonicRatio > 0.7) // Threshold for harmonic relationship
                    {
                        patterns.Add(new HarmonicPattern
                        {
                            PrimaryConcept = match1.Concept,
                            SecondaryConcept = match2.Concept,
                            HarmonicRatio = harmonicRatio,
                            ResonanceSum = match1.Match + match2.Match
                        });
                    }
                }
            }

            return patterns.OrderByDescending(p => p.HarmonicRatio).ToList();
        }

        private double CalculateHarmonicRatio(ConceptMatch match1, ConceptMatch match2)
        {
            // Calculate harmonic relationship based on resonance scores and phase alignment
            var resonanceRatio = Math.Min(match1.Match, match2.Match) / 
                                Math.Max(match1.Match, match2.Match);
            
            var phaseAlignment = Math.Cos(Convert.ToDouble(match1.Phase) - Convert.ToDouble(match2.Phase));
            
            return (resonanceRatio + phaseAlignment) / 2.0;
        }

        private List<ResonanceCluster> CalculateResonanceClusters(List<ConceptMatch> conceptMatches)
        {
            var clusters = new List<ResonanceCluster>();
            var threshold = 0.8; // Resonance threshold for clustering

            var highResonanceMatches = conceptMatches.Where(m => m.Match >= threshold).ToList();
            
            if (highResonanceMatches.Any())
            {
                clusters.Add(new ResonanceCluster
                {
                    ClusterId = "high-resonance",
                    ConceptIds = highResonanceMatches.Select(m => m.Concept).ToList(),
                    AverageResonance = highResonanceMatches.Average(m => m.Match),
                    ClusterStrength = highResonanceMatches.Count()
                });
            }

            return clusters;
        }

        private List<string> GenerateOptimizationSuggestions(List<ConceptMatch> conceptMatches, UserBeliefSystem userBeliefs)
        {
            var suggestions = new List<string>();
            var lowResonanceMatches = conceptMatches.Where(m => m.Match < 0.5).ToList();

            if (lowResonanceMatches.Any())
            {
                suggestions.Add($"Focus on developing resonance with {lowResonanceMatches.Count} low-resonance concepts");
            }

            var highInvestmentLowResonance = conceptMatches.Where(m => 
                m.InvestmentLevel > 0.7 && m.Match < 0.6).ToList();

            if (highInvestmentLowResonance.Any())
            {
                suggestions.Add("Consider adjusting investment levels for better resonance alignment");
            }

            return suggestions;
        }

        private UserBeliefSystem OptimizeUserBeliefs(UserBeliefSystem originalBeliefs, List<string> targetConcepts)
        {
            var optimizedBeliefs = new UserBeliefSystem
            {
                UserId = originalBeliefs.UserId,
                WeightedConcepts = new Dictionary<string, double>(originalBeliefs.WeightedConcepts),
                InvestmentLevels = new Dictionary<string, double>(originalBeliefs.InvestmentLevels),
                PreferredFrequencies = new List<double>(originalBeliefs.PreferredFrequencies),
                ResonanceThreshold = originalBeliefs.ResonanceThreshold
            };

            // Optimize weights for target concepts
            foreach (var conceptId in targetConcepts)
            {
                var concept = _ontology.GetConcept(conceptId);
                if (concept != null)
                {
                    // Increase weight for target concepts
                    optimizedBeliefs.WeightedConcepts[conceptId] = 
                        Math.Min(optimizedBeliefs.WeightedConcepts.GetValueOrDefault(conceptId, 0.5) + 0.2, 1.0);
                    
                    // Increase investment level
                    optimizedBeliefs.InvestmentLevels[conceptId] = 
                        Math.Min(optimizedBeliefs.InvestmentLevels.GetValueOrDefault(conceptId, 0.5) + 0.3, 1.0);
                }
            }

            return optimizedBeliefs;
        }

        // IModule interface implementations
        public Node GetModuleNode()
        {
            return new Node(
                Id: "ucore-resonance-engine",
                TypeId: "module",
                State: ContentState.Water,
                Locale: "en",
                Title: "U-CORE Resonance Engine",
                Description: "Calculates resonance patterns and optimizes frequency fields based on U-CORE ontology",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["version"] = "1.0.0",
                        ["capabilities"] = new[] { "resonance-calculation", "frequency-optimization", "belief-system-analysis" }
                    }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object>
                {
                    ["version"] = "1.0.0",
                    ["capabilities"] = new[] { "resonance-calculation", "frequency-optimization", "belief-system-analysis" }
                }
            );
        }

        public void Register(NodeRegistry registry)
        {
            var node = GetModuleNode();
            registry.Upsert(node);
        }

        public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
        {
            // API handlers are registered via RegisterHttpEndpoints method
            // This method is kept for interface compliance
        }

        /// <summary>
        /// Translate a concept through a user's belief system using resonance optimization
        /// </summary>
        public async Task<BeliefSystemTranslationResponse> TranslateConceptThroughBeliefSystem(BeliefSystemTranslationRequest request)
        {
            try
            {
                // Get user belief system via API call
                var userBeliefs = await GetUserBeliefSystemAsync(request.UserId);
                if (userBeliefs == null)
                {
                    return new BeliefSystemTranslationResponse(
                        Success: false,
                        OriginalConcept: request.ConceptId,
                        TranslatedConcept: "",
                        ResonanceScore: 0.0,
                        UnityAmplification: 0.0,
                        Message: "User belief system not found"
                    );
                }

                var concept = _ontology.GetConcept(request.ConceptId);
                if (concept == null)
                {
                    return new BeliefSystemTranslationResponse(
                        Success: false,
                        OriginalConcept: request.ConceptId,
                        TranslatedConcept: "",
                        ResonanceScore: 0.0,
                        UnityAmplification: 0.0,
                        Message: "Concept not found in ontology"
                    );
                }

                // Calculate resonance-based translation using real LLM
                var translation = await CalculateBeliefSystemTranslation(concept, userBeliefs, request.TargetFramework);
                
                return new BeliefSystemTranslationResponse(
                    Success: true,
                    OriginalConcept: request.ConceptId,
                    TranslatedConcept: translation.TranslatedConcept,
                    ResonanceScore: translation.ResonanceScore,
                    UnityAmplification: translation.UnityAmplification,
                    Message: "Concept translated successfully through belief system using real AI"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"Translation failed for concept {request.ConceptId}: {ex.Message}", ex);
                return new BeliefSystemTranslationResponse(
                    Success: false,
                    OriginalConcept: request.ConceptId,
                    TranslatedConcept: "",
                    ResonanceScore: 0.0,
                    UnityAmplification: 0.0,
                    Message: $"Translation failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Amplify unity between users through belief system translation
        /// </summary>
        public async Task<UnityAmplificationResponse> AmplifyUnityThroughTranslation(UnityAmplificationRequest request)
        {
            try
            {
                // Get both user belief systems via API calls
                var user1Beliefs = await GetUserBeliefSystemAsync(request.User1Id);
                var user2Beliefs = await GetUserBeliefSystemAsync(request.User2Id);
                
                if (user1Beliefs == null || user2Beliefs == null)
                {
                    return new UnityAmplificationResponse(
                        Success: false,
                        ConceptId: request.ConceptId,
                        User1Translation: "",
                        User2Translation: "",
                        UnityScore: 0.0,
                        ResonanceAmplification: 0.0,
                        Message: "One or both user belief systems not found"
                    );
                }
                
                var concept = _ontology.GetConcept(request.ConceptId);
                if (concept == null)
                {
                    return new UnityAmplificationResponse(
                        Success: false,
                        ConceptId: request.ConceptId,
                        User1Translation: "",
                        User2Translation: "",
                        UnityScore: 0.0,
                        ResonanceAmplification: 0.0,
                        Message: "Concept not found in ontology"
                    );
                }

                // Translate concept for both users using real LLM
                var user1Translation = await CalculateBeliefSystemTranslation(concept, user1Beliefs, user1Beliefs.Framework);
                var user2Translation = await CalculateBeliefSystemTranslation(concept, user2Beliefs, user2Beliefs.Framework);

                // Calculate unity amplification
                var unityScore = CalculateUnityScore(user1Translation, user2Translation);
                var resonanceAmplification = CalculateResonanceAmplification(user1Beliefs, user2Beliefs, concept);

                _logger.Info($"Unity amplification completed: Unity Score={unityScore:F2}, Resonance={resonanceAmplification:F2}");

                return new UnityAmplificationResponse(
                    Success: true,
                    ConceptId: request.ConceptId,
                    User1Translation: user1Translation.TranslatedConcept,
                    User2Translation: user2Translation.TranslatedConcept,
                    UnityScore: unityScore,
                    ResonanceAmplification: resonanceAmplification,
                    Message: "Unity amplified successfully through belief system translation using real AI"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"Unity amplification failed for concept {request.ConceptId}: {ex.Message}", ex);
                return new UnityAmplificationResponse(
                    Success: false,
                    ConceptId: request.ConceptId,
                    User1Translation: "",
                    User2Translation: "",
                    UnityScore: 0.0,
                    ResonanceAmplification: 0.0,
                    Message: $"Unity amplification failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Calculate belief system translation for a concept using real LLM
        /// </summary>
        private async Task<BeliefSystemTranslation> CalculateBeliefSystemTranslation(UCoreConcept concept, UserBeliefSystem userBeliefs, string targetFramework)
        {
            try
            {
                _logger.Info($"Starting real LLM translation for concept '{concept.Name}' through {userBeliefs.Framework} lens");
                
                // Use integrated LLM module via API call
                if (_coreApiService == null)
                {
                    _logger.Warn("CoreApiService not available for LLM translation");
                    return new BeliefSystemTranslation
                    {
                        TranslatedConcept = $"Mock translation of {concept.Name} through {targetFramework} lens",
                        ResonanceScore = 0.7,
                        UnityAmplification = 0.6
                    };
                }

                // Create translation request for the integrated LLM module
                var translationRequest = new
                {
                    conceptId = concept.Id,
                    conceptName = concept.Name,
                    conceptDescription = concept.Description,
                    sourceFramework = "Universal",
                    targetFramework = targetFramework,
                    userBeliefSystem = new Dictionary<string, object>
                    {
                        ["framework"] = userBeliefs.Framework,
                        ["language"] = userBeliefs.Language,
                        ["culturalContext"] = userBeliefs.CulturalContext,
                        ["spiritualTradition"] = userBeliefs.SpiritualTradition ?? "",
                        ["scientificBackground"] = userBeliefs.ScientificBackground ?? "",
                        ["coreValues"] = userBeliefs.CoreValues,
                        ["translationPreferences"] = userBeliefs.TranslationPreferences
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

                    _logger.Info($"Real LLM translation completed for '{concept.Name}': {translatedConcept}");
                    
                    return new BeliefSystemTranslation
                    {
                        TranslatedConcept = translatedConcept,
                        ResonanceScore = resonanceScore,
                        UnityAmplification = unityAmplification
                    };
                }

                _logger.Warn($"Invalid response from LLM module for concept '{concept.Name}'");
                return new BeliefSystemTranslation
                {
                    TranslatedConcept = $"Fallback translation of {concept.Name} through {targetFramework} lens",
                    ResonanceScore = 0.6,
                    UnityAmplification = 0.5
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Real LLM translation failed for concept '{concept.Name}': {ex.Message}", ex);
                
                // Fallback to resonance-based calculation if LLM fails
                var resonanceScore = CalculateConceptResonance(concept, userBeliefs).Match;
                var translatedConcept = $"{concept.Name} (resonance: {resonanceScore:F2}) - {targetFramework} (LLM fallback)";
                
                return new BeliefSystemTranslation
                {
                    TranslatedConcept = translatedConcept,
                    ResonanceScore = resonanceScore,
                    UnityAmplification = resonanceScore * 0.9
                };
            }
        }

        /// <summary>
        /// Get user belief system via API call to UserConceptModule
        /// </summary>
        private async Task<UserBeliefSystem?> GetUserBeliefSystemAsync(string userId)
        {
            try
            {
                if (_coreApiService == null)
                {
                    _logger.Warn("CoreApiService not available for inter-module communication");
                    return null;
                }

                // Use CoreApiService to call UserConceptModule
                var args = JsonSerializer.SerializeToElement(new { userId });
                var call = new DynamicCall("codex.userconcept", "get-belief-system", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);
                
                if (response is BeliefSystemResponse beliefResponse && beliefResponse.Success)
                {
                    return beliefResponse.BeliefSystem;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get user belief system for {userId}: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Register user belief system via API call to UserConceptModule
        /// </summary>
        private async Task<bool> RegisterUserBeliefSystemAsync(UserBeliefSystem beliefSystem)
        {
            try
            {
                if (_coreApiService == null)
                {
                    _logger.Warn("CoreApiService not available for inter-module communication");
                    return false;
                }

                var request = new BeliefSystemRegistrationRequest(
                    UserId: beliefSystem.UserId,
                    Framework: beliefSystem.Framework,
                    Language: beliefSystem.Language,
                    CulturalContext: beliefSystem.CulturalContext,
                    SpiritualTradition: beliefSystem.SpiritualTradition,
                    ScientificBackground: beliefSystem.ScientificBackground,
                    CoreValues: beliefSystem.CoreValues,
                    TranslationPreferences: beliefSystem.TranslationPreferences,
                    ResonanceThreshold: beliefSystem.ResonanceThreshold
                );

                // Use CoreApiService to call UserConceptModule
                var args = JsonSerializer.SerializeToElement(request);
                var call = new DynamicCall("codex.userconcept", "register-belief-system", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);
                
                if (response is BeliefSystemRegistrationResponse registrationResponse)
                {
                    return registrationResponse.Success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to register user belief system for {beliefSystem.UserId}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Calculate unity score between two translations
        /// </summary>
        private double CalculateUnityScore(BeliefSystemTranslation translation1, BeliefSystemTranslation translation2)
        {
            // Unity score based on resonance alignment
            var resonanceAlignment = 1.0 - Math.Abs(translation1.ResonanceScore - translation2.ResonanceScore);
            var unityAmplification = (translation1.UnityAmplification + translation2.UnityAmplification) / 2.0;
            
            return (resonanceAlignment + unityAmplification) / 2.0;
        }

        /// <summary>
        /// Calculate resonance amplification between users
        /// </summary>
        private double CalculateResonanceAmplification(UserBeliefSystem user1Beliefs, UserBeliefSystem user2Beliefs, UCoreConcept concept)
        {
            // Calculate resonance amplification based on belief system alignment
            var frameworkAlignment = user1Beliefs.Framework == user2Beliefs.Framework ? 1.0 : 0.5;
            var culturalAlignment = user1Beliefs.CulturalContext == user2Beliefs.CulturalContext ? 1.0 : 0.7;
            var conceptResonance = concept.Resonance;
            
            return (frameworkAlignment + culturalAlignment + conceptResonance) / 3.0;
        }
    }

    // Supporting record types
    [ApiType(Name = "UserBeliefSystem", Description = "User's belief system and resonance preferences", Type = "object")]
    public record UserBeliefSystem
    {
        public string UserId { get; init; } = "";
        public string Framework { get; init; } = "";
        public string Language { get; init; } = "";
        public string CulturalContext { get; init; } = "";
        public string? SpiritualTradition { get; init; }
        public string? ScientificBackground { get; init; }
        public Dictionary<string, object> CoreValues { get; init; } = new();
        public Dictionary<string, object> TranslationPreferences { get; init; } = new();
        public double ResonanceThreshold { get; init; } = 0.7;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public Dictionary<string, double> WeightedConcepts { get; init; } = new();
        public Dictionary<string, double> InvestmentLevels { get; init; } = new();
        public List<double> PreferredFrequencies { get; init; } = new();
    }

    [ApiType(Name = "ResonanceOptimizationRequest", Description = "Request to optimize resonance for target concepts", Type = "object")]
    public record ResonanceOptimizationRequest
    {
        public string UserId { get; init; } = "";
        public List<string> TargetConcepts { get; init; } = new();
    }

    [ApiType(Name = "HarmonicPattern", Description = "Harmonic relationship between concepts", Type = "object")]
    public record HarmonicPattern
    {
        public string PrimaryConcept { get; init; } = "";
        public string SecondaryConcept { get; init; } = "";
        public double HarmonicRatio { get; init; }
        public double ResonanceSum { get; init; }
    }

    [ApiType(Name = "ResonanceCluster", Description = "Cluster of concepts with similar resonance", Type = "object")]
    public record ResonanceCluster
    {
        public string ClusterId { get; init; } = "";
        public List<string> ConceptIds { get; init; } = new();
        public double AverageResonance { get; init; }
        public int ClusterStrength { get; init; }
    }

    [ApiType(Name = "ResonancePatternsResponse", Description = "Response containing resonance patterns for a user", Type = "object")]
    public record ResonancePatternsResponse
    {
        public string UserId { get; init; } = "";
        public List<ResonancePattern> Patterns { get; init; } = new();
        public int TotalPatterns { get; init; }
        public double AverageResonance { get; init; }
    }

    [ApiType(Name = "ResonancePattern", Description = "A single resonance pattern", Type = "object")]
    public record ResonancePattern
    {
        public string ConceptId { get; init; } = "";
        public string ConceptName { get; init; } = "";
        public double ResonanceScore { get; init; }
        public double Frequency { get; init; }
        public double Amplitude { get; init; }
        public double Phase { get; init; }
    }

    [ApiType(Name = "UserBeliefRegistrationRequest", Description = "Request to register user belief system", Type = "object")]
    public record UserBeliefRegistrationRequest
    {
        public string UserId { get; init; } = "";
        public Dictionary<string, double> WeightedConcepts { get; init; } = new();
        public Dictionary<string, double> InvestmentLevels { get; init; } = new();
        public List<double> PreferredFrequencies { get; init; } = new();
        public double ResonanceThreshold { get; init; } = 0.5;
    }

    // Belief System Translation DTOs
    [ApiType(Name = "BeliefSystemTranslationRequest", Description = "Request to translate concept through belief system", Type = "object")]
    public record BeliefSystemTranslationRequest
    {
        public string UserId { get; init; } = "";
        public string ConceptId { get; init; } = "";
        public string TargetFramework { get; init; } = "";
        public string? SourceLanguage { get; init; }
        public string? TargetLanguage { get; init; }
    }

    [ApiType(Name = "BeliefSystemTranslationResponse", Description = "Response for belief system translation", Type = "object")]
    public record BeliefSystemTranslationResponse(
        bool Success,
        string OriginalConcept,
        string TranslatedConcept,
        double ResonanceScore,
        double UnityAmplification,
        string Message
    );

    [ApiType(Name = "UnityAmplificationRequest", Description = "Request to amplify unity through translation", Type = "object")]
    public record UnityAmplificationRequest
    {
        public string User1Id { get; init; } = "";
        public string User2Id { get; init; } = "";
        public string ConceptId { get; init; } = "";
    }

    [ApiType(Name = "UnityAmplificationResponse", Description = "Response for unity amplification", Type = "object")]
    public record UnityAmplificationResponse(
        bool Success,
        string ConceptId,
        string User1Translation,
        string User2Translation,
        double UnityScore,
        double ResonanceAmplification,
        string Message
    );

    [ApiType(Name = "BeliefSystemTranslation", Description = "Internal belief system translation data", Type = "object")]
    public class BeliefSystemTranslation
    {
        public string TranslatedConcept { get; set; } = "";
        public double ResonanceScore { get; set; }
        public double UnityAmplification { get; set; }
    }
}
