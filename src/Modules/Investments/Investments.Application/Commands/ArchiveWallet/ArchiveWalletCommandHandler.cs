namespace Investments.Application.Commands.ArchiveWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class ArchiveWalletCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IInvestmentsUnitOfWork unitOfWork,
    IInvestmentsReadDbContext db) : ICommandHandler<ArchiveWalletCommand, WalletDto>
{
    public async Task<WalletDto> HandleAsync(ArchiveWalletCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.Id, ct);

        wallet.Archive();
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return await db.ReadWalletAsync(caller, wallet.Id.Value, ct);
    }
}
