# N8 — WaterReminderJob + Templates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `WaterReminderJob` to the `DietReminderTickService` pipeline so users receive periodic water-intake reminders that respect their configured time window, interval, and dedup state — and wire in the corresponding Notifications handler and Polish template.

**Architecture:** The job follows the same `IDietReminderJob` pattern as `MealReminderJob`: a new `IWaterReminderCandidateQueries` interface (implemented via EF in Infrastructure) returns users with water reminders enabled along with their current dedup state; the job filters candidates in memory for window/interval conditions, publishes `WaterReminderDueIntegrationEvent` via `IIntegrationEventBus`, and upserts `water_reminder_state` rows before calling `IUnitOfWork.CommitAsync`. The Notifications side adds `WaterReminderDueIntegrationEventHandler` (delegates to `INotificationDispatcher`) and a Polish locale template.

**Tech Stack:** .NET 10, C# 13, EF Core 10 (DietPlanner), xUnit + Shouldly + NSubstitute, `dotnet ef migrations add`

---

## File Map

### Created
| File | Responsibility |
|---|---|
| `DietPlanner.Domain/Ledgers/WaterReminderState.cs` | Dedup ledger: `UserId` (PK) + `LastWaterReminderAt` |
| `DietPlanner.Domain/Repositories/IWaterReminderStateRepository.cs` | Repository contract |
| `DietPlanner.Application/Workers/WaterReminderCandidate.cs` | Query result record |
| `DietPlanner.Application/Workers/IWaterReminderCandidateQueries.cs` | Query contract |
| `DietPlanner.Application/Workers/WaterReminderJob.cs` | `IDietReminderJob` implementation |
| `DietPlanner.Infrastructure/Persistence/Configurations/WaterReminderStateConfiguration.cs` | EF mapping for `water_reminder_state` |
| `DietPlanner.Infrastructure/Persistence/Repositories/WaterReminderStateRepository.cs` | EF implementation of repository |
| `DietPlanner.Infrastructure/Workers/WaterReminderCandidateQueries.cs` | EF implementation of candidate queries |
| `Notifications.Application/EventHandlers/WaterReminderDueIntegrationEventHandler.cs` | Maps event → `INotificationDispatcher.DispatchAsync` |
| `DietPlanner.UnitTests/Application/Workers/WaterReminderJobTests.cs` | Unit tests for job logic |
| `Notifications.UnitTests/EventHandlers/WaterReminderDueIntegrationEventHandlerTests.cs` | Unit test for handler |

### Modified
| File | Change |
|---|---|
| `DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs` | Add `DbSet<WaterReminderState>` |
| `DietPlanner.Infrastructure/DependencyInjection.cs` | Register new repository, query, and job |
| `Notifications.Application/Templates/NotificationTemplateRegistry.cs` | Add `WaterReminder` Polish template |
| `Notifications.Infrastructure/DependencyInjection.cs` | Register `WaterReminderDueIntegrationEventHandler` |

### Generated
| File | How |
|---|---|
| `DietPlanner.Infrastructure/Migrations/<timestamp>_AddWaterReminderStateLedger.cs` | `dotnet ef migrations add` |

---

## Task 1: Domain ledger + repository interface

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Domain/Ledgers/WaterReminderState.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Domain/Repositories/IWaterReminderStateRepository.cs`

- [ ] **Step 1.1 — Write `WaterReminderState`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Domain/Ledgers/WaterReminderState.cs
namespace DietPlanner.Domain.Ledgers;

/// <summary>
/// Dedup ledger for water reminder publication. Keyed on UserId; updated every time
/// a reminder fires. Filed under Domain/Ledgers/ — not a DDD aggregate root.
/// </summary>
public sealed class WaterReminderState
{
    private WaterReminderState() { }

    public static WaterReminderState Create(string userId, DateTime lastReminderAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WaterReminderState
        {
            UserId = userId,
            LastWaterReminderAt = lastReminderAt
        };
    }

    public string UserId { get; private set; } = default!;
    public DateTime? LastWaterReminderAt { get; private set; }

    public void UpdateLastReminderAt(DateTime sentAt)
        => LastWaterReminderAt = sentAt;
}
```

