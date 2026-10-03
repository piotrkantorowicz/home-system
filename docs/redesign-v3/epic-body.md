## Goal
Deliver HomeSystem redesign v3 from the 3 October 2026 handoff across Diet Planner, Budget, Household, Notifications, Settings and Admin. Preserve existing workflows, role boundaries and real data while simplifying navigation, visual hierarchy and mobile actions.

## Design doc
Local handoff and complete comparison are prepared at docs/redesign-v3/PLAN.md and docs/redesign-v3/handoff/SPEC.md. The source archival child publishes these into the repository; paths are not yet committed. The ZIP was supplied by the owner. Mock data is illustrative; Outfit is the intended font.

## Delivery and backend scope
User requested epic/tickets and confirmed required backend work is included. Each child is one PR; dependencies are listed in each issue. Five handoff phases are split into smaller changes.

Required backend additions: expense descriptions/search, shared shopping check-offs, product completeness/sorting, recipe preparation/nutrition filters, exact filtered expense aggregates and monthly counts. Existing settlement suggestions, expense snapshots, admin retry/history and household shopping aggregation are reused.

Account deletion requires a separate design decision. Recipe photos and unresolved creator bylines are omitted rather than fabricated. Existing timezone #414, email sender #164 and notification matrix #166 remain separate dependencies/follow-ups; do not duplicate them.

## Done when
- [ ] All implementation children and final acceptance checks are complete; account deletion has an explicit disposition.
- [ ] Every screen in SPEC §§2–5 follows the supplied references at 390, 1024 and 1440 px in light/dark with real Outfit typography.
- [ ] Single navigation layer, working legacy redirects, keyboard/focus support and >=44px mobile touch targets.
- [ ] en/pl, goal directions, number/unit/date formatting and missing-data states are consistent; no raw interpolation, NaN or developer error page.
- [ ] Budget description/history/search, complete-data totals and shared shopping check-offs work with authorization and legacy-data coverage.
- [ ] Per-slice tests, path-aware verification and final real-stack E2E evidence pass; Control kit and E2E docs updated.

## Epic branch
No — children merge straight to main. Compatibility fallbacks and preserved routes keep each slice usable. No epic branch is opened by this planning task.

## Baseline and comparison
Inspected origin/main 9a27479 on 2026-10-03. Static code comparison plus handoff before/after screenshots; reported runtime bugs are not claimed reproduced. Full comparison follows.

# Redesign v3 — comparison and delivery plan

Baseline: origin/main `9a27479`, inspected 2026-10-03. Source: homesystem-redesign-handoff.zip, SPEC.md and supplied screenshots. Comparison uses current repository code plus supplied before/after screenshots; no live-app or runtime defect verification performed.

## Current versus target

| Area | Current evidence | V3 change / backend impact |
|---|---|---|
| Shell | AppShell renders 64px ModuleRail + 216px SectionPanel + Header; registry and role gates exist | Single 240px sidebar, registry switcher, footer, <=5 mobile tabs, stable PageHeader; frontend |
| Foundations | index.css has numeric size tokens from completed #279, shadows, red fat data color, .dark theme | Adapt semantic scale/radii and flat surfaces; minimum 12px; magenta fat; do not repeat token extraction as new work |
| Formatting | usePreferences stores kcal/kJ, kg/lb, ml/L/oz, week start, grouping; supplied helper only handles default units | Adapt helpers to actual preferences; don't silently relabel canonical values; exact money arithmetic |
| Robustness | router lacks errorElement; Preferences uses unqualified keys; WeightPredictionCard assumes numeric predictions | Regression tickets for handoff B1–B7; B8 calendar tint and B9 phone Save owned by screen tickets. Screenshot bug claims require reproduction |
| Diet activity | Dashboard, Calendar, NutritionSummary, Hydration and existing completion/history/goal hooks | Mostly frontend restructuring, direction-aware goals, labelled charts, phone actions |
| Shopping | GetShoppingList already aggregates household meals; rows keyed by product AND unit, no checked field | New persistence scoped by household/range with explicit mixed-unit semantics; one scrolling UI |
| Products | SearchProducts supports Search/OnlyMine/Page/PageSize; DTO nutrition per 100g | New completeness filter and server sorting. Per100ml display requires density conversion; default unit alone is insufficient |
| Recipes | SearchRecipes has search/mine/paging, prep time and computed nutrition; no photo/creator display name fields | New server filters; omit unavailable photos/byline or use permitted roster resolution; no fake names |
| Import | ValidateImport already returns create/reuse counts and warnings | File-first UI; preserve existing non-destructive import behavior |
| Settings | Separate Profile/Preferences/ChannelPreferences; existing forms/preferences | Consolidate/redirect; #414 remains timezone dependency; email stays disabled pending #164, matrix #166 remains separate |
| Household | Existing permissions include Guest, managed people and last-owner rules | Presentation only; preserve all roles despite mock omitting Guest |
| Notifications | Existing inbox, stream, unread and bulk-read hooks | Grouping/filter/actions, readable read state; no nested interactive elements |
| Budget description | ExpenseDto and snapshots lack description; list lacks text search | Add optional <=80 char field, search and backward-compatible history |
| Budget totals | GetSummary supplies exact monthly envelope/category spend, not counts; ListExpenses supplies paginated rows/count, no full-filter money/share/day aggregates | Extend aggregates/counts with shared access/filter logic; never total one loaded page |
| Budget scope | API uses Shared/Personal; mock says shared/mine | Map UI labels to actual contract |
| Budget splits | EqualSplit sorts ascending PersonId before distributing remainder cents | Preview must match server order, not visible card order |
| Settlement | SettlementDto already has Suggestions, Balances and IsSettled; repayment/void exists | Reuse API for screen and optional badge; no new count endpoint |
| Expense detail | Immutable snapshots already returned | Client-side changed-field diffs, local timestamps and danger row |
| Admin | Retry all, payload and history shipped (#432–#435) | Frontend only; retain authorization |
| Delete account | Preferences has copy; no production endpoint found, only test-support purge | Separate needs-decision design ticket; no active destructive button until lifecycle approved |

## Delivery

Each child is one independently mergeable PR; default target main. Five source phases are milestones, not five oversized PRs. Order dependencies below. UI can use documented local/category fallbacks, but backend-enabled completion must be demonstrated before epic closes.

Required backend implementation tickets: products_api, recipes_api, description, shopping_api, totals_api.
Decision-only backend scope: account_decision. Reuse existing #414, #164 and #166 instead of duplicating/reparenting.

## Decisions to settle before dependent implementation

- High protein: choose and document threshold and null behavior before recipe API work.
- goalStatus helper uses 3% tolerance; monetary overspend remains exact. Confirm nutrition tolerance in source decision record.
- Unit preferences must work truthfully; canonical backend units unchanged.
- Shopping checks: product+unit rows need deliberate identity; choose per-row checks or product-wide behavior.
- Recipe photos/author byline lack direct fields: omit unsupported photo space; author only when permitted identity resolution exists.
- Account deletion: separate decision ticket, not presumed functional in v3.
- Budget nav/title mismatch in spec: choose consistent Budget/Overview wording in source record.

## Acceptance baseline

Per PR: targeted tests, relevant verify gates, en/pl, keyboard/focus, loading/empty/error states, role permissions and 390/1024/1440 light/dark checks. Regenerate OpenAPI types for changed contracts. Update Control kit with actual reused/new primitives. Visual references use fallback font; verify real Outfit layout, not pixel-perfect text widths.

