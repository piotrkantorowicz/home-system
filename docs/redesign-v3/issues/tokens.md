## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §1.1–1.3, 1.5, 6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Adapt existing theme tokens, semantic type scale, three radii and flat surfaces; update Card/Button/Field variants and Control kit.
- Tests: Primitive tests; manual light/dark contrast and 390/1024/1440 checks.

## Depends on
#478

## Acceptance criteria
- [ ] Adapt existing theme tokens, semantic type scale, three radii and flat surfaces; update Card/Button/Field variants and Control kit.
- [ ] Keep Outfit and current .dark ThemeContext integration
- [ ] minimum 12px text, 44px mobile targets, readable focus/contrast. Reuse token work completed in #279. Migrate shared primitives first
- [ ] screen tickets remove remaining legacy sizes.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/index.css; src/ui/src/shared/components/ui; src/ui/src/modules/diet-planner/pages/ControlKit.tsx

## Test plan
Primitive tests; manual light/dark contrast and 390/1024/1440 checks.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
