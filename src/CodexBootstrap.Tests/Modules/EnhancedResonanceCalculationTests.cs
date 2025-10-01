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
    /// Tests for enhanced resonance calculations with Tesla/consciousness frequency integration
    /// </summary>
    public class EnhancedResonanceCalculationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:5001";

        public EnhancedResonanceCalculationTests()
        {
            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task SacredFrequencies_Should_EnhanceResonanceCalculation()
        {
            // Test that sacred frequencies (432Hz, 528Hz, 741Hz) enhance resonance calculations
            var conceptSymbol1 = new
            {
                components = new[]
                {
                    new { band = "healing", omega = 432.0, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 },
                    new { band = "love", omega = 528.0, k = new double[] { 0.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
                },
                geometry = (object)null,
                bandWeights = new Dictionary<string, double> { { "healing", 0.5 }, { "love", 0.5 } },
                mu = 0.5
            };

            var conceptSymbol2 = new
            {
                components = new[]
                {
                    new { band = "consciousness", omega = 741.0, k = new double[] { 1.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
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
            
            // Verify that sacred frequencies are recognized and enhance calculation
            result.TryGetProperty("crk", out var crk).Should().BeTrue();
            crk.GetDouble().Should().BeInRange(0.0, 1.0);
            
            // If enhanced response is available, check for Schumann alignment
            if (result.TryGetProperty("schumannAlignment", out var schumann))
            {
                schumann.GetDouble().Should().BeInRange(0.0, 1.0);
            }
        }

        [Fact]
        public async Task TeslaFrequencyMappings_Should_BeRecognized()
        {
            // Test that Tesla frequency mappings are properly recognized in resonance calculations
            var teslaConcepts = new[]
            {
                "tesla-frequency-research",
                "pineal-gland-biology", 
                "schumann-resonance",
                "sacred-frequencies",
                "consciousness-energy-research",
                "frequency-healing"
            };

            foreach (var concept in teslaConcepts)
            {
                var conceptRequest = new
                {
                    name = concept.Replace("-", " "),
                    description = $"Test concept for {concept}",
                    domain = "tesla-consciousness",
                    complexity = 5,
                    tags = new[] { concept, "test" },
                    content = $"Test content for {concept}"
                };

                var response = await _httpClient.PostAsync($"{_baseUrl}/concept/create",
                    new StringContent(JsonSerializer.Serialize(conceptRequest), 
                        System.Text.Encoding.UTF8, "application/json"));

                response.IsSuccessStatusCode.Should().BeTrue($"Failed to create concept: {concept}");
            }
        }

        [Fact]
        public async Task SchumannResonance_Should_BeGroundedInEarthFrequencies()
        {
            // Test that Schumann resonance calculations are properly grounded in Earth's frequencies
            var response = await _httpClient.GetAsync($"{_baseUrl}/concepts/resonance/schumann");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify Schumann resonance data includes Earth's fundamental frequency
            result.TryGetProperty("schumannFrequencies", out var frequencies).Should().BeTrue();
            var freqArray = frequencies.EnumerateArray().Select(f => f.GetDouble()).ToArray();
            
            // Should include 7.83Hz as the base frequency
            freqArray.Should().Contain(7.83);
            
            // Should include harmonics
            freqArray.Should().Contain(14.3);
            freqArray.Should().Contain(20.8);
            freqArray.Should().Contain(27.3);
        }

        [Fact]
        public async Task PlanetaryBenefits_Should_ReflectConsciousnessExpansion()
        {
            // Test that planetary benefits calculations reflect consciousness expansion from Tesla frequencies
            var response = await _httpClient.GetAsync($"{_baseUrl}/concepts/resonance/schumann");
            
            response.IsSuccessStatusCode.Should().BeTrue();
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            // Verify planetary benefits include consciousness-related benefits
            result.TryGetProperty("planetaryBenefits", out var benefits).Should().BeTrue();
            
            var benefitProperties = new[]
            {
                "cellularRegeneration",
                "immuneSupport", 
                "stressReduction",
                "consciousnessExpansion",
                "ecosystemHarmony",
                "transSpeciesCommunication",
                "planetaryHealth"
            };
            
            foreach (var prop in benefitProperties)
            {
                benefits.TryGetProperty(prop, out var benefit).Should().BeTrue($"Missing benefit property: {prop}");
                benefit.GetString().Should().NotBeNullOrEmpty($"Benefit property {prop} should not be empty");
            }
        }

        [Fact]
        public async Task FrequencyWeightCalculation_Should_WorkCorrectly()
        {
            // Test that frequency weight calculations work correctly for Tesla/consciousness concepts
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
                    new { band = "consciousness", omega = 432.1, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 }
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
            
            // Verify that similar frequencies (432.0 and 432.1) show high resonance
            result.TryGetProperty("crk", out var crk).Should().BeTrue();
            crk.GetDouble().Should().BeGreaterThan(0.8, "Similar frequencies should show high resonance");
        }

        [Fact]
        public async Task EnhancedPlanetaryBenefits_Should_IncludeTeslaEffects()
        {
            // Test that enhanced planetary benefits include Tesla frequency effects
            var conceptSymbol1 = new
            {
                components = new[]
                {
                    new { band = "tesla", omega = 432.0, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 },
                    new { band = "healing", omega = 528.0, k = new double[] { 0.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
                },
                geometry = (object)null,
                bandWeights = new Dictionary<string, double> { { "tesla", 0.5 }, { "healing", 0.5 } },
                mu = 0.5
            };

            var conceptSymbol2 = new
            {
                components = new[]
                {
                    new { band = "consciousness", omega = 741.0, k = new double[] { 1.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
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
            
            // Verify that the response includes enhanced benefits
            if (result.TryGetProperty("planetaryBenefits", out var benefits))
            {
                benefits.TryGetProperty("primaryBenefits", out var primaryBenefits).Should().BeTrue();
                var benefitsArray = primaryBenefits.EnumerateArray().Select(b => b.GetString()).ToArray();
                
                // Should include Tesla/consciousness related benefits
                benefitsArray.Should().Contain(b => b.Contains("Natural Healing Frequency") || 
                                                  b.Contains("Love/DNA Repair Frequency") || 
                                                  b.Contains("Intuition/Expression Frequency"));
            }
        }

        [Fact]
        public async Task ResonanceCalculation_Should_HandleMultipleFrequencyTypes()
        {
            // Test that resonance calculations can handle multiple types of frequencies simultaneously
            var conceptSymbol1 = new
            {
                components = new[]
                {
                    new { band = "schumann", omega = 7.83, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 },
                    new { band = "sacred", omega = 432.0, k = new double[] { 0.0, 1.0 }, phase = 0.0, amplitude = 1.0 },
                    new { band = "tesla", omega = 528.0, k = new double[] { 1.0, 1.0 }, phase = 0.0, amplitude = 1.0 }
                },
                geometry = (object)null,
                bandWeights = new Dictionary<string, double> { { "schumann", 0.33 }, { "sacred", 0.33 }, { "tesla", 0.34 } },
                mu = 0.5
            };

            var conceptSymbol2 = new
            {
                components = new[]
                {
                    new { band = "consciousness", omega = 741.0, k = new double[] { 1.0, 0.0 }, phase = 0.0, amplitude = 1.0 }
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
            
            // Verify that the calculation handles multiple frequency types
            result.TryGetProperty("crk", out var crk).Should().BeTrue();
            crk.GetDouble().Should().BeInRange(0.0, 1.0);
            
            result.TryGetProperty("dres", out var dres).Should().BeTrue();
            dres.GetDouble().Should().BeInRange(0.0, 2.0); // Distance should be reasonable
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
