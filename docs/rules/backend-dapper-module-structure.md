# Backend — Dapper Module Structure (Style 2)

Companion to `backend-module-structure.md`. Applies to modules that picked **Style 2**
(Lightweight + Dapper) per `backend-persistence-styles.md`.

The high-level layer rules are the same as Style 1. Only the Infrastructure layer's
internal layout differs — EF `DbContext` and configurations are replaced by a
connection factory, a Dapper unit of work, SQL constants, and DbUp-managed `.sql`
migrations.

---

## Solution layout

```
src/
  Modules/
    {ModuleName}/
      {ModuleName}.Api/
      {ModuleName}.Application/
      {ModuleName}.Contracts/
      {ModuleName}.Domain/
      {ModuleName}.Infrastructure/
      {ModuleName}.Migrator/         # Console project for CI/prod migrations (optional in dev)
      {ModuleName}.UnitTests/
      {ModuleName}.IntegrationTests/
```

The `*.Migrator` project is **optional**: in Development, DbUp runs from the host on
startup. CI/prod pipelines invoke the migrator console as a separate step before
deploying the API, so schema changes ship before the code that depends on them.

---

## Full module anatomy (Dapper module)

```
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
      NotificationType.cs
      NotificationChannel.cs
      DeliveryStatus.cs
    Abstractions/
      INotificationRepository.cs
      INotificationChannelPreferencesRepository.cs
      IInboxStore.cs

  Notifications.Application/
    Commands/
      UpdateChannelPreferences/
        UpdateChannelPreferencesCommand.cs
        UpdateChannelPreferencesCommandHandler.cs
        UpdateChannelPreferencesCommandValidator.cs
      MarkNotificationRead/
        MarkNotificationReadCommand.cs
        MarkNotificationReadCommandHandler.cs
    Queries/
      ListNotifications/
        ListNotificationsQuery.cs
        ListNotificationsQueryHandler.cs
        NotificationDto.cs
      GetChannelPreferences/
        GetChannelPreferencesQuery.cs
        GetChannelPreferencesQueryHandler.cs
        ChannelPreferencesDto.cs
    EventHandlers/
      MealReminderDueIntegrationEventHandler.cs
      ...

  Notifications.Contracts/
    Events/                          # only when this module publishes events
    Interfaces/                      # only when other modules need a sync contract

  Notifications.Infrastructure/
    Persistence/
      NotificationsConnectionFactory.cs    # NpgsqlDataSource wrapper, scoped lifetime
      DapperUnitOfWork.cs                  # IUnitOfWork — opens connection + tx lazily
      Repositories/
        NotificationRepository.cs
        NotificationChannelPreferencesRepository.cs
        InboxStore.cs                      # implements IInboxExecutor for this module
      Sql/
        NotificationSql.cs                 # const string fields per query
        NotificationDeliverySql.cs
        ChannelPreferencesSql.cs
        InboxSql.cs
      Migrations/                          # embedded resources in .csproj
        001_create_notifications.sql
        002_create_notification_deliveries.sql
        003_create_notification_channel_preferences.sql
        004_create_inbox_messages.sql
        DbUpRunner.cs
    DependencyInjection.cs               # internal wiring

  Notifications.Api/
    NotificationsEndpoints.cs
    NotificationChannelPreferencesEndpoints.cs
    DependencyInjection.cs               # AddNotificationsModule()
```

---

## What each file looks like

### Connection factory

```csharp
// Notifications.Infrastructure/Persistence/NotificationsConnectionFactory.cs
internal sealed class NotificationsConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificationsConnectionFactory(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;

    public Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
        => _dataSource.OpenConnectionAsync(ct);
}
```

`NpgsqlDataSource` is registered as a singleton (it pools connections internally).
The factory is scoped, but it's a thin wrapper — there is no per-request state on it.

### Unit of work

See `backend-persistence-styles.md` §2 — `DapperUnitOfWork` opens an `NpgsqlConnection`
lazily and exposes `Connection` + `Transaction`. It implements `IUnitOfWork` so the
shared `TransactionCommandDispatcherDecorator` works without changes.

### Repository

```csharp
// Notifications.Infrastructure/Persistence/Repositories/NotificationRepository.cs
internal sealed class NotificationRepository : INotificationRepository
{
    private readonly DapperUnitOfWork _uow;

    public NotificationRepository(DapperUnitOfWork uow) => _uow = uow;

    public async Task AddAsync(Notification notification, CancellationToken ct)
    {
        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(
            NotificationSql.Insert,
            new
            {
                notification.Id,
                notification.UserId,
                Type = notification.Type.ToString(),
                notification.Title,
                notification.Body,
                Payload = notification.Payload,
                notification.CreatedAt
            },
            transaction: tx);
    }

    public async Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct)
    {
        var connection = await _uow.GetConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Notification>(
            NotificationSql.SelectById, new { Id = id.Value });
    }

    public async Task UpdateReadAsync(NotificationId id, DateTime readAt, CancellationToken ct)
    {
        var tx = await _uow.BeginTransactionAsync(ct);
        await tx.Connection!.ExecuteAsync(
            NotificationSql.MarkRead,
            new { Id = id.Value, ReadAt = readAt },
            transaction: tx);
    }
}
```

