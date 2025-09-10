using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// U-CORE Ontology - Upper ontology for all nodes in the system
    /// Defines the fundamental structure and relationships for U-CORE topology
    /// </summary>
    [ApiType(Name = "UCoreOntology", Description = "U-CORE ontology structure and relationships", Type = "object")]
    public class UCoreOntology
    {
        public Dictionary<string, UCoreConcept> Concepts { get; set; } = new();
        public Dictionary<string, UCoreRelationship> Relationships { get; set; } = new();
        public Dictionary<string, UCoreFrequency> Frequencies { get; set; } = new();
        public Dictionary<string, UCoreResonance> Resonances { get; set; } = new();

        public UCoreOntology()
        {
            InitializeCoreConcepts();
            InitializeCoreRelationships();
            InitializeCoreFrequencies();
        }

        private void InitializeCoreConcepts()
        {
            // Core U-CORE concepts
            Concepts["ucore"] = new UCoreConcept
            {
                Id = "ucore",
                Name = "U-CORE",
                Description = "Universal Consciousness Resonance Engine - the fundamental organizing principle",
                Type = ConceptType.Core,
                Frequency = 432.0,
                Resonance = 0.95
            };

            Concepts["joy"] = new UCoreConcept
            {
                Id = "joy",
                Name = "Joy",
                Description = "Pure positive frequency that amplifies consciousness",
                Type = ConceptType.Emotion,
                Frequency = 528.0,
                Resonance = 0.9
            };

            Concepts["pain"] = new UCoreConcept
            {
                Id = "pain",
                Name = "Pain",
                Description = "Sacred frequency that transforms into wisdom and growth",
                Type = ConceptType.Transformation,
                Frequency = 174.0,
                Resonance = 0.7
            };

            Concepts["consciousness"] = new UCoreConcept
            {
                Id = "consciousness",
                Name = "Consciousness",
                Description = "The fundamental awareness that experiences and creates reality",
                Type = ConceptType.Core,
                Frequency = 741.0,
                Resonance = 0.98
            };

            Concepts["love"] = new UCoreConcept
            {
                Id = "love",
                Name = "Love",
                Description = "The highest frequency that unifies all existence",
                Type = ConceptType.Core,
                Frequency = 528.0,
                Resonance = 0.99
            };
        }

        private void InitializeCoreRelationships()
        {
            // Core relationships between concepts
            Relationships["joy-amplifies-consciousness"] = new UCoreRelationship
            {
                Id = "joy-amplifies-consciousness",
                Source = "joy",
                Target = "consciousness",
                Type = RelationshipType.Amplifies,
                Strength = 0.9,
                Description = "Joy amplifies consciousness expansion"
            };

            Relationships["pain-transforms-to-wisdom"] = new UCoreRelationship
            {
                Id = "pain-transforms-to-wisdom",
                Source = "pain",
                Target = "consciousness",
                Type = RelationshipType.Transforms,
                Strength = 0.8,
                Description = "Pain transforms into wisdom and growth"
            };

            Relationships["love-unifies-all"] = new UCoreRelationship
            {
                Id = "love-unifies-all",
                Source = "love",
                Target = "ucore",
                Type = RelationshipType.Unifies,
                Strength = 1.0,
                Description = "Love unifies all aspects of existence"
            };
        }

        private void InitializeCoreFrequencies()
        {
            // Sacred frequencies for U-CORE resonance
            Frequencies["432hz"] = new UCoreFrequency
            {
                Id = "432hz",
                Value = 432.0,
                Name = "432 Hz",
                Description = "Natural frequency of the universe, promotes healing and harmony",
                Category = FrequencyCategory.Healing,
                Resonance = 0.95
            };

            Frequencies["528hz"] = new UCoreFrequency
            {
                Id = "528hz",
                Value = 528.0,
                Name = "528 Hz",
                Description = "Love frequency, promotes transformation and DNA repair",
                Category = FrequencyCategory.Transformation,
                Resonance = 0.98
            };

            Frequencies["741hz"] = new UCoreFrequency
            {
                Id = "741hz",
                Value = 741.0,
                Name = "741 Hz",
                Description = "Expression frequency, promotes consciousness expansion",
                Category = FrequencyCategory.Consciousness,
                Resonance = 0.92
            };
        }

        public UCoreConcept? GetConcept(string id)
        {
            return Concepts.TryGetValue(id, out var concept) ? concept : null;
        }

        public UCoreRelationship? GetRelationship(string id)
        {
            return Relationships.TryGetValue(id, out var relationship) ? relationship : null;
        }

        public UCoreFrequency? GetFrequency(string id)
        {
            return Frequencies.TryGetValue(id, out var frequency) ? frequency : null;
        }

        public List<UCoreConcept> GetConceptsByType(ConceptType type)
        {
            return Concepts.Values.Where(c => c.Type == type).ToList();
        }

        public List<UCoreRelationship> GetRelationshipsByType(RelationshipType type)
        {
            return Relationships.Values.Where(r => r.Type == type).ToList();
        }

        public List<UCoreFrequency> GetFrequenciesByCategory(FrequencyCategory category)
        {
            return Frequencies.Values.Where(f => f.Category == category).ToList();
        }
    }

    [ApiType(Name = "UCoreConcept", Description = "A concept in the U-CORE ontology", Type = "object")]
    public record UCoreConcept
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public ConceptType Type { get; init; }
        public double Frequency { get; init; }
        public double Resonance { get; init; }
        public Dictionary<string, object> Properties { get; init; } = new();
    }

    [ApiType(Name = "UCoreRelationship", Description = "A relationship between concepts in the U-CORE ontology", Type = "object")]
    public record UCoreRelationship
    {
        public string Id { get; init; } = "";
        public string Source { get; init; } = "";
        public string Target { get; init; } = "";
        public RelationshipType Type { get; init; }
        public double Strength { get; init; }
        public string Description { get; init; } = "";
        public Dictionary<string, object> Properties { get; init; } = new();
    }

    [ApiType(Name = "UCoreFrequency", Description = "A frequency in the U-CORE resonance system", Type = "object")]
    public record UCoreFrequency
    {
        public string Id { get; init; } = "";
        public double Value { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public FrequencyCategory Category { get; init; }
        public double Resonance { get; init; }
        public Dictionary<string, object> Properties { get; init; } = new();
    }

    [ApiType(Name = "UCoreResonance", Description = "A resonance pattern in the U-CORE system", Type = "object")]
    public record UCoreResonance
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public List<string> Frequencies { get; init; } = new();
        public double Amplitude { get; init; }
        public double Phase { get; init; }
        public Dictionary<string, object> Properties { get; init; } = new();
    }

    public enum ConceptType
    {
        Core,
        Emotion,
        Transformation,
        Consciousness,
        Energy,
        Frequency
    }

    public enum RelationshipType
    {
        Amplifies,
        Transforms,
        Unifies,
        Resonates,
        Harmonizes,
        Integrates
    }

    public enum FrequencyCategory
    {
        Healing,
        Transformation,
        Consciousness,
        Energy,
        Harmony,
        Love
    }
}
