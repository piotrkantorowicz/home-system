namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="ShoppingCheck"/> rows. Every write is a single idempotent statement
/// so two members ticking the same row at once cannot fail or duplicate it; queries read the DbContext directly.
/// </summary>
public interface IShoppingCheckRepository
{
    /// <summary>Stores the check unless the row is already checked; repeating is a no-op.</summary>
    /// <param name="check">The check to store.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task CheckAsync(ShoppingCheck check, CancellationToken ct = default);
    /// <summary>Removes one row's check; repeating is a no-op.</summary>
    /// <param name="scopeId">Household identifier, or the person identifier without a household.</param>
    /// <param name="rangeFrom">First day of the range, or <see langword="null"/>.</param>
    /// <param name="rangeTo">Last day of the range, or <see langword="null"/>.</param>
    /// <param name="productId">The product.</param>
    /// <param name="unit">The unit.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task UncheckAsync(Guid scopeId, DateOnly? rangeFrom, DateOnly? rangeTo, ProductId productId, string unit, CancellationToken ct = default);
    /// <summary>Removes every check of one range.</summary>
    /// <param name="scopeId">Household identifier, or the person identifier without a household.</param>
    /// <param name="rangeFrom">First day of the range, or <see langword="null"/>.</param>
    /// <param name="rangeTo">Last day of the range, or <see langword="null"/>.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task ClearAsync(Guid scopeId, DateOnly? rangeFrom, DateOnly? rangeTo, CancellationToken ct = default);
}
