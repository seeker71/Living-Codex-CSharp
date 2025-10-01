# UI Test Coverage Plan - Living Codex
**Date**: 2025-10-01  
**Goal**: Ensure all 13 main pages have test coverage for core features with real API validation

## Test Coverage Matrix

### Priority 1: Core User Journey Pages

| Page | Route | Main Features | Test File | Status | API Endpoints Used |
|------|-------|---------------|-----------|--------|-------------------|
| **Home** | `/` | Landing, navigation, quick actions | `home-page-integration.test.tsx` | ✅ EXISTS | `/health` |
| **Discover** | `/discover` | Lens switching (Stream/Gallery/Threads), concept browsing | ❌ **MISSING** | ❌ NEEDED | `/concept/discover`, `/users/discover`, `/gallery/list` |
| **Profile** | `/profile` | User info display, edit profile, contributions | `profile-page.test.tsx` + 2 more | ⚠️ **FAILING** | `/profile/{userId}`, `/contributions/user/{userId}` |
| **News** | `/news` | News feed, item details, concepts | `news-item-flows.test.tsx` + 1 more | ✅ EXISTS | `/news/feed/{userId}`, `/news/item/{id}` |
| **Auth** | `/auth` | Login, register, validation | `auth-route-integration.test.tsx` | ✅ EXISTS | `/auth/login`, `/auth/register` |

### Priority 2: Exploration & Knowledge Pages

| Page | Route | Main Features | Test File | Status | API Endpoints Used |
|------|-------|---------------|-----------|--------|-------------------|
| **Ontology** | `/ontology` | U-CORE navigation, axis exploration | `ontology/integration.test.tsx` + 1 more | ✅ EXISTS | `/ucore/ontology`, `/ontology/axes` |
| **Graph** | `/graph` | Node visualization, stats | `graph-filters.test.tsx` | ✅ EXISTS | `/storage-endpoints/stats` |
| **People** | `/people` | User discovery, resonance | ❌ **MISSING** | ❌ NEEDED | `/users/discover`, `/users/{userId}` |
| **Resonance** | `/resonance` | Frequency controls, compare concepts | ❌ **MISSING** | ❌ NEEDED | `/concepts/resonance/compare`, `/user-preferences/{userId}/controls` |

### Priority 3: Creation & Developer Pages

| Page | Route | Main Features | Test File | Status | API Endpoints Used |
|------|-------|---------------|-----------|--------|-------------------|
| **Create** | `/create` | Create concepts, threads, content | ❌ **MISSING** | ❌ NEEDED | `/concepts`, `/threads/create` |
| **Portals** | `/portals` | External connections, adapters | ❌ **MISSING** | ❌ NEEDED | `/portals/list`, `/portals/{id}` |
| **Code/Dev** | `/code`, `/dev` | Code editor, file browser, hot reload | `CodeIDE.test.tsx` + 2 more | ✅ EXISTS | `/filesystem/*`, `/hotreload/*` |
| **About** | `/about` | System info, logs, health | ❌ **MISSING** | ❌ NEEDED | `/health`, `/spec/modules` |

### Lenses Test Coverage

| Lens | Main Features | Test File | Status | API Endpoints |
|------|---------------|-----------|--------|---------------|
| **StreamLens** | Concept+user feed, pagination | ❌ **MISSING** | ❌ NEEDED | `/concept/discover`, `/users/discover` |
| **GalleryLens** | Visual grid, image display | `gallery-lens-unit.test.tsx` + 7 more | ⚠️ **FAILING** | `/gallery/list`, `/concepts` |
| **ThreadsLens** | Conversation list, create dialog | `ThreadsLens.test.tsx` + 2 more | ✅ PASSING | `/threads/list`, `/threads/create` |
| **ChatsLens** | Direct messages, replies | ❌ **MISSING** | ❌ NEEDED | `/threads/list`, `/threads/{id}` |
| **NearbyLens** | Location-based discovery | ❌ **MISSING** | ❌ NEEDED | `/users/discover?location=...` |
| **SwipeLens** | Rapid concept browsing | ❌ **MISSING** | ❌ NEEDED | `/concept/discover` |
| **CirclesLens** | Community groups | ❌ **MISSING** | ❌ NEEDED | `/circles/list` |
| **LiveLens** | Real-time updates | ❌ **MISSING** | ❌ NEEDED | WebSocket/SSE endpoints |

## Implementation Strategy

### Phase 1: Fix Failing Tests (Immediate Priority)
1. **Gallery Tests** (8 files, 241 failures primarily here)
   - Issue: Components stuck in loading state
   - Fix: Ensure proper async resolution with real API or proper mocks
   - Target: All 8 gallery test files passing

2. **Profile Tests** (3 files)
   - Issue: Element not found, form control issues
   - Fix: Wait for async data loading, fix accessibility labels
   - Target: All profile tests passing

### Phase 2: Create Missing Core Page Tests
1. **/discover** - Most important (main exploration page)
2. **/people** - User discovery and connections
3. **/resonance** - Core feature (frequency controls)
4. **/create** - Content creation flow
5. **/about** - System health and info

### Phase 3: Create Missing Lens Tests
1. **StreamLens** - Default lens, high priority
2. **ChatsLens** - Communication feature
3. **NearbyLens** - Location-based discovery
4. **SwipeLens** - Rapid exploration
5. **CirclesLens** - Community feature
6. **LiveLens** - Real-time updates

### Phase 4: Real API Integration Testing
- Ensure server is running on port 5002
- Configure tests to use real backend
- Validate end-to-end flows work
- Test error handling with real failure scenarios

## Success Criteria

✅ **All 13 main pages** have at least one test file  
✅ **All 8 main lenses** have dedicated test coverage  
✅ **90%+ tests passing** with real API calls  
✅ **Core features tested** for each page:
  - Page renders without errors
  - Main feature interactions work
  - Loading states handled
  - Error states handled
  - Real API calls succeed

## Current Gaps Summary

- **Missing Tests**: 6 pages, 6 lenses (12 test files needed)
- **Failing Tests**: 241 tests (mostly Gallery/Profile)
- **Test Pass Rate**: 62% (target: 90%+)
- **With Real API**: Not validated yet

---

**Next Action**: Start with Phase 1 - Fix Gallery tests (highest failure count)

