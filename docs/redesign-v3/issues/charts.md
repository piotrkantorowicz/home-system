## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.1 last 7 days, 6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Reusable daily bars with value/date labels, target line and today highlight; replace Today weekly review.
- Tests: Known seven-day fixtures, midnight/week-start cases, empty data and accessible labels.

## Depends on
#490

## Acceptance criteria
- [ ] Reusable daily bars with value/date labels, target line and today highlight; replace Today weekly review.
- [ ] Full-day average excludes current incomplete day
- [ ] zero and missing logs distinguished
- [ ] textual over markers and accessible equivalent data. Handle no target/no data.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/components/dashboard/WeekReviewCard.tsx; chart components

## Test plan
Known seven-day fixtures, midnight/week-start cases, empty data and accessible labels.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
