# HomeSystem redesign — implementation spec

Target: the React + Tailwind v4 frontend that builds the `dist/` reviewed on 3 Oct 2026
(modules: Diet planner `/diet-planner`, Budget `/budget`, Household `/household`,
Notifications `/notifications`, Admin `/admin`).

- **Target designs:** `screens/*.png` (rendered at 1440 px / 390 px).
- **Current state:** `current/*.png`. These were rendered with mock data, so ignore odd values.
- **Exact values:** `reference/**/*.dc.html` are the mockup sources. They use inline styles; copy spacing, sizes and copy text from them, not the markup.
- **Code you can drop in:** `tokens.css` and `src/lib/format.ts` (with tests).

The direction keeps the current identity (Outfit, violet `--color-primary`, the neutral greys) and changes structure, density and the meaning of colour. Most of the work is **subtraction**.

---

## 0. Principles (apply everywhere)

1. **One navigation layer.** A single sidebar on desktop, a ≤5-item bottom bar on phones. No icon rail, no brand in a top bar, no in-page tab bars that duplicate the sidebar.
2. **Nav label = page title.** Today, Meal plan, Shopping list, Recipes, Products, Nutrition, Water, Settings, Household, Notifications, Budget, Expenses, Settle up, Envelopes.
3. **One surface level.** A section is one white surface: `bg-card`, a 1 px `border-border`, `rounded-lg` (16 px), no shadow. Inside it, group things with spacing and hairlines, never another bordered card.
4. **Red only means state** ("over a limit", errors, destructive actions), and it always comes with an icon or a word. Data colours (protein/carbs/fat/fiber/water) are only for chart marks.
5. **Goals have a direction.** Calories, fat, carbs and envelope limits are *limits* (over = bad). Protein and fiber are *minimums* (over = met). Use `goalStatus()` in `format.ts`.
6. **One way to write each number** (see § 1.4). Show the currency once per section; rows show bare amounts.
7. **Each screen does its core job first:** the answer before the controls, the primary action next to the thing it acts on.

---

## 1. Foundations (Phase 1)

### 1.1 Tokens
- Merge `tokens.css` into the global stylesheet: type scale, three radii, and colour changes (fat → magenta; darker muted text; new `over`, `good-soft`, `warning-soft` and `track` tokens).
- Remove card shadows (`shadow`, `shadow-sm` on cards and sections). Keep `shadow-lg` for popovers, dialogs and menus only.

### 1.2 Typography codemod
Replace arbitrary `text-[Npx]` / `text-Npx` utilities:

| Current | New |
|---|---|
| 9.5, 10, 10.5, 11, 11.5, 12, 12.5px, 0.7rem | `text-label` (12px). Nothing in the UI goes below 12px. |
| 13, 13.5px | `text-meta` (13px) |
| 14, 14.5, 15px, 0.9rem, 0.95rem | `text-body` (15px). Keep 14px only for dense table cells. |
| 17, 19, 20, 22px | `text-section` (18px) |
| 26px, page `<h1>` sizes (text-3xl/4xl) | `text-title` (28px) |
| 34px, hero numerals (text-5xl) | `text-display` (44px) |

All numbers in tables, meters and totals get `tabular-nums`.

### 1.3 Radius codemod
- `rounded-6px`, `rounded-8px`, `rounded-9px` → `rounded-sm` (6px).
- `rounded-10px` through `rounded-13px`, and `rounded-md` → `rounded-md` (10px).
- `rounded-15px` through `rounded-26px`, plus `rounded-xl` and `rounded-2xl` → `rounded-lg` (16px).
- Circles stay `rounded-full`.

