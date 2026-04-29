# N7 — DietReminderTickService + Meal Reminder/Missed Jobs — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish `MealReminderDue` and `MealMissed` integration events from DietPlanner on a 60s tick, dedup at source via a ledger table, and have Notifications consume them through an idempotent inbox to dispatch user-facing notifications. Also closes the gap from N6 (#191) by registering the missing `GoalMilestoneReached` consumer.

**Architecture:** A `DietReminderTickService` `BackgroundService` invokes each registered `IDietReminderJob` per minute in a fresh DI scope. `MealReminderJob` queries planned meals + slot defaults to compute `PlannedAt`, filters via the `sent_meal_reminders` ledger, publishes one event per candidate, and inserts the ledger row in the same UoW transaction. The Notifications module wires `AddDapperInbox<NotificationsConnectionFactory>()` for the first time and registers three `IIntegrationEventHandler<T>` implementations that delegate to `INotificationDispatcher`.

**Tech Stack:** .NET 10 / EF Core 10 / Npgsql / Dapper / xUnit + Shouldly + NSubstitute / DbUp.

**Issue:** [#157](https://github.com/piotrkantorowicz/home-system/issues/157)
**Branch:** `feat/157-meal-reminder-jobs`

---

## File Structure

### DietPlanner (publisher side)

| Path | Action | Purpose |
|---|---|---|
| `src/Modules/DietPlanner/DietPlanner.Domain/Aggregates/SentMealReminder.cs` | Create | POCO ledger entity (composite PK on `MealEntryId` + `Kind`) |
| `src/Modules/DietPlanner/DietPlanner.Domain/ValueObjects/MealReminderKind.cs` | Create | Enum `Reminder \| Missed` |
| `src/Modules/DietPlanner/DietPlanner.Domain/Repositories/ISentMealReminderRepository.cs` | Create | `ExistsAsync` + `AddAsync` |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/IDietReminderJob.cs` | Create | Job interface (`Name`, `RunAsync(nowUtc, ct)`) |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderCandidate.cs` | Create | DTO read by the job |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/IMealReminderCandidateQueries.cs` | Create | Read-side abstraction over candidates |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderJob.cs` | Create | Orchestrator: load → publish → ledger → commit |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickService.cs` | Create | `BackgroundService` ticker |
| `src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickServiceOptions.cs` | Create | Bound options (interval, enabled flag) |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/SentMealReminderConfiguration.cs` | Create | EF mapping for the ledger table |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/SentMealReminderRepository.cs` | Create | EF repo impl |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/MealReminderCandidateQueries.cs` | Create | EF read-side impl over `IDietPlannerReadDbContext` |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs` | Modify | Add `DbSet<SentMealReminder>` |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/<timestamp>_AddSentMealRemindersLedger.cs` | Generate | EF migration |
| `src/Modules/DietPlanner/DietPlanner.Infrastructure/DependencyInjection.cs` | Modify | Register repo, queries, jobs, hosted service, options |

### Notifications (consumer side)

| Path | Action | Purpose |
|---|---|---|
| `src/Modules/Notifications/Notifications.Application/EventHandlers/MealReminderDueIntegrationEventHandler.cs` | Create | Delegates to dispatcher |
| `src/Modules/Notifications/Notifications.Application/EventHandlers/MealMissedIntegrationEventHandler.cs` | Create | Delegates to dispatcher |
| `src/Modules/Notifications/Notifications.Application/EventHandlers/GoalMilestoneReachedIntegrationEventHandler.cs` | Create | Closes N6 gap |
| `src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj` | Modify | Add ProjectReference to `DietPlanner.Contracts` |
| `src/Modules/Notifications/Notifications.Infrastructure/DependencyInjection.cs` | Modify | `AddDapperInbox<NotificationsConnectionFactory>()` + register the 3 handlers |
| `src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs` | Modify | Final wording + `pl` locale variants for MealReminder/MealMissed |

### Tests

| Path | Action |
|---|---|
| `src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/MealReminderJobTests.cs` | Create |
| `src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/DietReminderTickServiceTests.cs` | Create |
| `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealReminderDueIntegrationEventHandlerTests.cs` | Create |
| `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealMissedIntegrationEventHandlerTests.cs` | Create |
| `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/GoalMilestoneReachedIntegrationEventHandlerTests.cs` | Create |
| `src/Modules/DietPlanner/DietPlanner.IntegrationTests/Workers/MealReminderJobIntegrationTests.cs` | Create |

---

## Pre-flight

- [ ] **Step P1: Confirm clean working tree and create branch**

```bash
git status
git checkout -b feat/157-meal-reminder-jobs
```

Expected: branch created from `main`, no untracked files relevant to this work.

- [ ] **Step P2: Confirm baseline build is green**

```bash
dotnet build HomeSystem.slnx
```

Expected: build succeeds with no errors.

---

## Task 1: SentMealReminder ledger — Domain pieces

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Domain/ValueObjects/MealReminderKind.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Domain/Aggregates/SentMealReminder.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Domain/Repositories/ISentMealReminderRepository.cs`

- [ ] **Step 1.1: Create the kind enum**

```csharp
// src/Modules/DietPlanner/DietPlanner.Domain/ValueObjects/MealReminderKind.cs
namespace DietPlanner.Domain.ValueObjects;

public enum MealReminderKind
{
    Reminder = 1,
    Missed = 2
}
```

- [ ] **Step 1.2: Create the ledger entity**

```csharp
// src/Modules/DietPlanner/DietPlanner.Domain/Aggregates/SentMealReminder.cs
namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;

public sealed class SentMealReminder
{
    private SentMealReminder() { }

    public static SentMealReminder Create(MealEntryId mealEntryId, MealReminderKind kind, DateTime sentAtUtc)
    {
        ArgumentNullException.ThrowIfNull(mealEntryId);
        return new SentMealReminder
        {
            MealEntryId = mealEntryId,
            Kind = kind,
            SentAt = sentAtUtc
        };
    }

    public MealEntryId MealEntryId { get; private set; } = default!;
    public MealReminderKind Kind { get; private set; }
    public DateTime SentAt { get; private set; }
}
```

> Note: not an `AggregateRoot`. It's a flat ledger row, but kept under `Aggregates/` to match the module's existing folder style. No domain events.

- [ ] **Step 1.3: Create the repository interface**

```csharp
// src/Modules/DietPlanner/DietPlanner.Domain/Repositories/ISentMealReminderRepository.cs
namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface ISentMealReminderRepository
{
    Task<bool> ExistsAsync(MealEntryId mealEntryId, MealReminderKind kind, CancellationToken ct = default);
    Task AddAsync(SentMealReminder ledgerRow, CancellationToken ct = default);
}
```

- [ ] **Step 1.4: Build to verify**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Domain/DietPlanner.Domain.csproj
```

Expected: success.

- [ ] **Step 1.5: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Domain/
git commit -m "feat(diet-planner): add sent_meal_reminders ledger entity and repository contract"
```

---

## Task 2: EF mapping + DbContext + migration for `sent_meal_reminders`

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/SentMealReminderConfiguration.cs`
- Modify: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs`
- Generate: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/<timestamp>_AddSentMealRemindersLedger.cs`

- [ ] **Step 2.1: Add the EF configuration**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/SentMealReminderConfiguration.cs
namespace DietPlanner.Infrastructure.Persistence.Configurations;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class SentMealReminderConfiguration : IEntityTypeConfiguration<SentMealReminder>
{
    public void Configure(EntityTypeBuilder<SentMealReminder> builder)
    {
        builder.ToTable("sent_meal_reminders");

        builder.HasKey(x => new { x.MealEntryId, x.Kind });

        builder.Property(x => x.MealEntryId)
            .HasConversion(id => id.Value, value => MealEntryId.From(value))
            .HasColumnName("meal_entry_id");

        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasColumnName("kind");

        builder.Property(x => x.SentAt)
            .HasColumnName("sent_at");
    }
}
```

> `MealEntryId.From(Guid)` already exists — verify by opening `DietPlanner.Domain/ValueObjects/MealEntryId.cs`. If the static factory is named differently (e.g. `Create`), use the existing convention.

- [ ] **Step 2.2: Register the DbSet**

Open `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs` and add a `DbSet<SentMealReminder>` next to existing sets:

```csharp
public DbSet<SentMealReminder> SentMealReminders => Set<SentMealReminder>();
```

Add `using DietPlanner.Domain.Aggregates;` if not already present.

- [ ] **Step 2.3: Generate the migration**

```bash
dotnet ef migrations add AddSentMealRemindersLedger \
  --project src/Modules/DietPlanner/DietPlanner.Infrastructure \
  --startup-project src/Apis/HomeSystem.REST
```

Expected: a new file under `src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/<timestamp>_AddSentMealRemindersLedger.cs` plus designer/snapshot updates. Inspect the generated `Up` and verify:
- `sent_meal_reminders` table with `meal_entry_id uuid`, `kind varchar(16)`, `sent_at timestamptz`
- composite primary key on `(meal_entry_id, kind)`

- [ ] **Step 2.4: Build to verify**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj
```

Expected: success.

- [ ] **Step 2.5: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Configurations/SentMealReminderConfiguration.cs \
        src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/DietPlannerDbContext.cs \
        src/Modules/DietPlanner/DietPlanner.Infrastructure/Migrations/
git commit -m "feat(diet-planner): map sent_meal_reminders ledger and add migration"
```

---

## Task 3: SentMealReminderRepository (EF impl)

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/SentMealReminderRepository.cs`

- [ ] **Step 3.1: Implement the repository**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/SentMealReminderRepository.cs
namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class SentMealReminderRepository(DietPlannerDbContext dbContext) : ISentMealReminderRepository
{
    public Task<bool> ExistsAsync(MealEntryId mealEntryId, MealReminderKind kind, CancellationToken ct = default)
        => dbContext.SentMealReminders
            .AsNoTracking()
            .AnyAsync(x => x.MealEntryId == mealEntryId && x.Kind == kind, ct);

    public async Task AddAsync(SentMealReminder ledgerRow, CancellationToken ct = default)
        => await dbContext.SentMealReminders.AddAsync(ledgerRow, ct);
}
```

- [ ] **Step 3.2: Build**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj
```

Expected: success.

- [ ] **Step 3.3: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Infrastructure/Persistence/Repositories/SentMealReminderRepository.cs
git commit -m "feat(diet-planner): add SentMealReminderRepository EF implementation"
```

---

## Task 4: Worker abstractions (job interface + candidate read-side)

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/IDietReminderJob.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderCandidate.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/IMealReminderCandidateQueries.cs`

- [ ] **Step 4.1: Job interface**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/IDietReminderJob.cs
namespace DietPlanner.Application.Workers;

public interface IDietReminderJob
{
    string Name { get; }
    Task RunAsync(DateTime nowUtc, CancellationToken ct);
}
```

> Public, not internal — the host project may need the type for diagnostics, and tests live in a separate assembly.

- [ ] **Step 4.2: Candidate DTO**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderCandidate.cs
namespace DietPlanner.Application.Workers;

public sealed record MealReminderCandidate(
    string UserId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAtUtc);
```

> `Locale` is included so the job doesn't need a second lookup. v1 source: hardcoded `"en"` constant in the query implementation, matching `GoalMilestoneEvaluator.DefaultLocale`. N17 (timezone/locale rework) replaces this.

- [ ] **Step 4.3: Read-side abstraction**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/IMealReminderCandidateQueries.cs
namespace DietPlanner.Application.Workers;

public interface IMealReminderCandidateQueries
{
    /// <summary>
    /// Returns meals where computed PlannedAt ∈ (nowUtc, nowUtc + lead]
    /// for users whose DietReminderSettings has MealRemindersEnabled = true,
    /// excluding rows already present in sent_meal_reminders with kind = Reminder.
    /// </summary>
    Task<IReadOnlyList<MealReminderCandidate>> GetDueRemindersAsync(DateTime nowUtc, CancellationToken ct);

    /// <summary>
    /// Returns meals where computed PlannedAt + grace ≤ nowUtc and PlannedAt > nowUtc - 24h
    /// and Status != Done, for users with MealRemindersEnabled = true,
    /// excluding rows already in sent_meal_reminders with kind = Missed.
    /// </summary>
    Task<IReadOnlyList<MealReminderCandidate>> GetMissedRemindersAsync(DateTime nowUtc, CancellationToken ct);
}
```

- [ ] **Step 4.4: Build**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Application/DietPlanner.Application.csproj
```

Expected: success.

- [ ] **Step 4.5: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Application/Workers/
git commit -m "feat(diet-planner): add diet-reminder job + meal candidate abstractions"
```

---

## Task 5: MealReminderCandidateQueries (EF impl)

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/MealReminderCandidateQueries.cs`

> The two queries share a join shape: `MealEntries ⋈ MealSlots ⋈ DietReminderSettings`. PlannedAt is computed as `Date.ToDateTime(MealEntry.MealTime ?? MealSlot.DefaultTime, DateTimeKind.Unspecified)` then treated as UTC. Backend operates entirely in UTC per the spec §2.

- [ ] **Step 5.1: Implement**

```csharp
// src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/MealReminderCandidateQueries.cs
namespace DietPlanner.Infrastructure.Workers;

using DietPlanner.Application.Persistence;
using DietPlanner.Application.Workers;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class MealReminderCandidateQueries(IDietPlannerReadDbContext dbContext)
    : IMealReminderCandidateQueries
{
    private const string DefaultLocale = "en";
    private static readonly TimeSpan MissedLookback = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<MealReminderCandidate>> GetDueRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var nowDate = DateOnly.FromDateTime(nowUtc);
        var horizonDate = DateOnly.FromDateTime(nowUtc.AddDays(1));

        var rows = await (
            from settings in dbContext.Set<DietReminderSettings>().AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.Set<MealEntry>().AsNoTracking()
                on settings.UserId equals entry.UserId
            join slot in dbContext.Set<MealSlot>().AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= nowDate && entry.Date <= horizonDate
                && !dbContext.Set<SentMealReminder>().AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Reminder)
            select new
            {
                settings.UserId,
                settings.MealReminderLeadTimeMinutes,
                EntryId = entry.Id,
                entry.Date,
                entry.MealTime,
                SlotName = slot.Name,
                SlotDefaultTime = slot.DefaultTime
            }).ToListAsync(ct);

        var result = new List<MealReminderCandidate>(rows.Count);
        foreach (var r in rows)
        {
            var time = r.MealTime ?? r.SlotDefaultTime;
            var plannedAt = DateTime.SpecifyKind(r.Date.ToDateTime(time), DateTimeKind.Utc);
            var leadEnd = nowUtc.AddMinutes(r.MealReminderLeadTimeMinutes);
            if (plannedAt > nowUtc && plannedAt <= leadEnd)
            {
                result.Add(new MealReminderCandidate(
                    r.UserId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<MealReminderCandidate>> GetMissedRemindersAsync(
        DateTime nowUtc, CancellationToken ct)
    {
        var lowerDate = DateOnly.FromDateTime(nowUtc.Subtract(MissedLookback));
        var upperDate = DateOnly.FromDateTime(nowUtc);

        var rows = await (
            from settings in dbContext.Set<DietReminderSettings>().AsNoTracking()
            where settings.MealRemindersEnabled
            join entry in dbContext.Set<MealEntry>().AsNoTracking()
                on settings.UserId equals entry.UserId
            join slot in dbContext.Set<MealSlot>().AsNoTracking()
                on entry.MealSlotId equals slot.Id
            where entry.Status == MealEntryStatus.Planned
                && entry.Date >= lowerDate && entry.Date <= upperDate
                && !dbContext.Set<SentMealReminder>().AsNoTracking()
                    .Any(s => s.MealEntryId == entry.Id && s.Kind == MealReminderKind.Missed)
            select new
            {
                settings.UserId,
                settings.MealMissedGraceMinutes,
                EntryId = entry.Id,
                entry.Date,
                entry.MealTime,
                SlotName = slot.Name,
                SlotDefaultTime = slot.DefaultTime
            }).ToListAsync(ct);

        var result = new List<MealReminderCandidate>(rows.Count);
        foreach (var r in rows)
        {
            var time = r.MealTime ?? r.SlotDefaultTime;
            var plannedAt = DateTime.SpecifyKind(r.Date.ToDateTime(time), DateTimeKind.Utc);
            var missedAt = plannedAt.AddMinutes(r.MealMissedGraceMinutes);
            if (missedAt <= nowUtc && plannedAt > nowUtc.Subtract(MissedLookback))
            {
                result.Add(new MealReminderCandidate(
                    r.UserId, DefaultLocale, r.EntryId.Value, r.SlotName, plannedAt));
            }
        }
        return result;
    }
}
```

> Why filter PlannedAt in memory after a coarse `Date` filter? PostgreSQL doesn't have a clean way to combine a `date` column with a nullable `time` column into a `timestamptz` inside an EF-translatable predicate. The day-window pre-filter keeps the candidate set small.

- [ ] **Step 5.2: Build**

```bash
dotnet build src/Modules/DietPlanner/DietPlanner.Infrastructure/DietPlanner.Infrastructure.csproj
```

Expected: success.

- [ ] **Step 5.3: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Infrastructure/Workers/
git commit -m "feat(diet-planner): EF implementation of meal reminder candidate queries"
```

---

## Task 6: MealReminderJob — TDD

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/MealReminderJobTests.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderJob.cs`

- [ ] **Step 6.1: Write the failing tests**

```csharp
// src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/MealReminderJobTests.cs
namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

public sealed class MealReminderJobTests
{
    private readonly IMealReminderCandidateQueries _queries = Substitute.For<IMealReminderCandidateQueries>();
    private readonly ISentMealReminderRepository _ledger = Substitute.For<ISentMealReminderRepository>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly MealReminderJob _sut;
    private static readonly DateTime Now = new(2026, 4, 28, 11, 30, 0, DateTimeKind.Utc);

    public MealReminderJobTests()
        => _sut = new MealReminderJob(_queries, _ledger, _bus, _uow);

    [Fact]
    public void Name_IsStable() => _sut.Name.ShouldBe("MealReminderJob");

    [Fact]
    public async Task RunAsync_WhenNoCandidates_DoesNothing()
    {
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealMissedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().AddAsync(Arg.Any<SentMealReminder>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PublishesReminderEventAndWritesLedger_PerDueCandidate()
    {
        var entryId = Guid.NewGuid();
        var planned = Now.AddMinutes(10);
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Lunch", planned)]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _ledger.ExistsAsync(Arg.Any<MealEntryId>(), MealReminderKind.Reminder, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MealReminderDueIntegrationEvent>(e =>
                e.UserId == "u1" && e.Locale == "en" &&
                e.MealEntryId == entryId && e.MealSlotName == "Lunch" &&
                e.PlannedAt == planned),
            Arg.Any<CancellationToken>());

        await _ledger.Received(1).AddAsync(
            Arg.Is<SentMealReminder>(l =>
                l.MealEntryId.Value == entryId && l.Kind == MealReminderKind.Reminder),
            Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_PublishesMissedEventAndWritesLedger_PerMissedCandidate()
    {
        var entryId = Guid.NewGuid();
        var planned = Now.AddHours(-2);
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Breakfast", planned)]);
        _ledger.ExistsAsync(Arg.Any<MealEntryId>(), MealReminderKind.Missed, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<MealMissedIntegrationEvent>(e =>
                e.MealEntryId == entryId && e.MealSlotName == "Breakfast" && e.PlannedAt == planned),
            Arg.Any<CancellationToken>());

        await _ledger.Received(1).AddAsync(
            Arg.Is<SentMealReminder>(l => l.Kind == MealReminderKind.Missed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenLedgerAlreadyHasEntry_SkipsCandidate()
    {
        var entryId = Guid.NewGuid();
        _queries.GetDueRemindersAsync(Now, Arg.Any<CancellationToken>())
            .Returns([new MealReminderCandidate("u1", "en", entryId, "Lunch", Now.AddMinutes(5))]);
        _queries.GetMissedRemindersAsync(Now, Arg.Any<CancellationToken>()).Returns([]);
        _ledger.ExistsAsync(Arg.Is<MealEntryId>(id => id.Value == entryId), MealReminderKind.Reminder,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _sut.RunAsync(Now, CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(Arg.Any<MealReminderDueIntegrationEvent>(), Arg.Any<CancellationToken>());
        await _ledger.DidNotReceive().AddAsync(Arg.Any<SentMealReminder>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 6.2: Run tests, verify all four fail with "MealReminderJob not found"**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~MealReminderJobTests"
```

Expected: build error or test failures referencing missing `MealReminderJob`.

- [ ] **Step 6.3: Implement MealReminderJob**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderJob.cs
namespace DietPlanner.Application.Workers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Messaging;

internal sealed class MealReminderJob(
    IMealReminderCandidateQueries queries,
    ISentMealReminderRepository ledger,
    IIntegrationEventBus bus,
    IUnitOfWork unitOfWork) : IDietReminderJob
{
    public string Name => "MealReminderJob";

    public async Task RunAsync(DateTime nowUtc, CancellationToken ct)
    {
        var due = await queries.GetDueRemindersAsync(nowUtc, ct);
        var missed = await queries.GetMissedRemindersAsync(nowUtc, ct);

        var publishedAny = false;

        foreach (var c in due)
        {
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Reminder, ct)) continue;

            await bus.PublishAsync(new MealReminderDueIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
                Locale: c.Locale,
                MealEntryId: c.MealEntryId,
                MealSlotName: c.MealSlotName,
                PlannedAt: c.PlannedAtUtc), ct);

            await ledger.AddAsync(SentMealReminder.Create(id, MealReminderKind.Reminder, nowUtc), ct);
            publishedAny = true;
        }

        foreach (var c in missed)
        {
            var id = MealEntryId.From(c.MealEntryId);
            if (await ledger.ExistsAsync(id, MealReminderKind.Missed, ct)) continue;

            await bus.PublishAsync(new MealMissedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: nowUtc,
                UserId: c.UserId,
                Locale: c.Locale,
                MealEntryId: c.MealEntryId,
                MealSlotName: c.MealSlotName,
                PlannedAt: c.PlannedAtUtc), ct);

            await ledger.AddAsync(SentMealReminder.Create(id, MealReminderKind.Missed, nowUtc), ct);
            publishedAny = true;
        }

        if (publishedAny)
            await unitOfWork.CommitAsync(ct);
    }
}
```

> Single commit per tick: outbox rows + ledger rows go through one `IUnitOfWork.CommitAsync`. Atomicity ensures we never publish without a ledger entry (or vice versa).

- [ ] **Step 6.4: Run tests, verify all four pass**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~MealReminderJobTests"
```

