# Notifications Module — Design

**Status:** Draft
**Date:** 2026-04-25
**Epic:** #147

---

## 1. Goals

- Introduce a dedicated **Notifications** module that delivers user-facing notifications (meal reminders, water reminders, weekly summaries, goal-milestone alerts).
- Establish the project's first **cross-module integration-event plumbing** (outbox + inbox + transport abstraction).
- Keep the v1 transport in-process while making the abstraction broker-ready (drop-in RabbitMQ later).
- Establish a sanctioned alternative persistence style (**Dapper + DbUp**) for modules where rich aggregates would be ceremony.
- Extract a domain-specific dedup mechanism so duplicate notifications are impossible end-to-end.

## 2. Non-goals (v1)

- Real WebSocket / Email channels — only a `Console` channel is implemented; the abstraction is in place for the others.
- A delivery DLQ admin UI.
- Per-(notification-type × channel) preferences — only per-channel toggles.
- Server-side timezone awareness. Backend operates entirely in UTC. Frontend converts
  user-local times to UTC before sending and back to user-local for display. DST drift
  on stored "wall-clock" preferences (e.g., weekly summary time) is accepted for v1.
- RabbitMQ transport (the seam exists; add when needed).

## 3. Architecture overview

```text
┌────────────────────────┐       integration event        ┌──────────────────────┐
│ DietPlanner            │ ─────────────────────────────► │ Notifications        │
│ ─ DietReminderSettings │                                │ ─ ChannelPreferences │
│ ─ DietReminderTickService                               │ ─ Templates          │
│   (one BackgroundService                                │ ─ Dispatcher         │
│    invoking N IDietReminderJob)                         │ ─ Inbox              │
│ ─ GoalMilestoneEvaluator                                │ ─ Notifications +    │
│   (IDomainEventHandler)                                 │   NotificationDeliv. │
│ ─ Outbox                                                │ ─ RetryDeliveryWorker│
└────────────────────────┘                                └──────────────────────┘

                  Shared.Infrastructure.Messaging
                  ─ IIntegrationEventBus       (publisher abstraction)
                  ─ IIntegrationEventHandler<T>
                  ─ Outbox / OutboxWorker      (per-publisher DbContext)
                  ─ IIntegrationEventTransport (v1 InProcess; v2 RabbitMq)
                  ─ IInboxExecutor             (per-consumer; Ef or Dapper impl)
```

**Key seams:**

- `IIntegrationEventBus` is the only contract publishers use.
- `IIntegrationEventHandler<T>` is the only contract consumers use.
- Routing/exchange/queue logic is encapsulated in `IIntegrationEventTransport`. Swapping
  transport is a single DI registration change; no module code changes.
- Idempotency is enforced by `IInboxExecutor` per consumer module — EF implementation for
  EF-backed modules, Dapper implementation for Dapper-backed modules.

## 4. Persistence styles (new project rule)

A new rules file `.claude/rules/backend-persistence-styles.md` introduces two sanctioned
styles. A module must choose one; mixing within a module is forbidden.

### Style 1 — DDD + EF Core (existing, used by DietPlanner)

Use when:
- Rich aggregates with private collections and invariants.
- Multi-step state transitions per write.
- Domain events raised from aggregates.

### Style 2 — Lightweight + Dapper (new, used by Notifications)

Use when:
- Tables are flat. Writes are inserts and column-targeted updates.
- Reads are SELECT-by-key or SELECT-paged-by-userid.
- No domain invariants worth a rich aggregate.

Style-2 rules:

1. **Domain stays present, just thinner.** Models in `<Module>.Domain/Models` are sealed
   with private setters, mutated via methods (`MarkSent`, `MarkRead`). No collection
   navigation, no change tracking.
2. **One `IDbConnection` per request, one transaction per command.** A scoped
   `DapperUnitOfWork` opens an `NpgsqlConnection` lazily and exposes `Connection` and
   `Transaction`. Implements the same `IUnitOfWork` interface as EF modules.
3. **All SQL lives in `<Module>.Infrastructure/Persistence/Sql/*.cs` constants.** No
   inline SQL strings inside repositories.
