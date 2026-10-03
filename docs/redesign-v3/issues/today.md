## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.1.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Unified calorie/macros/water surface, all today slots, next meal emphasis and check toggles; planned footer.
- Tests: Completion/undo and water tests; desktop/phone E2E and goal direction cases.

## Depends on
#483, #484, #480

## Acceptance criteria
- [ ] Unified calorie/macros/water surface, all today slots, next meal emphasis and check toggles; planned footer.
- [ ] Use real goals/completion/water data
- [ ] no meal strikethrough. Show missing-goal and empty-day states
- [ ] mutations refresh existing queries. Match 10/11 screenshots.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Dashboard.tsx; components/dashboard

## Test plan
Completion/undo and water tests; desktop/phone E2E and goal direction cases.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
