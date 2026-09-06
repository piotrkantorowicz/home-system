# Handoff: HomeSystem UI refresh (dashboard, meal plan, goals, import)

## Overview
A visual modernisation of **HomeSystem**, the diet/meal-planning app in `dist/`. Same
information architecture and feature set — new surface treatment, new navigation shape,
tighter typographic hierarchy, and a dashboard reorganised around two questions:
*"Am I on track today?"* and *"What am I eating next?"*

Scope of this handoff: **every route in the app** —
`/` Today · `/calendar` Meal plan · `/products` + `/products/new|:id/edit` Products &
product form · `/recipes` + `/recipes/:id` Recipes & recipe detail · `/nutrition` ·
`/hydration` · `/profile` Profile & goals · `/import` Import wizard · `/preferences` —
plus a **mobile** treatment of the dashboard and a **Control kit** page that specifies every
control in every state.

## About the design files
The files in this bundle are **design references written in HTML** — prototypes that show
intended look and behaviour. They are **not production code to copy**. The task is to
**recreate these designs inside the existing app**: React 19 + React Router + TanStack Query
+ Tailwind (v4, `@theme`/CSS-variable tokens) + Radix primitives, as seen in the shipped
`dist/` bundle. Keep the existing component library, class conventions, i18n layer and data
hooks; change tokens, layout and composition only.

`HomeSystem Redesign.dc.html` (open it in a browser — `support.js` must sit beside it) is the
single source of truth for pixel values. Every number below is taken from it.

## Fidelity
**High fidelity.** Final colours, type scale, radii, shadows and spacing. Recreate closely.
Two deliberate omissions: no real charting library (bars/rings are CSS), and no real drag
interaction in the calendar — use the codebase's existing chart and DnD choices.

---

## Design tokens

Authoring format is **oklch** (matches the Tailwind v4 setup already in `index.css`). Hex
values are approximate fallbacks. The current app stores shadcn-style HSL triples
(`--color-primary: 258 88% 52%`); either keep that format using the HSL column or migrate
the slots to oklch — but do not mix.

### Light theme
| Token | oklch | ≈hex | ≈HSL triple | Use |
|---|---|---|---|---|
| `--bg` | oklch(97.4% .005 286) | #f7f6f9 | 260 20% 97% | app background |
| `--bg-2` | oklch(94.6% .008 286) | #eeecf1 | 264 14% 93% | progress tracks, inactive bars, segmented control trough |
| `--surface` | #ffffff | #ffffff | 0 0% 100% | cards, nav rail, header |
| `--surface-2` | oklch(98.2% .004 286) | #fbfafc | 270 20% 99% | nested tiles inside cards |
| `--border` | oklch(90.5% .008 286) | #e3e0e8 | 258 14% 89% | all 1px hairlines |
| `--border-strong` | oklch(85% .01 286) | #d3cfdb | 258 14% 83% | dashed empty slots, hover borders |
| `--text` | oklch(21% .02 286) | #26232c | 264 12% 15% | primary text |
| `--text-2` | oklch(46% .015 286) | #67626f | 264 6% 41% | secondary values |
| `--text-3` | oklch(62% .012 286) | #918c99 | 264 6% 57% | labels, Polish sub-labels, placeholders |
| `--primary` | oklch(53% .21 288) | #6d33e0 | 261 75% 54% | brand violet: primary buttons, active nav, today column |
| `--primary-soft` | oklch(95.5% .028 288) | #f0eafe | 258 88% 96% | active nav pill, "next meal" card, plan badge |
| `--primary-ink` | oklch(43% .19 288) | #5620b8 | 262 70% 42% | text/icons on `--primary-soft` |
| `--protein` | oklch(58% .16 250) | #3f7ac9 | 215 60% 52% | protein bar, protein-led meal chips |
| `--carbs` | oklch(72% .16 70) | #d18b2a | 35 70% 50% | carbs bar, carb-led chips, warnings |
| `--fat` | oklch(63% .19 15) | #d8484f | 357 65% 57% | fat bar, over-target values, notification dot |
| `--fiber` | oklch(60% .13 160) | #2f9370 | 162 50% 38% | fiber bar, veg-led chips |
| `--water` | oklch(64% .13 220) | #2e93bd | 195 60% 46% | hydration |
| `--good` | oklch(55% .14 155) | #2c8556 | 150 50% 35% | on-track / deficit / success |

