namespace Budget.Application.Queries.GetExpense;

/// <summary>An expense as seen by an authorised caller.</summary>
/// <param name="Id">Expense identifier.</param>
/// <param name="AccountId">The envelope it was recorded in.</param>
/// <param name="Amount">Decimal string with two fractional digits, e.g. <c>"123.45"</c>.</param>
/// <param name="Category">Category code.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="FundingSource"><c>Individual</c> or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Whose money paid; <see langword="null"/> for household funds.</param>
/// <param name="PaidByDisplayName">Current roster name when the payer is a member, else the stored snapshot.</param>
/// <param name="AddedByPersonId">Who recorded it.</param>
/// <param name="AddedByDisplayName">Current roster name when the recorder is a member, else the stored snapshot.</param>
/// <param name="Revision">Current revision number.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="IsVoided">Voided expenses no longer count and cannot be edited.</param>
/// <param name="Shares">Exact stored shares; empty when not shared.</param>
/// <param name="History">Immutable revision history, oldest first; only on the detail response (<see langword="null"/> in lists).</param>
public sealed record ExpenseDto(
    Guid Id,
    Guid AccountId,
    string Amount,
    string Category,
    DateOnly OccurredOn,
    string FundingSource,
    Guid? PaidByPersonId,
    string? PaidByDisplayName,
    Guid AddedByPersonId,
    string AddedByDisplayName,
    int Revision,
    DateTime CreatedAt,
    bool IsVoided,
    IReadOnlyList<ExpenseShareDto> Shares,
    IReadOnlyList<ExpenseRevisionDto>? History = null);
