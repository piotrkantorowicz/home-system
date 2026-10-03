## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.2 desktop.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Week grid, person/day-week controls, Import/Add actions, eaten/planned/missed/next states and daily/weekly meters.
- Tests: Existing household calendar and completion scenarios; past/future/today state fixtures, weekly totals and no goals.

## Depends on
#483, #480

## Acceptance criteria
- [ ] Week grid, person/day-week controls, Import/Add actions, eaten/planned/missed/next states and daily/weekly meters.
- [ ] Remove duplicate CalendarTabBar and macro-led tint B8. Preserve household assignment, swap/reset/edit/delete and overrides. Protein/fiber minima differ from calorie/fat/carb limits.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Calendar.tsx; components/calendar; components/CalendarTabBar.tsx

## Test plan
Existing household calendar and completion scenarios; past/future/today state fixtures, weekly totals and no goals.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
