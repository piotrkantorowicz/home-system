namespace Budget.Application.Queries.GetExpense;

/// <summary>The full state an expense revision produced, with the names recorded at the time.</summary>
/// <param name="Amount">Decimal string with two fractional digits.</param>
/// <param name="Category">Category code.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="Description">Description in this state; <see langword="null"/> when none or recorded before descriptions existed.</param>
/// <param name="FundingSource"><c>Individual</c> or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer, or <see langword="null"/>.</param>
/// <param name="PaidByDisplayName">Payer name snapshot.</param>
/// <param name="AddedByPersonId">Recorder.</param>
/// <param name="AddedByDisplayName">Recorder name snapshot.</param>
/// <param name="IsVoided">Whether the expense was void in this state.</param>
/// <param name="Shares">The exact shares in this state.</param>
public sealed record ExpenseSnapshotDto(
    string Amount,
    string Category,
    DateOnly OccurredOn,
    string? Description,
    string FundingSource,
    Guid? PaidByPersonId,
    string? PaidByDisplayName,
    Guid AddedByPersonId,
    string AddedByDisplayName,
    bool IsVoided,
    IReadOnlyList<ExpenseShareDto> Shares);
