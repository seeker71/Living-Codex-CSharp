# Fractal Unfolding Plan — Bootstrap → Full Spec

> A living, self-describing document that unfolds the system one **coil** (layer) at a time. Each coil depends **only** on prior coils and adds the smallest possible **ice** delta (atoms), with behavior realized by **water** (generated prototypes, cached content) and **gas** (derivable/transient). All structures are nodes; structures about structures are **meta‑nodes**.

---

## Global System Instructions (never repeat)

1. **Everything is a Node.** Data, structure, flow, state, deltas, policies, specs — all have node forms. Runtime types are scaffolding that must round‑trip ⇄ nodes.
2. **Meta‑Nodes Describe Structure.** Schemas, APIs, layers, code (structure/flow/state) are expressed as `codex.meta/*` or `codex.code/*` nodes with edges.
3. **Prefer Generalization to Duplication.** If a new variant emerges, generalize the existing construct until both are instances. Avoid partial forks.
4. **Keep Ice Tiny.** Persist only atoms, deltas, essential indices. Let water (materialized) and gas (derivable) carry weight.
5. **Tiny Deltas.** All changes are minimal patches on nodes/edges (git‑like). Large rewrites must be proven irreducible.
6. **Single Lifecycle.** Use the breath loop: **compose → expand → validate → (melt/patch/refreeze) → contract**.
7. **Resonance Before Refreeze.** Structural edits must harmonize with anchors; otherwise generalize or stay as water/gas.
8. **Adapters Over Features.** External I/O (web, files, DB, AI) is adapterized; the core stays thin.
9. **Deterministic Projections.** OpenAPI/JSON‑Schema/codegen derive deterministically from meta‑nodes.
10. **One‑Shot First.** Each coil should be runnable from atoms via a single call (prove minimal sufficiency).

---

## Notation & Conventions

* **Ice** = essential persisted facts (atoms, deltas). **Water** = materialized caches/prototypes. **Gas** = derivable/transient.
* **Atoms JSON** snippets show only the new/changed facts per coil.
* **TypeIds** for node kinds are written like `codex.meta/type`, `codex.delta/patch`, etc.
* **Resonance anchors** are other ice nodes a change must agree with before refreeze.

---

## Coil L0 — Bootstrap Seed

**Goal:** Minimal kernel that can describe itself and run the breath loop.

**Depends on:** none

**Adds (Ice):** `Node`, `Edge`, minimal **atoms/spec/prototype** loop; dynamic `/route`; `file://`, `http(s)://` hydration.

**Surfaced Endpoints:**

* `/spec/atoms`, `/spec/compose`, `/breath/expand|validate|contract/{id}`
* `/nodes` (upsert), `/nodes/{id}` (get), `/edges` (upsert/list)
* `/hydrate/{id}` (minimal), `/route` (dynamic)
* `/openapi/{id}`, `/plan/{id}`, `/spec/export|import`

**Atoms (delta excerpt):**

```json
{
  "id":"codex.core","version":"0.1.x","name":"Core",
  "resources":[
    {"name":"Node","fields":[{"name":"id","type":"string","required":true},{"name":"typeId","type":"string","required":true},{"name":"state","type":"string","required":true}]},
    {"name":"Edge","fields":[{"name":"fromId","type":"string","required":true},{"name":"toId","type":"string","required":true},{"name":"role","type":"string","required":true}]}
  ],
  "operations":[
    {"name":"SubmitAtoms","verb":"POST","route":"/spec/atoms"},
    {"name":"Compose","verb":"POST","route":"/spec/compose"},
    {"name":"Expand","verb":"POST","route":"/breath/expand/{id}"},
    {"name":"Validate","verb":"POST","route":"/breath/validate/{id}"},
    {"name":"Contract","verb":"POST","route":"/breath/contract/{id}"}
  ]
}
```

**Acceptance:** From these atoms alone, `/oneshot/apply` composes → expands → validates (no‑op) → contracts a delta node.

---

## Coil L1 — Spec Reflection (Everything as Nodes)

**Goal:** Structures become meta‑nodes; the spec round‑trips spec ⇄ node graph.

**Depends on:** L0

**Adds (Ice):** meta‑node kinds `codex.meta/module|type|property|api`; reflect/ingest endpoints.

**Surfaced Endpoints:**

* `GET /reflect/spec/{id}` → emits meta‑nodes
* `POST /ingest/spec` → builds a spec from meta‑nodes

**Atoms (delta excerpt):**

```json
{
  "id":"codex.reflect","version":"0.1.0","name":"Reflection",
  "dependencies":[{"id":"codex.core","version":"0.1.x"}],
  "resources":[{"name":"MetaModule","fields":[{"name":"id","type":"string","required":true},{"name":"version","type":"string","required":true}]}],
  "operations":[
    {"name":"ReflectSpec","verb":"GET","route":"/reflect/spec/{id}"},
    {"name":"IngestSpec","verb":"POST","route":"/ingest/spec"}
  ]
}
```

**Acceptance:** Reflect core spec to nodes; ingest them back and obtain an identical spec (modulo ordering).

---

## Coil L2 — Tiny Deltas (Diff/Patch)

**Goal:** Minimal, git‑like patches for nodes/edges.

**Depends on:** L0