- [ ] **Step 1.2 — Write `IWaterReminderStateRepository`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Domain/Repositories/IWaterReminderStateRepository.cs
namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;

public interface IWaterReminderStateRepository
{
    Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(WaterReminderState state, CancellationToken ct = default);
}
```

---

## Task 2: Application-layer query contract + candidate record + job

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderCandidate.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/IWaterReminderCandidateQueries.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderJob.cs`

- [ ] **Step 2.1 — Write `WaterReminderCandidate` record**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderCandidate.cs
namespace DietPlanner.Application.Workers;

internal sealed record WaterReminderCandidate(
    string UserId,
    string Locale,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStartUtc,
    TimeOnly WaterWindowEndUtc,
    DateTime? LastWaterReminderAt);
```

- [ ] **Step 2.2 — Write `IWaterReminderCandidateQueries`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/IWaterReminderCandidateQueries.cs
namespace DietPlanner.Application.Workers;

internal interface IWaterReminderCandidateQueries
{
    /// <summary>
    /// Returns users with WaterRemindersEnabled = true, including their current
    /// water_reminder_state (LastWaterReminderAt = null if never sent).
    /// Window/interval filtering is performed in the job.
    /// </summary>
    Task<IReadOnlyList<WaterReminderCandidate>> GetCandidatesAsync(
        DateTime nowUtc, CancellationToken ct);
}
```

- [ ] **Step 2.3 — Write `WaterReminderJob`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderJob.cs
namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class WaterReminderJob(
    IWaterReminderCandidateQueries queries,
    IWaterReminderStateRepository stateRepo,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork) : IDietReminderJob
{
    public string Name => "WaterReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        IReadOnlyList<WaterReminderCandidate> candidates = await queries.GetCandidatesAsync(nowUtc, ct);

        var nowTime = TimeOnly.FromDateTime(nowUtc);
        var publishedAny = false;

        foreach (var c in candidates)
        {
            if (!IsInWindow(nowTime, c.WaterWindowStartUtc, c.WaterWindowEndUtc))
                continue;

            if (!IsIntervalPassed(nowUtc, c.LastWaterReminderAt, c.WaterReminderIntervalMinutes))
                continue;

            await bus.PublishAsync(new WaterReminderDueIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
                Locale: c.Locale), ct);

            var existing = await stateRepo.GetByUserIdAsync(c.UserId, ct);
            if (existing is null)
                await stateRepo.AddAsync(WaterReminderState.Create(c.UserId, nowUtc), ct);
            else
                existing.UpdateLastReminderAt(nowUtc);

            publishedAny = true;
        }

        if (publishedAny)
            await unitOfWork.CommitAsync(ct);
    }

    private static bool IsInWindow(TimeOnly now, TimeOnly start, TimeOnly end)
        => now >= start && now < end;

    private static bool IsIntervalPassed(DateTime nowUtc, DateTime? lastAt, int intervalMinutes)
        => lastAt is null || lastAt.Value.AddMinutes(intervalMinutes) <= nowUtc;
}
```

---

## Task 3: Unit tests for `WaterReminderJob`

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/WaterReminderJobTests.cs`

- [ ] **Step 3.1 — Write failing tests**

