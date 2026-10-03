# Prompt for Claude Code

Copy this whole folder into the repo as `docs/redesign/` (or anywhere Claude Code can read), then paste the prompt below into Claude Code from the frontend project root.

---

```
We're implementing a UI redesign of HomeSystem (Diet planner, Budget, Household,
Notifications, Admin). The full spec, tokens and screenshots are in docs/redesign/.

Before writing code:
1. Read docs/redesign/SPEC.md end to end.
2. Look at the target screenshots in docs/redesign/screens/ (and the "before"
   screenshots in docs/redesign/current/) for every screen you touch.
3. Explore the codebase: the module manifests (basePath / navItems / routes),
   the app shell (icon rail, module sidebar, top bar, mobile bottom bar),
   the shared UI components (the Control kit page lists them), the Tailwind v4
   theme in the global CSS, i18n resources (en + pl), and the data hooks for
   each screen.
4. Reply with a short plan per phase (files you'll change, new components,
   anything in the spec that conflicts with the code) and wait for my OK.

Then implement phase by phase, as described in SPEC.md § "Delivery plan":
- One branch / PR per phase. Don't start the next phase until I approve.
- Keep existing API contracts and data hooks. Backend changes listed in
  SPEC.md § "Backend changes" are separate tasks — build the UI so it works
  without them (feature-flag or hide the dependent bits) unless I say otherwise.
- Implement with Tailwind classes and the existing components; extend those
  components rather than creating parallel ones. The .dc.html files in
  docs/redesign/reference/ are mockups with inline styles — use them for exact
  spacing, sizes and copy, not as code to paste.
- Sample data in the mockups (names, amounts, recipes) is fake. Never hardcode it.
- Every new or changed string goes into BOTH en and pl translation files,
  using i18next plural forms where a count is involved.
- Use docs/redesign/src/lib/format.ts as the starting point for number/date
  formatting and move the app onto it; keep its tests passing.
- Accessibility rules in SPEC.md § "Foundations" are requirements: real
  buttons/links, 44 px touch targets on mobile, 4.5:1 text contrast, status
  never conveyed by colour alone.
- After each phase: run typecheck, lint and tests, check light + dark mode
  and a 390 px wide viewport, update the Control kit page with any new
  components, and give me a short summary with before/after screenshots
  if you can take them.
```

---

Tip: if Claude Code's context is tight, point it at one section at a time, e.g. "Implement Phase 2 (App shell) from docs/redesign/SPEC.md".