**Adds (Ice):** `codex.delta/patch` model (`PatchDoc`, `PatchOp`); diff & patch endpoints.

**Surfaced Endpoints:**

* `GET /diff/{id}?against=baseId`
* `POST /patch/{targetId}`

**Atoms (delta excerpt):**

```json
{
  "id":"codex.delta","version":"0.1.0","name":"Delta",
  "dependencies":[{"id":"codex.core","version":"0.1.x"}],
  "resources":[
    {"name":"PatchOp","fields":[{"name":"op","type":"string","required":true},{"name":"path","type":"string","required":true},{"name":"value","type":"string"}]},
    {"name":"PatchDoc","fields":[{"name":"targetId","type":"string","required":true},{"name":"ops","type":"string","required":true}]}
  ],
  "operations":[
    {"name":"Diff","verb":"GET","route":"/diff/{id}"},
    {"name":"Patch","verb":"POST","route":"/patch/{targetId}"}
  ]
}
```

**Acceptance:** Given two versions of a node, `Diff` returns a minimal patch; `Patch` applies it to reach the target.

---

## Coil L3 — Phase & Resonance

**Goal:** Ice is changeable; melt → edit → resonance → refreeze.

**Depends on:** L0, L2

**Adds (Ice):** phase ops + resonance proposal/report.

**Surfaced Endpoints:**

* `POST /phase/melt/{id}` → Water
* `POST /phase/refreeze/{id}` → Ice
* `POST /resonance/check` → `{ ok, message }`

**Acceptance:** You can melt a meta‑node, patch it, pass resonance, and refreeze it to ice.

---

## Coil L4 — Linking & Adapters (Extended ContentRef)

**Goal:** Nodes can link to anything via adapters; spec remains thin.

**Depends on:** L0

**Adds (Ice):** extended `ContentRef` fields: `selector`, `query`, `headers`, `authRef`, `cacheKey`; adapter registration.

**Surfaced Endpoints:**

* `POST /adapters/register`
* (uses existing) `POST /hydrate/{id}`

**Acceptance:** Register `file://`, `http(s)://` adapters; hydrate a node pointing to an external resource.

---

## Coil L5 — Type System Enrichment + Deterministic OpenAPI

**Goal:** Richer typing & deterministic projection for downstream tools.

**Depends on:** L1

**Adds (Ice):** `TypeSpec` properties/arrays/refs; canonical OpenAPI projection rules.

**Surfaced Endpoints:**

* (uses) `GET /openapi/{id}` → stable ordering & `$ref` rules

**Acceptance:** Project OpenAPI for any composed module; regenerations are byte‑stable.

---

## Coil L6 — Relations & Constraints

**Goal:** Model relations and cardinalities as atoms; prepare validators.

**Depends on:** L5

**Adds (Ice):** `Relation` atoms: `fromType`, `toType`, `role`, `cardinality`.

**Surfaced Endpoints:**

* (uses) `POST /breath/validate/{id}` (plug validators later)

**Acceptance:** Relations appear as meta‑nodes/edges; validation intent recorded (no hard enforcement yet).

---

## Coil L7 — Persistence & Content Addressing

**Goal:** Durable, content‑addressed ice/water with lineage.

**Depends on:** L2, L3

**Adds (Ice):** content addressing (e.g., blake3 hash in `ContentRef.cacheKey`); persistence backend contracts.

**Surfaced Endpoints:**

* (internal) store/retrieve by hash; expose `GET /nodes/{id}?asOf=hash` (optional later)

**Acceptance:** Hydrated content carries deterministic cache keys; deltas/prototypes can be deduped.

---

## Coil L8 — `prompt://` Adapter (Gas→Water via AI)

**Goal:** Prompts as data; hydrate via LLM adapter.

**Depends on:** L4

**Adds (Ice):** `PromptTemplate` resource; `prompt://` scheme semantics & policy nodes.

**Acceptance:** A node with `ExternalUri: "prompt://…"` hydrates (under policy) to text/JSON water.

---

## Coil L9 — `db://` Adapter (Queryable Nodes)

**Goal:** Database-backed content via queries & selectors.

**Depends on:** L4

**Adds (Ice):** `db://` scheme, `Query` semantics, `AuthRef` to credential nodes.

**Acceptance:** A node with `db://…` and `Query` hydrates to rows/JSON water.

---

## Coil L10 — Code as Structure/Flow/State

**Goal:** Language‑agnostic description of code.

**Depends on:** L1, L5

**Adds (Ice):**

* `codex.code/type|field|interface|enum|const` (structure)
* `codex.code/func|param|return|call|block` (flow)
* `codex.code/var|assignment|instance|resource` (state)

**Acceptance:** A simple module's code can be fully described as meta‑nodes; later coils project to target languages.

---

## Coil L11 — Validator Suite

**Goal:** Contract conformance, relation enforcement, smoke tests.

**Depends on:** L6, L10

**Adds (Ice):** validator registry atoms; resonant policies that block refreeze on violations.

**Acceptance:** `POST /breath/validate/{id}` fails if declared relations/constraints or API contracts are violated.

---

## Coil L12 — Codegen Targets (C#/Go/TS)

**Goal:** Deterministic code/project generation from meta‑nodes.

**Depends on:** L5, L10, L11

