namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging.Ef.Outbox;

[Collection(DatabaseCollectionDefinition.Name)]
public sealed class GoalMilestoneEvaluatorTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly DatabaseFixture _db;

    public GoalMilestoneEvaluatorTests(DatabaseFixture db) => _db = db;

    [Fact]
    public async Task POST_WeightEntry_WhenCrossesTarget_WritesOutboxRowAndMarksMilestone()
    {
        var userId = $"goal-milestone-{Guid.NewGuid():N}";
        var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString, userId);
        var client = factory.CreateClient();

        // Seed: profile + goal with weight target
        await EnsureProfileExists(client);
        UserGoalId goalId = await SeedGoalWithTargetWeight(factory, userId, targetWeightKg: 70m);

        // Act: log weight that crosses target
        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries",
            new LogWeightEntryRequest(Today, 69.5m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Assert: outbox row written + milestone achieved
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

        // Postgres jsonb doesn't support LIKE — load matching event-type rows then filter
        // by payload substring in memory.
        var milestoneRows = (await dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.EventType.Contains("GoalMilestoneReachedIntegrationEvent"))
            .ToListAsync())
            .Where(x => x.Payload.Contains(userId, StringComparison.Ordinal))
            .ToList();
        milestoneRows.ShouldNotBeEmpty();

        var goal = await dbContext.UserGoals.FirstOrDefaultAsync(g => g.Id == goalId);
        goal.ShouldNotBeNull();
        goal.MilestoneAchievedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task POST_WeightEntry_WhenAboveTarget_DoesNotWriteOutboxRow()
    {
        var userId = $"goal-no-milestone-{Guid.NewGuid():N}";
        var factory = new DietPlannerWebApplicationFactory(_db.ConnectionString, userId);
        var client = factory.CreateClient();

        await EnsureProfileExists(client);
        await SeedGoalWithTargetWeight(factory, userId, targetWeightKg: 65m);

        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries",
            new LogWeightEntryRequest(Today, 75m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();

        // Outbox may have rows from other tests but none should include this user's id
        // tied to a GoalMilestoneReachedIntegrationEvent
        var milestoneRows = (await dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.EventType.Contains("GoalMilestoneReachedIntegrationEvent"))
            .ToListAsync())
            .Where(x => x.Payload.Contains(userId, StringComparison.Ordinal))
            .ToList();
        milestoneRows.ShouldBeEmpty();
    }

    private static async Task EnsureProfileExists(HttpClient client)
    {
        var existing = await client.GetAsync("/api/v1/profile");
        if (existing.StatusCode == HttpStatusCode.OK) return;

        var request = new ProfileRequest(
            new DateOnly(1990, 1, 1), "Male", 180m, 80m, 75m, "ModeratelyActive");
        var response = await client.PostAsJsonAsync("/api/v1/profile", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task<UserGoalId> SeedGoalWithTargetWeight(
        DietPlannerWebApplicationFactory factory, string userId, decimal targetWeightKg)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        var goal = UserGoal.Create(
            UserGoalId.New(), userId, 2000, 150m, 250m, 70m, 30m,
            targetWeightKg: targetWeightKg);
        dbContext.UserGoals.Add(goal);
        await dbContext.SaveChangesAsync();
        return goal.Id;
    }
}