```csharp
// src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/WaterReminderJobTests.cs
namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

public sealed class WaterReminderJobTests
{
    private readonly IWaterReminderCandidateQueries _queries =
        Substitute.For<IWaterReminderCandidateQueries>();
    private readonly IWaterReminderStateRepository _stateRepo =
        Substitute.For<IWaterReminderStateRepository>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly WaterReminderJob _sut;

    // 12:00 UTC — inside default window 06:00–22:00, interval=60 min
    private static readonly DateTime Now = new(2026, 4, 29, 12, 0, 0, DateTimeKind.Utc);

    private static WaterReminderCandidate MakeCandidate(
        string userId = "u1",
        int intervalMinutes = 60,
        string windowStart = "06:00",
        string windowEnd = "22:00",
        DateTime? lastAt = null)
        => new(userId, "en", intervalMinutes,
            TimeOnly.Parse(windowStart), TimeOnly.Parse(windowEnd), lastAt);

    public WaterReminderJobTests()
        => _sut = new WaterReminderJob(_queries, _stateRepo, _bus, _uow);

    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("WaterReminderJob");

    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenOutsideTimeWindow_SkipsCandidate()
    {
        // Now = 12:00, window = 14:00–22:00 → outside
        var candidate = MakeCandidate(windowStart: "14:00", windowEnd: "22:00");
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenIntervalNotYetPassed_SkipsCandidate()
    {
        // last reminder 30 min ago, interval = 60 min → too soon
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-30));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenFirstReminderEver_PublishesAndCreatesNewState()
    {
        var candidate = MakeCandidate(lastAt: null);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e =>
                e.UserId == "u1" && e.Locale == "en"),
            Arg.Any<CancellationToken>());

        await _stateRepo.Received(1).AddAsync(
            Arg.Is<WaterReminderState>(s => s.UserId == "u1" && s.LastWaterReminderAt == Now),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenIntervalPassed_PublishesAndUpdatesExistingState()
    {
        var lastAt = Now.AddMinutes(-61);
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: lastAt);
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);

        var existing = WaterReminderState.Create("u1", lastAt);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(existing);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<WaterReminderDueIntegrationEvent>(e => e.UserId == "u1"),
            Arg.Any<CancellationToken>());

        // No AddAsync — state was updated in place
        await _stateRepo.DidNotReceive().AddAsync(Arg.Any<WaterReminderState>(), Arg.Any<CancellationToken>());
        existing.LastWaterReminderAt.ShouldBe(Now);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ExactlyAtIntervalBoundary_IsEligible()
    {
        // last at exactly Now - interval → eligible (boundary inclusive)
        var candidate = MakeCandidate(intervalMinutes: 60, lastAt: Now.AddMinutes(-60));
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>()).Returns([candidate]);
        _stateRepo.GetByUserIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns(WaterReminderState.Create("u1", Now.AddMinutes(-60)));

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_MultipleEligibleUsers_CommitsOnce()
    {
        _queries.GetCandidatesAsync(Now, Arg.Any<CancellationToken>())
            .Returns([MakeCandidate("u1"), MakeCandidate("u2")]);
        _stateRepo.GetByUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((WaterReminderState?)null);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(2).PublishAsync(
            Arg.Any<WaterReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 3.2 — Run tests and confirm they fail (types not yet compiled)**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~WaterReminderJob" --no-build 2>&1 | tail -20
```

Expected: build error — `WaterReminderJob`, `IWaterReminderCandidateQueries`, etc. do not exist yet.

---

## Task 4: EF configuration + repository implementations

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/WaterReminderStateConfiguration.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/WaterReminderStateRepository.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/WaterReminderCandidateQueries.cs`
- Modify: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs`

- [ ] **Step 4.1 — Write EF configuration**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/WaterReminderStateConfiguration.cs
namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class WaterReminderStateConfiguration : IEntityTypeConfiguration<WaterReminderState>
{
    public void Configure(EntityTypeBuilder<WaterReminderState> builder)
    {
        builder.ToTable("water_reminder_state");

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.Property(x => x.LastWaterReminderAt)
            .HasColumnName("last_water_reminder_at");
    }
}
```

- [ ] **Step 4.2 — Add `DbSet<WaterReminderState>` to `DietPlannerDbContext`**

In `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs`, add after the `SentMealReminders` property:

```csharp
public DbSet<WaterReminderState> WaterReminderStates => Set<WaterReminderState>();
```

- [ ] **Step 4.3 — Write `WaterReminderStateRepository`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/WaterReminderStateRepository.cs
namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterReminderStateRepository(DietPlannerDbContext dbContext)
    : IWaterReminderStateRepository
{
    public Task<WaterReminderState?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => dbContext.WaterReminderStates
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task AddAsync(WaterReminderState state, CancellationToken ct = default)
        => await dbContext.WaterReminderStates.AddAsync(state, ct);
}
```

