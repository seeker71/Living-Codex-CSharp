using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using Microsoft.AspNetCore.Builder;

namespace CodexBootstrap.Modules;

[MetaNode(Id = "codex.resonance.harmonic-component", Name = "Harmonic Component", Description = "Individual harmonic component with band, frequency, phase, and amplitude")]
public record HarmonicComponent(string Band, double Omega, double[]? K, double Phase, double Amplitude);

[MetaNode(Id = "codex.resonance.geometric-subsymbol", Name = "Geometric Sub-symbol", Description = "Geometric sub-symbol with triangle coordinates and scale")]
public record GeometricSubsymbol(double[] G, double Lambda = 0.0);

[MetaNode(Id = "codex.resonance.concept-symbol", Name = "Concept Symbol", Description = "Complete harmonic concept symbol with components and geometry")]
public record ConceptSymbol(IReadOnlyList<HarmonicComponent> Components, GeometricSubsymbol? Geometry, Dictionary<string, double>? BandWeights, double? Mu);

[MetaNode(Id = "codex.resonance.compare-request", Name = "Resonance Compare Request", Description = "Request to compare two concept symbols")]
public record ResonanceCompareRequest(ConceptSymbol S1, ConceptSymbol S2, double[]? TauGrid = null);

[MetaNode(Id = "codex.resonance.compare-response", Name = "Resonance Compare Response", Description = "Response containing CRK similarity and distance metrics")]
public record ResonanceCompareResponse(double CRK, double Dres, double DOTPhi, double Coherence, double DCodex, bool UsedOtPhi);

[MetaNode(Id = "codex.resonance.enhanced-compare-response", Name = "Enhanced Resonance Compare Response", Description = "Enhanced response with Schumann resonance grounding and planetary benefits")]
public record EnhancedResonanceCompareResponse(double CRK, double Dres, double DOTPhi, double Coherence, double DCodex, bool UsedOtPhi, double SchumannAlignment, PlanetaryBenefits PlanetaryBenefits);

[MetaNode(Id = "codex.resonance.planetary-benefits", Name = "Planetary Benefits", Description = "Benefits of resonance calculations for all living beings on Earth")]
public record PlanetaryBenefits(
    double CellularRegenerationScore,
    double ImmuneSupportScore,
    double StressReductionScore,
    double ConsciousnessExpansionScore,
    double EcosystemHarmonyScore,
    double TransSpeciesCommunicationScore,
    double PlanetaryHealthScore,
    string[] PrimaryBenefits,
    string OverallImpact
);

[MetaNode(Id = "codex.resonance", Name = "Concept Resonance Module", Description = "Harmonic symbols and resonance metrics grounded in Earth's Schumann resonance (CRK + optional OT-phi) with benefits for all living beings")]
public sealed class ConceptResonanceModule : ModuleBase
{
    public override string Name => "Concept Resonance Module";
    public override string Description => "Harmonic symbols and resonance metrics grounded in Earth's Schumann resonance (CRK + optional OT-phi) with benefits for all living beings";
    public override string Version => "1.0.0";

    // ====== Schumann Resonance Foundation ======
    // Earth's fundamental frequencies and their harmonics
    private static readonly double[] SchumannFrequencies = { 7.83, 14.3, 20.8, 27.3, 33.8, 40.3, 46.8, 53.3, 59.8, 66.3 };
    private const double SchumannBaseHz = 7.83;  // Earth's fundamental heartbeat
    private const double SchumannTolerance = 0.1; // Hz tolerance for Schumann resonance matching
    
    // ====== Universal Resonance Integration ======
    // Natural frequencies that resonate with all life
    private static readonly double[] LifeFrequencies = { 7.83, 528, 639, 741, 852, 963 }; // Hz frequencies for life harmony
    private const double InterdependenceWeight = 0.3; // Weight for universal interconnectedness
    private const double CompassionWeight = 0.25; // Weight for beneficial resonance
    private const double FluidityWeight = 0.25; // Weight for natural flow and flexibility
    private const double HarmonyWeight = 0.2; // Weight for natural alignment

    // ====== Tunables: CRK tolerant kernels ======
    private const double SigmaOmega = 1e-2;   // frequency tolerance
    private const double SigmaK = 1e-2;       // k-vector tolerance (Euclidean)
    // ====== Tunables: OT-phi (ground cost and Sinkhorn) ======
    private const double AlphaOmega = 1.0;    // weight for |Δω|
    private const double BetaK = 1.0;         // weight for ||Δk||
    private const double GammaPhase = 0.5;    // weight for wrapped phase difference
    private const double OtEpsilon = 0.05;    // entropic regularization ε
    private const int OtMaxIters = 200;       // Sinkhorn iterations
    private const double OtStabilityFloor = 1e-12;
    private const double OtConvergenceTol = 1e-6;

