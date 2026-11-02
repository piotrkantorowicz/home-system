namespace Investments.Domain.NetWorth;

using Investments.Domain.Fx;

/// <summary>A wallet's contribution on a date.</summary>
/// <param name="Wallet">The wallet.</param>
/// <param name="Balance">Its latest balance on or before the date; <see langword="null"/> when none (contributes nothing).</param>
/// <param name="Pln">That balance in PLN at the date's rate; <see langword="null"/> without a balance.</param>
/// <param name="Invested">Net deposits on or before the date, in the wallet's currency; <see langword="null"/> when none recorded.</param>
/// <param name="InvestedPln"><paramref name="Invested"/> in PLN at the same rate as the balance; <see langword="null"/> without it.</param>
public sealed record WalletValue(NetWorthWallet Wallet, NetWorthBalance? Balance, PlnValue? Pln, decimal? Invested = null, PlnValue? InvestedPln = null)
{
    /// <summary>Balance − invested in PLN; <see langword="null"/> unless both have a PLN amount.</summary>
    public decimal? GainPln => Pln?.Amount - InvestedPln?.Amount;
}
