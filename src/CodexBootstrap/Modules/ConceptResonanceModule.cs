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

[MetaNode(Id = "codex.resonance", Name = "Concept Resonance Module", Description = "Harmonic symbols and resonance metrics (CRK + optional OT-phi)")]
public sealed class ConceptResonanceModule : IModule
{
    private readonly NodeRegistry _registry;
    private readonly CodexBootstrap.Core.ILogger _logger;

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

    public ConceptResonanceModule(NodeRegistry registry, CodexBootstrap.Core.ILogger logger /* HttpClient removed */)
    {
        _registry = registry;
        _logger = logger;
    }
    
    // Parameterless constructor for module loader
    public ConceptResonanceModule() : this(new NodeRegistry(), new Log4NetLogger(typeof(ConceptResonanceModule)))
    {
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.resonance",
            name: "Concept Resonance Module",
            version: "1.2.0",
            description: "Compare concepts via harmonic symbols using CRK and inline OT-phi",
            capabilities: new[] { "resonance", "harmonics", "similarity" },
            tags: new[] { "crk", "ot-phi", "concepts" },
            specReference: "codex.spec.concepts.resonance"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());

        var compareApi = NodeStorage.CreateApiNode("codex.resonance", "compare", "/concepts/resonance/compare", "Compare two concept symbols (CRK + OT-phi)");
        var encodeApi = NodeStorage.CreateApiNode("codex.resonance", "encode", "/concepts/resonance/encode", "Store a concept symbol as a node");
        registry.Upsert(compareApi);
        registry.Upsert(encodeApi);
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.resonance", "compare"));
        registry.Upsert(NodeStorage.CreateModuleApiEdge("codex.resonance", "encode"));
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
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

    [ApiRoute("POST", "/concepts/resonance/compare", "Compare Concepts", "Compare two concept symbols using CRK and inline OT-phi", "codex.resonance")]
    public async Task<object> CompareConceptsAsync([ApiParameter("request", "Resonance comparison request", Required = true, Location = "body")] ResonanceCompareRequest request)
    {
        try
        {
            var crk = ComputeCrk(request.S1, request.S2, request.TauGrid);
            var dRes = Math.Sqrt(Math.Max(0.0, 1.0 - crk * crk));

            var (dOt, usedOt) = ComputeOtPhiInline(request.S1, request.S2);
            var coherence = crk * Math.Exp(-dOt);
            var dCodex = 1.0 - coherence;

            return new ResonanceCompareResponse(crk, dRes, dOt, coherence, dCodex, usedOt);
        }
        catch (Exception ex)
        {
            _logger.Error($"Resonance compare failed: {ex.Message}");
            return new ErrorResponse("Resonance compare failed");
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

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
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
}