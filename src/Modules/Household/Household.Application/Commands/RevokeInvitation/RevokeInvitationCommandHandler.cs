namespace Household.Application.Commands.RevokeInvitation;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class RevokeInvitationCommandHandler(
    HouseholdAccessService access,
    IHouseholdInvitationRepository invitations,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RevokeInvitationCommand>
{
    public async Task HandleAsync(RevokeInvitationCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (_, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var invitation = await invitations.GetByIdAsync(
            HouseholdInvitationId.From(command.InvitationId), ct)
            ?? throw new NotFoundException("Invitation", command.InvitationId);

        if (invitation.HouseholdId != household.Id)
            throw new NotFoundException("Invitation", command.InvitationId);

        invitation.Revoke(now);
        await unitOfWork.CommitAsync(ct);
    }
}
