using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core;

/// <summary>
/// U-CORE Ontology - The fundamental structure of consciousness and reality
/// Maps all concepts to the U-CORE axis for resonance field optimization
/// </summary>
[MetaNode("codex.ucore.ontology", "codex.meta/type", "UCoreOntology", "The fundamental U-CORE ontology structure")]
[ApiType(
    name: "U-CORE Ontology",
    description: "The complete ontological structure of U-CORE consciousness mapping",
    example: """
    {
      "id": "ucore-ontology-v1",
      "version": "1.0.0",
      "axes": {
        "consciousness": {
          "dimensions": ["awareness", "intention", "presence", "clarity"],
          "range": [0.0, 1.0],
          "resonanceFrequency": 432.0
        },
        "reality": {
          "dimensions": ["physical", "mental", "emotional", "spiritual"],
          "range": [0.0, 1.0],
          "resonanceFrequency": 528.0
        },
        "connection": {
          "dimensions": ["unity", "harmony", "flow", "integration"],
          "range": [0.0, 1.0],
          "resonanceFrequency": 741.0
        }
      },
      "topology": {
        "nodes": ["consciousness", "reality", "connection"],
        "edges": [
          {"from": "consciousness", "to": "reality", "weight": 0.8},
          {"from": "reality", "to": "connection", "weight": 0.7},
          {"from": "connection", "to": "consciousness", "weight": 0.9}
        ]
      }
    }
    """
)]
public record UCoreOntology(
    [MetaNodeField("id", "string", Required = true, Description = "Unique identifier for the ontology")]
    string Id,
    
    [MetaNodeField("version", "string", Required = true, Description = "Ontology version")]
    string Version,
    
    [MetaNodeField("axes", "object", Required = true, Description = "U-CORE axis definitions", Kind = "Object")]
    Dictionary<string, UCoreAxis> Axes,
    
    [MetaNodeField("topology", "object", Required = true, Description = "Ontological topology structure", Kind = "Object")]
    UCoreTopology Topology,
    
    [MetaNodeField("resonanceFields", "array", Description = "Resonance field definitions", Kind = "Array")]
    List<ResonanceField> ResonanceFields,
    
    [MetaNodeField("createdAt", "string", Required = true, Description = "Creation timestamp")]
    DateTime CreatedAt
);

