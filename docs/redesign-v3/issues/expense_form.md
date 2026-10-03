## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §5.3, B9.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: Amount-first create/correct form, category chips, payer and split cards, description, duplicate panel and phone sheet.
- Tests: 100/3 and reordered participant parity, household-funded path, duplicate bypass, correction concurrency, phone keyboard/save E2E.

## Depends on
#483, #480, #487

## Acceptance criteria
- [ ] Amount-first create/correct form, category chips, payer and split cards, description, duplicate panel and phone sheet.
- [ ] 390x844 Save stays reachable with keyboard and scroll
- [ ] comma/dot input
- [ ] required correction reason
- [ ] exact integer-cent preview matches backend ascending PersonId order, not visual order. Preserve revisions/conflicts and duplicate choices.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/budget/components; expense mutations

## Test plan
100/3 and reordered participant parity, household-funded path, duplicate bypass, correction concurrency, phone keyboard/save E2E.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
