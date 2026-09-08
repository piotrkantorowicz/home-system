# Redesign v2 — focused design exploration

Open [index.html](index.html) directly in a browser. No server, install, CDN, or API is required. Keep `styles.css` and `prototype.js` beside it.

This is a proposal for **Today and Meal plan**, not a replacement specification for every route or an implementation in `src/ui`. All data is illustrative; changes reset on reload. English is the chosen preview language. Production must continue using the existing language switcher and `t()`.

## How the original was made

`HomeSystem-Redesign.dc.html` is a design-canvas document: `<x-dc>` contains the screens, inline styles define most geometry, and a `text/x-dc` script exposes theme state through `DCLogic`. `support.js` is a generated renderer, not application source. It loads React 18.3.1, React DOM, and Babel from unpkg; Outfit comes from Google Fonts. The original therefore depends on network resources when opened standalone.

The original is a broad visual reference, with a small theme interaction, rather than a functioning application. Its mobile page is a separately drawn 390px phone frame. That demonstrates intent but does not prove that the desktop screen reflows. Existing screenshots are useful historical snapshots, but some show older search and bilingual treatments.

The handoff already says to recreate it using application components rather than copy its HTML. That remains the correct approach. The checked-in `src/ui/package.json` currently lists `react-router-dom`; repository guidance also mentions TanStack Router, so confirm the actual router implementation when integrating rather than using the design document to choose a router.

## Review and priorities

| Priority | Finding in the reference | Recommendation |
| --- | --- | --- |
| High | README and implementation plan describe 76px navigation and bilingual labels; BUILD_REVIEW replaces the shell; OVERRIDES rejects bilingual labels and speculative modules. | Publish a single consolidated handoff before production work. For now, apply project overrides over round-two review over original specification. V2 is a proposal, not a new override. |
| High | Small navigation labels (8.5–9.5px) and pale secondary text make dense screens harder to scan. The supplied `01-light.png` also shows macro-label crowding. | Use readable single-language labels, stronger secondary text, and separate label/value rows. Validate final production contrast and text zoom. |
| High | A large calorie ring competes with the next meal, while logged meals can consume the first screen. | Use a compact nutrition strip, feature the next unlogged meal, and collapse completed meals. V2 demonstrates this. |
| High | Fixed-width calendar and separate mobile artboard leave real narrow-screen behavior underspecified. | Use a compact seven-day selector plus a readable daily agenda on narrow screens. V2 keeps this pattern on desktop for direct comparison. Retain a desktop week grid if cross-day comparison is the dominant workflow. |
| Medium | Round-two water proposal makes filled glasses delete the newest entry of a matching size. Equal volumes can represent different entries. | Delete by explicit entry identity; expose Remove in the entry list and Undo after mutation. Keep the progress visual informational. |
| Medium | Original Today reference includes unsupported “Burned” data; overrides already identify this gap. | Preserve the override: show logged intake and configured target only. Keep planned and logged totals separate. |
| Medium | Future chart bars use a fixed-height stub, visually suggesting intake where no data exists. | Render empty baselines with explanatory text and accessible values. Distinguish a partial current day from completed days. |
| Medium | Many related radii, small type roles, nested cards, and shadows add implementation and visual complexity. | Reduce the set to three surface/control radii; use dividers for meal rows and reserve the strongest tint for the next action. |

Keep the violet identity, semantic tokens, existing shared controls, registered module navigation, real data hooks, and loading/empty/error handling described in the original handoff. Those are foundations worth preserving.

## V2 visual direction

Palette: mist background `#f6f5f9`, white surface `#ffffff`, ink `#282431`, muted text `#686171`, violet `#6836cc`, water `#176c86`. Separate dark tokens live in `styles.css`. Nutrient colors encode named metrics rather than categories inferred from meals.

Type: retain Outfit in production. The offline prototype uses Outfit if locally available, followed by Avenir and system sans-serif fallbacks; no font download is required. Body text is 15px; supporting information is predominantly 13px. Numerals use clear size hierarchy and tabular formatting where quantities change.

Layout: left-aligned content, a compact nutrition strip, then a wide meal agenda beside a narrower water panel. The next meal carries the strongest tint. On mobile, everything becomes one column, with completed meals collapsed.

```text
Desktop                         Mobile
Navigation | Heading            Heading
           | Nutrition          Nutrition
           | Next meal | Water  Next meal
           | Upcoming  | Week   Upcoming
           | Logged ▸           Logged ▸
                                Water / Week
```

The prototype intentionally exposes only its two implemented pages. Its simplified sidebar is **not** a proposal to delete other registered destinations or the module switcher. Production shell scope remains governed by `OVERRIDES.md` and the registry.

## Working interactions

- Switch Today / Meal plan and select a weekday. Thursday has the sample plan; other days explicitly explain the demo scope.
- Mark a meal eaten: update calories, macros, meal count, chart, and planned/logged status. Undo restores the previous meal state.
- Add 250 or 500 ml, enter a custom integer amount, remove a specific entry, and undo the most recent action.
- Switch light/dark themes. Open the custom-water dialog with keyboard support, native validation, and Escape dismissal.

Undo applies only to the most recent mutation and remains visible until dismissed or replaced. This is in-memory interaction design, not a server-mutation strategy. Production should retain stable entry IDs, pending/error states, and query reconciliation; do not copy these demo arrays or DOM rendering functions into React.

## Verification

Headless Chromium checks passed at 390, 768, 1280, and 1600px in both themes: no document-level horizontal overflow. Meal logging/undo, water quick-add/custom-add/removal/undo, weekday selection, and empty-day copy passed. No page errors occurred during the checks. Desktop light and mobile dark screenshots were visually reviewed, then logged meals were moved below upcoming meals to improve action visibility.

These checks cover this prototype only, not backend integration, full accessibility conformance, browser compatibility, translations, or all original routes. The preview deliberately does not simulate loading and network failures. Validate those in the existing application during integration.

Screenshots: [desktop light](1280-light.png), [mobile dark](390-dark.png), [mobile meal plan](plan-mobile.png). Additional captures cover all four widths in both themes.

## Suggested integration sequence

1. Consolidate the source-of-truth documents and verify current shell/data capabilities.
2. Apply Today composition changes using existing nutrition, meal, and hydration components and queries. Preserve `/diet-planner/*` routes and registry navigation.
3. Add narrow-screen meal-plan agenda behavior using existing date selection and meal detail flows; do not add a DnD dependency for this change.
4. Verify real mutations, failed requests, undo semantics, empty data, over-target data, both languages, keyboard flow, and text zoom. Run the repository's relevant frontend and E2E checks.

The v2 HTML/CSS/JavaScript is an isolated reference artifact. Production implementation must follow the repository's React/TypeScript/Tailwind conventions, component boundaries, and existing token names.