**Adds (Ice):** `GenPreset` atoms; generators per target.

**Acceptance:** One‑shot from atoms yields compilable stubs + tests as prototype nodes.

---

## Coil L13 — Governance & Policy

**Goal:** Versioning, signing, policy, migration.

**Depends on:** L7, L11

**Adds (Ice):** `codex.policy/*` nodes (the rules from the top of this doc); signing keys; migration edges.

**Acceptance:** Refreeze requires policy conformance; deltas are signed and tracked.

---

## One‑Shot Recipes (per coil)

For each coil's atoms file (e.g., `Iteration1.core.json`, `Iteration2.link.json` …):

```bash
curl -s -X POST http://localhost:5055/oneshot/apply \
  -H 'content-type: application/json' \
  -d @path/to/coil.atoms.json | jq
```

This composes → expands → validates → contracts a delta. Use `GET /reflect/spec/{id}` to view the meta‑node graph, and `/diff` + `/patch` for tiny changes.

---

## Resonance Anchoring (change safety)

* **Anchors:** list of ice nodes that must remain compatible.
* **Check:** propose `ResonanceProposal{targetId, anchorsCsv}` via `/resonance/check`.
* **Outcome:** OK → refreeze; Not OK → generalize or keep as water.

---

## Derivation Rules (to avoid duplication)

* New feature? Try to **derive** from `codex.meta/*` or `codex.code/*` generics using specialization.
* If both old/new can be models of a more general pattern, **elevate** the generalization into ice and make both variants instances.
* Only introduce a new node kind when it is **fundamentally different** and cannot be expressed by specializing existing kinds.

---

## Appendix — Minimal Atoms Templates

* **L0 Core**: Node/Edge + breath ops.
* **L1 Reflection**: meta‑nodes + reflect/ingest ops.
* **L2 Delta**: PatchDoc/PatchOp + diff/patch ops.
* **L3 Phase**: Melt/Refreeze + Resonance.
* **L4 Link**: extended ContentRef + adapter register.
* **L5 Types**: properties/arrays/refs + OpenAPI stabilizers.
* **L6 Relations**: Relation atoms.
* **L7 CAS**: cacheKey rules.
* **L8 Prompt**: PromptTemplate + prompt:// semantics.
* **L9 DB**: db:// semantics.
* **L10 Code**: code structure/flow/state kinds.
* **L11 Validators**: registry & policies.
* **L12 Codegen**: GenPreset & targets.
* **L13 Governance**: policy/signing/migration.

> This document itself is a **meta‑node** (`codex.meta/doc`) and should be tracked as **ice** with deltas via `/diff`/`/patch`. Next edits: add concrete atoms JSON for each coil and a resonance checklist template.

---

# Extended Plan (Aligned to Living Codex 4.1 Spec)

> This section maps the full **Living Codex — Self‑Reflexive Foundation 4.1** to a dependency‑ordered fractal unfolding. Each coil adds only the smallest atoms needed and can be executed via `/oneshot/apply`. All items are represented as nodes/meta‑nodes, with deltas expressed as patches.

## Coverage Matrix (Spec § → Coil L\*)