4. **Queries call Dapper directly via the shared `IDbConnection`** — no transaction
   needed for reads.
5. **Migrations: DbUp, embedded numbered SQL files** under
   `<Module>.Infrastructure/Persistence/Migrations/`. Forward-only. Runs on startup in
   Development; via a `<Module>.Migrator` console project for CI/prod.
6. **Outbox/Inbox use the same shared abstractions but with Dapper-backed
   implementations.** Each consuming Style-2 module registers its own `DapperInboxExecutor`.

A companion rule file `.claude/rules/backend-dapper-module-structure.md` mirrors
`backend-module-structure.md` for Style 2.

## 5. Shared project decomposition (prerequisite)

The current `Shared.Abstractions` and `Shared.Infrastructure` projects bundle CQRS,
domain primitives, EF persistence helpers, and web middleware into two libraries.
Adding messaging on top would deepen the over-coupling. Before that, split both
into focused libraries so each module references only what it needs.

### 5.1 New shared projects

```text
src/Shared/
  Shared.Abstractions.Core/         -- IDomainEvent, AggregateRoot, Entity,
                                       IUnitOfWork, DomainException, NotFoundException,
                                       PagedList
  Shared.Abstractions.Cqrs/         -- ICommand, IQuery, ICommandDispatcher,
                                       IQueryDispatcher, ICommandHandler, IQueryHandler,
                                       ICommandValidator, CommandValidationException
  Shared.Abstractions.Messaging/    -- IIntegrationEvent, IIntegrationEventBus,
                                       IIntegrationEventHandler<T>, IInboxExecutor

  Shared.Infrastructure.Cqrs/       -- CommandDispatcher, QueryDispatcher,
                                       Logging/Validation/Transaction decorators,
                                       CqrsExtensions
  Shared.Infrastructure.Messaging/  -- OutboxIntegrationEventBus, OutboxWorker,
                                       IIntegrationEventTransport,
                                       InProcessIntegrationEventTransport,
                                       EfInboxExecutor, DapperInboxExecutor
  Shared.Infrastructure.Persistence/-- DomainEventDispatcherInterceptor,
                                       EF model conventions
  Shared.Infrastructure.Web/        -- ExceptionHandlingMiddleware,
                                       ProblemDetails helpers
```

### 5.2 Reference rules

- `Shared.Abstractions.*` projects depend on **nothing** (or each other only when truly
  necessary — e.g., `Messaging` references `Core` for `IIntegrationEvent`).
- `Shared.Infrastructure.*` projects depend on the matching abstractions plus only the
  third-party packages they need (EF Core only in `*.Persistence` and the EF inbox
  executor; Dapper / Npgsql only in the Dapper inbox executor; ASP.NET Core only in
  `*.Web`).
- Module Infrastructure projects reference only the shared infrastructure packages they
  actually use. Example matrix:

| Module project | Core | Cqrs (abs) | Messaging (abs) | Cqrs (inf) | Messaging (inf) | Persistence (inf) | Web (inf) |
|---|---|---|---|---|---|---|---|
| `DietPlanner.Domain` | ✅ | | | | | | |
| `DietPlanner.Application` | ✅ | ✅ | ✅ | | | | |
| `DietPlanner.Infrastructure` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `Notifications.Domain` | ✅ | | | | | | |
| `Notifications.Application` | ✅ | ✅ | ✅ | | | | |
| `Notifications.Infrastructure` (Dapper) | ✅ | ✅ | ✅ | ✅ | ✅ | | |
| `HomeSystem.REST` (host) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

Notifications.Infrastructure does **not** reference `Shared.Infrastructure.Persistence`
because it's Dapper-backed and has no EF interceptor needs.

### 5.3 Migration path

The split is mechanical: move types to their new projects, update `using`s, fix project
references in `.csproj` files. Public API surface is unchanged. Done as a single
preparatory PR (issue **N0**) before any messaging work, so Notifications can be added
without further reshuffling.

## 6. Shared messaging infrastructure

### 6.1 Abstractions (`Shared.Abstractions.Messaging/`)

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

