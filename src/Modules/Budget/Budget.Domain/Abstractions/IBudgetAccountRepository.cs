namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;

/// <summary>Write-side access to the <c>BudgetAccount</c> (envelope) aggregate. Queries bypass it.</summary>
public interface IBudgetAccountRepository
{
    /// <summary>Stages a new envelope for the next commit.</summary>
    /// <param name="account">The envelope to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(BudgetAccount account, CancellationToken ct);
}
