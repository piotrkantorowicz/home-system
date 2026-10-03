## Context
Part of #477. Redesign v3. Design: docs/redesign-v3/handoff/SPEC.md §4.1 app/account.
The source archival issue publishes the supplied ZIP and comparison (docs/redesign-v3/PLAN.md); these paths were prepared locally during planning and are not committed yet.
Baseline: origin/main 9a27479, compared on 2026-10-03.

## Scope
- Backend: No new backend contract in this slice; consume existing APIs or the dependency named below.
- Delivery: App/account route combines channel preferences, theme previews, language/density/unit preferences and sign out.
- Tests: Route redirects, preferences persistence, en/pl, light/dark/system, channel mutation and logout.

## Depends on
#501, #480

## Acceptance criteria
- [ ] App/account route combines channel preferences, theme previews, language/density/unit preferences and sign out.
- [ ] Redirect both old preferences routes with correct destination. Email disabled with Coming soon (#164). No functional account deletion without separate approved lifecycle design
- [ ] show honest unavailable state/omit action per decision ticket. Units and number examples reflect actual behavior.
- [ ] Verify en/pl, loading/empty/error states, keyboard/focus, authorized roles, and 390/1024/1440 light/dark layouts. Update Control kit for components introduced or changed.

## Out of scope
Other redesign screens belong to sibling sub-issues of #477. No unrelated architecture migration or new dependency. Account deletion implementation, email delivery (#164), notification matrix (#166), recipe photo upload and per-user timezone work (#414) are excluded unless explicitly named here. Source handoff mock values must never become production data.

## Likely files / layers
src/ui/src/modules/diet-planner/pages/Preferences.tsx; src/ui/src/modules/notifications/pages/ChannelPreferences.tsx; app/router.tsx

## Test plan
Route redirects, preferences persistence, en/pl, light/dark/system, channel mutation and logout.
Run the applicable path-aware verification when implementing; planning itself does not execute application tests.
