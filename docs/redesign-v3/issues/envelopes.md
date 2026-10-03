## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.6.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Shared/personal rows with monthly counts/limits, menus and collapsed archived section.
- Tests: Archive/restore, rename, counts/month boundary, zero limit and personal visibility E2E.

## Depends on
#505

## Acceptance criteria
- [ ] Shared/personal rows with monthly counts/limits, menus and collapsed archived section.
- [ ] Preserve create/rename/set-clear-limit/archive/restore and privacy
- [ ] counts from server month summary
- [ ] zero limit differs from no limit
- [ ] archived rows remain reachable.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/pages/EnvelopesPage.tsx; envelope components

## Test plan
Archive/restore, rename, counts/month boundary, zero limit and personal visibility E2E.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
