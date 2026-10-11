namespace Investments.Application.Commands.RecordSnapshot;

using Investments.Application.Common;
using Investments.Application.Fx;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;

internal sealed partial class RecordSnapshotCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IBalanceSnapshotRepository snapshots,
    IInvestmentsUnitOfWork unitOfWork,
    FxRateSync fx,
    TimeProvider clock,
    ILogger<RecordSnapshotCommandHandler> logger) : ICommandHandler<RecordSnapshotCommand, RecordSnapshotResult>
{
    public async Task<RecordSnapshotResult> HandleAsync(RecordSnapshotCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.WalletId, ct);
        if (!Money.TryParse(command.Amount, out var amount))
            throw new InvalidOperationException("Amount is checked by RecordSnapshotCommandValidator.");
        var now = clock.GetUtcNow();
        var today = HouseholdCalendar.Today(clock);

        var snapshot = await snapshots.GetAsync(wallet.Id, command.AsOf, ct);
        var created = snapshot is null;
        if (snapshot is null)
        {
            snapshot = BalanceSnapshot.Record(
                BalanceSnapshotId.New(), wallet, command.AsOf, amount, command.Note,
                caller.PersonId, caller.NameOf(caller.PersonId) ?? "Unknown", today, now);
            await snapshots.AddAsync(snapshot, ct);
        }
        else
        {
            snapshot.Correct(amount, command.Note, now);
        }

        await unitOfWork.CommitOrThrowConflictAsync(ct);

        if (wallet.Currency != WalletCurrency.PLN)
        {
            // Backfill rates for a date older than any stored one. NBP being down never fails the
            // write: the read side shows rateMissing until the worker fills the gap.
            try
            {
                await fx.SyncAsync(wallet.Currency, today, ct);
            }
            catch (NbpUnavailableException ex)
            {
                LogBackfillFailed(ex, wallet.Currency);
            }
        }

        return new RecordSnapshotResult(snapshot.AsOf, amount.ToString(), snapshot.Note, created);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = "NBP rate backfill for {Currency} failed; the worker will retry")]
    private partial void LogBackfillFailed(Exception exception, WalletCurrency currency);
}
