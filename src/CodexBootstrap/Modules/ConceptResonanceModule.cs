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
    private readonly HttpClient _http;

    public ConceptResonanceModule(NodeRegistry registry, CodexBootstrap.Core.ILogger logger, HttpClient http)
    {
        _registry = registry;
        _logger = logger;
        _http = http;
    }

    // Parameterless constructor for module loader
    public ConceptResonanceModule() : this(new NodeRegistry(), new Log4NetLogger(typeof(ConceptResonanceModule)), new HttpClient())
    {
    }

    public Node GetModuleNode()
    {
        return NodeStorage.CreateModuleNode(
            id: "codex.resonance",
            name: "Concept Resonance Module",
            version: "1.0.0",
            description: "Compare concepts via harmonic symbols using CRK and optional OT-phi",
            capabilities: new[] { "resonance", "harmonics", "similarity" },
            tags: new[] { "crk", "ot-phi", "concepts" },
            specReference: "codex.spec.concepts.resonance"
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());

        var compareApi = NodeStorage.CreateApiNode("codex.resonance", "compare", "/concepts/resonance/compare", "Compare two concept symbols (CRK + optional OT-phi)");
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

    [ApiRoute("POST", "/concepts/resonance/compare", "Compare Concepts", "Compare two concept symbols using CRK and optional OT-phi", "codex.resonance")]
    public async Task<object> CompareConceptsAsync([ApiParameter("request", "Resonance comparison request", Required = true, Location = "body")] ResonanceCompareRequest request)
    {
        try
        {
            var crk = ComputeCrk(request.S1, request.S2, request.TauGrid);
            var dRes = Math.Sqrt(Math.Max(0.0, 1.0 - crk * crk));

            var (dOt, usedOt) = await TryComputeOtPhiAsync(request.S1, request.S2);
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

    private double ComputeCrk(ConceptSymbol s1, ConceptSymbol s2, double[]? tauGrid)
    {
        var mu = Math.Max(0.0, s1.Mu ?? s2.Mu ?? 0.0);
        var (psi1, psi2) = (s1.Components, s2.Components);
        var (bw1, bw2) = (s1.BandWeights ?? new(), s2.BandWeights ?? new());

        double best = 0.0;
        var taus = tauGrid ?? new[] { 0.0 };
        foreach (var tau in taus)
        {
            double numReal = 0.0, numImag = 0.0;
            double norm1 = 0.0, norm2 = 0.0;

            foreach (var h1 in psi1)
            {
                var w = GetBandWeight(h1.Band, bw1, bw2);
                var a1 = h1.Amplitude;
                norm1 += w * a1 * a1;
                foreach (var h2 in psi2)
                {
                    if (!BandMatch(h1.Band, h2.Band)) continue;
                    // same (approx) frequency bin
                    if (Math.Abs(h1.Omega - h2.Omega) > 1e-6) continue;
                    var a2 = h2.Amplitude;
                    // complex product with phase diff and lag
                    var phase = (h1.Phase - h2.Phase) - h1.Omega * tau;
                    numReal += w * a1 * a2 * Math.Cos(phase);
                    numImag += w * a1 * a2 * Math.Sin(phase);
                }
            }
            foreach (var h2 in psi2)
            {
                var w = GetBandWeight(h2.Band, bw1, bw2);
                var a2 = h2.Amplitude;
                norm2 += w * a2 * a2;
            }

            // geometric sub-symbol contribution
            double geoNum = 0.0, geoDen = 0.0;
            if (s1.Geometry?.G != null && s2.Geometry?.G != null && s1.Geometry.G.Length == s2.Geometry.G.Length)
            {
                var g1 = s1.Geometry.G; var g2 = s2.Geometry.G;
                var dot = 0.0; var n1 = 0.0; var n2 = 0.0;
                for (int i = 0; i < g1.Length; i++) { dot += g1[i] * g2[i]; n1 += g1[i] * g1[i]; n2 += g2[i] * g2[i]; }
                geoNum = mu * dot;
                geoDen = mu * (Math.Sqrt(n1) * Math.Sqrt(n2));
            }

            var num = Math.Sqrt(numReal * numReal + numImag * numImag) + geoNum;
            var den = (Math.Sqrt(norm1) * Math.Sqrt(norm2)) + geoDen;
            if (den <= 0) continue;
            var score = Math.Clamp(num / den, 0.0, 1.0);
            if (score > best) best = score;
        }
        return best;
    }

    private static bool BandMatch(string b1, string b2)
    {
        return string.Equals(b1, b2, StringComparison.OrdinalIgnoreCase);
    }

    private static double GetBandWeight(string band, Dictionary<string, double> bw1, Dictionary<string, double> bw2)
    {
        if (bw1.TryGetValue(band, out var w1)) return w1;
        if (bw2.TryGetValue(band, out var w2)) return w2;
        return band.Equals("low", StringComparison.OrdinalIgnoreCase) ? 1.0 : band.Equals("mid", StringComparison.OrdinalIgnoreCase) ? 1.0 : 1.0;
    }

    private async Task<(double dOtPhi, bool used)> TryComputeOtPhiAsync(ConceptSymbol s1, ConceptSymbol s2)
    {
        try
        {
            var otUrl = Environment.GetEnvironmentVariable("OT_SERVICE_URL");
            if (string.IsNullOrEmpty(otUrl)) return (0.0, false);
            var payload = JsonSerializer.Serialize(new { s1, s2 });
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(new Uri(new Uri(otUrl), "/otphi"), content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("distance", out var d))
            {
                return (Math.Max(0.0, d.GetDouble()), true);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"OT-phi call failed, using 0: {ex.Message}");
        }
        return (0.0, false);
    }
}
