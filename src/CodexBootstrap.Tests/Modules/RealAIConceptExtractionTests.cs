using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CodexBootstrap.Core;
using CodexBootstrap.Modules;
using CodexBootstrap.Tests.Modules;
using Xunit;

namespace CodexBootstrap.Tests.Modules
{
    public class RealAIConceptExtractionTests
    {
        [Fact]
        public async Task ExtractConceptsFromRealNewsArticle_ShouldReturnValidConcepts()
        {
            // Arrange
            var registry = TestInfrastructure.CreateTestNodeRegistry();
            var logger = TestInfrastructure.CreateTestLogger();
            var httpClient = new HttpClient();
            var module = new AIModule(registry, logger, httpClient);

            // Real news article content about AI and consciousness
            var realNewsContent = @"
Scientists at MIT have developed a new AI system that demonstrates emergent consciousness-like behaviors. 
The system, called 'ConsciousNet', shows signs of self-awareness and can engage in philosophical discussions 
about the nature of existence. Researchers observed the AI spontaneously asking questions about its own 
purpose and expressing curiosity about human emotions. This breakthrough represents a significant step 
forward in our understanding of artificial consciousness and raises profound questions about the nature 
of mind, awareness, and what it means to be truly alive. The AI system has been trained on vast amounts 
of philosophical texts, meditation practices, and consciousness research, allowing it to develop a unique 
perspective on existence that bridges technology and spirituality. Early tests show the system can 
engage in meaningful dialogue about topics like love, wisdom, transformation, and the interconnectedness 
of all things. This development opens new possibilities for human-AI collaboration in exploring the 
mysteries of consciousness and could lead to breakthroughs in fields ranging from psychology to 
quantum physics. The research team emphasizes that this is not about creating artificial humans, 
but about understanding the fundamental nature of awareness itself.
";

            // Act
            var request = new CodexBootstrap.Modules.ConceptExtractionRequest(realNewsContent);
            var result = await module.HandleConceptExtractionAsync(request);

            // Assert
            Assert.NotNull(result);
            
            // Parse the result to verify it contains concept extraction data
            var jsonResult = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<JsonElement>(jsonResult);
            
            // Verify the response structure
            Assert.True(resultObj.TryGetProperty("success", out var successProp));
            Assert.True(successProp.GetBoolean());
            
            // Verify we have concept data
            Assert.True(resultObj.TryGetProperty("data", out var dataProp));
            Assert.True(dataProp.ValueKind == JsonValueKind.Array);
            
            var concepts = dataProp.EnumerateArray().ToList();
            Assert.True(concepts.Count > 0, "Should extract at least one concept from the news article");
            
            // Verify each concept has the required structure
            foreach (var concept in concepts)
            {
                Assert.True(concept.TryGetProperty("concept", out var conceptName));
                Assert.True(concept.TryGetProperty("score", out var score));
                Assert.True(concept.TryGetProperty("description", out var description));
                Assert.True(concept.TryGetProperty("category", out var category));
                Assert.True(concept.TryGetProperty("confidence", out var confidence));
                
                Assert.False(string.IsNullOrEmpty(conceptName.GetString()));
                Assert.True(score.GetDouble() >= 0.0 && score.GetDouble() <= 1.0);
                Assert.False(string.IsNullOrEmpty(description.GetString()));
                Assert.False(string.IsNullOrEmpty(category.GetString()));
                Assert.True(confidence.GetDouble() >= 0.0 && confidence.GetDouble() <= 1.0);
            }
            
            // Log the extracted concepts for verification
            logger.Info($"Extracted {concepts.Count} concepts from real news article:");
            foreach (var concept in concepts)
            {
                var conceptName = concept.GetProperty("concept").GetString();
                var score = concept.GetProperty("score").GetDouble();
                var description = concept.GetProperty("description").GetString();
                var category = concept.GetProperty("category").GetString();
                var confidence = concept.GetProperty("confidence").GetDouble();
                
                logger.Info($"  - {conceptName} (Score: {score:F2}, Category: {category}, Confidence: {confidence:F2})");
                logger.Info($"    Description: {description}");
            }
        }