Expected: 4 passed.

- [ ] **Step 6.5: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Application/Workers/MealReminderJob.cs \
        src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/
git commit -m "feat(diet-planner): MealReminderJob publishes due + missed events with ledger dedup"
```

---

## Task 7: DietReminderTickService — TDD

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickServiceOptions.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/DietReminderTickServiceTests.cs`
- Create: `src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickService.cs`

- [ ] **Step 7.1: Add options class**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickServiceOptions.cs
namespace DietPlanner.Application.Workers;

public sealed class DietReminderTickServiceOptions
{
    public const string SectionName = "DietPlanner:DietReminderTick";
    public bool Enabled { get; init; } = true;
    public int TickIntervalSeconds { get; init; } = 60;
}
```

- [ ] **Step 7.2: Write failing tests**

The tick service is a `BackgroundService`. The unit-testable surface is its inner orchestration: per tick, resolve `IDietReminderJob` instances from a fresh scope and invoke each in try/catch. Extract that into a public `RunOnceAsync(CancellationToken)` method on the service (same pattern as `OutboxWorker.RunOnceAsync`).

```csharp
// src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/DietReminderTickServiceTests.cs
namespace DietPlanner.UnitTests.Application.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public sealed class DietReminderTickServiceTests
{
    [Fact]
    public async Task RunOnceAsync_InvokesEachRegisteredJob_WithUtcNowApproximately()
    {
        var jobA = new RecordingJob("A");
        var jobB = new RecordingJob("B");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => jobA);
        services.AddScoped<IDietReminderJob>(_ => jobB);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions()),
            NullLogger<DietReminderTickService>.Instance);

        var before = DateTime.UtcNow;
        await sut.RunOnceAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        jobA.Calls.Count.ShouldBe(1);
        jobB.Calls.Count.ShouldBe(1);
        jobA.Calls[0].ShouldBeInRange(before, after);
    }

    [Fact]
    public async Task RunOnceAsync_OneJobThrows_OtherStillRuns()
    {
        var failing = new ThrowingJob();
        var ok = new RecordingJob("ok");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => failing);
        services.AddScoped<IDietReminderJob>(_ => ok);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions()),
            NullLogger<DietReminderTickService>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        ok.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RunOnceAsync_WhenDisabled_DoesNothing()
    {
        var job = new RecordingJob("J");
        var services = new ServiceCollection();
        services.AddScoped<IDietReminderJob>(_ => job);
        var sp = services.BuildServiceProvider();

        var sut = new DietReminderTickService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DietReminderTickServiceOptions { Enabled = false }),
            NullLogger<DietReminderTickService>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        job.Calls.ShouldBeEmpty();
    }

    private sealed class RecordingJob(string name) : IDietReminderJob
    {
        public string Name { get; } = name;
        public List<DateTime> Calls { get; } = [];
        public Task RunAsync(DateTime nowUtc, CancellationToken ct)
        {
            Calls.Add(nowUtc);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingJob : IDietReminderJob
    {
        public string Name => "throw";
        public Task RunAsync(DateTime nowUtc, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }
}
```

- [ ] **Step 7.3: Run tests, verify they fail**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~DietReminderTickServiceTests"
```

Expected: build error referencing missing `DietReminderTickService`.

- [ ] **Step 7.4: Implement the tick service**

```csharp
// src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickService.cs
namespace DietPlanner.Application.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class DietReminderTickService(
    IServiceScopeFactory scopeFactory,
    IOptions<DietReminderTickServiceOptions> options,
    ILogger<DietReminderTickService> logger) : BackgroundService
{
    private readonly DietReminderTickServiceOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("DietReminderTickService disabled via configuration; not ticking.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(_options.TickIntervalSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DietReminderTickService tick failed");
            }

            try { await Task.Delay(interval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        if (!_options.Enabled) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var jobs = scope.ServiceProvider.GetServices<IDietReminderJob>();
        var nowUtc = DateTime.UtcNow;

        foreach (var job in jobs)
        {
            try
            {
                await job.RunAsync(nowUtc, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DietReminderJob {JobName} failed", job.Name);
            }
        }
    }
}
```

- [ ] **Step 7.5: Run tests, verify all three pass**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj \
  --filter "FullyQualifiedName~DietReminderTickServiceTests"
```

Expected: 3 passed.

- [ ] **Step 7.6: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickService.cs \
        src/Modules/DietPlanner/DietPlanner.Application/Workers/DietReminderTickServiceOptions.cs \
        src/Modules/DietPlanner/DietPlanner.UnitTests/Application/Workers/DietReminderTickServiceTests.cs
git commit -m "feat(diet-planner): DietReminderTickService BackgroundService with RunOnceAsync orchestrator"
```

---

## Task 8: Wire DietPlanner DI registrations

**Files:**
- Modify: `src/Modules/DietPlanner/DietPlanner.Infrastructure/DependencyInjection.cs`

- [ ] **Step 8.1: Register repository, queries, jobs, options, hosted service**

In `AddDietPlannerInfrastructure`, after the existing repository registrations and `services.AddCqrs<DietPlannerDbContext>(...)`, add:

```csharp
// Meal-reminder ledger + read-side
services.AddScoped<ISentMealReminderRepository, SentMealReminderRepository>();
services.AddScoped<IMealReminderCandidateQueries, MealReminderCandidateQueries>();

// Diet reminder jobs (registered as IDietReminderJob; resolved per tick)
services.AddScoped<IDietReminderJob, MealReminderJob>();

// Tick service options + hosted service
services.AddOptions<DietReminderTickServiceOptions>()
    .BindConfiguration(DietReminderTickServiceOptions.SectionName);
services.AddHostedService<DietReminderTickService>();
```

Add the necessary `using`s at the top:

```csharp
using DietPlanner.Application.Workers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Infrastructure.Workers;
```

- [ ] **Step 8.2: Build the host project**

```bash
dotnet build src/Apis/HomeSystem.REST/HomeSystem.REST.csproj
```

Expected: success.

- [ ] **Step 8.3: Add config defaults**

Open `src/Apis/HomeSystem.REST/appsettings.json` and ensure a `DietPlanner:DietReminderTick` section exists (under the existing `DietPlanner` section, or as a new top-level key consistent with other module sections — check what convention is already used). Minimal:

```json
"DietPlanner": {
  "DietReminderTick": {
    "Enabled": true,
    "TickIntervalSeconds": 60
  }
}
```

If a `DietPlanner` section already exists, merge into it instead of duplicating.

- [ ] **Step 8.4: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.Infrastructure/DependencyInjection.cs \
        src/Apis/HomeSystem.REST/appsettings.json
git commit -m "feat(diet-planner): register meal reminder job + tick service"
```

---

## Task 9: Reference DietPlanner.Contracts from Notifications.Application

**Files:**
- Modify: `src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj`

> Notifications consumes events declared in `DietPlanner.Contracts`. This is the **only** sanctioned cross-module dependency direction (per `backend-integration-patterns.md`).

- [ ] **Step 9.1: Add the project reference**

Inspect the `.csproj` and add inside the existing `<ItemGroup>` of project references:

```xml
<ProjectReference Include="..\..\DietPlanner\DietPlanner.Contracts\DietPlanner.Contracts.csproj" />
```

- [ ] **Step 9.2: Build**

```bash
dotnet build src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj
```

Expected: success.

- [ ] **Step 9.3: Commit**

```bash
git add src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj
git commit -m "chore(notifications): reference DietPlanner.Contracts for integration events"
```

---

## Task 10: Notifications event handlers — TDD

> Three handlers, one per integration event we now consume. Each is a thin shim that maps the event payload to placeholder dictionary + JSON payload string and calls the dispatcher. Combine into one task with three test files (one per handler) for symmetry.

**Files:**
- Create: `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealReminderDueIntegrationEventHandlerTests.cs`
- Create: `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealMissedIntegrationEventHandlerTests.cs`
- Create: `src/Modules/Notifications/Notifications.UnitTests/EventHandlers/GoalMilestoneReachedIntegrationEventHandlerTests.cs`
- Create: `src/Modules/Notifications/Notifications.Application/EventHandlers/MealReminderDueIntegrationEventHandler.cs`
- Create: `src/Modules/Notifications/Notifications.Application/EventHandlers/MealMissedIntegrationEventHandler.cs`
- Create: `src/Modules/Notifications/Notifications.Application/EventHandlers/GoalMilestoneReachedIntegrationEventHandler.cs`

> Verify the test project exists first: `ls src/Modules/Notifications/Notifications.UnitTests/`. If missing, scaffold it modeled on `DietPlanner.UnitTests.csproj` (xUnit + Shouldly + NSubstitute + InternalsVisibleTo to `Notifications.Application` and `Notifications.Infrastructure`). If you have to create it, commit that scaffold separately (`chore(notifications): add unit test project`) before continuing.

- [ ] **Step 10.1: Write the failing test for MealReminderDue**

```csharp
// src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealReminderDueIntegrationEventHandlerTests.cs
namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class MealReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealReminderDueIntegrationEventHandler _sut;

    public MealReminderDueIntegrationEventHandlerTests()
        => _sut = new MealReminderDueIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesMealReminderWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);
        var @event = new MealReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddMinutes(-10),
            UserId: "u1", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Lunch", PlannedAt: planned);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealReminder,
            "u1",
            "en",
            Arg.Is<string>(p => p.Contains("\"MealEntryId\"") && p.Contains("Lunch")),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MealSlotName"] == "Lunch" && d["PlannedAt"] == "12:00"),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 10.2: Write the failing test for MealMissed**

```csharp
// src/Modules/Notifications/Notifications.UnitTests/EventHandlers/MealMissedIntegrationEventHandlerTests.cs
namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class MealMissedIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealMissedIntegrationEventHandler _sut;

    public MealMissedIntegrationEventHandlerTests()
        => _sut = new MealMissedIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesMealMissedWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
        var @event = new MealMissedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddHours(1),
            UserId: "u2", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Breakfast", PlannedAt: planned);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealMissed,
            "u2",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MealSlotName"] == "Breakfast" && d["PlannedAt"] == "08:00"),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 10.3: Write the failing test for GoalMilestoneReached**

```csharp
// src/Modules/Notifications/Notifications.UnitTests/EventHandlers/GoalMilestoneReachedIntegrationEventHandlerTests.cs
namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class GoalMilestoneReachedIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly GoalMilestoneReachedIntegrationEventHandler _sut;

    public GoalMilestoneReachedIntegrationEventHandlerTests()
        => _sut = new GoalMilestoneReachedIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesGoalMilestoneWithLabelPlaceholder()
    {
        var @event = new GoalMilestoneReachedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: DateTime.UtcNow,
            UserId: "u3", Locale: "en",
            GoalKind: "WeightTarget",
            MilestoneLabel: "Reached target weight of 70 kg",
            Value: 70m);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.GoalMilestone,
            "u3",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MilestoneLabel"] == "Reached target weight of 70 kg"),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 10.4: Run tests, verify all three fail**

```bash
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj \
  --filter "FullyQualifiedName~EventHandlers"
```

Expected: build error referencing missing handler types.

- [ ] **Step 10.5: Implement MealReminderDueIntegrationEventHandler**

```csharp
// src/Modules/Notifications/Notifications.Application/EventHandlers/MealReminderDueIntegrationEventHandler.cs
namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class MealReminderDueIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<MealReminderDueIntegrationEvent>
{
    public Task HandleAsync(MealReminderDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MealSlotName"] = @event.MealSlotName,
            ["PlannedAt"] = @event.PlannedAt.ToString("HH:mm")
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.MealEntryId,
            @event.MealSlotName,
            @event.PlannedAt
        });

        return dispatcher.DispatchAsync(
            NotificationType.MealReminder,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
```

- [ ] **Step 10.6: Implement MealMissedIntegrationEventHandler**

```csharp
// src/Modules/Notifications/Notifications.Application/EventHandlers/MealMissedIntegrationEventHandler.cs
namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class MealMissedIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<MealMissedIntegrationEvent>
{
    public Task HandleAsync(MealMissedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MealSlotName"] = @event.MealSlotName,
            ["PlannedAt"] = @event.PlannedAt.ToString("HH:mm")
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.MealEntryId,
            @event.MealSlotName,
            @event.PlannedAt
        });

        return dispatcher.DispatchAsync(
            NotificationType.MealMissed,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
```

- [ ] **Step 10.7: Implement GoalMilestoneReachedIntegrationEventHandler**

```csharp
// src/Modules/Notifications/Notifications.Application/EventHandlers/GoalMilestoneReachedIntegrationEventHandler.cs
namespace Notifications.Application.EventHandlers;

using System.Text.Json;
using DietPlanner.Contracts.Events;
using Notifications.Application.Dispatching;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

internal sealed class GoalMilestoneReachedIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<GoalMilestoneReachedIntegrationEvent>
{
    public Task HandleAsync(GoalMilestoneReachedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var placeholders = new Dictionary<string, string>
        {
            ["MilestoneLabel"] = @event.MilestoneLabel,
            ["GoalKind"] = @event.GoalKind
        };

        var payload = JsonSerializer.Serialize(new
        {
            @event.GoalKind,
            @event.MilestoneLabel,
            @event.Value
        });

        return dispatcher.DispatchAsync(
            NotificationType.GoalMilestone,
            @event.UserId,
            @event.Locale,
            payload,
            placeholders,
            ct);
    }
}
```

- [ ] **Step 10.8: Run tests, verify all three pass**

```bash
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj \
  --filter "FullyQualifiedName~EventHandlers"
```

Expected: 3 passed.

- [ ] **Step 10.9: Commit**

```bash
git add src/Modules/Notifications/Notifications.Application/EventHandlers/ \
        src/Modules/Notifications/Notifications.UnitTests/EventHandlers/
git commit -m "feat(notifications): integration-event handlers for meal reminders and goal milestone"
```

---

## Task 11: Notifications DI — register inbox + handlers

**Files:**
- Modify: `src/Modules/Notifications/Notifications.Infrastructure/DependencyInjection.cs`

- [ ] **Step 11.1: Add inbox executor and explicit handler registrations**

Inside `AddNotificationsInfrastructure`, after the existing `services.AddOptions<RetryDeliveryWorkerOptions>()...AddHostedService<RetryDeliveryWorker>();` block, add:

```csharp
// Idempotent inbox for incoming integration events (Dapper-backed; uses inbox_messages)
services.AddDapperInbox<NotificationsConnectionFactory>();

// Integration-event handler registrations (explicit per spec — no assembly scanning for these)
services.AddScoped<
    IIntegrationEventHandler<MealReminderDueIntegrationEvent>,
    MealReminderDueIntegrationEventHandler>();
services.AddScoped<
    IIntegrationEventHandler<MealMissedIntegrationEvent>,
    MealMissedIntegrationEventHandler>();
services.AddScoped<
    IIntegrationEventHandler<GoalMilestoneReachedIntegrationEvent>,
    GoalMilestoneReachedIntegrationEventHandler>();
```

Add the necessary `using`s near the top:

```csharp
using DietPlanner.Contracts.Events;
using Notifications.Application.EventHandlers;
using Shared.Abstractions.Messaging;
```

- [ ] **Step 11.2: Build the host**

```bash
dotnet build src/Apis/HomeSystem.REST/HomeSystem.REST.csproj
```

Expected: success.

- [ ] **Step 11.3: Commit**

```bash
git add src/Modules/Notifications/Notifications.Infrastructure/DependencyInjection.cs
git commit -m "feat(notifications): wire Dapper inbox and integration-event handlers"
```

---

## Task 12: Finalize templates (en wording + pl variants)

**Files:**
- Modify: `src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs`

- [ ] **Step 12.1: Replace placeholder en wording for MealReminder/MealMissed and add pl variants**

Replace the existing `Templates` dictionary entries for `MealReminder` and `MealMissed` and add two new `pl` entries. The other entries (WaterReminder, WeeklySummary, GoalMilestone) remain untouched in this task — N8/N9 will revise water/weekly. GoalMilestone wording stays as-is.

```csharp
[(NotificationType.MealReminder, "en")] =
    new(NotificationType.MealReminder, "en",
        "Time for {{MealSlotName}}",
        "Your {{MealSlotName}} is planned at {{PlannedAt}}."),
[(NotificationType.MealReminder, "pl")] =
    new(NotificationType.MealReminder, "pl",
        "Czas na {{MealSlotName}}",
        "Twój posiłek {{MealSlotName}} zaplanowano na {{PlannedAt}}."),

[(NotificationType.MealMissed, "en")] =
    new(NotificationType.MealMissed, "en",
        "Missed {{MealSlotName}}",
        "You missed your {{MealSlotName}} planned for {{PlannedAt}}."),
[(NotificationType.MealMissed, "pl")] =
    new(NotificationType.MealMissed, "pl",
        "Pominięty {{MealSlotName}}",
        "Pominąłeś posiłek {{MealSlotName}} zaplanowany na {{PlannedAt}}."),
```

- [ ] **Step 12.2: Build**

```bash
dotnet build src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj
```

Expected: success.

- [ ] **Step 12.3: Commit**

```bash
git add src/Modules/Notifications/Notifications.Application/Templates/NotificationTemplateRegistry.cs
git commit -m "feat(notifications): finalize meal reminder/missed wording + add pl locale"
```

---

## Task 13: Integration test — full publish→consume roundtrip

**Files:**
- Create: `src/Modules/DietPlanner/DietPlanner.IntegrationTests/Workers/MealReminderJobIntegrationTests.cs`

> Verifies: seed a `DietReminderSettings` row, a `MealSlot`, and a `MealEntry` whose computed PlannedAt sits inside the lead-time window. Run `MealReminderJob.RunAsync(now)`. Assert: outbox row inserted; ledger row inserted. A second run with the same `now` should be a no-op (no duplicate outbox row).

This test runs against a real Postgres via the existing Testcontainers setup (look at any existing test under `DietPlanner.IntegrationTests` for the fixture pattern, e.g. `GoalMilestone*` integration tests if they exist, or any existing `*Endpoint*Tests`).

- [ ] **Step 13.1: Inspect existing integration test fixture**

```bash
ls src/Modules/DietPlanner/DietPlanner.IntegrationTests/
find src/Modules/DietPlanner/DietPlanner.IntegrationTests -name "*Fixture*.cs" -o -name "*ApplicationFactory*.cs"
```

Read whatever fixture is present to understand connection-string injection, container lifecycle, and DI access. Mirror that pattern in the new test file rather than inventing a new one.

- [ ] **Step 13.2: Write the integration test**

```csharp
// src/Modules/DietPlanner/DietPlanner.IntegrationTests/Workers/MealReminderJobIntegrationTests.cs
namespace DietPlanner.IntegrationTests.Workers;

#pragma warning disable IDE0005
using DietPlanner.Application.Workers;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Adjust the base class / fixture name to match the existing integration-test pattern
public sealed class MealReminderJobIntegrationTests(DietPlannerWebApplicationFactory factory)
    : IClassFixture<DietPlannerWebApplicationFactory>
{
    private readonly DietPlannerWebApplicationFactory _factory = factory;

    [Fact]
    public async Task RunAsync_PublishesOutboxRowAndWritesLedger_AndIsIdempotentOnSecondRun()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<DietPlannerDbContext>();
        var job = sp.GetRequiredService<IDietReminderJob>(); // resolves MealReminderJob

        // Arrange: seed slot, settings, planned meal entry
        var userId = $"user-{Guid.NewGuid()}";
        var slot = MealSlotTestFactory.Create(name: "Lunch", defaultTime: new TimeOnly(12, 0));
        var settings = DietReminderSettings.Create(
            DietReminderSettingsId.New(), userId,
            mealRemindersEnabled: true, mealReminderLeadTimeMinutes: 30);
        var nowUtc = DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddHours(11).AddMinutes(45), DateTimeKind.Utc); // 15 min before noon
        var entry = MealEntryTestFactory.CreatePlanned(
            userId,
            DateOnly.FromDateTime(nowUtc),
            slot.Id,
            mealTime: new TimeOnly(12, 0));

        dbContext.Set<MealSlot>().Add(slot);
        dbContext.Set<DietReminderSettings>().Add(settings);
        dbContext.Set<MealEntry>().Add(entry);
        await dbContext.SaveChangesAsync();

        // Act
        await job.RunAsync(nowUtc, CancellationToken.None);

        // Assert: ledger row + outbox row exist
        var ledgerCount = await dbContext.SentMealReminders.CountAsync(
            x => x.MealEntryId == entry.Id && x.Kind == MealReminderKind.Reminder);
        ledgerCount.ShouldBe(1);

        var outboxCount = await dbContext.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*)::int AS \"Value\" FROM outbox_messages WHERE event_type LIKE '%MealReminderDueIntegrationEvent%'")
            .FirstAsync();
        outboxCount.ShouldBe(1);

        // Act 2: idempotency
        await job.RunAsync(nowUtc, CancellationToken.None);

        var ledgerCountAfter = await dbContext.SentMealReminders.CountAsync(
            x => x.MealEntryId == entry.Id && x.Kind == MealReminderKind.Reminder);
        ledgerCountAfter.ShouldBe(1);
    }
}
```

> `MealSlotTestFactory.Create` and `MealEntryTestFactory.CreatePlanned` are placeholders — use whichever test-data builders the existing integration tests use (search for `MealSlot.Create` or `MealEntry.Create` calls in `DietPlanner.IntegrationTests/`). If no factory exists, instantiate the aggregates directly via their public `Create` static methods.

> The raw-SQL outbox count avoids needing to import the `Shared.Infrastructure.Messaging.Ef` outbox entity into the test project.

- [ ] **Step 13.3: Run the test**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj \
  --filter "FullyQualifiedName~MealReminderJobIntegrationTests"
```

Expected: 1 passed (Docker required for Testcontainers). If it fails, investigate the failure: most likely the test factory shape, the outbox table location (per-module schema), or the integration-test fixture name. Adjust and re-run rather than papering over.

- [ ] **Step 13.4: Commit**

```bash
git add src/Modules/DietPlanner/DietPlanner.IntegrationTests/Workers/
git commit -m "test(diet-planner): integration test for MealReminderJob roundtrip and idempotency"
```

---

## Task 14: Final verification

- [ ] **Step 14.1: Run the full backend test suite**

```bash
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj
dotnet test src/Modules/Notifications/Notifications.UnitTests/Notifications.UnitTests.csproj
dotnet test src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj
```

Expected: all green.

- [ ] **Step 14.2: Build the whole solution**

```bash
dotnet build HomeSystem.slnx
```

Expected: success, no warnings escalated.

- [ ] **Step 14.3: Manual smoke (optional but recommended)**

Spin up the API in Development mode (auto-migrates, in-process transport). Insert a `DietReminderSettings`, `MealSlot`, and a `MealEntry` (any user) with `MealTime` 1–2 minutes after `now`. Within ~60 seconds you should see the console sender log a `MealReminder` notification. Reset state by truncating `sent_meal_reminders` if re-running the same row.

- [ ] **Step 14.4: Push the branch and open a PR**

```bash
git push -u origin feat/157-meal-reminder-jobs
```

Open the PR using the project's `commit-commands:commit-push-pr` skill or manually with `gh pr create`. Reference issue #157 (`Closes #157`). Title: `feat(diet-planner): meal reminder/missed jobs + tick service (#157)`. Body: brief summary + test plan checklist.

---

## Spec self-review

| Spec section | Where covered |
|---|---|
| §8.2 `DietReminderTickService` + `IDietReminderJob` | Tasks 4, 7 |
| §8.2 `MealReminderJob` (reminder + missed) | Task 6 |
| §8.2 `sent_meal_reminders` ledger | Tasks 1, 2, 3 |
| §8.4 publishes `MealReminderDueIntegrationEvent` / `MealMissedIntegrationEvent` | Task 6 |
| §10 Notifications-side handlers + templates | Tasks 9, 10, 11, 12 |
| §6.4 module wiring (`AddDapperInbox<…>`) | Task 11 |
| §9.2 unit tests for jobs | Tasks 6, 7 |
| §9.4 unit tests for handlers | Task 10 |
| §9.4 integration roundtrip test | Task 13 |
| Closes N6 gap (GoalMilestoneReached consumer) | Tasks 10, 11 |

Out of scope: water/weekly schedulers (N8/N9), per-(type×channel) prefs, SignalR/email senders, server-side timezones.
