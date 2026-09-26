namespace DietPlanner.Application.Households;

using Household.Contracts.Interfaces;
using Microsoft.Extensions.Logging;

/// <summary>
/// Resolves the caller's <see cref="HouseholdRoster"/> through the Household module. Falls back
/// to the caller alone when they have no household or the Household module is unavailable, so
/// DietPlanner never fails because of a household lookup.
/// </summary>
internal sealed partial class HouseholdRosterProvider(
    IHouseholdQueryService households,
    ILogger<HouseholdRosterProvider> logger)
{
    public async Task<HouseholdRoster> GetAsync(Guid callerPersonId, string authSubject, CancellationToken ct)
    {
        try
        {
            var context = await households.GetHouseholdContextForUserAsync(authSubject, ct);
            return context is null
                ? new HouseholdRoster(callerPersonId, null, [])
                : new HouseholdRoster(callerPersonId, context.Role, context.Members);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ponytail: falls back to "caller only, no role", so a Guest can write their own data while
            // the Household module is down; fail closed (throw) if that ever matters.
            LogHouseholdUnresolved(ex);
            return new HouseholdRoster(callerPersonId, null, []);
        }
    }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Warning,
        Message = "Could not resolve the household; using the caller's own data only.")]
    private partial void LogHouseholdUnresolved(Exception exception);
}