internal interface IInboxExecutor
{
    Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct);
}
```

### 6.2 Implementations (`Shared.Infrastructure.Messaging/`)

- `OutboxIntegrationEventBus` — writes a serialized event into the calling module's
  outbox using its DbContext (`AddOutbox<TDbContext>()`).
- `OutboxWorker<TDbContext>` — `BackgroundService`; polls unprocessed rows, invokes
  `IIntegrationEventTransport.DispatchAsync`, marks `processed_at`. Tracks
  `attempt_count` and `last_error` for poison-message visibility.
- `IIntegrationEventTransport`:
  - `InProcessIntegrationEventTransport` (v1) — resolves `IIntegrationEventHandler<T>`
    instances from DI in a fresh scope per handler; calls into `IInboxExecutor` for
    idempotency + transactional commit.
  - `RabbitMqIntegrationEventTransport` (v2, future) — drop-in replacement.
- `EfInboxExecutor<TDbContext>` — opens a DbContext transaction, checks `inbox_messages`,
  invokes the handler, inserts the inbox row, commits.
- `DapperInboxExecutor` — same flow with raw Npgsql transaction + Dapper.

### 6.3 Outbox / Inbox tables

Each publishing module owns an `outbox_messages` table; each consuming module owns an
`inbox_messages` table.

```text
outbox_messages
  id              uuid       pk
  event_id        uuid       not null  -- the IIntegrationEvent.EventId
  event_type      text       not null  -- assembly-qualified name
  payload         jsonb      not null
  occurred_at     timestamptz not null
  processed_at    timestamptz null
  attempt_count   int        not null default 0
  last_error      text       null
  index on (processed_at) where processed_at is null

inbox_messages
  event_id        uuid       pk
  event_type      text       not null
  consumed_at     timestamptz not null
```

### 6.4 Module wiring

```csharp
// Host (HomeSystem.REST)
services.AddIntegrationEventBus()
        .UseInProcessTransport();

// Publishing module
services.AddOutbox<DietPlannerDbContext>();

// Consuming module (EF)
services.AddInbox<SomeDbContext>();

// Consuming module (Dapper)
services.AddDapperInbox<NotificationsConnectionFactory>();
```

## 7. Notifications module

### 7.1 Layout (Style 2)

```text
Modules/Notifications/
  Notifications.Domain/
    Models/
      Notification.cs
      NotificationDelivery.cs
      NotificationChannelPreferences.cs
    ValueObjects/
      NotificationId.cs
      NotificationDeliveryId.cs
      NotificationChannelPreferencesId.cs
      NotificationType.cs            -- MealReminder | MealMissed | WaterReminder | WeeklySummary | GoalMilestone
      NotificationChannel.cs         -- Console | Email | WebSocket
      DeliveryStatus.cs              -- Pending | Sent | Failed | Skipped
    Abstractions/
      INotificationRepository.cs
      INotificationChannelPreferencesRepository.cs
      IInboxStore.cs

  Notifications.Application/
    Templates/
      INotificationTemplateRegistry.cs
      NotificationTemplate.cs
      NotificationTemplateRegistry.cs
      Templates/                     -- one file per (type × locale)
    Channels/
      INotificationChannelSender.cs
      ConsoleNotificationChannelSender.cs
    Dispatching/
      INotificationDispatcher.cs
      NotificationDispatcher.cs
    EventHandlers/
      MealReminderDueIntegrationEventHandler.cs
      MealMissedIntegrationEventHandler.cs
      WaterReminderDueIntegrationEventHandler.cs
      WeeklySummaryDueIntegrationEventHandler.cs
      GoalMilestoneReachedIntegrationEventHandler.cs
    Workers/
      RetryDeliveryWorker.cs
    Commands/
      UpdateChannelPreferences/
      MarkNotificationRead/
    Queries/
      ListNotifications/
      GetChannelPreferences/

  Notifications.Contracts/           -- empty for v1; reserved

  Notifications.Infrastructure/
    Persistence/
      NotificationsConnectionFactory.cs
      DapperUnitOfWork.cs
      Repositories/
        NotificationRepository.cs
        NotificationChannelPreferencesRepository.cs
        InboxStore.cs
      Sql/
        NotificationSql.cs
        NotificationDeliverySql.cs
        ChannelPreferencesSql.cs
        InboxSql.cs
      Migrations/
        001_create_notifications.sql
        002_create_notification_deliveries.sql
        003_create_notification_channel_preferences.sql
        004_create_inbox_messages.sql
        DbUpRunner.cs
    DependencyInjection.cs

  Notifications.Api/
    NotificationsEndpoints.cs        -- GET /notifications, POST /notifications/{id}/read
    NotificationChannelPreferencesEndpoints.cs  -- GET/PUT /notification-preferences
    DependencyInjection.cs
