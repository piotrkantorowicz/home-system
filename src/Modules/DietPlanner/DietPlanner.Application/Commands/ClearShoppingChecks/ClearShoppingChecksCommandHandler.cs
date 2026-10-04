namespace DietPlanner.Application.Commands.ClearShoppingChecks;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ClearShoppingChecksCommandHandler(
    IShoppingCheckRepository repository,
    HouseholdRosterProvider households) : ICommandHandler<ClearShoppingChecksCommand>
{
    public async Task HandleAsync(ClearShoppingChecksCommand command, CancellationToken ct = default)
    {
        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        if (!roster.CanPlanFor(command.PersonId))
            throw new ForbiddenException("Your household role does not allow changing the shopping list.");

        await repository.ClearAsync(roster.ScopeId, command.From, command.To, ct);
    }
}
