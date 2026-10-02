namespace Budget.Domain.ValueObjects;

/// <summary>
/// The normalised user inputs of one create/edit/void submission, stored with its revision so a
/// retry is recognised without a receipt table. Excludes server-calculated names, timestamps and
/// shares. Compared with <see cref="Matches"/>.
/// </summary>
/// <param name="Action">What the submission does, e.g. <c>Create</c>.</param>
/// <param name="AccountId">The envelope.</param>
/// <param name="Amount">Normalised money string, e.g. <c>12.30</c>.</param>
/// <param name="Category">Category code.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="FundingSource">Funding source.</param>
/// <param name="PaidByPersonId">Resolved payer, or <see langword="null"/>.</param>
/// <param name="ParticipantIds">Participants in ascending ID order.</param>
/// <param name="ExpenseId">Target expense of an update or void; <see langword="null"/> for a create.</param>
/// <param name="ExpectedRevision">Revision the caller based an update or void on.</param>
/// <param name="Reason">Trimmed reason of an update or void.</param>
public sealed record ExpenseRequest(
    string Action,
    Guid AccountId,
    string Amount,
    ExpenseCategory Category,
    DateOnly OccurredOn,
    FundingSource FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid> ParticipantIds,
    Guid? ExpenseId = null,
    int? ExpectedRevision = null,
    string? Reason = null)
{
    /// <summary>Whether <paramref name="other"/> is the same logical request.</summary>
    /// <param name="other">A stored or incoming request.</param>
    public bool Matches(ExpenseRequest other)
        => Action == other.Action && AccountId == other.AccountId && Amount == other.Amount
           && Category == other.Category && OccurredOn == other.OccurredOn
           && FundingSource == other.FundingSource && PaidByPersonId == other.PaidByPersonId
           && ParticipantIds.SequenceEqual(other.ParticipantIds)
           && ExpenseId == other.ExpenseId && ExpectedRevision == other.ExpectedRevision && Reason == other.Reason;

    /// <summary>The request identity of voiding <paramref name="expenseId"/>; financial fields are neutral because a void carries none.</summary>
    /// <param name="accountId">The expense's envelope.</param>
    /// <param name="expenseId">The target expense.</param>
    /// <param name="expectedRevision">The revision the caller saw.</param>
    /// <param name="reason">Trimmed reason.</param>
    public static ExpenseRequest ForVoid(Guid accountId, Guid expenseId, int expectedRevision, string reason)
        => new("Void", accountId, "0.00", ExpenseCategory.Other, default, FundingSource.Individual, null, [], expenseId, expectedRevision, reason);
}
