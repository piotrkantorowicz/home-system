namespace Investments.Application.Commands.DeleteSnapshot;

using Investments.Application.Common;
using Investments.Domain.Abstractions;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteSnapshotCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IBalanceSnapshotRepository snapshots,
    IInvestmentsUnitOfWork unitOfWork) : ICommandHandler<DeleteSnapshotCommand>
{
    public async Task HandleAsync(DeleteSnapshotCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.WalletId, ct);
        var snapshot = await snapshots.GetAsync(wallet.Id, command.AsOf, ct)
            ?? throw new NotFoundException($"No balance is recorded for {command.AsOf:yyyy-MM-dd}.");

        snapshots.Remove(snapshot);
        await unitOfWork.CommitAsync(ct);
    }
}
