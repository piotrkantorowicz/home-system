namespace Budget.Application.Queries.ListLimits;

/// <summary>A monthly spending target for one envelope.</summary>
/// <param name="AccountId">The envelope.</param>
/// <param name="Month"><c>YYYY-MM</c>.</param>
/// <param name="Amount">Decimal string with two fractional digits; <c>"0.00"</c> is a real limit.</param>
/// <param name="Revision">Send back when changing or clearing the limit.</param>
public sealed record MonthlyLimitDto(Guid AccountId, string Month, string Amount, int Revision);
