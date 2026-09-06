# notifications/ — Notifications module specs

Two specs live under `e2e/notifications/`, sharing the diet-planner auth
fixture (`notifications/fixtures.ts` re-exports it). Both are **route-mocked**
— they `page.route` the notifications API rather than seeding a real backend.

## channel-preferences.spec.ts — Channel preferences

Route: `/notifications/preferences`.

> **#208 redesign — the "console" channel row was dropped.** The page now
> renders only two `ChannelToggleRow`s: **Email** (disabled, "coming soon")
> and **Websocket** (the one interactive toggle). The old three-row layout
> (console enabled, email + websocket disabled) is gone. If a console channel
> returns, these tests need a third row again.

- **`renders the email (disabled) and websocket (enabled) channel rows`** —
  mocks `GET /api/notification-preferences`, asserts exactly 2
  `role="switch"`es: `nth(0)` (Email) disabled, `nth(1)` (Websocket) enabled.
- **`toggling websocket fires a PUT`** — clicks the websocket switch, polls
  until the mocked `PUT /api/notification-preferences` was hit.

## inbox.spec.ts — Inbox

Route: `/notifications`.

- **`shows the empty state when no notifications exist`** — mocks an empty
  list, asserts the `h1` "Notifications" (`{ name: 'Notifications', level: 1 }`
  — the empty-state's own "No notifications yet" is also a heading and matches
  a loose substring) and the empty-state text.
- **`clicking an unread row marks it read`** — a **stateful** list mock
  (returns `readAt` set once the `POST .../read` was called, so the mutation's
  `onSettled` refetch doesn't revert the optimistic update). The row button is
  located via its stable title text (`getByText('Time for lunch').locator('xpath=ancestor::button[1]')`)
  — `NotificationListItem` drops the button's `aria-label` once read, so a
  name-scoped role locator would stop matching the instant the click lands.

## Gaps

- Real backend (both specs are fully mocked)
- Pagination / infinite scroll in the inbox
- Bulk "mark all read"
- Websocket live-push updating the unread badge
- The `console` channel (removed from the UI)