### 1.4 Formatting — use `src/lib/format.ts` everywhere
| What | Always | Seen today (wrong) |
|---|---|---|
| Energy | `2 100 kcal` — `formatKcal` | 2100 · 14740kcal · 379.0 |
| Macros | `46 g`, `4.5 g` (decimal only under 10) — `formatGrams` | 46.0 g · 859.0g · P 26.0g |
| Water | `1.3 of 2.5 L` — `formatWaterProgress`; buttons `+250 ml` | 1 330 / 2 500 ml · 1.3 L · 5/10 glasses |
| Money | `4 114.65` (pl: `4 114,65`); currency once per section — `formatMoney` | 4114.65 PLN on every row |
| Dates | `Sat 3 Oct`, `28 Sep – 4 Oct` — `formatDayShort`, `formatDayRange` | 2026-09-27 · Saturday, October 3, 2026 · 10/2/2026 8:40 PM |
| Times | local `21:12` — `formatTime` | 19:12 UTC |
| Units | `g · ml · L · pcs` — `unitLabel`, `formatQuantity` | Gram · Milliliter · Piece · 1000 Milliliter |
| Plurals | i18next `_one/_few/_many/_other` | "1 servings", "Total for 1 servings" |

Wire `format.ts` to the existing `home-system-prefs` store (`thinSpaceThousands`) and to the i18n language.

### 1.5 Accessibility requirements
- **Contrast:** text must reach 4.5:1 (3:1 at 24px and above). The new `--color-muted-foreground` fixes the 3.3:1 grey.
- **Real elements:** use `<button>`, `<a href>` and `<input>` with a `<label>`. Icon-only buttons need an `aria-label`.
- **Touch targets:** at least 44 × 44 px on phones. Desktop table rows are 44 px; compact rows are 36 px.
- **Status:** never shown by colour alone. Pair it with an icon (↑ over, ✓ met) or a word.

### 1.6 Robustness bugs to fix in this phase
| # | Where | Bug | Fix |
|---|---|---|---|
| B1 | Diet planner › Preferences | Page heading reads "Channel preferences / Choose where you receive notifications" (wrong i18n keys). | Page is replaced in Phase 4. Fix the keys now if Phase 4 is later. |
| B2 | Preferences | Ships a dev note: "Unit preferences are saved now; screens will adopt them in a later pass." | Remove it, or adopt the prefs via `format.ts`. |
| B3 | Profile › Energy model | Shows `NaN` / `−NaN kg` when the prediction is missing (no birth date or sex). | Show an empty state: "Add your birth date and sex to see your resting and daily calorie burn…" plus an *Add details* button. See `21-settings-app-account.png` / `20-settings-diet.png`; the `profileComplete` tweak in the reference shows both states. |
| B4 | Profile › Notifications | Raw `{{count}} min before`, `every {{count}} min` and `undefined` when reminder settings are empty. | Default values; hide the hint while a reminder is off. |
| B5 | Recipes / Recipe detail | "1 servings", "Total for 1 servings". | Plural keys (en: one/other; pl: one/few/many). |
| B6 | All routes | No `errorElement`. A bad API response shows React Router's developer page ("Hey developer 👋"). | Add a route-level error boundary with a friendly message and a Retry button. |
| B7 | Profile page | Sidebar highlights "Goals" while the page highlights "Overview". | Disappears with Settings in Phase 4. Until then, honour `?section=`. |
| B8 | Meal plan "macro-led" tint | Based on grams, so nearly every meal is "carb-led". | Drop the tint (Phase 3), or compute by % of calories. |
| B9 | Add expense on phone | Save button sits below the viewport at 390×844. | Phase 5: full-screen sheet with a sticky footer. |

---

## 2. App shell (Phase 2)

**References:** `screens/10-diet-today.png`, `01-module-switcher-*.png`, `11-diet-today-phone.png`, `37-budget-overview-phone.png`.

### Desktop sidebar (240 px, `bg` slightly off-white `#fbfafc` / dark `--color-secondary`, right border)
1. **Module switcher** (top). App mark + current module name + household name + chevron. It opens a menu listing the modules (Diet planner: "Meals, recipes, nutrition, water"; Budget: "Envelopes, expenses, settle up"), with a check on the current one, and a footer link to Household. Build it from the existing module registry (`basePath`, `translationKey`, `icon`, `description`), so new modules appear automatically.
2. **Search** button (opens the existing command palette, ⌘K).
3. **Module nav:**
   - Diet planner: *Today, Meal plan, Shopping list* · LIBRARY *Recipes, Products* · TRACK *Nutrition, Water*.
   - Budget: *Overview, Expenses, Settle up* (badge = number of open suggested payments), *Envelopes*.
