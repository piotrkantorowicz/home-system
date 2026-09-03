# Redesign — project overrides

The design handoff in this folder (`README.md`, `IMPLEMENTATION_PLAN.md`,
`HomeSystem-Redesign.dc.html`, `screenshots/`) is the visual source of truth for the
UI refresh tracked in **piotrkantorowicz/home-system#208**.

A few points where this project deliberately diverges from the handoff:

## Single language, not bilingual

The handoff shows English + Polish side by side everywhere ("bilingual rule", stacked
nav labels, `EN · PL` sub-lines). **We do not do this.** The app has a language
switcher; every label renders once via `t()` in the active language. Ignore
IMPLEMENTATION_PLAN item 40 and the bilingual instructions in README §"Layout shell".

## Routing unchanged

The handoff assumes diet-planner routes live at the root (`/`, `/calendar`, `/profile`,
`/import`, …). This is a modular app: those routes stay under `/diet-planner/*`, and
`/` remains the system dashboard. Nav is built from the module registry. Screen
rebuilds are presentation-only — no route restructuring.

## No new dependencies without discussion

Drag-to-move on the meal plan (plan item 21) needs a DnD library the app doesn't
bundle — deferred pending a decision on adding one.

## Data gaps

Some handoff figures have no backing data yet (e.g. "Burned" on the Today hero, a
start-weight baseline for the identity goal bar). Those tiles are dropped or
approximated rather than faked.