- [ ] **Step 4.4 — Write `WaterReminderCandidateQueries`**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/WaterReminderCandidateQueries.cs
namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class WaterReminderCandidateQueries(DietPlannerDbContext dbContext)
    : IWaterReminderCandidateQueries
{
    private const string DefaultLocale = "en";

    public async Task<IReadOnlyList<WaterReminderCandidate>> GetCandidatesAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var rows = await (
            from settings in dbContext.DietReminderSettings.AsNoTracking()
            where settings.WaterRemindersEnabled
            join state in dbContext.WaterReminderStates.AsNoTracking()
                on settings.UserId equals state.UserId into stateJoin
            from state in stateJoin.DefaultIfEmpty()
            select new
            {
                settings.UserId,
                settings.WaterReminderIntervalMinutes,
                settings.WaterWindowStartUtc,
                settings.WaterWindowEndUtc,
                LastAt = state == null ? (DateTime?)null : state.LastWaterReminderAt
            }).ToListAsync(ct);

        return rows
            .Select(r => new WaterReminderCandidate(
                r.UserId,
                DefaultLocale,
                r.WaterReminderIntervalMinutes,
                r.WaterWindowStartUtc,
                r.WaterWindowEndUtc,
                r.LastAt))
            .ToList();
    }
}
```

---

## Task 5: EF migration

**Files:**
- Generated: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/<timestamp>_AddWaterReminderStateLedger.cs`

- [ ] **Step 5.1 — Create migration**

```bash
dotnet ef migrations add AddWaterReminderStateLedger \
  --project src/Modules/DietPlanner/DietPlanner.Infrastructure \
  --startup-project src/Apis/HomeSystem.REST
```

Expected output: `Build succeeded. Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 5.2 — Verify migration content**

Open the generated migration file. Verify it contains:
```csharp
migrationBuilder.CreateTable(
    name: "water_reminder_state",
    columns: table => new
    {
        user_id = table.Column<string>(nullable: false),
        last_water_reminder_at = table.Column<DateTime>(nullable: true)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_water_reminder_state", x => x.user_id);
    });
```

If the column names differ (EF uses PascalCase), the `WaterReminderStateConfiguration` is not being picked up — ensure `ApplyConfigurationsFromAssembly` is called in `DietPlannerDbContext.OnModelCreating`.

---

## Task 6: DI registration (DietPlanner)

**Files:**
- Modify: `src/Modules/DietPlanner/DietPlanner.Infrastructure/DependencyInjection.cs`

- [ ] **Step 6.1 — Register new repository, query, and job**

Add after the `ISentMealReminderRepository` line:

```csharp
// Water-reminder ledger + read-side
services.AddScoped<IWaterReminderStateRepository, WaterReminderStateRepository>();
services.AddScoped<IWaterReminderCandidateQueries, WaterReminderCandidateQueries>();

// Register WaterReminderJob alongside MealReminderJob
services.AddScoped<IDietReminderJob, WaterReminderJob>();
```

The final block of diet-reminder registrations should look like:

```csharp
// Meal-reminder ledger + read-side
services.AddScoped<ISentMealReminderRepository, SentMealReminderRepository>();
services.AddScoped<IMealReminderCandidateQueries, MealReminderCandidateQueries>();

// Water-reminder ledger + read-side
services.AddScoped<IWaterReminderStateRepository, WaterReminderStateRepository>();
services.AddScoped<IWaterReminderCandidateQueries, WaterReminderCandidateQueries>();

// Diet reminder jobs (registered as IDietReminderJob; resolved per tick)
services.AddScoped<IDietReminderJob, MealReminderJob>();
services.AddScoped<IDietReminderJob, WaterReminderJob>();
```

---

## Task 7: Build and run unit tests for `WaterReminderJob`

- [ ] **Step 7.1 — Build DietPlanner**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Application/DietPlanner.Application.csproj && \
dotnet build src/Modules/DietPlanner/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7.2 — Run `WaterReminderJobTests`**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~WaterReminderJobTests"
```

