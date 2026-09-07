namespace Household.Application.Commands.ConvertManagedMemberToAccount;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ConvertManagedMemberToAccountCommandHandler
    : ICommandHandler<ConvertManagedMemberToAccountCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IPersonRepository _persons;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public ConvertManagedMemberToAccountCommandHandler(
        HouseholdAccessService access,
        IPersonRepository persons,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _persons, _unitOfWork) = (access, persons, unitOfWork);

    public async Task HandleAsync(ConvertManagedMemberToAccountCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var person = await _persons.GetByIdAsync(PersonId.From(command.PersonId), ct)
            ?? throw new NotFoundException("Person", command.PersonId);

        if (!household.HasMember(person.Id))
            throw new ForbiddenException("That person is not a member of this household.");

        person.MarkPendingAccountLink(PersonEmail.Create(command.Email));

        await _unitOfWork.CommitAsync(ct);
    }
}
