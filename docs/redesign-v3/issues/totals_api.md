## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.1, 5.2, 5.6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Required backend implementation, including additive API/schema changes and regenerated OpenAPI types.
- Delivery: Expose exact full-filter expense totals, caller split-share totals and daily totals; add monthly expense counts to summary/envelopes.
- Tests: Integration >100 expenses, multi-day pagination, personal/shared/child access, search and void filters; exact cents.

## Depends on
#487

## Acceptance criteria
- [ ] Expose exact full-filter expense totals, caller split-share totals and daily totals; add monthly expense counts to summary/envelopes.
- [ ] Aggregates share list authorization/search/category/envelope/date/void filters and cover all pages. Monthly active counts exclude voids
- [ ] personal spending never leaks. Define voided aggregate treatment
- [ ] do not total first 20/100 loaded rows. Regenerate schema.
- [ ] Preserve module boundaries, authenticated scope, typed endpoint results and cancellation; update API documentation and regenerate frontend schemas.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/Modules/Budget/Budget.Application/Queries/ListExpenses; Queries/GetSummary; Budget.Api/BudgetEndpoints.cs

## Test plan
Integration >100 expenses, multi-day pagination, personal/shared/child access, search and void filters; exact cents.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
