namespace Budget.Application.Commands.ImportExpenses;

/// <summary>One reviewed statement row, with the values the user confirmed. Fields follow <c>CreateExpenseCommand</c>.</summary>
/// <param name="SourceKey">The preview's source key: 64 lowercase hex characters, once per import.</param>
/// <param name="Amount">Positive decimal string, at most two decimals.</param>
/// <param name="OccurredOn">Purchase date.</param>
/// <param name="Category">Category code.</param>
/// <param name="FundingSource"><c>Individual</c> (default) or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer of an individually funded shared expense; omitted otherwise.</param>
/// <param name="ParticipantIds">Adults sharing the cost; required for individually funded shared expenses.</param>
/// <param name="Description">Optional short note, at most 80 characters.</param>
/// <param name="MerchantKey">The preview's merchant key; when present, <paramref name="Category"/> is learned for it.</param>
public sealed record ImportExpenseRow(
    string SourceKey,
    string Amount,
    DateOnly OccurredOn,
    string Category,
    string? FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid>? ParticipantIds,
    string? Description,
    string? MerchantKey = null);
