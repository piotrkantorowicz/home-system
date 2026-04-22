namespace DietPlanner.Application.Commands.PurgeUserData;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class PurgeUserDataCommandHandler : ICommandHandler<PurgeUserDataCommand>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public PurgeUserDataCommandHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task HandleAsync(PurgeUserDataCommand command, CancellationToken ct = default)
    {
        var userId = command.UserId;

        // FK-safe order: dependents first, roots last.
        // Recipe→Ingredients and MealScheduleConfig→Slots cascade via EF configuration.
        await _dbContext.MealEntries.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.WaterIntakes.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.Recipes.Where(x => x.CreatedByUserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.Products.Where(x => x.CreatedByUserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.HydrationConfigs.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserGoals.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.MealScheduleConfigs.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.NotificationPreferences.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserProfiles.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
    }
}
