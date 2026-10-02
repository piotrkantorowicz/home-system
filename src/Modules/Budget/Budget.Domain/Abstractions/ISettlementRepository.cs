namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>Write-side access to the <c>Settlement</c> (recorded repayment) aggregate. Queries bypass it.</summary>
public interface ISettlementRepository
{
    /// <summary>Stages a new repayment for the next commit.</summary>
    /// <param name="settlement">The repayment to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Settlement settlement, CancellationToken ct);

    /// <summary>
    /// Serialises submissions that share a creation request identity for the rest of the
    /// transaction, so a concurrent retry waits for the winner and then finds it.
    /// </summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The recording person.</param>
    /// <param name="clientRequestId">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct);

    /// <summary>Serialises writers of one repayment; call before loading it.</summary>
    /// <param name="settlementId">The repayment about to be modified.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockSettlementAsync(Guid settlementId, CancellationToken ct);

    /// <summary>Finds the repayment a creation request identity already produced, voided or not.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The recording person; other actors' keys never match.</param>
    /// <param name="clientRequestId">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Settlement?> FindByRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct);

    /// <summary>Loads a repayment for modification, or <see langword="null"/> when it is not in the budget.</summary>
    /// <param name="id">The repayment.</param>
    /// <param name="budgetId">The budget it must belong to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Settlement?> GetAsync(SettlementId id, BudgetId budgetId, CancellationToken ct);

    /// <summary>
    /// The latest name stored in this budget's shared ledger (active shared expenses and repayments)
    /// for a person, or <see langword="null"/> when they have never taken part — i.e. an outsider.
    /// </summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="personId">The person.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> FindLedgerNameAsync(BudgetId budgetId, Guid personId, CancellationToken ct);
}
