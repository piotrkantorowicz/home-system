namespace Household.Application.Commands.LeaveHousehold;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class LeaveHouseholdCommandHandler(
    HouseholdAccessService access,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<LeaveHouseholdCommand>
{
    public async Task HandleAsync(LeaveHouseholdCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (caller, household) = await access.RequireMemberAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        // The last-owner rule in the aggregate stops the sole owner from leaving —
        // they must hand ownership to someone else or delete the household first.
        household.RemoveMember(caller.Id, now);
        await unitOfWork.CommitAsync(ct);
    }
}
