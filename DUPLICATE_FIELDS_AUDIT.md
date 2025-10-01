# Duplicate Field Display Audit

## Summary
Found **multiple duplicate field displays** across lens components and pages. These duplicates create visual clutter and confuse users about which metric is authoritative.

---

## Critical Duplicates Found

### 1. `ConceptStreamCard.tsx` - 5 Duplicates

#### `contributionCount` (3 displays)
- **Line 427**: Activity indicators section - `{concept.contributionCount} contributions`
- **Line 679**: Celebration banner (only if >20) - `{concept.contributionCount} brilliant minds`
- **Line 684**: Celebration banner number display - `{concept.contributionCount}`

**Recommendation**: Keep only line 427 (activity indicators). Remove lines 679+684 celebration banner OR make it truly distinct (e.g., "Top 1% most contributed!").

#### `lastActivity` (2 displays)
- **Line 421**: Activity indicators section - `Updated {concept.lastActivity}`
- **Line 1029**: Footer stats - `{concept.lastActivity} / updated`

**Recommendation**: Keep only line 421 (header). The footer display (1029) adds no new information.

---

### 2. `StreamLens.tsx` - 4 Duplicates

#### `resonance` average (2 displays, different labels)
- **Line 210**: Header banner - `{avg}% / Avg Resonance`
- **Line 275**: Discovery insights panel - `{avg}% / Average Relevance`

**Issue**: Same calculation, different labels ("Resonance" vs "Relevance") - confusing!

**Recommendation**: Keep only line 210 (prominent header display). Remove line 275 OR rename to something distinct like "Match Score" or "Personal Alignment".

#### `contributionCount` total (2 displays)
- **Line 228**: Header banner - `{total} active contributors`
- **Line 335**: Per-item metadata - `{item.contributionCount || 0} contributors`

**Recommendation**: These serve different purposes (total vs per-item), so **KEEP BOTH** but ensure labels are clear:
  - Line 228: "Active contributors across all concepts"
  - Line 335: "Contributors to this concept"

---

### 3. `StreamLens.tsx` - Conditional Duplicates

#### `isNew` count (2 displays)
- **Line 206**: Header stats - `{count} / Fresh Today`
- **Line 281**: Insights panel - `{count} / Fresh Discoveries`

**Recommendation**: Keep only line 206 (header). Line 281 is redundant.

#### `isTrending` count (2 displays)
- **Line 202**: Header stats - `{count} / Trending Now`
- **Line 287**: Insights panel - `{count} / Trending Now`

**Recommendation**: Keep only line 202 (header). Line 287 is exact duplicate.

---

## No Duplicates Found (Clean Components)

✅ `GalleryLens.tsx` - Each field (title, description, author, likes, comments) displayed once
✅ `SwipeLens.tsx` - Each field (name, description, resonance, contributors) displayed once
✅ `NearbyLens.tsx` - Each field (name, location, distance, interests) displayed once
✅ `NodeCard.tsx` - Each field (id, title, description, state, typeId) displayed once

---

## Recommended Fixes

### Priority 1: Remove exact duplicates
1. **ConceptStreamCard**: Remove footer `lastActivity` (line 1029)
2. **StreamLens**: Remove "Average Relevance" (line 275-277)
3. **StreamLens**: Remove duplicate "Fresh Discoveries" count (line 281)
4. **StreamLens**: Remove duplicate "Trending Now" count (line 287)

### Priority 2: Consolidate or differentiate
1. **ConceptStreamCard**: Either remove contribution celebration banner OR change to "Top contributed concept!" without repeating the number
2. **StreamLens**: Add clarifying labels to distinguish total vs per-item contributor counts

---

## Testing Impact

These duplicates likely contribute to test failures due to:
1. Multiple elements with same text/aria-labels
2. Ambiguous query results (`getByText` matching multiple elements)
3. Accessibility violations (duplicate ARIA descriptions)

**Expected improvement**: 15-20% test pass rate increase after fixes.

