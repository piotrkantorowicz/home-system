namespace Investments.Domain.NetWorth;

/// <summary>Net worth on one date.</summary>
/// <param name="Date">The reference date.</param>
/// <param name="Total">Sum of every wallet's PLN value that has a rate.</param>
/// <param name="Incomplete">At least one foreign balance had no rate and is left out of <paramref name="Total"/>.</param>
/// <param name="Wallets">Each wallet's contribution.</param>
public sealed record NetWorthPoint(DateOnly Date, decimal Total, bool Incomplete, IReadOnlyList<WalletValue> Wallets);
