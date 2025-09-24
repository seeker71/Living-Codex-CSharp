# Living Codex UI Specification — Grounded in Current Implementation

_Last reviewed: 2025-09-24_

## 1. Purpose & Status
- **Purpose**: Provide a spec-first, resonance-driven user interface for exploring the Living Codex concept graph, people network, news feed, and self-updating tooling.
- **Status**: Functional prototype. All routes render and include loading/error handling, but most experiences depend on a backend at `http://localhost:5002`. When the backend is unavailable the UI falls back to seeded atoms or empty views rather than mutating data.
- **Scope**: Everything under `living-codex-ui` (Next.js App Router). This specification covers the navigation shell, resonance controls, knowledge exploration, creation tooling, developer utilities, UX primitives, and telemetry hooks confirmed in code as of this review.
- **Design themes confirmed in code**: “Everything is a node”, resonance-first exploration, adapter-based data access, AI-assisted helpers (concept creation, hot-reload regeneration), and instrumented interactions via contribution tracking.

## 2. Architecture Overview

### 2.1 Frontend stack
- Next.js App Router with TypeScript; all screens referenced in this spec live under `src/app`.
- Styling is Tailwind via `globals.css` plus component-level classes; lucide-react icons and emoji supply visual cues.
- Component primitives (cards, pagination, status badges) live in `src/components/ui` and are reused across routes.

### 2.2 Data access & caching
- `src/lib/config.ts` centralises `NEXT_PUBLIC_BACKEND_URL`; defaults to `http://localhost:5002`.
- `src/lib/api.ts` provides `apiCall` with timeout, retry, structured logging, and convenience wrappers (`api.get`, `api.post`, `endpoints.*`). Logs are cached in-memory and surfaced on the About page.
- `AtomFetcher` in `src/lib/atoms.ts` retrieves meta-nodes (`codex.ui.*`) from `/storage-endpoints`. `bootstrapUI()` seeds the backend with the default module/pages/lenses/actions/controls when the app loads in the browser.
- `defaultAtoms` define:
  - module: `ui.module.core` with the full route list.
  - pages: `ui.page.home`, `ui.page.discover`.
  - lenses: `lens.stream`, `lens.gallery`.
  - actions: `action.attune`.
  - controls: `controls.resonance` (axes, joy, serendipity).
- When `/storage-endpoints` calls fail, components fall back to these defaults.
- React Query (`@tanstack/react-query`) is the caching layer. `Providers` constructs a single `QueryClient` and wraps the tree.

### 2.3 State, auth & telemetry
- `AuthContext` stores `{ user, token, isAuthenticated, isLoading }`. It reads `auth_token`/`auth_user` from `localStorage`, validates tokens through `/auth/validate`, and exposes `login`, `register`, `logout`, `refreshUser`, `testConnection`.
- Successful login/registration expects backend responses in the form `{ success, token, user }`; failing responses surface inline errors.
- Most mutations and some views require an authenticated user ID; unauthenticated users see disabled buttons or fallback text.
- `useTrackInteraction` builds on `useRecordContribution` to post `"ui-interaction"` contributions to `/contributions/record`. It deduplicates page visits per session via an in-memory `Set`.

### 2.4 Layout, navigation & global controls
- `RootLayout` enforces dark mode (`<html class="dark">`), renders a header with the `Navigation` component, and mounts `GlobalControls` as a floating widget across all pages.
- `Navigation`:
  - Builds a primary/secondary menu from the route list; authenticated users gain People, Create, Portals, Dev, Profile, Swagger.
  - Uses `usePathname()` for active styling and collapses overflow items into a “More” dropdown (outside click handling is implemented).
  - Shows either a “Sign In” button or the user avatar/initial.
- `GlobalControls` fetch and persist per-user resonance/joy/serendipity/curiosity levels via `/user-preferences/{userId}/controls`, trigger `/serendipity/trigger` and `/curiosity/generate-prompt`, and log adjustments through `useTrackInteraction`. The UI currently surfaces suggestions via `alert()` dialogs.

## 3. Controls, Lenses & UX Primitives

- **ResonanceControls (`components/ui/ResonanceControls`)**
  - Uses `useResonanceControls` to read `codex.ui.controls` and mirrors the axes, joy, and serendipity options defined in `defaultAtoms`.
  - Emits control changes upward; skeleton state shown while loading.