4. **Footer group:** *Household*, *Notifications* (unread badge), *Admin* (only when `profile.roles` includes `admin`; badge = failed messages), *Settings*, then the avatar and name.
5. **Active item:** `bg-accent text-accent-foreground font-semibold`, plus `aria-current="page"`.

Remove:
- the icon rail;
- the top bar's brand and module name. If the top bar stays at all, it only holds a mobile menu button;
- the Meal plan page tabs (Calendar / Nutrition / Shopping list / Import). Import becomes a button on Meal plan;
- the third menu on Profile (Phase 4).

### Page template
- **`<PageHeader>`:**
  - an optional breadcrumb;
  - an `<h1 class="text-title">` equal to the nav label;
  - a one-line subtitle (`text-body text-muted-foreground`, e.g. "Sat 3 Oct", "28 Sep – 4 Oct · target 2 100 kcal a day");
  - actions on the right: at most one primary button (filled) plus secondary buttons (outlined), wrapping on narrow screens.
- **Content width:** `max-w-[1120px]`, left-aligned, padding `40px 48px` (desktop) / `16px` (phone). Narrower content (forms, lists) uses `max-w-[880px]` or `max-w-[760px]` *inside* the same container, so the title never jumps.

### Mobile
- The sidebar collapses. A bottom bar with at most 5 items, 44 px+ targets, labels at 12px.
  - Diet planner: Today, Plan, Recipes, Shopping, More.
  - Budget: Overview, Expenses, **raised + button** (Add expense), Settle up, More.
- "More" opens a sheet with the remaining items, the module switcher and the footer group.

### Renames / redirects
| Old label / route | New |
|---|---|
| Dashboard | Today (`/diet-planner`) |
| Calendar page title | Meal plan (`/diet-planner/calendar`, keep route) |
| Nutrition Summary | Nutrition |
| Hydration / Hydration Tracking | Water (keep `/hydration` route or alias `/water`) |
| Goals, Profile & Settings, Preferences, Channel preferences | Settings — two tabs: *Profile · Goals · Meal times · Water · Reminders* and *App & account* (Phase 4) |
| Import Plan (nav item) | "Import" button on Meal plan → `/diet-planner/import` |
| Dead letters | Failed messages (Admin) |

---

## 3. Diet planner screens (Phase 3)

Every screen uses the shell and page template above. Each item lists the reference screenshot, then the changes.

### 3.1 Today — `10-diet-today.png`, phone: `11-diet-today-phone.png`
- **Summary surface:** three columns that wrap.
  - **Calories:** a 44 px "kcal left" number, a bar, "690 eaten / 2 100 target", and a status pill (✓ Within budget, or ↑ Over budget).
  - **Macros:** four rows, each with a name, a thin bar in its data colour and `50 / 140 g`.
  - **Water:** `1.3 of 2.5 L`, a bar, and `+250 ml`, `+500 ml`, `+` (custom) buttons.
- **Meals:** all of today's slots as 60 px rows: time · slot / recipe · kcal · a 44 px check toggle (`aria-pressed`). The next unchecked meal is highlighted (`bg-accent`), shows its macros in one line and a primary "Mark eaten" button. Eaten meals show a filled check. **Never strikethrough.** Footer: "Planned for today: 2 050 kcal · 50 under target".
- **Last 7 days:**
  - Bars labelled with day and date, a value above each bar, and a dashed target line labelled "2 100 target".
  - Today's bar is drawn in the primary colour; over-target values are red with ▲.
  - Under the chart: Average (full days), Over target (n days), Protein average.
  - Use the existing week preference (Monday start) when the range is calendar-based.

### 3.2 Meal plan (week) — `12-diet-meal-plan.png`
- Controls row:
  - "For [person]" select;
  - Day/Week segmented control;
  - ‹ This week › on the right.