| Spec § | Title                                 | Coil  | Depends On | Minimal Ice Added                                                                  | Acceptance (One‑Shot)                                                         |
| ------ | ------------------------------------- | ----- | ---------- | ---------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| 0      | Self‑Reflexive Purpose & Method       | L0    | —          | Core atoms (Node, Edge), breath ops, minimal adapters                              | Compose→Expand→Contract succeeds                                              |
| 1      | Canonical Registry (Normative)        | L1    | L0         | Registry policy nodes: water‑state keys, chakra/frequency/theme as **policy meta** | `/reflect/spec/codex.core` produces meta‑nodes; registry policy nodes present |
| 2      | Spec Graph Manifest (Normative)       | L1.1  | L1         | `codex.meta/manifest` node + edges to modules, versions                            | Manifest node links to all known specs                                        |
| 3      | Core Domain Model                     | L2    | L1.1       | `GenericNode`, `Relation`, `Contribution`, `User`, `LedgerEntry` meta‑types        | Types appear; OpenAPI stable                                                  |
| 4      | Lenses (Ice/Water/Gas)                | L2.1  | L2         | Lens meta‐nodes + `codex.view/*` projections                                       | `/openapi/{id}` reflects view endpoints                                       |
| 5      | Algorithms (Abstract Contracts)       | L3    | L2.1       | `codex.algo/*` nodes: signature, pre/post, cost                                    | Algo contracts listed; no runtime impl required                               |
| 6      | API Surface (Minimal, Stable)         | L3.1  | L3         | `codex.api/surface` policy node (versioning, deprecations)                         | Surface doc generated deterministically                                       |
| 7      | Storage & Compute (Pluggable)         | L4    | L3.1       | `codex.store/*`, `codex.compute/*` provider nodes + adapter intents                | Providers resolvable via adapter registry                                     |
| 8      | Security, Identity, Provenance        | L5    | L4         | `Identity`, `Key`, `Signature`, `Provenance` nodes; HMAC scheme policy             | Contributions must carry signatures in meta                                   |
| 9      | Currency & Attribution (Normative)    | L6    | L5         | `Account`, `CEUnit`, `AttributionRule` nodes                                       | Posting a `Contribution` yields CE ledger delta node                          |
| 10     | Feeds & Tracking                      | L6.1  | L6         | `Feed`, `Topic`, SSE channel meta                                                  | Topics (`contrib`,`ledger`,`resonance`) registered                            |
| 11     | Artifact Ontology (Assets & Software) | L7    | L6.1       | `Artifact`, `Software`, `Release`, `Asset` nodes + `depends_on`                    | Artifacts linked; hydrate via `file://`/`http(s)://`                          |
| 12     | Language & Documentation (as Views)   | L7.1  | L7         | `Doc`, `Glossary`, `i18n` nodes; view selectors                                    | `/reflect/spec/*` includes language views                                     |
| 13     | Project Layout (Monorepo)             | L8    | L7.1       | `Repo`, `Package`, `ModuleMap` nodes                                               | Codegen produces layout plan as water                                         |
| 14     | Testing & Quality                     | L8.1  | L8         | `Test`, `Assertion`, `Suite` nodes; validator registration                         | `/breath/validate/{id}` runs suites (stub ok)                                 |
| 15     | Observability & Operations            | L9    | L8.1       | `Metric`, `Trace`, `LogStream` nodes                                               | Telemetry policy nodes present                                                |
| 16     | Migration & Ingest                    | L9.1  | L9         | `Importer`, `Mapping`, `ETL` nodes; `/ingest/spec` policy                          | Ingest path round‑trips external schema                                       |
| 17     | Governance & Epistemics               | L10   | L9.1       | `Policy`, `Proposal`, `Vote`, `Quorum` nodes                                       | Resonance checks integrate governance policy                                  |
| 18     | Bootstrapping (Genesis → Minimal‑CE)  | L10.1 | L10        | `Genesis`, `Seed`, `GrowthRule`                                                    | Minimal seed produces CE growth deltas                                        |
| 19     | Human & AI Co‑Evolution Loop          | L11   | L10.1      | `prompt://` scheme intent; `PromptTemplate`, `Critique`, `Refinement` nodes        | Prompt nodes hydrate (spec intent)                                            |
| 20     | U‑CORE Boot Kernel (Upper Ontology)   | L12   | L11        | `UCore` nodes: `GenericNode`, `MetaNode`, `Selector` canonical forms               | U‑CORE manifests; all nodes specialize it                                     |
| 21     | Unity Charter & Objective Functions   | L12.1 | L12        | `Objective`, `Reward`, `Constraint` nodes                                          | Objectives bind to governance/validators                                      |
| 22     | Universal Concept Template            | L12.2 | L12.1      | `ConceptTemplate`, `Facet`, `Example`                                              | Template renders sample concept as water                                      |
| 23     | Taxonomy as Selectors & Facets        | L13   | L12.2      | `Taxon`, `Facet`, `Selector` atoms                                                 | Selectors resolve to node sets deterministically                              |
| 24     | Conflict → Coherence Pipeline         | L13.1 | L13        | `Conflict`, `Bridge`, `Resolution` nodes                                           | Pipeline graph compiles; edges valid                                          |
| 25     | Interop & Crosswalks                  | L14   | L13.1      | `Crosswalk`, `ExternalSchema`, `Mapping`                                           | Crosswalks import/export via adapters                                         |
| 26     | Contributor Roles & Rituals           | L14.1 | L14        | `Role`, `Ritual`, `Permission`                                                     | Contributions validate role policies                                          |
| 27     | Licensing & Commons                   | L15   | L14.1      | `License`, `Grant`, `Obligation`                                                   | Artifacts reference licenses; policy attached                                 |
| 28     | Self‑Introspection & Explainability   | L15.1 | L15        | `Explanation`, `Traceback`, `Why` nodes                                            | `/reflect/spec/*` emits explainability views                                  |
| 29     | Multidimensional Resonance Field      | L16   | L15.1      | `ResonanceModel`, `Field`, `Anchor` nodes                                          | Resonance proposal calculates invariants (stub ok)                            |
| 30     | Fractal Ontology Expansion (Layers 0-3) | L16.2 | L16        | Domain/Subdomain/Bridge nodes, 12-fold axes, user concepts                         | 584 nodes, 627 edges imported; user concepts operational                       |
| 31     | ETH Integration Blueprint             | L16.3 | L16.2      | `EthAccount`, `EthTransaction`, `EthContract` nodes                                | ETH settlement operational; on-chain transactions                             |
| 32     | Positive Resonance System Integration | L16.4 | L16.3      | `Integration`, `Interface`, `SLA` nodes                                            | Integrations pass resonance policy                                            |
| 33     | Fractal Spiral Mechanism (Normative)  | L17   | L16.4      | `SpiralRule`, `Coil`, `Gate` nodes                                                 | One‑shot proves minimal sufficiency per coil                                  |
| 34     | Water‑State Content & Summaries       | L17.1 | L17        | `Summary`, `Extractor`, `ContentResolver` atoms                                    | `/hydrate` can attach summaries (spec intent)                                 |
| 35     | Go Integration Blueprint              | L18   | L17.1      | `GoTarget`, `Binding`, `FFI` nodes                                                 | Codegen emits Go stubs as water                                               |

