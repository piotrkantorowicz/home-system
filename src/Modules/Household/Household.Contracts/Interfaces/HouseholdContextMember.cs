namespace Household.Contracts.Interfaces;

/// <summary>One member in a <see cref="HouseholdContext"/>'s roster.</summary>
/// <param name="PersonId">The member's person identifier — the key personal data is stored under.</param>
/// <param name="DisplayName">Name to show for the member.</param>
/// <param name="Role">The member's role name (<c>Owner</c>, <c>Adult</c>, <c>Child</c>, <c>Guest</c>).</param>
/// <param name="IsManaged">True for a member maintained by an adult who has no login of their own.</param>
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
