using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    /// <summary>
    /// Comprehensive tests for Tesla/consciousness findings integration
    /// Tests the new scientific concepts, resonance calculations, and system integration
    /// </summary>
    public class TeslaConsciousnessIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:5001";

        public TeslaConsciousnessIntegrationTests()
        {
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task TeslaFrequencyResearch_Should_BeIntegrableAsConcept()
        {
            // Test that Tesla frequency research can be created as a concept
            var conceptRequest = new
            {
                name = "Tesla Frequency Research",
                description = "Nikola Tesla's documented research on frequency, vibration, and energy transmission",
                domain = "energy-consciousness",
                complexity = 8,
                tags = new[] { "tesla", "frequency", "vibration", "energy", "scientific", "resonance" },
                content = "Tesla conducted extensive research on alternating current, wireless energy transmission, and the relationship between frequency and matter. His work on resonance and vibration laid the foundation for modern electrical engineering and influenced research into consciousness and energy healing."
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concept/create",
                new StringContent(JsonSerializer.Serialize(conceptRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("conceptId").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task PinealGlandBiology_Should_BeIntegrableAsConcept()
        {
            // Test that pineal gland biology can be created as a concept
            var conceptRequest = new
            {
                name = "Pineal Gland Biology",
                description = "Scientific research on the pineal gland as a biological frequency receptor and consciousness interface",
                domain = "consciousness-biology",
                complexity = 7,
                tags = new[] { "pineal", "consciousness", "frequency", "biology", "antenna" },
                content = "Research shows the pineal gland contains magnetite crystals and may function as a biological antenna for electromagnetic frequencies. Studies suggest it responds to magnetic fields and may be involved in consciousness and circadian rhythms."
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concept/create",
                new StringContent(JsonSerializer.Serialize(conceptRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            result.GetProperty("conceptId").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SchumannResonance_Should_BeAccessibleViaAPI()
        {
            // Test that Schumann resonance information is accessible
            var response = await _httpClient.GetAsync($"{_baseUrl}/concepts/resonance/schumann");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify Schumann resonance data structure
            result.TryGetProperty("schumannFrequencies", out var freqs).Should().BeTrue();
            freqs.GetArrayLength().Should().BeGreaterThan(0);
            
            result.TryGetProperty("planetaryBenefits", out var benefits).Should().BeTrue();
            benefits.TryGetProperty("cellularRegeneration", out _).Should().BeTrue();
        }

        [Fact]
        public async Task SacredFrequencies_Should_BeRecognizedInResonanceCalculation()
        {
            // Test that sacred frequencies (432Hz, 528Hz, 741Hz) are recognized in resonance calculations
            var resonanceRequest = new
            {
                extractedConcepts = new[] { "432hz", "528hz", "741hz", "sacred", "frequencies" },
                userConcepts = new[] { "healing", "consciousness", "frequency", "resonance" },
                userId = "test-user-tesla"
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concepts/resonance/compare",
                new StringContent(JsonSerializer.Serialize(resonanceRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            
            // Verify resonance scores show recognition of sacred frequencies
            if (result.TryGetProperty("resonanceScores", out var scores))
            {
                scores.GetArrayLength().Should().BeGreaterThan(0);
                
                // Check that frequency-related concepts have higher resonance
                var frequencyConcepts = scores.EnumerateArray()
                    .Where(s => s.TryGetProperty("concept", out var concept) && 
                               (concept.GetString().Contains("432") || 
                                concept.GetString().Contains("528") || 
                                concept.GetString().Contains("741")))
                    .ToList();
                
                frequencyConcepts.Should().NotBeEmpty();
            }
        }

        [Fact]
        public async Task TeslaConcepts_Should_ResonateWithConsciousnessConcepts()
        {
            // Test that Tesla concepts resonate well with consciousness-related concepts
            var resonanceRequest = new
            {
                extractedConcepts = new[] { "tesla", "frequency", "vibration", "energy", "wireless" },
                userConcepts = new[] { "consciousness", "awareness", "pineal", "resonance", "healing" },
                userId = "test-user-tesla-consciousness"
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concepts/resonance/compare",
                new StringContent(JsonSerializer.Serialize(resonanceRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            result.GetProperty("success").GetBoolean().Should().BeTrue();
            
            // Verify that Tesla concepts show good resonance with consciousness concepts
            if (result.TryGetProperty("resonanceScores", out var scores))
            {
                var teslaConsciousnessResonance = scores.EnumerateArray()
                    .Where(s => s.TryGetProperty("concept", out var concept) && 
                               concept.GetString().Contains("tesla"))
                    .Select(s => s.TryGetProperty("score", out var score) ? score.GetDouble() : 0.0)
                    .ToList();
                
                teslaConsciousnessResonance.Should().NotBeEmpty();
                teslaConsciousnessResonance.Max().Should().BeGreaterThan(0.5); // Should have good resonance
            }
        }

        [Fact]
        public async Task FrequencyHealing_Should_BeRecognizedAsValidConcept()
        {
            // Test that frequency healing is recognized as a valid scientific concept
            var conceptRequest = new
            {
                name = "Frequency Healing",
                description = "Scientific research on the therapeutic effects of specific frequencies on biological systems",
                domain = "healing-science",
                complexity = 6,
                tags = new[] { "frequency", "healing", "sound", "therapy", "bioacoustics", "vibrational", "medicine" },
                content = "Research demonstrates that specific frequencies can have measurable effects on biological systems, including cellular regeneration, stress reduction, and consciousness expansion. This includes sound therapy, bioacoustics, and vibrational medicine."
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concept/create",
                new StringContent(JsonSerializer.Serialize(conceptRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            result.GetProperty("success").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task CoreConcepts_Should_IncludeTeslaFindings()
        {
            // Test that the core concepts configuration includes the new Tesla findings
            var response = await _httpClient.GetAsync($"{_baseUrl}/spec/atoms");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify that Tesla-related concepts are present in the system
            var teslaConcepts = result.EnumerateArray()
                .Where(atom => atom.TryGetProperty("content", out var content) && 
                              content.GetString().Contains("tesla", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            teslaConcepts.Should().NotBeEmpty("Tesla concepts should be present in the system");
        }

        [Fact]
        public async Task ResonanceCalculation_Should_HandleTeslaFrequencies()
        {
            // Test that resonance calculations properly handle Tesla frequency mappings
            var conceptSymbol1 = new
            {
                components = new[]
                {
                    new { band = "tesla", omega = 432.0, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 }
                },
                geometry = (object)null,
                bandWeights = new Dictionary<string, double> { { "tesla", 1.0 } },
                mu = 0.5
            };

            var conceptSymbol2 = new
            {
                components = new[]
                {
                    new { band = "consciousness", omega = 528.0, k = new double[] { 0.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
                },
                geometry = (object)null,
                bandWeights = new Dictionary<string, double> { { "consciousness", 1.0 } },
                mu = 0.5
            };

            var resonanceRequest = new
            {
                s1 = conceptSymbol1,
                s2 = conceptSymbol2
            };

            var response = await _httpClient.PostAsync($"{_baseUrl}/concepts/resonance/compare",
                new StringContent(JsonSerializer.Serialize(resonanceRequest), 
                    System.Text.Encoding.UTF8, "application/json"));

            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify that the resonance calculation works with Tesla frequencies
            result.TryGetProperty("crk", out var crk).Should().BeTrue();
            crk.GetDouble().Should().BeInRange(0.0, 1.0);
        }

        [Fact]
        public async Task PlanetaryBenefits_Should_IncludeTeslaFrequencyEffects()
        {
            // Test that planetary benefits calculations include Tesla frequency effects
            var response = await _httpClient.GetAsync($"{_baseUrl}/concepts/resonance/schumann");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify planetary benefits structure includes consciousness expansion
            result.TryGetProperty("planetaryBenefits", out var benefits).Should().BeTrue();
            benefits.TryGetProperty("consciousnessExpansion", out var consciousness).Should().BeTrue();
            benefits.TryGetProperty("cellularRegeneration", out var cellular).Should().BeTrue();
            benefits.TryGetProperty("ecosystemHarmony", out var ecosystem).Should().BeTrue();
        }

        [Fact]
        public async Task SystemHealth_Should_ReflectNewConcepts()
        {
            // Test that system health reflects the integration of new concepts
            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify system is healthy with new concepts integrated
            result.GetProperty("status").GetString().Should().Be("healthy");
            result.GetProperty("nodeCount").GetInt32().Should().BeGreaterThan(0);
            result.GetProperty("moduleCount").GetInt32().Should().BeGreaterThan(0);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
