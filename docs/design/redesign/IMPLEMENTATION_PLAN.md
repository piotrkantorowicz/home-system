# Implementation plan (for Claude Code)

Read `README.md` first — it is the spec. Open `HomeSystem Redesign.dc.html` in a browser
beside the running app while you work.

**Ground rules**
- Recreate the design in the existing stack (React 19, React Router, TanStack Query, Tailwind v4,
  Radix, lucide-react). Do not port the HTML.
- No new dependencies without asking. No API/schema changes — this is presentation only.
- Keep both languages wired through the existing i18n dictionaries (PL strings already exist).
- Every screen must pass: light + dark, 390px / 768px / 1280px / 1600px, keyboard focus visible.

## Phase 1 — Tokens (do this first, alone)
1. In the theme layer of `index.css`, replace the light and dark palettes with the token table
   in the README. Keep the existing token *names* the app already consumes
   (`background`, `card`, `border`, `primary`, `muted-foreground`, …) and map:
   `background→--bg`, `card/popover→--surface`, `secondary→--surface-2`, `muted→--bg-2`,
   `muted-foreground→--text-3`, `foreground→--text`, `border/input→--border`,
   `accent→--primary-soft`, `accent-foreground→--primary-ink`, `ring→--primary`,
   `success→--good`, `warning→--carbs`, `destructive→--fat`.
2. Add the four macro tokens plus `--water`, `--border-strong`, `--shadow`, `--shadow-lg`.
3. Set card radius to 22px, control radius to 12–13px in the theme's radius scale.
4. Add the global `letter-spacing: -0.01em` and `tabular-nums` utility for data.
5. Stop here and eyeball every existing screen — most of the refresh lands in this phase.

## Phase 2 — Shell
6. Build the 76px icon rail (`Sidebar` replacement) with stacked EN/PL labels + active pill.
7. Rebuild the header: sticky, blurred, search field, plan pill, notification button.
8. Below 768px: rail → bottom tab bar (4 tabs), header condenses to title + notification.
9. Move the theme toggle into the rail; keep the `system` option in Preferences.

## Phase 3 — Control kit (build against the "Control kit" page in the design)
10. `Card` (22px radius, 22–26px padding, 1px `--border`, single `--shadow`).
11. `Button` — primary / secondary / ghost / destructive × 42 / 34 / 32 / 30px, plus disabled,
    loading and icon-only. `Input` / `Select` / `Stepper` — default, focus, error, disabled,
    unit suffix. `Switch`, `Checkbox` (incl. indeterminate), `Radio`, `Slider` restyled on the
    existing Radix primitives.
12. `Badge`, `Chip` (removable + filter), `MealChip` (macro-tinted + current), `StatusPill`,
    `SegmentedControl`, `Tabs`, `Breadcrumb`, `Dialog`, `Tooltip`, `DropdownMenu`, `Toast`.
13. `MetricTile`, `MacroBar`, `Ring`, `BarChart`, `Skeleton`, `EmptyState`, `Banner`
    (success / warning / error).
14. Verify every state against the Control kit page in both themes before moving on.

## Phase 4 — Today
13. Compose the hero (ring + stats strip + 4 macro bars) from Phase 3 parts.
14. Next-up card off `useMeals` — featured next meal, following meals, eaten state.
15. Water card off `useHydration` — glass count/size from settings.
16. This-week bar chart with today highlighted and future days as dashed stubs.
17. Empty and over-target states.

## Phase 5 — Meal plan
18. Week grid (92px + 7 columns), today column tint, macro-tinted chips, dashed empty slots.
19. Day-total row with signed deltas vs target.
20. Wire chip click → meal sheet, empty slot → add-meal dialog, chevrons → `?week=`.
21. Drag to move a meal (existing DnD approach or `@dnd-kit` if already present).
22. Day and Month view modes reusing the same chip.

## Phase 6 — Profile & goals
23. Four cards per README; keep all existing form logic and validation.
24. Energy-model tiles from the prediction query; deficit tile tinted `--good`.
25. Reminders rows on the restyled Radix Switch.

## Phase 7 — Import wizard
26. Stepper bar with done/current/upcoming states.
27. Two-column body: dropzone + JSON preview / detected tiles + warning + preview table.
28. Gate "Import N days" on resolved units; error banner variant; success → calendar + toast.

## Phase 8 — Products
29. Filter strip (search + chips + Table/Cards toggle).
30. Table card: 7-column grid, macro-coloured dominant values, incomplete-row tint + badge,
    kebab menu, footer pagination.
31. Product form: two-column layout, required/invalid states, live macro-split sidebar with the
    reconciliation banner, serving presets.

## Phase 9 — Recipes
32. Recipe card + `auto-fill minmax(268px)` grid, filter segmented control, dashed create tile.
33. Recipe detail: hero, action row, ingredients table, numbered method, per-serving card,
    servings stepper that rescales ingredient amounts.

## Phase 10 — Nutrition & Hydration
34. Nutrition: KPI row, intake-vs-target chart with target line and over/unlogged/today variants,
    macro split actual vs target, weight trend with dashed prediction, most-eaten bars.
35. Hydration: glass visual, quick-add row (`--water` primary here), 14-day chart with
    goal-hit highlighting, today's log with delete.

## Phase 11 — Preferences
36. Appearance (theme preview tiles, language, density), Units & formats, Account rows.
37. Keep the theme choice, language and unit prefs in the existing settings store.

## Phase 12 — Sweep
38. Dark mode on every screen; focus rings; `prefers-reduced-motion`; 390 / 768 / 1280 / 1600px.
39. Tabular numerals everywhere data appears; thin-space thousands separators; U+2212 minus signs.
40. Bilingual labels wired through i18n on every screen, not hard-coded.
