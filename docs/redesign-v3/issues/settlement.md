## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.5.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Suggested payment cards, prefilled confirmation, different amount, neutral balances and recent/history expansion.
- Tests: 2/3-person settlement, partial payments, just-recorded undo, unauthorized access and stale suggestion tests.

## Depends on
#505

## Acceptance criteria
- [ ] Suggested payment cards, prefilled confirmation, different amount, neutral balances and recent/history expansion.
- [ ] Reuse Suggestions for optional nav badge
- [ ] no new count API. Three-plus-person language reflects actual payer/payee
- [ ] Undo voids only just-recorded authorized repayment
- [ ] stale suggestions/conflicts recover. Explicit no-money-transfer note.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/pages/SettlementPage.tsx; api/queries.ts; module nav badge

## Test plan
2/3-person settlement, partial payments, just-recorded undo, unauthorized access and stale suggestion tests.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
