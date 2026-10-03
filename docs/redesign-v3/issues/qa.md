## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §8.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Final cross-screen acceptance sweep and stable regression fixtures after per-slice tests.
- Tests: Run path-aware verify plus real-stack E2E; visual/manual screenshot comparison with Outfit loaded; record evidence, avoid pixel equality to fallback-font mockups.

## Depends on
#493, #494, #495, #496, #497, #499, #500, #502, #503, #504, #508, #509, #510, #511

## Acceptance criteria
- [ ] Final cross-screen acceptance sweep and stable regression fixtures after per-slice tests.
- [ ] Cover all supplied targets at 1440/1024/390 in light/dark and en/pl
- [ ] all legacy routes resolve
- [ ] no tiny text/legacy radius/format remnants, raw keys or NaN. Control kit covers reused/new primitives
- [ ] update E2E docs.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
e2e; docs/e2e; src/ui/src/modules/diet-planner/pages/ControlKit.tsx

## Test plan
Run path-aware verify plus real-stack E2E; visual/manual screenshot comparison with Outfit loaded; record evidence, avoid pixel equality to fallback-font mockups.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
