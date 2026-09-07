namespace Household.Domain.ValueObjects;

public sealed record HouseholdInvitationId(Guid Value)
{
    public static HouseholdInvitationId New() => new(Guid.NewGuid());
    public static HouseholdInvitationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
