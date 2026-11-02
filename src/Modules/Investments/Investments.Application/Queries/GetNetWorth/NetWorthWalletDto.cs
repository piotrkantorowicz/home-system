namespace Investments.Application.Queries.GetNetWorth;

/// <summary>One wallet's contribution to net worth.</summary>
/// <param name="Id">The wallet.</param>
/// <param name="Name">Display name.</param>
/// <param name="Kind">Kind, e.g. <c>IKE</c>.</param>
/// <param name="Currency"><c>PLN</c>, <c>EUR</c> or <c>USD</c>.</param>
/// <param name="Balance">Latest balance on or before the date, in the wallet's currency; <see langword="null"/> when none.</param>
/// <param name="BalanceAsOf">Date of <paramref name="Balance"/>.</param>
/// <param name="BalancePln">That balance in PLN at the reference date's rate; <see langword="null"/> without a balance or rate.</param>
/// <param name="RateMissing">A foreign balance with no NBP rate on or before the reference date.</param>
/// <param name="Invested">Net deposits on or before the reference date, in the wallet's currency; <see langword="null"/> when none recorded.</param>
/// <param name="InvestedPln"><paramref name="Invested"/> in PLN at the balance's rate; <see langword="null"/> without it or a rate.</param>
/// <param name="GainPln"><paramref name="BalancePln"/> − <paramref name="InvestedPln"/>; <see langword="null"/> unless both are present.</param>
public sealed record NetWorthWalletDto(
    Guid Id,
    string Name,
    string Kind,
    string Currency,
    string? Balance,
    DateOnly? BalanceAsOf,
    string? BalancePln,
    bool RateMissing,
    string? Invested,
    string? InvestedPln,
    string? GainPln);