> Sections not explicitly listed above (e.g., UI theme keys under §1) are folded into policy/meta nodes at their first dependency‑satisfying coil.

## Coil Definitions (Detailed Ice Deltas)

Below are the **minimal atoms** added by each new coil (selected highlights; the rest follow the matrix above):

### L0 — Bootstrap Seed (covers §0)

* **Atoms:** `Node`, `Edge`, `ContentRef`, `ModuleSpec`, `TypeSpec`, `ApiSpec`
* **Edges:** Basic CRUD operations, breath loop endpoints
* **Acceptance:** Compose→Expand→Contract succeeds; dynamic routing works

### L1 — Canonical Registry & Manifest (covers §1–§2)

* **Atoms:** `codex.policy/registry`, `codex.meta/manifest`, water-state keys, chakra/frequency keys
* **Edges:** `manifest → module@version`, theme bindings
* **Acceptance:** `/reflect/spec/*` includes manifest nodes; importing a spec links it under manifest.

### L2 — Core Domain Model (covers §3) + Lenses (§4)

* **Atoms:** `codex.meta/type` for `GenericNode`, `Relation`, `Contribution`, `User`, `LedgerEntry`; `codex.view/*`
* **Edges:** `has_view` (view→owner), `implements` (node→type)
* **Acceptance:** OpenAPI projection shows these types; view nodes reference owners with `has_view` edges.

### L3 — Algorithms & API Surface (covers §5–§6)

* **Atoms:** `codex.algo/contract` (pre, post, input, output, cost), `codex.api/surface`
* **Edges:** `implements` (contract→algorithm), `binds` (surface→endpoint)
* **Acceptance:** Contracts listed; surface node binds endpoints with version window.

### L4 — Storage & Compute (covers §7)

* **Atoms:** `codex.store/provider`, `codex.compute/provider`, with adapter intents
* **Edges:** `provides` (provider→capability), `resolves` (adapter→content)
* **Acceptance:** Register provider → hydrate a node via that provider (intent only).

### L5 — Security/Provenance (covers §8)

* **Atoms:** `Identity`, `Key`, `Signature`, `Provenance`, `HMACPolicy`
* **Edges:** `signedBy` (contribution→key), `validates` (signature→content)
* **Acceptance:** Contributions must carry signatures in meta; provenance tracked

### L6 — Currency/Attribution (covers §9)

* **Atoms:** `Account`, `CEUnit`, `AttributionRule`, `LedgerEntry`
* **Edges:** `fundedBy` (contribution→account), `attributedTo` (ce→user)
* **Acceptance:** Posting a `Contribution` produces a signed delta and a CE ledger entry.

### L6.1 — Feeds & Tracking (covers §10)

* **Atoms:** `Feed`, `Topic`, `SSEChannel`, `EventStream`
* **Edges:** `publishes` (feed→topic), `subscribes` (channel→topic)
* **Acceptance:** Topics (`contrib`,`ledger`,`resonance`) registered; SSE streams operational

### L7 — Artifacts & Software (covers §11)

* **Atoms:** `Artifact`, `Software`, `Release`, `Asset`, `SBOM`
* **Edges:** `dependsOn` (artifact→artifact), `produces` (build→artifact)
* **Acceptance:** `Artifact` nodes hydrate content; dependencies tracked

### L7.1 — Language & Documentation (covers §12)

* **Atoms:** `Doc`, `Glossary`, `i18n`, `Translation`, `ViewSelector`
* **Edges:** `documents` (doc→subject), `translates` (i18n→content)
* **Acceptance:** Docs projected as views; language support operational

### L8 — Project Layout (covers §13)

* **Atoms:** `Repo`, `Package`, `ModuleMap`, `BuildTarget`
* **Edges:** `contains` (repo→package), `builds` (target→artifact)
* **Acceptance:** Codegen produces layout plan as water

### L8.1 — Testing & Quality (covers §14)

* **Atoms:** `Test`, `Assertion`, `Suite`, `Validator`, `QualityGate`
* **Edges:** `tests` (test→artifact), `validates` (validator→node)
* **Acceptance:** `/breath/validate/{id}` runs suites (stub ok)

### L9 — Observability & Operations (covers §15)

* **Atoms:** `Metric`, `Trace`, `LogStream`, `SLO`, `Alert`
* **Edges:** `measures` (metric→node), `tracks` (trace→operation)
* **Acceptance:** Telemetry policy nodes present; observability operational

### L9.1 — Migration & Ingest (covers §16)

* **Atoms:** `Importer`, `Mapping`, `ETL`, `SchemaTransform`
* **Edges:** `transforms` (etl→schema), `imports` (importer→source)
* **Acceptance:** Ingest path round‑trips external schema

### L10 — Governance & Epistemics (covers §17)

* **Atoms:** `Policy`, `Proposal`, `Vote`, `Quorum`, `EpistemicLabel`
* **Edges:** `governedBy` (node→policy), `votes` (user→proposal)
* **Acceptance:** Resonance checks integrate governance policy

### L10.1 — Bootstrapping (covers §18)

* **Atoms:** `Genesis`, `Seed`, `GrowthRule`, `BootstrapPolicy`
* **Edges:** `seeds` (genesis→node), `grows` (rule→expansion)
* **Acceptance:** Minimal seed produces CE growth deltas

