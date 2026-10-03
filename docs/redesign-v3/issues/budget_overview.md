## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.1.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Monthly summary, limit meters, settlement prompt, category bars and latest expenses.
- Tests: Personal/shared isolation, child view, zero/no/over limits, month navigation and settlement state.

## Depends on
#483, #480, #489

## Acceptance criteria
- [ ] Monthly summary, limit meters, settlement prompt, category bars and latest expenses.
- [ ] Map Shared/Just mine to actual API Shared/Personal values. Hide settlement for children/personal/settled state
- [ ] exact overspend (including zero limit), not helper tolerance. Reuse limits and settlement endpoints.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/pages/BudgetPage.tsx; api/queries.ts

## Test plan
Personal/shared isolation, child view, zero/no/over limits, month navigation and settlement state.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
