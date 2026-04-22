# theme.spec.ts — Theme Toggle

**Purpose**: the theme toggle (now inside the user-menu dropdown — #108) cycles through modes and persists across reloads.

**Setup**: opens the user-menu dropdown once before clicking the toggle. The toggle is rendered as raw content inside the dropdown (not a `DropdownMenuItem`), so the dropdown stays open across clicks.

## Tests

- `clicking the theme toggle cycles through light and dark modes` — `<html>` class changes on each click
- `selected theme persists after a page reload` — cycle to dark, reload, assert dark class still present

## Acceptance

The theme toggle works and persists user preference.

## Gaps

- System / auto theme mode (the toggle cycles light → dark → system per UI)
- Per-component dark-mode rendering
- Language switcher (which lives next to the theme toggle but isn't tested)
