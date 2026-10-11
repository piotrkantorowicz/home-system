namespace Investments.IntegrationTests.Infrastructure;

using System.Net;
using System.Net.Http.Json;

/// <summary>Builds signed-in households, members and wallets over the real HTTP surface for integration tests.</summary>
internal sealed class InvestmentsScenario(InvestmentsApiFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal sealed record Household(HttpClient Owner, Guid Id, Guid OwnerPersonId);

    internal sealed record Member(HttpClient Client, Guid PersonId);

    internal sealed record WalletBody(
        Guid Id, string Name, string Institution, string Kind, string Currency, string Visibility,
        Guid OwnerPersonId, string OwnerName, bool IsArchived, uint Version, bool CanChangeVisibility);

    internal sealed record WalletPage(List<WalletBody> Items, int TotalCount);

    public async Task<HttpClient> SignedInAsync(string name)
    {
        var client = factory.CreateClientFor($"auth|{Guid.NewGuid():N}", name);
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    public async Task<Household> NewHouseholdAsync()
    {
        var owner = await SignedInAsync("Owner");
        (await owner.PostAsJsonAsync("/api/households", new { name = "The House" }, Ct)).EnsureSuccessStatusCode();
        var id = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", Ct))!.Id;
        var ownerPersonId = (await owner.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
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

    public static Task<HttpResponseMessage> PostWalletAsync(
        HttpClient client, string name, string visibility = "Shared", Guid? ownerPersonId = null,
        string kind = "Brokerage", string currency = "PLN")
        => client.PostAsJsonAsync("/api/investments/wallets",
            new { name, institution = "XTB", kind, currency, visibility, ownerPersonId }, Ct);

    public static async Task<WalletBody> CreateWalletAsync(
        HttpClient client, string name, string visibility = "Shared", Guid? ownerPersonId = null,
        string kind = "Brokerage", string currency = "PLN")
    {
        var response = await PostWalletAsync(client, name, visibility, ownerPersonId, kind, currency);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<WalletBody>(Ct))!;
    }

    public static async Task<WalletPage> ListAsync(HttpClient client, string query = "")
        => (await client.GetFromJsonAsync<WalletPage>($"/api/investments/wallets{query}", Ct))!;

    private sealed record IdBody(Guid Id);
    private sealed record InvitationBody(Guid InvitationId);
}
