namespace Household.Application.Commands.AddExistingPersonAsMember;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class AddExistingPersonAsMemberCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    IHouseholdRepository households,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<AddExistingPersonAsMemberCommand>
{
    public async Task HandleAsync(AddExistingPersonAsMemberCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var targetId = PersonId.From(command.PersonId);
        var target = await persons.GetByIdAsync(targetId, ct)
            ?? throw new NotFoundException("Person", command.PersonId);

        if (await households.GetByMemberPersonIdAsync(target.Id, ct) is not null)
            throw new HouseholdDomainException($"{target.DisplayName} already belongs to a household.");

        household.AddMember(target.Id, command.Role, now, command.Nickname);
        await unitOfWork.CommitAsync(ct);
    }
}