- Header actions: *Import* (secondary) and *Add meal* (primary).
- **Grid:** a 112 px slot column plus 7 day columns; the today column is tinted `bg-accent`.
- **Cells:** a 2-line clamped recipe name, then a meta line. Cell states:
  - **Eaten:** solid border, ✓ in `--color-good`, then kcal.
  - **Planned (future):** dashed border.
  - **Not logged (past, not eaten):** `bg-warning-soft`, with "Not logged" in `--color-warning` on its own line above the kcal. The reference wraps; fix that.
  - **Next:** a 1.5 px primary border and "Next ·".
- Drop the macro-led tint and legend (B8).
- **Day total row:** total kcal, then one of:
  - `↑ 100 over` in `--color-over` (bold);
  - `80 under` in muted text;
  - `on target` in `--color-good`.
- **"This week, planned and eaten":** five meters (Calories, Protein, Carbs, Fat, Fiber).
  - Each has a fill in its data colour and a target tick.
  - Each has a status line from `goalStatus`: *On target*, *121 g to go*, *128 g left*, *↑ 63 g over*, *✓ Goal met*.
- Day view on phones: replace the 5 tiny icon buttons per meal with one 44 px check plus a ⋯ menu (swap, reset, edit, delete). See `current/meal-plan-phone.png`.

### 3.3 Shopping list — `13-diet-shopping-list.png`
- Subtitle: "Shared with Kantorowicz Home · from meals planned 28 Sep – 4 Oct". Remove "overrides are ignored".
- Actions:
  - a date-range button;
  - *Copy*;
  - a ⋯ menu holding *Export CSV* and *Export JSON*. The three equal export buttons go.
- A progress line ("5 of 16 bought") with a thin bar.
- **One scrolling list, no pagination.**
  - Rows are 52 px: a native checkbox (22 px, `accent-color: primary`), the name, and the amount via `formatQuantity` ("480 g", "1.2 kg", "1 L", "8 pcs").
  - Checked items move into a "Bought · n" section: muted, with strikethrough (correct here), and an *Uncheck all* button.
- **Needs backend** for shared persistence (§ 7). Until then, keep checks in local state keyed by household + range.

### 3.4 Nutrition — `14-diet-nutrition.png`
- Range control: 7 / 30 / 90 days, segmented, in the header.
- A summary surface with four label/value pairs and status words. **Neutral tiles — no red fills.**
  - Average intake: *✓ On target (2 100)*.
  - Average protein: *17 g short of 140 g a day*.
  - Days logged: *7 of 7*.
  - Days over target: *↑ by 100 kcal each*.
- **Calories by day:** same chart component as Today, larger.
- **Where calories come from:** two labelled stacked bars ("You ate" / "Your goals") showing protein/carbs/fat by % of kcal, with percentages inside the segments; then a legend and one plain-language sentence.
- **Daily totals table:**
  - Day as `Sun 27 Sep`; kcal; "vs target" status (`↑ 100 over` / `80 under` / `on target`); protein/carbs/fat/fiber as integers.
  - 44 px rows, no pagination for ≤ 31 rows.

### 3.5 Water — `15-diet-water.png`
- Title: "Water". Header action: *Water settings* (links to Settings › Water).
- **Today:**
  - `1.3` (44px) "of 2.5 L", a bar, and "1.2 L to go" (or "✓ Goal reached").
  - Four quick buttons, each with a sublabel: +250 ml *glass*, +330 ml *can*, +500 ml *bottle*, *Other…* (dashed).
  - Entries: time · amount · 44 px delete button, newest first; "n drinks". Remove the "Keep tapping — every button stays live…" copy.
- **Last 7 days:** water-coloured bars, a dashed goal line, "Goal met 3 of 6 days".
- **Your setup:** daily goal, glass size and reminders, read-only.

### 3.6 Products — `16-diet-products.png`
- Subtitle: "16 products · nutrition per 100 g or 100 ml".
- Filters: search plus chips (*All · Mine · Incomplete*). Drop the Table/Cards toggle, or keep Cards behind a menu.
- **Table:**
  - 44 px rows, sticky header, sortable columns (sort button in the header with ↑/↓).
  - Columns: Name · Unit (g/ml/pcs) · kcal · Protein · Carbs · Fat · Fiber · ⋯ actions.
  - Numbers are plain text and formatted with `formatGrams` / `formatKcal`. **No coloured "dominant macro" numbers.**
  - Visibility: show "🔒 Private" next to the name only when it isn't the default (Household). Use a lock icon, not an emoji.