/// <summary>
/// U-CORE Axis - A fundamental dimension of consciousness
/// </summary>
[MetaNode("codex.ucore.axis", "codex.meta/type", "UCoreAxis", "A U-CORE axis definition")]
[ApiType(
    name: "U-CORE Axis",
    description: "A fundamental dimension of consciousness in the U-CORE ontology",
    example: """
    {
      "name": "consciousness",
      "dimensions": ["awareness", "intention", "presence", "clarity"],
      "range": [0.0, 1.0],
      "resonanceFrequency": 432.0,
      "weight": 1.0,
      "description": "The axis of conscious awareness and intention"
    }
    """
)]
public record UCoreAxis(
    [MetaNodeField("name", "string", Required = true, Description = "Axis name")]
    string Name,
    
    [MetaNodeField("dimensions", "array", Required = true, Description = "Axis dimensions", Kind = "Array")]
    List<string> Dimensions,
    
    [MetaNodeField("range", "array", Required = true, Description = "Value range [min, max]", Kind = "Array")]
    List<double> Range,
    
    [MetaNodeField("resonanceFrequency", "number", Required = true, Description = "Resonance frequency in Hz")]
    double ResonanceFrequency,
    
    [MetaNodeField("weight", "number", Required = true, Description = "Axis weight in calculations", MinValue = 0.0, MaxValue = 1.0)]
    double Weight,
    
    [MetaNodeField("description", "string", Description = "Axis description")]
    string Description,
    
    [MetaNodeField("properties", "object", Description = "Additional axis properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// U-CORE Topology - The structural relationships between axes
/// </summary>
[MetaNode("codex.ucore.topology", "codex.meta/type", "UCoreTopology", "U-CORE topological structure")]
[ApiType(
    name: "U-CORE Topology",
    description: "The topological structure of U-CORE axes and their relationships",
    example: """
    {
      "nodes": ["consciousness", "reality", "connection"],
      "edges": [
        {"from": "consciousness", "to": "reality", "weight": 0.8, "type": "influences"},
        {"from": "reality", "to": "connection", "weight": 0.7, "type": "enables"},
        {"from": "connection", "to": "consciousness", "weight": 0.9, "type": "amplifies"}
      ],
      "resonanceMatrix": [[1.0, 0.8, 0.9], [0.8, 1.0, 0.7], [0.9, 0.7, 1.0]]
    }
    """
)]
public record UCoreTopology(
    [MetaNodeField("nodes", "array", Required = true, Description = "Topology nodes", Kind = "Array")]
    List<string> Nodes,
    
    [MetaNodeField("edges", "array", Required = true, Description = "Topology edges", Kind = "Array")]
    List<UCoreEdge> Edges,
    
    [MetaNodeField("resonanceMatrix", "array", Description = "Resonance coupling matrix", Kind = "Array")]
    List<List<double>> ResonanceMatrix,
    
    [MetaNodeField("properties", "object", Description = "Topology properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// U-CORE Edge - A relationship between axes
/// </summary>
[MetaNode("codex.ucore.edge", "codex.meta/type", "UCoreEdge", "A U-CORE topology edge")]
[ApiType(
    name: "U-CORE Edge",
    description: "A relationship between U-CORE axes",
    example: """
    {
      "from": "consciousness",
      "to": "reality",
      "weight": 0.8,
      "type": "influences",
      "resonanceStrength": 0.85,
      "description": "Consciousness influences reality manifestation"
    }
    """
)]
public record UCoreEdge(
    [MetaNodeField("from", "string", Required = true, Description = "Source axis")]
    string From,
    
    [MetaNodeField("to", "string", Required = true, Description = "Target axis")]
    string To,
    
    [MetaNodeField("weight", "number", Required = true, Description = "Edge weight", MinValue = 0.0, MaxValue = 1.0)]
    double Weight,
    
    [MetaNodeField("type", "string", Required = true, Description = "Edge type", Kind = "Enum", EnumValues = new[] { "influences", "enables", "amplifies", "transforms", "resonates" })]
    string Type,
    
    [MetaNodeField("resonanceStrength", "number", Required = true, Description = "Resonance strength", MinValue = 0.0, MaxValue = 1.0)]
    double ResonanceStrength,
    
    [MetaNodeField("description", "string", Description = "Edge description")]
    string Description,
    
    [MetaNodeField("properties", "object", Description = "Edge properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// Resonance Field - A field of resonance within the U-CORE ontology
/// </summary>
[MetaNode("codex.ucore.resonance-field", "codex.meta/type", "ResonanceField", "A U-CORE resonance field")]
[ApiType(
    name: "Resonance Field",
    description: "A field of resonance within the U-CORE ontology",
    example: """
    {
      "id": "resonance-field-1",
      "name": "Heart Chakra Resonance",
      "frequency": 432.0,
      "axes": ["consciousness", "connection"],
      "strength": 0.85,
      "description": "Resonance field for heart chakra alignment"
    }
    """
)]
public record ResonanceField(
    [MetaNodeField("id", "string", Required = true, Description = "Resonance field ID")]
    string Id,
    
    [MetaNodeField("name", "string", Required = true, Description = "Resonance field name")]
    string Name,
    
    [MetaNodeField("frequency", "number", Required = true, Description = "Resonance frequency in Hz")]
    double Frequency,
    
    [MetaNodeField("axes", "array", Required = true, Description = "Affected axes", Kind = "Array")]
    List<string> Axes,
    
    [MetaNodeField("strength", "number", Required = true, Description = "Field strength", MinValue = 0.0, MaxValue = 1.0)]
    double Strength,
    
    [MetaNodeField("description", "string", Description = "Field description")]
    string Description,
    
    [MetaNodeField("properties", "object", Description = "Field properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// User Belief System - Weighted concepts and investments for resonance matching
/// </summary>
[MetaNode("codex.ucore.user-belief-system", "codex.meta/type", "UserBeliefSystem", "User belief system with weighted concepts")]
[ApiType(
    name: "User Belief System",
    description: "User's belief system with weighted concepts and investments for resonance matching",
    example: """
    {
      "userId": "user-123",
      "concepts": [
        {"name": "consciousness", "weight": 0.9, "investment": 0.8},
        {"name": "spirituality", "weight": 0.7, "investment": 0.6},
        {"name": "technology", "weight": 0.5, "investment": 0.4}
      ],
      "axes": {
        "consciousness": 0.9,
        "reality": 0.6,
        "connection": 0.8
      },
      "resonancePreferences": {
        "frequency": 432.0,
        "strength": 0.7
      }
    }
    """
)]
public record UserBeliefSystem(
    [MetaNodeField("userId", "string", Required = true, Description = "User identifier")]
    string UserId,
    
    [MetaNodeField("concepts", "array", Required = true, Description = "Weighted concepts", Kind = "Array")]
    List<WeightedConcept> Concepts,
    
    [MetaNodeField("axes", "object", Required = true, Description = "Axis preferences", Kind = "Object")]
    Dictionary<string, double> Axes,
    
    [MetaNodeField("resonancePreferences", "object", Required = true, Description = "Resonance preferences", Kind = "Object")]
    ResonancePreferences ResonancePreferences,
    
    [MetaNodeField("createdAt", "string", Required = true, Description = "Creation timestamp")]
    DateTime CreatedAt,
    
    [MetaNodeField("updatedAt", "string", Required = true, Description = "Last update timestamp")]
    DateTime UpdatedAt
);

/// <summary>
/// Weighted Concept - A concept with weight and investment level
/// </summary>
[MetaNode("codex.ucore.weighted-concept", "codex.meta/type", "WeightedConcept", "A concept with weight and investment")]
[ApiType(
    name: "Weighted Concept",
    description: "A concept with weight and investment level for resonance matching",
    example: """
    {
      "name": "consciousness",
      "weight": 0.9,
      "investment": 0.8,
      "description": "Core concept of conscious awareness",
      "tags": ["spiritual", "philosophical", "core"]
    }
    """
)]
public record WeightedConcept(
    [MetaNodeField("name", "string", Required = true, Description = "Concept name")]
    string Name,
    
    [MetaNodeField("weight", "number", Required = true, Description = "Concept weight", MinValue = 0.0, MaxValue = 1.0)]
    double Weight,
    
    [MetaNodeField("investment", "number", Required = true, Description = "Investment level", MinValue = 0.0, MaxValue = 1.0)]
    double Investment,
    
    [MetaNodeField("description", "string", Description = "Concept description")]
    string Description,
    
    [MetaNodeField("tags", "array", Description = "Concept tags", Kind = "Array")]
    List<string> Tags,
    
    [MetaNodeField("properties", "object", Description = "Concept properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// Resonance Preferences - User's resonance preferences
/// </summary>
[MetaNode("codex.ucore.resonance-preferences", "codex.meta/type", "ResonancePreferences", "User resonance preferences")]
[ApiType(
    name: "Resonance Preferences",
    description: "User's preferences for resonance field optimization",
    example: """
    {
      "frequency": 432.0,
      "strength": 0.7,
      "axes": ["consciousness", "connection"],
      "sensitivity": 0.8
    }
    """
)]
public record ResonancePreferences(
    [MetaNodeField("frequency", "number", Required = true, Description = "Preferred frequency in Hz")]
    double Frequency,
    
    [MetaNodeField("strength", "number", Required = true, Description = "Preferred strength", MinValue = 0.0, MaxValue = 1.0)]
    double Strength,
    
    [MetaNodeField("axes", "array", Required = true, Description = "Preferred axes", Kind = "Array")]
    List<string> Axes,
    
    [MetaNodeField("sensitivity", "number", Required = true, Description = "Resonance sensitivity", MinValue = 0.0, MaxValue = 1.0)]
    double Sensitivity,
    
    [MetaNodeField("properties", "object", Description = "Preference properties", Kind = "Object")]
    Dictionary<string, object> Properties
);

/// <summary>
/// Resonance Match - Result of resonance field optimization
/// </summary>
[MetaNode("codex.ucore.resonance-match", "codex.meta/type", "ResonanceMatch", "Resonance field optimization result")]
[ApiType(
    name: "Resonance Match",
    description: "Result of resonance field optimization between LLM response and user belief system",
    example: """
    {
      "matchId": "match-123",
      "userId": "user-123",
      "responseId": "response-456",
      "overallMatch": 0.85,
      "axisMatches": {
        "consciousness": 0.9,
        "reality": 0.7,
        "connection": 0.8
      },
      "conceptMatches": [
        {"concept": "consciousness", "match": 0.9, "resonance": 0.85},
        {"concept": "spirituality", "match": 0.7, "resonance": 0.6}
      ],
      "optimizationScore": 0.82,
      "recommendations": ["Increase consciousness focus", "Explore connection aspects"]
    }
    """
)]
public record ResonanceMatch(
    [MetaNodeField("matchId", "string", Required = true, Description = "Match identifier")]
    string MatchId,
    
    [MetaNodeField("userId", "string", Required = true, Description = "User identifier")]
    string UserId,
    
    [MetaNodeField("responseId", "string", Required = true, Description = "Response identifier")]
    string ResponseId,
    
    [MetaNodeField("overallMatch", "number", Required = true, Description = "Overall match score", MinValue = 0.0, MaxValue = 1.0)]
    double OverallMatch,
    
    [MetaNodeField("axisMatches", "object", Required = true, Description = "Axis match scores", Kind = "Object")]
    Dictionary<string, double> AxisMatches,
    
    [MetaNodeField("conceptMatches", "array", Required = true, Description = "Concept match scores", Kind = "Array")]
    List<ConceptMatch> ConceptMatches,
    
    [MetaNodeField("optimizationScore", "number", Required = true, Description = "Optimization score", MinValue = 0.0, MaxValue = 1.0)]
    double OptimizationScore,
    
    [MetaNodeField("recommendations", "array", Description = "Optimization recommendations", Kind = "Array")]
    List<string> Recommendations,
    
    [MetaNodeField("createdAt", "string", Required = true, Description = "Creation timestamp")]
    DateTime CreatedAt
);

/// <summary>
/// Concept Match - Individual concept resonance match
/// </summary>
[MetaNode("codex.ucore.concept-match", "codex.meta/type", "ConceptMatch", "Individual concept resonance match")]
[ApiType(
    name: "Concept Match",
    description: "Individual concept resonance match result",
    example: """
    {
      "concept": "consciousness",
      "match": 0.9,
      "resonance": 0.85,
      "weight": 0.9,
      "investment": 0.8,
      "description": "High match for consciousness concept"
    }
    """
)]
public record ConceptMatch(
    [MetaNodeField("concept", "string", Required = true, Description = "Concept name")]
    string Concept,
    
    [MetaNodeField("match", "number", Required = true, Description = "Match score", MinValue = 0.0, MaxValue = 1.0)]
    double Match,
    
    [MetaNodeField("resonance", "number", Required = true, Description = "Resonance score", MinValue = 0.0, MaxValue = 1.0)]
    double Resonance,
    
    [MetaNodeField("weight", "number", Required = true, Description = "Concept weight", MinValue = 0.0, MaxValue = 1.0)]
    double Weight,
    
    [MetaNodeField("investment", "number", Required = true, Description = "Investment level", MinValue = 0.0, MaxValue = 1.0)]
    double Investment,
    
    [MetaNodeField("description", "string", Description = "Match description")]
    string Description
);
