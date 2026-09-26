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
| `AdminAuthorization` — `Admin` policy: token has `roles` claim with `admin` (accepts both raw `roles` and the mapped `ClaimTypes.Role`). | `Shared.Infrastructure.Web` |
| Outbox admin endpoints (store-direct, no dispatcher — pure infra, one store call each). | `Shared.Infrastructure.Web/Admin/OutboxAdminEndpoints.cs` |
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

**Not done:** Playwright spec. The E2E pool users aren't admins, and the page needs an admin
identity (see decision 4).

## 4. Out of scope / known limits

- Retry is per row. No "retry all" / bulk action.
- Outbox payload isn't shown (it can contain personal data); only type, time, attempts, error.
- Dead rows are never purged; they stay until retried.
- No alerting: you only find out by opening the page / seeing the badge.

---

## 5. Decisions needed

1. **Outbox has no backoff — is 10 attempts OK?** The outbox worker ticks every second, so a
   message dead-letters after about **10 s** of failures. A short consumer-DB outage longer than
   that now needs a manual retry, where before it retried forever. Options:
   **(a)** keep 10 and just retry by hand; **(b)** raise the default (e.g. 60 ≈ 1 min);
   **(c) recommended:** follow-up issue to add `last_attempt_at` + exponential backoff to
   `outbox_messages` (EF migration in DietPlanner and Household), like the delivery retry worker
   already does. I kept (a) so this PR has no migrations.
2. **Admin = Authentik superusers *or* `home-system-admins` group.** Fine, or group only (then
   add akadmin to the group explicitly)? Or a config allow-list of subjects instead of Authentik?
3. **Retry semantics = requeue (reset attempts, wait for the worker).** Alternative: "send now"
   (run the sender/handler inline and return the result). Requeue is simpler and safer; say if
   you want immediate feedback instead.
4. **E2E coverage.** Add an `E2eAdmin` user in the blueprint (member of `home-system-admins`)
   plus a Playwright spec for `/admin`? It needs an extra auth-setup project for that user.
5. **Bulk "retry all" + payload view** — wanted? Both are small follow-ups.
6. **Where the admin endpoints live.** Outbox admin is in `Shared.Infrastructure.Web` and calls
   the store directly (no CQRS dispatcher) because it's messaging infrastructure, not a module
   use case. If you'd rather keep "endpoints only dispatch", it needs query/command types in
   shared infra and a way to register their handlers.
