namespace Budget.Application.Commands.UpdateExpense;

using Budget.Application.Commands.CreateExpense;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Corrects an expense in place. Envelope and recorder cannot change. Participants and payer may
/// keep people who have since left, but cannot newly introduce ineligible ones.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="ExpenseId">The expense.</param>
/// <param name="ClientRequestId">Idempotency key, generated once per logical submission.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>
/// <param name="Reason">Short reason, 1–200 characters.</param>
/// <param name="Amount">Positive decimal string, at most two decimals.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="Category">Category code.</param>
/// <param name="FundingSource"><c>Individual</c> (default) or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer of an individually funded shared expense.</param>
/// <param name="ParticipantIds">Adults sharing the cost equally.</param>
/// <param name="Description">New optional short note, at most 80 characters; omitted or blank clears it.</param>
public sealed record UpdateExpenseCommand(
    string AuthSubject,
    Guid ExpenseId,
    Guid ClientRequestId,
    int ExpectedRevision,
    string Reason,
    string Amount,
    DateOnly OccurredOn,
    string Category,
    string? FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid>? ParticipantIds,
    string? Description = null) : ICommand<ExpenseMutationResult>;
