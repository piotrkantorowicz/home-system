namespace Investments.Application.Commands.RecordSnapshot;

using Investments.Application.Common;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordSnapshotCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IBalanceSnapshotRepository snapshots,
    IInvestmentsUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RecordSnapshotCommand, RecordSnapshotResult>
{
    public async Task<RecordSnapshotResult> HandleAsync(RecordSnapshotCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.WalletId, ct);
        if (!Money.TryParse(command.Amount, out var amount))
            throw new InvalidOperationException("Amount is checked by RecordSnapshotCommandValidator.");
        var now = clock.GetUtcNow();

        var snapshot = await snapshots.GetAsync(wallet.Id, command.AsOf, ct);
        var created = snapshot is null;
        if (snapshot is null)
        {
            snapshot = BalanceSnapshot.Record(
                BalanceSnapshotId.New(), wallet, command.AsOf, amount, command.Note,
                caller.PersonId, caller.NameOf(caller.PersonId) ?? "Unknown", HouseholdCalendar.Today(clock), now);
            await snapshots.AddAsync(snapshot, ct);
        }
        else
        {
            snapshot.Correct(amount, command.Note, now);
        }

        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return new RecordSnapshotResult(snapshot.AsOf, amount.ToString(), snapshot.Note, created);
    }
}
