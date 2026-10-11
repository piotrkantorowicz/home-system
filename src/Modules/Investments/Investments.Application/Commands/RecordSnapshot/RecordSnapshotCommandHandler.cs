namespace Investments.Application.Commands.RecordSnapshot;

using Investments.Application.Common;
using Investments.Application.Fx;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordSnapshotCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IBalanceSnapshotRepository snapshots,
    IInvestmentsUnitOfWork unitOfWork,
    FxRateSync fx,
    TimeProvider clock) : ICommandHandler<RecordSnapshotCommand, RecordSnapshotResult>
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

        // Backfill rates for a date older than any stored one; a failure never fails the committed write.
        if (wallet.Currency != WalletCurrency.PLN)
            await fx.TrySyncAsync(wallet.Currency, today, ct);

        return new RecordSnapshotResult(snapshot.AsOf, amount.ToString(), snapshot.Note, created);
    }
}
