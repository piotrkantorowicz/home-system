using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.InvitePersonByEmail;

internal sealed class InvitePersonByEmailCommandHandler
    : ICommandHandler<InvitePersonByEmailCommand, InvitePersonByEmailResult>
{
    private readonly HouseholdAccessService _access;
    private readonly IPersonRepository _persons;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdInvitationRepository _invitations;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public InvitePersonByEmailCommandHandler(
        HouseholdAccessService access,
        IPersonRepository persons,
        IHouseholdRepository households,
        IHouseholdInvitationRepository invitations,
        IHouseholdUnitOfWork unitOfWork)
    {
        _access = access;
        _persons = persons;
        _households = households;
        _invitations = invitations;
        _unitOfWork = unitOfWork;
    }

    public async Task<InvitePersonByEmailResult> HandleAsync(
        InvitePersonByEmailCommand command, CancellationToken ct)
    {
        var (caller, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var email = PersonEmail.Create(command.Email);

        var existing = await _persons.GetByEmailAsync(email, ct);
        if (existing is not null)
        {
            if (household.HasMember(existing.Id))
                throw new HouseholdDomainException($"{existing.DisplayName} is already in this household.");

            if (await _households.GetByMemberPersonIdAsync(existing.Id, ct) is not null)
                throw new HouseholdDomainException($"{existing.DisplayName} already belongs to a household.");

            household.AddMember(existing.Id, command.Role);
            await _unitOfWork.CommitAsync(ct);
            return new InvitePersonByEmailResult(AddedImmediately: true, existing.Id.Value, InvitationId: null);
        }

        if (await _invitations.HasPendingForEmailInHouseholdAsync(household.Id, email, ct))
            throw new HouseholdDomainException("There is already a pending invitation for that email.");

        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), household.Id, email, command.Role, caller.Id);

        await _invitations.AddAsync(invitation, ct);
        await _unitOfWork.CommitAsync(ct);

        return new InvitePersonByEmailResult(AddedImmediately: false, PersonId: null, invitation.Id.Value);
    }
}