```

### 7.2 Schema

```text
notifications
  id              uuid       pk
  user_id         text       not null
  type            text       not null
  title           text       not null
  body            text       not null
  payload         jsonb      not null   -- original event payload
  created_at      timestamptz not null
  read_at         timestamptz null
  index on (user_id, created_at desc)

notification_deliveries
  id              uuid       pk
  notification_id uuid       not null fk → notifications(id) on delete cascade
  channel         text       not null
  status          text       not null   -- Pending | Sent | Failed | Skipped
  attempt_count   int        not null default 0
  last_attempt_at timestamptz null
  sent_at         timestamptz null
  failure_reason  text       null
  index on (status, last_attempt_at) where status = 'Failed'

notification_channel_preferences
  id              uuid       pk
  user_id         text       unique not null
  console_enabled boolean    not null default true
  email_enabled   boolean    not null default true
  websocket_enabled boolean  not null default true
  updated_at      timestamptz not null
```

### 7.3 Dispatcher flow

```text
DispatchAsync(type, userId, locale, payload)
  1. Load NotificationChannelPreferences for userId; create defaults if missing.
  2. Resolve template (type, locale) → render Title + Body from payload.
     Locale fallback: requested → en → throw.
  3. Create Notification + one NotificationDelivery per enabled channel (Status=Pending).
  4. Persist (single transaction).
  5. For each delivery: pick INotificationChannelSender by channel.
       success → MarkSent(); failure → MarkFailed(reason); persist.
```

### 7.4 Retry policy

`RetryDeliveryWorker` runs every 60s:

```text
SELECT id FROM notification_deliveries
WHERE status = 'Failed'
  AND attempt_count < 5
  AND last_attempt_at < now() - (attempt_count^2 || ' minutes')::interval
ORDER BY last_attempt_at
LIMIT 100
```

Backoff: 1m, 4m, 9m, 16m, 25m. After 5 attempts the row stays `Failed` permanently.

## 8. DietPlanner-side changes

### 8.1 Rename `NotificationPreferences` → `DietReminderSettings`

Aggregate stays in `DietPlanner.Domain`. Renamed and trimmed to diet timing only:

```text
DietReminderSettings
  MealRemindersEnabled         bool
  MealReminderLeadTimeMinutes  int
  MealMissedGraceMinutes       int        -- new
  WaterRemindersEnabled        bool
  WaterReminderIntervalMinutes int
  WaterWindowStartUtc          TimeOnly   -- new, UTC; FE converts from user-local
  WaterWindowEndUtc            TimeOnly   -- new, UTC
  WeeklySummaryEnabled         bool
  WeeklySummaryDayOfWeekUtc    DayOfWeek  -- new, UTC
  WeeklySummaryTimeOfDayUtc    TimeOnly   -- new, UTC
  GoalAlertsEnabled            bool
```

Endpoints renamed: `/diet-reminder-settings`. EF migration renames the table + columns
and adds the new columns; existing rows preserved.

### 8.2 `DietReminderTickService`

One `BackgroundService` ticking every 60s and invoking each registered `IDietReminderJob`
in its own scope and try/catch:

```csharp
internal interface IDietReminderJob
{
    string Name { get; }
    Task RunAsync(DateTime nowUtc, CancellationToken ct);
}

internal sealed class MealReminderJob   : IDietReminderJob { ... }   // both reminder + missed
internal sealed class WaterReminderJob  : IDietReminderJob { ... }
internal sealed class WeeklySummaryJob  : IDietReminderJob { ... }
```

Dedup ledgers (DietPlanner-owned):

```text
sent_meal_reminders
  meal_entry_id  uuid     not null
  kind           text     not null  -- 'Reminder' | 'Missed'
  sent_at        timestamptz not null
  primary key (meal_entry_id, kind)

