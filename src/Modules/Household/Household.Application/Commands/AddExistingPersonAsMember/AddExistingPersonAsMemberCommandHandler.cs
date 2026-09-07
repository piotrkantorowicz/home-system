using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.AddExistingPersonAsMember;

internal sealed class AddExistingPersonAsMemberCommandHandler
    : ICommandHandler<AddExistingPersonAsMemberCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IPersonRepository _persons;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public AddExistingPersonAsMemberCommandHandler(
        HouseholdAccessService access,
        IPersonRepository persons,
        IHouseholdRepository households,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _persons, _households, _unitOfWork) = (access, persons, households, unitOfWork);

    public async Task HandleAsync(AddExistingPersonAsMemberCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var targetId = PersonId.From(command.PersonId);
        var target = await _persons.GetByIdAsync(targetId, ct)
            ?? throw new NotFoundException("Person", command.PersonId);

        if (await _households.GetByMemberPersonIdAsync(target.Id, ct) is not null)
            throw new HouseholdDomainException($"{target.DisplayName} already belongs to a household.");

        household.AddMember(target.Id, command.Role, command.Nickname);
        await _unitOfWork.CommitAsync(ct);
    }
}
