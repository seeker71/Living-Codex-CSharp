using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly UCoreOntology _ontology;
        private readonly Dictionary<string, UserBeliefSystem> _userBeliefs = new();

        public UCoreResonanceEngine(UCoreOntology ontology)
        {
            _ontology = ontology ?? throw new ArgumentNullException(nameof(ontology));
        }

        public void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
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
    }

    // Supporting record types
    [ApiType(Name = "UserBeliefSystem", Description = "User's belief system and resonance preferences", Type = "object")]
    public record UserBeliefSystem
    {
        public string UserId { get; init; } = "";
        public Dictionary<string, double> WeightedConcepts { get; init; } = new();
        public Dictionary<string, double> InvestmentLevels { get; init; } = new();
        public List<double> PreferredFrequencies { get; init; } = new();
        public double ResonanceThreshold { get; init; } = 0.5;
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
}
