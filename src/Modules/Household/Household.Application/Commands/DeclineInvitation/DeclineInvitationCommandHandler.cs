namespace Household.Application.Commands.DeclineInvitation;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeclineInvitationCommandHandler(
    HouseholdAccessService access,
    IHouseholdInvitationRepository invitations,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeclineInvitationCommand>
{
    public async Task HandleAsync(DeclineInvitationCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var caller = await access.RequirePersonAsync(command.RequestingAuthSubject, ct);

        var invitation = await invitations.GetByIdAsync(
            HouseholdInvitationId.From(command.InvitationId), ct)
            ?? throw new NotFoundException("Invitation", command.InvitationId);

        if (!invitation.IsAddressedTo(caller.Id, caller.Email))
            throw new ForbiddenException("This invitation is not addressed to you.");

        invitation.Decline(now);
        await unitOfWork.CommitOrThrowConflictAsync(ct);
    }
}
