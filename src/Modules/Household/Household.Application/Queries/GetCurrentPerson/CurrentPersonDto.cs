namespace Household.Application.Queries.GetCurrentPerson;

public sealed record CurrentPersonDto(
    Guid Id,
    string DisplayName,
    string? Email,
    string? AvatarUrl,
    bool IsManaged);
