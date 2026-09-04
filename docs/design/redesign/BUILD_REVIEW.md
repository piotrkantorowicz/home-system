# Build review — round 2 (against `dist` of 4 Sep 2026)

Read this **after** `README.md`. It reviews the shipped build of the round-1 spec and specifies
the changes to make next. The designs for every item live in the **Build review** section of
`HomeSystem Redesign.dc.html` (first item in the rail) — ids `#fix-nav`, `#fix-water`, `#fix-home`.

## What landed well — do not touch
- **The token layer is used properly.** Semantic names throughout (`--color-water`, `text-text-2`,
  `border-border-strong`); both themes resolve from one place. No raw hexes in components.
- **Real primitives, not copies.** Ring, MacroBar, MetricTile, StatusPill, SegmentedControl,
  Skeleton, EmptyState, Banner, Pagination, Field, Switch each ship as their own module.
- **Every screen has loading and empty states**, with skeletons at the real geometry.
- **The module registry is the strongest decision in the build** — `basePath`, `navItems`,
  `routes`, `localeNamespaces` and `i18nResources` per module means a second module needs no
  shell changes. Everything below builds on it rather than around it.
- **Bilingual copy is fully wired through i18n**, not hard-coded.
- **The redundant header search is gone.** Correct call — it was a permanently-open global field
  that searched nothing.

## Changes, in priority order

### 1 — Two-tier navigation, grouped per module → `#fix-nav`
**Problem.** The rail flattens every registered module's `navItems` into one unlabeled column of
icons. With one module it reads as an app nav; with two it becomes a soup where nothing says
which product an icon belongs to. Separately, only 4 of the 10 diet-planner destinations are
registered — Hydration, Nutrition, Import, Profile, Preferences and Control Kit are reachable
only by typing the URL.

**Design.**
- **Module rail, 64px**, `--surface-2`, 1px right border. 38px "H" mark (opens the module
  switcher), then one 42px / 13px-radius tile per registered module — active tile gets
  `--surface` + `--border-strong` + `--shadow`, its icon in `--primary`, and a 3px × 22px
  `--primary` marker pinned to the rail's left edge. A dashed "+" tile ends the list. Theme
  toggle and avatar sit at the bottom.
- **Section panel, 216px**, `--surface`, 1px right border, `padding: 16px 12px`. Module heading:
  28px `--primary-soft` icon square + 13.5px/700 name over a 10.5px `--text-3` Polish name.
  Then grouped links: a 10px/700 uppercase `--text-3` group heading (`letter-spacing: .07em`)
  over rows of `padding: 8px 9px`, 10px radius, 16px icon + 13px label — active row
  `--primary-soft` / `--primary-ink` / 650, idle `--text-2`, hover `--bg-2`. Rows may carry a
  right-aligned count (11px `--text-3` tabular) or a badge (18px `--primary` pill).
  Preferences sits below a 1px divider at the bottom in `--text-3`.
- **Groups:** Plan (Today, Meal plan, Import plan) · Library (Products, Recipes, Shopping list) ·
  Track (Nutrition, Hydration, Goals) · then Preferences. Control Kit stays dev-only.
- **Registry change:** register all ten destinations, and add one optional `group` field to
  `navItem` (plus the existing optional `Badge`). Items without `group` fall into an unlabeled
  first group, so any module written today keeps working. The shell renders nav *entirely* from
  the registry — adding a module is still one file.
- **Collapsing:** a chevron on the module heading folds the panel to icons-only (64 + 56px);
  persist the choice. Below 768px both tiers go away — the module rail becomes a switcher in the
  header, the sections become the bottom tab bar already specified in the mobile mock.
- **Cost:** 280px of chrome vs 76px today. That is the price of labels, and labels are what let a
  module carry ten destinations.

### 2 — Water: add and remove in the same place → `#fix-water`
**Problem.** `Hydration` does have a per-entry delete, but a mistapped glass sends you down the
page to find it. Three more control faults in the same card: `disabled={isPending}` greys *all*
quick-add buttons while any one is in flight; the custom amount is two permanent 42px inputs
plus a third button for what is one number; and the success toast has no action.

