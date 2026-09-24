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

        if (invitation.HasExpired(now))
        {
            invitation.Expire(now);
            await unitOfWork.CommitAsync(ct);
            throw new HouseholdDomainException("This invitation has expired.");
        }

        if (!invitation.IsPending)
            throw new HouseholdDomainException($"This invitation is already {invitation.Status.ToString().ToLowerInvariant()}.");

        var household = await households.GetByIdAsync(invitation.HouseholdId, ct)
            ?? throw new NotFoundException("Household", invitation.HouseholdId.Value);

        household.AddMember(caller.Id, invitation.Role, now, invitation.Nickname);
        invitation.Accept(now);

        await unitOfWork.CommitAsync(ct);
    }
}
