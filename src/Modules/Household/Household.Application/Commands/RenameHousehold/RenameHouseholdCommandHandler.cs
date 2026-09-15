namespace Household.Application.Commands.RenameHousehold;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class RenameHouseholdCommandHandler(
    HouseholdAccessService access,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RenameHouseholdCommand>
{
    public async Task HandleAsync(RenameHouseholdCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        household.Rename(command.Name, now);
        await unitOfWork.CommitAsync(ct);
    }
}
