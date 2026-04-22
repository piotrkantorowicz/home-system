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
        // IgnoreQueryFilters ensures soft-deleted rows (Product.DeletedAt / Recipe.DeletedAt)
        // are also purged — otherwise their orphaned FK references would block parent deletes.
        await _dbContext.MealEntries.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.WaterIntakes.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.Recipes.IgnoreQueryFilters().Where(x => x.CreatedByUserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.Products.IgnoreQueryFilters().Where(x => x.CreatedByUserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.HydrationConfigs.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserGoals.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.MealScheduleConfigs.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.NotificationPreferences.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserProfiles.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
    }
}
