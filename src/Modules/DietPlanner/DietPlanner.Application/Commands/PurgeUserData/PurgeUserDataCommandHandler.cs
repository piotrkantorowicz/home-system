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

        // Raw SQL for predictability — EF's LINQ translation of composite subqueries
        // across IgnoreQueryFilters didn't always emit the SQL we need, and this
        // endpoint is dev-only test plumbing where schema coupling is acceptable.
        //
        // Stale-sub resilience: `docker compose down -v` regenerates Authentik `sub`
        // hashes, so prior-session rows carry a different user_id but may still point
        // at the current user's recipes/products. Junction deletes are broadened to
        // match orphans regardless of their own user_id.
        //
        // Order: children → junctions → roots. Soft-deleted rows are included because
        // raw SQL bypasses EF query filters.
        await _dbContext.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM meal_entries
            WHERE user_id = {userId}
               OR recipe_id IN (SELECT id FROM recipes WHERE created_by_user_id = {userId})
            """, ct);

        await _dbContext.Database.ExecuteSqlAsync(
            $"DELETE FROM water_intakes WHERE user_id = {userId}", ct);

        // recipe_ingredients referencing the current user's products (regardless of
        // who owns the containing recipe) — prevents FK violation on the product delete.
        // Ingredients belonging to user-owned recipes cascade when the recipe is deleted.
        await _dbContext.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM recipe_ingredients
            WHERE product_id IN (SELECT id FROM products WHERE created_by_user_id = {userId})
            """, ct);

        await _dbContext.Database.ExecuteSqlAsync(
            $"DELETE FROM recipes WHERE created_by_user_id = {userId}", ct);

        await _dbContext.Database.ExecuteSqlAsync(
            $"DELETE FROM products WHERE created_by_user_id = {userId}", ct);

        // Singletons. meal_schedule_configs cascades to meal_slots via FK.
        await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM hydration_configs WHERE user_id = {userId}", ct);
        await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM user_goals WHERE user_id = {userId}", ct);
        await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM meal_schedule_configs WHERE user_id = {userId}", ct);
        await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM notification_preferences WHERE user_id = {userId}", ct);
        await _dbContext.Database.ExecuteSqlAsync($"DELETE FROM user_profiles WHERE user_id = {userId}", ct);
    }
}
