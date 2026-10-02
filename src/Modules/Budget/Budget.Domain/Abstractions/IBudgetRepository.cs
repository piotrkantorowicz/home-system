namespace Budget.Domain.Abstractions;

using BudgetAggregate = Budget.Domain.Aggregates.Budget;

/// <summary>Write-side access to the <c>Budget</c> aggregate. Queries bypass it.</summary>
public interface IBudgetRepository
{
    /// <summary>
    /// Serialises concurrent initialisation of one household's budget until the ambient transaction
    /// ends (a transaction-scoped advisory lock). Call before <see cref="GetByHouseholdAsync"/>.
    /// </summary>
    /// <param name="householdId">The household being initialised.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockHouseholdAsync(Guid householdId, CancellationToken ct);

    /// <summary>Loads the household's budget, or <see langword="null"/> when it has none.</summary>
    /// <param name="householdId">The owning household.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BudgetAggregate?> GetByHouseholdAsync(Guid householdId, CancellationToken ct);

    /// <summary>Stages a new budget for the next commit.</summary>
    /// <param name="budget">The budget to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(BudgetAggregate budget, CancellationToken ct);
}
