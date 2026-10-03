## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §7.2.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Required backend implementation, including additive API/schema changes and regenerated OpenAPI types.
- Delivery: Add authorized idempotent check/uncheck persistence and expose bought state in household shopping query.
- Tests: Two-member shared reads, isolation, repeated writes, same product in two units, range changes, membership removal and concurrency.

## Depends on
#478

## Acceptance criteria
- [ ] Add authorized idempotent check/uncheck persistence and expose bought state in household shopping query.
- [ ] Scope derives from authenticated household and normalized date range. Resolve existing product+unit rows: use product+unit key or explicitly check all units together. Cross-household reads/writes denied
- [ ] range isolation and uncheck-all defined
- [ ] additive migration and schema.
- [ ] Preserve module boundaries, authenticated scope, typed endpoint results and cancellation; update API documentation and regenerate frontend schemas.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/Modules/DietPlanner/DietPlanner.Application/Queries/GetShoppingList; DietPlanner.Domain; DietPlanner.Infrastructure; DietPlanner.Api/MealEndpoints.cs

## Test plan
Two-member shared reads, isolation, repeated writes, same product in two units, range changes, membership removal and concurrency.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
