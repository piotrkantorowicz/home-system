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

## Decisions

Settled choices (high-protein rule, goal tolerance, shopping-check identity, Budget/Overview naming, unit preferences, author fallback) live in [DECISIONS.md](DECISIONS.md), the single source of truth for dependent tickets.

Still open: account deletion is a separate decision ticket (#513), not presumed functional in v3.

## Acceptance baseline

Per PR: targeted tests, relevant verify gates, en/pl, keyboard/focus, loading/empty/error states, role permissions and 390/1024/1440 light/dark checks. Regenerate OpenAPI types for changed contracts. Update Control kit with actual reused/new primitives. Visual references use fallback font; verify real Outfit layout, not pixel-perfect text widths.

## Tickets

Epic: [#477](https://github.com/piotrkantorowicz/home-system/issues/477).

- [#478 — docs(ui): archive redesign v3 handoff and resolve contract mismatches](https://github.com/piotrkantorowicz/home-system/issues/478) — Publish handoff, before/after assets, comparison and decision record under docs/redesign-v3. Dependencies: none. Backend: existing/dependent contracts only.
- [#479 — feat(ui): establish redesign v3 tokens and surface primitives](https://github.com/piotrkantorowicz/home-system/issues/479) — Adapt existing theme tokens, semantic type scale, three radii and flat surfaces; update Card/Button/Field variants and Control kit. Dependencies: #478. Backend: existing/dependent contracts only.
- [#480 — feat(ui): add preference-aware formatting and goal status](https://github.com/piotrkantorowicz/home-system/issues/480) — Adapt supplied pure formatting helpers into shared/lib and existing preferences; expose goal status and numeric text primitives. Dependencies: #478. Backend: existing/dependent contracts only.
- [#481 — fix(ui): show recoverable route errors](https://github.com/piotrkantorowicz/home-system/issues/481) — Add friendly route error boundary with Retry and safe navigation. Dependencies: none. Backend: existing/dependent contracts only.
- [#482 — fix(diet-planner): handle incomplete profile and translated settings states](https://github.com/piotrkantorowicz/home-system/issues/482) — Fix namespace collision in preferences, remove development copy, guard missing prediction fields, default reminder hints, recipe plurals and section deep links. Dependencies: #480. Backend: existing/dependent contracts only.
- [#483 — feat(ui-shell): replace rail navigation with a module sidebar](https://github.com/piotrkantorowicz/home-system/issues/483) — One 240px sidebar, registry-driven module switcher, search and footer destinations; shared PageHeader and stable content container. Dependencies: #479. Backend: existing/dependent contracts only.
- [#484 — feat(ui-shell): add module mobile tabs and More sheet](https://github.com/piotrkantorowicz/home-system/issues/484) — At most five tabs per module; More sheet exposes remaining routes and module switcher; Budget add action opens existing expense form. Dependencies: #483. Backend: existing/dependent contracts only.
- [#485 — feat(diet-planner): support product completeness filters and sorting](https://github.com/piotrkantorowicz/home-system/issues/485) — Add incomplete-nutrition filtering and whitelisted stable column sorting before pagination. Dependencies: #478. Backend: required.
- [#486 — feat(diet-planner): support recipe nutrition and preparation filters](https://github.com/piotrkantorowicz/home-system/issues/486) — Add high-protein and under-15-minute filters before pagination using approved high-protein definition. Dependencies: #478. Backend: required.
- [#487 — feat(budget): persist expense descriptions and search](https://github.com/piotrkantorowicz/home-system/issues/487) — Optional description up to 80 characters across create/correct/list/detail/search and immutable revision snapshots. Dependencies: none. Backend: required.
- [#488 — feat(diet-planner): persist household shopping check-offs](https://github.com/piotrkantorowicz/home-system/issues/488) — Add authorized idempotent check/uncheck persistence and expose bought state in household shopping query. Dependencies: #478. Backend: required.
- [#489 — feat(budget): expose filtered expense totals and monthly counts](https://github.com/piotrkantorowicz/home-system/issues/489) — Expose exact full-filter expense totals, caller split-share totals and daily totals; add monthly expense counts to summary/envelopes. Dependencies: #487. Backend: required.
- [#490 — feat(diet-planner): redesign Today summary and meal list](https://github.com/piotrkantorowicz/home-system/issues/490) — Unified calorie/macros/water surface, all today slots, next meal emphasis and check toggles; planned footer. Dependencies: #483, #484, #480. Backend: existing/dependent contracts only.
- [#491 — feat(diet-planner): add labelled daily charts and Today history](https://github.com/piotrkantorowicz/home-system/issues/491) — Reusable daily bars with value/date labels, target line and today highlight; replace Today weekly review. Dependencies: #490. Backend: existing/dependent contracts only.
- [#492 — feat(diet-planner): redesign week meal plan states](https://github.com/piotrkantorowicz/home-system/issues/492) — Week grid, person/day-week controls, Import/Add actions, eaten/planned/missed/next states and daily/weekly meters. Dependencies: #483, #480. Backend: existing/dependent contracts only.
- [#493 — feat(diet-planner): simplify mobile meal actions](https://github.com/piotrkantorowicz/home-system/issues/493) — Day meal rows with one 44px completion control and overflow menu for swap/reset/edit/delete. Dependencies: #492, #484. Backend: existing/dependent contracts only.
- [#494 — feat(diet-planner): redesign shared shopping list](https://github.com/piotrkantorowicz/home-system/issues/494) — One scrolling list, bought progress/section, date range, Copy and export menu; integrate shared check-offs. Dependencies: #483, #480, #488. Backend: existing/dependent contracts only.
- [#495 — feat(diet-planner): redesign nutrition summaries and charts](https://github.com/piotrkantorowicz/home-system/issues/495) — 7/30/90-day selector, neutral summaries, calories chart, kcal-based macro comparison and daily totals. Dependencies: #491, #480. Backend: existing/dependent contracts only.
- [#496 — feat(diet-planner): redesign water logging and history](https://github.com/piotrkantorowicz/home-system/issues/496) — Today progress, 250/330/500/custom actions, newest-first entries, 7-day goal chart and setup summary. Dependencies: #491, #480. Backend: existing/dependent contracts only.
- [#497 — feat(diet-planner): redesign product table and filters](https://github.com/piotrkantorowicz/home-system/issues/497) — Neutral sortable sticky-header table with All/Mine/Incomplete chips, unit labels and permission-aware actions. Dependencies: #483, #480, #485. Backend: existing/dependent contracts only.
- [#498 — feat(diet-planner): redesign compact recipe library](https://github.com/piotrkantorowicz/home-system/issues/498) — Compact cards without placeholder photo space, kcal-based macro bar, plural servings, chip filters and menu actions. Dependencies: #483, #480, #486. Backend: existing/dependent contracts only.
- [#499 — feat(diet-planner): redesign recipe detail and plan context](https://github.com/piotrkantorowicz/home-system/issues/499) — Scaled ingredient list with product links, method, per-serving nutrition, Add to plan, danger action and this-week meal context. Dependencies: #498. Backend: existing/dependent contracts only.
- [#500 — feat(diet-planner): redesign file-first plan import](https://github.com/piotrkantorowicz/home-system/issues/500) — Choose-file/dropzone, optional JSON paste/format, review counts/warnings and actionable success. Dependencies: #483, #480. Backend: existing/dependent contracts only.
- [#501 — feat(diet-planner): consolidate profile and diet settings](https://github.com/piotrkantorowicz/home-system/issues/501) — One scrolling Settings route with Profile/Goals/Meal times/Water/Reminders anchors and existing forms. Dependencies: #483, #482. Backend: existing/dependent contracts only.
- [#502 — feat(ui-shell): consolidate app and account settings](https://github.com/piotrkantorowicz/home-system/issues/502) — App/account route combines channel preferences, theme previews, language/density/unit preferences and sign out. Dependencies: #501, #480. Backend: existing/dependent contracts only.
- [#503 — feat(household): redesign people and household settings](https://github.com/piotrkantorowicz/home-system/issues/503) — People rows, initials, permission-aware roles/actions, invitations/name save and separate leave/delete danger rows. Dependencies: #483, #479. Backend: existing/dependent contracts only.
- [#504 — feat(notifications): redesign grouped notification inbox](https://github.com/piotrkantorowicz/home-system/issues/504) — All/Unread chips, Today/Earlier groups, semantic item action and mark-all-read; remove selection checkboxes. Dependencies: #483, #502. Backend: existing/dependent contracts only.
- [#505 — feat(budget): redesign monthly budget overview](https://github.com/piotrkantorowicz/home-system/issues/505) — Monthly summary, limit meters, settlement prompt, category bars and latest expenses. Dependencies: #483, #480, #489. Backend: existing/dependent contracts only.
- [#506 — feat(budget): redesign grouped expense history](https://github.com/piotrkantorowicz/home-system/issues/506) — Month/search/envelope/category filters, day groups, server totals and description-first rows. Dependencies: #505, #487. Backend: existing/dependent contracts only.
- [#507 — feat(budget): redesign expense entry with sticky mobile save](https://github.com/piotrkantorowicz/home-system/issues/507) — Amount-first create/correct form, category chips, payer and split cards, description, duplicate panel and phone sheet. Dependencies: #483, #480, #487. Backend: existing/dependent contracts only.
- [#508 — feat(budget): redesign expense detail and revision timeline](https://github.com/piotrkantorowicz/home-system/issues/508) — Description/amount header, split summary, details and client-side changed-field revision timeline; separate Void row. Dependencies: #506, #507. Backend: existing/dependent contracts only.
- [#509 — feat(budget): redesign settlement actions and payment history](https://github.com/piotrkantorowicz/home-system/issues/509) — Suggested payment cards, prefilled confirmation, different amount, neutral balances and recent/history expansion. Dependencies: #505. Backend: existing/dependent contracts only.
- [#510 — feat(budget): redesign shared and personal envelope lists](https://github.com/piotrkantorowicz/home-system/issues/510) — Shared/personal rows with monthly counts/limits, menus and collapsed archived section. Dependencies: #505. Backend: existing/dependent contracts only.
- [#511 — feat(admin): redesign failed-message monitoring](https://github.com/piotrkantorowicz/home-system/issues/511) — Neutral summaries, source display names, local times, one-line error previews, payload/history panel and retry actions. Dependencies: #483, #480. Backend: existing/dependent contracts only.
- [#512 — test(e2e): validate redesign v3 across screens and themes](https://github.com/piotrkantorowicz/home-system/issues/512) — Final cross-screen acceptance sweep and stable regression fixtures after per-slice tests. Dependencies: #493, #494, #495, #496, #497, #499, #500, #502, #503, #504, #508, #509, #510, #511. Backend: existing/dependent contracts only.
- [#513 — docs(account): decide account deletion lifecycle for redesign v3](https://github.com/piotrkantorowicz/home-system/issues/513) — Document whether deletion belongs in v3 and design Authentik, household ownership, shared budget history and per-module cleanup responsibilities. Dependencies: #478. Backend: required.
