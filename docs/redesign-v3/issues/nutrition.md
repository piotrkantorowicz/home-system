## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.4.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: 7/30/90-day selector, neutral summaries, calories chart, kcal-based macro comparison and daily totals.
- Tests: Known meal totals, 7/30/90 ranges, goal limits/minima, macro kcal percentages and en/pl.

## Depends on
#491, #480

## Acceptance criteria
- [ ] 7/30/90-day selector, neutral summaries, calories chart, kcal-based macro comparison and daily totals.
- [ ] Use actual logged totals
- [ ] no misleading red data fills. <=31 rows show without pagination
- [ ] 90 days remain accessible. Goal statuses and missing-data denominators explicit.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/NutritionSummary.tsx; components/MacroDistributionCard.tsx

## Test plan
Known meal totals, 7/30/90 ranges, goal limits/minima, macro kcal percentages and en/pl.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
