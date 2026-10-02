namespace Budget.Application.Commands.VoidExpense;

using Budget.Application.Commands.CreateExpense;
using Shared.Abstractions.Cqrs;

/// <summary>Voids an expense: it stops counting and can no longer be edited; history stays. Repeating a completed void adds no revision.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="ExpenseId">The expense.</param>
/// <param name="ClientRequestId">Idempotency key, generated once per logical submission.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>
/// <param name="Reason">Short reason, 1–200 characters.</param>
public sealed record VoidExpenseCommand(string AuthSubject, Guid ExpenseId, Guid ClientRequestId, int ExpectedRevision, string Reason)
    : ICommand<ExpenseMutationResult>;
