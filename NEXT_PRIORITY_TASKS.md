# Next High-Priority Tasks - Living Codex UI

**Generated:** October 1, 2025, 3:15 AM  
**Goal:** Achieve Fully Functional UI (95%+ Production Ready)  
**Current:** 75% Complete

---

## 🔴 TOP 3 CRITICAL TASKS (Must Do First)

### 1. Fix Gallery Test Async Issues ⏱️ 2 hours
**Priority:** CRITICAL  
**Impact:** Very High (blocks 8 test suites)  
**Current:** 12/14 passing in gallery-lens-unit

**Problem:**
- Tests expecting data but component stuck in loading state
- `waitFor` timeouts due to async race conditions
- Mock fetch not properly resolving

**Solution:**
- Add proper async/await handling in mocks
- Increase timeout for slow renders
- Ensure all promises resolve before assertions
- Fix remaining 2/14 failures in gallery-lens-unit
- Apply fixes to all 8 gallery test files

**Files:**
```
src/__tests__/gallery-lens-unit.test.tsx (12/14 ✅)
src/__tests__/gallery-edge-cases.test.tsx
src/__tests__/gallery-image-display.test.tsx
src/__tests__/gallery-item-view.test.tsx
src/__tests__/gallery-image-validation.test.tsx
src/__tests__/gallery-image-simple.test.tsx
src/__tests__/gallery-discover-integration.test.tsx
src/__tests__/gallery-lens-real-api.test.tsx
```

---

### 2. Fix Profile Edit Functionality ⏱️ 2 hours
**Priority:** CRITICAL  
**Impact:** High (core user feature)

**Problem:**
- Edit form renders but doesn't submit
- PUT `/auth/profile/{userId}` not being called
- No validation or error feedback

**Solution:**
- Wire up form submission to `endpoints.updateUserProfile()`
- Add form validation (displayName, email)
- Show success/error messages
- Update local state after successful edit
- Fix 3 failing profile test files

**Files:**
```
src/app/profile/page.tsx
src/__tests__/profile-page.test.tsx
src/__tests__/profile-page-real-api.test.tsx
src/__tests__/profile-integration.test.tsx
```

---

### 3. Implement Create Concept Flow ⏱️ 3 hours
**Priority:** CRITICAL  
**Impact:** Very High (content creation)

**Problem:**
- Create page exists but form doesn't work
- No POST to `/concepts` endpoint
- No success navigation or feedback

**Solution:**
- Connect form to `endpoints.createConcept()`
- Add field validation (title, description, axes)
- Navigate to created concept on success
- Show error feedback on failure
- Add integration tests

**Files:**
```
src/app/create/page.tsx
src/__tests__/create-page.test.tsx
living-codex-ui/src/lib/api.ts
```

---

## 🟡 SECONDARY PRIORITIES (Do Next)

### 4. Add React Error Boundaries ⏱️ 1 hour
**Priority:** HIGH  
**Impact:** Medium (prevents crashes)

**What:**
- Create `<ErrorBoundary>` component
- Wrap all pages in layout
- Show user-friendly error UI
- Log errors to backend

**Files:**
```
src/components/ErrorBoundary.tsx (NEW)
src/app/layout.tsx
```

---

### 5. Fix Discover Page Lens Switching ⏱️ 1 hour
**Priority:** MEDIUM  
**Impact:** Medium (UX improvement)

**Problem:**
- Lens selection doesn't persist
- URL params not synced
- Tests failing

**Solution:**
- Use URL search params for lens state
- Persist selection in localStorage as backup
- Fix discover-page.test.tsx

---

### 6. Mobile Responsive Validation ⏱️ 2 hours
**Priority:** MEDIUM  
**Impact:** High (mobile users)

**What:**
- Test all pages on mobile viewports
- Fix broken layouts
- Add touch interactions
- Update navigation for mobile

---

## 🟢 NICE-TO-HAVE (Later)

### 7. SwipeLens Persistence ⏱️ 30 min
- Integrate backend persistence for swipe actions
- Low effort, low priority

### 8. Concept Taxonomy Tests ⏱️ 2 hours
- Test Wikipedia enrichment
- Test hierarchy validation
- Test deduplication

### 9. Analytics Dashboard ⏱️ 4-6 hours
- User interaction analytics
- Concept popularity tracking
- System health dashboard

---

