using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.CreateManagedMember;

internal sealed class CreateManagedMemberCommandHandler
    : ICommandHandler<CreateManagedMemberCommand, Guid>
{
    private readonly HouseholdAccessService _access;
    private readonly IPersonRepository _persons;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public CreateManagedMemberCommandHandler(
        HouseholdAccessService access,
        IPersonRepository persons,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _persons, _unitOfWork) = (access, persons, unitOfWork);

    public async Task<Guid> HandleAsync(CreateManagedMemberCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var person = Person.CreateManaged(
            PersonId.New(),
            command.DisplayName,
            PersonEmail.CreateOrNull(command.Email));

        await _persons.AddAsync(person, ct);
        household.AddMember(person.Id, command.Role, command.Nickname);
        await _unitOfWork.CommitAsync(ct);

        return person.Id.Value;
    }
}
