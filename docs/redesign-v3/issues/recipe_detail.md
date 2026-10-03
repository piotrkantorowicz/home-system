## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §3.8.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Scaled ingredient list with product links, method, per-serving nutrition, Add to plan, danger action and this-week meal context.
- Tests: Serving scaling, fractional pieces, no goal/no meals, authorized product links, add-to-plan and deletion.

## Depends on
#498

## Acceptance criteria
- [ ] Scaled ingredient list with product links, method, per-serving nutrition, Add to plan, danger action and this-week meal context.
- [ ] Fetch complete bounded week meal data and filter recipeId. No creator display name/photo in RecipeDto: show permitted roster name only when resolvable, otherwise omit byline
- [ ] never expose auth subject as name. Preserve decimal ingredient quantities.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/recipes/RecipeDetail.tsx; api/hooks/useMeals.ts

## Test plan
Serving scaling, fractional pieces, no goal/no meals, authorized product links, add-to-plan and deletion.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
