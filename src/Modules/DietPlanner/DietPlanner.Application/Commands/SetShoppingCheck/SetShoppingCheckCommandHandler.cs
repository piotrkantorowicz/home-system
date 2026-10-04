namespace DietPlanner.Application.Commands.SetShoppingCheck;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class SetShoppingCheckCommandHandler(
    IShoppingCheckRepository repository,
    HouseholdRosterProvider households,
    TimeProvider clock) : ICommandHandler<SetShoppingCheckCommand>
{
    public async Task HandleAsync(SetShoppingCheckCommand command, CancellationToken ct = default)
    {
        HouseholdRoster roster = await households.GetAsync(command.PersonId, command.AuthSubject, ct);
        if (!roster.CanPlanFor(command.PersonId))
            throw new ForbiddenException("Your household role does not allow changing the shopping list.");

        var productId = ProductId.From(command.ProductId);
        var unit = command.Unit.Trim();

        if (command.IsChecked)
            await repository.CheckAsync(
                ShoppingCheck.Create(roster.ScopeId, command.From, command.To, productId, unit, command.PersonId, clock.GetUtcNow().UtcDateTime), ct);
        else
            await repository.UncheckAsync(roster.ScopeId, command.From, command.To, productId, unit, ct);
    }
}
