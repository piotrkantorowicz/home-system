## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.3.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: One scrolling list, bought progress/section, date range, Copy and export menu; integrate shared check-offs.
- Tests: Two-member check/uncheck E2E, range switching, mixed units, optimistic rollback and export tests.

## Depends on
#483, #480, #488

## Acceptance criteria
- [ ] One scrolling list, bought progress/section, date range, Copy and export menu; integrate shared check-offs.
- [ ] Preserve CSV/JSON/copy correctness and quantities. Fallback checks isolated by household/range and visibly local when shared API unavailable
- [ ] API errors never silently appear saved. No pagination
- [ ] uncheck-all works.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/ShoppingList.tsx; api/hooks/useShoppingList.ts

## Test plan
Two-member check/uncheck E2E, range switching, mixed units, optimistic rollback and export tests.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
