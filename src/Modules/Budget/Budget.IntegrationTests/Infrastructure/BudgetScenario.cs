namespace Budget.IntegrationTests.Infrastructure;

using System.Net.Http.Json;

/// <summary>Builds signed-in households, members and envelopes over the real HTTP surface for integration tests.</summary>
internal sealed class BudgetScenario(BudgetApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal sealed record Household(HttpClient Owner, Guid Id, Guid OwnerPersonId);

    internal sealed record Member(HttpClient Client, Guid PersonId);

    public async Task<HttpClient> SignedInAsync(string name)
    {
        var client = factory.CreateClientFor($"auth|{Guid.NewGuid():N}", name);
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    public async Task<Household> NewHouseholdAsync(bool initialiseBudget = true)
    {
        var owner = await SignedInAsync("Owner");
        (await owner.PostAsJsonAsync("/api/households", new { name = "The House" }, Ct)).EnsureSuccessStatusCode();
        var id = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", Ct))!.Id;
        var ownerPersonId = (await owner.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
        if (initialiseBudget)
            (await owner.PostAsJsonAsync("/api/budget", new { }, Ct)).EnsureSuccessStatusCode();
        return new Household(owner, id, ownerPersonId);
    }

    public async Task<Member> MemberAsync(Household household, string role)
    {
        var member = await SignedInAsync(role);
        var personId = (await member.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
        var add = await household.Owner.PostAsJsonAsync(
            $"/api/households/{household.Id}/members", new { personId, role, nickname = (string?)null }, Ct);
        add.EnsureSuccessStatusCode();
        var invitationId = (await add.Content.ReadFromJsonAsync<InvitationBody>(Ct))!.InvitationId;
        (await member.PostAsync($"/api/households/invitations/{invitationId}/accept", null, Ct)).EnsureSuccessStatusCode();
        return new Member(member, personId);
    }

    public static async Task<Guid> ManagedChildAsync(Household household)
    {
        var created = await household.Owner.PostAsJsonAsync($"/api/households/{household.Id}/managed-members",
            new { displayName = "Kid", email = (string?)null, role = "Child", nickname = (string?)null }, Ct);
        created.EnsureSuccessStatusCode();
        return Guid.Parse(created.Headers.Location!.ToString().Split('/').Last());
    }

    /// <summary>The shared default envelope created at initialisation.</summary>
    public static async Task<Guid> DefaultEnvelopeAsync(HttpClient adult)
        => (await adult.GetFromJsonAsync<PageBody>("/api/budget/accounts", Ct))!.Items.Single(a => a.Visibility == "Household").Id;

    private sealed record IdBody(Guid Id);
    private sealed record InvitationBody(Guid InvitationId);
    private sealed record AccountBody(Guid Id, string Visibility);
    private sealed record PageBody(List<AccountBody> Items);
}
