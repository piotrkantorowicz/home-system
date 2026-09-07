using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RemoveMember;

internal sealed class RemoveMemberCommandHandler : ICommandHandler<RemoveMemberCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public RemoveMemberCommandHandler(HouseholdAccessService access, IHouseholdUnitOfWork unitOfWork)
        => (_access, _unitOfWork) = (access, unitOfWork);

    public async Task HandleAsync(RemoveMemberCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        household.RemoveMember(PersonId.From(command.PersonId));
        await _unitOfWork.CommitAsync(ct);
    }
}