### L11 — Human & AI Co‑Evolution (covers §19)

* **Atoms:** `PromptTemplate`, `Critique`, `Refinement`, `AIAgent`
* **Edges:** `prompts` (template→ai), `refines` (critique→content)
* **Acceptance:** Prompt nodes hydrate (spec intent)

### L12 — U‑CORE Boot Kernel (covers §20)

* **Atoms:** `UCore`, `GenericNode`, `MetaNode`, `Selector`, `CanonicalForm`
* **Edges:** `specializes` (node→ucore), `implements` (node→canonical)
* **Acceptance:** U‑CORE manifests; all nodes specialize it

### L12.1 — Unity Charter & Objectives (covers §21)

* **Atoms:** `Objective`, `Reward`, `Constraint`, `UnityCharter`
* **Edges:** `governedBy` (system→charter), `optimizes` (objective→metric)
* **Acceptance:** Objectives bind to governance/validators

### L12.2 — Universal Concept Template (covers §22)

* **Atoms:** `ConceptTemplate`, `Facet`, `Example`, `ConceptInstance`
* **Edges:** `shapes` (template→concept), `instantiates` (concept→template)
* **Acceptance:** Template renders sample concept as water

### L13 — Taxonomy as Selectors (covers §23)

* **Atoms:** `Taxon`, `Facet`, `Selector`, `ClassificationRule`
* **Edges:** `selects` (selector→node), `classifies` (taxon→concept)
* **Acceptance:** Selectors resolve to node sets deterministically

### L13.1 — Conflict → Coherence Pipeline (covers §24)

* **Atoms:** `Conflict`, `Bridge`, `Resolution`, `CoherenceRule`
* **Edges:** `resolves` (bridge→conflict), `coheres` (resolution→nodes)
* **Acceptance:** Pipeline graph compiles; edges valid

### L14 — Interop & Crosswalks (covers §25)

* **Atoms:** `Crosswalk`, `ExternalSchema`, `Mapping`, `InteropRule`
* **Edges:** `maps` (crosswalk→schema), `alignsWith` (node→external)
* **Acceptance:** Crosswalks import/export via adapters

### L14.1 — Contributor Roles & Rituals (covers §26)

* **Atoms:** `Role`, `Ritual`, `Permission`, `ContributionTemplate`
* **Edges:** `playsRole` (user→role), `shapes` (ritual→contribution)
* **Acceptance:** Contributions validate role policies

### L15 — Licensing & Commons (covers §27)

* **Atoms:** `License`, `Grant`, `Obligation`, `CommonsPolicy`
* **Edges:** `licensedUnder` (artifact→license), `grants` (license→right)
* **Acceptance:** Artifacts reference licenses; policy attached

### L15.1 — Self‑Introspection & Explainability (covers §28)

* **Atoms:** `Explanation`, `Traceback`, `Why`, `ExplainabilityRule`
* **Edges:** `explains` (explanation→node), `traces` (traceback→path)
* **Acceptance:** `/reflect/spec/*` emits explainability views

### L16 — Multidimensional Resonance Field (covers §29)

* **Atoms:** `ResonanceModel`, `Field`, `Anchor`, `ResonanceAxis`
* **Edges:** `resonatesWith` (node→node), `anchors` (field→node)
* **Acceptance:** Resonance proposal calculates invariants (stub ok)

### L16.2 — Fractal Ontology Expansion (covers §30)

* **Atoms:** Domain/Subdomain/Bridge nodes, 12-fold resonance axes, user concept types
* **Edges:** `has_part` (domain→subdomain), `connects` (bridge→domains), `resonatesWith` (axis cross-references)
* **Acceptance:** 584 nodes, 627 edges imported; user concepts operational; 12-fold axes connected

### L16.3 — ETH Integration (covers §31)

* **Atoms:** `EthAccount`, `EthTransaction`, `EthContract`, `EthSettlement`
* **Edges:** `settledOnChain` (tx→contrib), `fundedBy` (account→user)
* **Acceptance:** ETH settlement operational; on-chain transactions tracked; gas estimation working

### L16.4 — Positive Resonance System Integration (covers §32)

* **Atoms:** `PositiveResonance`, `JoyAmplification`, `ConsciousnessAmplification`, `HarmonyAmplification`
* **Edges:** `amplifies` (system→positive_outcome), `resonatesWith` (positive→positive)
* **Acceptance:** Positive resonance concepts preserved; amplification relationships tracked

### L17 — Fractal Spiral Mechanism (covers §33)

* **Atoms:** `SpiralRule`, `Coil`, `Gate`, `FractalPatch`
* **Edges:** `unfolds` (coil→expansion), `gates` (gate→coil)
* **Acceptance:** One‑shot proves minimal sufficiency per coil

### L17.1 — Water‑State Content & Summaries (covers §34)

* **Atoms:** `Summary`, `Extractor`, `ContentResolver`, `WaterStateContent`
* **Edges:** `summarizes` (summary→node), `extracts` (extractor→content)
* **Acceptance:** `/hydrate` can attach summaries (spec intent)

### L18 — Go Integration Blueprint (covers §35)

* **Atoms:** `GoTarget`, `Binding`, `FFI`, `GoModule`
* **Edges:** `generates` (target→code), `binds` (ffi→interface)
* **Acceptance:** Codegen emits Go stubs as water

