## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §1.6 B1–B5, B7.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Fix namespace collision in preferences, remove development copy, guard missing prediction fields, default reminder hints, recipe plurals and section deep links.
- Tests: Regression cases for empty/partial API payloads, en/pl counts 1/2/5 and deep links; reproduce handoff reports before fixing.

## Depends on
#480

## Acceptance criteria
- [ ] Fix namespace collision in preferences, remove development copy, guard missing prediction fields, default reminder hints, recipe plurals and section deep links.
- [ ] Missing birth date/sex or null predictions show Add details state
- [ ] no NaN, undefined or raw interpolation. Reminder hints disappear when off. Existing ?section= selects correct section pending redirects.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Preferences.tsx; pages/Profile.tsx; components/WeightPredictionCard.tsx; components/settings; locales

## Test plan
Regression cases for empty/partial API payloads, en/pl counts 1/2/5 and deep links; reproduce handoff reports before fixing.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
