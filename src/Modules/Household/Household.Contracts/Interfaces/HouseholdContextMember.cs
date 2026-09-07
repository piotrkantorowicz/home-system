namespace Household.Contracts.Interfaces;

/// <summary>One member in a <see cref="HouseholdContext"/>'s roster.</summary>
public sealed record HouseholdContextMember(
    Guid PersonId,
    string DisplayName,
    string Role,
    bool IsManaged);
