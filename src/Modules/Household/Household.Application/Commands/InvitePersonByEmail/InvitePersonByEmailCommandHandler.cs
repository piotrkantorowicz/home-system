namespace Household.Application.Commands.InvitePersonByEmail;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class InvitePersonByEmailCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    IHouseholdRepository households,
    IHouseholdInvitationRepository invitations,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<InvitePersonByEmailCommand, InvitePersonByEmailResult>
{
    public async Task<InvitePersonByEmailResult> HandleAsync(
        InvitePersonByEmailCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var (caller, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var email = PersonEmail.Create(command.Email);

        var existing = await persons.GetByEmailAsync(email, ct);
        if (existing is not null)
        {
            if (household.HasMember(existing.Id))
                throw new HouseholdDomainException($"{existing.DisplayName} is already in this household.");

            if (await households.GetByMemberPersonIdAsync(existing.Id, ct) is not null)
                throw new HouseholdDomainException($"{existing.DisplayName} already belongs to a household.");

            household.AddMember(existing.Id, command.Role, now);
            await unitOfWork.CommitAsync(ct);
            return new InvitePersonByEmailResult(AddedImmediately: true, existing.Id.Value, InvitationId: null);
        }

        if (await invitations.HasPendingForEmailInHouseholdAsync(household.Id, email, ct))
            throw new HouseholdDomainException("There is already a pending invitation for that email.");

        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(), household.Id, email, command.Role, caller.Id, now);

        await invitations.AddAsync(invitation, ct);
        await unitOfWork.CommitAsync(ct);

        return new InvitePersonByEmailResult(AddedImmediately: false, PersonId: null, invitation.Id.Value);
    }
}