### Dark theme (applied on `.dark`, same names)
| Token | oklch | ≈hex |
|---|---|---|
| `--bg` | oklch(16.5% .014 286) | #201f26 |
| `--bg-2` | oklch(20% .016 286) | #292830 |
| `--surface` | oklch(21.5% .016 286) | #2b2a33 |
| `--surface-2` | oklch(24.5% .017 286) | #333139 |
| `--border` | oklch(29% .018 286) | #3d3b46 |
| `--border-strong` | oklch(35% .02 286) | #4c4a56 |
| `--text` | oklch(95% .008 286) | #f0eef4 |
| `--text-2` | oklch(72% .014 286) | #b3aeba |
| `--text-3` | oklch(58% .014 286) | #8a8492 |
| `--primary` | oklch(70% .17 290) | #a487f0 |
| `--primary-soft` | oklch(28% .06 290) | #34255c |
| `--primary-ink` | oklch(82% .11 290) | #c9b4fb |
| `--protein` | oklch(72% .13 250) | #6ba4e2 |
| `--carbs` | oklch(80% .14 75) | #e5ab55 |
| `--fat` | oklch(72% .15 18) | #ec7a7d |
| `--fiber` | oklch(74% .12 162) | #5fc79f |
| `--water` | oklch(75% .12 220) | #62b8d9 |
| `--good` | oklch(74% .14 158) | #56c795 |

The dark theme is **desaturated violet-neutral, not blue-black** — this is a change from the
current `250 30% 6%` background, which is both too dark and too blue for long sessions.

### Radii
`8px` chips/inline · `9–11px` small buttons & water glasses · `12–14px` inputs, icon buttons,
nav pills, primary buttons · `15–16px` nested tiles, tables, banners · `18px` dashed dropzone,
legend bar · `20–22px` **all cards** · `34px` phone frame · `999px` pills, toggles, progress bars.

### Shadows
```css
--shadow:    0 1px 2px oklch(20% .02 286 / .05), 0 8px 24px -12px oklch(20% .02 286 / .18);
--shadow-lg: 0 2px 4px oklch(20% .02 286 / .06), 0 24px 48px -20px oklch(20% .02 286 / .28);
/* dark: 0 1px 2px #0006, 0 8px 24px -12px #0009  /  0 2px 4px #0006, 0 24px 48px -20px #000c */
```
Exactly one shadow level on cards. No inner glows, no gradients except two: the avatar and the
goal-progress bar (`linear-gradient(140deg, var(--primary), var(--water))` and
`linear-gradient(90deg, var(--primary), var(--water))`).

### Typography
**Outfit** (already loaded in `index.html`), weights 300–800. Global `letter-spacing: -0.01em`;
headings `-0.03em`; big numerals `-0.04em`.

| Role | Size / weight | Notes |
|---|---|---|
| Screen title (h1) | 30px / 700 | dashboard only |
| Screen title (h2) | 26px / 700 | other screens |
| Screen sub-line | 14px / 400, `--text-3` | Polish + date |
| Card title | 15px / 700 | |
| Card sub-line | 12.5px / 400, `--text-3` | Polish + context |
| Hero numeral | 34px / 700 | kcal remaining |
| Metric numeral | 17–20px / 700 | KPI tiles |
| Body / row label | 13–13.5px / 600 | |
| Data value | 12.5–13px / 400, `--text-2` | `font-variant-numeric: tabular-nums` **always** |
| Micro label | 10.5–11px / 600, `--text-3` | uppercase, `letter-spacing .05–.06em` |
| Nav label | 9.5px / 600 (EN) + 8.5px / 400 @ 65% opacity (PL) | |

Numbers use thin-space thousands separators: `2 150`, `1 240`. Minus signs are U+2212 (`−330`).

### Spacing
4px base. Card padding `22–26px`; grid gap `18px`; screen horizontal padding `32px`;
section gap `40px`; intra-card stack gap `14–20px`; table cell padding `8–14px`.

---

## Layout shell

```
┌──────┬─────────────────────────────────────────────┐
│ rail │ sticky header (blurred)                     │
│ 76px ├─────────────────────────────────────────────┤
│      │ screen content, 32px gutters                │
└──────┴─────────────────────────────────────────────┘
```

**Icon rail** — `width: 76px`, `flex: 0 0 76px`, `position: sticky; top: 0`, full viewport
height, `--surface` with a 1px right border. Contents top-to-bottom: 40×40 logo mark
(13px radius, `--primary`, white "H", 16px bottom margin), then nav items, `flex: 1` spacer,
theme-toggle button, 34×34 avatar.

