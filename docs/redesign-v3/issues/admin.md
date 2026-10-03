## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.7.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Neutral summaries, source display names, local times, one-line error previews, payload/history panel and retry actions.
- Tests: Admin gate, per-item/bulk retry success/failure, payload/history and long errors.

## Depends on
#483, #480

## Acceptance criteria
- [ ] Neutral summaries, source display names, local times, one-line error previews, payload/history panel and retry actions.
- [ ] Reuse delivery/outbox retry-all/history endpoints completed in #432–#435. Admin-only access preserved, source counts accurate, full errors accessible, empty sections readable.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/admin/pages/DeadLetters.tsx; api/hooks

## Test plan
Admin gate, per-item/bulk retry success/failure, payload/history and long errors.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
