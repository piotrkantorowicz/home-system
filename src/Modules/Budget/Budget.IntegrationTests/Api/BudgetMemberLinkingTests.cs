namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#456 — linking a managed member to a real account ends adults' delegated access to their private envelope.</summary>
public sealed class BudgetMemberLinkingTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetMemberLinkingTests(BudgetDatabaseFixture fixture) => _factory = new BudgetApiFactory(fixture);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The adult manages a managed child's envelope until the child links an account; after that it is the child's alone.</summary>
    [Fact]
    public async Task LinkingManagedMember_EndsAdultAccessToTheirPrivateEnvelope_OnTheNextRequest()
    {
        var h = await new BudgetScenario(_factory).NewHouseholdAsync();
        var kid = await BudgetScenario.ManagedChildAsync(h);
        var created = await h.Owner.PostAsJsonAsync("/api/budget/accounts",
            new { name = "Kid pocket money", visibility = "Personal", ownerPersonId = kid }, Ct);
        created.EnsureSuccessStatusCode();
        var envelopeId = (await created.Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        (await h.Owner.GetAsync($"/api/budget/accounts/{envelopeId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var email = $"{Guid.NewGuid():N}@x.com";
        (await h.Owner.PostAsJsonAsync($"/api/households/{h.Id}/members/{kid}/convert-to-account", new { email }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var kidClient = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", "Kid", email);
        var sync = await kidClient.PostAsync("/api/persons/me/sync", null, Ct);
        sync.EnsureSuccessStatusCode();
        (await sync.Content.ReadFromJsonAsync<PersonBody>(Ct))!.PersonId.ShouldBe(kid);

        (await h.Owner.GetAsync($"/api/budget/accounts/{envelopeId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var adultList = await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/accounts?includeArchived=true", Ct);
        adultList!.Items.ShouldNotContain(a => a.Id == envelopeId);
        (await kidClient.GetAsync($"/api/budget/accounts/{envelopeId}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed record IdBody(Guid Id);
    private sealed record PersonBody(Guid PersonId);
    private sealed record PageBody(List<IdBody> Items);
}