**Nav item** — 56px wide, vertical stack: 20px icon, 9.5px/600 English label, 8.5px Polish
label at 65% opacity; `padding: 9px 0`, `border-radius: 14px`, gap 5px.
Active: `--primary-soft` background, `--primary-ink` text. Idle: `--text-2`.
Hover: `--bg-2` background, `--text` text.
Order: Today/Dzisiaj · Plan/Plan · Food/Jedzenie · Water/Woda · Import/Import · Goals/Cele.

**Header** — `position: sticky; top: 0; z-index: 5`, `padding: 14px 32px`, 1px bottom border,
`background: color-mix(in oklab, var(--bg) 88%, transparent)` + `backdrop-filter: blur(14px)`.
Left: "HomeSystem" 17px/700 + "Diet planner · Planer diety" 12px `--text-3`.
Middle: a **command trigger, not a search field** — 38px tall, 12px radius, `--surface` +
border, magnifier icon, label "Search · Szukaj" and a `⌘K` key hint chip (22px, 6px radius,
`--bg-2`). It opens the cross-module command palette; a permanently-open global input that
searches nothing is exactly what got removed.
Right: plan pill (`--primary-soft`, 34px tall, 999px radius, 6px dot + "Cut plan · 2 150 kcal")
and a 38px notification icon button with a `--fat` count badge (17px min-width, 2px `--bg` ring).

**Bilingual rule** — English leads, Polish follows. Titles get the Polish translation in the
sub-line; short labels use an inline "EN · PL" separator (middle dot, thin spaces); nav uses
stacked EN/PL. Wire both through the existing i18n dictionaries (the bundle already carries
full PL strings) — the design shows both because the app serves a bilingual household.

---

## Screens

### 1. Today (dashboard) — `/`
**Purpose:** answer "am I on track today" in under a second, then "what's next".

**Layout:** title row (title left, actions right, `flex-wrap`), then
`grid-template-columns: repeat(auto-fit, minmax(330px, 1fr)); gap: 18px; align-items: start`.
Hero and week cards span 2 columns; Next-up and Water occupy 1 each. On a single-column
viewport the order becomes hero → next up → water → week.

**Title row:** "Today" 30px/700; sub "Dzisiaj · Thursday, 3 September 2026" 14px `--text-3`.
Actions: secondary "Log water" (42px tall, 13px radius, `--surface` + border, droplet icon)
and primary "Log a meal · Dodaj posiłek" (`--primary`, white, plus icon, `--shadow`;
hover `filter: brightness(1.08)`).

**Hero card** (span 2, 26px padding, 22px radius):
- Header row: "On track" 15px/700 + "Na dobrej drodze · 58% of target eaten" 12.5px `--text-3`;
  right side a status pill — 30px tall, `color-mix(in oklab, var(--good) 14%, transparent)`
  background, `--good` text, check icon, "Within budget". Over target → swap to `--fat`
  and "Over budget"; no data yet → `--text-3` on `--bg-2`, "Nothing logged".
- Ring: 176px circle, `conic-gradient(var(--primary) 0 <pct>, var(--bg-2) <pct> 100%)`, with an
  absolutely positioned `inset: 15px` `--surface` disc; centred "910" 34px/700 and
  "kcal left / pozostało" 11.5px `--text-3`. Over target: ring colour → `--fat`.
- Stats strip: Eaten 1 240 · Target 2 150 · Burned 2 480 — 11px uppercase label, 19px/700 value,
  separated by 1px `--border` dividers, 22px gap.
- Macro bars ×4 (Protein/Białko 96/140 g, Carbs/Węglowodany 142/240 g, Fat/Tłuszcz 42/70 g,
  Fiber/Błonnik 18/30 g): label 12.5px/600 left, value 12.5px `--text-2` right, then an 8px
  `--bg-2` track with a 999px fill in the macro colour. Clamp fill at 100% and add a 2px
  `--fat` right cap when exceeded.

**Next up card:** header "Next up / Następne posiłki" + "Full plan →" link to the calendar.
Featured meal block: `--primary-soft` background, 16px radius, 1px
`color-mix(in oklab, var(--primary) 22%, transparent)` border; 46px time column (15px/700 time,
10px meal type in `--primary-ink`); title 14px/650; macros line 12px `--text-2`; two buttons —
"Mark eaten" (`--primary`, white, 32px) and "Swap" (transparent, violet border/text).
Then upcoming rows separated by 1px top borders: 16:30 Snack, 19:30 Dinner. Completed meals
render at 60% opacity with `line-through` and "620 kcal logged".