- **StreamLens (`components/lenses/StreamLens`)**
  - Combines concepts (`useConceptDiscovery` → `/concept/discover` with fallback to `/concepts`) and people (`useUserDiscovery` → `/users/discover`) into a single feed.
  - Supports pagination via `PaginationControls`; sorts results if the lens ranking is `resonance*joy*recency`.

- **ThreadsLens** and **GalleryLens**
  - Fetch long-form conversations (`/threads/list`, `/threads/create`) and media items (`/gallery/list`) respectively.
  - Both expose creation dialogs, depend on `useTrackInteraction`, and are selectable from Discover’s lens tabs.
  - If the backend lacks the required endpoints these views fall back to zero-state messaging.

- **ChatsLens**
  - Provides a conversation-first view backed by `/threads/list`, `/threads/{threadId}`, and `/threads/{threadId}/reply`.
  - Supports new conversation creation, reply posting, and telemetry for thread selection and message sending.

- **NearbyLens**
  - Location-driven user discovery backed by `/users/discover` with `location` and `radiusKm` filters.
  - Provides manual search, radius slider, pagination, and per-user resonance metadata.

- **SwipeLens**
  - Rapid concept exploration that pulls batches from `/concept/discover` and presents one card at a time.
  - Supports skip/attune/amplify actions; advancing past the batch loads the next page automatically.

- **ConceptStreamCard**
  - Presents concept/user entries with attune/amplify/reflect/weave actions.
  - `useAttune` calls `/userconcept/link`; `useAmplify` records contributions via `/contributions/record`.
  - “Reflect” and “Weave” buttons integrate with `UXPrimitives`.

- **UX Primitives (`components/primitives/UXPrimitives`)**
  - `WeaveAction`: POST `/weave/create`.
  - `ReflectAction`: POST `/reflect/generate`.
  - `InviteAction`: POST `/invite/send`.
  - `AmplifyAction`: POST `/contributions/record`.
  - Each enforces user authentication and records contribution telemetry.

- **Developer primitives**
  - `CodeIDE`, `FileBrowser`, `CodeEditor` consume `/filesystem/files`, `/storage-endpoints/nodes/{id}`, `/filesystem/content/{id}`.
  - `RouteStatusBadge` labels route statuses (untested, fullyTested, etc.) and is used throughout graph views.

## 4. Route Reference (confirmed behaviour)

### 4.1 Public exploration

- **`/` – Home (Resonance Stream)**
  - Components: `ResonanceControls`, `StreamLens`, quick action buttons.
  - Data: `usePages`, `useLenses`, `useConceptDiscovery`, `useUserDiscovery`.
  - Notes: Fallback to `defaultAtoms` when backend pages/lenses fail; shows skeletons during loading.

- **`/discover` – Multi-lens exploration**
  - Lens tabs (Stream, Threads, Chats, Gallery, Nearby, Swipe) toggle dedicated components. Stream combines concepts and people; Threads and Chats are backed by `/threads/*` endpoints; Gallery pulls `/gallery/list`; Nearby queries `/users/discover`; Swipe serves one concept at a time with attune/amplify actions.
  - Shares control state with Home via `ResonanceControls`.

- **`/news` – News feed**
  - Supports categories: personalized (requires auth), trending, topical, search.
  - Data paths:
    - `endpoints.getNewsFeed`, `getTrendingTopics`, `getNewsStats`.
    - Direct `fetch(buildApiUrl(...))` calls for `/news/latest`, `/news/search`, `/news/trending`, `/news/read`.
  - Tracks page visits and search interactions via `useTrackInteraction`.

- **`/resonance` – Energy metrics**
  - Reads collective metrics (`/contributions/abundance/collective-energy`) and per-user data (`/contributions/abundance/contributor-energy/{userId}`).
  - Shows cards, progress bars, and error alerts.

- **`/ontology` – U-CORE browser**
  - Fetches axes from `/storage-endpoints/nodes?typeId=codex.ontology.axis` with fallback to hard-coded defaults.
  - Concept listing uses `/storage-endpoints/nodes/search`; relationships rely on `/storage-endpoints/edges`.
  - Provides search, filter, pagination, and status commentary in console logs.

- **`/about` – System overview**
  - Displays mission copy, multiple view modes (human/technical/data).
  - Surfaces `api.health()` response and the `ApiLogger` history (`api.getLogs()`, `getFailedCalls()`, `getSlowCalls()`).

### 4.2 Graph & storage exploration

