# E2E coverage plan

Plan for gaps in the current UI, sized as independent one-PR slices. Each slice
adds a user journey to an existing spec or one new spec, updates its page object
and per-area doc, and runs focused Playwright plus `npx tsc --noEmit` in `e2e/`.
Use real APIs for successful flows; route-mock only failures or states that
cannot be produced deterministically. Seed through existing API helpers and
assert persisted results after reload. Run the full suite before each PR.

| Order | Proposed PR title | Journey and acceptance | Likely spec | Priority |
|---|---|---|---|---|
| 1 | `test(e2e): cover hydration logging and history` | Set a target, add one glass and a custom amount, reload, verify total/history, remove one entry, verify total falls. Check dashboard water card reflects same total. | `hydration.spec.ts` | High |
| 2 | `test(e2e): cover calendar day and meal status flows` | Import a known plan; switch week ↔ day while preserving date; mark a meal done, record an actual meal, revert it, then bulk-complete remaining planned meals. Verify statuses survive reload and nutrition reflects actual intake. Existing `redesign-v2.spec.ts` already covers dashboard mark/undo. | `meals.spec.ts` or a new status spec | High |
| 3 | `test(e2e): cover import file and recovery paths` | Upload a `.json` file, review detected rows, go back without importing, then import. Separately submit invalid data and verify row errors keep Import disabled; cover warnings only with a deterministic payload accepted by current validation. | `import.spec.ts` | High |
| 4 | `test(e2e): cover product and recipe editing details` | Save product with non-default unit and fiber; create recipe with two ingredients, instructions, and mixed supported units; edit ingredient list and servings; reload and verify ingredients and per-serving nutrition. `detail-improvements.spec.ts` already checks the scaling control. | `products.spec.ts`, `recipes.spec.ts` | Medium |
| 5 | `test(e2e): cover real notification preferences and inbox` | Save WebSocket channel preference against real API and reload. Produce one notification through a deterministic supported trigger; verify unread badge, mark it read, create multiple, bulk mark read, and verify inbox/count after reload. First confirm an existing trigger can produce notifications reliably; if none can, scope a minimal test-support seed through existing [#125](https://github.com/piotrkantorowicz/home-system/issues/125). | `notifications/` specs | Medium |
| 6 | `test(e2e): cover list pagination and search state` | Seed enough products and recipes for two pages; verify Next/Previous changes visible rows and bounds disable correctly. Search, clear, and reload; assert only URL state the current lists actually persist. Avoid duplicating selector-only checks in `pagination.spec.ts`. | `pagination.spec.ts` | Medium |
| 7 | `test(e2e): cover profile and schedule validation` | Submit out-of-range body stats and empty meal-slot name/time; verify field errors and no save. Save corrected values and verify after reload. Route-mock one profile save failure to verify error feedback without changing backend data. | `profile.spec.ts`, `meal-schedule.spec.ts` | Medium |
| 8 | `test(e2e): cover nutrition totals for known meals` | Seed a small known plan and goal; complete meals; assert daily and average calories plus macro totals in summary and table. Check chart's accessible labels, not pixel heights or CSS. | `nutrition.spec.ts` | Medium |
| 9 | `test(e2e): cover keyboard and Polish navigation` | Navigate module switcher and command palette using keyboard, verify focus/Escape behavior, switch language to Polish, then verify one route and one settings action remain usable after reload. Existing `navigation.spec.ts` covers pointer navigation. | `navigation.spec.ts`, `theme.spec.ts` | Low |

Slices 1–4 have no new infrastructure dependency. Slice 5 depends on a stable
notification trigger. Keep the reserved invitee and worker households out of
new test cleanup; the existing teardown purges DietPlanner rows only.

## Not E2E gaps in current code

- No UI currently offers calendar drag-to-move, meal-slot drag/reorder or reset,
  CSV export, profile delete/export, product bulk operations, recipe dietary
  tags/categories, or the removed interactive weight calculator. Add E2E tests
  when those features ship, not before.
- Root redirect's zero/one-module branches already have unit tests. The running
  app registers three modules; E2E covers last-visited redirect.
- Formula boundaries, BMI category thresholds, validation schema edge cases,
  and chart geometry fit unit/component tests. E2E checks their visible result
  within one representative journey.
- Authentication lockout/password reset need stable Authentik provisioning.
  Visual baselines and extra browser projects are separate infrastructure work;
  [#124](https://github.com/piotrkantorowicz/home-system/issues/124) already tracks browsers.
- Multi-user role coverage belongs with existing
  [#126](https://github.com/piotrkantorowicz/home-system/issues/126) and related
  household feature work.

Tracking issues: [#393](https://github.com/piotrkantorowicz/home-system/issues/393),
[#394](https://github.com/piotrkantorowicz/home-system/issues/394),
[#395](https://github.com/piotrkantorowicz/home-system/issues/395),
[#396](https://github.com/piotrkantorowicz/home-system/issues/396),
[#397](https://github.com/piotrkantorowicz/home-system/issues/397),
[#398](https://github.com/piotrkantorowicz/home-system/issues/398),
[#399](https://github.com/piotrkantorowicz/home-system/issues/399),
[#400](https://github.com/piotrkantorowicz/home-system/issues/400), and
[#401](https://github.com/piotrkantorowicz/home-system/issues/401).
Check open issue scope before each slice; existing #125 and #126 may absorb
prerequisites.
