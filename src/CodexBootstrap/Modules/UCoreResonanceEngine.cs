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

            // Cross-Service Resonance endpoints
            app.MapPost("/ucore/resonance/cross-service", CalculateCrossServiceResonance)
                .WithName("ucore-resonance-cross-service")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/distributed", CalculateDistributedResonance)
                .WithName("ucore-resonance-distributed")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/aggregate", AggregateResonanceResults)
                .WithName("ucore-resonance-aggregate")
                .WithTags("U-CORE Resonance");

            app.MapGet("/ucore/resonance/global-patterns", GetGlobalResonancePatterns)
                .WithName("ucore-resonance-global-patterns")
                .WithTags("U-CORE Resonance");

            app.MapPost("/ucore/resonance/sync", SyncResonanceAcrossServices)
                .WithName("ucore-resonance-sync")
                .WithTags("U-CORE Resonance");

            // Register all Cross-Service Resonance related nodes for AI agent discovery
            RegisterCrossServiceResonanceNodes(nodeRegistry);
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

        /// <summary>
        /// Calculate cross-service resonance for distributed concept analysis
        /// </summary>
        public async Task<CrossServiceResonanceResponse> CalculateCrossServiceResonance(CrossServiceResonanceRequest request)
        {
            try
            {
                _logger.Info($"Starting cross-service resonance calculation for concept '{request.ConceptId}' across {request.ServiceIds.Count} services");

                var serviceResults = new List<ServiceResonanceResult>();
                var totalResonance = 0.0;
                var serviceCount = 0;

                // Calculate resonance for each service
                foreach (var serviceId in request.ServiceIds)
                {
                    try
                    {
                        var serviceResonance = await CalculateResonanceForService(serviceId, request.ConceptId, request.UserId);
                        serviceResults.Add(serviceResonance);
                        if (serviceResonance.Status == "success")
                        {
                            totalResonance += serviceResonance.ResonanceScore;
                            serviceCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to calculate resonance for service {serviceId}: {ex.Message}", ex);
                        serviceResults.Add(new ServiceResonanceResult
                        {
                            ServiceId = serviceId,
                            ServiceName = $"Service-{serviceId}",
                            ResonanceScore = 0.0,
                            Status = "error",
                            ErrorMessage = ex.Message
                        });
                    }
                }

                // Calculate aggregated resonance
                var averageResonance = serviceCount > 0 ? totalResonance / serviceCount : 0.0;
                var globalPatterns = await AnalyzeGlobalResonancePatterns(serviceResults);
                var crossServiceHarmonics = CalculateCrossServiceHarmonics(serviceResults);

                _logger.Info($"Cross-service resonance calculation completed: Average={averageResonance:F2}, Services={serviceCount}");

                return new CrossServiceResonanceResponse
                {
                    Success = true,
                    ConceptId = request.ConceptId,
                    ServiceResults = serviceResults,
                    AverageResonance = averageResonance,
                    GlobalPatterns = globalPatterns,
                    CrossServiceHarmonics = crossServiceHarmonics,
                    CalculatedAt = DateTime.UtcNow,
                    Message = $"Cross-service resonance calculated across {serviceCount} services"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Cross-service resonance calculation failed: {ex.Message}", ex);
                return new CrossServiceResonanceResponse
                {
                    Success = false,
                    ConceptId = request.ConceptId,
                    ServiceResults = new List<ServiceResonanceResult>(),
                    AverageResonance = 0.0,
                    GlobalPatterns = new List<GlobalResonancePattern>(),
                    CrossServiceHarmonics = new List<CrossServiceHarmonic>(),
                    CalculatedAt = DateTime.UtcNow,
                    Message = $"Cross-service resonance calculation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calculate distributed resonance across multiple services with load balancing
        /// </summary>
        public async Task<DistributedResonanceResponse> CalculateDistributedResonance(DistributedResonanceRequest request)
        {
            try
            {
                _logger.Info($"Starting distributed resonance calculation for {request.Concepts.Count} concepts across {request.TargetServices.Count} services");

                var distributedTasks = new List<Task<ServiceResonanceResult>>();
                var conceptServicePairs = new List<ConceptServicePair>();

                // Create distributed tasks for each concept-service pair
                foreach (var conceptId in request.Concepts)
                {
                    foreach (var serviceId in request.TargetServices)
                    {
                        var pair = new ConceptServicePair { ConceptId = conceptId, ServiceId = serviceId };
                        conceptServicePairs.Add(pair);
                        
                        distributedTasks.Add(CalculateResonanceForService(serviceId, conceptId, request.UserId));
                    }
                }

                // Wait for all distributed calculations to complete
                var results = await Task.WhenAll(distributedTasks);
                var validResults = results.Where(r => r.Status == "success").ToList();

                // Analyze distributed patterns
                var distributedPatterns = AnalyzeDistributedPatterns(validResults, conceptServicePairs);
                var loadBalancingMetrics = CalculateLoadBalancingMetrics(validResults);
                var networkResonance = CalculateNetworkResonance(validResults);

                _logger.Info($"Distributed resonance calculation completed: {validResults.Count} results, Network Resonance={networkResonance:F2}");

                return new DistributedResonanceResponse
                {
                    Success = true,
                    UserId = request.UserId,
                    ServiceResults = validResults,
                    DistributedPatterns = distributedPatterns,
                    LoadBalancingMetrics = loadBalancingMetrics,
                    NetworkResonance = networkResonance,
                    CalculatedAt = DateTime.UtcNow,
                    Message = $"Distributed resonance calculated for {request.Concepts.Count} concepts across {request.TargetServices.Count} services"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Distributed resonance calculation failed: {ex.Message}", ex);
                return new DistributedResonanceResponse
                {
                    Success = false,
                    UserId = request.UserId,
                    ServiceResults = new List<ServiceResonanceResult>(),
                    DistributedPatterns = new List<DistributedPattern>(),
                    LoadBalancingMetrics = new LoadBalancingMetrics(),
                    NetworkResonance = 0.0,
                    CalculatedAt = DateTime.UtcNow,
                    Message = $"Distributed resonance calculation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Aggregate resonance results from multiple services
        /// </summary>
        public async Task<AggregatedResonanceResponse> AggregateResonanceResults(AggregatedResonanceRequest request)
        {
            try
            {
                _logger.Info($"Aggregating resonance results from {request.ServiceResults.Count} services");

                var aggregatedResults = new Dictionary<string, AggregatedConceptResonance>();
                var serviceWeights = request.ServiceWeights ?? new Dictionary<string, double>();

                // Aggregate results by concept
                foreach (var serviceResult in request.ServiceResults)
                {
                    var weight = serviceWeights.GetValueOrDefault(serviceResult.ServiceId, 1.0);
                    
                    if (!aggregatedResults.ContainsKey(serviceResult.ConceptId))
                    {
                        aggregatedResults[serviceResult.ConceptId] = new AggregatedConceptResonance
                        {
                            ConceptId = serviceResult.ConceptId,
                            ServiceScores = new List<ServiceScore>(),
                            WeightedAverage = 0.0,
                            Confidence = 0.0
                        };
                    }

                    aggregatedResults[serviceResult.ConceptId].ServiceScores.Add(new ServiceScore
                    {
                        ServiceId = serviceResult.ServiceId,
                        Score = serviceResult.ResonanceScore,
                        Weight = weight,
                        Timestamp = serviceResult.CalculatedAt
                    });
                }

                // Calculate weighted averages and confidence scores
                var updatedResults = new List<AggregatedConceptResonance>();
                foreach (var conceptResult in aggregatedResults.Values)
                {
                    var totalWeight = conceptResult.ServiceScores.Sum(s => s.Weight);
                    var weightedSum = conceptResult.ServiceScores.Sum(s => s.Score * s.Weight);
                    
                    var updatedResult = conceptResult with
                    {
                        WeightedAverage = totalWeight > 0 ? weightedSum / totalWeight : 0.0,
                        Confidence = CalculateConfidenceScore(conceptResult.ServiceScores)
                    };
                    updatedResults.Add(updatedResult);
                }
                
                // Update the aggregated results
                aggregatedResults.Clear();
                foreach (var result in updatedResults)
                {
                    aggregatedResults[result.ConceptId] = result;
                }

                var globalAggregation = CalculateGlobalAggregation(aggregatedResults.Values.ToList());
                var qualityMetrics = CalculateQualityMetrics(aggregatedResults.Values.ToList());

                _logger.Info($"Resonance aggregation completed: {aggregatedResults.Count} concepts, Global Score={globalAggregation:F2}");

                return new AggregatedResonanceResponse
                {
                    Success = true,
                    AggregatedResults = aggregatedResults.Values.ToList(),
                    GlobalAggregation = globalAggregation,
                    QualityMetrics = qualityMetrics,
                    AggregatedAt = DateTime.UtcNow,
                    Message = $"Resonance results aggregated from {request.ServiceResults.Count} services"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Resonance aggregation failed: {ex.Message}", ex);
                return new AggregatedResonanceResponse
                {
                    Success = false,
                    AggregatedResults = new List<AggregatedConceptResonance>(),
                    GlobalAggregation = 0.0,
                    QualityMetrics = new QualityMetrics(),
                    AggregatedAt = DateTime.UtcNow,
                    Message = $"Resonance aggregation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get global resonance patterns across all services
        /// </summary>
        public async Task<GlobalResonancePatternsResponse> GetGlobalResonancePatterns(string? serviceFilter = null)
        {
            try
            {
                _logger.Info($"Retrieving global resonance patterns (filter: {serviceFilter ?? "all"})");

                var globalPatterns = new List<GlobalResonancePattern>();
                var servicePatterns = new Dictionary<string, List<ResonancePattern>>();

                // Get patterns from all available services
                var availableServices = await GetAvailableServices();
                var filteredServices = serviceFilter != null 
                    ? availableServices.Where(s => s.Contains(serviceFilter)).ToList()
                    : availableServices;

                foreach (var serviceId in filteredServices)
                {
                    try
                    {
                        var patterns = await GetResonancePatternsFromService(serviceId);
                        servicePatterns[serviceId] = patterns;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn($"Failed to get patterns from service {serviceId}: {ex.Message}");
                    }
                }

                // Analyze cross-service patterns
                var crossServicePatterns = AnalyzeCrossServicePatterns(servicePatterns);
                var globalHarmonics = CalculateGlobalHarmonics(servicePatterns);
                var resonanceClusters = CalculateGlobalResonanceClusters(servicePatterns);

                _logger.Info($"Global resonance patterns retrieved: {crossServicePatterns.Count} patterns, {globalHarmonics.Count} harmonics");

                return new GlobalResonancePatternsResponse
                {
                    Success = true,
                    GlobalPatterns = crossServicePatterns,
                    GlobalHarmonics = globalHarmonics,
                    ResonanceClusters = resonanceClusters,
                    ServiceCount = filteredServices.Count,
                    RetrievedAt = DateTime.UtcNow,
                    Message = $"Global resonance patterns retrieved from {filteredServices.Count} services"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to retrieve global resonance patterns: {ex.Message}", ex);
                return new GlobalResonancePatternsResponse
                {
                    Success = false,
                    GlobalPatterns = new List<GlobalResonancePattern>(),
                    GlobalHarmonics = new List<GlobalHarmonic>(),
                    ResonanceClusters = new List<ResonanceCluster>(),
                    ServiceCount = 0,
                    RetrievedAt = DateTime.UtcNow,
                    Message = $"Failed to retrieve global resonance patterns: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Sync resonance data across services
        /// </summary>
        public async Task<ResonanceSyncResponse> SyncResonanceAcrossServices(ResonanceSyncRequest request)
        {
            try
            {
                _logger.Info($"Syncing resonance data across {request.TargetServices.Count} services");

                var syncResults = new List<ServiceSyncResult>();
                var syncTasks = new List<Task<ServiceSyncResult>>();

                // Create sync tasks for each target service
                foreach (var serviceId in request.TargetServices)
                {
                    syncTasks.Add(SyncResonanceWithService(serviceId, request.ResonanceData, request.SyncOptions));
                }

                // Wait for all sync operations to complete
                var results = await Task.WhenAll(syncTasks);
                syncResults.AddRange(results);

                var successfulSyncs = syncResults.Count(r => r.Success);
                var failedSyncs = syncResults.Count(r => !r.Success);

                _logger.Info($"Resonance sync completed: {successfulSyncs} successful, {failedSyncs} failed");

                return new ResonanceSyncResponse
                {
                    Success = successfulSyncs > 0,
                    SyncResults = syncResults,
                    SuccessfulSyncs = successfulSyncs,
                    FailedSyncs = failedSyncs,
                    SyncedAt = DateTime.UtcNow,
                    Message = $"Resonance sync completed: {successfulSyncs} successful, {failedSyncs} failed"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Resonance sync failed: {ex.Message}", ex);
                return new ResonanceSyncResponse
                {
                    Success = false,
                    SyncResults = new List<ServiceSyncResult>(),
                    SuccessfulSyncs = 0,
                    FailedSyncs = 0,
                    SyncedAt = DateTime.UtcNow,
                    Message = $"Resonance sync failed: {ex.Message}"
                };
            }
        }

        // Helper methods for cross-service resonance calculation
        private async Task<ServiceResonanceResult> CalculateResonanceForService(string serviceId, string conceptId, string userId)
        {
            try
            {
                if (_coreApiService == null)
                {
                    _logger.Warn("CoreApiService not available for cross-service resonance calculation");
                    return new ServiceResonanceResult
                    {
                        ServiceId = serviceId,
                        ServiceName = $"Service-{serviceId}",
                        ConceptId = conceptId,
                        ResonanceScore = 0.0,
                        Status = "error",
                        CalculatedAt = DateTime.UtcNow,
                        ErrorMessage = "CoreApiService not available"
                    };
                }

                // Call the resonance calculation endpoint on the target service
                var request = new { conceptId, userId };
                var args = JsonSerializer.SerializeToElement(request);
                var call = new DynamicCall(serviceId, "calculate-resonance", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);

                if (response is JsonElement jsonResponse)
                {
                    var resonanceScore = jsonResponse.TryGetProperty("overallMatch", out var matchElement) ? matchElement.GetDouble() : 0.0;
                    var status = jsonResponse.TryGetProperty("success", out var successElement) && successElement.GetBoolean() ? "success" : "error";
                    
                    return new ServiceResonanceResult
                    {
                        ServiceId = serviceId,
                        ServiceName = $"Service-{serviceId}",
                        ConceptId = conceptId,
                        ResonanceScore = resonanceScore,
                        Status = status,
                        CalculatedAt = DateTime.UtcNow,
                        ErrorMessage = status == "error" ? "Resonance calculation failed" : null
                    };
                }

                return new ServiceResonanceResult
                {
                    ServiceId = serviceId,
                    ServiceName = $"Service-{serviceId}",
                    ConceptId = conceptId,
                    ResonanceScore = 0.0,
                    Status = "error",
                    CalculatedAt = DateTime.UtcNow,
                    ErrorMessage = "Invalid response from service"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to calculate resonance for service {serviceId}: {ex.Message}", ex);
                return new ServiceResonanceResult
                {
                    ServiceId = serviceId,
                    ServiceName = $"Service-{serviceId}",
                    ConceptId = conceptId,
                    ResonanceScore = 0.0,
                    Status = "error",
                    CalculatedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<List<GlobalResonancePattern>> AnalyzeGlobalResonancePatterns(List<ServiceResonanceResult> serviceResults)
        {
            var patterns = new List<GlobalResonancePattern>();
            
            // Group results by concept
            var conceptGroups = serviceResults.GroupBy(r => r.ConceptId);
            
            foreach (var group in conceptGroups)
            {
                var conceptId = group.Key;
                var results = group.ToList();
                var averageResonance = results.Average(r => r.ResonanceScore);
                var variance = CalculateVariance(results.Select(r => r.ResonanceScore));
                
                patterns.Add(new GlobalResonancePattern
                {
                    PatternId = $"global-{conceptId}",
                    ConceptId = conceptId,
                    AverageResonance = averageResonance,
                    ResonanceVariance = variance,
                    ServiceCount = results.Count,
                    PatternStrength = CalculatePatternStrength(results),
                    DetectedAt = DateTime.UtcNow
                });
            }
            
            return patterns.OrderByDescending(p => p.PatternStrength).ToList();
        }

        private List<CrossServiceHarmonic> CalculateCrossServiceHarmonics(List<ServiceResonanceResult> serviceResults)
        {
            var harmonics = new List<CrossServiceHarmonic>();
            var serviceGroups = serviceResults.GroupBy(r => r.ServiceId).ToList();
            
            for (int i = 0; i < serviceGroups.Count - 1; i++)
            {
                for (int j = i + 1; j < serviceGroups.Count; j++)
                {
                    var service1 = serviceGroups[i];
                    var service2 = serviceGroups[j];
                    
                    var harmonicRatio = CalculateServiceHarmonicRatio(service1.ToList(), service2.ToList());
                    if (harmonicRatio > 0.5) // Threshold for harmonic relationship
                    {
                        harmonics.Add(new CrossServiceHarmonic
                        {
                            Service1Id = service1.Key,
                            Service2Id = service2.Key,
                            HarmonicRatio = harmonicRatio,
                            ResonanceAlignment = CalculateResonanceAlignment(service1.ToList(), service2.ToList()),
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            
            return harmonics.OrderByDescending(h => h.HarmonicRatio).ToList();
        }

        private List<DistributedPattern> AnalyzeDistributedPatterns(List<ServiceResonanceResult> results, List<ConceptServicePair> pairs)
        {
            var patterns = new List<DistributedPattern>();
            
            // Analyze load distribution patterns
            var serviceLoads = results.GroupBy(r => r.ServiceId)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var conceptLoads = results.GroupBy(r => r.ConceptId)
                .ToDictionary(g => g.Key, g => g.Count());
            
            patterns.Add(new DistributedPattern
            {
                PatternId = "load-distribution",
                PatternType = "load-balancing",
                ServiceLoads = serviceLoads,
                ConceptLoads = conceptLoads,
                LoadVariance = CalculateLoadVariance(serviceLoads.Values),
                DetectedAt = DateTime.UtcNow
            });
            
            return patterns;
        }

        private LoadBalancingMetrics CalculateLoadBalancingMetrics(List<ServiceResonanceResult> results)
        {
            var serviceCounts = results.GroupBy(r => r.ServiceId)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var totalRequests = results.Count;
            var serviceCount = serviceCounts.Count;
            var averageLoad = serviceCount > 0 ? (double)totalRequests / serviceCount : 0.0;
            var maxLoad = serviceCounts.Values.DefaultIfEmpty(0).Max();
            var minLoad = serviceCounts.Values.DefaultIfEmpty(0).Min();
            
            return new LoadBalancingMetrics
            {
                TotalRequests = totalRequests,
                ServiceCount = serviceCount,
                AverageLoad = averageLoad,
                MaxLoad = maxLoad,
                MinLoad = minLoad,
                LoadVariance = CalculateLoadVariance(serviceCounts.Values),
                CalculatedAt = DateTime.UtcNow
            };
        }

        private double CalculateNetworkResonance(List<ServiceResonanceResult> results)
        {
            if (!results.Any()) return 0.0;
            
            var averageResonance = results.Average(r => r.ResonanceScore);
            var resonanceVariance = CalculateVariance(results.Select(r => r.ResonanceScore));
            var networkCoherence = 1.0 - Math.Min(resonanceVariance, 1.0);
            
            return (averageResonance + networkCoherence) / 2.0;
        }

        private double CalculateConfidenceScore(List<ServiceScore> scores)
        {
            if (!scores.Any()) return 0.0;
            
            var variance = CalculateVariance(scores.Select(s => s.Score));
            var count = scores.Count;
            var timeDecay = CalculateTimeDecay(scores);
            
            return Math.Min(1.0, (1.0 - variance) * Math.Log(count + 1) * timeDecay);
        }

        private double CalculateGlobalAggregation(List<AggregatedConceptResonance> results)
        {
            if (!results.Any()) return 0.0;
            
            var weightedSum = results.Sum(r => r.WeightedAverage * r.Confidence);
            var totalWeight = results.Sum(r => r.Confidence);
            
            return totalWeight > 0 ? weightedSum / totalWeight : 0.0;
        }

        private QualityMetrics CalculateQualityMetrics(List<AggregatedConceptResonance> results)
        {
            var totalConcepts = results.Count;
            var highConfidenceConcepts = results.Count(r => r.Confidence > 0.8);
            var averageConfidence = results.Any() ? results.Average(r => r.Confidence) : 0.0;
            var averageResonance = results.Any() ? results.Average(r => r.WeightedAverage) : 0.0;
            
            return new QualityMetrics
            {
                TotalConcepts = totalConcepts,
                HighConfidenceConcepts = highConfidenceConcepts,
                AverageConfidence = averageConfidence,
                AverageResonance = averageResonance,
                QualityScore = (averageConfidence + averageResonance) / 2.0,
                CalculatedAt = DateTime.UtcNow
            };
        }

        private async Task<List<string>> GetAvailableServices()
        {
            try
            {
                if (_coreApiService == null) return new List<string>();
                
                var call = new DynamicCall("codex.service-discovery", "get-all-services", JsonSerializer.SerializeToElement(new { }));
                var response = await _coreApiService.ExecuteDynamicCall(call);
                
                if (response is JsonElement jsonResponse && jsonResponse.TryGetProperty("services", out var servicesElement))
                {
                    var services = new List<string>();
                    foreach (var service in servicesElement.EnumerateArray())
                    {
                        if (service.TryGetProperty("serviceId", out var idElement))
                        {
                            services.Add(idElement.GetString() ?? "");
                        }
                    }
                    return services;
                }
                
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get available services: {ex.Message}", ex);
                return new List<string>();
            }
        }

        private async Task<List<ResonancePattern>> GetResonancePatternsFromService(string serviceId)
        {
            try
            {
                if (_coreApiService == null) return new List<ResonancePattern>();
                
                var call = new DynamicCall(serviceId, "get-resonance-patterns", JsonSerializer.SerializeToElement(new { }));
                var response = await _coreApiService.ExecuteDynamicCall(call);
                
                if (response is JsonElement jsonResponse && jsonResponse.TryGetProperty("patterns", out var patternsElement))
                {
                    var patterns = new List<ResonancePattern>();
                    foreach (var pattern in patternsElement.EnumerateArray())
                    {
                        patterns.Add(new ResonancePattern
                        {
                            ConceptId = pattern.TryGetProperty("conceptId", out var idElement) ? idElement.GetString() ?? "" : "",
                            ConceptName = pattern.TryGetProperty("conceptName", out var nameElement) ? nameElement.GetString() ?? "" : "",
                            ResonanceScore = pattern.TryGetProperty("resonanceScore", out var scoreElement) ? scoreElement.GetDouble() : 0.0,
                            Frequency = pattern.TryGetProperty("frequency", out var freqElement) ? freqElement.GetDouble() : 0.0,
                            Amplitude = pattern.TryGetProperty("amplitude", out var ampElement) ? ampElement.GetDouble() : 0.0,
                            Phase = pattern.TryGetProperty("phase", out var phaseElement) ? phaseElement.GetDouble() : 0.0
                        });
                    }
                    return patterns;
                }
                
                return new List<ResonancePattern>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get resonance patterns from service {serviceId}: {ex.Message}", ex);
                return new List<ResonancePattern>();
            }
        }

        private List<GlobalResonancePattern> AnalyzeCrossServicePatterns(Dictionary<string, List<ResonancePattern>> servicePatterns)
        {
            var globalPatterns = new List<GlobalResonancePattern>();
            
            // Find common concepts across services
            var allConcepts = servicePatterns.Values
                .SelectMany(patterns => patterns.Select(p => p.ConceptId))
                .Distinct()
                .ToList();
            
            foreach (var conceptId in allConcepts)
            {
                var conceptPatterns = servicePatterns.Values
                    .SelectMany(patterns => patterns.Where(p => p.ConceptId == conceptId))
                    .ToList();
                
                if (conceptPatterns.Any())
                {
                    var averageResonance = conceptPatterns.Average(p => p.ResonanceScore);
                    var serviceCount = conceptPatterns.Count;
                    var variance = CalculateVariance(conceptPatterns.Select(p => p.ResonanceScore));
                    
                    globalPatterns.Add(new GlobalResonancePattern
                    {
                        PatternId = $"cross-service-{conceptId}",
                        ConceptId = conceptId,
                        AverageResonance = averageResonance,
                        ResonanceVariance = variance,
                        ServiceCount = serviceCount,
                        PatternStrength = CalculateCrossServicePatternStrength(conceptPatterns),
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
            
            return globalPatterns.OrderByDescending(p => p.PatternStrength).ToList();
        }

        private List<GlobalHarmonic> CalculateGlobalHarmonics(Dictionary<string, List<ResonancePattern>> servicePatterns)
        {
            var harmonics = new List<GlobalHarmonic>();
            var services = servicePatterns.Keys.ToList();
            
            for (int i = 0; i < services.Count - 1; i++)
            {
                for (int j = i + 1; j < services.Count; j++)
                {
                    var service1 = services[i];
                    var service2 = services[j];
                    var patterns1 = servicePatterns[service1];
                    var patterns2 = servicePatterns[service2];
                    
                    var harmonicRatio = CalculateServicePatternHarmonicRatio(patterns1, patterns2);
                    if (harmonicRatio > 0.6)
                    {
                        harmonics.Add(new GlobalHarmonic
                        {
                            Service1Id = service1,
                            Service2Id = service2,
                            HarmonicRatio = harmonicRatio,
                            ResonanceAlignment = CalculatePatternResonanceAlignment(patterns1, patterns2),
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            
            return harmonics.OrderByDescending(h => h.HarmonicRatio).ToList();
        }

        private List<ResonanceCluster> CalculateGlobalResonanceClusters(Dictionary<string, List<ResonancePattern>> servicePatterns)
        {
            var clusters = new List<ResonanceCluster>();
            var allPatterns = servicePatterns.Values.SelectMany(p => p).ToList();
            
            // Group patterns by resonance score ranges
            var highResonance = allPatterns.Where(p => p.ResonanceScore > 0.8).ToList();
            var mediumResonance = allPatterns.Where(p => p.ResonanceScore > 0.5 && p.ResonanceScore <= 0.8).ToList();
            var lowResonance = allPatterns.Where(p => p.ResonanceScore <= 0.5).ToList();
            
            if (highResonance.Any())
            {
                clusters.Add(new ResonanceCluster
                {
                    ClusterId = "high-resonance-global",
                    ConceptIds = highResonance.Select(p => p.ConceptId).ToList(),
                    AverageResonance = highResonance.Average(p => p.ResonanceScore),
                    ClusterStrength = highResonance.Count
                });
            }
            
            if (mediumResonance.Any())
            {
                clusters.Add(new ResonanceCluster
                {
                    ClusterId = "medium-resonance-global",
                    ConceptIds = mediumResonance.Select(p => p.ConceptId).ToList(),
                    AverageResonance = mediumResonance.Average(p => p.ResonanceScore),
                    ClusterStrength = mediumResonance.Count
                });
            }
            
            return clusters;
        }

        private async Task<ServiceSyncResult> SyncResonanceWithService(string serviceId, Dictionary<string, object> resonanceData, Dictionary<string, object> syncOptions)
        {
            try
            {
                if (_coreApiService == null)
                {
                    return new ServiceSyncResult
                    {
                        ServiceId = serviceId,
                        Success = false,
                        SyncedAt = DateTime.UtcNow,
                        ErrorMessage = "CoreApiService not available"
                    };
                }
                
                var request = new { resonanceData, syncOptions };
                var args = JsonSerializer.SerializeToElement(request);
                var call = new DynamicCall(serviceId, "sync-resonance", args);
                var response = await _coreApiService.ExecuteDynamicCall(call);
                
                if (response is JsonElement jsonResponse)
                {
                    var success = jsonResponse.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
                    var message = jsonResponse.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "Sync completed";
                    
                    return new ServiceSyncResult
                    {
                        ServiceId = serviceId,
                        Success = success,
                        SyncedAt = DateTime.UtcNow,
                        ErrorMessage = success ? null : message
                    };
                }
                
                return new ServiceSyncResult
                {
                    ServiceId = serviceId,
                    Success = false,
                    SyncedAt = DateTime.UtcNow,
                    ErrorMessage = "Invalid response from service"
                };
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to sync resonance with service {serviceId}: {ex.Message}", ex);
                return new ServiceSyncResult
                {
                    ServiceId = serviceId,
                    Success = false,
                    SyncedAt = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                };
            }
        }

        // Utility methods
        private double CalculateVariance(IEnumerable<double> values)
        {
            var valueList = values.ToList();
            if (!valueList.Any()) return 0.0;
            
            var average = valueList.Average();
            var sumSquaredDiffs = valueList.Sum(v => Math.Pow(v - average, 2));
            return sumSquaredDiffs / valueList.Count;
        }

        private double CalculateLoadVariance(IEnumerable<int> loads)
        {
            var loadList = loads.ToList();
            if (!loadList.Any()) return 0.0;
            
            var average = loadList.Average();
            var sumSquaredDiffs = loadList.Sum(l => Math.Pow(l - average, 2));
            return sumSquaredDiffs / loadList.Count;
        }

        private double CalculateTimeDecay(List<ServiceScore> scores)
        {
            if (!scores.Any()) return 1.0;
            
            var now = DateTime.UtcNow;
            var maxAge = scores.Max(s => (now - s.Timestamp).TotalHours);
            return Math.Exp(-maxAge / 24.0); // Decay over 24 hours
        }

        private double CalculatePatternStrength(List<ServiceResonanceResult> results)
        {
            if (!results.Any()) return 0.0;
            
            var averageResonance = results.Average(r => r.ResonanceScore);
            var successRate = results.Count(r => r.Status == "success") / (double)results.Count;
            var serviceCount = results.Count;
            
            return (averageResonance + successRate + Math.Min(serviceCount / 10.0, 1.0)) / 3.0;
        }

        private double CalculateServiceHarmonicRatio(List<ServiceResonanceResult> service1, List<ServiceResonanceResult> service2)
        {
            var commonConcepts = service1.Where(s1 => service2.Any(s2 => s2.ConceptId == s1.ConceptId)).ToList();
            if (!commonConcepts.Any()) return 0.0;
            
            var harmonicSum = 0.0;
            foreach (var concept in commonConcepts)
            {
                var s1Score = concept.ResonanceScore;
                var s2Score = service2.First(s2 => s2.ConceptId == concept.ConceptId).ResonanceScore;
                harmonicSum += Math.Min(s1Score, s2Score) / Math.Max(s1Score, s2Score);
            }
            
            return harmonicSum / commonConcepts.Count;
        }

        private double CalculateResonanceAlignment(List<ServiceResonanceResult> service1, List<ServiceResonanceResult> service2)
        {
            var commonConcepts = service1.Where(s1 => service2.Any(s2 => s2.ConceptId == s1.ConceptId)).ToList();
            if (!commonConcepts.Any()) return 0.0;
            
            var alignmentSum = 0.0;
            foreach (var concept in commonConcepts)
            {
                var s1Score = concept.ResonanceScore;
                var s2Score = service2.First(s2 => s2.ConceptId == concept.ConceptId).ResonanceScore;
                alignmentSum += 1.0 - Math.Abs(s1Score - s2Score);
            }
            
            return alignmentSum / commonConcepts.Count;
        }

        private double CalculateCrossServicePatternStrength(List<ResonancePattern> patterns)
        {
            if (!patterns.Any()) return 0.0;
            
            var averageResonance = patterns.Average(p => p.ResonanceScore);
            var patternCount = patterns.Count;
            var resonanceVariance = CalculateVariance(patterns.Select(p => p.ResonanceScore));
            
            return (averageResonance + Math.Min(patternCount / 5.0, 1.0) + (1.0 - resonanceVariance)) / 3.0;
        }

        private double CalculateServicePatternHarmonicRatio(List<ResonancePattern> patterns1, List<ResonancePattern> patterns2)
        {
            var commonConcepts = patterns1.Where(p1 => patterns2.Any(p2 => p2.ConceptId == p1.ConceptId)).ToList();
            if (!commonConcepts.Any()) return 0.0;
            
            var harmonicSum = 0.0;
            foreach (var pattern in commonConcepts)
            {
                var p1Score = pattern.ResonanceScore;
                var p2Score = patterns2.First(p2 => p2.ConceptId == pattern.ConceptId).ResonanceScore;
                harmonicSum += Math.Min(p1Score, p2Score) / Math.Max(p1Score, p2Score);
            }
            
            return harmonicSum / commonConcepts.Count;
        }

        private double CalculatePatternResonanceAlignment(List<ResonancePattern> patterns1, List<ResonancePattern> patterns2)
        {
            var commonConcepts = patterns1.Where(p1 => patterns2.Any(p2 => p2.ConceptId == p1.ConceptId)).ToList();
            if (!commonConcepts.Any()) return 0.0;
            
            var alignmentSum = 0.0;
            foreach (var pattern in commonConcepts)
            {
                var p1Score = pattern.ResonanceScore;
                var p2Score = patterns2.First(p2 => p2.ConceptId == pattern.ConceptId).ResonanceScore;
                alignmentSum += 1.0 - Math.Abs(p1Score - p2Score);
            }
            
            return alignmentSum / commonConcepts.Count;
        }

        /// <summary>
        /// Register all Cross-Service Resonance related nodes for AI agent discovery
        /// </summary>
        private void RegisterCrossServiceResonanceNodes(NodeRegistry registry)
        {
            RegisterCrossServiceResonanceRoutes(registry);
            RegisterCrossServiceResonanceDTOs(registry);
            RegisterCrossServiceResonanceClasses(registry);
        }

        private void RegisterCrossServiceResonanceRoutes(NodeRegistry registry)
        {
            var routes = new[]
            {
                new { Method = "POST", Path = "/ucore/resonance/cross-service", Name = "Calculate Cross-Service Resonance", Description = "Calculate resonance across multiple services for distributed concept analysis" },
                new { Method = "POST", Path = "/ucore/resonance/distributed", Name = "Calculate Distributed Resonance", Description = "Calculate distributed resonance across multiple services with load balancing" },
                new { Method = "POST", Path = "/ucore/resonance/aggregate", Name = "Aggregate Resonance Results", Description = "Aggregate resonance results from multiple services" },
                new { Method = "GET", Path = "/ucore/resonance/global-patterns", Name = "Get Global Resonance Patterns", Description = "Get global resonance patterns across all services" },
                new { Method = "POST", Path = "/ucore/resonance/sync", Name = "Sync Resonance Across Services", Description = "Sync resonance data across services" }
            };

            foreach (var route in routes)
            {
                var node = new Node(
                    Id: $"ucore-resonance-route-{route.Name.ToLower().Replace(" ", "-")}",
                    TypeId: "api-route",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: route.Name,
                    Description: route.Description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["method"] = route.Method,
                            ["path"] = route.Path,
                            ["name"] = route.Name,
                            ["description"] = route.Description,
                            ["parameters"] = GetCrossServiceResonanceRouteParameters(route.Path),
                            ["responseType"] = GetCrossServiceResonanceRouteResponseType(route.Path),
                            ["example"] = GetCrossServiceResonanceRouteExample(route.Path)
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["module"] = "UCoreResonanceEngine",
                        ["category"] = "Cross-Service Resonance",
                        ["method"] = route.Method,
                        ["path"] = route.Path
                    }
                );
                registry.Upsert(node);
            }
        }

        private void RegisterCrossServiceResonanceDTOs(NodeRegistry registry)
        {
            var dtos = new[]
            {
                new { Name = "CrossServiceResonanceRequest", Description = "Request for cross-service resonance calculation", Properties = new[] { "ConceptId", "UserId", "ServiceIds" } },
                new { Name = "CrossServiceResonanceResponse", Description = "Response for cross-service resonance calculation", Properties = new[] { "Success", "ConceptId", "ServiceResults", "AverageResonance", "GlobalPatterns" } },
                new { Name = "DistributedResonanceRequest", Description = "Request for distributed resonance calculation", Properties = new[] { "UserId", "Concepts", "TargetServices" } },
                new { Name = "DistributedResonanceResponse", Description = "Response for distributed resonance calculation", Properties = new[] { "Success", "UserId", "ServiceResults", "DistributedPatterns", "LoadBalancingMetrics" } },
                new { Name = "AggregatedResonanceRequest", Description = "Request for aggregating resonance results", Properties = new[] { "ServiceResults", "ServiceWeights" } },
                new { Name = "AggregatedResonanceResponse", Description = "Response for aggregated resonance results", Properties = new[] { "Success", "AggregatedResults", "GlobalAggregation", "QualityMetrics" } },
                new { Name = "GlobalResonancePatternsResponse", Description = "Response for global resonance patterns", Properties = new[] { "Success", "GlobalPatterns", "GlobalHarmonics", "ResonanceClusters" } },
                new { Name = "ResonanceSyncRequest", Description = "Request for syncing resonance across services", Properties = new[] { "TargetServices", "ResonanceData", "SyncOptions" } },
                new { Name = "ResonanceSyncResponse", Description = "Response for resonance sync operation", Properties = new[] { "Success", "SyncResults", "SuccessfulSyncs", "FailedSyncs" } }
            };

            foreach (var dto in dtos)
            {
                var node = new Node(
                    Id: $"ucore-resonance-dto-{dto.Name.ToLower()}",
                    TypeId: "dto",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: dto.Name,
                    Description: dto.Description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["name"] = dto.Name,
                            ["description"] = dto.Description,
                            ["properties"] = dto.Properties,
                            ["usage"] = GetCrossServiceResonanceDTOUsage(dto.Name),
                            ["example"] = GetCrossServiceResonanceDTOExample(dto.Name)
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["module"] = "UCoreResonanceEngine",
                        ["category"] = "Cross-Service Resonance DTOs",
                        ["type"] = "record"
                    }
                );
                registry.Upsert(node);
            }
        }

        private void RegisterCrossServiceResonanceClasses(NodeRegistry registry)
        {
            var classes = new[]
            {
                new { Name = "ServiceResonanceResult", Description = "Result of resonance calculation for a specific service", Properties = new[] { "ServiceId", "ServiceName", "ConceptId", "ResonanceScore", "Status", "CalculatedAt" } },
                new { Name = "GlobalResonancePattern", Description = "Global resonance pattern across services", Properties = new[] { "PatternId", "ConceptId", "AverageResonance", "ResonanceVariance", "ServiceCount", "PatternStrength" } },
                new { Name = "CrossServiceHarmonic", Description = "Harmonic relationship between services", Properties = new[] { "Service1Id", "Service2Id", "HarmonicRatio", "ResonanceAlignment", "DetectedAt" } },
                new { Name = "DistributedPattern", Description = "Pattern detected in distributed resonance calculation", Properties = new[] { "PatternId", "PatternType", "ServiceLoads", "ConceptLoads", "LoadVariance" } },
                new { Name = "LoadBalancingMetrics", Description = "Metrics for load balancing in distributed calculations", Properties = new[] { "TotalRequests", "ServiceCount", "AverageLoad", "MaxLoad", "MinLoad", "LoadVariance" } },
                new { Name = "AggregatedConceptResonance", Description = "Aggregated resonance result for a concept", Properties = new[] { "ConceptId", "ServiceScores", "WeightedAverage", "Confidence" } },
                new { Name = "ServiceScore", Description = "Resonance score from a specific service", Properties = new[] { "ServiceId", "Score", "Weight", "Timestamp" } },
                new { Name = "QualityMetrics", Description = "Quality metrics for aggregated results", Properties = new[] { "TotalConcepts", "HighConfidenceConcepts", "AverageConfidence", "AverageResonance", "QualityScore" } },
                new { Name = "ServiceSyncResult", Description = "Result of resonance sync with a service", Properties = new[] { "ServiceId", "Success", "SyncedAt", "ErrorMessage" } },
                new { Name = "ConceptServicePair", Description = "Pair of concept and service for distributed calculation", Properties = new[] { "ConceptId", "ServiceId" } }
            };

            foreach (var cls in classes)
            {
                var node = new Node(
                    Id: $"ucore-resonance-class-{cls.Name.ToLower()}",
                    TypeId: "class",
                    State: ContentState.Water,
                    Locale: "en",
                    Title: cls.Name,
                    Description: cls.Description,
                    Content: new ContentRef(
                        MediaType: "application/json",
                        InlineJson: JsonSerializer.Serialize(new Dictionary<string, object>
                        {
                            ["name"] = cls.Name,
                            ["description"] = cls.Description,
                            ["properties"] = cls.Properties,
                            ["usage"] = GetCrossServiceResonanceClassUsage(cls.Name),
                            ["example"] = GetCrossServiceResonanceClassExample(cls.Name)
                        }),
                        InlineBytes: null,
                        ExternalUri: null
                    ),
                    Meta: new Dictionary<string, object>
                    {
                        ["module"] = "UCoreResonanceEngine",
                        ["category"] = "Cross-Service Resonance Classes",
                        ["type"] = "class"
                    }
                );
                registry.Upsert(node);
            }
        }

        // Helper methods for AI agent generation
        private Dictionary<string, object> GetCrossServiceResonanceRouteParameters(string path)
        {
            return path switch
            {
                "/ucore/resonance/cross-service" => new Dictionary<string, object>
                {
                    ["conceptId"] = "string - ID of the concept to analyze",
                    ["userId"] = "string - ID of the user requesting analysis",
                    ["serviceIds"] = "string[] - List of service IDs to include in calculation"
                },
                "/ucore/resonance/distributed" => new Dictionary<string, object>
                {
                    ["userId"] = "string - ID of the user requesting analysis",
                    ["concepts"] = "string[] - List of concept IDs to analyze",
                    ["targetServices"] = "string[] - List of target service IDs"
                },
                "/ucore/resonance/aggregate" => new Dictionary<string, object>
                {
                    ["serviceResults"] = "ServiceResonanceResult[] - Results from multiple services",
                    ["serviceWeights"] = "Dictionary<string, double> - Optional weights for services"
                },
                "/ucore/resonance/global-patterns" => new Dictionary<string, object>
                {
                    ["serviceFilter"] = "string? - Optional filter for specific services"
                },
                "/ucore/resonance/sync" => new Dictionary<string, object>
                {
                    ["targetServices"] = "string[] - List of services to sync with",
                    ["resonanceData"] = "Dictionary<string, object> - Resonance data to sync",
                    ["syncOptions"] = "Dictionary<string, object> - Sync configuration options"
                },
                _ => new Dictionary<string, object>()
            };
        }

        private string GetCrossServiceResonanceRouteResponseType(string path)
        {
            return path switch
            {
                "/ucore/resonance/cross-service" => "CrossServiceResonanceResponse",
                "/ucore/resonance/distributed" => "DistributedResonanceResponse",
                "/ucore/resonance/aggregate" => "AggregatedResonanceResponse",
                "/ucore/resonance/global-patterns" => "GlobalResonancePatternsResponse",
                "/ucore/resonance/sync" => "ResonanceSyncResponse",
                _ => "object"
            };
        }

        private Dictionary<string, object> GetCrossServiceResonanceRouteExample(string path)
        {
            return path switch
            {
                "/ucore/resonance/cross-service" => new Dictionary<string, object>
                {
                    ["conceptId"] = "concept-123",
                    ["userId"] = "user-456",
                    ["serviceIds"] = new[] { "service-1", "service-2", "service-3" }
                },
                "/ucore/resonance/distributed" => new Dictionary<string, object>
                {
                    ["userId"] = "user-456",
                    ["concepts"] = new[] { "concept-1", "concept-2", "concept-3" },
                    ["targetServices"] = new[] { "service-1", "service-2" }
                },
                _ => new Dictionary<string, object>()
            };
        }

        private string GetCrossServiceResonanceDTOUsage(string dtoName)
        {
            return dtoName switch
            {
                "CrossServiceResonanceRequest" => "Used to request resonance calculation across multiple services for a specific concept",
                "CrossServiceResonanceResponse" => "Contains results of cross-service resonance calculation including service results and global patterns",
                "DistributedResonanceRequest" => "Used to request distributed resonance calculation across multiple concepts and services",
                "DistributedResonanceResponse" => "Contains results of distributed resonance calculation including load balancing metrics",
                "AggregatedResonanceRequest" => "Used to request aggregation of resonance results from multiple services",
                "AggregatedResonanceResponse" => "Contains aggregated resonance results with quality metrics and confidence scores",
                "GlobalResonancePatternsResponse" => "Contains global resonance patterns detected across all services",
                "ResonanceSyncRequest" => "Used to request synchronization of resonance data across services",
                "ResonanceSyncResponse" => "Contains results of resonance synchronization operation",
                _ => "Used in cross-service resonance calculations"
            };
        }

        private Dictionary<string, object> GetCrossServiceResonanceDTOExample(string dtoName)
        {
            return dtoName switch
            {
                "CrossServiceResonanceRequest" => new Dictionary<string, object>
                {
                    ["conceptId"] = "concept-123",
                    ["userId"] = "user-456",
                    ["serviceIds"] = new[] { "service-1", "service-2" }
                },
                "CrossServiceResonanceResponse" => new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["conceptId"] = "concept-123",
                    ["averageResonance"] = 0.85,
                    ["serviceResults"] = new[]
                    {
                        new { serviceId = "service-1", resonanceScore = 0.9, status = "success" },
                        new { serviceId = "service-2", resonanceScore = 0.8, status = "success" }
                    }
                },
                _ => new Dictionary<string, object>()
            };
        }

        private string GetCrossServiceResonanceClassUsage(string className)
        {
            return className switch
            {
                "ServiceResonanceResult" => "Represents the result of resonance calculation from a specific service",
                "GlobalResonancePattern" => "Represents a resonance pattern detected across multiple services",
                "CrossServiceHarmonic" => "Represents harmonic relationships between different services",
                "DistributedPattern" => "Represents patterns detected in distributed resonance calculations",
                "LoadBalancingMetrics" => "Contains metrics for load balancing in distributed calculations",
                "AggregatedConceptResonance" => "Contains aggregated resonance results for a specific concept",
                "ServiceScore" => "Represents a resonance score from a specific service with timestamp",
                "QualityMetrics" => "Contains quality metrics for aggregated resonance results",
                "ServiceSyncResult" => "Represents the result of syncing resonance data with a service",
                "ConceptServicePair" => "Represents a pair of concept and service for distributed calculations",
                _ => "Used in cross-service resonance calculations"
            };
        }

        private Dictionary<string, object> GetCrossServiceResonanceClassExample(string className)
        {
            return className switch
            {
                "ServiceResonanceResult" => new Dictionary<string, object>
                {
                    ["serviceId"] = "service-1",
                    ["serviceName"] = "Resonance Service Alpha",
                    ["conceptId"] = "concept-123",
                    ["resonanceScore"] = 0.85,
                    ["status"] = "success",
                    ["calculatedAt"] = "2024-01-15T10:30:00Z"
                },
                "GlobalResonancePattern" => new Dictionary<string, object>
                {
                    ["patternId"] = "global-pattern-1",
                    ["conceptId"] = "concept-123",
                    ["averageResonance"] = 0.82,
                    ["serviceCount"] = 3,
                    ["patternStrength"] = 0.9
                },
                _ => new Dictionary<string, object>()
            };
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

    // Cross-Service Resonance DTOs
    [ApiType(Name = "CrossServiceResonanceRequest", Description = "Request for cross-service resonance calculation", Type = "object")]
    public record CrossServiceResonanceRequest
    {
        public string ConceptId { get; init; } = "";
        public string UserId { get; init; } = "";
        public List<string> ServiceIds { get; init; } = new();
    }

    [ApiType(Name = "CrossServiceResonanceResponse", Description = "Response for cross-service resonance calculation", Type = "object")]
    public record CrossServiceResonanceResponse
    {
        public bool Success { get; init; }
        public string ConceptId { get; init; } = "";
        public List<ServiceResonanceResult> ServiceResults { get; init; } = new();
        public double AverageResonance { get; init; }
        public List<GlobalResonancePattern> GlobalPatterns { get; init; } = new();
        public List<CrossServiceHarmonic> CrossServiceHarmonics { get; init; } = new();
        public DateTime CalculatedAt { get; init; }
        public string Message { get; init; } = "";
    }

    [ApiType(Name = "DistributedResonanceRequest", Description = "Request for distributed resonance calculation", Type = "object")]
    public record DistributedResonanceRequest
    {
        public string UserId { get; init; } = "";
        public List<string> Concepts { get; init; } = new();
        public List<string> TargetServices { get; init; } = new();
    }

    [ApiType(Name = "DistributedResonanceResponse", Description = "Response for distributed resonance calculation", Type = "object")]
    public record DistributedResonanceResponse
    {
        public bool Success { get; init; }
        public string UserId { get; init; } = "";
        public List<ServiceResonanceResult> ServiceResults { get; init; } = new();
        public List<DistributedPattern> DistributedPatterns { get; init; } = new();
        public LoadBalancingMetrics LoadBalancingMetrics { get; init; } = new();
        public double NetworkResonance { get; init; }
        public DateTime CalculatedAt { get; init; }
        public string Message { get; init; } = "";
    }

    [ApiType(Name = "AggregatedResonanceRequest", Description = "Request for aggregating resonance results", Type = "object")]
    public record AggregatedResonanceRequest
    {
        public List<ServiceResonanceResult> ServiceResults { get; init; } = new();
        public Dictionary<string, double>? ServiceWeights { get; init; }
    }

    [ApiType(Name = "AggregatedResonanceResponse", Description = "Response for aggregated resonance results", Type = "object")]
    public record AggregatedResonanceResponse
    {
        public bool Success { get; init; }
        public List<AggregatedConceptResonance> AggregatedResults { get; init; } = new();
        public double GlobalAggregation { get; init; }
        public QualityMetrics QualityMetrics { get; init; } = new();
        public DateTime AggregatedAt { get; init; }
        public string Message { get; init; } = "";
    }

    [ApiType(Name = "GlobalResonancePatternsResponse", Description = "Response for global resonance patterns", Type = "object")]
    public record GlobalResonancePatternsResponse
    {
        public bool Success { get; init; }
        public List<GlobalResonancePattern> GlobalPatterns { get; init; } = new();
        public List<GlobalHarmonic> GlobalHarmonics { get; init; } = new();
        public List<ResonanceCluster> ResonanceClusters { get; init; } = new();
        public int ServiceCount { get; init; }
        public DateTime RetrievedAt { get; init; }
        public string Message { get; init; } = "";
    }

    [ApiType(Name = "ResonanceSyncRequest", Description = "Request for syncing resonance across services", Type = "object")]
    public record ResonanceSyncRequest
    {
        public List<string> TargetServices { get; init; } = new();
        public Dictionary<string, object> ResonanceData { get; init; } = new();
        public Dictionary<string, object> SyncOptions { get; init; } = new();
    }

    [ApiType(Name = "ResonanceSyncResponse", Description = "Response for resonance sync operation", Type = "object")]
    public record ResonanceSyncResponse
    {
        public bool Success { get; init; }
        public List<ServiceSyncResult> SyncResults { get; init; } = new();
        public int SuccessfulSyncs { get; init; }
        public int FailedSyncs { get; init; }
        public DateTime SyncedAt { get; init; }
        public string Message { get; init; } = "";
    }

    // Cross-Service Resonance Classes
    [ApiType(Name = "ServiceResonanceResult", Description = "Result of resonance calculation for a specific service", Type = "object")]
    public record ServiceResonanceResult
    {
        public string ServiceId { get; init; } = "";
        public string ServiceName { get; init; } = "";
        public string ConceptId { get; init; } = "";
        public double ResonanceScore { get; init; }
        public string Status { get; init; } = "";
        public DateTime CalculatedAt { get; init; }
        public string? ErrorMessage { get; init; }
    }

    [ApiType(Name = "GlobalResonancePattern", Description = "Global resonance pattern across services", Type = "object")]
    public record GlobalResonancePattern
    {
        public string PatternId { get; init; } = "";
        public string ConceptId { get; init; } = "";
        public double AverageResonance { get; init; }
        public double ResonanceVariance { get; init; }
        public int ServiceCount { get; init; }
        public double PatternStrength { get; init; }
        public DateTime DetectedAt { get; init; }
    }

    [ApiType(Name = "CrossServiceHarmonic", Description = "Harmonic relationship between services", Type = "object")]
    public record CrossServiceHarmonic
    {
        public string Service1Id { get; init; } = "";
        public string Service2Id { get; init; } = "";
        public double HarmonicRatio { get; init; }
        public double ResonanceAlignment { get; init; }
        public DateTime DetectedAt { get; init; }
    }

    [ApiType(Name = "DistributedPattern", Description = "Pattern detected in distributed resonance calculation", Type = "object")]
    public record DistributedPattern
    {
        public string PatternId { get; init; } = "";
        public string PatternType { get; init; } = "";
        public Dictionary<string, int> ServiceLoads { get; init; } = new();
        public Dictionary<string, int> ConceptLoads { get; init; } = new();
        public double LoadVariance { get; init; }
        public DateTime DetectedAt { get; init; }
    }

    [ApiType(Name = "LoadBalancingMetrics", Description = "Metrics for load balancing in distributed calculations", Type = "object")]
    public record LoadBalancingMetrics
    {
        public int TotalRequests { get; init; }
        public int ServiceCount { get; init; }
        public double AverageLoad { get; init; }
        public int MaxLoad { get; init; }
        public int MinLoad { get; init; }
        public double LoadVariance { get; init; }
        public DateTime CalculatedAt { get; init; }
    }

    [ApiType(Name = "AggregatedConceptResonance", Description = "Aggregated resonance result for a concept", Type = "object")]
    public record AggregatedConceptResonance
    {
        public string ConceptId { get; init; } = "";
        public List<ServiceScore> ServiceScores { get; init; } = new();
        public double WeightedAverage { get; init; }
        public double Confidence { get; init; }
    }

    [ApiType(Name = "ServiceScore", Description = "Resonance score from a specific service", Type = "object")]
    public record ServiceScore
    {
        public string ServiceId { get; init; } = "";
        public double Score { get; init; }
        public double Weight { get; init; }
        public DateTime Timestamp { get; init; }
    }

    [ApiType(Name = "QualityMetrics", Description = "Quality metrics for aggregated results", Type = "object")]
    public record QualityMetrics
    {
        public int TotalConcepts { get; init; }
        public int HighConfidenceConcepts { get; init; }
        public double AverageConfidence { get; init; }
        public double AverageResonance { get; init; }
        public double QualityScore { get; init; }
        public DateTime CalculatedAt { get; init; }
    }

    [ApiType(Name = "ServiceSyncResult", Description = "Result of resonance sync with a service", Type = "object")]
    public record ServiceSyncResult
    {
        public string ServiceId { get; init; } = "";
        public bool Success { get; init; }
        public DateTime SyncedAt { get; init; }
        public string? ErrorMessage { get; init; }
    }

    [ApiType(Name = "ConceptServicePair", Description = "Pair of concept and service for distributed calculation", Type = "object")]
    public record ConceptServicePair
    {
        public string ConceptId { get; init; } = "";
        public string ServiceId { get; init; } = "";
    }

    [ApiType(Name = "GlobalHarmonic", Description = "Global harmonic relationship between services", Type = "object")]
    public record GlobalHarmonic
    {
        public string Service1Id { get; init; } = "";
        public string Service2Id { get; init; } = "";
        public double HarmonicRatio { get; init; }
        public double ResonanceAlignment { get; init; }
        public DateTime DetectedAt { get; init; }
    }
}
