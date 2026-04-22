# theme.spec.ts — Theme Toggle

**Purpose**: the theme toggle (now inside the user-menu dropdown — #108) cycles through modes and persists across reloads.

**Setup**: each test starts on `/` and opens the user-menu dropdown once before clicking the toggle. The toggle is rendered as raw content inside the dropdown (not a `DropdownMenuItem`), so the dropdown stays open across clicks — the test exploits this to interact with the toggle repeatedly without re-opening the menu.

**No POM** — both tests are short enough that the spec drives the page directly.

## Tests

### `clicking the theme toggle cycles through light and dark modes`

- **Given** the user is on `/` and the user-menu dropdown is open
- **When** the user clicks `getByTestId('theme-toggle')` once, then again
- **Then** after each click the `<html>` element matches `class=/light|dark/`
- **Selectors**:
  - Trigger: `getByRole('button', { name: /user menu/i })` (`aria-label` from `common.user_menu` i18n key)
  - Toggle: `getByTestId('theme-toggle')` rendered by the `ThemeToggle` component
- **Notes**: the assertion is permissive (`/light|dark/`) because the toggle cycles through three states (light → dark → system); the test only proves the class flips, not the specific mode order.

### `selected theme persists after a page reload`

- **Given** the user is on `/` and the user-menu dropdown is open
- **When** the user clicks the theme toggle up to 4 times until the `<html>` class includes `dark`, then reloads the page
- **Then** after the reload the `<html>` element still matches `class=/dark/`
- **Notes**: the loop bound (4 attempts) is the shortest cycle through all theme modes back to dark; if no dark class is present after 4 clicks, the assertion fails on the explicit `expect(...).toHaveClass(/dark/)` check before reload. Persistence relies on the `theme` value stored in `localStorage`.

## Acceptance

The theme toggle works and persists user preference across reloads.

## Gaps

- System / auto theme mode (the toggle cycles light → dark → system per UI but the test only asserts on `light|dark`)
- Per-component dark-mode rendering (e.g., charts, modals, dropdowns)
- Language switcher (which lives next to the theme toggle in the same dropdown but isn't tested)
- Keyboard navigation through the dropdown (Tab / Arrow / Escape)
