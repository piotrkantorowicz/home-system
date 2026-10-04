namespace Budget.Application.Queries.GetSummary;

/// <summary>One envelope's month: spent, optional limit and what remains.</summary>
/// <param name="AccountId">The envelope.</param>
/// <param name="Name">Display name.</param>
/// <param name="Visibility"><c>Household</c> or <c>Personal</c>.</param>
/// <param name="OwnerPersonId">Owner of a personal envelope.</param>
/// <param name="IsArchived">Archived envelopes appear only when they have spending or a limit this month.</param>
/// <param name="Spent">Active expenses with a purchase date in the month.</param>
/// <param name="ExpenseCount">Number of those active expenses.</param>
/// <param name="Limit"><see langword="null"/> means no limit; <c>"0.00"</c> is a real limit.</param>
/// <param name="LimitRevision">Revision of the limit, to change or clear it.</param>
/// <param name="Remaining">Limit minus spent; negative when overspent. <see langword="null"/> without a limit.</param>
/// <param name="IsOverspent">Spent exceeds the limit (never true without one). Informational — it blocks nothing.</param>
public sealed record SummaryEnvelopeDto(
    Guid AccountId,
    string Name,
    string Visibility,
    Guid? OwnerPersonId,
    bool IsArchived,
    string Spent,
    int ExpenseCount,
    string? Limit,
    int? LimitRevision,
    string? Remaining,
    bool IsOverspent);