## 📋 Detailed Implementation Plan

### Task 1: Fix Gallery Tests (START HERE)

**Step 1:** Fix remaining 2 failures in gallery-lens-unit.test.tsx
```bash
cd living-codex-ui
npm test -- --testPathPattern="gallery-lens-unit"
# Identify exact failures
# Fix async timing
# Verify 14/14 passing
```

**Step 2:** Apply fixes to other 7 gallery test files
- Same pattern: ensure fetch resolves before assertions
- Add proper timeouts
- Fix mock data structure

**Step 3:** Validate with real API
```bash
# Ensure server running on 5002
npm test -- --testPathPattern="gallery" --no-coverage
# Target: 90%+ passing
```

**Acceptance Criteria:**
- ✅ 14/14 tests in gallery-lens-unit.test.tsx
- ✅ 90%+ of all gallery tests passing
- ✅ No timeout errors
- ✅ Proper async/await handling

---

### Task 2: Fix Profile Edit

**Step 1:** Read current implementation
```typescript
// src/app/profile/page.tsx
const handleSaveProfile = async () => {
  // TODO: Currently missing - need to implement
}
```

**Step 2:** Implement
```typescript
const handleSaveProfile = async () => {
  try {
    const response = await endpoints.updateUserProfile(userId, {
      displayName: editedDisplayName,
      email: editedEmail
    });
    
    if (response.success) {
      setUser({...user, displayName: editedDisplayName, email: editedEmail});
      setIsEditing(false);
      setSuccessMessage('Profile updated successfully');
    }
  } catch (error) {
    setErrorMessage('Failed to update profile');
  }
}
```

**Step 3:** Add validation
- Email format check
- DisplayName min/max length
- Show inline errors

**Step 4:** Update tests
- Test successful save
- Test validation errors
- Test API error handling

**Acceptance Criteria:**
- ✅ Edit form submits to backend
- ✅ Success message shows
- ✅ Profile updates in UI
- ✅ All profile tests passing

---

### Task 3: Implement Create Concept

**Step 1:** Wire up form submission
```typescript
const handleCreateConcept = async (formData) => {
  const response = await endpoints.createConcept({
    title: formData.title,
    description: formData.description,
    axes: formData.selectedAxes,
    tags: formData.tags
  });
  
  if (response.success && response.data) {
    router.push(`/node/${response.data.id}`);
  }
}
```

**Step 2:** Add validation
- Title required, max 100 chars
- Description optional, max 500 chars
- At least one axis selected

**Step 3:** Show feedback
- Loading spinner during creation
- Success message + navigate
- Error message on failure

**Acceptance Criteria:**
- ✅ Form POSTs to `/concepts`
- ✅ Validates all fields
- ✅ Navigates to created concept
- ✅ Tests pass

---

## 🎯 Success Metrics

| Metric | Current | Target | Gap |
|--------|---------|--------|-----|
| Test Pass Rate | 54% | 90%+ | -36% 🔴 |
| Functional Pages | 9/13 | 13/13 | 4 pages 🔴 |
| Core Flows | 70% | 100% | -30% 🔴 |
| Build Errors | 0 | 0 | ✅ |
| Performance | ✅ | ✅ | ✅ |

---

## ⏱️ Time Estimate to Production

```
Task 1: Gallery Tests → 2 hours
Task 2: Profile Edit  → 2 hours  
Task 3: Create Flow   → 3 hours
Task 4: Error Bounds  → 1 hour
Task 5: Fix Tests     → 3 hours
────────────────────────────────
TOTAL: 11 hours → 95% Production Ready
```

---

## 🚀 Recommended Execution Order

1. ✅ **Update tracking sheets** (DONE)
2. 🔴 **Fix Gallery tests** (CURRENT - IN PROGRESS: 12/14 passing)
3. 🔴 **Fix Profile edit**
4. 🔴 **Implement Create flow**
5. 🟡 **Add Error Boundaries**
6. 🟡 **Fix remaining tests**
7. 🟢 **Mobile validation**

---

## 📈 Expected Outcome

After completing Top 3 tasks:
- Test pass rate: 54% → 85%+
- Functional pages: 9/13 → 13/13
- Core flows: 70% → 95%+
- **Production confidence: HIGH** ✅

**START WITH:** Fix remaining 2 Gallery test failures, then proceed to Profile edit.