- **`/graph` – Knowledge graph dashboard**
  - Tabs: Overview (storage stats), Nodes, Edges, Insights.
  - Uses hooks: `useStorageStats`, `useHealthStatus`, `useNodeTypes`, `useAdvancedNodeSearch`, `useAdvancedEdgeSearch`.
  - Provides filters, pagination, view toggles, and status badges.

- **`/nodes` – Node browser**
  - `NodeBrowser` fetches `/storage-endpoints/nodes`, filters by search/type/state, and exposes selected node/edge summaries.
  - Displays tips and a node state legend.

- **`/node/[id]` – Node detail**
  - Loads node via `/storage-endpoints/nodes/{id}` and related edges via `/storage-endpoints/edges?nodeId=`.
  - Fetches neighbour nodes individually; supports tabbed views for details/content/relationships/metadata and inline editing (PUT back to storage endpoints).

- **`/edge/[fromId]/[toId]` – Edge detail**
  - Reads edge and both endpoint nodes, displays metadata cards, relationship summary, and navigation aids.

### 4.3 Creation & community

- **`/people` – Resonance-based discovery**
  - Discovery modes: interests, location, concept-centric.
  - Posts to `/users/discover`; optionally fetches `/concepts/{conceptId}/contributors`.
  - Shows resonance overlap estimates and supports pagination.

- **`/create` – Concept creation studio**
  - Tabs: Concept basics, AI assistance, Visual creation.
  - Calls `/ai/extract-concepts` for suggestions, `/concept/create` to persist, `/image/concept/create` to generate imagery.
  - Tracks user actions via `useTrackInteraction`.

- **`/portals` – External/temporal portals**
  - Loads portal lists from `/portal/list`, `/temporal/portals/list`, `/portal/explorations/list`.
  - Allows creating portals (`/portal/connect`), launching temporal explorations (`/temporal/explore`), and disconnecting portals (`/portal/disconnect`).
  - Includes modal flows and action tracking.

### 4.4 User & authentication

- **`/auth` – Sign in / registration**
  - Toggle between login and register forms.
  - Uses `useAuth.login`/`register`; provides backend connectivity test via `testConnection()` (pings `/health`).
  - Shows feature preview and switchers.

- **`/profile` – Account management**
  - Tabs for profile, interests, belief system, location (forms inside `ProfilePage`).
  - Reads `/auth/profile/{userId}` and `/userconcept/belief-system/{userId}`; updates via `api.put('/identity/{userId}')` and `api.post('/userconcept/belief-system/register')`.
  - Supports interest tag management and slider controls for resonance thresholds.

- **Global controls** (see §2.4) also require authentication for persistence.

### 4.5 Developer & system tooling

- **`/code` – In-browser IDE**
  - `CodeIDE` splits `FileBrowser` and `CodeEditor`.
  - `FileBrowser` pulls `/filesystem/files?limit=1000`, builds a tree, and marks RouteStatus.
  - `CodeEditor` loads `/storage-endpoints/nodes/{id}` metadata and `/filesystem/content/{id}` body; saves via PUT to `/filesystem/content/{id}`.

- **`/dev` – Hot reload dashboard (auth required)**
  - Redirects unauthenticated users to `/auth`.
  - `HotReloadDashboard` uses `useHotReload`/`useHotReloadNotifications` to control `/self-update/*` endpoints (start/stop watching, regenerate components, hot swap, history polling).
  - Presents AI provider info (OpenAI, Cursor, Ollama) and quick test links to primary pages.

## 5. Backend interaction summary

