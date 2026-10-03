## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.5.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Today progress, 250/330/500/custom actions, newest-first entries, 7-day goal chart and setup summary.
- Tests: Quick/custom add and delete E2E; history denominators, local timestamps and empty data.

## Depends on
#491, #480

## Acceptance criteria
- [ ] Today progress, 250/330/500/custom actions, newest-first entries, 7-day goal chart and setup summary.
- [ ] Keep existing daily intake queries (seven bounded days suffice
- [ ] no backend endpoint required). Delete errors recover
- [ ] disabled tracking/no goal represented
- [ ] settings deep link remains valid.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Hydration.tsx; api/hooks/useHydration.ts

## Test plan
Quick/custom add and delete E2E; history denominators, local timestamps and empty data.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
