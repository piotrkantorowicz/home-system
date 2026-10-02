namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>Write-side access to the <c>BudgetAccount</c> (envelope) aggregate. Queries bypass it.</summary>
public interface IBudgetAccountRepository
{
    /// <summary>Stages a new envelope for the next commit.</summary>
    /// <param name="account">The envelope to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(BudgetAccount account, CancellationToken ct);

    /// <summary>Loads an envelope for modification, or <see langword="null"/> when it is not in the budget.</summary>
    /// <param name="id">The envelope.</param>
    /// <param name="budgetId">The budget it must belong to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BudgetAccount?> GetAsync(BudgetAccountId id, BudgetId budgetId, CancellationToken ct);
}