- Mutating methods open / reuse the transaction via `BeginTransactionAsync`.
- Read methods use the connection without a transaction.
- The handler eventually calls `IUnitOfWork.CommitAsync` once at the end of the command.

### SQL constants

```csharp
// Notifications.Infrastructure/Persistence/Sql/NotificationSql.cs
internal static class NotificationSql
{
    internal const string Insert = """
        INSERT INTO notifications
            (id, user_id, type, title, body, payload, created_at)
        VALUES
            (@Id, @UserId, @Type, @Title, @Body, @Payload::jsonb, @CreatedAt);
        """;

    internal const string SelectById = """
        SELECT id           AS Id,
               user_id      AS UserId,
               type         AS Type,
               title        AS Title,
               body         AS Body,
               payload      AS Payload,
               created_at   AS CreatedAt,
               read_at      AS ReadAt
        FROM notifications
        WHERE id = @Id;
        """;

    internal const string MarkRead = """
        UPDATE notifications
        SET read_at = @ReadAt
        WHERE id = @Id
          AND read_at IS NULL;
        """;

    internal const string ListByUserPaged = """
        SELECT id, user_id, type, title, body, created_at, read_at
        FROM notifications
        WHERE user_id = @UserId
        ORDER BY created_at DESC
        OFFSET @Offset LIMIT @Limit;
        """;

    internal const string CountByUser = """
        SELECT count(*) FROM notifications WHERE user_id = @UserId;
        """;
}
```

### Migrations

```sql
-- Notifications.Infrastructure/Persistence/Migrations/001_create_notifications.sql
CREATE TABLE notifications (
    id          uuid        PRIMARY KEY,
    user_id     text        NOT NULL,
    type        text        NOT NULL,
    title       text        NOT NULL,
    body        text        NOT NULL,
    payload     jsonb       NOT NULL,
    created_at  timestamptz NOT NULL,
    read_at     timestamptz NULL
);

CREATE INDEX ix_notifications_user_created
    ON notifications (user_id, created_at DESC);
```

In the `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Persistence\Migrations\*.sql" />
</ItemGroup>
```

### DbUp runner

See `backend-persistence-styles.md` §5 — runs at host startup in Development; the
`*.Migrator` console project runs it in CI/prod.

### DI registration

```csharp
// Notifications.Infrastructure/DependencyInjection.cs
internal static class InfrastructureDependencyInjection
{
    internal static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Notifications")
            ?? throw new InvalidOperationException("Missing Notifications connection string.");

        services.AddSingleton(_ =>
            new NpgsqlDataSourceBuilder(connectionString).Build());

        services.AddScoped<NotificationsConnectionFactory>();
        services.AddScoped<DapperUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DapperUnitOfWork>());

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationChannelPreferencesRepository,
                           NotificationChannelPreferencesRepository>();

        // Inbox executor for incoming integration events
        services.AddDapperInbox<NotificationsConnectionFactory>();

        return services;
    }
}
```

The host project still calls `services.AddNotificationsModule(...)` — exactly the
same module-registration shape as Style 1.

---

## Layer dependency rules

Same as `backend-module-structure.md`. To reiterate the cross-module portion:

```
Cross-module (OK):    ModuleA.Application → ModuleB.Contracts
Cross-module (NEVER): ModuleA.Application → ModuleB.Domain
Cross-module (NEVER): ModuleA.Application → ModuleB.Infrastructure
Cross-module (NEVER): ModuleA.Infrastructure → ModuleB.Infrastructure (shared connection)
```

A Style-2 module's connection pool is its own — no other module ever resolves
`NotificationsConnectionFactory`.

---

## Things explicitly forbidden in Style-2 modules

- Inline SQL strings inside repository methods. SQL belongs in `Sql/*.cs` constants.
- Reaching for EF Core "just for one entity." If you want a `DbContext`, you picked
  the wrong style — see `backend-persistence-styles.md` decision checklist.
- Hand-rolled connection lifetime management in handlers. Always go through
  `DapperUnitOfWork`.
- String concatenation in SQL. Always use Dapper named parameters (`@UserId`).
- Returning domain models from query handlers. Same rule as Style 1 — DTOs only.