Expected: all 7 tests pass.

- [ ] **Step 7.3 — Commit DietPlanner changes**

```bash
git add \
  src/Modules/DietPlanner/DietPlanner.Domain/Ledgers/WaterReminderState.cs \
  src/Modules/DietPlanner/DietPlanner.Domain/Repositories/IWaterReminderStateRepository.cs \
  src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderCandidate.cs \
  src/Modules/DietPlanner/DietPlanner.Application/Workers/IWaterReminderCandidateQueries.cs \
  src/Modules/DietPlanner/DietPlanner.Application/Workers/WaterReminderJob.cs \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/WaterReminderStateConfiguration.cs \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/WaterReminderStateRepository.cs \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/WaterReminderCandidateQueries.cs \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/ \
  src/Modules/DietPlanner/DietPlanner.Infrastructure/DependencyInjection.cs \
  src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/WaterReminderJobTests.cs
git commit -m "feat(diet-planner): add WaterReminderJob with dedup state ledger (#N8)"
```

---

## Task 8: Notifications — handler + Polish template

**Files:**
- Create: `src/Modules/Notifications/Notifications.Application/EventHandlers/WaterReminderDueIntegrationEventHandler.cs`
- Modify: `src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs`

- [ ] **Step 8.1 — Write `WaterReminderDueIntegrationEventHandler`**

```csharp
// src/Modules/Notifications/Notifications.Application/EventHandlers/WaterReminderDueIntegrationEventHandler.cs
namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class WaterReminderDueIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<WaterReminderDueIntegrationEvent>
{
    public Task HandleAsync(WaterReminderDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var payload = JsonSerializer.Serialize(new { @event.UserId });

        return dispatcher.DispatchAsync(
            NotificationType.WaterReminder,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders: new Dictionary<string, string>(),
            ct);
    }
}
```

- [ ] **Step 8.2 — Add Polish template for `WaterReminder`**

In `src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs`, add a Polish entry after the existing `WaterReminder "en"` entry:

```csharp
[(NotificationType.WaterReminder, "pl")] =
    new(NotificationType.WaterReminder, "pl",
        "Przerwa na wodę",
        "Czas na wypicie wody."),
```

The relevant section of the dictionary after the change:

```csharp
[(NotificationType.WaterReminder, "en")] =
    new(NotificationType.WaterReminder, "en",
        "Water break",
        "Time to drink some water."),
[(NotificationType.WaterReminder, "pl")] =
    new(NotificationType.WaterReminder, "pl",
        "Przerwa na wodę",
        "Czas na wypicie wody."),
```

---

## Task 9: Unit test for `WaterReminderDueIntegrationEventHandler`

**Files:**
- Create: `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/WaterReminderDueIntegrationEventHandlerTests.cs`

- [ ] **Step 9.1 — Write failing test**

```csharp
// src/Modules/Notifications/Notifications.UnitTests/EventHandlers/WaterReminderDueIntegrationEventHandlerTests.cs
namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class WaterReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WaterReminderDueIntegrationEventHandler _sut;

    public WaterReminderDueIntegrationEventHandlerTests()
        => _sut = new WaterReminderDueIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesWaterReminderTypeWithCorrectUserAndLocale()
    {
        var @event = new WaterReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            UserId: "u42",
            Locale: "pl");

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.WaterReminder,
            "u42",
            "pl",
            Arg.Is<string>(p => p.Contains("u42")),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEventIsNull_Throws()
    {
        var act = async () => await _sut.HandleAsync(null!, CancellationToken.None);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }
}
```

- [ ] **Step 9.2 — Run tests to confirm they fail**

```bash
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj \
  --filter "FullyQualifiedName~WaterReminderDueIntegrationEventHandlerTests" --no-build 2>&1 | tail -10
```