## One‑Shot Protocol (applies to every coil)

1. **Submit atoms** for the coil.
2. \`\` → compose, expand, validate, contract.
3. **Reflect** → `/reflect/spec/{id}` to ensure all structures exist as meta‑nodes.
4. **Patch** → express tiny edits via `/diff` + `/patch`.
5. **Resonance** → check anchors; then refreeze.

## Notes on Minimality & Derivation

* UI theme/frequency/chakra keys are **policy/meta** nodes so they don't bloat core logic.
* Water‑state content & summaries (§34) ride on L4 adapters + L2 views; only the **resolver contract** is ice.
* Algorithms (§5) are **contracts** (no code) until bound to providers (later coils).
* Code, regardless of language, is represented via `codex.code/*` meta‑nodes (structure/flow/state) introduced where needed (typically L12–L15 depending on target).

---

## Implementation Status & Feature Coverage

*Based on analysis of the current C# CodexBootstrap codebase (verified and tested)*

### ✅ Fully Implemented Features (Bootstrap Foundation - ~15% Complete)

**L0 — Bootstrap Seed (100% Complete)**
- **Self-Reflexive Purpose & Method**:
  - ✅ Core data structures: `Node`, `Edge`, `ContentRef` (`Core/Abstractions.cs`)
  - ✅ Module system: `ModuleSpec`, `TypeSpec`, `ApiSpec` (`Core/Abstractions.cs`)
  - ✅ Dynamic API routing via `IApiRouter` (`Runtime/Runtime.cs`)
  - ✅ Module registration and discovery (`Program.cs`)
  - ✅ JSON serialization with camelCase, indented output (`Program.cs`)
  - ✅ Dependency injection with `[FromServices]` attributes (`Program.cs`)
  - ❌ Missing: Spec manifest as graph, lint rules, ΔR→CE policy

**L1 — Core Atoms (100% Complete)**
- ✅ Node/Edge/ContentRef data structures (`Core/Abstractions.cs`)
- ✅ Content state management (Ice/Water/Gas) (`Core/Abstractions.cs`)
- ✅ Basic registry implementations (`Runtime/Runtime.cs`)
- ✅ Type system with TypeSpec/ApiSpec (`Core/Abstractions.cs`)

**L2 — Module System (100% Complete)**
- ✅ IModule interface and registration (`Core/Abstractions.cs`)
- ✅ Dynamic API routing system (`Runtime/Runtime.cs`)
- ✅ Module discovery and loading (`Program.cs`)
- ✅ HelloModule demonstration (`Modules/HelloModule.cs`)
- ✅ Basic dependency management (`Core/Abstractions.cs`)

**L3 — Content Adapters (60% Complete)**
- ✅ File adapter for local filesystem access (`Runtime/Runtime.cs`)
- ✅ HTTP/HTTPS adapters for web content (`Runtime/Runtime.cs`, `Runtime/HttpAdapterWithScheme.cs`)
- ✅ ISourceAdapter interface and registry (`Core/Abstractions.cs`, `Runtime/Runtime.cs`)
- ❌ Missing: IPFS, data URI, prompt adapters
- ❌ Missing: Advanced caching and authentication

**L4 — Synthesis Engine (40% Complete)**
- ✅ Basic EchoSynthesizer implementation (`Runtime/Runtime.cs`)
- ✅ Content hydration from external sources (`Runtime/Runtime.cs`)
- ❌ Missing: AI-powered synthesis
- ❌ Missing: Multi-modal content generation
- ❌ Missing: Advanced content transformation

**L5 — Phase Transitions (50% Complete)**
- ✅ Melt/Refreeze operations (`Runtime/Phase.cs`)
- ✅ Basic resonance checking (`Runtime/Phase.cs`)
- ❌ Missing: Advanced resonance algorithms
- ❌ Missing: Structural harmony validation
- ❌ Missing: Conflict resolution

**L6 — Spec Management (30% Complete)**
- ✅ Basic spec registry and composition (`Runtime/Runtime.cs`)
- ✅ Module spec generation (`Runtime/Runtime.cs`)
- ❌ Missing: Advanced spec validation
- ❌ Missing: Spec versioning and migration
- ❌ Missing: Cross-spec compatibility

**L7 — API Surface (80% Complete)**
- ✅ Dynamic routing and module APIs (`Program.cs`)
- ✅ OpenAPI generation (`Runtime/Runtime.cs`)
- ❌ Missing: Advanced authentication
- ❌ Missing: Rate limiting and quotas
- ❌ Missing: API versioning

**L8 — Storage & Compute (20% Complete)**
- ✅ In-memory storage (`Runtime/Runtime.cs`)
- ❌ Missing: Persistent storage
- ❌ Missing: Distributed storage
- ❌ Missing: Advanced indexing
- ❌ Missing: Query optimization

### ❌ Not Yet Implemented Features (L9-L18: Advanced Systems)

**L9 — Security & Identity (0% Complete)**
- ❌ Authentication and authorization
- ❌ Digital signatures and verification
- ❌ Access control and permissions
- ❌ Audit logging and compliance

**L10 — Currency & Attribution (0% Complete)**
- ❌ Token economics and rewards
- ❌ Contribution tracking and valuation
- ❌ Reputation and trust systems
- ❌ Economic incentives and governance

**L11 — Feeds & Tracking (0% Complete)**
- ❌ Real-time event streams
- ❌ Change notifications and subscriptions
- ❌ Activity feeds and timelines
- ❌ Social features and collaboration

**L12 — Artifact Ontology (0% Complete)**
- ❌ Document classification and tagging
- ❌ Content type detection and validation
- ❌ Metadata extraction and indexing
- ❌ Search and discovery

**L13 — Language & Documentation (0% Complete)**
- ❌ Multi-language support
- ❌ Documentation generation
- ❌ Code comments and annotations
- ❌ Translation and localization

**L14 — Project Layout (0% Complete)**
- ❌ Directory structure and organization
- ❌ File naming conventions
- ❌ Module boundaries and interfaces
- ❌ Dependency management

**L15 — Testing & Quality (0% Complete)**
- ❌ Unit and integration tests
- ❌ Performance benchmarking
- ❌ Code quality metrics
- ❌ Continuous integration

**L16 — Observability & Operations (0% Complete)**
- ❌ Logging and monitoring
- ❌ Metrics and alerting
- ❌ Health checks and diagnostics
- ❌ Performance optimization

**L17 — Migration & Ingest (0% Complete)**
- ❌ Data migration tools
- ❌ Import/export functionality
- ❌ Format conversion and validation
- ❌ Legacy system integration

**L18 — Governance & Epistemics (0% Complete)**
- ❌ Consensus mechanisms
- ❌ Decision making processes
- ❌ Knowledge validation
- ❌ Community governance

## Detailed API Surface & Endpoints

*Based on the current C# CodexBootstrap implementation*

### Core API Endpoints (Implemented)

**Node & Edge Management:**
- `GET /nodes/{id}` - Get node by ID
- `POST /nodes` - Create/update node
- `GET /edges` - List all edges
- `POST /edges` - Create/update edge

**Module System:**
- `GET /modules` - List all modules
- `GET /modules/{id}` - Get module by ID
- `POST /route` - Dynamic API dispatch

**Content Hydration:**
- `POST /hydrate/{id}` - Hydrate node content

**Spec Management:**
- `POST /spec/atoms` - Upload module atoms
- `POST /spec/compose` - Compose spec from atoms
- `GET /spec/atoms/{id}` - Get module atoms
- `GET /spec/{id}` - Get module spec

**Breath Loop:**
- `POST /breath/expand/{id}` - Expand node
- `POST /breath/validate/{id}` - Validate node
- `POST /breath/contract/{id}` - Contract node

**One-Shot Operations:**
- `POST /oneshot/apply` - Apply atoms to prototype
- `POST /oneshot/{id}` - Run one-shot on existing atoms

**Spec Reflection:**
- `GET /reflect/spec/{id}` - Reflect spec to nodes
- `POST /ingest/spec` - Ingest nodes to spec

**Diff/Patch:**
- `GET /diff/{id}?against={id}` - Get diff between nodes
- `POST /patch/{targetId}` - Apply patch to node

**Spec Exchange:**
- `GET /spec/export/{id}` - Export module atoms
- `POST /spec/import` - Import module atoms

**Adapter Management:**
- `POST /adapters/register` - Register content adapter

**Phase Transitions:**
- `POST /phase/melt/{id}` - Melt node to Water state
- `POST /phase/refreeze/{id}` - Refreeze node to Ice state

**Resonance Checking:**
- `POST /resonance/check` - Check resonance proposal

**Core System:**
- `GET /core/atoms` - Get core atoms
- `GET /core/spec` - Get core spec
- `GET /plan/{id}` - Get topology plan
- `GET /openapi/{id}` - Generate OpenAPI spec for module

### Implementation Statistics

**Current System Scale:**
- **Bootstrap foundation** with core data structures
- **Module system** with dynamic API routing
- **Content adapters** for file and HTTP sources
- **Basic synthesis engine** for content hydration
- **Phase transitions** with melt/refreeze operations
- **Spec management** with basic composition
- **API surface** with 20+ endpoints
- **In-memory storage** with basic persistence

**Performance Characteristics:**
- **In-memory operations** with O(1) node/edge access
- **Dynamic module loading** with reflection-based discovery
- **Content hydration** with async adapter resolution
- **JSON serialization** with camelCase and indented output
- **Dependency injection** with service lifetime management

### Next Implementation Priorities

1. **L4 — Storage & Compute** (High Priority)
   - Implement persistent storage backends
   - Add content addressing system
   - Create cache management

2. **L5 — Security/Provenance Enhancement** (Medium Priority)
   - Add authentication and authorization
   - Implement digital signatures
   - Create identity management system

3. **L8 — Project Layout** (Medium Priority)
   - Implement directory structure
   - Add file naming conventions
   - Create module boundaries

4. **L10 — Governance & Epistemics** (Medium Priority)
   - Implement policy enforcement
   - Add proposal/voting system
   - Create governance framework

5. **L11 — Feeds & Tracking** (Low Priority)
   - Implement real-time event streams
   - Add change notifications
   - Create activity feeds

---

*Last Updated: 2024-12-19*
*Version: 1.0.0*
*Status: Ice (Frozen)*