**Water card:** header "Water / Woda · 1.4 / 2.5 L" with a 24px/700 `--water` percentage.
7 glass cells in a `flex; gap: 7px` row, 46px tall, 11px radius: filled = `--water`,
in-progress = `--water` at .55 opacity, empty = `--bg-2` + 1px dashed `--border-strong`.
Buttons: "+ 250 ml", "+ 500 ml" (`--surface-2` + border) and "Custom · Własna" (dashed).
Glass count and size come from the existing hydration settings.

**This week card** (span 2): header + three KPIs (Avg intake 2 015 kcal, Weight 82.4 kg with
`−0.6` in `--good`, Adherence 6 / 7 days). Bar chart 132px tall, 10px gap, 1px bottom border;
bars `max-width: 46px`, radius `10px 10px 3px 3px`; past days
`color-mix(in oklab, var(--primary) 32%, transparent)`, today solid `--primary` with a bold
day label, future days `--bg-2` + 1px dashed border at a fixed 30% stub height.

### 2. Meal plan — `/calendar`
**Purpose:** scan and edit the week's plan; see each day's total against target.

Header: "Meal plan / Plan posiłków · Week 36 · 31 Aug – 6 Sep"; right side a Day|Week|Month
segmented control (3px padding, `--bg-2` trough, active chip = `--surface` + `--shadow`,
12.5px labels) and two 38px chevron icon buttons.

Grid card: 22px radius, `overflow: hidden`,
`grid-template-columns: 92px repeat(7, minmax(0, 1fr))`. Rows: day header, Breakfast/Śniadanie,
Lunch/Obiad, Dinner/Kolacja, Day total/Suma dnia. Every cell has a 1px left border, every row a
1px bottom border (last row none). Day header: 12px/700 weekday + 11px `--text-3` date;
today's column header uses `--primary-soft` + `--primary-ink` and reads "3 Sep · today"; the
whole today column gets `color-mix(in oklab, var(--primary) 5%, transparent)`.

Meal chip: `padding: 9px 10px`, 12px radius, 11.5px/600 name + 500-weight `--text-2` kcal line,
tinted `color-mix(in oklab, var(--macro) 16%, transparent)` by dominant macro
(carbs = breakfast-ish, protein, fiber = veg-led). The *current* meal renders solid `--primary`
with white text and `--shadow`. Empty slot: full-height dashed `--border-strong` box, 12px
radius, centred 17px "+", `min-height: 52px`.

Day total cell: 12.5px/700 tabular number + an 10.5px delta line — under target in `--good`
(`−130`), over in `--fat` (`+200`), incomplete in `--text-3` ("gap").

Below the grid, a legend bar (18px radius, `--surface` + border, `padding: 14px 18px`):
"Legend · Legenda" then three 12px swatch+label pairs (Carb-led / Protein-led / Veg-led) and a
right-aligned hint "Drag a meal to move it · Przeciągnij, aby przenieść".

### 3. Profile & goals — `/profile`
Four cards in `repeat(auto-fit, minmax(320px, 1fr))`, `align-items: start`:

1. **Identity** — 60px avatar (20px radius, violet→cyan gradient, initials 20px/700), name
   18px/700, "34 · Moderately active · Umiarkowanie aktywny" 12.5px `--text-3`;
   3-up tiles (Height 180 cm / Weight 82.4 kg / Target 75 kg) in `--surface-2`, 15px radius,
   13px padding, 10.5px uppercase micro label + 17px/700 value; goal progress (10px 999px track,
   violet→cyan gradient fill, "7.4 kg to go", "Estimated goal date · Szacowana data celu:
   **12 Nov 2026**"); full-width secondary "Edit profile · Edytuj profil" button.
2. **Energy model** — "Model energetyczny · recalculated daily";
   `repeat(auto-fit, minmax(120px, 1fr))` tiles: BMR 1 690, TDEE 2 480, Daily deficit **−330**
   (tinted `--good` 12% background, 26% border, `--good` numeral), Weekly change −0.3 kg,
   Current BMI 25.4, Target BMI 23.1. Each tile: 14px padding, 15px radius, 10.5px label,
   20px/700 value, 10.5px `--text-3` unit or Polish label.
3. **Macro targets** — 12px 999px stacked bar split 26 / 45 / 29 % in protein/carbs/fat colours,
   then legend rows (10px swatch, 13px/600 label, tabular value "140 g · 26%"), with Fiber
   separated by a 1px top border and 12px padding.
4. **Reminders** — four rows separated by 1px top borders: Meal reminders (15 min before),
   Water reminders (every 90 min, 8:00–20:00), Weekly nutrition summary (Sunday 18:00, **off**),
   Goal milestone alerts. Row: 13.5px/600 title + 11.5px `--text-3` Polish/detail line, and a
   42×24 switch — on = `--primary` with an 18px white knob right; off = `--bg-2` +
   `--border-strong` with a `--surface` knob left and `--shadow`. Use the existing Radix Switch.

### 4. Import wizard — `/import`
Single card, 22px radius, `overflow: hidden`.

**Stepper bar** — `padding: 20px 24px`, `--surface-2`, 1px bottom border. Three steps, each a
28px circle + 13px/650 English label over a 10.5px Polish label, joined by `flex: 1` 2px
connector rails. Done = `--good` circle with a white check and a `--good` rail; current =
`--primary` circle with the white step number; upcoming = `--bg-2` circle, `--border-strong`
border, whole group at 55% opacity, `--border-strong` rail.
Steps: Upload/Wgraj plik → Review/Sprawdź → Confirm/Zatwierdź.

**Body** — `repeat(auto-fit, minmax(300px, 1fr))`, 20px gap, 24px padding.
*Left column:* accepted-file banner (`--good` 10% background / 24% border, 16px radius, file
icon, "autumn-cut-plan.json", "18 KB · parsed in 0.4 s · 28 days", 30px "Replace" button);
dashed dropzone (18px radius, `--surface-2`, 26px padding, centred 26px upload icon,
"Drop another plan here", "Przeciągnij plik JSON · or paste it below"); JSON preview block
(16px radius, `--bg-2`, monospace 11.5px/1.7, `white-space: pre`, `overflow: auto`).
*Right column:* "Detected · Wykryto" 13px/700 `--text-2`; 2×2 tiles (Plan name "Autumn cut",
Range "1 – 28 Sep", Meals 112, New products 37); warning banner (`--carbs` 14% background /
30% border, triangle icon, "3 products need a unit" + Polish detail); preview table
(16px radius, bordered; header row `--surface-2`, 10.5px uppercase; columns
`1.6fr 1fr 0.8fr` = Product / Unit / kcal right-aligned; a missing unit shows "missing" in
`--fat` 600); footer buttons pinned with `margin-top: auto` — "Back · Wstecz" secondary and a
`flex: 1` primary "Import 28 days · Importuj".

### 5. Products — `/products`
Title row ("Products / Produkty · 412 items · 37 added this month") with "Export CSV"
secondary and "New product · Nowy produkt" primary.

**Filter bar** — its own card-ish strip: 18px radius, `--surface` + border, `padding: 12px 14px`.
Holds a 38px search field on `--surface-2`, a row of filter chips (active =
`--primary-soft`/`--primary-ink` with an × ; idle = `--surface-2` + border; one chip carries a
count, e.g. "Missing data · 3"), and a right-aligned Table|Cards segmented control.

**Table card** — 22px radius, `overflow: hidden`,
`grid-template-columns: 2.2fr 1fr .8fr .8fr .8fr .8fr 44px`, 12px gap, rows `padding: 14px 20px`
separated by 1px borders. Header row on `--surface-2`, 10.5px uppercase, sort caret on the
active column. Cell content: name 13.5px/650 + 11.5px `--text-3` Polish name & basis
("per 100 g"); category 12.5px `--text-2`; kcal 13px/650; the dominant macro's number takes its
macro colour at 650 weight, the others stay `--text-2`. All numerics right-aligned and tabular.
Row hover `--surface-2`; trailing 32px kebab menu button.
A row with incomplete data tints `color-mix(in oklab, var(--carbs) 7%, transparent)` and shows a
"NEEDS UNIT" badge (19px, 6px radius, `--carbs` on 22% tint) beside the name.

**Footer** — inside the card, `--surface-2`, 1px top border: "Showing 1–5 of 412 · Pokazano"
and pagination (34px squares, 11px radius; current page solid `--primary`).

### 6. Product form — `/products/new`, `/products/:id/edit`
Breadcrumb (12.5px, chevrons, last crumb `--text-2`/600) above the title.
Two-thirds form card + one-third sidebar, in the same `auto-fit minmax(320px)` grid.

**Form card:** "Basics · Podstawy" section, `auto-fit minmax(220px)` field grid — Name (EN)
required, Nazwa (PL), Category select, Unit select. Field: 12px/650 `--text-2` label above a
42px control, 13px radius, 1px `--border`, `--surface-2` fill; selects carry a 15px chevron in
`--text-3`. **Invalid** field: 1.5px `--fat` border, `color-mix(in oklab, var(--fat) 7%)` fill,
11.5px `--fat` message below, and its label turns `--fat`. A 1px divider, then
"Nutrition per 100 g" with an `auto-fit minmax(150px)` grid of five numeric fields
(kcal, Protein, Carbs, Fat, Fiber) — value 15px/650 tabular, each label in its macro colour.
Footer: right-aligned "Cancel · Anuluj" + "Save product · Zapisz".

**Sidebar:** *Live macro split* card — 12px stacked bar in macro colours, legend rows with
percentages, and a `--good` validation banner confirming macros reconcile with the energy value
(swap to the `--carbs` warning banner when they don't). *Serving presets* card — preset chips
(32px, 10px radius) plus a dashed "+ Add".

### 7. Recipes — `/recipes`
Title row with an All | High protein | Quick | Favourites segmented control and a primary
"New recipe · Nowy przepis".

Grid: `repeat(auto-fill, minmax(268px, 1fr))`, 18px gap. **Recipe card** — 22px radius,
`overflow: hidden`, no padding at the top: a 132px image area (use the recipe photo; the design
shows a macro-tinted `linear-gradient(140deg, …)` placeholder with a "PHOTO · ZDJĘCIE" label),
a top-left time badge (26px, 8px radius, `--surface`, clock icon, `--shadow`) and a top-right
28px favourite button with a filled `--fat` heart when saved. Body `padding: 16px`: title
14.5px/700 + 11.5px `--text-3` Polish name and serving count; a 4-up macro strip where each
cell is a 9px-radius tile (kcal on `--surface-2` + border, P/C/F on their macro at 12% tint)
with a 13px/700 value over a 9.5px `--text-3` letter; then tag chips (24px, 7px radius) —
"In plan · Thu" uses `--primary-soft`, the rest `--bg-2`.
Last grid cell is a dashed "Create a recipe" tile (22px radius, 44px violet icon square).

### 8. Recipe detail — `/recipes/:id`
Breadcrumb + title + "25 min · 2 servings" sub-line. Main card spans 2 columns: 180px hero
image, then `padding: 24px` with an action row ("Add to plan · Dodaj do planu" primary,
"Log now · Zapisz teraz", "Edit"), an **Ingredients** table (16px radius, columns
`2fr .7fr .7fr` = Product / Amount / kcal, header on `--surface-2`) and a **Method** list —
26px `--primary-soft` numbered circles beside 13.5px `--text-2` steps at `line-height: 1.6`,
`text-wrap: pretty`.

Sidebar: *Per serving* card — 34px/700 kcal, three macro bars whose fill shows the share of
today's remaining target, with that caveat spelled out in an 11.5px `--text-3` line.
*Servings* card — 40px −/+ steppers around a 22px/700 tabular count, plus "Ingredient amounts
scale automatically".

### 9. Nutrition — `/nutrition`
Range segmented control (7d | 30d | 90d | All) in the title row.

**KPI row** — `auto-fit minmax(200px)`, 20px radius, 18px padding: 10.5px uppercase label,
24px/700 value with a 12px `--text-3` unit, and a 12px/650 delta line coloured `--good` /
`--carbs` / `--fat` / `--text-3`. Four KPIs: Avg intake, Avg protein, Adherence, Weight change.

**Intake vs target** (spans 2) — 190px bar chart, 4px gap, 5px top radius; in-range days
`color-mix(in oklab, var(--primary) 30%)`, over-target days `color-mix(in oklab, var(--fat) 45%)`,
unlogged days `--bg-2` + dashed border at a short stub height, today solid `--primary`. A 2px
dashed `--fat` target line overlays at the height 2 150 kcal maps to on the bars' own scale —
in the mockup that is `bottom: 77%`, so the only bars crossing it are the two over-target
`--fat` ones. Compute it from the same max as the bars; never hard-code the percentage.
Legend chips above, date ticks below (11px/600 `--text-3`).

**Average macro split** — Actual and Target stacked bars (14px, 999px radius; target at 40%
opacity) under 11px uppercase labels, then legend rows with the delta coloured by direction.

**Weight trend** — 150px plot inside a 14px-radius `--surface-2` panel: solid 2.5px `--primary`
polyline for logged weight, 6/5 dashed continuation for the prediction, a 4.5px dot at today,
and corner labels for start weight and goal. Legend below distinguishes Logged / Predicted.

**Most-eaten products** (spans 2) — horizontal bars: 170px name column, 22px `--bg-2` track with
a 7px-radius fill in the product's dominant macro colour at 55%, and a right-aligned 60px
tabular quantity.

### 10. Hydration — `/hydration`
**Today card** (spans 2, 26px padding) — a 132×176 glass: `18px 18px 26px 26px` radius, 2px
`--water` border, `--surface-2` fill, and an absolutely positioned bottom fill at the
consumed percentage (`linear-gradient(180deg, color-mix(in oklab, var(--water) 75%, transparent),
var(--water))`), with "1.4 L / of 2.5 L" centred over it. Beside it: "1.1 L to go" headline,
the 8-glass row (54px, 12px radius — filled / partial at .55 / dashed empty) and quick-add
buttons where **+250 ml is the primary action in `--water`, not violet** (this is the one screen
where water owns the accent), plus 500/750 secondary and a dashed "Custom · Własna".

**Last 14 days** — 116px bars, 6px gap, `color-mix(in oklab, var(--water) 40%)`, days that hit
the goal in solid `--water`, today at 70% opacity; three footer stats (Daily avg, Goal hit,
Streak) above a 1px divider.

**Today's log** — rows separated by 1px top borders: 44px tabular time in `--text-3`, drink name
13.5px/600, amount 13px/700 in `--water`, and a 30px ghost delete button that turns `--fat` on
hover.

### 11. Preferences — `/preferences`
Three cards, `auto-fit minmax(320px)`.

**Appearance** — Theme as three 14px-radius preview tiles (34px swatch: white / dark surface /
split gradient) over EN+PL labels; the selected tile gets a 1.5px `--primary` border and
`--primary-soft` fill. Language as a 3-up segmented control (English | Polski | Both · Oba).
A "Compact density" switch row below a 1px divider.

**Units & formats** — `auto-fit minmax(140px)` selects: Energy (kcal), Weight (kg), Volume
(ml / L), Week starts (Monday). Plus a "Thin-space thousands" switch (`2 150` vs `2,150`).

**Account** — a 16px-radius `--surface-2` identity tile (44px gradient avatar, name, provider
line), then rows for Export all data, Sign out, and Delete account. The destructive row's label
is `--fat` and its button uses the destructive style (`--fat` text on a 10% tint with a 40%
border). Wire the identity rows to the existing OIDC session.

### 12. Control kit (reference page, not a route)
Specifies every control in every state; **build this before the screens**. Six cards:

- **Buttons** — primary / secondary / ghost / destructive at 42px; small 34px; row-action 32px;
  disabled (`opacity: .45`, `cursor: not-allowed`); loading (14px 2px spinner ring, label
  "Saving…"); 42px icon-only; and the focus treatment (2px `--primary` outline at 2px offset).
- **Inputs** — default / focused (1.5px `--primary` + 30% tint outline) / error / disabled
  (`--bg-2`, 70% opacity); a field with a unit suffix; a −/+ stepper in a single 42px shell.
- **Selection** — switch on/off/disabled; checkbox checked/unchecked/indeterminate (20px, 7px
  radius); radio selected/unselected (20px, 999px); slider (6px `--bg-2` track, `--primary`
  fill, 20px `--surface` thumb with a 2px `--primary` ring).
- **Badges, chips & pills** — status badges (26px, 8px radius) in primary-soft / good / carbs /
  fat / neutral; removable and filter chips (32px, 999px); the notification count badge; and the
  macro-tinted meal chip in its four variants including the solid `--primary` "current" state.
- **Feedback** — success / warning / error banners (16px radius, 10–14% tint, 24–30% border,
  icon in the semantic colour, error variant carries a Retry ghost button); toast (inverted:
  `--text` background, `--bg` text, `--shadow-lg`, trailing Undo); skeleton block; empty state
  (dashed 16px box, 40px icon square, title + PL line + primary CTA).
- **Navigation & overlays** — segmented control; underline tabs (2px `--primary`); breadcrumb;
  dialog (18px radius, `--shadow-lg`, title + PL sub-line, close button, body copy, right-aligned
  Cancel/Delete); tooltip (inverted, 30px, 9px radius); dropdown menu (14px radius,
  `--shadow-lg`, 6px padding, 9px-radius items, hovered item on `--bg-2`, destructive item
  in `--fat`).
- **Palette & type** — swatch grid for all tokens and the type scale, so you can eyeball the
  dark theme by flipping the rail toggle.

### 13. Mobile (dashboard)
390px single column, 18px gutters, 44px minimum hit targets.
Hero collapses into a **solid `--primary` card** (20px radius, white type): 84px ring
(`conic-gradient(#fff 0 58%, #ffffff40 58% 100%)`, `inset: 9px` disc in `--primary`),
"1 240 of 2 150 eaten", three 6px white-on-`#ffffff33` macro bars, "P 96 · C 142 · F 42 g".
Next meal becomes one row with a 44px violet check button. Water keeps 6 glasses at 34px.
Navigation moves to a bottom tab bar (4 tabs: Today, Plan, Food, Goals — 21px icon + 10px/650
label, active `--primary`, idle `--text-3`, 1px top border, `--surface`, 16px bottom padding).
Desktop breakpoints: rail collapses to tabs below 768px; all `auto-fit` grids reflow with no
extra media queries.

---

## Interactions & behaviour
- **Theme** — light/dark toggle in the rail (moon in light, sun in dark). Persist to
  `localStorage['home-system-theme']` and keep the existing no-flash bootstrap script in
  `index.html`; keep the `system` option in Preferences.
- **Nav** — rail items are router links; active state from `useMatch`/`NavLink`.
- **Hover** — buttons and tiles: 120–160ms `ease-out` on `background-color`, `border-color`,
  `filter`. Primary buttons brighten 8%. Nav idle → `--bg-2`.
- **Focus** — 2px `--primary` ring at 2px offset on every interactive element (keep the
  existing `--ring` token wired to `--primary`).
- **Mount** — screens fade in: `opacity 0 → 1`, `translateY(6px) → 0`, 400ms ease. No stagger,
  no scroll-triggered animation.
- **Ring & bars** — animate width/gradient stop over 500ms `ease-out` when values change;
  respect `prefers-reduced-motion` by rendering final state.
- **Calendar** — click an empty slot to open the existing add-meal dialog with date + slot
  prefilled; click a chip for the meal sheet; drag a chip to move it (chip lifts with
  `--shadow-lg`, target cell shows a `--primary` dashed outline); chevrons page the week and
  push `?week=`.
- **Import** — dropzone accepts `.json` only; parse errors replace the accepted banner with a
  `--fat` variant plus the parser message; "Import N days" is disabled until required units are
  resolved; success routes to the calendar on the plan's start week with a toast.
- **Loading** — skeletons in `--bg-2` at the card's real geometry (ring = 176px circle, bars =
  8px pills, table = 3 rows). Never a full-page spinner.
- **Empty** — nothing logged today: ring track only, "Nothing logged yet · Nic nie zapisano"
  and a primary "Log a meal" CTA inside the hero.
- **Errors** — inline, per card: 13px `--fat` text plus a "Retry · Ponów" ghost button; leave
  the card frame intact.

## State
Nothing new: reuse `useGoals`, `useMeals`, `useProducts`, `useRecipes`, `useHydration` and the
weight-prediction query. Local UI state only — theme (`'light' | 'dark' | 'system'`), calendar
`viewMode` (`'day' | 'week' | 'month'`) and `weekStart`, import wizard `step` +
`parsedPlan` + `unitOverrides`, and open/closed state for dialogs. Derived on the client:
kcal remaining, ring percentage (clamped 0–1), macro percentages, day totals and target deltas.

## Assets
None. All iconography is inline 24×24 stroke SVG at `stroke-width: 1.9` (2–2.6 for small
glyphs), `stroke-linecap: round` — geometry matches **lucide-react**, which the app already
bundles; use `lucide-react` rather than the inline copies. Fonts: **Outfit** via the existing
Google Fonts link. No photography or illustration.

## Screenshots
`screenshots/` — `NN-light.png` and `NN-dark.png`, same numbering in both themes:
01 Today · 02 Meal plan · 03 Profile & goals · 04 Import wizard · 05 Products ·
06 Product form · 07 Recipes · 08 Recipe detail · 09 Nutrition · 10 Hydration ·
11 Preferences · 12 Control kit · 13 Mobile. Plus `00-review-*` (audit, nav, water, home) for
the round-2 changes in `BUILD_REVIEW.md`. Captured at a ~900px-wide viewport, so some rows
wrap tighter than they will at desktop widths, and the conic-gradient ring and blurred header
render approximately. **The HTML file is authoritative** for geometry.

## Files
- `BUILD_REVIEW.md` — review of the shipped build + the round-2 changes (nav grouping,
  water controls, dropping the launcher page). **Start here if the app is already built.**
- `HomeSystem Redesign.dc.html` — the full design (all four screens + mobile + theme toggle)
- `support.js` — runtime needed to open that file in a browser
- `IMPLEMENTATION_PLAN.md` — suggested phase order for Claude Code
