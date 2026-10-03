## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Required backend implementation, including additive API/schema changes and regenerated OpenAPI types.
- Delivery: Add incomplete-nutrition filtering and whitelisted stable column sorting before pagination.
- Tests: Integration tests with >1 page, null fields, ownership and inaccessible products.

## Depends on
#478

## Acceptance criteria
- [ ] Add incomplete-nutrition filtering and whitelisted stable column sorting before pagination.
- [ ] All/Mine/Incomplete combine with search
- [ ] totalCount matches filtered dataset. Null nutrition has defined sort order and completeness meaning
- [ ] visibility enforced. Regenerate schema.
- [ ] Preserve module boundaries, authenticated scope, typed endpoint results and cancellation; update API documentation and regenerate frontend schemas.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/Modules/DietPlanner/DietPlanner.Application/Queries/SearchProducts; DietPlanner.Api/ProductEndpoints.cs; generated schema

## Test plan
Integration tests with >1 page, null fields, ownership and inaccessible products.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