- **Storage & graph**: `/storage-endpoints/nodes`, `/storage-endpoints/nodes/{id}`, `/storage-endpoints/nodes/search`, `/storage-endpoints/edges`, `/storage-endpoints/edges/{from}/{to}`, `/storage-endpoints/stats`, `/storage-endpoints/types`.
- **Filesystem**: `/filesystem/files`, `/filesystem/content/{id}` (GET/PUT).
- **Concepts & discovery**: `/concept/discover`, `/concepts`, `/concept/create`, `/concepts/{id}/contributors`, `/userconcept/link`, `/userconcept/unlink`, `/userconcept/user-concepts/{userId}`.
- **News**: `/news/trending`, `/news/latest`, `/news/search`, `/news/feed/{userId}`, `/news/read`, `/news/stats`.
- **Resonance & contributions**: `/contributions/record`, `/contributions/abundance/collective-energy`, `/contributions/abundance/contributor-energy/{userId}`, `/contributions/user/{userId}`.
- **People & portals**: `/users/discover`, `/portal/list`, `/portal/connect`, `/portal/disconnect`, `/portal/explorations/list`, `/temporal/portals/list`, `/temporal/explore`.
- **Preferences & experience**: `/user-preferences/{userId}/controls`, `/serendipity/trigger`, `/curiosity/generate-prompt`.
- **AI helpers**: `/ai/extract-concepts`, `/image/concept/create`, `/threads/list`, `/threads/create`, `/gallery/list`.
- **Auth**: `/auth/login`, `/auth/register`, `/auth/logout`, `/auth/validate`, `/auth/profile/{userId}`, `/auth/change-password`.
- **Self-update/hot reload**: `/self-update/hot-reload-status`, `/self-update/start-watching`, `/self-update/stop-watching`, `/self-update/regenerate-component`, `/self-update/hot-swap`, `/self-update/hot-reload-history`.
- **Health**: `/health`.

Every endpoint call already implements optimistic logging and error capture; unhandled failures usually show console warnings and an empty state instead of throwing.

## 6. Telemetry & contribution tracking
- `useTrackInteraction(entityId, interactionType, metadata)` posts to `/contributions/record` with a normalized `contributionType` map (page-visit → View, button-click → Share, etc.).
- Page components use the hook for visits (`news`, `people`, `create`, `portals`, `node`, `edge`) and for key actions (AI assistance, portal creation, thread creation).
- Attune/Amplify buttons trigger dedicated mutations; success handlers invalidate cached queries so downstream views refresh.

## 7. Bootstrap & offline behaviour
- On first render in the browser, `bootstrapUI()` attempts to create the default module/pages/lenses/actions/controls plus sample concepts/users via `/storage-endpoints/nodes`. Errors are ignored so that pre-seeded backends are left untouched.
  - Concepts seeded: `concept-quantum-resonance`, `concept-fractal-consciousness`, `concept-abundance-mindset`.
  - Users seeded: `user-alex-resonance`, `user-maya-fractal`.
- If the backend is unavailable:
  - Navigation still renders because route metadata is defined locally.
  - `ResonanceControls`/`StreamLens` show skeletons and then fall back to defaults or empty states.
  - Creation and action buttons remain visible but underlying requests will fail; errors are logged to the console.

## 8. Loading, error & UX patterns
- Lists/panels use animated skeletons (`animate-pulse`) while data loads.
- Error blocks use contextual colours (red for failures, yellow for warnings). Some flows display `alert()` (serendipity/curiosity suggestions) pending richer UX.
- Modal patterns are implemented with fixed overlays in ThreadsLens, GalleryLens, and Portals.
- Forms consistently use the `input-standard` Tailwind utility for styling.

## 9. Known gaps & follow-ups
- Discover tabs other than Stream show metadata but do not yet render their dedicated lenses within the page.
- Many endpoints assume backend implementations that may not exist; failures fall back silently. The specification recommends surfacing inline errors before production use.
- `StreamLens` ranking expects each item to expose `timestamp`; backend responses should supply it to avoid NaN results.
- `GlobalControls` rely on `alert()` for surfaced suggestions and do not persist expanded/collapsed state per session.
- `ThreadsLens`/`GalleryLens` creation flows require authenticated users; unauthenticated usage currently fails silently.
- `CodeEditor` refreshes node metadata after save, but there is no diffing or conflict detection.
- Hot reload dashboard assumes `/self-update/*` endpoints; without them it reports a disconnected state.
- Accessibility: navigation relies on horizontal scroll; additional keyboard focus handling may be required.
- i18n: the UI is single-language; placeholders for localisation are not present.

## 10. Test coverage snapshot
- Jest tests exist for core UI primitives (`CodeEditor`, `FileBrowser`, `RouteStatusBadge`) and multiple integration scenarios under `src/__tests__` (dark theme accessibility, backend integration smoke, minimal UI bootstrap).
- Tests expect backend endpoints to be available or mocked; see each spec for exact fixtures.
- No Playwright/E2E runner is bundled; manual verification remains necessary for multi-step flows.

---

This specification mirrors the concrete implementation inside `living-codex-ui` and should be kept in sync with code changes (components, routes, or endpoints). Update the sections above whenever new features, lenses, or data contracts land in the repository.
