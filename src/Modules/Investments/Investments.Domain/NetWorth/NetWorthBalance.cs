namespace Investments.Domain.NetWorth;

using Investments.Domain.ValueObjects;

/// <summary>One recorded balance.</summary>
/// <param name="WalletId">The wallet.</param>
/// <param name="AsOf">Its date.</param>
/// <param name="Amount">The amount in the wallet's currency.</param>
public sealed record NetWorthBalance(WalletId WalletId, DateOnly AsOf, decimal Amount);