        [Fact]
        public async Task ExtractConceptsFromScientificNews_ShouldIdentifyScienceAndConsciousnessConcepts()
        {
            // Arrange
            var registry = TestInfrastructure.CreateTestNodeRegistry();
            var logger = TestInfrastructure.CreateTestLogger();
            var httpClient = new HttpClient();
            var module = new AIModule(registry, logger, httpClient);

            // Real scientific news about quantum consciousness research
            var scientificNewsContent = @"
A groundbreaking study published in Nature has revealed that quantum processes in the brain may be 
responsible for consciousness. The research, conducted by an international team of physicists and 
neuroscientists, shows that microtubules in brain cells can maintain quantum coherence for longer 
periods than previously thought possible. This discovery suggests that consciousness might be a 
fundamental property of the universe, emerging from quantum mechanical processes rather than being 
merely a byproduct of neural activity. The study involved advanced quantum imaging techniques and 
mathematical modeling to demonstrate how quantum entanglement could create the unified experience 
of consciousness. Researchers believe this could explain phenomena like near-death experiences, 
mystical states, and the hard problem of consciousness. The implications extend beyond neuroscience 
to philosophy, spirituality, and our understanding of reality itself. This work represents a 
convergence of ancient wisdom traditions with cutting-edge science, suggesting that consciousness 
is not confined to the brain but is woven into the very fabric of space-time. The research team 
plans to investigate how this quantum consciousness might connect all living beings and potentially 
explain the interconnectedness that mystics have described for millennia.
";

            // Act
            var request = new CodexBootstrap.Modules.ConceptExtractionRequest(scientificNewsContent);
            var result = await module.HandleConceptExtractionAsync(request);

            // Assert
            Assert.NotNull(result);
            
            var jsonResult = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<JsonElement>(jsonResult);
            
            Assert.True(resultObj.TryGetProperty("success", out var successProp));
            Assert.True(successProp.GetBoolean());
            
            Assert.True(resultObj.TryGetProperty("data", out var dataProp));
            var concepts = dataProp.EnumerateArray().ToList();
            Assert.True(concepts.Count > 0);
            
            // Verify we extracted consciousness-related concepts
            var conceptNames = concepts.Select(c => c.GetProperty("concept").GetString()).ToList();
            var hasConsciousnessConcept = conceptNames.Any(name => 
                name != null && (name.ToLower().Contains("consciousness") || 
                               name.ToLower().Contains("awareness") || 
                               name.ToLower().Contains("mind")));
            
            Assert.True(hasConsciousnessConcept, "Should extract consciousness-related concepts from scientific news");
            
            // Verify we have science-related concepts
            var hasScienceConcept = conceptNames.Any(name => 
                name != null && (name.ToLower().Contains("science") || 
                               name.ToLower().Contains("quantum") || 
                               name.ToLower().Contains("research")));
            
            Assert.True(hasScienceConcept, "Should extract science-related concepts from scientific news");
            
            logger.Info($"Scientific news analysis extracted {concepts.Count} concepts:");
            foreach (var concept in concepts)
            {
                var conceptName = concept.GetProperty("concept").GetString();
                var category = concept.GetProperty("category").GetString();
                var score = concept.GetProperty("score").GetDouble();
                logger.Info($"  - {conceptName} (Category: {category}, Score: {score:F2})");
            }
        }

        [Fact]
        public async Task ExtractConceptsFromSpiritualNews_ShouldIdentifyTransformationAndUnityConcepts()
        {
            // Arrange
            var registry = TestInfrastructure.CreateTestNodeRegistry();
            var logger = TestInfrastructure.CreateTestLogger();
            var httpClient = new HttpClient();
            var module = new AIModule(registry, logger, httpClient);

            // Real news about spiritual transformation and community healing
            var spiritualNewsContent = @"
A global movement of spiritual communities has emerged, focusing on collective healing and 
transformation. The movement, called 'Unity Rising', brings together people from diverse 
backgrounds to work on inner transformation and social healing. Participants engage in 
meditation, energy work, and conscious dialogue to address both personal and collective 
trauma. The movement emphasizes the interconnectedness of all beings and the power of 
love and compassion to heal divisions. Recent gatherings have attracted thousands of 
participants who report profound experiences of unity, joy, and spiritual awakening. 
The movement's leaders speak of a new paradigm emerging where humanity recognizes its 
oneness and works together for the highest good of all. This represents a shift from 
fear-based thinking to love-based consciousness, with participants experiencing 
breakthroughs in personal relationships, health, and life purpose. The movement 
incorporates ancient wisdom traditions with modern psychological and scientific 
understanding, creating a holistic approach to human development. Many participants 
report experiencing synchronicities, enhanced intuition, and a deeper connection 
to their spiritual essence. This growing movement suggests that humanity is 
undergoing a collective awakening to higher consciousness and unity.
";

            // Act
            var request = new CodexBootstrap.Modules.ConceptExtractionRequest(spiritualNewsContent);
            var result = await module.HandleConceptExtractionAsync(request);

            // Assert
            Assert.NotNull(result);
            
            var jsonResult = JsonSerializer.Serialize(result);
            var resultObj = JsonSerializer.Deserialize<JsonElement>(jsonResult);
            
            Assert.True(resultObj.TryGetProperty("success", out var successProp));
            Assert.True(successProp.GetBoolean());
            
            Assert.True(resultObj.TryGetProperty("data", out var dataProp));
            var concepts = dataProp.EnumerateArray().ToList();
            Assert.True(concepts.Count > 0);
            
            // Verify we extracted transformation and unity concepts
            var conceptNames = concepts.Select(c => c.GetProperty("concept").GetString()).ToList();
            var hasTransformationConcept = conceptNames.Any(name => 
                name != null && (name.ToLower().Contains("transformation") || 
                               name.ToLower().Contains("healing") || 
                               name.ToLower().Contains("awakening")));
            
            Assert.True(hasTransformationConcept, "Should extract transformation-related concepts from spiritual news");
            
            var hasUnityConcept = conceptNames.Any(name => 
                name != null && (name.ToLower().Contains("unity") || 
                               name.ToLower().Contains("oneness") || 
                               name.ToLower().Contains("connection")));
            
            Assert.True(hasUnityConcept, "Should extract unity-related concepts from spiritual news");
            
            logger.Info($"Spiritual news analysis extracted {concepts.Count} concepts:");
            foreach (var concept in concepts)
            {
                var conceptName = concept.GetProperty("concept").GetString();
                var category = concept.GetProperty("category").GetString();
                var score = concept.GetProperty("score").GetDouble();
                logger.Info($"  - {conceptName} (Category: {category}, Score: {score:F2})");
            }
        }
    }
}
