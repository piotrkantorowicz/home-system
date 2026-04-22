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

        // Stale-sub resilience: rows created under a previous Authentik `sub` hash
        // (e.g. after `docker compose down -v`) carry a different user_id but still
        // reference this user's recipes/products. Scoping deletes solely by user_id
        // would leave those orphans behind and make their FK constraints block the
        // parent deletes. Each of the two junction tables below is broadened to also
        // match rows pointing at soon-to-be-deleted roots, regardless of their own
        // user_id.
        //
        // FK-safe order: dependents first, roots last.
        // Recipe→Ingredients and MealScheduleConfig→Slots cascade via EF configuration.
        // IgnoreQueryFilters covers soft-deleted rows (Product.DeletedAt / Recipe.DeletedAt).

        await _dbContext.MealEntries.IgnoreQueryFilters()
            .Where(x => x.UserId == userId
                     || _dbContext.Recipes.IgnoreQueryFilters()
                         .Any(r => r.CreatedByUserId == userId && r.Id == x.RecipeId))
            .ExecuteDeleteAsync(ct);

        await _dbContext.WaterIntakes.IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        await _dbContext.Recipes.IgnoreQueryFilters()
            .Where(r => r.CreatedByUserId == userId
                     || r.Ingredients.Any(i => _dbContext.Products.IgnoreQueryFilters()
                         .Any(p => p.CreatedByUserId == userId && p.Id == i.ProductId)))
            .ExecuteDeleteAsync(ct);

        await _dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.CreatedByUserId == userId)
            .ExecuteDeleteAsync(ct);

        await _dbContext.HydrationConfigs.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserGoals.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.MealScheduleConfigs.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.NotificationPreferences.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await _dbContext.UserProfiles.IgnoreQueryFilters().Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
    }
}
