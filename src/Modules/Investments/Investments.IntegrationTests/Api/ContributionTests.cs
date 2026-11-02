namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using global::Investments.Domain.ValueObjects;
using global::Investments.Infrastructure.Persistence;
using Investments.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>#794 — deposits and withdrawals: validation, archive rules, visibility, ordering.</summary>
public sealed class ContributionTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsDatabaseFixture _fixture;
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public ContributionTests(InvestmentsDatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> PostAsync(
        HttpClient client, Guid walletId, string date, string amount, string direction = "Deposit", string? note = null)
        => client.PostAsJsonAsync($"/api/investments/wallets/{walletId}/contributions", new { date, direction, amount, note }, Ct);

    private static async Task<Contribution> CreateAsync(
        HttpClient client, Guid walletId, string date, string amount, string direction = "Deposit")
    {
        var response = await PostAsync(client, walletId, date, amount, direction);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<Contribution>(Ct))!;
    }

    private static async Task<ContributionPage> ListAsync(HttpClient client, Guid walletId, string query = "")
        => (await client.GetFromJsonAsync<ContributionPage>($"/api/investments/wallets/{walletId}/contributions{query}", Ct))!;

    private sealed record Contribution(Guid Id, DateOnly Date, string Direction, string Amount, string? Note, string RecordedByName);

    private sealed record ContributionPage(List<Contribution> Items, int TotalCount);

    /// <summary>A deposit is created with a location and normalised amount, and the list shows it.</summary>
    [Fact]
    public async Task Post_Valid_CreatesAndLists()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "IKE", kind: "IKE");

        var response = await PostAsync(h.Owner, wallet.Id, "2026-09-01", "500.5", note: " top-up ");
        var list = await ListAsync(h.Owner, wallet.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.Content.ReadFromJsonAsync<Contribution>(Ct))!;
        response.Headers.Location!.ToString().ShouldBe($"/api/investments/wallets/{wallet.Id}/contributions/{created.Id}");
        list.Items.ShouldHaveSingleItem().ShouldBe(
            new Contribution(created.Id, new DateOnly(2026, 9, 1), "Deposit", "500.50", "top-up", "Owner"));
    }

    /// <summary>Zero, negative, over-precise and unknown-direction bodies are 400; a future or too-early date is 422.</summary>
    /// <param name="date">Body date.</param>
    /// <param name="amount">Body amount.</param>
    /// <param name="direction">Body direction.</param>
    /// <param name="expected">Expected status.</param>
    [Theory]
    [InlineData("2026-09-01", "0", "Deposit", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "-1", "Deposit", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "1.234", "Deposit", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "10", "Transfer", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "10", "1", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-13", "10", "Deposit", HttpStatusCode.UnprocessableEntity)]
    [InlineData("2002-01-01", "10", "Deposit", HttpStatusCode.UnprocessableEntity)]
    public async Task Post_Invalid_IsRejected(string date, string amount, string direction, HttpStatusCode expected)
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");

        var response = await PostAsync(h.Owner, wallet.Id, date, amount, direction);

        response.StatusCode.ShouldBe(expected);
        (await ListAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>A note over 200 characters is 400.</summary>
    [Fact]
    public async Task Post_NoteTooLong_IsRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");

        var response = await PostAsync(h.Owner, wallet.Id, "2026-09-01", "10", note: new string('x', 201));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>An archived wallet rejects a new contribution but allows deleting existing ones.</summary>
    [Fact]
    public async Task ArchivedWallet_RejectsNew_AllowsDelete()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Closed deposit", kind: "Savings");
        var existing = await CreateAsync(h.Owner, wallet.Id, "2026-08-31", "500");
        (await h.Owner.PostAsync($"/api/investments/wallets/{wallet.Id}/archive", null, Ct)).EnsureSuccessStatusCode();

        var post = await PostAsync(h.Owner, wallet.Id, "2026-09-01", "100");
        var delete = await h.Owner.DeleteAsync($"/api/investments/wallets/{wallet.Id}/contributions/{existing.Id}", Ct);

        post.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>Deleting an unknown id, or another wallet's contribution through this wallet, is not-found.</summary>
    [Fact]
    public async Task Delete_UnknownOrOtherWallet_Returns404()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        var other = await InvestmentsScenario.CreateWalletAsync(h.Owner, "IKE", kind: "IKE");
        var foreign = await CreateAsync(h.Owner, other.Id, "2026-09-01", "100");

        var unknown = await h.Owner.DeleteAsync($"/api/investments/wallets/{wallet.Id}/contributions/{Guid.NewGuid()}", Ct);
        var crossWallet = await h.Owner.DeleteAsync($"/api/investments/wallets/{wallet.Id}/contributions/{foreign.Id}", Ct);

        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        crossWallet.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(h.Owner, other.Id)).TotalCount.ShouldBe(1);
    }

    /// <summary>Another adult's private wallet: list, record and delete are all 404.</summary>
    [Fact]
    public async Task InvisibleWallet_Returns404Everywhere()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var hidden = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Secret", "Private");
        var existing = await CreateAsync(adult.Client, hidden.Id, "2026-09-01", "100");

        var list = await h.Owner.GetAsync($"/api/investments/wallets/{hidden.Id}/contributions", Ct);
        var post = await PostAsync(h.Owner, hidden.Id, "2026-09-02", "1");
        var delete = await h.Owner.DeleteAsync($"/api/investments/wallets/{hidden.Id}/contributions/{existing.Id}", Ct);

        list.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        post.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ListAsync(adult.Client, hidden.Id)).TotalCount.ShouldBe(1);
    }

    /// <summary>The list is newest first and pages; several entries on one date are all kept.</summary>
    [Fact]
    public async Task List_PagesNewestFirst()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        await CreateAsync(h.Owner, wallet.Id, "2026-07-31", "100");
        await CreateAsync(h.Owner, wallet.Id, "2026-09-01", "300");
        await CreateAsync(h.Owner, wallet.Id, "2026-08-31", "200", "Withdrawal");
        await CreateAsync(h.Owner, wallet.Id, "2026-09-01", "50");

        var first = await ListAsync(h.Owner, wallet.Id, "?page=1&pageSize=3");
        var second = await ListAsync(h.Owner, wallet.Id, "?page=2&pageSize=3");

        first.TotalCount.ShouldBe(4);
        first.Items.Select(c => (c.Date, c.Direction, c.Amount)).ShouldBe([
            (new DateOnly(2026, 9, 1), "Deposit", "50.00"),
            (new DateOnly(2026, 9, 1), "Deposit", "300.00"),
            (new DateOnly(2026, 8, 31), "Withdrawal", "200.00"),
        ]);
        second.Items.ShouldHaveSingleItem().Amount.ShouldBe("100.00");
    }

    /// <summary>The composite foreign key stops a contribution pointing at a wallet in another household.</summary>
    [Fact]
    public async Task Database_RejectsCrossHouseholdContribution()
    {
        var h = await _scenario.NewHouseholdAsync();
        var created = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        await using var db = new InvestmentsDbContext(
            new DbContextOptionsBuilder<InvestmentsDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var walletId = WalletId.From(created.Id);
        var wallet = await db.Wallets.SingleAsync(w => w.Id == walletId, Ct);
        Money.TryParse("10", out var amount).ShouldBeTrue();
        var contribution = global::Investments.Domain.Aggregates.Contribution.Record(
            ContributionId.New(), wallet, new DateOnly(2026, 9, 1), ContributionDirection.Deposit, amount, null,
            h.OwnerPersonId, "Owner", new DateOnly(2026, 9, 12), TestClock.Now);
        db.Contributions.Add(contribution);
        db.Entry(contribution).Property(c => c.HouseholdId).CurrentValue = Guid.CreateVersion7();

        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
    }
}
