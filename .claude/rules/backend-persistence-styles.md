# Backend — Persistence Styles

This project sanctions **two** persistence styles. Each module must pick one and stick
to it. Mixing the two within a single module is forbidden — that boundary is what keeps
the rules in this file enforceable.

| Style | When | Reference rules |
|---|---|---|
| **Style 1 — DDD + EF Core** | Rich aggregates with invariants, multi-step state transitions, domain events raised from aggregates | `backend-ddd-patterns.md`, `backend-ef-core-patterns.md`, `backend-module-structure.md` |
| **Style 2 — Lightweight + Dapper** | Flat tables, insert + column-targeted update writes, key/page-by-userid reads, no domain invariants worth a rich aggregate | this file + `backend-dapper-module-structure.md` |

> The choice is per **module**, not per aggregate. If you find yourself wanting Dapper
> for one table inside a Style-1 module, that's a sign the table belongs in a different
> module.

---

## When to choose Style 1 (DDD + EF Core)

Pick this when **all** of the following apply:

- Writes mutate aggregates with private collections or invariants worth enforcing
  (e.g. "no entry may exceed the daily limit").
- A single command can change multiple rows that must stay consistent.
- The aggregate raises domain events that other code reacts to within the same
  transaction (via `DomainEventDispatcherInterceptor`).
- Reads occasionally need joins across owned types or child collections.

Examples in this repo: `DietPlanner`.

## When to choose Style 2 (Lightweight + Dapper)

Pick this when **all** of the following apply:

- Tables are flat. Each row is independent — no parent/child collection navigation.
- Writes are inserts and column-targeted updates (`UPDATE ... SET status = ...`).
- Reads are `SELECT` by primary key, or paged `SELECT` filtered by `user_id`.
- There are no domain invariants worth a rich aggregate.
- Domain events, if any, are simple and emitted from a service, not from an aggregate.

Examples in this repo: `Notifications`.

---

## Style 2 — Rules

These rules apply to every module that picks Style 2. They mirror the DDD rules where
useful and replace them where Dapper differs.

### 1. Domain stays present, just thinner

Models live in `<Module>.Domain/Models/`. They are `sealed`, with `private` setters and
a private parameterless constructor. Mutations go through named methods
(`MarkSent`, `MarkRead`, `RecordFailure`). No collection navigation, no change tracking.

```csharp
public sealed class NotificationDelivery
{
    private NotificationDelivery() { }   // for Dapper materialization

    public static NotificationDelivery Create(
        NotificationDeliveryId id,
        NotificationId notificationId,
        NotificationChannel channel)
        => new()
        {
            Id = id,
            NotificationId = notificationId,
            Channel = channel,
            Status = DeliveryStatus.Pending,
            AttemptCount = 0
        };

    public NotificationDeliveryId Id { get; private set; } = default!;
    public NotificationId NotificationId { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkSent(DateTime utcNow)
    {
        Status = DeliveryStatus.Sent;
        SentAt = utcNow;
        LastAttemptAt = utcNow;
        AttemptCount++;
    }

    public void MarkFailed(DateTime utcNow, string reason)
    {
        Status = DeliveryStatus.Failed;
        LastAttemptAt = utcNow;
        FailureReason = reason;
        AttemptCount++;
    }
}
```

- **No public setters.** Same rule as Style 1.
- **Typed IDs.** Same rule as Style 1 (`NotificationId`, `NotificationDeliveryId`).
- **Value objects** are records, immutable, validated in constructors.

### 2. One `IDbConnection` per request, one transaction per command

A scoped `DapperUnitOfWork` opens an `NpgsqlConnection` lazily and exposes
`Connection` and `Transaction`. It implements the same `IUnitOfWork` interface that
EF modules use — this keeps the CQRS dispatcher's transaction decorator agnostic.

```csharp
internal sealed class DapperUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly NotificationsConnectionFactory _factory;
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public DapperUnitOfWork(NotificationsConnectionFactory factory)
        => _factory = factory;

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is not null) return _connection;
        _connection = await _factory.OpenAsync(ct);
        return _connection;
    }

    public async Task<NpgsqlTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        var connection = await GetConnectionAsync(ct);
        _transaction ??= await connection.BeginTransactionAsync(ct);
        return _transaction;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
```

- Reads do **not** open a transaction.
- Writes flow through a command handler that calls `BeginTransactionAsync`, mutates via
  the repository, and commits via `IUnitOfWork.CommitAsync` — exactly like Style 1.

### 3. SQL lives in `Sql/*.cs` constants

All SQL is in `<Module>.Infrastructure/Persistence/Sql/<Aggregate>Sql.cs` as `internal
const string` fields. **No inline SQL strings inside repositories.**

```csharp
// Notifications.Infrastructure/Persistence/Sql/NotificationDeliverySql.cs
internal static class NotificationDeliverySql
{
    internal const string Insert = """
        INSERT INTO notification_deliveries
            (id, notification_id, channel, status, attempt_count)
        VALUES
            (@Id, @NotificationId, @Channel, @Status, @AttemptCount);
        """;

    internal const string MarkSent = """
        UPDATE notification_deliveries
        SET status = 'Sent',
            sent_at = @SentAt,
            last_attempt_at = @LastAttemptAt,
            attempt_count = attempt_count + 1
        WHERE id = @Id;
        """;

    internal const string SelectFailedForRetry = """
        SELECT id
        FROM notification_deliveries
        WHERE status = 'Failed'
          AND attempt_count < 5
          AND last_attempt_at < now() - (attempt_count ^ 2 || ' minutes')::interval
        ORDER BY last_attempt_at
        LIMIT @BatchSize;
        """;
}
```

