namespace Investments.Application.Queries.GetNetWorth;

/// <summary>Net worth on one date, with its breakdowns.</summary>
/// <param name="AsOf">The reference date.</param>
/// <param name="Scope"><c>household</c> or <c>mine</c>.</param>
/// <param name="Total">PLN total of every value that has a rate, e.g. <c>"345012.20"</c>.</param>
/// <param name="Incomplete">Some foreign balance had no NBP rate and is left out of the total.</param>
/// <param name="ByKind">PLN total per wallet kind, largest first.</param>
/// <param name="ByOwner">PLN total per owner, largest first.</param>
/// <param name="Wallets">Every in-scope wallet's contribution.</param>
/// <param name="RatesUsed">The NBP rates the conversion used.</param>
public sealed record NetWorthDto(
    DateOnly AsOf,
    string Scope,
    string Total,
    bool Incomplete,
    IReadOnlyList<NetWorthKindDto> ByKind,
    IReadOnlyList<NetWorthOwnerDto> ByOwner,
    IReadOnlyList<NetWorthWalletDto> Wallets,
    IReadOnlyList<NetWorthRateDto> RatesUsed);