**Design.**
- **Glasses become real buttons**, 60px tall, 13px radius, `gap: 8px`. Filled = `--color-water`
  with its amount in 11px/700 white at 85% opacity; partial = 55% tint printing the *actual*
  millilitres (so a 150 ml sip isn't drawn as a full glass); empty = `--bg-2` + dashed
  `--border-strong`, and the **first** empty glass shows a plus. On hover/focus a filled glass
  swaps its amount for a 26px white minus disc, gains a 2px `--color-water` focus outline at 2px
  offset, and shows a "Remove 250 ml" tooltip (inverted, 24px, 8px radius) **above** the row —
  reserve `padding-top: 36px` on the row wrapper and give the tooltip a `z-index`, or flip it to
  `top: calc(100% + 8px)`. Tapping removes the newest entry of that size: a mistap is undone by
  a second tap, in place.
- **Quick add:** keep the other buttons live and let mutations queue — water logging is additive,
  so two fast taps are a feature. Only the pressed button takes the 14px spinner ring.
  `+ 250 ml` stays primary in `--color-water`; 500 / 750 secondary.
- **Custom amount becomes a popover** behind a dashed "Custom · Własna" button: 18px radius,
  `--surface-2`, `--shadow-lg`, 16px padding. Inside, a single 42px stepper shell (38px −/+
  buttons around a 68px tabular value, ±50 ml, arrow keys), a "ml" label, three presets derived
  from the user's own most-used amounts, and the note demoted to an optional 38px field beside a
  38px `--color-water` "Add".
- **Undo in the toast:** the success toast already fires; give it an "Undo" action (28px, 9px
  radius, 1px `currentColor` border on the inverted surface), 6 s, keyboard reachable. Apply the
  same pattern to "Mark eaten" on the dashboard.
- **Log rows:** trash glyph instead of `×`; delete confirms **inline** — the row turns
  `color-mix(in oklab, var(--fat) 7%, transparent)`, full-bleed to the card edges, with
  "Remove this entry?" and No / Remove buttons at 28px — no dialog. Add the day's total
  (13px/700 `--color-water`, tabular) to the card header.

### 3 — Drop the launcher page → `#fix-home`
**Problem.** `/` renders "Welcome back {name}" plus a grid containing one module card. It costs a
click, teaches nothing, and gets worse — not better — as modules are added, because switching
modules becomes "navigate back out to the launcher".

**Design.** `/` **resolves rather than renders**: one module installed → redirect to its
`basePath`; several → redirect to the last one used (persist in local storage); none → the only
case that still needs a page, and then it's an onboarding empty state, not a grid of one card.
Module switching moves into the rail's "H" mark: a 16px-radius `--shadow-lg` menu, 7px padding,
11px-radius rows, active row `--primary-soft` with a check; then a divider and
"Browse modules" (dashed icon square) and "Search everything" with a `⌘K` chip. The launcher's
two real jobs are absorbed — discovery by the rail's "+" tile, the greeting by the "Today"
heading, which greets you with something useful.

### 4 — Smaller fixes
- **One `GlassRow`, one geometry.** The dashboard card draws 44px glasses, `Hydration` draws
  54px (now 60px), and the dashboard's "Custom" navigates away instead of opening the popover.
  Extract the component and share it; the dashboard passes a compact size.
- **Signed "Remaining".** The hero swaps to the over-budget treatment, but the stat strip still
  prints a bare number when `target − eaten` is negative. Print U+2212 and colour it `--fat`.
- **Delete the stale `search_placeholder` key** (`"Search food, recipes… / Szukaj…"`).
- **Header middle slot** is now a *command trigger*, not a field: 38px, 12px radius, magnifier +
  "Search · Szukaj" + a `⌘K` chip (22px, 6px radius, `--bg-2`), opening the cross-module palette.

## Suggested order
1 (nav + registry) → 3 (drop `/`) → 2 (water controls) → 4 (sweep). Items 1 and 3 are the same
shell change and are best done together; 2 is self-contained; 4 is cleanup.
