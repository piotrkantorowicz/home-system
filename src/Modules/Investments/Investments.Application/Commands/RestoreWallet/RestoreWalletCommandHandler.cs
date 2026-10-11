namespace Investments.Application.Commands.RestoreWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class RestoreWalletCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IInvestmentsUnitOfWork unitOfWork,
    IInvestmentsReadDbContext db) : ICommandHandler<RestoreWalletCommand, WalletDto>
{
    public async Task<WalletDto> HandleAsync(RestoreWalletCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.Id, ct);

        wallet.Restore();
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return await db.ReadWalletAsync(caller, wallet.Id.Value, ct);
    }
}
