# V2 implementation delivery

Implemented in the React application and rebuilt `src/ui/dist` on 7 September 2026.

## Delivered

- Today uses a compact nutrition summary, prioritizes upcoming meals, and collapses logged meals. Existing add-meal and goals flows remain available.
- Consumed nutrition sums Done/Modified meal DTOs, whose backend-calculated quantities already include servings and overrides. Planned meals do not count as eaten. The existing summary API retains its semantics for other consumers.
- Dashboard distinguishes loading and failed queries from missing data. Nutrition-summary request errors now propagate through the query hook.
- Calendar defaults to its existing day agenda on narrow screens unless the URL explicitly selects another view. The seven-day selector preserves date/history, and placeholder meals are hidden while the selected day's request resolves.
- Day-summary water uses the selected date. Day-summary calorie/macronutrient totals represent consumed meals.
- Water mutations share per-amount duplicate protection and per-entry deletion state. Explicit entry removal replaces ambiguous filled-glass deletion on the affected screens. Failed removal retains confirmation controls; add Undo deletes the exact returned entry ID and reports failures.
- Meal-completion Undo uses the existing reset endpoint for a recently completed Planned meal, with local-state checks. Completed/reset query refreshes finish before mutation success settles.
- Weekly context uses consumed records, real chart geometry, accessible daily values, and empty baselines. Today is labeled as partial.
- English/Polish strings were added, including missing calendar day/week labels.
- Goal creation now accepts the backend's successful empty `201 Created` response, then refreshes goals before closing setup.

## Verification

- Production build passed (TypeScript plus Vite); rebuilt output is in `src/ui/dist`.
- Full frontend lint passed; targeted lint of changed code also passed.
- Diet Planner Vitest run: **37 files, 143 tests passed**.
- Existing dashboard/hydration/meal E2E selection: **17 scenarios plus 4 authentication setup tests passed** during implementation.
- Final dedicated v2 E2E run: **1 scenario plus 4 authentication setup tests passed**, against real Authentik/backend. It exercises goal setup, planned/consumed separation, completion/Undo, narrow-screen day selection, browser Back, and no main-content horizontal overflow at 390/768/1280/1600px in both themes.
- Final desktop-light, mobile-dark, and mobile-calendar captures were visually reviewed. Captures use seeded test records, not household data. Test teardown purged only the suite's worker data.

Screenshots: [Today desktop](implemented/today-desktop.png), [Today mobile dark](implemented/today-mobile-dark.png), [mobile calendar](implemented/calendar-mobile.png).

## Product, recipe, and Today follow-up

- Product details show a single nutrition definition list, a short `kcal / 100 g` unit, and a compact conversions card. Missing product nutrition remains unknown (`—`) instead of becoming zero.
- Recipe details no longer show an empty decorative banner or an ingredient calorie column without data. Ingredients and total calories scale with the selected servings; per-serving nutrition stays fixed. Macro labels and serving controls use translations.
- Add to plan opens the existing meal form with the recipe and selected servings, including shared recipes. Successful creation opens the chosen calendar day; failed creation keeps the form available for retry.
- The meal form reports schedule loading/failure and links to schedule setup when no meal slots exist.
- Today opens today's Day view, selects its date, and focuses the date heading. Browser Back restores the previous week/date.

Follow-up verification: production build and focused lint passed; 16 focused Vitest tests passed across five files. The nine existing product/recipe CRUD browser scenarios passed. The final dedicated flow passed (one scenario plus four authentication setup tests), covering recipe scaling, creation from the prefilled meal form, Today focus/selection, Back navigation, and 390/1280px layouts in both themes. Earlier test runs exposed missing schedule setup and test-selector timing problems; the final dedicated run passed without retries. Worker-data cleanup succeeded.

Reviewed previews: [product desktop](implemented/product-desktop.png), [recipe desktop](implemented/recipe-desktop.png), [product mobile dark](implemented/product-mobile-dark.png), [recipe mobile dark](implemented/recipe-mobile-dark.png).

## Boundaries

Existing module navigation remains intact; the optional mobile “More” navigation redesign was not included. Exact Undo after deleting a historical water entry remains deferred because the current API cannot restore its timestamp/identity. Meal Undo cannot guarantee cross-client conflict protection without a conditional backend operation.

No global palette rewrite, backend schema/API change, or new dependency was required. Existing Vite warnings about large chunks and dependency annotations remain. Browser verification covers Chromium and English sample screens; full translated accessibility/browser certification is not claimed.
