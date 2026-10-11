namespace Investments.Application.Queries.ListSnapshots;

using Investments.Application.Common;

/// <summary>One recorded balance, with the one before it so the change can be shown.</summary>
/// <param name="AsOf">The date the balance applies to.</param>
/// <param name="Amount">Decimal string in the wallet's currency, e.g. <c>"12450.10"</c>.</param>
/// <param name="PreviousAmount">The wallet's previous balance (latest earlier date), or <see langword="null"/> for the first.</param>
/// <param name="Note">Optional note.</param>
/// <param name="RecordedByName">Who first recorded it (current name while they are a member).</param>
/// <param name="RecordedAt">When it was first recorded, UTC.</param>
/// <param name="UpdatedAt">When the amount or note last changed, UTC.</param>
/// <param name="AmountPln">The amount in PLN at the NBP rate for <paramref name="AsOf"/>; <see langword="null"/> when the rate is missing.</param>
/// <param name="Rate">The NBP rate used; <see langword="null"/> for PLN wallets and when missing.</param>
/// <param name="RateMissing">A foreign amount with no NBP rate on or before its date yet.</param>
public sealed record SnapshotDto(
    DateOnly AsOf,
    string Amount,
    string? PreviousAmount,
    string? Note,
    string RecordedByName,
    DateTimeOffset RecordedAt,
    DateTimeOffset UpdatedAt,
    string? AmountPln,
    FxRateDto? Rate,
    bool RateMissing);
