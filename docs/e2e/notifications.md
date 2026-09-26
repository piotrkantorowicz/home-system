# notifications/ — Notifications module specs

Three specs live under `e2e/notifications/`, sharing the diet-planner auth
fixture (`notifications/fixtures.ts` re-exports it). `real-inbox.spec.ts` uses
the real backend and scheduler. The original two specs retain route mocks.

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

## real-inbox.spec.ts — Real preferences and inbox

`real preferences and meal-missed inbox support single and bulk read`:

- Toggles **Real-time**, waits for the real preference PUT, reloads, and verifies the saved state.
- Enables WebSocket delivery and creates a uniquely named meal slot, product, recipe, and three meals planned two hours ago in UTC.
- Enables meal reminders with a one-minute missed grace period. Polls the real inbox API for three `MealMissed` notifications from the scheduler, outbox, and notification handler. No route mocks or test-support seed endpoint.
- Opens the inbox, verifies three unread rows and the badge total, reads one, and verifies the reduced count and read state after reload.
- Selects the remaining two rows, bulk marks them read, then reloads to verify all three read states and the count reduced by three.
- Keeps existing notifications in the count baseline. Restores channel preferences, reminder settings, and the previous meal schedule; deletes created meals, recipe, and product. Notifications have no delete endpoint, so created rows remain read. Existing global teardown purges DietPlanner worker data.

The scheduler must be enabled (`DietPlanner:DietReminderTick:Enabled=true`, default).
Production tick interval defaults to 60 seconds; the journey allows 90 seconds
for notification creation and 150 seconds overall. The seed adds one slot to
the worker's schedule, which must have fewer than eight slots. Selectors live
in `notifications/pages/notifications.page.ts` and use accessible roles.

## Gaps

- Pagination / infinite scroll in the inbox
- WebSocket live-push updating an already-open inbox (the real journey opens the inbox after delivery)
- Email delivery (#164) and per-type preferences (#166)