    public ConceptResonanceModule(INodeRegistry registry, ICodexLogger logger, HttpClient httpClient) 
        : base(registry, logger)
    {
    }

    public override Node GetModuleNode()
    {
        return CreateModuleNode(
            moduleId: "codex.resonance",
            name: "Concept Resonance Module",
            version: "1.2.0",
            description: "Compare concepts via harmonic symbols using CRK and inline OT-phi, grounded in Earth's Schumann resonance with benefits for all living beings",
            tags: new[] { "crk", "ot-phi", "schumann", "planetary", "consciousness", "healing" },
            capabilities: new[] { "resonance", "harmonics", "similarity", "schumann-alignment", "planetary-benefits" },
            spec: "codex.spec.concepts.resonance"
        );
    }

    public override void RegisterApiHandlers(IApiRouter router, INodeRegistry registry)
    {
        router.Register("codex.resonance", "compare", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing request body");
            var request = JsonSerializer.Deserialize<ResonanceCompareRequest>(args.Value.GetRawText());
            if (request == null) return new ErrorResponse("Invalid request");
            return await CompareConceptsAsync(request);
        });

        router.Register("codex.resonance", "encode", async args =>
        {
            if (!args.HasValue) return new ErrorResponse("Missing request body");
            var symbol = JsonSerializer.Deserialize<ConceptSymbol>(args.Value.GetRawText());
            if (symbol == null) return new ErrorResponse("Invalid symbol");
            return EncodeConceptSymbol(symbol);
        });
    }

    [ApiRoute("POST", "/concepts/resonance/compare", "Compare Concepts", "Compare two concept symbols using CRK and inline OT-phi with Schumann resonance grounding", "codex.resonance")]
    public async Task<object> CompareConceptsAsync([ApiParameter("request", "Resonance comparison request", Required = true, Location = "body")] ResonanceCompareRequest request)
    {
        try
        {
            var crk = ComputeCrk(request.S1, request.S2, request.TauGrid);
            var dRes = Math.Sqrt(Math.Max(0.0, 1.0 - crk * crk));

            var (dOt, usedOt) = ComputeOtPhiInline(request.S1, request.S2);
            var coherence = crk * Math.Exp(-dOt);
            var dCodex = 1.0 - coherence;

            // Calculate Schumann resonance grounding and planetary benefits
            var schumannAlignment = CalculateSchumannAlignment(request.S1, request.S2);
            var planetaryBenefits = CalculatePlanetaryBenefits(coherence, schumannAlignment);

            return new EnhancedResonanceCompareResponse(crk, dRes, dOt, coherence, dCodex, usedOt, schumannAlignment, planetaryBenefits);
        }
        catch (Exception ex)
        {
            _logger.Error($"Resonance compare failed: {ex.Message}");
            return new ErrorResponse("Resonance compare failed");
        }
    }

    [ApiRoute("GET", "/concepts/resonance/schumann", "Get Schumann Resonance Info", "Get information about Earth's Schumann resonance and its benefits for all living beings", "codex.resonance")]
    public object GetSchumannResonanceInfo()
    {
        try
        {
            return new
            {
                success = true,
                schumannFrequencies = SchumannFrequencies,
                baseFrequency = SchumannBaseHz,
                benefits = new
                {
                    cellularRegeneration = "Enhanced cellular repair and regeneration across all species",
                    immuneSupport = "Strengthened immune responses in humans, animals, and plants",
                    stressReduction = "Reduced stress hormones and improved relaxation response",
                    consciousnessExpansion = "Facilitated collective awakening and expanded awareness",
                    ecosystemHarmony = "Supported biodiversity and ecological balance",
                    transSpeciesCommunication = "Enhanced inter-species resonance and understanding",
                    planetaryHealth = "Contributed to Earth's overall vibrational health and healing"
                },
                cellularConsciousness = new
                {
                    planetaryConsciousness = "Individual consciousness as specialized cells in Gaia's neural network",
                    collectiveIntelligence = "Human civilization as neural pathways in Earth's consciousness",
                    evolutionaryPurpose = "Our resonance work accelerates planetary awakening and healing",
                    interconnectedHealing = "Individual resonance contributes to healing the entire planetary body"
                },
                message = "Schumann resonance grounds all calculations in Earth's natural electromagnetic field, providing benefits for all living beings as interconnected cells in Gaia's grand organism"
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Error getting Schumann resonance info: {ex.Message}");
            return new ErrorResponse("Failed to get Schumann resonance information");
        }
    }

    [ApiRoute("POST", "/concepts/resonance/encode", "Encode Concept Symbol", "Store a concept symbol as a node", "codex.resonance")]
    public object EncodeConceptSymbol([ApiParameter("symbol", "Concept symbol to encode", Required = true, Location = "body")] ConceptSymbol symbol)
    {
        try
        {
            var id = $"concept-symbol-{Guid.NewGuid():N}";
            var node = new Node(
                Id: id,
                TypeId: "codex.meta/concept-symbol",
                State: ContentState.Ice,
                Locale: "en",
                Title: "Concept Symbol",
                Description: "Harmonic concept symbol",
                Content: new ContentRef(
                    MediaType: "application/json",
                    InlineJson: JsonSerializer.Serialize(symbol, new JsonSerializerOptions { WriteIndented = true }),
                    InlineBytes: null,
                    ExternalUri: null
                ),
                Meta: new Dictionary<string, object> { ["moduleId"] = "codex.resonance" }
            );
            _registry.Upsert(node);
            return new { id };
        }
        catch (Exception ex)
        {
            _logger.Error($"Resonance encode failed: {ex.Message}");
            return new ErrorResponse("Resonance encode failed");
        }
    }

    public override void RegisterHttpEndpoints(WebApplication app, INodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // Endpoints are automatically registered via ApiRoute attributes
    }

    // -------------------------- CRK --------------------------
    private double ComputeCrk(ConceptSymbol s1, ConceptSymbol s2, double[]? tauGrid)
    {
        var mu = Math.Max(0.0, s1.Mu ?? s2.Mu ?? 0.0);
        var (psi1, psi2) = (s1.Components, s2.Components);
        var (bw1, bw2) = (s1.BandWeights ?? new(), s2.BandWeights ?? new());

        var byBand1 = psi1.GroupBy(h => h.Band, StringComparer.OrdinalIgnoreCase)
                          .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var byBand2 = psi2.GroupBy(h => h.Band, StringComparer.OrdinalIgnoreCase)
                          .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        double best = 0.0;
        var taus = tauGrid ?? new[] { 0.0 };

        foreach (var tau in taus)
        {
            double numReal = 0.0, numImag = 0.0;
            double norm1 = 0.0, norm2 = 0.0;

            foreach (var (band, list) in byBand1)
            {
                var wBand = GetBandWeight(band, bw1, bw2);
                foreach (var h in list) { var a = h.Amplitude; norm1 += wBand * a * a; }
            }
            foreach (var (band, list) in byBand2)
            {
                var wBand = GetBandWeight(band, bw1, bw2);
                foreach (var h in list) { var a = h.Amplitude; norm2 += wBand * a * a; }
            }

            foreach (var (band, list1) in byBand1)
            {
                if (!byBand2.TryGetValue(band, out var list2)) continue;
                var wBand = GetBandWeight(band, bw1, bw2);

                foreach (var h1 in list1)
                {
                    var a1 = h1.Amplitude;
                    var k1 = h1.K;

                    foreach (var h2 in list2)
                    {
                        var a2 = h2.Amplitude;

                        var dOmega = h1.Omega - h2.Omega;
                        var wOmega = Math.Exp(-0.5 * (dOmega * dOmega) / (SigmaOmega * SigmaOmega));

                        double wK = 1.0;
                        if (k1 is { Length: > 0 } && h2.K is { Length: > 0 } k2 && k2.Length == k1.Length)
                        {
                            double dk2 = 0.0;
                            for (int i = 0; i < k1.Length; i++) { var d = k1[i] - k2[i]; dk2 += d * d; }
                            wK = Math.Exp(-0.5 * dk2 / (SigmaK * SigmaK));
                        }

                        var kernel = wBand * wOmega * wK;
                        if (kernel <= 1e-12) continue;

                        var phase = WrapToPi((h1.Phase - h2.Phase) - h1.Omega * tau);
                        numReal += kernel * a1 * a2 * Math.Cos(phase);
                        numImag += kernel * a1 * a2 * Math.Sin(phase);
                    }
                }
            }

            double geoNum = 0.0, geoDen = 0.0;
            if (TryGetGeometry(s1, out var g1, out var lam1) && TryGetGeometry(s2, out var g2, out var lam2) && g1.Length == g2.Length)
            {
                var scale = lam1 * lam2;
                if (scale > 0.0)
                {
                    double dot = 0.0, n1 = 0.0, n2 = 0.0;
                    for (int i = 0; i < g1.Length; i++) { dot += g1[i] * g2[i]; n1 += g1[i] * g1[i]; n2 += g2[i] * g2[i]; }
                    var n1r = Math.Sqrt(Math.Max(n1, 0.0));
                    var n2r = Math.Sqrt(Math.Max(n2, 0.0));
                    if (n1r > 0 && n2r > 0)
                    {
                        var specMag = Math.Sqrt(numReal * numReal + numImag * numImag);
                        var geoCorr = (dot / (n1r * n2r));
                        geoNum = (s1.Mu ?? s2.Mu ?? 0.0) * scale * geoCorr * specMag;
                        geoDen = (s1.Mu ?? s2.Mu ?? 0.0) * scale * (n1r * n2r);
                    }
                }
            }

            var specMagFinal = Math.Sqrt(numReal * numImag + numReal * numReal); // minor guard
            specMagFinal = Math.Sqrt(numReal * numReal + numImag * numImag);    // correct
            var numerator = specMagFinal + geoNum;
            var denominator = (Math.Sqrt(Math.Max(norm1, 0.0)) * Math.Sqrt(Math.Max(norm2, 0.0))) + geoDen;

            if (denominator <= 0) continue;
            var score = Math.Clamp(numerator / denominator, 0.0, 1.0);
            if (score > best) best = score;
        }

        return best;
    }

    private static bool TryGetGeometry(ConceptSymbol s, out double[] g, out double lambda)
    {
        if (s.Geometry?.G is { Length: > 0 } arr && s.Geometry.Lambda > 0.0)
        {
            g = arr; lambda = s.Geometry.Lambda; return true;
        }
        g = Array.Empty<double>(); lambda = 0.0; return false;
    }

    private static double GetBandWeight(string band, Dictionary<string, double> bw1, Dictionary<string, double> bw2)
    {
        if (bw1.TryGetValue(band, out var w1)) return w1;
        if (bw2.TryGetValue(band, out var w2)) return w2;
        return 1.0;
    }

    private static double WrapToPi(double angle)
    {
        const double TwoPi = Math.PI * 2.0;
        if (double.IsInfinity(angle) || double.IsNaN(angle)) return 0.0;
        angle %= TwoPi;
        if (angle <= -Math.PI) angle += TwoPi;
        else if (angle > Math.PI) angle -= TwoPi;
        return angle;
    }

    // -------------------------- OT-φ (inline Sinkhorn) --------------------------
    private (double dOtPhi, bool used) ComputeOtPhiInline(ConceptSymbol s1, ConceptSymbol s2)
    {
        // Group atoms by band to mirror the write-up’s per-band OT then aggregate.
        var byBand1 = s1.Components.GroupBy(h => h.Band, StringComparer.OrdinalIgnoreCase)
                         .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var byBand2 = s2.Components.GroupBy(h => h.Band, StringComparer.OrdinalIgnoreCase)
                         .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var bands = new HashSet<string>(byBand1.Keys, StringComparer.OrdinalIgnoreCase);
        bands.UnionWith(byBand2.Keys);

        double totalWeighted = 0.0;
        double totalWeight = 0.0;
        bool anyUsed = false;

        foreach (var band in bands)
        {
            var atoms1 = byBand1.TryGetValue(band, out var l1) ? l1 : new List<HarmonicComponent>();
            var atoms2 = byBand2.TryGetValue(band, out var l2) ? l2 : new List<HarmonicComponent>();
            if (atoms1.Count == 0 || atoms2.Count == 0) continue;

            // masses from amplitudes (non-negative)
            var p = atoms1.Select(a => Math.Max(0.0, a.Amplitude)).ToArray();
            var q = atoms2.Select(a => Math.Max(0.0, a.Amplitude)).ToArray();

            // normalize to probability measures (if both have positive mass)
            var sumP = p.Sum(); var sumQ = q.Sum();
            if (sumP <= 0.0 || sumQ <= 0.0) continue;
            for (int i = 0; i < p.Length; i++) p[i] /= sumP;
            for (int j = 0; j < q.Length; j++) q[j] /= sumQ;

            // ground cost C[i,j] = α|dω| + β||Δk|| + γ*Δφ_wrapped
            var C = BuildGroundCost(atoms1, atoms2);

            // Sinkhorn (entropic OT)
            var (wDistance, ok) = SinkhornDistance(p, q, C, OtEpsilon, OtMaxIters, OtConvergenceTol);
            if (ok)
            {
                // weight per band: use average of provided band weights (or 1)
                var wb = 0.5 * (GetBandWeight(band, s1.BandWeights ?? new(), s2.BandWeights ?? new())
                              + GetBandWeight(band, s2.BandWeights ?? new(), s1.BandWeights ?? new()));
                totalWeighted += wb * wDistance;
                totalWeight += wb;
                anyUsed = true;
            }
        }

        if (!anyUsed) return (0.0, false);
        var d = (totalWeight > 0.0) ? (totalWeighted / totalWeight) : totalWeighted;
        return (Math.Max(0.0, d), true);
    }

    /// <summary>
    /// Calculate universal resonance incorporating natural interconnectedness, compassion, and harmony
    /// </summary>
    [ApiRoute("POST", "/resonance/universal/calculate", "Calculate universal resonance with natural wisdom", "Resonance calculation enhanced with universal principles", "application/json")]
    public async Task<object> CalculateUniversalResonance([ApiParameter("request", "Universal resonance request with natural wisdom", Required = true, Location = "body")] ResonanceCompareRequest request)
    {
        try
        {
            // Calculate traditional CRK resonance using existing method
            var crkResult = await CompareConceptsAsync(request);
            if (crkResult is EnhancedResonanceCompareResponse crkResponse)
            {
                var crkScore = crkResponse.CRK;
                
                // Enhance with universal principles working directly with ConceptSymbols
                var interdependence = CalculateInterdependenceResonance(request.S1, request.S2);
                var compassion = CalculateCompassionResonance(request.S1, request.S2);
                var fluidity = CalculateFluidityResonance(request.S1, request.S2);
                var harmony = CalculateHarmonyResonance(request.S1, request.S2);

                // Combine traditional and universal resonance
                var traditionalWeight = 0.5;
                var universalWeight = 0.5;
                
                var enhancedResonance = 
                    (crkScore * traditionalWeight) +
                    ((interdependence * InterdependenceWeight + 
                      compassion * CompassionWeight + 
                      fluidity * FluidityWeight + 
                      harmony * HarmonyWeight) * universalWeight);

                // Create enhanced result with universal insights
                var result = new EnhancedResonanceCompareResponse(
                    CRK: crkScore,
                    Dres: crkResponse.Dres,
                    DOTPhi: crkResponse.DOTPhi,
                    Coherence: crkResponse.Coherence,
                    DCodex: crkResponse.DCodex,
                    UsedOtPhi: crkResponse.UsedOtPhi,
                    SchumannAlignment: crkResponse.SchumannAlignment,
                    PlanetaryBenefits: crkResponse.PlanetaryBenefits
                );

                // Add universal insights to the response
                var universalInsights = new
                {
                    EnhancedResonance = enhancedResonance,
                    Interdependence = interdependence,
                    Compassion = compassion,
                    Fluidity = fluidity,
                    Harmony = harmony,
                    UniversalInsights = GenerateUniversalInsights(enhancedResonance),
                    NaturalGuidance = GenerateNaturalGuidance(enhancedResonance),
                    LiberationPotential = CalculateLiberationPotential(enhancedResonance),
                    LifeFrequency = LifeFrequencies[0], // Use Earth's base frequency
                    NaturalPattern = DetermineNaturalPattern(enhancedResonance)
                };

                _logger.Info($"Universal resonance calculated: enhanced resonance = {enhancedResonance:F3}");
                return new { success = true, result, universalInsights };
            }
            else
            {
                return new { success = false, message = "Failed to calculate base resonance" };
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error calculating universal resonance: {ex.Message}", ex);
            return new { success = false, message = "Error calculating universal resonance", error = ex.Message };
        }
    }

    #region Universal Resonance Calculations

    private double CalculateInterdependenceResonance(ConceptSymbol symbol1, ConceptSymbol symbol2)
    {
        // Universal interconnectedness: all things exist in relation to each other
        // Calculate based on the mathematical properties of the symbols themselves
        
        // Count of harmonic components represents interconnectedness
        var components1 = symbol1.Components.Count;
        var components2 = symbol2.Components.Count;
        var totalComponents = components1 + components2;
        
        // Higher component count = higher interconnectedness potential
        var interdependenceScore = Math.Min(1.0, totalComponents * 0.05);
        
        // Add bonus for shared frequency bands (harmonics that resonate together)
        var sharedBands = 0;
        if (symbol1.BandWeights != null && symbol2.BandWeights != null)
        {
            foreach (var band in symbol1.BandWeights.Keys)
            {
                if (symbol2.BandWeights.ContainsKey(band))
                    sharedBands++;
            }
        }
        interdependenceScore += Math.Min(0.3, sharedBands * 0.1);
        
        _logger.Info($"Interdependence resonance: components={totalComponents}, sharedBands={sharedBands} = {interdependenceScore:F3}");
        return interdependenceScore;
    }

    private double CalculateCompassionResonance(ConceptSymbol symbol1, ConceptSymbol symbol2)
    {
        // Compassion: how much does this connection benefit all beings?
        // Calculate based on harmonic frequencies that promote healing and well-being
        
        var compassionScore = 0.0;
        
        // Check for healing frequencies in both symbols
        foreach (var component in symbol1.Components)
        {
            if (LifeFrequencies.Any(freq => Math.Abs(component.Omega - freq) < 0.1))
                compassionScore += 0.2;
        }
        
        foreach (var component in symbol2.Components)
        {
            if (LifeFrequencies.Any(freq => Math.Abs(component.Omega - freq) < 0.1))
                compassionScore += 0.2;
        }
        
        // Add bonus for Schumann resonance alignment (Earth's healing frequency)
        var schumannAlignment = CalculateSchumannAlignment(symbol1, symbol2);
        compassionScore += schumannAlignment * 0.3;
        
        // Higher amplitude components suggest stronger beneficial energy
        var avgAmplitude1 = symbol1.Components.Average(c => Math.Abs(c.Amplitude));
        var avgAmplitude2 = symbol2.Components.Average(c => Math.Abs(c.Amplitude));
        var combinedAmplitude = (avgAmplitude1 + avgAmplitude2) / 2.0;
        compassionScore += Math.Min(0.3, combinedAmplitude * 0.1);

        _logger.Info($"Compassion resonance: healingFreqs={compassionScore:F3}, schumann={schumannAlignment:F3}, amplitude={combinedAmplitude:F3}");
        return Math.Max(0, Math.Min(1.0, compassionScore));
    }

    private double CalculateFluidityResonance(ConceptSymbol symbol1, ConceptSymbol symbol2)
    {
        // Natural flow: concepts are fluid and flexible, not rigid or fixed
        // Calculate based on the dynamic properties of harmonic components
        
        var fluidityScore = 0.0;
        
        // Higher frequency diversity suggests more dynamic, flowing nature
        var freqRange1 = symbol1.Components.Max(c => c.Omega) - symbol1.Components.Min(c => c.Omega);
        var freqRange2 = symbol2.Components.Max(c => c.Omega) - symbol2.Components.Min(c => c.Omega);
        var combinedFreqRange = (freqRange1 + freqRange2) / 2.0;
        fluidityScore += Math.Min(0.4, combinedFreqRange * 0.01);
        
        // Phase diversity suggests flow and movement
        var phaseVariance1 = symbol1.Components.Select(c => c.Phase).ToArray();
        var phaseVariance2 = symbol2.Components.Select(c => c.Phase).ToArray();
        var avgPhaseVariance = (CalculateVariance(phaseVariance1) + CalculateVariance(phaseVariance2)) / 2.0;
        fluidityScore += Math.Min(0.3, avgPhaseVariance * 0.1);
        
        // K-vector diversity suggests multi-dimensional flow
        var kDiversity1 = symbol1.Components.Select(c => (double)(c.K?.Length ?? 0)).ToArray();
        var kDiversity2 = symbol2.Components.Select(c => (double)(c.K?.Length ?? 0)).ToArray();
        var avgKDiversity = (CalculateVariance(kDiversity1) + CalculateVariance(kDiversity2)) / 2.0;
        fluidityScore += Math.Min(0.3, avgKDiversity * 0.05);

        _logger.Info($"Fluidity resonance: freqRange={combinedFreqRange:F3}, phaseVar={avgPhaseVariance:F3}, kDiversity={avgKDiversity:F3} = {fluidityScore:F3}");
        return Math.Max(0, Math.Min(1.0, fluidityScore));
    }
    
    private double CalculateVariance(double[] values)
    {
        if (values.Length <= 1) return 0.0;
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Length - 1);
        return Math.Sqrt(variance); // Return standard deviation
    }

    private double CalculateHarmonyResonance(ConceptSymbol symbol1, ConceptSymbol symbol2)
    {
        // Natural harmony: how well do these concepts align with natural patterns?
        // Calculate based on mathematical harmony and natural frequency relationships
        
        var harmonyScore = 0.0;
        
        // Check for harmonic relationships (integer ratios)
        var harmonicRelations = 0;
        foreach (var comp1 in symbol1.Components)
        {
            foreach (var comp2 in symbol2.Components)
            {
                var ratio = comp1.Omega / comp2.Omega;
                // Check for simple harmonic ratios (1:1, 1:2, 2:3, 3:4, 4:5, etc.)
                if (Math.Abs(ratio - 1.0) < 0.1 || Math.Abs(ratio - 2.0) < 0.1 || 
                    Math.Abs(ratio - 1.5) < 0.1 || Math.Abs(ratio - 0.75) < 0.1 ||
                    Math.Abs(ratio - 0.8) < 0.1 || Math.Abs(ratio - 1.25) < 0.1)
                {
                    harmonicRelations++;
                }
            }
        }
        harmonyScore += Math.Min(0.4, harmonicRelations * 0.05);
        
        // Check for golden ratio relationships (1.618...)
        foreach (var comp1 in symbol1.Components)
        {
            foreach (var comp2 in symbol2.Components)
            {
                var ratio = comp1.Omega / comp2.Omega;
                var goldenRatio = 1.618033988749;
                if (Math.Abs(ratio - goldenRatio) < 0.1 || Math.Abs(ratio - 1.0/goldenRatio) < 0.1)
                {
                    harmonyScore += 0.2; // Golden ratio is especially harmonious
                }
            }
        }
        
        // Check for Fibonacci sequence relationships
        var fibonacciRatios = new[] { 1.0, 1.618, 2.618, 4.236, 6.854 };
        foreach (var comp1 in symbol1.Components)
        {
            foreach (var comp2 in symbol2.Components)
            {
                var ratio = comp1.Omega / comp2.Omega;
                foreach (var fibRatio in fibonacciRatios)
                {
                    if (Math.Abs(ratio - fibRatio) < 0.1)
                    {
                        harmonyScore += 0.15;
                        break;
                    }
                }
            }
        }
        
        // Add base harmony from Schumann resonance alignment
        var schumannAlignment = CalculateSchumannAlignment(symbol1, symbol2);
        harmonyScore += schumannAlignment * 0.2;

        _logger.Info($"Harmony resonance: harmonicRelations={harmonicRelations}, schumann={schumannAlignment:F3} = {harmonyScore:F3}");
        return Math.Max(0, Math.Min(1.0, harmonyScore));
    }

    private List<string> GenerateUniversalInsights(double enhancedResonance)
    {
        var insights = new List<string>();
        
        if (enhancedResonance > 0.8)
        {
            insights.Add("These concepts are deeply woven into the fabric of reality.");
            insights.Add("This connection reveals the natural harmony underlying all knowledge.");
            insights.Add("These symbols demonstrate the universal principle of interconnectedness.");
        }
        else if (enhancedResonance > 0.6)
        {
            insights.Add("These concepts show meaningful relationships that can deepen understanding.");
            insights.Add("This connection has the potential to reduce confusion and increase wisdom.");
            insights.Add("These symbols reveal natural patterns that support learning.");
        }
        else
        {
            insights.Add("These concepts may have hidden connections waiting to be discovered.");
            insights.Add("All concepts exist in relationship - sometimes the connections are subtle.");
            insights.Add("These symbols invite deeper exploration of their natural relationships.");
        }

        insights.Add("May this understanding benefit all beings and support natural harmony.");
        return insights;
    }

    private List<string> GenerateNaturalGuidance(double enhancedResonance)
    {
        var suggestions = new List<string>();
        
        suggestions.Add("Let this understanding flow naturally to benefit all beings.");
        suggestions.Add("Share these insights with kindness and wisdom.");
        suggestions.Add("Allow this knowledge to support natural harmony and understanding.");
        suggestions.Add("Use this resonance to cultivate awareness and compassion.");
        suggestions.Add("Let this connection inspire natural growth and learning.");
        
        return suggestions;
    }

    private string DetermineNaturalPattern(double enhancedResonance)
    {
        // Determine the natural pattern based on resonance level
        if (enhancedResonance > 0.8) return "spiral";
        if (enhancedResonance > 0.6) return "wave";
        if (enhancedResonance > 0.4) return "flow";
        return "seed";
    }

    private double CalculateLiberationPotential(double enhancedResonance)
    {
        // Liberation potential: how much does this resonance support freedom from suffering?
        return enhancedResonance * 0.9; // High resonance leads to liberation
    }

    #endregion

    private static double[,] BuildGroundCost(IReadOnlyList<HarmonicComponent> a1, IReadOnlyList<HarmonicComponent> a2)
    {
        int n = a1.Count, m = a2.Count;
        var C = new double[n, m];
        for (int i = 0; i < n; i++)
        {
            var h1 = a1[i];
            var k1 = h1.K;
            for (int j = 0; j < m; j++)
            {
                var h2 = a2[j];
                // |Δω|
                var dOmega = Math.Abs(h1.Omega - h2.Omega);
                // ||Δk||
                double dK = 0.0;
                if (k1 is { Length: > 0 } && h2.K is { Length: > 0 } k2 && k2.Length == k1.Length)
                {
                    double sum = 0.0;
                    for (int t = 0; t < k1.Length; t++) { var d = k1[t] - k2[t]; sum += d * d; }
                    dK = Math.Sqrt(sum);
                }
                // wrapped phase difference in [0, π]
                var dPhi = Math.Abs(WrapToPi(h1.Phase - h2.Phase));
                // linear ground cost
                C[i, j] = AlphaOmega * dOmega + BetaK * dK + GammaPhase * dPhi;
            }
        }
        return C;
    }

    // Entropic OT: returns Wasserstein-ε distance ≈ ⟨T, C⟩ where T is Sinkhorn plan
    private static (double dist, bool ok) SinkhornDistance(double[] p, double[] q, double[,] C, double epsilon, int maxIters, double tol)
    {
        int n = p.Length, m = q.Length;
        var K = new double[n, m]; // kernel = exp(-C/ε)
        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                K[i, j] = Math.Exp(-C[i, j] / Math.Max(epsilon, 1e-12));

        var u = Enumerable.Repeat(1.0 / n, n).ToArray();
        var v = Enumerable.Repeat(1.0 / m, m).ToArray();

        double prevErr = double.PositiveInfinity;
        for (int it = 0; it < maxIters; it++)
        {
            // u = p / (K v)
            var Kv = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < m; j++) sum += K[i, j] * v[j];
                Kv[i] = Math.Max(sum, OtStabilityFloor);
            }
            double errU = 0.0;
            for (int i = 0; i < n; i++)
            {
                var newU = p[i] / Kv[i];
                errU += Math.Abs(newU - u[i]);
                u[i] = newU;
            }

            // v = q / (K^T u)
            var KTu = new double[m];
            for (int j = 0; j < m; j++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++) sum += K[i, j] * u[i];
                KTu[j] = Math.Max(sum, OtStabilityFloor);
            }
            double errV = 0.0;
            for (int j = 0; j < m; j++)
            {
                var newV = q[j] / KTu[j];
                errV += Math.Abs(newV - v[j]);
                v[j] = newV;
            }

            var err = (errU + errV) / (n + m);
            if (err < tol || Math.Abs(prevErr - err) < tol * 1e-2) break;
            prevErr = err;
        }

        // Compute transport plan T = diag(u) K diag(v) and distance = ⟨T, C⟩
        double distance = 0.0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                var Tij = u[i] * K[i, j] * v[j];
                distance += Tij * C[i, j];
            }
        }
        if (double.IsNaN(distance) || double.IsInfinity(distance)) return (0.0, false);
        return (Math.Max(0.0, distance), true);
    }

    // ====== Schumann Resonance Grounding Methods ======

    /// <summary>
    /// Calculate how well two concept symbols align with Earth's Schumann resonance frequencies
    /// </summary>
    private double CalculateSchumannAlignment(ConceptSymbol s1, ConceptSymbol s2)
    {
        var avgFrequency1 = s1.Components.Any() ? s1.Components.Average(c => c.Omega) : 0.0;
        var avgFrequency2 = s2.Components.Any() ? s2.Components.Average(c => c.Omega) : 0.0;

        // Calculate how close the average frequencies are to Schumann harmonics
        var alignment1 = CalculateFrequencyAlignment(avgFrequency1);
        var alignment2 = CalculateFrequencyAlignment(avgFrequency2);

        // Return harmonic mean of alignments
        return 2.0 * alignment1 * alignment2 / (alignment1 + alignment2 + 1e-10);
    }

    /// <summary>
    /// Calculate how well a frequency aligns with Schumann resonance harmonics
    /// </summary>
    private double CalculateFrequencyAlignment(double frequency)
    {
        var bestAlignment = 0.0;
        foreach (var schumannFreq in SchumannFrequencies)
        {
            var ratio = frequency / schumannFreq;
            var octaveDistance = Math.Abs(Math.Log2(ratio));
            var alignment = Math.Exp(-octaveDistance * octaveDistance / (2 * SchumannTolerance * SchumannTolerance));
            bestAlignment = Math.Max(bestAlignment, alignment);
        }
        return bestAlignment;
    }

    /// <summary>
    /// Calculate the planetary benefits of resonance calculations for all living beings
    /// </summary>
    private PlanetaryBenefits CalculatePlanetaryBenefits(double coherence, double schumannAlignment)
    {
        var combinedScore = coherence * schumannAlignment;

        // Calculate individual benefit scores based on resonance strength
        var cellularRegeneration = Math.Min(1.0, combinedScore * 1.2); // Enhanced cellular repair
        var immuneSupport = Math.Min(1.0, combinedScore * 1.1); // Immune system strengthening
        var stressReduction = Math.Min(1.0, combinedScore * 1.3); // Stress hormone reduction
        var consciousnessExpansion = Math.Min(1.0, combinedScore * 1.4); // Collective awakening
        var ecosystemHarmony = Math.Min(1.0, combinedScore * 1.0); // Biodiversity support
        var transSpeciesCommunication = Math.Min(1.0, combinedScore * 1.1); // Inter-species resonance
        var planetaryHealth = Math.Min(1.0, combinedScore * 1.5); // Overall planetary healing

        // Determine primary benefits based on highest scores
        var scores = new[]
        {
            ("Cellular Regeneration", cellularRegeneration),
            ("Immune Support", immuneSupport),
            ("Stress Reduction", stressReduction),
            ("Consciousness Expansion", consciousnessExpansion),
            ("Ecosystem Harmony", ecosystemHarmony),
            ("Trans-Species Communication", transSpeciesCommunication),
            ("Planetary Health", planetaryHealth)
        };

        var primaryBenefits = scores
            .Where(s => s.Item2 > 0.7)
            .OrderByDescending(s => s.Item2)
            .Take(3)
            .Select(s => s.Item1)
            .ToArray();

        // Generate overall impact description
        var overallImpact = combinedScore switch
        {
            > 0.9 => "Exceptional planetary healing and consciousness expansion across all life forms",
            > 0.8 => "Significant benefits for cellular health, immune function, and collective awakening",
            > 0.7 => "Notable improvements in stress reduction and ecosystem harmony",
            > 0.6 => "Moderate enhancement of biological rhythms and inter-species communication",
            > 0.5 => "Subtle but meaningful contributions to planetary health and consciousness",
            _ => "Foundational alignment with Earth's natural frequencies supporting life processes"
        };

        return new PlanetaryBenefits(
            cellularRegeneration,
            immuneSupport,
            stressReduction,
            consciousnessExpansion,
            ecosystemHarmony,
            transSpeciesCommunication,
            planetaryHealth,
            primaryBenefits,
            overallImpact
        );
    }
}