namespace Investments.Application.Commands.RecordContribution;

using Investments.Application.Common;
using Investments.Application.Queries.ListContributions;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RecordContributionCommandHandler(
    InvestmentsAccessService access,
    IWalletRepository wallets,
    IContributionRepository contributions,
    IInvestmentsUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RecordContributionCommand, ContributionDto>
{
    public async Task<ContributionDto> HandleAsync(RecordContributionCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var wallet = await wallets.LoadAccessibleAsync(caller, command.WalletId, ct);
        if (!Money.TryParse(command.Amount, out var amount))
            throw new InvalidOperationException("Amount is checked by RecordContributionCommandValidator.");
        var name = caller.NameOf(caller.PersonId) ?? "Unknown";

        var contribution = Contribution.Record(
            ContributionId.New(), wallet, command.Date, EnumValue.Parse<ContributionDirection>(command.Direction), amount,
            command.Note, caller.PersonId, name, HouseholdCalendar.Today(clock), clock.GetUtcNow());
        await contributions.AddAsync(contribution, ct);
        await unitOfWork.CommitAsync(ct);

        return new ContributionDto(
            contribution.Id.Value, contribution.Date, contribution.Direction.ToString(), amount.ToString(),
            contribution.Note, name, contribution.RecordedAt);
    }
}
