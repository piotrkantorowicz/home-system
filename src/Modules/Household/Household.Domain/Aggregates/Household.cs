namespace Household.Domain.Aggregates;

using global::Household.Domain.Entities;
using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The sharing boundary: the people who live together and the resources they share.
/// A household always has at least one <see cref="HouseholdRole.Owner"/>.
/// </summary>
/// <remarks>
/// "A person belongs to at most one household" is a cross-aggregate rule and is enforced in
/// the application layer (checked against <c>IHouseholdRepository</c> before adding a member),
/// not here — deliberately, so multi-household can be enabled later without a schema change.
/// </remarks>
public sealed class Household : AggregateRoot<HouseholdId>
{
    private const int MaxNameLength = 120;

    private readonly List<HouseholdMember> _members = [];

    private Household() { }

    /// <summary>Creates a household with its first owner and raises the created/joined events.</summary>
    /// <param name="id">Identifier for the new household.</param>
    /// <param name="name">Display name; trimmed and capped at 120 characters.</param>
    /// <param name="ownerPersonId">The person who becomes the first <see cref="HouseholdRole.Owner"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ownerPersonId"/> is null.</exception>
    /// <exception cref="HouseholdDomainException"><paramref name="name"/> is blank.</exception>
    public static Household Create(HouseholdId id, string name, PersonId ownerPersonId)
    {
        ArgumentNullException.ThrowIfNull(ownerPersonId);

        var household = new Household
        {
            Id = id,
            Name = NormaliseName(name),
            CreatedAt = DateTime.UtcNow,
        };

        var owner = HouseholdMember.Create(
            HouseholdMemberId.New(), ownerPersonId, HouseholdRole.Owner, nickname: null);
        household._members.Add(owner);

        household.RaiseDomainEvent(new HouseholdCreatedDomainEvent(id, ownerPersonId));
        household.RaiseDomainEvent(
            new MemberJoinedHouseholdDomainEvent(id, ownerPersonId, HouseholdRole.Owner));

        return household;
    }

    /// <summary>Display name; never blank, at most 120 characters.</summary>
    public string Name { get; private set; } = default!;
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last change to the name or membership, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }
    /// <summary>Current members, including at least one owner.</summary>
    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    /// <summary>Changes the display name; a no-op when the normalised name is unchanged.</summary>
    /// <param name="name">New display name; trimmed and capped at 120 characters.</param>
    /// <exception cref="HouseholdDomainException"><paramref name="name"/> is blank.</exception>
    public void Rename(string name)
    {
        var normalised = NormaliseName(name);
        if (normalised == Name)
            return;

        Name = normalised;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Adds a person with a role and raises <see cref="MemberJoinedHouseholdDomainEvent"/>.</summary>
    /// <param name="personId">The person to add.</param>
    /// <param name="role">Their authority in this household.</param>
    /// <param name="nickname">Optional name used inside this household instead of the person's display name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="personId"/> is null.</exception>
    /// <exception cref="HouseholdDomainException">The person is already a member.</exception>
    public void AddMember(PersonId personId, HouseholdRole role, string? nickname = null)
    {
        ArgumentNullException.ThrowIfNull(personId);

        if (FindMember(personId) is not null)
            throw new HouseholdDomainException("That person is already a member of this household.");

        _members.Add(HouseholdMember.Create(HouseholdMemberId.New(), personId, role, nickname));
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberJoinedHouseholdDomainEvent(Id, personId, role));
    }

    /// <summary>Removes a member and raises <see cref="MemberLeftHouseholdDomainEvent"/>.</summary>
    /// <param name="personId">The member to remove.</param>
    /// <exception cref="HouseholdDomainException">The person is not a member, or is the last owner.</exception>
    public void RemoveMember(PersonId personId)
    {
        var member = RequireMember(personId);

        if (member.Role == HouseholdRole.Owner && OwnerCount == 1)
            throw new HouseholdDomainException("A household must keep at least one owner.");

        _members.Remove(member);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberLeftHouseholdDomainEvent(Id, personId));
    }

    /// <summary>Changes a member's role and raises <see cref="MemberRoleChangedDomainEvent"/>; a no-op when unchanged.</summary>
    /// <param name="personId">The member whose role changes.</param>
    /// <param name="newRole">The new role.</param>
    /// <exception cref="HouseholdDomainException">The person is not a member, or demoting them would leave no owner.</exception>
    public void ChangeMemberRole(PersonId personId, HouseholdRole newRole)
    {
        var member = RequireMember(personId);
        if (member.Role == newRole)
            return;

        if (member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner && OwnerCount == 1)
            throw new HouseholdDomainException("A household must keep at least one owner.");

        var previous = member.Role;
        member.ChangeRole(newRole);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberRoleChangedDomainEvent(Id, personId, previous, newRole));
    }

    /// <summary>Sets or clears a member's household-local nickname.</summary>
    /// <param name="personId">The member to rename.</param>
    /// <param name="nickname">The nickname, or blank to clear it; trimmed and capped at 100 characters.</param>
    /// <exception cref="HouseholdDomainException">The person is not a member.</exception>
    public void RenameMember(PersonId personId, string? nickname)
    {
        RequireMember(personId).Rename(nickname);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Whether the person is currently a member.</summary>
    /// <param name="personId">The person to look up.</param>
    public bool HasMember(PersonId personId) => FindMember(personId) is not null;

    /// <summary>The person's role, or <see langword="null"/> when they are not a member.</summary>
    /// <param name="personId">The person to look up.</param>
    public HouseholdRole? RoleOf(PersonId personId) => FindMember(personId)?.Role;

    private int OwnerCount => _members.Count(m => m.Role == HouseholdRole.Owner);

    private HouseholdMember? FindMember(PersonId personId)
        => _members.SingleOrDefault(m => m.PersonId == personId);

    private HouseholdMember RequireMember(PersonId personId)
        => FindMember(personId)
           ?? throw new HouseholdDomainException("That person is not a member of this household.");

    private static string NormaliseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new HouseholdDomainException("A household must have a name.");

        var trimmed = name.Trim();
        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }
}
