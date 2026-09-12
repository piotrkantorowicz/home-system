namespace Household.Application.Commands.ChangeMemberRole;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class ChangeMemberRoleCommandHandler : ICommandHandler<ChangeMemberRoleCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IPersonRepository _persons;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public ChangeMemberRoleCommandHandler(
        HouseholdAccessService access,
        IPersonRepository persons,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _persons, _unitOfWork) = (access, persons, unitOfWork);

    public async Task HandleAsync(ChangeMemberRoleCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var targetId = PersonId.From(command.PersonId);

        if (command.Role == HouseholdRole.Owner
            && household.RoleOf(targetId) != HouseholdRole.Owner
            && await _persons.GetByIdAsync(targetId, ct) is { IsManaged: true })
        {
            throw new HouseholdDomainException("A managed member cannot be made an owner.");
        }

        household.ChangeMemberRole(targetId, command.Role);
        await _unitOfWork.CommitAsync(ct);
    }
}