### 3.7 Recipes — `17-diet-recipes.png`
- **No image area unless the recipe has a photo.** Cards are compact:
  - a 2-line name;
  - "⏱ 30 min · 1 serving" (plural-aware);
  - kcal in 22px;
  - one stacked bar of protein/carbs/fat by % of kcal (with an `aria-label` giving the percentages);
  - "46 g protein · 58 g carbs · 19 g fat";
  - a ⋯ menu in the top right.
- One legend row next to the filters. Filters are one chip group: *All · Mine · High protein · Under 15 min*.
- Grid: `repeat(auto-fill, minmax(250px, 1fr))`.

### 3.8 Recipe detail — `18-diet-recipe-detail.png`
- Breadcrumb. Title. Meta: "30 min · shared with household · by Paweł".
- Actions: *Edit* (secondary), a ⋯ menu with Delete, and *Add to plan* (primary).
- **Ingredients:**
  - The servings stepper sits in the section header ("− 2 servings +").
  - Amounts scale with servings and are formatted with `formatQuantity`.
  - Each ingredient name links to its product.
- Method: numbered steps.
- **Per serving:**
  - 40px kcal and "29% of your day";
  - a stacked macro bar;
  - rows: Protein, Carbs, Fat, Fiber;
  - with servings > 1, a note: "All n servings: X kcal".
- **In your plan:** the dates and slots where this recipe is planned this week. Use the meals query filtered by `recipeId`; hide the section if there are none.

### 3.9 Import — `19-diet-import.png`
- Breadcrumb: Meal plan › Import. Steps: *Choose file → Review → Done*.
- **Choose file:**
  - a large dropzone with *Choose file* (primary) and *Try the sample plan*;
  - "Paste JSON instead" behind a `<details>`;
  - a link to the format and to a sample file.
  - The JSON schema panel moves behind the "See the format" link.
- **Review:**
  - the file name, size and *Choose another file*;
  - three counts: Meals (+ date range), Recipes (n new · n already in library), Products (n new · n matched);
  - warnings as amber rows in plain language;
  - footer: "Meals already planned for … stay as they are." with *Back* and *Import 35 meals* (the count goes in the button).
- **Done:** a success state with *Import another* and *Open meal plan*.

---

## 4. Settings, Household, Notifications (Phase 4)

### 4.1 Settings — `20-settings-diet.png`, `21-settings-app-account.png`
- A single **Settings** page (sidebar footer item). A tab row of anchor links:
  - *Profile · Goals · Meal times · Water · Reminders* (all on one scrolling page, `id` anchors);
  - *App & account* (a second route, e.g. `/settings/app`).
- **Profile:**
  - a read-only definition list (height, weight + date, target weight, birth date, sex, activity) with *Edit*;
  - an **Energy model** panel: BMR, TDEE, BMI now → target, and weekly change at the target intake, plus one sentence ("About 8 weeks to reach 79 kg"). Empty state per B3;
  - a weight sparkline with the target line, "−4.1 kg since 1 Aug" and *Log weight*.
- **Goals:** five rows (colour dot, name, value, kind badge *Limit* / *At least*) with the explainer "Limits turn red when you go over; minimums turn green when you reach them."
- **Meal times:** labelled `type=time` inputs per slot, plus *Add meal*.
- **Water:** a track-water switch, daily goal (L) and glass size (ml).
- **Reminders:** switches (`role="switch"`, `aria-checked`) with full sentences, for example:
  - "15 min before each planned meal";
  - "Every 90 min between 08:00 and 20:00, until you reach your goal";
  - "Sunday at 19:00 — intake, protein and weight for the week".
- **App & account:**
  - **Where notifications go:** In the app (switch); Email (*Coming soon* badge, disabled). This replaces the Channel preferences page.
  - **Appearance:** theme cards Light / Dark / Match device, each with a mini preview; Language select; *Compact layout* switch.
  - **Units and numbers:** Energy, Weight, Liquids and Week starts on, plus a thousands-separator radio ("2 150" / "2,150") with a live example line. Remove B2.
  - **Account:** avatar, name and email, *Sign out*; then a separate *Delete account* row with an explanation and an outlined red button.
