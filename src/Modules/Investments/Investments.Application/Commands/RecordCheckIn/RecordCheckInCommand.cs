namespace Investments.Application.Commands.RecordCheckIn;

using Shared.Abstractions.Cqrs;

/// <summary>Records balances for several wallets on one date, all or nothing.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="AsOf">The date for every entry; not in the future.</param>
/// <param name="Entries">One per wallet; wallets left out are skipped.</param>
public sealed record RecordCheckInCommand(string AuthSubject, DateOnly AsOf, IReadOnlyList<CheckInEntry> Entries)
    : ICommand<RecordCheckInResult>;
