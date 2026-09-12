namespace Household.Application.Commands.LeaveHousehold;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class LeaveHouseholdCommandHandler : ICommandHandler<LeaveHouseholdCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public LeaveHouseholdCommandHandler(HouseholdAccessService access, IHouseholdUnitOfWork unitOfWork)
        => (_access, _unitOfWork) = (access, unitOfWork);

    public async Task HandleAsync(LeaveHouseholdCommand command, CancellationToken ct)
    {
        var (caller, household) = await _access.RequireMemberAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        // The last-owner rule in the aggregate stops the sole owner from leaving —
        // they must hand ownership to someone else or delete the household first.
        household.RemoveMember(caller.Id);
        await _unitOfWork.CommitAsync(ct);
    }
}
