namespace Household.Application.Commands.RevokeInvitation;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class RevokeInvitationCommandHandler : ICommandHandler<RevokeInvitationCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdInvitationRepository _invitations;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public RevokeInvitationCommandHandler(
        HouseholdAccessService access,
        IHouseholdInvitationRepository invitations,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _invitations, _unitOfWork) = (access, invitations, unitOfWork);

    public async Task HandleAsync(RevokeInvitationCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var invitation = await _invitations.GetByIdAsync(
            HouseholdInvitationId.From(command.InvitationId), ct)
            ?? throw new NotFoundException("Invitation", command.InvitationId);

        if (invitation.HouseholdId != household.Id)
            throw new NotFoundException("Invitation", command.InvitationId);

        invitation.Revoke(DateTime.UtcNow);
        await _unitOfWork.CommitAsync(ct);
    }
}
