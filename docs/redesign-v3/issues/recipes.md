## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.7.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Compact cards without placeholder photo space, kcal-based macro bar, plural servings, chip filters and menu actions.
- Tests: Multi-page filters, 1/2/5 servings in en/pl, zero kcal and permission cases.

## Depends on
#483, #480, #486

## Acceptance criteria
- [ ] Compact cards without placeholder photo space, kcal-based macro bar, plural servings, chip filters and menu actions.
- [ ] No photo API exists: omit images rather than invent uploads. Filters cover server dataset
- [ ] zero nutrition and missing prep time handled
- [ ] preserve Public badges and CRUD.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/recipes/RecipeList.tsx; components/recipes/RecipeCard.tsx

## Test plan
Multi-page filters, 1/2/5 servings in en/pl, zero kcal and permission cases.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
