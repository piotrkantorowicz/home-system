namespace Investments.Application.Queries.GetWallet;

/// <summary>A wallet as seen by an authorised caller.</summary>
/// <param name="Id">Wallet identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Institution">Where it is held, e.g. <c>XTB</c>.</param>
/// <param name="Kind">Account kind, e.g. <c>IKE</c>.</param>
/// <param name="Currency"><c>PLN</c>, <c>EUR</c> or <c>USD</c>.</param>
/// <param name="Visibility"><c>Shared</c> or <c>Private</c>.</param>
/// <param name="OwnerPersonId">The legal owner.</param>
/// <param name="OwnerName">The owner's current name, or the stored snapshot once they left the roster.</param>
/// <param name="IsArchived">Hidden from active lists and totals.</param>
/// <param name="Version">Concurrency token; send it back on edit.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="CanChangeVisibility">Whether the caller may change visibility (owner or managing adult).</param>
/// <param name="LatestBalance">Latest recorded balance in the wallet's currency, e.g. <c>"12450.10"</c>; <see langword="null"/> before the first.</param>
/// <param name="LatestAsOf">Date of <paramref name="LatestBalance"/>.</param>
public sealed record WalletDto(
    Guid Id,
    string Name,
    string Institution,
    string Kind,
    string Currency,
    string Visibility,
    Guid OwnerPersonId,
    string OwnerName,
    bool IsArchived,
    uint Version,
    DateTimeOffset CreatedAt,
    bool CanChangeVisibility,
    string? LatestBalance,
    DateOnly? LatestAsOf);