water_reminder_state
  user_id                 text     pk
  last_water_reminder_at  timestamptz null

weekly_summary_state
  user_id                 text     pk
  last_weekly_summary_at  timestamptz null
```

Dedup belongs at the source so duplicates are never even published. The Notifications
inbox is a second line of defence, not the primary one.

### 9.3 Goal milestone evaluator

Not on a tick. A domain-event handler invoked by the existing
`DomainEventDispatcherInterceptor` when a user logs a weight entry / completes meals:

```csharp
internal sealed class GoalMilestoneEvaluator
    : IDomainEventHandler<WeightEntryAddedDomainEvent>
{
    public async Task HandleAsync(WeightEntryAddedDomainEvent e, CancellationToken ct)
    {
        var goal = await _goals.GetActiveAsync(e.UserId, ct);
        if (goal is null) return;

        var milestone = goal.EvaluateAgainst(e.WeightKg, _clock.UtcNow);
        if (milestone is null) return;

        goal.MarkMilestoneAchieved(milestone.Kind, _clock.UtcNow);

        await _outbox.WriteAsync(new GoalMilestoneReachedIntegrationEvent(...), ct);
    }
}
```

Idempotency lives on the `Goal` aggregate (`MarkMilestoneAchieved` is no-op or throw if
already achieved), so the same write cannot fire twice.

### 8.4 Integration events (`DietPlanner.Contracts`)

```csharp
public sealed record MealReminderDueIntegrationEvent(
    Guid EventId, DateTime OccurredAt,
    string UserId, string Locale,
    Guid MealEntryId, string MealSlotName, DateTime PlannedAt) : IIntegrationEvent;

public sealed record MealMissedIntegrationEvent(
    Guid EventId, DateTime OccurredAt,
    string UserId, string Locale,
    Guid MealEntryId, string MealSlotName, DateTime PlannedAt) : IIntegrationEvent;

public sealed record WaterReminderDueIntegrationEvent(
    Guid EventId, DateTime OccurredAt,
    string UserId, string Locale) : IIntegrationEvent;

public sealed record WeeklySummaryDueIntegrationEvent(
    Guid EventId, DateTime OccurredAt,
    string UserId, string Locale,
    DateOnly WeekStart, DateOnly WeekEnd,
    int TotalKcal, int TargetKcal,
    decimal AvgWaterLiters,
    decimal? WeightDeltaKg,
    int MealsCompleted, int MealsPlanned) : IIntegrationEvent;

public sealed record GoalMilestoneReachedIntegrationEvent(
    Guid EventId, DateTime OccurredAt,
    string UserId, string Locale,
    string GoalKind, string MilestoneLabel, decimal? Value) : IIntegrationEvent;
