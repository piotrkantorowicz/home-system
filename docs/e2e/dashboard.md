# dashboard.spec.ts — Dashboard

**Purpose**: smoke that the diet-planner dashboard renders its three stat cards and links into the right pages.

**Setup**: none (uses whatever counts the backend returns).

## Tests

- `all three stat cards are visible on load` — Products / Recipes / Calendar cards render
- `stat cards show numeric counts after data loads` — counts resolve to a number
- `each stat card links to its respective page` — clicking each card navigates to the right URL

## Acceptance

The dashboard is reachable, the three module cards render, and their navigation contracts hold.

## Gaps

- Goals CTA card (when no goals configured)
- Inline goal progress card (when goals exist)
- WeightPredictionCard rendering on the dashboard (covered separately by [weight-prediction](weight-prediction.md))
- Welcome / empty-state messaging
