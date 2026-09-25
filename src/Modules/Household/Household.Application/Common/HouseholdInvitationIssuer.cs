namespace Household.Application.Common;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>
/// Issues a pending <see cref="HouseholdInvitation"/> addressed by email, by a known
/// <see cref="Person"/>, or both — shared by <c>InvitePersonByEmailCommandHandler</c> and
/// <c>AddExistingPersonAsMemberCommandHandler</c> so both routes enforce the same consent rule and
/// the same duplicate / already-a-member checks.
/// </summary>
internal sealed class HouseholdInvitationIssuer(
    IPersonRepository persons,
    IHouseholdRepository households,
    IHouseholdInvitationRepository invitations,
    TimeProvider clock)
{
    /// <summary>Creates and stages a pending invitation.</summary>
    /// <param name="household">The household being joined.</param>
    /// <param name="knownTarget">The person to address, when already known (the existing-person picker); otherwise <see langword="null"/> to resolve one from <paramref name="email"/>.</param>
    /// <param name="email">The invitee's address; falls back to <paramref name="knownTarget"/>'s email when <see langword="null"/>. At least one of the two must resolve to a non-null value.</param>
    /// <param name="role">The role granted on acceptance; cannot be owner.</param>
    /// <param name="invitedByPersonId">The owner who issued it.</param>
    /// <param name="nickname">Optional household-local nickname, applied when the invitation is accepted.</param>
    /// <param name="ct">Propagates cancellation to the storage calls.</param>
    /// <exception cref="HouseholdDomainException">
    /// The target already belongs to a member of this or another household, already has a pending
    /// invitation to this household, or neither a target nor an email resolved.
    /// </exception>
    public async Task<HouseholdInvitationId> IssueAsync(
        HouseholdAggregate household,
        Person? knownTarget,
        PersonEmail? email,
        HouseholdRole role,
        PersonId invitedByPersonId,
        string? nickname,
        CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var existingPerson = knownTarget ?? (email is not null ? await persons.GetByEmailAsync(email, ct) : null);

        if (existingPerson is not null)
        {
            if (household.HasMember(existingPerson.Id))
                throw new HouseholdDomainException($"{existingPerson.DisplayName} is already in this household.");

            if (await households.GetByMemberPersonIdAsync(existingPerson.Id, ct) is not null)
                throw new HouseholdDomainException($"{existingPerson.DisplayName} already belongs to a household.");

            if (await invitations.HasPendingForPersonInHouseholdAsync(household.Id, existingPerson.Id, now, ct))
                throw new HouseholdDomainException($"{existingPerson.DisplayName} already has a pending invitation to this household.");
        }

        email ??= existingPerson?.Email;

        if (email is not null && await invitations.HasPendingForEmailInHouseholdAsync(household.Id, email, now, ct))
            throw new HouseholdDomainException("There is already a pending invitation for that email.");

        var invitation = HouseholdInvitation.Create(
            HouseholdInvitationId.New(),
            household.Id,
            email,
            existingPerson?.Id,
            role,
            invitedByPersonId,
            now,
            nickname);

        await invitations.AddAsync(invitation, ct);
        return invitation.Id;
    }
}
