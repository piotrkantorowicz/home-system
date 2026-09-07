namespace Household.Contracts.Interfaces;

/// <summary>
/// A caller's household context: the household they belong to, their own <c>PersonId</c>,
/// their role, and the full member roster. Primitive-only so it can cross the module
/// boundary.
/// </summary>
public sealed record HouseholdContext(
    Guid HouseholdId,
    Guid PersonId,
    string Role,
    IReadOnlyList<HouseholdContextMember> Members);
