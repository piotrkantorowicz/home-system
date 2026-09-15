namespace Household.Application.Commands.ConvertManagedMemberToAccount;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class ConvertManagedMemberToAccountCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<ConvertManagedMemberToAccountCommand>
{
    public async Task HandleAsync(ConvertManagedMemberToAccountCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var person = await persons.GetByIdAsync(PersonId.From(command.PersonId), ct)
            ?? throw new NotFoundException("Person", command.PersonId);

        if (!household.HasMember(person.Id))
            throw new ForbiddenException("That person is not a member of this household.");

        person.MarkPendingAccountLink(PersonEmail.Create(command.Email), now);

        await unitOfWork.CommitAsync(ct);
    }
}
