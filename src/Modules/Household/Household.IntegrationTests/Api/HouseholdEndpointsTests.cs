namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

public sealed class HouseholdEndpointsTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdApiFactory _factory;

    public HouseholdEndpointsTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    private async Task<HttpClient> SignedInClientAsync(string? name = null)
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: $"{Guid.NewGuid():N}@x.com", name: name ?? "Owner");
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<Guid> PersonIdAsync(HttpClient client)
        => (await client.GetFromJsonAsync<MeBody>("/api/persons/me"))!.Id;

    [Fact]
    public async Task Owner_CanRunTheFullMemberLifecycle()
    {
        var owner = await SignedInClientAsync("Owner");

        // create
        var create = await owner.PostAsJsonAsync("/api/households", new { name = "The Test House" });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);

        var mine = await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me");
        mine!.Name.ShouldBe("The Test House");
        mine.MyRole.ShouldBe("Owner");
        mine.Members.Count.ShouldBe(1);
        var householdId = mine.Id;

        // add a managed member
        var addManaged = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/managed-members",
            new { displayName = "Kiddo", email = (string?)null, role = "Child", nickname = "K" });
        addManaged.StatusCode.ShouldBe(HttpStatusCode.Created);

        var members = await owner.GetFromJsonAsync<List<MemberBody>>($"/api/households/{householdId}/members");
        members!.Count.ShouldBe(2);
        var kiddo = members.Single(m => m.DisplayName == "Kiddo");
        kiddo.Role.ShouldBe("Child");
        kiddo.IsManaged.ShouldBeTrue();

        // promote to Adult
        var changeRole = await owner.PutAsJsonAsync(
            $"/api/households/{householdId}/members/{kiddo.PersonId}/role", new { role = "Adult" });
        changeRole.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // remove
        var remove = await owner.DeleteAsync($"/api/households/{householdId}/members/{kiddo.PersonId}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // rename
        var rename = await owner.PutAsJsonAsync($"/api/households/{householdId}", new { name = "Renamed House" });
        rename.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me"))!.Name.ShouldBe("Renamed House");

        // delete
        var delete = await owner.DeleteAsync($"/api/households/{householdId}");
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await owner.GetAsync("/api/households/me")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddExistingPerson_ThenThatPersonSeesTheHousehold()
    {
        var owner = await SignedInClientAsync("Owner");
        var invitee = await SignedInClientAsync("Invitee");
        var inviteeId = await PersonIdAsync(invitee);

        await owner.PostAsJsonAsync("/api/households", new { name = "Shared" });
        var householdId = (await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me"))!.Id;

        var add = await owner.PostAsJsonAsync(
            $"/api/households/{householdId}/members",
            new { personId = inviteeId, role = "Adult", nickname = (string?)null });
        add.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var inviteeView = await invitee.GetFromJsonAsync<MyHouseholdBody>("/api/households/me");
        inviteeView!.Id.ShouldBe(householdId);
        inviteeView.MyRole.ShouldBe("Adult");
    }

    [Fact]
    public async Task NonOwner_CannotRename_Returns403()
    {
        var owner = await SignedInClientAsync("Owner");
        var adult = await SignedInClientAsync("Adult");
        var adultId = await PersonIdAsync(adult);

        await owner.PostAsJsonAsync("/api/households", new { name = "H" });
        var householdId = (await owner.GetFromJsonAsync<MyHouseholdBody>("/api/households/me"))!.Id;
        await owner.PostAsJsonAsync($"/api/households/{householdId}/members",
            new { personId = adultId, role = "Adult", nickname = (string?)null });

        var rename = await adult.PutAsJsonAsync($"/api/households/{householdId}", new { name = "Hacked" });

        rename.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateHousehold_WhenAlreadyInOne_Returns422()
    {
        var owner = await SignedInClientAsync();
        await owner.PostAsJsonAsync("/api/households", new { name = "First" });

        var second = await owner.PostAsJsonAsync("/api/households", new { name = "Second" });

        second.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PickablePersons_ListsPeopleNotYetInAHousehold()
    {
        var owner = await SignedInClientAsync("Owner");
        var free = await SignedInClientAsync("Free Agent");
        var freeId = await PersonIdAsync(free);

        await owner.PostAsJsonAsync("/api/households", new { name = "H" });

        var pickable = await owner.GetFromJsonAsync<List<PickableBody>>("/api/households/pickable-persons");

        pickable!.ShouldContain(p => p.PersonId == freeId);
        pickable.ShouldNotContain(p => p.DisplayName == "Owner");
    }

    private sealed record MeBody(Guid Id);
    private sealed record MyHouseholdBody(Guid Id, string Name, string MyRole, List<MemberBody> Members);
    private sealed record MemberBody(Guid PersonId, string DisplayName, string Role, bool IsManaged);
    private sealed record PickableBody(Guid PersonId, string DisplayName, string? Email, bool IsManaged);
}
