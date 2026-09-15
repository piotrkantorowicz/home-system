namespace Household.Application.Commands.CreateHousehold;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

internal sealed class CreateHouseholdCommandHandler(
    HouseholdAccessService access,
    IHouseholdRepository households,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateHouseholdCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateHouseholdCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var caller = await access.RequirePersonAsync(command.RequestingAuthSubject, ct);

        if (await households.GetByMemberPersonIdAsync(caller.Id, ct) is not null)
            throw new HouseholdDomainException("You already belong to a household.");

        var household = HouseholdAggregate.Create(HouseholdId.New(), command.Name, caller.Id, now);
        await households.AddAsync(household, ct);
        await unitOfWork.CommitAsync(ct);

        return household.Id.Value;
    }
}
