## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §4.1 account.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: Backend design decision only; no endpoint implementation in this issue.
- Planning: Document whether deletion belongs in v3 and design Authentik, household ownership, shared budget history and per-module cleanup responsibilities.
- Tests: Owner review of decision record; no application code or data deletion in this ticket.

## Depends on
#478

## Acceptance criteria
- [ ] Document whether deletion belongs in v3 and design Authentik, household ownership, shared budget history and per-module cleanup responsibilities.
- [ ] No production delete-account endpoint found
- [ ] test-support purge is not account deletion. Decide retention/anonymization, last-owner transfer, reauthentication, async failures and module contracts
- [ ] split approved implementation into later PR tickets. V3 account UI uses explicit unavailable/omitted action meanwhile.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
docs/design; src/Modules/Household; src/Modules/Budget; Authentik integration

## Test plan
Owner review of decision record; no application code or data deletion in this ticket.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
