## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Neutral sortable sticky-header table with All/Mine/Incomplete chips, unit labels and permission-aware actions.
- Tests: Multi-page sort/filter tests, null values, per100g/ml conversion decisions, visibility/edit permissions.

## Depends on
#483, #480, #485

## Acceptance criteria
- [ ] Neutral sortable sticky-header table with All/Mine/Incomplete chips, unit labels and permission-aware actions.
- [ ] Wire server sorting/filtering with pagination reset. Nutrition is currently per 100g: do not relabel as per 100ml without density conversion
- [ ] missing density stays explicit. Preserve Public visibility and product CRUD.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/products; api/hooks/useProducts.ts

## Test plan
Multi-page sort/filter tests, null values, per100g/ml conversion decisions, visibility/edit permissions.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