```

All payloads carry `UserId` and `Locale`; Notifications never reads back into DietPlanner
or IAM at consume time.

## 9. Testing strategy

### 9.1 Shared messaging
- *Unit*: bus writes outbox row; worker polls and dispatches; in-process transport
  resolves handlers; `EfInboxExecutor` and `DapperInboxExecutor` are idempotent.
- *Integration*: full roundtrip (publish → outbox → transport → handler → inbox; second
  publish of same `EventId` is skipped).

### 9.2 DietPlanner schedulers
- *Unit*: each job against fake `IClock` and in-memory data — meal-in-window fires once,
  re-tick does not refire (dedup), missed-grace fires, water windowing respects bounds,
  weekly summary fires once per week per user.
- *Integration*: tick `DietReminderTickService` against real Postgres; assert outbox rows.

### 9.3 Goal milestone evaluator
- *Unit*: handler with `Goal` stub — first crossing emits, second below-threshold does
  nothing, second crossing of same milestone is a no-op.
- *Integration*: POST `/weight-entries` → outbox row + `Goal.AchievedAt` set.

### 9.4 Notifications module
- *Unit*: dispatcher with substituted senders/preferences/templates; template registry
  fallback to `en`; each handler delegates to dispatcher with right type/payload; retry
  worker honours backoff and 5-attempt cap.
- *Integration*: publish `MealReminderDueIntegrationEvent` to in-process bus →
  `notifications` row + one `notification_deliveries` row `Status = Sent` + console log
  captured. `GET /notifications` returns it paged.

### 9.5 E2E
- One smoke spec: trigger meal reminder via test-support endpoint → notification appears
  in `GET /notifications` → toggling channel preference hides it on next event.

### 9.6 Test-support endpoints (Development only)
- `POST /test-support/notifications/trigger` — fires an integration event by name +
  payload. Used by E2E and manual QA.

## 10. Rollout plan (issue split)

13 issues across 6 milestones. Backend → frontend.

### Milestone 0 — Shared project decomposition

- **N0** Split `Shared.Abstractions` and `Shared.Infrastructure` into focused libraries
  (`*.Core`, `*.Cqrs`, `*.Messaging`, `*.Persistence`, `*.Web`). Move existing types
  to their new homes. Public API surface unchanged.

### Milestone 1 — Foundations

- **N1** docs: persistence-styles + dapper-module-structure rule files.
- **N2** Shared messaging abstractions + in-process transport + EF/Dapper inbox executors.

### Milestone 2 — Notifications skeleton
- **N3** Notifications module skeleton (Dapper, DbUp, repositories, inbox executor).
- **N4** Notifications dispatcher, console channel, channel-preferences API, retry worker.

### Milestone 3 — DietPlanner outbox + goal alerts
- **N5** Rename `NotificationPreferences` → `DietReminderSettings` + new fields.
- **N6** DietPlanner outbox, integration events, `GoalMilestoneEvaluator`, Notifications
  handler for goal alerts.

### Milestone 4 — Schedulers
- **N7** `DietReminderTickService` + `MealReminderJob` (reminder + missed) + templates.
- **N8** `WaterReminderJob` + templates.
- **N9** `WeeklySummaryJob` + `GetWeeklySummaryQuery` + templates.

### Milestone 5 — Frontend
- **N10** FE rename `notification-preferences` → `diet-reminder-settings`.
- **N11** FE notifications inbox page (read-only).
- **N12** FE per-channel preferences page.

### Dependency graph

```text
N0 ──► N1
   ──► N2 ──┬─► N3 ──► N4
            │
            └─► N6
N5 ──► N6 ──► N7 ──► N8 ──► N9
Backend complete ──► N10 ──► N11 ──► N12
```

## 11. Future / backlog (tracked as separate issues)

These are out of scope for the v1 milestones above but are filed as GitHub issues so
they live in the backlog and can be picked up independently.

- **N13** WebSocket sender + connection registry — wires real-time delivery into the
  Console-equivalent slot. Adds an `INotificationChannelSender` impl backed by
  ASP.NET Core SignalR or raw WebSocket, plus a connection registry keyed by `userId`
  with reconnect handling. On connect, replays unread `notifications` rows.
- **N14** Email sender — `INotificationChannelSender` impl backed by SMTP / Resend /
  similar. Pulls templates and dispatches with retry; respects `email_enabled`
  preference.
- **N15** DLQ admin endpoint — read-only API + minimal UI surfacing
  `notification_deliveries` rows that exhausted retries (`Status = Failed AND
  attempt_count >= 5`), and outbox poison messages
  (`attempt_count >= N`). Manual requeue action.
- **N16** Per-(notification-type × channel) preferences — extends
  `notification_channel_preferences` from per-channel toggles to a matrix
  (`{ type, channel } → enabled`). UI gets a grid; dispatcher consults the matrix
  before fanning out.
- **N17** Server-side timezone storage / DST-correct scheduling — adds an IANA
  timezone column to user profiles and switches the schedulers to
  `TimeZoneInfo.ConvertTime`-aware windowing. Removes the v1 DST-drift compromise.
- **N18** RabbitMQ transport — implements `RabbitMqIntegrationEventTransport` against
  the existing `IIntegrationEventTransport` seam. Adds `RabbitMQ.Client` to
  `Shared.Infrastructure.Messaging`, a connection factory, topology declarator,
  consumer host. Switching is a single DI registration in the host. Adds a docker
  compose profile.
