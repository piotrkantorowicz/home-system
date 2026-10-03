## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §0–8.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Planning: Publish handoff, before/after assets, comparison and decision record under docs/redesign-v3.
- Tests: Review every screenshot mapping; check all source links; record unresolved choices explicitly.

## Depends on
None

## Acceptance criteria
- [ ] Publish handoff, before/after assets, comparison and decision record under docs/redesign-v3.
- [ ] Record backend/UI scope
- [ ] decide high-protein threshold, goal tolerance (helper uses 3%, budget uses exact overspend), unit preferences, author-name fallback and account-deletion disposition. Resolve Budget/Overview label contradiction. Default: preserve current API semantics
- [ ] do not invent working delete or photo-upload features.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
docs/redesign-v3; docs/rules/frontend-styling.md

## Test plan
Review every screenshot mapping; check all source links; record unresolved choices explicitly.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
