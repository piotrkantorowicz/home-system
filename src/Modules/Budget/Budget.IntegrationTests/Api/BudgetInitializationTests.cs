namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Domain.ValueObjects;
using global::Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#446 — Budget initialisation end-to-end: HTTP → Household Contracts → PostgreSQL.</summary>
public sealed class BudgetInitializationTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetInitializationTests(BudgetDatabaseFixture fixture) => _factory = new BudgetApiFactory(fixture);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> SignedInAsync(string name)
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}", name);
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        return client;
    }

    private async Task<(HttpClient Owner, Guid HouseholdId)> OwnerWithHouseholdAsync()
    {
        var owner = await SignedInAsync("Owner");
        (await owner.PostAsJsonAsync("/api/households", new { name = "The House" })).EnsureSuccessStatusCode();
        var household = (await owner.GetFromJsonAsync<IdBody>("/api/households/me"))!;
        return (owner, household.Id);
    }

    private async Task<HttpClient> MemberAsync(HttpClient owner, Guid householdId, string role)
    {
        var member = await SignedInAsync(role);
        var personId = (await member.GetFromJsonAsync<IdBody>("/api/persons/me"))!.Id;
        var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members",
            new { personId, role, nickname = (string?)null });
        add.EnsureSuccessStatusCode();
        var invitationId = (await add.Content.ReadFromJsonAsync<InvitationBody>())!.InvitationId;
        (await member.PostAsync($"/api/households/invitations/{invitationId}/accept", null)).EnsureSuccessStatusCode();
        return member;
    }

    /// <summary>First POST creates (201) a PLN budget; GET then returns it.</summary>
    [Fact]
    public async Task Initialize_CreatesBudgetInPln_AndGetReturnsIt()
    {
        var (owner, _) = await OwnerWithHouseholdAsync();

        var post = await owner.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken);
        var get = await owner.GetAsync("/api/budget", TestContext.Current.CancellationToken);

        post.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await post.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!;
        created.Currency.ShouldBe("PLN");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await get.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Id.ShouldBe(created.Id);
    }

    /// <summary>GET before initialisation is a 404 with no side effect.</summary>
    [Fact]
    public async Task Get_BeforeInitialisation_Returns404_AndDoesNotCreate()
    {
        var (owner, _) = await OwnerWithHouseholdAsync();

        (await owner.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await owner.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>A repeat POST with the same (or default) currency returns the existing budget with 200.</summary>
    [Fact]
    public async Task Initialize_Twice_ReturnsTheSameBudget()
    {
        var (owner, _) = await OwnerWithHouseholdAsync();
        var first = await owner.PostAsJsonAsync("/api/budget", new { currency = "EUR" }, TestContext.Current.CancellationToken);

        var again = await owner.PostAsJsonAsync("/api/budget", new { currency = "eur" }, TestContext.Current.CancellationToken);

        again.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await again.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Id
            .ShouldBe((await first.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Id);
    }

    /// <summary>A different currency for an existing budget is a 409.</summary>
    [Fact]
    public async Task Initialize_WithDifferentCurrency_Returns409()
    {
        var (owner, _) = await OwnerWithHouseholdAsync();
        (await owner.PostAsJsonAsync("/api/budget", new { currency = "PLN" }, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        var conflict = await owner.PostAsJsonAsync("/api/budget", new { currency = "USD" }, TestContext.Current.CancellationToken);

        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>An unsupported currency is a 400.</summary>
    [Fact]
    public async Task Initialize_WithUnsupportedCurrency_Returns400()
    {
        var (owner, _) = await OwnerWithHouseholdAsync();

        var bad = await owner.PostAsJsonAsync("/api/budget", new { currency = "GBP" }, TestContext.Current.CancellationToken);

        bad.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>An Adult may initialise; a Child may read but not initialise; a Guest has no access.</summary>
    [Fact]
    public async Task Roles_AdultInitialises_ChildReadsOnly_GuestDenied()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();
        var adult = await MemberAsync(owner, householdId, "Adult");
        var child = await MemberAsync(owner, householdId, "Child");
        var guest = await MemberAsync(owner, householdId, "Guest");

        (await child.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await guest.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await adult.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await child.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await guest.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>A signed-in person without a household gets 404 on both routes — no personal fallback.</summary>
    [Fact]
    public async Task WithoutHousehold_BothRoutesReturn404()
    {
        var loner = await SignedInAsync("Loner");

        (await loner.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await loner.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Another household never sees, or collides with, this household's budget.</summary>
    [Fact]
    public async Task Households_AreIsolated()
    {
        var (ownerA, _) = await OwnerWithHouseholdAsync();
        var (ownerB, _) = await OwnerWithHouseholdAsync();
        var a = await ownerA.PostAsJsonAsync("/api/budget", new { currency = "EUR" }, TestContext.Current.CancellationToken);

        (await ownerB.GetAsync("/api/budget", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var b = await ownerB.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken);

        b.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await b.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Currency.ShouldBe("PLN");
        (await a.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Currency.ShouldBe("EUR");
    }

    /// <summary>Simultaneous first requests leave exactly one budget and one default envelope.</summary>
    [Fact]
    public async Task ConcurrentInitialisation_CreatesOneBudgetAndOneDefaultEnvelope()
    {
        var (owner, householdId) = await OwnerWithHouseholdAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => owner.PostAsJsonAsync("/api/budget", new { }, TestContext.Current.CancellationToken)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var ids = new HashSet<Guid>();
        foreach (var r in responses)
            ids.Add((await r.Content.ReadFromJsonAsync<BudgetBody>(TestContext.Current.CancellationToken))!.Id);
        ids.Count.ShouldBe(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        (await db.Budgets.CountAsync(b => b.HouseholdId == householdId, TestContext.Current.CancellationToken)).ShouldBe(1);
        var accounts = await db.BudgetAccounts.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        accounts.ShouldContain(a => a.BudgetId.Value == ids.Single()
            && a.Visibility == AccountVisibility.Household && a.OwnerPersonId == null);
        accounts.Count(a => a.BudgetId.Value == ids.Single()).ShouldBe(1);
    }

    private sealed record IdBody(Guid Id);
    private sealed record InvitationBody(Guid InvitationId);
    private sealed record BudgetBody(Guid Id, string Currency);
}
