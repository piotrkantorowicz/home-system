namespace Household.Application.Queries.GetCurrentPerson;

/// <summary>
/// The caller's own person record.
/// </summary>
/// <param name="Id">Person identifier.</param>
/// <param name="DisplayName">Name shown across the app.</param>
/// <param name="Email">Email, if known.</param>
/// <param name="AvatarUrl">Avatar URL, if any.</param>
/// <param name="IsManaged">True while the person has no login of their own.</param>
public sealed record CurrentPersonDto(
    Guid Id,
    string DisplayName,
    string? Email,
    string? AvatarUrl,
    bool IsManaged);
