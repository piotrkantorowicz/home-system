namespace Investments.Application.Queries.GetNetWorthHistory;

/// <summary>Net worth on one date of a series.</summary>
/// <param name="Date">The date.</param>
/// <param name="Total">PLN total of every value that has a rate.</param>
/// <param name="Incomplete">Some foreign balance had no rate on that date.</param>
public sealed record NetWorthHistoryPointDto(DateOnly Date, string Total, bool Incomplete);
