namespace Investments.Application.Commands.RecordSnapshot;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Records a wallet's balance for a date, or corrects the one already recorded for that date.
/// Repeating the same request changes nothing.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="WalletId">A wallet the caller may see.</param>
/// <param name="AsOf">The date; not in the future.</param>
/// <param name="Amount">Decimal string, ≥ 0, at most two decimals, e.g. <c>"12450.10"</c>.</param>
/// <param name="Note">Optional note, at most 200 characters.</param>
public sealed record RecordSnapshotCommand(string AuthSubject, Guid WalletId, DateOnly AsOf, string Amount, string? Note)
    : ICommand<RecordSnapshotResult>;
