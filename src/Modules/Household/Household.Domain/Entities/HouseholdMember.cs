namespace Household.Domain.Entities;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A person's membership of one <see cref="Aggregates.Household"/>: their role and an optional
/// household-local nickname. Created and changed only through the household aggregate, which
/// enforces the "at least one owner" rule.
/// </summary>
public sealed class HouseholdMember : Entity<HouseholdMemberId>
{
    private const int MaxNicknameLength = 100;

    private HouseholdMember() { }

    internal static HouseholdMember Create(
        HouseholdMemberId id,
        PersonId personId,
        HouseholdRole role,
        string? nickname)
        => new()
        {
            Id = id,
            PersonId = personId,
            Role = role,
            Nickname = NormaliseNickname(nickname),
            JoinedAt = DateTime.UtcNow,
        };

    /// <summary>The person this membership belongs to.</summary>
    public PersonId PersonId { get; private set; } = default!;
    /// <summary>The member's authority in the household.</summary>
    public HouseholdRole Role { get; private set; }
    /// <summary>Name used inside this household instead of the person's display name, if set; at most 100 characters.</summary>
    public string? Nickname { get; private set; }
    /// <summary>When the person joined, UTC.</summary>
    public DateTime JoinedAt { get; private set; }

    internal void ChangeRole(HouseholdRole role) => Role = role;

    internal void Rename(string? nickname) => Nickname = NormaliseNickname(nickname);

    private static string? NormaliseNickname(string? nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return null;

        var trimmed = nickname.Trim();
        return trimmed.Length > MaxNicknameLength ? trimmed[..MaxNicknameLength] : trimmed;
    }
}
