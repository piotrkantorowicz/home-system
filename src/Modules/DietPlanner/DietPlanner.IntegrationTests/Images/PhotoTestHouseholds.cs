namespace DietPlanner.IntegrationTests.Images;

using Household.Contracts.Interfaces;

internal sealed class PhotoTestHouseholds : IHouseholdQueryService
{
    internal Dictionary<string, HouseholdContext> Contexts { get; } = [];

    public Task<HouseholdContext?> GetHouseholdContextForUserAsync(string authSubject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Contexts.GetValueOrDefault(authSubject));
    }

    public Task<string?> GetAuthSubjectForPersonAsync(Guid personId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(null);
    }
}
