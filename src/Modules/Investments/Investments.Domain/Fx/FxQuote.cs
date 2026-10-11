namespace Investments.Domain.Fx;

using Investments.Domain.ValueObjects;

/// <summary>A stored NBP rate as read for valuation; the read-side projection of <see cref="FxRate"/>.</summary>
/// <param name="Currency">The foreign currency.</param>
/// <param name="EffectiveDate">The business day the rate applies to.</param>
/// <param name="Mid">PLN per unit.</param>
/// <param name="TableNo">The NBP table number.</param>
public sealed record FxQuote(WalletCurrency Currency, DateOnly EffectiveDate, decimal Mid, string TableNo);
