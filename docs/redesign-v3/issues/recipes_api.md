## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.7.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Required backend implementation, including additive API/schema changes and regenerated OpenAPI types.
- Delivery: Add high-protein and under-15-minute filters before pagination using approved high-protein definition.
- Tests: Integration tests at threshold, 14/15 minutes, missing nutrition, page boundaries and visibility.

## Depends on
#478

## Acceptance criteria
- [ ] Add high-protein and under-15-minute filters before pagination using approved high-protein definition.
- [ ] Unknown preparation times do not match under-15
- [ ] nutrition filtering uses per-serving computed nutrition and documented threshold. Counts and visibility correct across pages
- [ ] regenerate schema.
- [ ] Preserve module boundaries, authenticated scope, typed endpoint results and cancellation; update API documentation and regenerate frontend schemas.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/Modules/DietPlanner/DietPlanner.Application/Queries/SearchRecipes; DietPlanner.Api/RecipeEndpoints.cs

## Test plan
Integration tests at threshold, 14/15 minutes, missing nutrition, page boundaries and visibility.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
