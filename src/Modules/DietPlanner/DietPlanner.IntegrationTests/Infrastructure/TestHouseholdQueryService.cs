namespace DietPlanner.IntegrationTests.Infrastructure;

using Household.Contracts.Interfaces;

internal sealed class TestHouseholdQueryService : IHouseholdQueryService
{
    public Task<HouseholdContext?> GetHouseholdContextForUserAsync(string authSubject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<HouseholdContext?>(null);
    }

    public Task<string?> GetAuthSubjectForPersonAsync(Guid personId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(TestAuthHandler.SubjectFor(personId));
    }
}
