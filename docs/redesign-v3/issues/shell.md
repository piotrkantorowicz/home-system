## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §2 desktop.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: One 240px sidebar, registry-driven module switcher, search and footer destinations; shared PageHeader and stable content container.
- Tests: Registry/nav tests across Owner/Adult/Child/Guest/admin; keyboard switcher; desktop/tablet layout.

## Depends on
#479

## Acceptance criteria
- [ ] One 240px sidebar, registry-driven module switcher, search and footer destinations; shared PageHeader and stable content container.
- [ ] Remove rail/duplicate brand
- [ ] retain role gates, remembered module, command palette and unread/admin badges. Use module-provided components/public APIs
- [ ] shared code never imports module internals. Keep legacy settings links until replacement exists.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/shared/components/layout; src/ui/src/shared/lib/module-registry.ts; src/ui/src/modules/*/index.ts

## Test plan
Registry/nav tests across Owner/Adult/Child/Guest/admin; keyboard switcher; desktop/tablet layout.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
