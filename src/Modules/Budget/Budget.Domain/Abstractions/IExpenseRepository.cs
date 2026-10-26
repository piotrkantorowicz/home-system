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

    /// <summary>
    /// Serialises writers of one expense for the rest of the transaction. Call it <em>before</em>
    /// loading the expense, so a waiting writer reads the winner's committed revision and fails the
    /// expected-revision check cleanly instead of racing at the database.
    /// </summary>
    /// <param name="expenseId">The expense about to be modified.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockExpenseAsync(Guid expenseId, CancellationToken ct);

    /// <summary>Finds the revision a request identity already produced, if any.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The submitting person; other actors' keys are never matched.</param>
    /// <param name="clientRequestId">The idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ExpenseRevision?> FindRevisionByRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct);

    /// <summary>Finds the revisions any of <paramref name="clientRequestIds"/> already produced; one batched lookup for an import.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="actorPersonId">The submitting person; other actors' keys are never matched.</param>
    /// <param name="clientRequestIds">The idempotency keys.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<ExpenseRevision>> FindRevisionsByRequestsAsync(
        BudgetId budgetId, Guid actorPersonId, IReadOnlyCollection<Guid> clientRequestIds, CancellationToken ct);

    /// <summary>
    /// Serialises imports into one privacy scope for the rest of the transaction, so the
    /// already-imported check cannot race another import into a unique violation.
    /// </summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="importScope">The scope, see <see cref="Expense.ImportScopeOf"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockImportScopeAsync(BudgetId budgetId, string importScope, CancellationToken ct);

    /// <summary>Whether any of <paramref name="sourceKeys"/> was already imported in the scope, voided expenses included.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="importScope">The scope.</param>
    /// <param name="sourceKeys">Statement row source keys.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> AnyImportedAsync(BudgetId budgetId, string importScope, IReadOnlyCollection<string> sourceKeys, CancellationToken ct);

    /// <summary>Loads an expense with its shares for modification, or <see langword="null"/> when it is not in the budget.</summary>
    /// <param name="id">The expense.</param>
    /// <param name="budgetId">The budget it must belong to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Expense?> GetAsync(ExpenseId id, BudgetId budgetId, CancellationToken ct);
}
