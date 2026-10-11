namespace Investments.Application.Commands.UpdateWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Abstractions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateWalletCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IInvestmentsUnitOfWork unitOfWork,
    IInvestmentsReadDbContext db) : ICommandHandler<UpdateWalletCommand, WalletDto>
{
    public async Task<WalletDto> HandleAsync(UpdateWalletCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.Id, ct);

        if (wallet.Version != command.Version)
            throw new ConflictException("This wallet was changed by someone else. Reload and try again.");

        var visibility = EnumValue.Parse<WalletVisibility>(command.Visibility); // validated
        if (visibility != wallet.Visibility && !caller.CanChangeVisibility(wallet.OwnerPersonId))
            throw new ForbiddenException("Only the wallet's owner, or an adult managing them, can change its visibility.");

        wallet.Rename(command.Name);
        wallet.ChangeVisibility(visibility);
        await unitOfWork.CommitOrThrowConflictAsync(ct);

        return await db.ReadWalletAsync(caller, wallet.Id.Value, ct);
    }
}
