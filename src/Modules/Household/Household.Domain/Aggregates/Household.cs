using Household.Domain.Entities;
using Household.Domain.Events;
using Household.Domain.Exceptions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

namespace Household.Domain.Aggregates;

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

    public string Name { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    public void Rename(string name)
    {
        var normalised = NormaliseName(name);
        if (normalised == Name)
            return;

        Name = normalised;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMember(PersonId personId, HouseholdRole role, string? nickname = null)
    {
        ArgumentNullException.ThrowIfNull(personId);

        if (FindMember(personId) is not null)
            throw new HouseholdDomainException("That person is already a member of this household.");

        _members.Add(HouseholdMember.Create(HouseholdMemberId.New(), personId, role, nickname));
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberJoinedHouseholdDomainEvent(Id, personId, role));
    }

    public void RemoveMember(PersonId personId)
    {
        var member = RequireMember(personId);

        if (member.Role == HouseholdRole.Owner && OwnerCount == 1)
            throw new HouseholdDomainException("A household must keep at least one owner.");

        _members.Remove(member);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberLeftHouseholdDomainEvent(Id, personId));
    }

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

    public void RenameMember(PersonId personId, string? nickname)
    {
        RequireMember(personId).Rename(nickname);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasMember(PersonId personId) => FindMember(personId) is not null;

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
