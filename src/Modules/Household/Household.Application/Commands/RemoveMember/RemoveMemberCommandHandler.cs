namespace Household.Application.Commands.RemoveMember;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class RemoveMemberCommandHandler(
    HouseholdAccessService access,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RemoveMemberCommand>
{
    public async Task HandleAsync(RemoveMemberCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        household.RemoveMember(PersonId.From(command.PersonId), now);
        await unitOfWork.CommitAsync(ct);
    }
}
