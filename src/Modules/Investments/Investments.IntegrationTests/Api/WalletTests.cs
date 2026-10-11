namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Investments.IntegrationTests.Infrastructure;

/// <summary>#574 — wallet lifecycle and the design §4 permission matrix, over list and direct-ID routes.</summary>
public sealed class WalletTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public WalletTests(InvestmentsDatabaseFixture fixture)
    {
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid id, object body)
        => client.PutAsJsonAsync($"/api/investments/wallets/{id}", body, Ct);

    /// <summary>An owner creates a shared wallet for themselves; it is listed with the owner's name.</summary>
    [Fact]
    public async Task Create_SharedWalletForSelf_IsListed()
    {
        var h = await _scenario.NewHouseholdAsync();

        var created = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB main", kind: "IKE", currency: "EUR");
        var page = await InvestmentsScenario.ListAsync(h.Owner);

        created.OwnerPersonId.ShouldBe(h.OwnerPersonId);
        created.OwnerName.ShouldBe("Owner");
        created.Kind.ShouldBe("IKE");
        created.Currency.ShouldBe("EUR");
        created.CanChangeVisibility.ShouldBeTrue();
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(created.Id);
    }

    /// <summary>An adult may create a wallet for a current managed member.</summary>
    [Fact]
    public async Task Create_AdultForManagedMember_Succeeds()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var kid = await InvestmentsScenario.ManagedChildAsync(h);

        var created = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Kid PPK", "Private", kid, kind: "PPK");

        created.OwnerPersonId.ShouldBe(kid);
        created.OwnerName.ShouldBe("Kid");
    }

    /// <summary>Nobody may create a wallet in another independent member's name.</summary>
    [Fact]
    public async Task Create_ForAnotherAdult_Returns403()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");

        var response = await InvestmentsScenario.PostWalletAsync(h.Owner, "Theirs", ownerPersonId: adult.PersonId);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A child with a login may create a wallet for themselves, not for a managed member.</summary>
    [Fact]
    public async Task Create_Child_OnlyForSelf()
    {
        var h = await _scenario.NewHouseholdAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var kid = await InvestmentsScenario.ManagedChildAsync(h);

        var own = await InvestmentsScenario.PostWalletAsync(child.Client, "Piggy bank", "Private", kind: "Cash");
        var forKid = await InvestmentsScenario.PostWalletAsync(child.Client, "Not mine", "Private", kid);

        own.StatusCode.ShouldBe(HttpStatusCode.Created);
        forKid.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A guest has no access to investments at all.</summary>
    [Fact]
    public async Task Guest_HasNoAccess()
    {
        var h = await _scenario.NewHouseholdAsync();
        var guest = await _scenario.MemberAsync(h, "Guest");

        var list = await guest.Client.GetAsync("/api/investments/wallets", Ct);
        var create = await InvestmentsScenario.PostWalletAsync(guest.Client, "Nope");

        list.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        create.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A caller without a household gets not-found rather than an empty list.</summary>
    [Fact]
    public async Task NoHousehold_Returns404()
    {
        var loner = await _scenario.SignedInAsync("Loner");

        var response = await loner.GetAsync("/api/investments/wallets", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Another adult's private wallet is absent from the list and count, and 404 on every direct route.</summary>
    [Fact]
    public async Task PrivateWalletOfAnotherAdult_IsInvisible()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var hidden = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Secret IKZE", "Private", kind: "IKZE");
        await InvestmentsScenario.CreateWalletAsync(adult.Client, "Joint savings", kind: "Savings");

        var page = await InvestmentsScenario.ListAsync(h.Owner, "?includeArchived=true");
        var detail = await h.Owner.GetAsync($"/api/investments/wallets/{hidden.Id}", Ct);
        var update = await PutAsync(h.Owner, hidden.Id, new { name = "Mine now", visibility = "Private", version = hidden.Version });
        var archive = await h.Owner.PostAsync($"/api/investments/wallets/{hidden.Id}/archive", null, Ct);

        page.Items.ShouldHaveSingleItem().Name.ShouldBe("Joint savings");
        page.TotalCount.ShouldBe(1);
        detail.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        update.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        archive.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>A child sees only their own wallets, never shared ones.</summary>
    [Fact]
    public async Task Child_DoesNotSeeSharedWallets()
    {
        var h = await _scenario.NewHouseholdAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var shared = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Family brokerage");
        var own = await InvestmentsScenario.CreateWalletAsync(child.Client, "Piggy bank", "Private", kind: "Cash");

        var page = await InvestmentsScenario.ListAsync(child.Client);
        var detail = await child.Client.GetAsync($"/api/investments/wallets/{shared.Id}", Ct);

        page.Items.ShouldHaveSingleItem().Id.ShouldBe(own.Id);
        detail.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>A managed member's private wallet is visible to and editable by every adult.</summary>
    [Fact]
    public async Task ManagedMemberPrivateWallet_VisibleToAdults()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var kid = await InvestmentsScenario.ManagedChildAsync(h);
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Kid savings", "Private", kid, kind: "Savings");

        var update = await PutAsync(adult.Client, wallet.Id, new { name = "Kid savings", visibility = "Shared", version = wallet.Version });

        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await update.Content.ReadFromJsonAsync<InvestmentsScenario.WalletBody>(Ct))!.Visibility.ShouldBe("Shared");
    }

    /// <summary>Any adult may rename a shared wallet, but only its owner may change its visibility.</summary>
    [Fact]
    public async Task Update_SharedWalletOfAnotherAdult_RenameOkVisibility403()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Joint");

        var listed = (await InvestmentsScenario.ListAsync(adult.Client)).Items.ShouldHaveSingleItem();
        var rename = await PutAsync(adult.Client, wallet.Id, new { name = "Joint account", visibility = "Shared", version = wallet.Version });
        var renamed = (await rename.Content.ReadFromJsonAsync<InvestmentsScenario.WalletBody>(Ct))!;
        var hide = await PutAsync(adult.Client, wallet.Id, new { name = "Joint account", visibility = "Private", version = renamed.Version });

        listed.CanChangeVisibility.ShouldBeFalse();
        rename.StatusCode.ShouldBe(HttpStatusCode.OK);
        renamed.Name.ShouldBe("Joint account");
        hide.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>The owner may make a wallet private, after which other adults lose it.</summary>
    [Fact]
    public async Task Update_OwnerMakesPrivate_HidesFromOtherAdults()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Joint");

        var update = await PutAsync(h.Owner, wallet.Id, new { name = "Joint", visibility = "Private", version = wallet.Version });
        var detail = await adult.Client.GetAsync($"/api/investments/wallets/{wallet.Id}", Ct);

        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        detail.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Owner, kind and currency are not part of the edit contract.</summary>
    [Theory]
    [InlineData("kind", "Cash")]
    [InlineData("currency", "USD")]
    [InlineData("ownerPersonId", "00000000-0000-0000-0000-000000000001")]
    public async Task Update_ImmutableField_Returns400(string field, string value)
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Mine");
        var body = new Dictionary<string, object> { ["name"] = "Mine", ["visibility"] = "Shared", ["version"] = wallet.Version, [field] = value };

        var response = await PutAsync(h.Owner, wallet.Id, body);
        var after = await h.Owner.GetFromJsonAsync<InvestmentsScenario.WalletBody>($"/api/investments/wallets/{wallet.Id}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        after!.Kind.ShouldBe("Brokerage");
        after.Currency.ShouldBe("PLN");
        after.OwnerPersonId.ShouldBe(h.OwnerPersonId);
    }

    /// <summary>An edit sent with a stale version is a conflict and changes nothing.</summary>
    [Fact]
    public async Task Update_StaleVersion_Returns409()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Original");
        (await PutAsync(h.Owner, wallet.Id, new { name = "First", visibility = "Shared", version = wallet.Version })).EnsureSuccessStatusCode();

        var stale = await PutAsync(h.Owner, wallet.Id, new { name = "Second", visibility = "Shared", version = wallet.Version });
        var after = await h.Owner.GetFromJsonAsync<InvestmentsScenario.WalletBody>($"/api/investments/wallets/{wallet.Id}", Ct);

        stale.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        after!.Name.ShouldBe("First");
    }

    /// <summary>Archived wallets are hidden by default, listed with includeArchived, and restorable.</summary>
    [Fact]
    public async Task Archive_HidesUntilRestored()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Old deposit", kind: "Savings");

        (await h.Owner.PostAsync($"/api/investments/wallets/{wallet.Id}/archive", null, Ct)).EnsureSuccessStatusCode();
        var active = await InvestmentsScenario.ListAsync(h.Owner);
        var all = await InvestmentsScenario.ListAsync(h.Owner, "?includeArchived=true");
        var restored = await h.Owner.PostAsync($"/api/investments/wallets/{wallet.Id}/restore", null, Ct);
        var afterRestore = await InvestmentsScenario.ListAsync(h.Owner);

        active.Items.ShouldBeEmpty();
        all.Items.ShouldHaveSingleItem().IsArchived.ShouldBeTrue();
        restored.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterRestore.Items.ShouldHaveSingleItem().IsArchived.ShouldBeFalse();
    }

    /// <summary>Unknown enum values and numeric enum values are rejected.</summary>
    [Theory]
    [InlineData("Crypto", "PLN")]
    [InlineData("1", "PLN")]
    [InlineData("Brokerage", "GBP")]
    public async Task Create_InvalidEnum_Returns400(string kind, string currency)
    {
        var h = await _scenario.NewHouseholdAsync();

        var response = await InvestmentsScenario.PostWalletAsync(h.Owner, "Bad", kind: kind, currency: currency);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
