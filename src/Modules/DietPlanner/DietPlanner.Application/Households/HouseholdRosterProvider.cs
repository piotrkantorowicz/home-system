namespace DietPlanner.Application.Households;

using Household.Contracts.Interfaces;

/// <summary>
/// Resolves the caller's <see cref="HouseholdRoster"/> through the Household module. Only a
/// confirmed "no household" yields the caller-alone roster; a failed lookup propagates, so an
/// unknown role never widens what the caller may write.
/// </summary>
internal sealed class HouseholdRosterProvider(IHouseholdQueryService households)
{
    public async Task<HouseholdRoster> GetAsync(Guid callerPersonId, string authSubject, CancellationToken ct)
    {
        HouseholdContext? context = await households.GetHouseholdContextForUserAsync(authSubject, ct);
        return context is null
            ? new HouseholdRoster(callerPersonId, null, [])
            : new HouseholdRoster(callerPersonId, context.Role, context.Members);
    }

    public async Task<LibraryAccess> GetLibraryAccessAsync(string authSubject, CancellationToken ct)
    {
        HouseholdContext? context = await households.GetHouseholdContextForUserAsync(authSubject, ct);
        return context is null
            ? new LibraryAccess(authSubject, null, [])
            : new LibraryAccess(authSubject, context.Role,
                [.. context.Members.Where(m => m.AuthSubject is not null).Select(m => m.AuthSubject!)]);
    }
}
