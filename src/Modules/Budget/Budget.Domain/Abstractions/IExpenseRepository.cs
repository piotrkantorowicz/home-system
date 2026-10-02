namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;
using Budget.Domain.Entities;
using Budget.Domain.ValueObjects;

/// <summary>Write-side access to the <c>Expense</c> aggregate. Queries bypass it.</summary>
public interface IExpenseRepository
{
    /// <summary>Stages a new expense (with its shares and creation revision) for the next commit.</summary>
    /// <param name="expense">The expense to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(Expense expense, CancellationToken ct);

    /// <summary>
    /// Serialises submissions that share a request identity for the rest of the transaction, so a
    /// concurrent retry waits for the winner's commit and then finds it. Avoids catching a unique
    /// violation, which would abort the PostgreSQL transaction.
    /// </summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The submitting person.</param>
    /// <param name="clientRequestId">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct);

    /// <summary>Finds the revision a request identity already produced, if any.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The submitting person; other actors' keys are never matched.</param>
    /// <param name="clientRequestId">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ExpenseRevision?> FindRevisionByRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct);
}
