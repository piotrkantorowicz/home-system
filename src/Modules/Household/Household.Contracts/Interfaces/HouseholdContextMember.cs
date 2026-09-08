namespace Household.Contracts.Interfaces;

/// <summary>One member in a <see cref="HouseholdContext"/>'s roster.</summary>
/// <param name="AuthSubject">
/// The member's Authentik subject, or <see langword="null"/> for a managed member with no
/// login. Lets other modules resolve household-owned data that is still keyed by subject.
/// </param>
public sealed record HouseholdContextMember(
    Guid PersonId,
    string DisplayName,
    string Role,
    bool IsManaged,
    string? AuthSubject);
