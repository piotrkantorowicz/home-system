namespace Investments.Domain.NetWorth;

using Investments.Domain.ValueObjects;

/// <summary>One deposit or withdrawal, signed.</summary>
/// <param name="WalletId">The wallet.</param>
/// <param name="Date">Its date.</param>
/// <param name="Amount">In the wallet's currency; positive for a deposit, negative for a withdrawal.</param>
public sealed record NetWorthContribution(WalletId WalletId, DateOnly Date, decimal Amount);
