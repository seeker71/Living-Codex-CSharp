# Concept Taxonomy Redesign - Living Codex

**Date:** October 1, 2025  
**Status:** Design Phase  
**Goal:** Eliminate duplicates, create clear hierarchy, max 3-word concepts

---

## 🎯 Requirements

1. **Specific Concepts** - Max 3 words (e.g., "Quantum Entanglement", "Machine Learning", "Water Cycle")
2. **Parent-Child Hierarchy** - Each concept links to more generic parent until reaching top topology
3. **Top Topology Concepts** - Root concepts for each category (7 axes)
4. **Relationship-Based** - Use edges (is-a, part-of, related-to)
5. **Placeholder Descriptions** - Start with placeholder, AI enriches later
6. **External Enrichment** - Wikipedia, LLM, or public APIs fill descriptions
7. **No Duplicates** - Single canonical entry per concept

---

## 🌳 Proposed Taxonomy Structure

### Top-Level Categories (7 U-CORE Axes)
```
u-core-root
├── Consciousness
├── Energy
├── Information
├── Matter
├── Space
├── Time
└── Unity
```

### Example Hierarchy: Water Concepts

#### Current (Problematic)
```
❌ u-core-concept-water
❌ u-core-concept-liquid
❌ u-core-concept-ice
❌ u-core-concept-vapor
❌ u-core-concept-plasma
❌ u-core-concept-amorphous
❌ u-core-concept-structured
❌ u-core-concept-colloidal
❌ u-core-concept-clustered
❌ u-core-concept-quantum_coherent
```
**Issues:** Flat structure, duplicates, unclear relationships, too many variants

#### Proposed (Clean Hierarchy)
```
✅ Matter
    ├── Substance
    │   ├── Water (H₂O)
    │   │   ├── Ice (solid)
    │   │   ├── Liquid Water
    │   │   ├── Water Vapor
    │   │   └── Plasma State
    │   ├── Air
    │   └── Earth
    └── Material Properties
        ├── Phase States
        ├── Molecular Structure
        └── Quantum Properties
```

**Max 3 words:** ✅ "Ice", "Liquid Water", "Water Vapor", "Plasma State"

---

## 🔗 Relationship Types

### is-a (Taxonomic)
- "Ice" **is-a** "Water Phase"
- "Water Phase" **is-a** "Matter State"
- "Matter State" **is-a** "Matter"

### part-of (Compositional)
- "Water Cycle" **part-of** "Hydrological System"
- "Evaporation" **part-of** "Water Cycle"

### related-to (Associative)
- "Quantum Coherence" **related-to** "Water Structure"
- "Plasma State" **related-to** "High Energy Physics"

### instance-of (Instantiation)
- "Pacific Ocean" **instance-of** "Ocean"
- "Lake Superior" **instance-of** "Freshwater Lake"

---

## 📊 Database Schema for Hierarchy

### Nodes
```sql
-- Specific concept (leaf node)
INSERT INTO ice_nodes (id, type_id, title, description, meta)
VALUES (
  'concept-liquid-water',
  'codex.concept',
  'Liquid Water',
  '[PLACEHOLDER] - Enrichment pending',
  '{"specificity": "high", "wordCount": 2, "topologyPath": ["Matter", "Substance", "Water"]}'
);

-- Parent concept
INSERT INTO ice_nodes (id, type_id, title, description, meta)
VALUES (
  'concept-water',
  'codex.concept',
  'Water',
  'H₂O - Essential compound for life',
  '{"specificity": "medium", "wordCount": 1, "topologyPath": ["Matter", "Substance"]}'
);

-- Top topology
INSERT INTO ice_nodes (id, type_id, title, description, meta)
VALUES (
  'u-core-axis-matter',
  'u-core.axis',
  'Matter',
  'Physical substance and material reality',
  '{"specificity": "low", "isTopology": true}'
);
```

### Edges (Hierarchy)
```sql
-- Leaf → Parent
INSERT INTO ice_edges (from_id, to_id, role, weight, meta)
VALUES (
  'concept-liquid-water',
  'concept-water',
  'is-a',
  1.0,
  '{"hierarchyLevel": 3, "descriptionStatus": "placeholder"}'
);

-- Parent → Grandparent
INSERT INTO ice_edges (from_id, to_id, role, weight, meta)
VALUES (
  'concept-water',
  'concept-substance',
  'is-a',
  1.0,
  '{"hierarchyLevel": 2}'
);

-- Grandparent → Top Topology
INSERT INTO ice_edges (from_id, to_id, role, weight, meta)
VALUES (
  'concept-substance',
  'u-core-axis-matter',
  'is-a',
  1.0,
  '{"hierarchyLevel": 1, "isTopologyLink": true}'
);
```

---

## 🤖 AI Enrichment Pipeline

