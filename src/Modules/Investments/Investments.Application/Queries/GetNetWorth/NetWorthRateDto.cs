namespace Investments.Application.Queries.GetNetWorth;

/// <summary>An NBP rate a net-worth figure used.</summary>
/// <param name="Currency"><c>EUR</c> or <c>USD</c>.</param>
/// <param name="Mid">PLN per unit, e.g. <c>"4.267100"</c>.</param>
/// <param name="EffectiveDate">The business day it applies to.</param>
/// <param name="TableNo">The NBP table number.</param>
public sealed record NetWorthRateDto(string Currency, string Mid, DateOnly EffectiveDate, string TableNo);
