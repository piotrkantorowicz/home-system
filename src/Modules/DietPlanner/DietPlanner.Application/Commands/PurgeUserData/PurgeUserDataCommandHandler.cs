namespace DietPlanner.Application.Commands.PurgeUserData;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class PurgeUserDataCommandHandler : ICommandHandler<PurgeUserDataCommand>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public PurgeUserDataCommandHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task HandleAsync(PurgeUserDataCommand command, CancellationToken ct = default)
    {
        var userId = command.UserId;

        // Single batched statement — one round trip, atomic with the outer command
        // transaction. Raw SQL is chosen for predictability: EF's LINQ translation of
        // composite subqueries across IgnoreQueryFilters did not always emit the SQL
        // we need, and this endpoint is dev-only test plumbing where schema coupling
        // is acceptable. Soft-deleted rows are included because raw SQL bypasses EF
        // query filters.
        //
        // Stale-sub resilience: `docker compose down -v` regenerates Authentik `sub`
        // hashes, so prior-session rows carry a different user_id but may still
        // reference the current user's recipes/products. Junction deletes match
        // orphans regardless of their own user_id.
        //
        // Order: children → junctions → roots.
        // meal_schedule_configs → meal_slots and recipes → recipe_ingredients cascade
        // via FK configuration; we still delete recipe_ingredients explicitly to cut
        // the edge from foreign recipes pointing at this user's products.
        await _dbContext.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM meal_entries
            WHERE user_id = {userId}
               OR recipe_id IN (SELECT id FROM recipes WHERE created_by_user_id = {userId});

            DELETE FROM water_intakes WHERE user_id = {userId};

            DELETE FROM recipe_ingredients
            WHERE product_id IN (SELECT id FROM products WHERE created_by_user_id = {userId});

            DELETE FROM recipes WHERE created_by_user_id = {userId};
            DELETE FROM products WHERE created_by_user_id = {userId};

            DELETE FROM hydration_configs WHERE user_id = {userId};
            DELETE FROM user_goals WHERE user_id = {userId};
            DELETE FROM meal_schedule_configs WHERE user_id = {userId};
            DELETE FROM diet_reminder_settings WHERE user_id = {userId};
            DELETE FROM user_profiles WHERE user_id = {userId};
            """, ct);
    }
}
