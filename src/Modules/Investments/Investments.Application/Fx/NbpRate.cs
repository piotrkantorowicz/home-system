namespace Investments.Application.Fx;

/// <summary>One row of an NBP table A response.</summary>
/// <param name="EffectiveDate">The business day the rate applies to.</param>
/// <param name="Mid">PLN per unit.</param>
/// <param name="TableNo">The NBP table number, e.g. <c>172/A/NBP/2026</c>.</param>
public sealed record NbpRate(DateOnly EffectiveDate, decimal Mid, string TableNo);
