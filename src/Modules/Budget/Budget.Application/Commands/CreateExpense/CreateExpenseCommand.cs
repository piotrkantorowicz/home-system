namespace Budget.Application.Commands.CreateExpense;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Records an expense. Actor, household and name snapshots come from the authenticated caller and
/// current roster — never from the request. A retry with the same <paramref name="ClientRequestId"/>
/// and payload returns the original result.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="ClientRequestId">Idempotency key, generated once per logical submission.</param>
/// <param name="AccountId">The envelope.</param>
/// <param name="Amount">Positive decimal string, at most two decimals, ≤ 9999999999.99.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="Category">Category code.</param>
/// <param name="FundingSource"><c>Individual</c> (default) or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer of an individually funded shared expense; omitted for personal envelopes and household funds.</param>
/// <param name="ParticipantIds">Adults sharing the cost equally; required for individually funded shared expenses, empty otherwise.</param>
public sealed record CreateExpenseCommand(
    string AuthSubject,
    Guid ClientRequestId,
    Guid AccountId,
    string Amount,
    DateOnly OccurredOn,
    string Category,
    string? FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid>? ParticipantIds) : ICommand<ExpenseMutationResult>;

/// <summary>Outcome of a create (or its replay).</summary>
/// <param name="ExpenseId">The expense.</param>
/// <param name="Revision">The revision this request produced.</param>
/// <param name="Created"><see langword="true"/> when this call created it; <see langword="false"/> for a recognised retry.</param>
public sealed record ExpenseMutationResult(Guid ExpenseId, int Revision, bool Created);
