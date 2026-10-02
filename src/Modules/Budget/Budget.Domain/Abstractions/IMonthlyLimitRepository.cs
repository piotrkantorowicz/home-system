namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>Write-side access to the <c>MonthlyLimit</c> aggregate. Queries bypass it.</summary>
public interface IMonthlyLimitRepository
{
    /// <summary>
    /// Serialises writers of one envelope/month for the rest of the transaction, so two first-time
    /// setters never race to a unique-key violation (which would abort the transaction).
    /// </summary>
    /// <param name="accountId">The envelope.</param>
    /// <param name="month">The month.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockAsync(BudgetAccountId accountId, BudgetMonth month, CancellationToken ct);

    /// <summary>Loads the limit for modification, or <see langword="null"/> when none is set.</summary>
    /// <param name="accountId">The envelope.</param>
    /// <param name="month">The month.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<MonthlyLimit?> GetAsync(BudgetAccountId accountId, BudgetMonth month, CancellationToken ct);

    /// <summary>Stages a new limit for the next commit.</summary>
    /// <param name="limit">The limit to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(MonthlyLimit limit, CancellationToken ct);

    /// <summary>Stages removal of a limit for the next commit.</summary>
    /// <param name="limit">The limit to remove.</param>
    void Remove(MonthlyLimit limit);
}