- Redirect `/diet-planner/profile`, `/diet-planner/preferences` and `/notifications/preferences` to the new sections.

### 4.2 Household — `22-household.png`
- Title "Household". Subtitle: "Kantorowicz Home · 3 people share recipes, plans and the shopping list". Primary action: *Add person*.
- **People:**
  - 68 px rows: avatar with real initials (PK, AK; a single letter when there's no surname), name with "(you)", and email or "Managed by you · doesn't sign in";
  - a role select only where the current user may change it;
  - a ⋯ menu (Remove).
  - Footer line explaining roles (Owner / Adult / Child as defined by the backend).
- **Invitations:** an empty state sentence, or the list.
- **Household name:** input plus *Save*, disabled until the value changes.
- **Leave or delete:** a red-bordered section with two rows, each with an explanation.
  - When the user is the last owner, *Leave* is disabled with the reason inline. **Show this message once** — today it appears twice.

### 4.3 Notifications — `23-notifications.png`
- Title, plus a subtitle with the unread count. Actions: *Mark all as read* and a settings icon linking to Settings › App & account.
- Filter chips: *All · Unread n*.
- Groups: *Today*, *Earlier*.
- Each item is one `<button>`:
  - a 40 px type icon tile (meal / people / water / chart);
  - the title, bold when unread;
  - the body;
  - an action link ("Open today", "View household", "Log water", "See nutrition");
  - the time; plus an unread dot with `aria-label="Unread"`.
- **Read items: normal weight, no strikethrough, no greyed-out text.** Remove the checkbox multi-select.
- Empty state: "You're all caught up."

---

## 5. Budget + Admin (Phase 5)

Currency is the household's (PLN). The page subtitle says "amounts in PLN"; rows show bare amounts.
Add a **`MoneyText`** component that wraps `formatMoney` with `tabular-nums`.

### 5.1 Overview — `30-budget-overview.png`, phone: `37-budget-overview-phone.png`
- H1 "Budget". Subtitle: "Kantorowicz Home · amounts in PLN". Primary action: *Add expense*.
- Controls: a month stepper (‹ October 2026 ›) and a segmented *Shared / Just mine*. The latter maps to `scope=shared|mine`.
- **Summary surface:**
  - Spent this month (44px), with "Across 3 shared envelopes · 6 expenses".
  - Envelopes with a limit: total spent / total limit, with a status.
  - A **Settle up card** ("You owe Ania 505.73 · Record a payment ›") linking to Settle up. Hide it when settled, for children, or in the *Just mine* scope.
- **Envelopes:** each row has the name, `spent / limit`, and a bar: primary fill, or red fill plus a limit tick when over.
  - Status: "52.75 left" or "↑ 200.00 over the limit".
  - A text-button *Change limit* / *Set limit*, which opens the existing limit dialog.
- **By category:** horizontal bars sorted by amount (single violet hue), with right-aligned amounts.
- **Latest:** 4–5 compact rows, using the same component as Expenses. *All expenses* link.

### 5.2 Expenses — `31-budget-expenses.png`, phone: `38-budget-expenses-phone.png`
- H1 "Expenses" (not "Budget").
- Controls: the same month stepper as Overview, search, Envelope select, Category select. Replace the From/To native date inputs.
- A totals line for the current filter: "6 expenses · 4 457.25 total · your share of split costs 878.63". Plus a *Show voided* checkbox.
- **Grouped by day** ("Today · Sat 3 Oct", "Yesterday · Fri 2 Oct"), with a day total. Each row is 64 px and links to detail:
  - a category icon tile (cart / home / zap / car / sun …);
  - **description** (falls back to the category name), see § 7;
  - meta: "Groceries · Everyday · Ania paid · split with you" or "· household account";
  - the amount, with a sub-line "your share 92.18" or "not split".
- **"Recorded by" is removed from rows** (it's on the detail page).

### 5.3 Add / Correct expense — dialog `32-budget-add-expense-dialog.png`, phone `33-budget-add-expense-phone.png`
- Field order:
  1. **Amount:** large (36–40px), the currency inside the field, autofocus, `inputmode="decimal"`, accepts "," or ".".
  2. **What for (optional):** the new description field, see § 7.
  3. **Category:** chips, one tap (8 categories).
  4. **Envelope** (select, labelled "Everyday · shared" / "Personal · only you") and **Date** (button, "Today, 3 Oct").
  5. **Who paid?** Cards for each adult plus *Household account*. This maps to `fundingSource` (`Individual` + `paidByPersonId`) or `HouseholdFunds`, replacing the abstract "Funded by" select.
  6. **Split equally between:** checkbox cards showing each person's share **live**, with cent remainders going to the first person (match the backend rule). Hidden for the household account, with the note "Paid from the household account — nobody is owed anything".
- **Footer:** "Recorded by you" on the left; *Cancel* and *Save 184.36* on the right (the amount goes in the button).
- **Correct** mode: same form plus the required **Reason** field (existing validation).
- Keep the existing duplicate-check flow (keep both / save without checking) as an inline panel above the footer.
- **Phone:** a full-screen sheet; header with Close and title; scrollable body; **sticky footer** with a full-width Save (fixes B9).

### 5.4 Expense detail — `34-budget-expense-detail.png`
- Breadcrumb: Expenses › {description}.
- Header:
  - meta "Groceries · Everyday · Fri 2 Oct";
  - H1 = description;
  - 40px amount plus currency.
  - Actions: *Correct* (secondary). **Void moves to a separate red-bordered row at the bottom**, with its explanation and the existing confirm dialog.
- **Who pays what:** a sentence ("Ania paid. Split equally — you owe her 92.18 for this."), a stacked split bar, and per-person rows.
- **Details:** envelope, category, purchase date, recorded by.
- **History:** a timeline, newest first.
  - Each revision: "Corrected by Ania · 2 Oct, 21:12" (local time), the reason in quotes, and **only the changed fields**: "Amount ~~176.36~~ → **184.36**", "Each share 88.18 → 92.18".
  - The first revision: "Added by you · …" with a one-line summary.
  - Compute the diffs client-side from consecutive `snapshot`s.

### 5.5 Settle up — `35-budget-settle-up.png`, phone: `39-budget-settle-up-phone.png`
- H1 "Settle up". Subtitle: "Shared costs one person paid for, between the adults · PLN". Remove the "Outstanding balance — all recorded entries" title.
- **Answer card first:**
  - avatars with an arrow;
  - "You owe Ania" and a 36px amount;
  - one primary **"I've paid Ania 505.73"**, which records a repayment prefilled from the suggestion. The existing review/confirm step can stay as a small confirm;
  - a text link *Record a different amount*, which opens the existing form;
  - a note: "Recording a payment only updates the balance — no money moves from here."
  - With 3+ people, show one card per suggestion.
- **Settled state:** a ✓ card "You're all settled up" with *Undo* (= void the repayment just recorded).
- **Balances:** a diverging bar around zero per person, labelled "owes 505.73" / "is owed 505.73" / "Settled". Neutral colours; no red/green.
- **Payments recorded:** the latest 3, each with a ⋯ menu (Void), plus *Show all*. No pagination controls for small lists.
- **How this is worked out:** a `<details>` containing the existing explanatory bullets.

### 5.6 Envelopes — `36-budget-envelopes.png`
- H1 "Envelopes". Subtitle: "Buckets for spending, like Everyday or Holidays — not bank accounts". Primary action: *New envelope*.
- Sections:
  - **Shared · n** ("Owners and adults can see these");
  - **Personal · n** ("🔒 Only you can see these"; use a lock icon).
- Rows: icon tile, name, "n expenses in October", monthly limit ("3 000.00 / month" or "No limit"), and a ⋯ menu (Rename, Set limit, Archive).
- **Archived · n** sits in a collapsed `<details>` with *Restore*.

### 5.7 Admin › Failed messages — `40-admin-failed-messages.png`
- H1 "Failed messages". Subtitle: "Messages that ran out of retries. Fix the cause, then send them again. Admins only."
- Summary: four **neutral** label/value pairs. A value turns red only when it's > 0.
- One surface per source (*Notification deliveries*, then one per outbox module with its **display name**, e.g. "Budget events"), with a count badge and a *Retry all* button in the section header (icon + gap).
- Rows:
  - name plus sub-line (type / channel / recipient);
  - local time (`2 Oct, 20:40`);
  - "5 tries";
  - the error on **one mono line**, truncated, with the full text in `title` and in the View panel;
  - *View* and *Retry*.
- Empty section: "✓ Nothing waiting."

---

## 6. Component inventory (build or extend; add each to the Control kit page)

| Component | Notes |
|---|---|
| `AppSidebar`, `ModuleSwitcher`, `MobileTabBar`, `MoreSheet` | Driven by the module registry. Badges via existing hooks (unread notifications, admin backlog, open settlements). |
| `PageHeader` | breadcrumb?, title, subtitle, actions. |
| `Surface` / `Section` | `bg-card border rounded-lg p-6`; optional header row with title plus link/action. |
| `Meter` | Bar with optional target tick, `over` styling, and `aria-valuenow` / `valuemax` (or a text equivalent). |
| `StatusText` | Takes `goalStatus` output and renders the icon plus the word in the right token colour. |
| `BarChartDays` | Today / Nutrition / Water: values on bars, dashed labelled target, day + date labels, highlighted today. |
| `StackedMacroBar` | Protein/carbs/fat by % of kcal, with an `aria-label`. |
| `CheckToggle` | 44 px round check button (`aria-pressed`), for meals. |
| `FilterChips` | One chip-group component (replaces chip + segmented mixes). |
| `SegmentedControl` | Existing — use for Day/Week, Shared/Just mine, 7/30/90 days. |
| `MoneyText`, `KcalText` | Thin wrappers around `format.ts`. |
| `DangerRow` | Title, explanation, outlined red button (Void, Delete, Leave). |
| `EmptyState` | Icon, sentence, optional action. |

---

## 7. Backend changes (separate tasks; the UI must degrade without them)

1. **Expense description** — an optional `description` (≤ 80 chars) on Budget expenses: create, update/correct, list, detail, search, and the revision snapshot. The UI falls back to the category name while it's missing.
2. **Shopping list check-offs (shared)** — persist bought state per household + product + date range. For example, `PUT /api/v1/meals/shopping-list/checks` with `{ from, to, productId, checked }`, returning `checked` per item from `GET /shopping-list`. The UI falls back to local state.
3. **Optional:** return the list of open suggestions to drive the Settle up nav badge (or compute it client-side from `/api/budget/settlement`).

---

## 8. Delivery plan (one PR each, in order)

| Phase | Scope | Done when |
|---|---|---|
| 1 Foundations | tokens.css, type and radius codemods, `format.ts` adopted app-wide, contrast, plurals, bugs B1–B6 | No `text-[Npx]` or `rounded-Npx` left; the formatting table in § 1.4 holds on every screen; tests green |
| 2 App shell | Sidebar + switcher, footer group, PageHeader, content width, mobile bottom bar + More, renames/redirects, Meal plan tabs removed | One nav layer at 1440 and 390 px; all old routes still resolve |
| 3 Diet planner | § 3.1–3.9 | Screens match `screens/1x-*.png` in light and dark |
| 4 Settings / Household / Notifications | § 4 | Old Profile / Preferences / Channel-preferences routes redirect |
| 5 Budget + Admin | § 5 (description and check-offs behind feature flags until § 7 lands) | Screens match `screens/3x-*.png`, `40-*.png`; Add expense usable one-handed at 390×844 |

### Definition of done (every phase)
- Typecheck, lint and tests pass. New logic (goal status, split shares, history diff, formatting) has unit tests.
- Checked at 1440, 1024 and 390 px wide, in light and dark.
- Keyboard: everything reachable by Tab, visible focus ring (`--color-ring`), Esc closes dialogs and menus.
- en + pl strings added, with no raw keys or `{{…}}` visible.
- Control kit page updated.
