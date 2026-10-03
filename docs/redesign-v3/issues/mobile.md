## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §2 mobile.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: At most five tabs per module; More sheet exposes remaining routes and module switcher; Budget add action opens existing expense form.
- Tests: 390px E2E for Diet/Budget navigation, More and add action; keyboard/focus checks.

## Depends on
#483

## Acceptance criteria
- [ ] At most five tabs per module; More sheet exposes remaining routes and module switcher; Budget add action opens existing expense form.
- [ ] All existing destinations remain reachable
- [ ] active states and back navigation work. Safe-area padding and 44px targets
- [ ] sheet traps focus and closes on Escape.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/shared/components/layout/BottomTabBar.tsx; module navigation definitions

## Test plan
390px E2E for Diet/Budget navigation, More and add action; keyboard/focus checks.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
