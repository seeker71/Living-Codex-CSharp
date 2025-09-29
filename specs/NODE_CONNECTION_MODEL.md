## Node Connection Model: Identity, Typing, Ontology Paths, and State

### How to read this spec
- This is a policy and enforcement specification. For runtime readiness and deviations, see the main spec’s Status Ledger and `GAP_ANALYSIS.md`.
- Enforcement surfaces primarily at storage endpoints (`/storage-endpoints/nodes*`, `/storage-endpoints/edges*`) and in the registry.

### Integration Status
- Phase A — Behavior changes: COMPLETE (2025-09-29)
  - Self-identity edge auto-creation removed (no self-edges allowed)
  - `instance-of` creation enforced to distinct type nodes (self-typing forbidden except meta anchor)
  - Placeholders and reflection artifacts created as Water with `meta.placeholder=true`
  - Shortcut topology from arbitrary nodes directly to U‑CORE root removed
- Phase B — Cleanup/backfill: IN PROGRESS (planning)
  - One-shot cleanup of legacy self-identity edges in historical data
  - Backfill missing `instance-of` where only identity existed
  - Backfill `subtype-of` for type hierarchies where applicable
- Treat related routes as PartiallyTested until backfill/cleanup steps are validated.

### Goals
- Remove non-essential self-identity edges; keep identity semantics via proper type and ontology links.
- Make typing explicit with `instance-of` edges to type nodes (`codex.meta/type/*`).
- Allow only the core meta type anchor to self-type; all other nodes must type to a distinct node.
- Route concept connectivity through ontology/topology (axes, parents/children), not direct shortcuts.
- Enforce state policy: generated = Water or Gas; keep Ice tiny and reserved for authored cores.

### Canonical Nodes and Roles
- **Type lattice root**: `codex.meta/type` (the meta-type for all type nodes).
- **Type nodes**: `codex.meta/type/*` (schema/type nodes).
- **Concept taxonomy**: `u-core-*` (axes and concepts from U‑CORE config/spec).
- **Edge roles** (canonical):
  - `instance-of` (instance → type)
  - `subtype-of` (type → supertype)
  - `is_a` (concept → broader concept)
  - `has_child` (concept parent → child)
  - `concept_on_axis` (concept → axis)
  - `has_content_type` (node → content-type)
  - `references` (node → meta/auxiliary)

### Identity and Self-Edges
- Self-identity edges (`identity` from a node to itself) are removed from the model and MUST NOT be auto-created.
- The only permitted fixed-point typing is at the meta-type anchor: `codex.meta/type` MAY be `instance-of` itself. No other node may be `instance-of` itself.
- The identity concept `u-core-meta-identity` remains part of the concept taxonomy (reachable via ontology), but does not require self-edges.

### Typing Semantics
- Every node MUST have at least one `instance-of` edge to a type node.
  - Instances e.g. `codex.news.item/*` → `codex.meta/type/news-item`.
  - Type nodes `codex.meta/type/*` → `codex.meta/type` via `instance-of`.
  - Only `codex.meta/type` MAY be `instance-of` itself.
- Type hierarchies MUST use `subtype-of` (type → supertype) instead of `is_a`.
- Concept taxonomies MUST use `is_a`/`has_child` between concept instances (not type nodes).

### Ontology and Topology Paths (No Shortcuts)
- Concepts MUST connect to U‑CORE through declared structure:
  - concept `is_a` broader concept(s)
  - concept `concept_on_axis` axis nodes
  - axes connect via parent/child axis edges
  - U‑CORE root contains concepts via `contains` edges (baseline linkage)
- Generic shortcuts like global `maps_to_axis` or arbitrary default concept mappings MUST NOT be auto-added. All paths should emerge from ontology/topology definitions or explicit domain logic.

### State Policy (Ice/Water/Gas)
- Ice (atoms): Only authored, canonical cores
  - U‑CORE ontology seeds from config (axes, relationships, identity concept)
  - Core specs and essential indices that are not reproducible from other atoms
- Water (materialized): Deterministic, generated, or reflection-derived
  - Reflection/meta discovery (module/api/type/field nodes) → Water
  - Placeholders auto-created to satisfy references → Water
  - Any derivable projections/adapters → Water
- Gas (derivable/transient): Ephemeral runtime state (sessions, in-flight jobs)
- Rule of thumb: If it can be recreated deterministically from Ice or external truth, it MUST NOT be Ice.

### Enforcement Rules (High-Level)
1) Node creation/update
   - Do NOT create `identity` self-edges.
   - Ensure/add `instance-of` to the correct type node.
   - Add `has_content_type` when `Content.MediaType` is present.
2) Type nodes
   - Type nodes are `instance-of` `codex.meta/type`.
   - Use `subtype-of` for type generalization, not `is_a`.
3) Concepts
   - Respect declared `parentConcepts`/`childConcepts` via `is_a`/`has_child`.
   - Respect declared axes with `concept_on_axis` (no direct global shortcuts).
4) Placeholders
   - Any runtime placeholder nodes MUST be Water and clearly marked.

### Gaps in Current Implementation
The following items have been resolved by Phase A (2025-09-29):
- Self-identity edges auto-creation removed across registry and storage endpoints
- Placeholder/ensured nodes now Water with `meta.placeholder=true`
- Shortcut edges from arbitrary nodes to U‑CORE root removed

Remaining:
- Meta/type node defaults audited; ensure authored atoms only are Ice
- Historical data cleanup/backfill (Phase B)

### Migration Plan (Minimal, Reversible Deltas)
Phase A — Behavior changes
1) Remove auto-creation of `identity` self-edges.
2) Keep/strengthen `instance-of` creation to proper type node; forbid self-typing except `codex.meta/type`.
3) Change NodeHelpers meta-node factories to Water for generated/reflection artifacts.
4) Change runtime placeholder creation to Water and tag `meta.placeholder=true`.
5) Deprecate global `maps_to_axis`/default concept mappings; rely on ontology.

Phase B — Cleanup and backfill
6) One-shot cleanup to delete all existing `identity` self-edges except allowed fixed-point at `codex.meta/type` (if present).
7) Backfill missing `instance-of` edges where only the self-identity existed.
8) Backfill `subtype-of` where applicable for type hierarchy.

Phase C — Validation
9) Ensure all nodes have a valid path to U‑CORE root via ontology/topology; remove reliance on shortcuts.
10) Verify state compliance: generated/meta placeholders are Water; Ice footprint is reduced to authored cores.

### Acceptance Criteria
- No auto-created `identity` edges appear on new node inserts.
- All nodes have exactly one primary `instance-of` to a distinct type (except `codex.meta/type`).
- Concept traversal to identity works through ontology (tests pass without relying on `identity` edges).
- Generated/meta/placeholder nodes are Water; Ice count drops substantially.

### Notes on Identity Concept vs. Type Anchor
- Keep `u-core-meta-identity` as a concept in U‑CORE (taxonomy).
- Treat `codex.meta/type` as the sole fixed-point of typing (the meta-type anchor). If a self `instance-of` is represented, it MUST ONLY occur here.


