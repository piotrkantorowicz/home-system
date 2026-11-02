namespace Investments.Domain.NetWorth;

/// <summary>Net worth on one date.</summary>
/// <param name="Date">The reference date.</param>
/// <param name="Total">Sum of every wallet's PLN value that has a rate.</param>
/// <param name="Incomplete">At least one foreign balance had no rate and is left out of <paramref name="Total"/>.</param>
/// <param name="Wallets">Each wallet's contribution.</param>
public sealed record NetWorthPoint(DateOnly Date, decimal Total, bool Incomplete, IReadOnlyList<WalletValue> Wallets)
{
    /// <summary>PLN invested over the wallets that have a gain (contributions, a balance and a rate); <see langword="null"/> when none.</summary>
    public decimal? Invested => Gaining.Any() ? Gaining.Sum(v => v.InvestedPln!.Amount) : null;

    /// <summary>PLN gain over the same wallets as <see cref="Invested"/>; <see langword="null"/> when none.</summary>
    public decimal? Gain => Gaining.Any() ? Gaining.Sum(v => v.GainPln) : null;

    private IEnumerable<WalletValue> Gaining => Wallets.Where(v => v.GainPln is not null);
}
