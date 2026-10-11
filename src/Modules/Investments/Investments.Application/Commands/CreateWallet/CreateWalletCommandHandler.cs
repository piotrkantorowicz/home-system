namespace Investments.Application.Commands.CreateWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateWalletCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IInvestmentsUnitOfWork unitOfWork,
    IInvestmentsReadDbContext db,
    TimeProvider clock) : ICommandHandler<CreateWalletCommand, WalletDto>
{
    public async Task<WalletDto> HandleAsync(CreateWalletCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);

        var owner = command.OwnerPersonId ?? caller.PersonId;
        if (!caller.ManagedOwnerIds.Contains(owner))
            throw new ForbiddenException("You can only create a wallet for yourself or a member you manage.");

        // Validated by CreateWalletCommandValidator.
        var wallet = Wallet.Create(
            WalletId.New(),
            caller.HouseholdId,
            owner,
            caller.NameOf(owner) ?? "Unknown",
            command.Name,
            EnumValue.Parse<WalletInstitution>(command.Institution),
            EnumValue.Parse<WalletKind>(command.Kind),
            EnumValue.Parse<WalletCurrency>(command.Currency),
            EnumValue.Parse<WalletVisibility>(command.Visibility),
            clock.GetUtcNow());

        await wallets.AddAsync(wallet, ct);
        await unitOfWork.CommitAsync(ct);

        return await db.ReadWalletAsync(caller, wallet.Id.Value, ct);
    }
}
