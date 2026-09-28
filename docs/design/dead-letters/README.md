# Dead letters — admin view and retry (#165)

Status: **implemented, awaiting review** — decisions needed at the end
Last updated: 2026-09-26

---

## 1. Problem

Two things can fail for good and nobody would notice:

- **Notification deliveries.** `RetryDeliveryWorker` retries a `Failed` delivery up to
  `Notifications:RetryDelivery:MaxAttempts` (5) with `attempt²` minute backoff, then silently
  stops. The row sits in `notification_deliveries` forever.
- **Outbox messages.** `OutboxWorker` retried a failed message **every tick (1 s), forever** —
  there was no cap. A poison message logged an error every second, and 50 of them (one batch)
  stopped every later message of that module from being dispatched at all.

There was no way to see either, and no way to push a row back once the cause was fixed.

## 2. What shipped

### Backend

| Piece | Where |
|---|---|
| Outbox attempt cap: `Messaging:Outbox:MaxAttempts` (default **10**). The worker skips rows at the cap — they are "dead-lettered". `OutboxWorkerOptions` is now actually bound from config (it was documented as bound but wasn't). | `Shared.Infrastructure.Messaging/Outbox` |
| `IOutboxDeadLetterStore` — list / count / requeue, implemented by `EfOutboxStore`, keyed per module like `IOutboxStore`. `AddOutbox<TDbContext>()` also registers an `OutboxModule(Name, Key)` so admin code can enumerate modules (`DietPlanner`, `Household`). | `Shared.Infrastructure.Messaging(.Ef)` |
| `AdminAuthorization` — `Admin` policy: token has `roles` claim with `admin` (accepts both raw `roles` and the mapped `ClaimTypes.Role`). | `Operations` module |
| Outbox admin endpoints (queries/commands through the dispatcher; see decision 6). | `Operations.Api/OutboxAdminEndpoints.cs` |
| `NotificationDelivery.Requeue()` (Failed only → `AttemptCount = 0`, reason kept), `RetryDeliveryCommand`, `ListDeadLetterDeliveriesQuery`, `GetDeliveryBacklogQuery`, admin endpoints. | Notifications module |

No schema changes: both tables already had `attempt_count`; the existing partial index
`ix_notification_deliveries_failed_retry` covers the delivery queries.

### API (all `RequireAuthorization("Admin")`, tag `Admin`)

| Method | Route | Result |
|---|---|---|
| GET | `/api/admin/notifications/deliveries/summary` | `{ deadLettered, retrying }` |
| GET | `/api/admin/notifications/deliveries/dead-letters?page&pageSize` | `PagedList<DeadLetterDeliveryDto>` (newest failure first) |
| POST | `/api/admin/notifications/deliveries/{id}/retry` | 204 · 404 unknown · 422 not `Failed` |
| GET | `/api/admin/outbox/summary` | `[{ module, deadLettered, retrying }]` |
| GET | `/api/admin/outbox/{module}/dead-letters?page&pageSize` | `PagedList<OutboxDeadLetter>` (oldest first, no payload) · 404 unknown module |
| POST | `/api/admin/outbox/{module}/dead-letters/{id}/retry` | 204 · 404 unknown module / message / already processed |

**Retry = requeue, not send-now.** It resets `attempt_count` to 0; the delivery is picked up by
`RetryDeliveryWorker` on its next tick (≤ 60 s), the outbox message by `OutboxWorker` (≤ 1 s).
The endpoint never runs the sender/handler inline, so it can't hang on a broken channel and
keeps one code path for sending.

### Who is an admin

Authentik blueprint adds a `roles` scope mapping (`home-system: roles`) attached to the
`home-system` provider:

```python
is_admin = request.user.is_superuser or ak_is_group_member(request.user, name="home-system-admins")
return {"roles": ["admin"] if is_admin else []}
```

plus an empty `home-system-admins` group. The SPA requests the `roles` scope. Verified on the
local Authentik: `akadmin → ["admin"]`, `E2eWorker0 → []`.

To make someone admin: add them to `home-system-admins` in Authentik. They need to log in again
(or wait for the next token refresh) to get the claim.

### UI

New `admin` module (`/admin`, "Dead letters"), hidden from the module rail, switcher and ⌘K
palette unless `roles` contains `admin` (`AppModule.requiredRole`). Hiding is cosmetic; the API
enforces access. The page shows:

- four counters — dead / retrying deliveries, dead / retrying events (the "dashboard counter";
  there is no system dashboard page, so the same total also shows as a **nav badge**);
- deliveries table (title, type · recipient, channel, attempts, last attempt, error, Retry);
- one events table per module that has dead letters (short event type, raised, attempts, error, Retry);
- toast on retry, lists and counters refresh.

A non-admin who opens `/admin` directly gets an "Admins only" empty state and no API calls.

### Why not a separate admin app or backend module

- **No separate SPA.** Security lives on the server (`Admin` policy on every `/api/admin/*`
  route), so a second app adds no protection. The admin page is lazy-loaded, so non-admins never
  download it. A second app would mean another Vite project, OIDC client, build/deploy and a
  copied shell (layout, theme, auth, i18n, API client) for a handful of pages. Revisit if admin
  needs a different exposure (VPN-only / own domain), a different IdP or MFA flow, or grows into
  a real back-office.
- **Frontend: already its own module** (`modules/admin`, registered like any other, gated by
  `requiredRole`).
- **Backend: no `Admin` module.** Each module owns the admin endpoints for its own data —
  generic outbox admin in shared messaging infra, delivery retry in Notifications (only it knows
  `NotificationDelivery.Requeue()`). A central admin module would have to reach into other
  modules' stores, breaking "no cross-module imports", or grow a Contracts pass-through per
  module. If one place to browse the routes is wanted, group them by OpenAPI tag (`Admin`, done).

## 3. Tests

| Suite | Added |
|---|---|
| `Notifications.UnitTests` | `Requeue` (failed / not failed), `RetryDeliveryCommandHandler` (requeue + commit, 404) |
| `Shared.Messaging.Tests` | worker passes `MaxAttempts` to the store |
| `Shared.Messaging.IntegrationTests` | dead row skipped/listed/counted, requeue, requeue of processed/unknown |
| `Household.IntegrationTests` | outbox admin: 403 non-admin (all routes), 401 anonymous, list → retry → gone, 404s |
| `Notifications.IntegrationTests` | new `NotificationsApiFactory` + test auth; 403 non-admin, summary/list/retry, 422/404 |
| Vitest | `navModel` role filtering, `shortEventType`, `DeadLetters` page (forbidden, counters, tables, both retries) |

`scripts/verify.sh --branch`: all 16 checks green.

E2E (`e2e/admin/dead-letters.spec.ts`, smoke only): a reserved `E2eAdmin` user (blueprint,
member of `home-system-admins`, own setup step + household) sees the counters and both sections;
a pool worker gets "Admins only". Seeding real dead letters would mean breaking a consumer on
the shared stack, so retry stays covered by Vitest and the API integration tests.

## 4. Out of scope / known limits

- "Retry all" (#432) handles up to 500 dead letters per click; a bigger backlog needs another click.
- Payloads (outbox event JSON, a delivery's title/body/payload) are not in the lists; an admin
  opens them per row ("View", #433). They can contain personal data: admin-only, fetched on
  demand, `Cache-Control: no-store`, not kept in the query cache.
- Dead rows are never purged; they stay until retried.
- No alerting: you only find out by opening the page / seeing the badge.

---

## 5. Decisions

1. **Outbox backoff.** Done in #431: `next_attempt_at` column, delay 1 s doubling per failure,
   capped at 5 min (`Messaging:Outbox:RetryBaseDelayMs` / `RetryMaxDelayMs`). With 10 attempts a
   message dead-letters after ~8.5 min instead of ~10 s. Retry clears the schedule.
2. **Admin = Authentik superusers or `home-system-admins` group.** Kept.
3. **Retry = new record** (#434). Retry no longer resets the row: the original keeps its attempts
   and error and is marked as history (outbox `retried_at`, delivery status `Retried`), and a new
   row linked through `retry_of` goes back to the worker. Retried originals leave the dead-letter
   list and counters; a retry that dies again shows a "Retried" badge on `/admin`.
4. **E2E coverage.** Lightweight smoke spec with a reserved `E2eAdmin` (see §3).
5. **Bulk retry and payload view.** Retry all per table done in #432 (`POST …/retry-all`, confirm
   dialog, 500 per call); payload view per row done in #433.
6. **Outbox admin endpoints live in the `Operations` module** (#435). With five endpoints after
   #432/#433 they moved out of `Shared.Infrastructure.Web` into `src/Modules/Operations`
   (Application + Api only — it owns no data and reads each module's keyed
   `IOutboxDeadLetterStore`). Endpoints now only dispatch queries/commands, like every other
   module; unknown module/message → `NotFoundException` → 404 ProblemDetails. Routes unchanged.
   Delivery admin stays in Notifications, since it is that module's use case.
