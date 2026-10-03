## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §4.1 diet.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: One scrolling Settings route with Profile/Goals/Meal times/Water/Reminders anchors and existing forms.
- Tests: Each settings save flow, anchors/deep-link redirects, incomplete profile and reminder defaults.

## Depends on
#483, #482

## Acceptance criteria
- [ ] One scrolling Settings route with Profile/Goals/Meal times/Water/Reminders anchors and existing forms.
- [ ] Preserve weight history/logging and energy model
- [ ] add direction labels. Redirect profile including ?section= to matching anchor. Per-user timezone remains #414, no duplicate backend task.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Profile.tsx; components/settings; module routes

## Test plan
Each settings save flow, anchors/deep-link redirects, incomplete profile and reminder defaults.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