- One `*Sql.cs` file per aggregate.
- Use raw string literals (`"""..."""`) for multi-line SQL.
- Parameter names match the C# property names (Dapper convention).

### 4. Queries call Dapper directly via the shared connection

Query handlers do **not** go through the repository. They take the `DapperUnitOfWork`
(or a read-only connection accessor), call `connection.QueryAsync<TDto>()`, and project
straight into a DTO.

```csharp
internal sealed class ListNotificationsQueryHandler
    : IQueryHandler<ListNotificationsQuery, PagedList<NotificationDto>>
{
    private readonly DapperUnitOfWork _uow;

    public ListNotificationsQueryHandler(DapperUnitOfWork uow) => _uow = uow;

    public async Task<PagedList<NotificationDto>> HandleAsync(
        ListNotificationsQuery query, CancellationToken ct)
    {
        var connection = await _uow.GetConnectionAsync(ct);

        var items = await connection.QueryAsync<NotificationDto>(
            NotificationSql.ListByUserPaged,
            new { query.UserId, Offset = (query.Page - 1) * query.PageSize, Limit = query.PageSize });

        var total = await connection.ExecuteScalarAsync<int>(
            NotificationSql.CountByUser, new { query.UserId });

        return new PagedList<NotificationDto>(items.ToList(), total, query.Page, query.PageSize);
    }
}
```

- No tracking, no aggregate hydration on the read side.
- DTOs are plain records — same rule as Style 1.

### 5. Migrations: DbUp + embedded numbered SQL files

- Migrations live in `<Module>.Infrastructure/Persistence/Migrations/` as `.sql` files.
- File naming: `NNN_short_description.sql` (`001_create_notifications.sql`,
  `002_create_notification_deliveries.sql`, …).
- Files are **embedded resources** in the `.csproj`.
- Forward-only. Never edit a deployed migration; add a new one.
- DbUp runs them in order, tracked in a `__db_up_journal` table.
- Development startup runs them automatically. CI/prod runs them via a
  `<Module>.Migrator` console project deployed alongside the host.

```csharp
internal static class DbUpRunner
{
    public static void Run(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DbUpRunner).Assembly,
                name => name.Contains(".Persistence.Migrations."))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful) throw result.Error;
    }
}
```

### 6. Outbox / Inbox

A Style-2 module that **publishes** integration events still uses the shared
`Shared.Infrastructure.Messaging` outbox — but with a Dapper-backed implementation
of the storage seam, not EF.

A Style-2 module that **consumes** integration events registers the shared
`DapperInboxExecutor` (from `Shared.Infrastructure.Messaging`) bound to its
`*ConnectionFactory`. The inbox table (`inbox_messages`) is owned by the consuming
module, created via that module's DbUp migrations.

```csharp
// Notifications.Infrastructure/DependencyInjection.cs
services.AddDapperInbox<NotificationsConnectionFactory>();
```

The contracts (`IIntegrationEventBus`, `IIntegrationEventHandler<T>`,
`IInboxExecutor`) are identical across the two styles — only the implementation
behind the seam differs.

---

## Things that are the same across both styles

These project-wide rules apply regardless of the persistence style chosen:

- **Module boundaries.** No cross-module domain imports. Communication via integration
  events or the Contracts project (see `backend-integration-patterns.md`).
- **CQRS.** Same `ICommand` / `IQuery` / dispatcher contracts; same handler structure.
- **Validation.** `ICommandValidator<T>` works identically.
- **Cancellation.** Every async method propagates `CancellationToken ct`.
- **API layer.** Endpoints look the same — the persistence style is invisible at the
  HTTP boundary.
- **Testing.** Unit tests mock the repository / `IUnitOfWork`. Integration tests run
  against a real PostgreSQL via Testcontainers.

## Things that change between styles

| Concern | Style 1 (EF) | Style 2 (Dapper) |
|---|---|---|
| Aggregates | Rich, with private collections | Thin models, no collections |
| Mapping | `IEntityTypeConfiguration<T>` | SQL constants + Dapper |
| Migrations | EF Core migrations (`dotnet ef migrations add`) | DbUp + numbered `.sql` files |
| Read side | `DbContext` with `AsNoTracking()` + `Select()` | `connection.QueryAsync<TDto>` |
| Domain events | Raised from aggregate, dispatched via interceptor | Emitted from a service if needed |
| Inbox executor | `EfInboxExecutor<TDbContext>` | `DapperInboxExecutor` |
| Outbox storage | EF entity + `IEntityTypeConfiguration` | Dapper repository + SQL constants |

---

## Decision checklist before starting a module

1. List the writes you expect. If they are mostly inserts and one-column updates → Style 2.
2. List the invariants you expect. If you can name three or more "X must always Y"
   rules across rows → Style 1.
3. List the reads you expect. If they all key on `user_id` and look like SELECT-paged
   → Style 2.
4. Will another module react to events from this module? Both styles support that
   identically — not a deciding factor.
5. Do you want EF migrations or DbUp scripts? This is the smallest of the five questions.

If 1–3 disagree, default to Style 1 — invariants are the expensive thing to retrofit.
