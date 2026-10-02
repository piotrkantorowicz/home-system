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
public sealed record ExpenseRequest(
    string Action,
    Guid AccountId,
    string Amount,
    ExpenseCategory Category,
    DateOnly OccurredOn,
    FundingSource FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid> ParticipantIds)
{
    /// <summary>Whether <paramref name="other"/> is the same logical request.</summary>
    /// <param name="other">A stored or incoming request.</param>
    public bool Matches(ExpenseRequest other)
        => Action == other.Action && AccountId == other.AccountId && Amount == other.Amount
           && Category == other.Category && OccurredOn == other.OccurredOn
           && FundingSource == other.FundingSource && PaidByPersonId == other.PaidByPersonId
           && ParticipantIds.SequenceEqual(other.ParticipantIds);
}
