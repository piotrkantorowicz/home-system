namespace Household.Application.Commands.AcceptInvitation;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class AcceptInvitationCommandHandler(
    HouseholdAccessService access,
    IHouseholdInvitationRepository invitations,
    IHouseholdRepository households,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<AcceptInvitationCommand>
{
    public async Task HandleAsync(AcceptInvitationCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var caller = await access.RequirePersonAsync(command.RequestingAuthSubject, ct);

        var invitation = await invitations.GetByIdAsync(
            HouseholdInvitationId.From(command.InvitationId), ct)
            ?? throw new NotFoundException("Invitation", command.InvitationId);

        if (!invitation.IsAddressedTo(caller.Id, caller.Email))
            throw new ForbiddenException("This invitation is not addressed to you.");

        if (await households.GetByMemberPersonIdAsync(caller.Id, ct) is not null)
            throw new HouseholdDomainException("You already belong to a household.");

        // An expired-but-still-Pending row is never persisted as Expired here: this handler runs
        // inside an ambient transaction that only commits when it returns without throwing, so a
        // commit followed by a throw would silently roll back. Read paths (ListPendingInvitations,
        // ListMyInvitations, HouseholdInvitationIssuer's duplicate checks) filter on ExpiresAt
        // instead of relying on the Status ever flipping to Expired.
        if (invitation.HasExpired(now))
            throw new HouseholdDomainException("This invitation has expired.");

        if (!invitation.IsPending)
            throw new HouseholdDomainException($"This invitation is already {invitation.Status.ToString().ToLowerInvariant()}.");

        var household = await households.GetByIdAsync(invitation.HouseholdId, ct)
            ?? throw new NotFoundException("Household", invitation.HouseholdId.Value);

        household.AddMember(caller.Id, invitation.Role, now, invitation.Nickname);
        invitation.Accept(now);

        await unitOfWork.CommitOrThrowConflictAsync(ct);
    }
}
