## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §7.1.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Required backend implementation, including additive API/schema changes and regenerated OpenAPI types.
- Delivery: Optional description up to 80 characters across create/correct/list/detail/search and immutable revision snapshots.
- Tests: Unit/integration tests for 0/80/81 chars, description correction/history, search privacy and legacy snapshots.

## Depends on
None

## Acceptance criteria
- [ ] Optional description up to 80 characters across create/correct/list/detail/search and immutable revision snapshots.
- [ ] Old expenses/history remain readable with null description
- [ ] whitespace normalization and length validated
- [ ] search is server-side before paging and authorization. Correction concurrency and duplicate detection preserved
- [ ] additive migration and regenerated schema.
- [ ] Preserve module boundaries, authenticated scope, typed endpoint results and cancellation; update API documentation and regenerate frontend schemas.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/Modules/Budget/Budget.Domain; Budget.Application/Commands; Queries/GetExpense; Queries/ListExpenses; Budget.Infrastructure; Budget.Api

## Test plan
Unit/integration tests for 0/80/81 chars, description correction/history, search privacy and legacy snapshots.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
