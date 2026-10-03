## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.4.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Description/amount header, split summary, details and client-side changed-field revision timeline; separate Void row.
- Tests: History diff unit cases (amount, payer, shares, description, void), legacy snapshots and correction/void E2E.

## Depends on
#506, #507

## Acceptance criteria
- [ ] Description/amount header, split summary, details and client-side changed-field revision timeline; separate Void row.
- [ ] Sort history newest first but compare consecutive snapshots chronologically
- [ ] handle old missing descriptions and zero changes. Local times, exact cents, permission-aware Correct/Void.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/pages/ExpenseDetailPage.tsx

## Test plan
History diff unit cases (amount, payer, shares, description, void), legacy snapshots and correction/void E2E.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
