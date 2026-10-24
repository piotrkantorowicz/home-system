namespace DietPlanner.IntegrationTests.Mcp;

using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;

/// <summary>
/// Household lookup for MCP tests: every subject is a solo household owner whose person id matches
/// <see cref="TestAuthHandler.PersonIdFor"/>, so REST (test scheme) and MCP (JWT) calls for one subject
/// resolve to the same person, the way Authentik's shared <c>sub</c> does in production. Subjects
/// starting with <see cref="NoPersonPrefix"/> have not synced a person yet.
/// </summary>
internal sealed class McpTestHouseholds : IHouseholdQueryService
{
    public const string NoPersonPrefix = "no-person-";

    public Task<HouseholdContext?> GetHouseholdContextForUserAsync(string authSubject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(authSubject.StartsWith(NoPersonPrefix, StringComparison.Ordinal)
            ? null
            : new HouseholdContext(Guid.CreateVersion7(), TestAuthHandler.PersonIdFor(authSubject), "Owner", []));
    }

    public Task<string?> GetAuthSubjectForPersonAsync(Guid personId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(TestAuthHandler.SubjectFor(personId));
    }
}