Expected: build error — `WaterReminderDueIntegrationEventHandler` does not exist yet.

---

## Task 10: DI registration (Notifications)

**Files:**
- Modify: `src/Modules/Notifications/Notifications.Infrastructure/DependencyInjection.cs`

- [ ] **Step 10.1 — Register the new handler**

Add after the `GoalMilestoneReachedIntegrationEvent` registration:

```csharp
services.AddScoped<
    IIntegrationEventHandler<WaterReminderDueIntegrationEvent>,
    WaterReminderDueIntegrationEventHandler>();
```

The handler registration block should look like:

```csharp
services.AddScoped<
    IIntegrationEventHandler<MealReminderDueIntegrationEvent>,
    MealReminderDueIntegrationEventHandler>();
services.AddScoped<
    IIntegrationEventHandler<MealMissedIntegrationEvent>,
    MealMissedIntegrationEventHandler>();
services.AddScoped<
    IIntegrationEventHandler<GoalMilestoneReachedIntegrationEvent>,
    GoalMilestoneReachedIntegrationEventHandler>();
services.AddScoped<
    IIntegrationEventHandler<WaterReminderDueIntegrationEvent>,
    WaterReminderDueIntegrationEventHandler>();
```

---

## Task 11: Build + run all Notifications unit tests

- [ ] **Step 11.1 — Build Notifications**

```bash
dotnet build src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj && \
dotnet build src/Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 11.2 — Run all Notifications unit tests**

```bash
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj
```

Expected: all tests pass (including the new `WaterReminderDueIntegrationEventHandlerTests`).

- [ ] **Step 11.3 — Commit Notifications changes**

```bash
git add \
  src/Modules/Notifications/Notifications.Application/EventHandlers/WaterReminderDueIntegrationEventHandler.cs \
  src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs \
  src/Modules/Notifications/Notifications.Infrastructure/DependencyInjection.cs \
  src/Modules/Notifications/Notifications.UnitTests/EventHandlers/WaterReminderDueIntegrationEventHandlerTests.cs
git commit -m "feat(notifications): add WaterReminderDueIntegrationEventHandler + pl template (#N8)"
```

---

## Task 12: Full solution build + smoke run

- [ ] **Step 12.1 — Build entire solution**

```bash
dotnet build HomeSystem.slnx
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s)`.

- [ ] **Step 12.2 — Run both module test suites**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj && \
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj
```

Expected: all tests pass.

---

## Self-Review Checklist

**Spec coverage check:**
- ✅ `WaterReminderJob` fires within `WaterWindowStartUtc`–`WaterWindowEndUtc` window (Task 2, `IsInWindow`)
- ✅ Interval dedup via `water_reminder_state.last_water_reminder_at` (Tasks 1, 4, 5)
- ✅ Publishes `WaterReminderDueIntegrationEvent` (Task 2)
- ✅ Dedup state created on first reminder, updated on subsequent (Task 2 job + Task 4 repo)
- ✅ Job registered as `IDietReminderJob` — picked up by `DietReminderTickService` (Task 6)
- ✅ Notifications handler maps to `NotificationType.WaterReminder` (Task 8)
- ✅ Polish locale template (Task 8)
- ✅ English locale template already existed (verified in `NotificationTemplateRegistry`)
- ✅ Unit tests for job (7 cases including window, interval, boundary, multi-user) (Task 3)
- ✅ Unit tests for handler (Task 9)

**No placeholders scan:** all code blocks are complete; no TBD/TODO.

**Type consistency:**
- `WaterReminderState.Create(userId, lastReminderAt)` used consistently in Task 1 definition and Task 3 tests.
- `WaterReminderState.LastWaterReminderAt` property name consistent across Task 1, Task 3 test assertions, and Task 4 EF mapping.
- `IWaterReminderCandidateQueries.GetCandidatesAsync` signature consistent across Task 2 interface and Task 4 implementation.
- `WaterReminderCandidate` constructor order consistent across Task 2 definition and Task 4 instantiation.
