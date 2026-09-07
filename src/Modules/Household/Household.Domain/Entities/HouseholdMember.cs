namespace Household.Domain.Entities;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

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

    public PersonId PersonId { get; private set; } = default!;
    public HouseholdRole Role { get; private set; }
    public string? Nickname { get; private set; }
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
