# Redesign v3 — decision record

Settled 2026-10-03. Referenced by #478 and the dependent tickets.

| # | Decision | Blocks |
|---|---|---|
| 1 | **High-protein recipe**: per serving protein >= 25 g **or** protein kcal share (protein g x 4 / kcal) >= 30%. Recipes with null protein are excluded from the filter, never treated as 0. | #486, #498 |
| 2 | **Shopping check identity**: per row, keyed by household + date range + product + unit. Same product in different units checks independently. | #488, #494 |
| 3 | **Nutrition goal tolerance**: `goalStatus` uses a 3% band around the goal. Monetary values stay exact, no tolerance. Single constant, easy to change. | #480 |
| 4 | **Budget naming**: sidebar item "Budget", page title "Overview". Used consistently in en/pl. | #483, #505 |
| 5 | **Unit preferences**: display helpers honour kcal/kJ, kg/lb, ml/L/oz from `usePreferences`; canonical backend units unchanged and never silently relabelled. Per-100ml display needs a density conversion, so default unit alone is not enough. | #480, #485, #497 |
| 6 | **Author name fallback**: recipe byline shown only when permitted identity resolution exists; otherwise omitted. No fake names. Recipe photo space omitted (no photo field, no upload in v3). | #498, #499 |

Default for anything unlisted: preserve current API semantics.

Account deletion remains a separate design decision (#513); no active destructive button until approved.
