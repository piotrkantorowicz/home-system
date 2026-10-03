# HomeSystem redesign — handoff for Claude Code

Everything needed to implement the redesign in the React app. This folder is the design side of the handoff; the code still has to be written in your repo.

## How to use

1. Copy this folder into the frontend repo as `docs/redesign/`.
2. Open Claude Code in the frontend project root.
3. Paste the prompt from `PROMPT.md`.

Claude Code reads the spec, plans each phase, and waits for your OK before writing any code. You get one PR per phase.

## What's inside

| Path | What it is |
|---|---|
| `PROMPT.md` | The prompt to paste into Claude Code. |
| `SPEC.md` | The full spec: principles, foundations (tokens, type, radius, formatting, accessibility, bugs B1–B9), app shell, every screen, component inventory, backend changes and the 5-phase delivery plan. **This is the source of truth.** |
| `tokens.css` | Tailwind v4 `@theme`, `:root` and `.dark` token changes to merge into the global stylesheet. |
| `src/lib/format.ts` (+ `.test.ts`) | Number, unit, date and goal-status formatting with vitest tests (all pass). Drop it into `src/lib/`. |
| `screens/` | Target screenshots, numbered by area: `0x` foundations and shell, `1x` diet, `2x` settings / household / notifications, `3x` budget, `40` admin. |
| `current/` | Before screenshots of the current build, for comparison. |
| `reference/` | The mockup sources (`.dc.html`). They use inline styles, so read them for exact spacing, sizes and copy, but don't paste them in as code. |

## Delivery plan (summary — details in SPEC.md § 8)

1. **Foundations**: tokens, type/radius codemod, `format.ts`, accessibility fixes, bugs B1–B9.
2. **App shell**: single sidebar with module switcher, top bar, mobile bar of 5 or fewer items, route error boundary.
3. **Diet planner**: Today, Meal plan, Shopping, Nutrition, Water, Products, Recipes, Import.
4. **Settings, Household, Notifications.**
5. **Budget + Admin**: Overview, Expenses, Add expense (sticky Save on phones), Detail, Settle up, Envelopes, Failed messages.

## Backend items (separate tasks, SPEC.md § 7)

- Expense **description/note** field (the UI hides it until the API has it).
- **Shared shopping-list check-offs** across the household.
- Optional: an "unsettled" count for the Settle up nav badge.

## Notes

- **The data in the mockups is fake.** Names, amounts and recipes were invented for the mockups and must not be hardcoded.
- **The screenshots use a fallback font.** The design uses **Outfit**, which couldn't be loaded where the screenshots were rendered, so letter widths look slightly different from the real app.
- The interactive design canvas, with all boards, review notes and a clickable module switcher, is at https://claude.ai/code/artifact/fa85c74c-4c9d-448c-af17-a6f9c06d7ae7. It's private to you unless you share it.
