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
`/import`, …). This is a modular app: those routes stay under `/diet-planner/*`. Nav is
built from the module registry. Screen rebuilds are presentation-only — no route
restructuring. **Update (BUILD_REVIEW.md round 2):** `/` itself no longer renders a
page — it redirects into a module per `#fix-home` (see below).

## Round 2 (BUILD_REVIEW.md) — a few narrower calls

- **No group sub-labels, no "Household" placeholder module.** The handoff's module
  switcher mock shows a second, not-yet-built "Household" module for illustration.
  We only have two real modules (diet-planner, notifications) — the switcher lists
  exactly what's registered, nothing speculative. Same reasoning drops the dashed
  "+ add module" rail tile: there's no module marketplace to open it onto.
- **Command palette is hand-rolled**, not a `cmdk`-style library — matches "no new
  dependencies without discussion". It's a filterable list of every registered
  module's `navItems`, opened by ⌘K, the header trigger, or the switcher's
  "Search everything" row.
- **No nav-item counts/badges** beyond the existing notifications unread dot. The
  handoff mocks a Products "412" and a Shopping-list "8" pill; we don't have cheap
  queries backing those numbers everywhere yet, so we don't fake them (same rule as
  "Data gaps" below). `Badge` stays wired for when a module has a real one.

## No new dependencies without discussion

Drag-to-move on the meal plan (plan item 21) needs a DnD library the app doesn't
bundle — deferred pending a decision on adding one.

## Data gaps

Some handoff figures have no backing data yet (e.g. "Burned" on the Today hero, a
start-weight baseline for the identity goal bar). Those tiles are dropped or
approximated rather than faked.