### Phase 1: Placeholder Creation
```csharp
var concept = new Node(
    Id: conceptId,
    TypeId: "codex.concept",
    State: ContentState.Ice,
    Title: conceptName,  // Max 3 words
    Description: "[PLACEHOLDER] - AI enrichment pending",
    Meta: new Dictionary<string, object>
    {
        ["enrichmentStatus"] = "pending",
        ["source"] = "extraction",
        ["createdAt"] = DateTime.UtcNow
    }
);
```

### Phase 2: Wikipedia Enrichment
```csharp
// Call Wikipedia API
var wikipediaUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(conceptName)}";
var response = await _httpClient.GetAsync(wikipediaUrl);
if (response.IsSuccessStatusCode)
{
    var data = await response.Content.ReadFromJsonAsync<WikipediaSummary>();
    concept.Description = data.Extract;  // First paragraph
    concept.Meta["enrichmentStatus"] = "enriched";
    concept.Meta["enrichmentSource"] = "wikipedia";
    concept.Meta["enrichedAt"] = DateTime.UtcNow;
    _registry.Upsert(concept);
}
```

### Phase 3: LLM Enrichment (Fallback)
```csharp
if (concept.Meta["enrichmentStatus"] == "pending")
{
    var prompt = $"Provide a concise 1-sentence description of '{conceptName}' suitable for a knowledge graph.";
    var llmResponse = await _llmService.GenerateAsync(prompt);
    concept.Description = llmResponse.Text;
    concept.Meta["enrichmentStatus"] = "enriched";
    concept.Meta["enrichmentSource"] = "llm";
}
```

---

## 🏗️ Implementation Plan

### Module: `ConceptTaxonomyModule.cs`

```csharp
public class ConceptTaxonomyModule : ModuleBase
{
    // Endpoints:
    // POST /taxonomy/normalize-concept - Ensure concept follows rules
    // GET  /taxonomy/hierarchy/{conceptId} - Get full path to root
    // POST /taxonomy/enrich/{conceptId} - Trigger AI enrichment
    // GET  /taxonomy/duplicates - Find duplicate concepts
    // POST /taxonomy/merge-duplicates - Merge duplicate entries
    // GET  /taxonomy/validate - Validate entire taxonomy
}
```

### Key Methods

1. **NormalizeConcept** - Ensure max 3 words, create placeholder
2. **BuildHierarchy** - Link to parent concepts up to topology
3. **EnrichDescription** - Wikipedia → LLM → Manual
4. **FindDuplicates** - Detect same concept with different IDs
5. **MergeDuplicates** - Consolidate to single canonical entry
6. **ValidateTaxonomy** - Check all concepts have paths to topology

---

## 🧪 Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task Concept_ShouldHave_MaxThreeWords()
{
    var concept = await CreateConcept("Quantum Field Theory Particle");
    Assert.True(concept.Title.Split(' ').Length <= 3, "Should truncate to 3 words");
}

[Fact]
public async Task Concept_ShouldLink_ToTopology()
{
    var concept = await CreateConcept("Photosynthesis");
    var path = await GetTopologyPath(concept.Id);
    Assert.Contains("Energy", path);  // Should link to Energy axis
    Assert.Equal("u-core-axis-energy", path.Last());  // Should reach topology
}

[Fact]
public async Task Enrichment_ShouldUse_Wikipedia()
{
    var concept = await CreateConcept("Photosynthesis");
    await EnrichConcept(concept.Id);
    Assert.NotEqual("[PLACEHOLDER]", concept.Description);
    Assert.Equal("enriched", concept.Meta["enrichmentStatus"]);
}
```

### Integration Tests
- Extract 100 concepts from news
- Validate all have <= 3 words
- Validate all link to topology
- Validate no duplicates
- Validate all enriched within 1 minute

---

## 📈 Success Metrics

✅ **Zero duplicates** - Each concept has single canonical ID  
✅ **Max 3 words** - All concept titles concise and specific  
✅ **100% topology links** - Every concept reaches a top-level axis  
✅ **90%+ enriched** - Wikipedia or LLM descriptions filled in  
✅ **Clear hierarchy** - Parent-child relationships explicit  

---

## 🚀 Implementation Steps

1. **Audit** - Identify all current u-core concepts and duplicates
2. **Design** - Define canonical taxonomy with 7 axes
3. **Create Module** - `ConceptTaxonomyModule.cs` with all APIs
4. **Normalize** - Process existing concepts (dedupe, truncate, link)
5. **Enrich** - Add Wikipedia/LLM service integration
6. **Test** - Comprehensive test suite
7. **Validate** - Run full taxonomy validation
8. **Deploy** - Enable for all new concept extraction

---

## Next Step

Continue with TODO #1: **Audit u-core duplicates** by querying current concepts and creating deduplication report.

