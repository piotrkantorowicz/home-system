namespace Household.Application.Commands.CreateManagedMember;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class CreateManagedMemberCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<CreateManagedMemberCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateManagedMemberCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var person = Person.CreateManaged(
            PersonId.New(),
            command.DisplayName,
            PersonEmail.CreateOrNull(command.Email),
            now);

        await persons.AddAsync(person, ct);
        household.AddMember(person.Id, command.Role, now, command.Nickname);
        await unitOfWork.CommitAsync(ct);

        return person.Id.Value;
    }
}
