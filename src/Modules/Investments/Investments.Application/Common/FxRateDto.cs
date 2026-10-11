namespace Investments.Application.Common;

using System.Globalization;
using Investments.Domain.Fx;

/// <summary>The NBP rate a PLN value was computed with.</summary>
/// <param name="Mid">PLN per unit, six decimals, e.g. <c>"4.265100"</c>.</param>
/// <param name="EffectiveDate">The business day the rate applies to.</param>
/// <param name="TableNo">The NBP table number, e.g. <c>172/A/NBP/2026</c>.</param>
public sealed record FxRateDto(string Mid, DateOnly EffectiveDate, string TableNo)
{
    internal static FxRateDto? From(FxQuote? rate)
        => rate is null ? null : new(rate.Mid.ToString("F6", CultureInfo.InvariantCulture), rate.EffectiveDate, rate.TableNo);
}
