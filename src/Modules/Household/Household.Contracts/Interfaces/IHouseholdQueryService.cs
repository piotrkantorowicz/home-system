namespace Household.Contracts.Interfaces;

/// <summary>
/// The synchronous query surface other modules use to resolve the household a caller
/// belongs to. Implemented in <c>Household.Infrastructure</c> with <c>AsNoTracking</c>
/// projections — no aggregate hydration.
/// </summary>
public interface IHouseholdQueryService
{
    /// <summary>
    /// Resolves the household context for the account behind <paramref name="authSubject"/>
    /// (the Authentik <c>sub</c>). Returns <see langword="null"/> when the caller has no
    /// <c>Person</c> record yet, or has one but no household.
    /// </summary>
    Task<HouseholdContext?> GetHouseholdContextForUserAsync(
        string authSubject,
        CancellationToken ct = default);
    /// <summary>Returns the linked authentication subject, or null for an unknown or managed person.</summary>
    Task<string?> GetAuthSubjectForPersonAsync(Guid personId, CancellationToken ct = default);
}

