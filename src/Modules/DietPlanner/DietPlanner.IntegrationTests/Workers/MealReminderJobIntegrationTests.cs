namespace DietPlanner.IntegrationTests.Workers;

using DietPlanner.Application.Workers;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging.Ef.Outbox;

[Collection(DatabaseCollection.Name)]
public sealed class MealReminderJobIntegrationTests
{
    private readonly DatabaseFixture _db;

    public MealReminderJobIntegrationTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task RunAsync_PublishesOutboxRowAndWritesLedger_AndIsIdempotentOnSecondRun()
    {
        // Arrange: unique userId and factory for test isolation
        var userId = $"meal-reminder-job-{Guid.NewGuid():N}";
        var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString, userId);

        // Capture today's date once to avoid midnight rollover issues
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

        // nowUtc is set to 11:50 UTC so that PlannedAt = 12:00 UTC falls in (nowUtc, nowUtc + 15min]
        var nowUtc = DateTime.SpecifyKind(todayUtc.ToDateTime(new TimeOnly(11, 50)), DateTimeKind.Utc);
        var mealTime = new TimeOnly(12, 0);

        MealSlotId seededSlotId;
        MealEntryId mealEntryId;

        // Seed all required entities
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

            // Seed MealScheduleConfig with one slot at 12:00 (default time, but will be overridden by MealEntry.MealTime)
            var config = MealScheduleConfig.Create(
                MealScheduleConfigId.New(),
                userId,
                [("Lunch", new TimeOnly(12, 0))]);

            dbContext.MealScheduleConfigs.Add(config);
            await dbContext.SaveChangesAsync();

            // Read back the persisted slot to get its ID (EF-generated)
            seededSlotId = config.Slots.First().Id;

            // Seed a minimal Recipe (required by FK on MealEntry.RecipeId)
            var recipe = Recipe.Create(
                RecipeId.New(),
                "Test Recipe",
                description: null,
                instructions: null,
                servings: 1,
                prepTimeMinutes: null,
                createdByUserId: userId);

            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            // Seed DietReminderSettings with MealRemindersEnabled = true, lead = 15 min
            var settings = DietReminderSettings.Create(
                DietReminderSettingsId.New(),
                userId,
                mealRemindersEnabled: true,
                mealReminderLeadTimeMinutes: 15);

            dbContext.DietReminderSettings.Add(settings);
            await dbContext.SaveChangesAsync();

            // Seed MealEntry: Date = today, MealTime = 12:00, Status = Planned
            mealEntryId = MealEntryId.New();
            var entry = MealEntry.Create(
                mealEntryId,
                userId,
                todayUtc,
                seededSlotId,
                recipe.Id,
                servings: 1m,
                notes: null,
                mealTime: mealTime,
                sequenceOrder: 1);

            dbContext.MealEntries.Add(entry);
            await dbContext.SaveChangesAsync();
        }

        // Resolve the MealReminderJob via the public IDietReminderJob interface
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var job = scope.ServiceProvider
                .GetServices<IDietReminderJob>()
                .Single(j => j.Name == "MealReminderJob");

            // Act 1: first run
            await job.RunAsync(nowUtc, CancellationToken.None);
        }

        // Assert: ledger has exactly 1 row for (mealEntryId, Reminder)
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

            var ledgerCount = await dbContext.SentMealReminders
                .CountAsync(r => r.MealEntryId == mealEntryId && r.Kind == MealReminderKind.Reminder);
            ledgerCount.ShouldBe(1);

            // Assert: outbox has exactly 1 MealReminderDueIntegrationEvent for this user
            var outboxRows = (await dbContext.Set<OutboxMessageEntity>()
                    .Where(x => x.EventType.Contains("MealReminderDueIntegrationEvent"))
                    .ToListAsync())
                .Where(x => x.Payload.Contains(userId, StringComparison.Ordinal))
                .ToList();
            outboxRows.Count.ShouldBe(1);
        }

        // Act 2: idempotency — run again with the same nowUtc
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var job = scope.ServiceProvider
                .GetServices<IDietReminderJob>()
                .Single(j => j.Name == "MealReminderJob");

            await job.RunAsync(nowUtc, CancellationToken.None);
        }

        // Assert idempotency: counts remain at 1
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

            var ledgerCount = await dbContext.SentMealReminders
                .CountAsync(r => r.MealEntryId == mealEntryId && r.Kind == MealReminderKind.Reminder);
            ledgerCount.ShouldBe(1, "ledger must not accumulate on second run");

            var outboxRows = (await dbContext.Set<OutboxMessageEntity>()
                    .Where(x => x.EventType.Contains("MealReminderDueIntegrationEvent"))
                    .ToListAsync())
                .Where(x => x.Payload.Contains(userId, StringComparison.Ordinal))
                .ToList();
            outboxRows.Count.ShouldBe(1, "outbox must not accumulate on second run");
        }
    }
}
