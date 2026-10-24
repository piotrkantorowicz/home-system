namespace DietPlanner.IntegrationTests.Mcp;

using DietPlanner.IntegrationTests.Infrastructure;
using Household.Contracts.Interfaces;

/// <summary>
/// Household lookup for MCP tests. By default every subject is a solo household owner whose person id
/// matches <see cref="TestAuthHandler.PersonIdFor"/>, so REST (test scheme) and MCP (JWT) calls for one
/// subject resolve to the same person, the way Authentik's shared <c>sub</c> does in production.
/// Subjects starting with <see cref="NoPersonPrefix"/> have not synced a person yet.
/// </summary>
internal sealed class McpTestHouseholds : IHouseholdQueryService
{
    public const string NoPersonPrefix = "no-person-";

    /// <summary>Explicit contexts, e.g. to put several subjects in one household.</summary>
    public Dictionary<string, HouseholdContext> Contexts { get; } = [];

    /// <summary>Puts <paramref name="subjects"/> in one household; the first is its owner.</summary>
    /// <param name="subjects">Auth subjects to join.</param>
    public void Join(params string[] subjects)
    {
        var householdId = Guid.CreateVersion7();
        var members = subjects
            .Select((s, i) => new HouseholdContextMember(TestAuthHandler.PersonIdFor(s), s, i == 0 ? "Owner" : "Adult", false, s))
            .ToList();
        foreach (var member in members)
            Contexts[member.AuthSubject!] = new HouseholdContext(householdId, member.PersonId, member.Role, members);
    }

    public Task<HouseholdContext?> GetHouseholdContextForUserAsync(string authSubject, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (Contexts.TryGetValue(authSubject, out var context))
            return Task.FromResult<HouseholdContext?>(context);

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
