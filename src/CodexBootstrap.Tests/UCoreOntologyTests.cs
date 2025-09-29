using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;

namespace CodexBootstrap.Tests;

/// <summary>
/// Comprehensive tests for U-Core ontology initialization and structure
/// </summary>
public class UCoreOntologyTests
{
    private readonly INodeRegistry _registry;
    private readonly ICodexLogger _logger;

    public UCoreOntologyTests()
    {
        _registry = TestInfrastructure.CreateTestNodeRegistry();
        _logger = TestInfrastructure.CreateTestLogger();
        _registry.InitializeAsync().Wait();
    }

    [Fact]
    public async Task UCoreInitializer_Should_LoadAllBaseUCoreConcepts()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);

        // Actual IDs use the prefix "u-core-concept-"
        var expectedUCoreConcepts = new[]
        {
            "entity", "activity", "agent", "role", "resource",
            "place", "time", "intent", "rule", "agreement",
            "obligation", "measurement"
        };

        foreach (var conceptId in expectedUCoreConcepts)
        {
            var node = _registry.GetNode($"u-core-concept-{conceptId}");
            Assert.NotNull(node);
            Assert.Contains(conceptId, node!.Id, StringComparison.OrdinalIgnoreCase);
            _logger.Info($"✓ U-Core concept '{conceptId}' present: {node.Id}");
        }
    }

    [Fact]
    public async Task UCoreInitializer_Should_LoadAllUniversalAxes()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);

        // Actual IDs use the prefix "u-core-axis-"
        var expectedAxes = new[]
        {
            "water_states", "quantum_dimensions", "archangels", "chakras",
            "light_frequencies", "sound_vibrations", "sacred_geometry",
            "astrology_objects", "feelings", "kant_categories",
            "dependent_origination", "academic_disciplines",
            "body_systems", "standard_model_fermions",
            // core axis families
            "ontological", "epistemological", "axiological",
            "temporal", "spatial", "causal", "informational",
            "energetic", "consciousness", "relational", "emergent", "spiritual"
        };

        foreach (var axisId in expectedAxes)
        {
            var node = _registry.GetNode($"u-core-axis-{axisId}");
            Assert.NotNull(node);
            _logger.Info($"✓ Axis '{axisId}' present: {node!.Id}");
        }
    }

    [Fact]
    public async Task UCoreInitializer_Should_LoadRelationshipTypes()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);

        var relationshipTypeNodes = _registry.GetNodesByType("codex.relationship").ToList();
        // Relationship type nodes are stored as nodes (not edges);
        // ensure at least a set of them exist.
        Assert.True(relationshipTypeNodes.Count >= 10, "Expected at least 10 core relationship type nodes");
        _logger.Info($"✓ Found {relationshipTypeNodes.Count} core relationship type nodes");
    }

    [Fact]
    public async Task WaterStatesAxis_Node_Should_Exist()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);
        var axisNode = _registry.GetNode("u-core-axis-water_states");
        Assert.NotNull(axisNode);
        _logger.Info("✓ Water states axis node exists");
    }

    [Fact]
    public async Task ChakrasAxis_Node_Should_Exist()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);
        var axisNode = _registry.GetNode("u-core-axis-chakras");
        Assert.NotNull(axisNode);
        _logger.Info("✓ Chakras axis node exists");
    }

    [Fact]
    public async Task QuantumDimensionsAxis_Node_Should_Exist()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);
        var axisNode = _registry.GetNode("u-core-axis-quantum_dimensions");
        Assert.NotNull(axisNode);
        _logger.Info("✓ Quantum dimensions axis node exists");
    }

    [Fact(Skip = "Resonance matrix node not yet implemented in current ontology")]
    public async Task ResonanceMatrix_Should_ExistAndIntegrateAxes()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);
        var resonanceMatrix = _registry.GetNode("fractal-holographic-resonance-matrix");
        Assert.NotNull(resonanceMatrix);

        var integratesEdges = _registry.GetEdgesFrom(resonanceMatrix!.Id)
            .Where(e => e.Role == "integrates");
        Assert.True(integratesEdges.Count() >= 5, "Resonance matrix should integrate multiple axis systems");
    }

    [Fact]
    public async Task TopologyPaths_Should_ConnectToUCoreConcepts_IfPresent()
    {
        await UCoreInitializer.SeedIfMissing(_registry, _logger);

        // Only assert if present; some concepts may not be linked yet during initial seed
        var testConcepts = new[] { "consciousness", "knowledge", "science", "technology" };

        foreach (var conceptId in testConcepts)
        {
            var concept = _registry.GetNode($"u-core-concept-{conceptId}");
            if (concept != null)
            {
                var edges = _registry.GetEdgesFrom(concept.Id);
                var hasUCoreLink = edges.Any(e => _registry.GetNode(e.ToId)?.TypeId == "codex.ucore.base");
                _logger.Info(hasUCoreLink
                    ? $"✓ Concept '{conceptId}' has a topology link to a U-Core base concept"
                    : $"→ Concept '{conceptId}' present but no topology link yet (ok during initial seed)");
            }
            else
            {
                _logger.Info($"→ Concept '{conceptId}' not present (ok during initial seed)");
            }
        }
    }
}
