namespace Investments.Application.Queries.ListSnapshots;

/// <summary>One recorded balance, with the one before it so the change can be shown.</summary>
/// <param name="AsOf">The date the balance applies to.</param>
/// <param name="Amount">Decimal string in the wallet's currency, e.g. <c>"12450.10"</c>.</param>
/// <param name="PreviousAmount">The wallet's previous balance (latest earlier date), or <see langword="null"/> for the first.</param>
/// <param name="Note">Optional note.</param>
/// <param name="RecordedByName">Who first recorded it (current name while they are a member).</param>
/// <param name="RecordedAt">When it was first recorded, UTC.</param>
/// <param name="UpdatedAt">When the amount or note last changed, UTC.</param>
public sealed record SnapshotDto(
    DateOnly AsOf,
    string Amount,
    string? PreviousAmount,
    string? Note,
    string RecordedByName,
    DateTimeOffset RecordedAt,
    DateTimeOffset UpdatedAt);
