# GAP ANALYSIS — Spec vs Implementation

This document lists notable gaps between the specs and current implementation, flags stubs/mocks/simulations, and records the closures applied in this session.

## Summary
- Implemented missing backend endpoint: `GET /contributions/stats/{userId}`
- Added UI route per spec: `/graph` with basic storage stats display
- Tests remain green (147 total; 135 passed, 12 skipped)

## UI Spec Coverage (specs/LIVING_UI_SPEC.md)
- Required routes (Level 1 IA):
  - Present: `/`, `/discover`, `/resonance`, `/news`, `/ontology`, `/people`, `/portals`, `/about` (core scaffolds present via atoms and pages for `/` and `/discover`).
  - Gap fixed: `/graph` page added (simple lens placeholder reading `/storage-endpoints/stats`).

- Lenses:
  - Present in defaults: Stream, Gallery (partial).
  - Gaps: Threads, Chats, Circles, Swipe, Nearby, Live, Making, Graph (interactive) — not yet generated; status should be Stub/Untested.

- RouteStatus tracking:
  - Present in code via `RouteStatus` enum, but not surfaced in UI badges. Opportunity to add status badges per route/lens.

## Endpoint Map Coverage (Spec §5)
- Concepts
  - Implemented: `GET /concepts`, `GET /concepts/{id}`, `POST /concepts`, `PUT /concepts/{id}`, `POST /concept/search`, `POST /concept/discover`.
  - Status: Simple.

- Resonance
  - Implemented: `POST /concepts/resonance/compare`, `POST /concepts/resonance/encode` (via modules; verify integration points).
  - Status: Simple/Untested.

- People / Discovery
  - Implemented: `POST /users/discover`, `GET /concepts/{conceptId}/contributors`.
  - Status: Simple.

- Contributions / Abundance / Ledger
  - Implemented: `POST /contributions/record`, `GET /contributions/user/{userId}`, `GET /rewards/user/{userId}`, `POST /rewards/claim`, `GET /ledger/balance/{address}`, `POST /ledger/transfer`, Abundance: `/contributions/abundance/*`.
  - Gap fixed: `GET /contributions/stats/{userId}` implemented with real aggregate data (counts, types, values, rewards, energy).
  - Note: ETH integration is stubbed when `_web3` is null.

- Storage / Graph
  - Implemented: `/storage-endpoints/nodes*`, `/storage-endpoints/edges*`, `/storage-endpoints/stats`.
  - UI binding now present at `/graph`.

- Spec / Meta
  - Implemented: `GET /spec/routes/all`, `POST /spec-driven/*` present; some generation paths remain placeholders.

## Stubs / Mocks / Simulations (Backend)
- Mock identity: `Modules/MockIdentityProvider.cs` — simulated login and user info.
- Api router placeholders: several modules default to `MockApiRouter` when DI not injected (should be avoided in production).
- ETH processing: user rewards and transfers simulate operations when `_web3` is null.
- Visual validation: placeholder image returned on error; pipeline exists but analysis may be mocked.
- SelfUpdateSystem / code generation: uses placeholder strings and files for generated DLLs.
- UCore LLM calculations: resonance and analysis include TODOs/fallback logic.
- Spec parsing (`SpecModule.ExtractResponseTypes`): returns empty list (placeholder).

## Changes Applied (This Session)
1) Backend: `GET /contributions/stats/{userId}`
   - File: `src/CodexBootstrap/Modules/UserContributionsModule.cs`
   - Computes totals, per-type counts, value aggregates, recent activity, energy level, and rewards summary.

2) UI: `/graph` page
   - File: `living-codex-ui/src/app/graph/page.tsx`
   - Displays counts from `/storage-endpoints/stats`; placeholder for interactive graph lens.

## Recommended Next Steps
- Surface `RouteStatus` in UI via badges per page/lens.
- Replace `MockApiRouter` fallbacks with DI-injected real router across modules.
- Gradually replace mocks in identity, ETH, and self-update with adapters.
- Implement additional lenses (Threads, Graph interactive) using `/storage-endpoints/*` and adapter pattern.
- Enhance SpecModule response type extraction and round-trip node generation.


