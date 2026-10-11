namespace Investments.Application.Commands.RecordCheckIn;

/// <summary>One wallet's balance in a check-in.</summary>
/// <param name="WalletId">A wallet the caller may see.</param>
/// <param name="Amount">Decimal string, ≥ 0, at most two decimals.</param>
/// <param name="Note">Optional note, at most 200 characters.</param>
public sealed record CheckInEntry(Guid WalletId, string Amount, string? Note);
