## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §1.4, 0.5.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Adapt supplied pure formatting helpers into shared/lib and existing preferences; expose goal status and numeric text primitives.
- Tests: Unit cases for en/pl, 4-digit grouping, zero/missing targets, 3% boundary decision, date boundaries and decimal money.

## Depends on
#478

## Acceptance criteria
- [ ] Adapt supplied pure formatting helpers into shared/lib and existing preferences; expose goal status and numeric text primitives.
- [ ] Handle missing/nonfinite data, decimal API strings, date-only versus instant values, en/pl plurals and fractional pieces without quantity loss. Preserve stored unit choices or document approved changes. Budget uses exact minor-unit overspend
- [ ] money arithmetic never uses floats. Adopt in Control kit
- [ ] screen tickets migrate consumers.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/shared/lib/utils.ts; src/ui/src/shared/hooks/usePreferences.ts; handoff/src/lib/format.ts

## Test plan
Unit cases for en/pl, 4-digit grouping, zero/missing targets, 3% boundary decision, date boundaries and decimal money.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
