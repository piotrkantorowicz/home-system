namespace Investments.Application.Commands.DeleteContribution;

using Investments.Application.Common;
using Investments.Domain.Abstractions;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteContributionCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IContributionRepository contributions,
    IInvestmentsUnitOfWork unitOfWork) : ICommandHandler<DeleteContributionCommand>
{
    public async Task HandleAsync(DeleteContributionCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.WalletId, ct);
        var contribution = await contributions.GetAsync(wallet.Id, ContributionId.From(command.ContributionId), ct)
            ?? throw new NotFoundException("Contribution", command.ContributionId);

        contributions.Remove(contribution);
        await unitOfWork.CommitAsync(ct);
    }
}
