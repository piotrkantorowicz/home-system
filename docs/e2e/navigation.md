# navigation.spec.ts — App navigation

Two journeys use real authentication and module pages. `NavigationPage.goto()`
opens Diet Planner and waits for the Today heading.

## Tests

- **Desktop navigation switches modules and finds destinations:** collapse the
  Diet Planner section panel, reload to check persistence, switch to
  Notifications through the module rail, revisit `/` to check the last-module
  redirect, then use Ctrl+K to find Products across modules.
- **Mobile tabs and module switcher navigate between modules:** use the
  390px viewport, follow the Products bottom tab, then switch to the household
  module through the header menu. Its menu label is the seeded household name.
- **Keyboard alone opens and closes navigation controls and reaches a
  destination:** open and close the module switcher and the command palette
  with `Enter`/`Escape` only, then filter the palette and press `Enter` to
  navigate without ever clicking an option.
- **Polish language switches labels and stays usable after reload:** toggle
  the language switcher in the user menu, verify a section link's label
  switches to Polish, reload to check it persists, then follow the
  Preferences settings link while Polish is active.

## Gaps

- Root redirect with zero or one registered module (covered by unit tests).
- Other mobile layouts and viewport widths.
- Keyboard traversal of the section panel and mobile tabs specifically (only
  the module switcher and command palette are covered).
