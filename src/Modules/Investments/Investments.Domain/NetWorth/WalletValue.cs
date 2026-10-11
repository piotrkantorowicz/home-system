namespace Investments.Domain.NetWorth;

using Investments.Domain.Fx;

/// <summary>A wallet's contribution on a date.</summary>
/// <param name="Wallet">The wallet.</param>
/// <param name="Balance">Its latest balance on or before the date; <see langword="null"/> when none (contributes nothing).</param>
/// <param name="Pln">That balance in PLN at the date's rate; <see langword="null"/> without a balance.</param>
public sealed record WalletValue(NetWorthWallet Wallet, NetWorthBalance? Balance, PlnValue? Pln);
