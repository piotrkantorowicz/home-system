using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

namespace Household.Application.Commands.CreateHousehold;

internal sealed class CreateHouseholdCommandHandler : ICommandHandler<CreateHouseholdCommand, Guid>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public CreateHouseholdCommandHandler(
        HouseholdAccessService access,
        IHouseholdRepository households,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _households, _unitOfWork) = (access, households, unitOfWork);

    public async Task<Guid> HandleAsync(CreateHouseholdCommand command, CancellationToken ct)
    {
        var caller = await _access.RequirePersonAsync(command.RequestingAuthSubject, ct);

        if (await _households.GetByMemberPersonIdAsync(caller.Id, ct) is not null)
            throw new HouseholdDomainException("You already belong to a household.");

        var household = HouseholdAggregate.Create(HouseholdId.New(), command.Name, caller.Id);
        await _households.AddAsync(household, ct);
        await _unitOfWork.CommitAsync(ct);

        return household.Id.Value;
    }
}
