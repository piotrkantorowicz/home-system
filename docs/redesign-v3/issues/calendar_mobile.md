## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.2 phone.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Day meal rows with one 44px completion control and overflow menu for swap/reset/edit/delete.
- Tests: 390px meal completion, edit/swap/reset/delete E2E; keyboard menu and focus.

## Depends on
#492, #484

## Acceptance criteria
- [ ] Day meal rows with one 44px completion control and overflow menu for swap/reset/edit/delete.
- [ ] Actions remain available without hover
- [ ] mobile day/week navigation and selected person preserved
- [ ] dialogs restore focus.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/components/calendar-day; components/diet-plans

## Test plan
390px meal completion, edit/swap/reset/delete E2E; keyboard menu and focus.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
