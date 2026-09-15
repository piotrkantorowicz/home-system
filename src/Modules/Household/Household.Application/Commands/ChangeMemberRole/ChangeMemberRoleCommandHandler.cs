namespace Household.Application.Commands.ChangeMemberRole;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class ChangeMemberRoleCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ChangeMemberRoleCommand>
{
    public async Task HandleAsync(ChangeMemberRoleCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var targetId = PersonId.From(command.PersonId);

        if (command.Role == HouseholdRole.Owner
            && household.RoleOf(targetId) != HouseholdRole.Owner
            && await persons.GetByIdAsync(targetId, ct) is { IsManaged: true })
        {
            throw new HouseholdDomainException("A managed member cannot be made an owner.");
        }

        household.ChangeMemberRole(targetId, command.Role, now);
        await unitOfWork.CommitAsync(ct);
    }
}
