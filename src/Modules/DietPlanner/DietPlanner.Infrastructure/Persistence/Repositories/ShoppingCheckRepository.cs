namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class ShoppingCheckRepository(DietPlannerDbContext dbContext) : IShoppingCheckRepository
{
    public async Task CheckAsync(ShoppingCheck check, CancellationToken ct = default)
        => await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO shopping_checks (scope_id, range_from, range_to, product_id, unit, checked_by_person_id, checked_at)
            VALUES ({check.ScopeId}, {check.RangeFrom}, {check.RangeTo}, {check.ProductId.Value}, {check.Unit}, {check.CheckedByPersonId}, {check.CheckedAt})
            ON CONFLICT DO NOTHING
            """, ct);

    public async Task UncheckAsync(
        Guid scopeId, DateOnly? rangeFrom, DateOnly? rangeTo, ProductId productId, string unit, CancellationToken ct = default)
    {
        var (lower, upper) = Range(rangeFrom, rangeTo);
        await dbContext.ShoppingChecks
            .Where(x => x.ScopeId == scopeId && x.RangeFrom == lower && x.RangeTo == upper && x.ProductId == productId && x.Unit == unit)
            .ExecuteDeleteAsync(ct);
    }

    public async Task ClearAsync(Guid scopeId, DateOnly? rangeFrom, DateOnly? rangeTo, CancellationToken ct = default)
    {
        var (lower, upper) = Range(rangeFrom, rangeTo);
        await dbContext.ShoppingChecks
            .Where(x => x.ScopeId == scopeId && x.RangeFrom == lower && x.RangeTo == upper)
            .ExecuteDeleteAsync(ct);
    }

    private static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to)
        => (ShoppingCheck.Bound(from, DateOnly.MinValue), ShoppingCheck.Bound(to, DateOnly.MaxValue));
}
