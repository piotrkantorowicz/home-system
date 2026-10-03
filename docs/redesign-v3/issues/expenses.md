## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.2.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Month/search/envelope/category filters, day groups, server totals and description-first rows.
- Tests: Search/filter/paging totals, same day across pages, voided state and unauthorized rows.

## Depends on
#505, #487

## Acceptance criteria
- [ ] Month/search/envelope/category filters, day groups, server totals and description-first rows.
- [ ] Use full-filter aggregates and daily totals
- [ ] paging never misrepresents total spend/shares. Category fallback for null description, Show voided, payer/share metadata and detail links.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/pages/ExpensesPage.tsx; budget row components; types.ts

## Test plan
Search/filter/paging totals, same day across pages, voided state and unauthorized rows.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
