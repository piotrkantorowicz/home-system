namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using global::Investments.Domain.Aggregates;
using global::Investments.Domain.ValueObjects;
using global::Investments.Infrastructure.Persistence;
using Investments.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

/// <summary>#575 — balance snapshots: upsert by date, validation, archive rules, visibility, paging.</summary>
public sealed class SnapshotTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsDatabaseFixture _fixture;
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public SnapshotTests(InvestmentsDatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid walletId, string asOf, string amount, string? note = null)
        => client.PutAsJsonAsync($"/api/investments/wallets/{walletId}/snapshots/{asOf}", new { amount, note }, Ct);

    private static async Task<SnapshotPage> HistoryAsync(HttpClient client, Guid walletId, string query = "")
        => (await client.GetFromJsonAsync<SnapshotPage>($"/api/investments/wallets/{walletId}/snapshots{query}", Ct))!;

    private sealed record Snapshot(DateOnly AsOf, string Amount, string? PreviousAmount, string? Note, string RecordedByName);

    private sealed record SnapshotPage(List<Snapshot> Items, int TotalCount);

    private sealed record Latest(Guid Id, string? LatestBalance, DateOnly? LatestAsOf);

    private sealed record LatestPage(List<Latest> Items);

    /// <summary>A new date is created; the same request again changes nothing; a different amount replaces it.</summary>
    [Fact]
    public async Task Put_SameDate_IsIdempotentAndReplaces()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");

        var first = await PutAsync(h.Owner, wallet.Id, "2026-09-01", "1000.5", "opening");
        var repeat = await PutAsync(h.Owner, wallet.Id, "2026-09-01", "1000.5", "opening");
        var correction = await PutAsync(h.Owner, wallet.Id, "2026-09-01", "1200", null);
        var history = await HistoryAsync(h.Owner, wallet.Id);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        repeat.StatusCode.ShouldBe(HttpStatusCode.OK);
        correction.StatusCode.ShouldBe(HttpStatusCode.OK);
        var only = history.Items.ShouldHaveSingleItem();
        only.Amount.ShouldBe("1200.00");
        only.Note.ShouldBeNull();
        only.RecordedByName.ShouldBe("Owner");
    }

    /// <summary>Negative, over-precise, exponent and non-numeric amounts are 400; a future date is 422.</summary>
    /// <param name="asOf">Route date.</param>
    /// <param name="amount">Body amount.</param>
    /// <param name="expected">Expected status.</param>
    [Theory]
    [InlineData("2026-09-01", "-1", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "1.234", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "1e3", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "1,50", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-01", "10000000000", HttpStatusCode.BadRequest)]
    [InlineData("2026-09-13", "10", HttpStatusCode.UnprocessableEntity)]
    [InlineData("not-a-date", "10", HttpStatusCode.BadRequest)]
    public async Task Put_Invalid_IsRejected(string asOf, string amount, HttpStatusCode expected)
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");

        var response = await PutAsync(h.Owner, wallet.Id, asOf, amount);

        response.StatusCode.ShouldBe(expected);
        (await HistoryAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>Zero is a valid balance, and today's date is not in the future.</summary>
    [Fact]
    public async Task Put_ZeroToday_IsAccepted()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Empty account");

        var response = await PutAsync(h.Owner, wallet.Id, "2026-09-12", "0");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>An archived wallet rejects a new date but allows correcting and deleting existing ones.</summary>
    [Fact]
    public async Task ArchivedWallet_RejectsNewDates_AllowsCorrectionsAndDeletes()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Closed deposit", kind: "Savings");
        (await PutAsync(h.Owner, wallet.Id, "2026-08-31", "500")).EnsureSuccessStatusCode();
        (await PutAsync(h.Owner, wallet.Id, "2026-07-31", "400")).EnsureSuccessStatusCode();
        (await h.Owner.PostAsync($"/api/investments/wallets/{wallet.Id}/archive", null, Ct)).EnsureSuccessStatusCode();

        var newDate = await PutAsync(h.Owner, wallet.Id, "2026-09-01", "600");
        var correction = await PutAsync(h.Owner, wallet.Id, "2026-08-31", "550");
        var delete = await h.Owner.DeleteAsync($"/api/investments/wallets/{wallet.Id}/snapshots/2026-07-31", Ct);
        var history = await HistoryAsync(h.Owner, wallet.Id);

        newDate.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        correction.StatusCode.ShouldBe(HttpStatusCode.OK);
        delete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        history.Items.ShouldHaveSingleItem().Amount.ShouldBe("550.00");
    }

    /// <summary>Deleting a date with no balance is not-found.</summary>
    [Fact]
    public async Task Delete_UnknownDate_Returns404()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");

        var response = await h.Owner.DeleteAsync($"/api/investments/wallets/{wallet.Id}/snapshots/2026-09-01", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Another adult's private wallet: history, record and delete are all 404.</summary>
    [Fact]
    public async Task InvisibleWallet_Returns404Everywhere()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var hidden = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Secret", "Private");
        (await PutAsync(adult.Client, hidden.Id, "2026-09-01", "100")).EnsureSuccessStatusCode();

        var list = await h.Owner.GetAsync($"/api/investments/wallets/{hidden.Id}/snapshots", Ct);
        var put = await PutAsync(h.Owner, hidden.Id, "2026-09-02", "1");
        var delete = await h.Owner.DeleteAsync($"/api/investments/wallets/{hidden.Id}/snapshots/2026-09-01", Ct);

        list.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        put.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await HistoryAsync(adult.Client, hidden.Id)).TotalCount.ShouldBe(1);
    }

    /// <summary>History is newest first and every row, including the last on a page, knows the previous balance.</summary>
    [Fact]
    public async Task History_PagesNewestFirstWithPreviousAmount()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        (await PutAsync(h.Owner, wallet.Id, "2026-07-31", "100")).EnsureSuccessStatusCode();
        (await PutAsync(h.Owner, wallet.Id, "2026-09-01", "300")).EnsureSuccessStatusCode();
        (await PutAsync(h.Owner, wallet.Id, "2026-08-31", "200")).EnsureSuccessStatusCode();

        var first = await HistoryAsync(h.Owner, wallet.Id, "?page=1&pageSize=2");
        var second = await HistoryAsync(h.Owner, wallet.Id, "?page=2&pageSize=2");

        first.TotalCount.ShouldBe(3);
        first.Items.Select(s => (s.AsOf.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), s.Amount, s.PreviousAmount))
            .ShouldBe([("2026-09-01", "300.00", "200.00"), ("2026-08-31", "200.00", "100.00")]);
        second.Items.ShouldHaveSingleItem().PreviousAmount.ShouldBeNull();
    }

    /// <summary>The wallet list and detail carry the latest balance and its date.</summary>
    [Fact]
    public async Task Wallet_ShowsLatestBalance()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        var empty = await InvestmentsScenario.CreateWalletAsync(h.Owner, "New");
        (await PutAsync(h.Owner, wallet.Id, "2026-08-31", "200")).EnsureSuccessStatusCode();
        (await PutAsync(h.Owner, wallet.Id, "2026-07-31", "100")).EnsureSuccessStatusCode();

        var page = await h.Owner.GetFromJsonAsync<LatestPage>("/api/investments/wallets", Ct);
        var detail = await h.Owner.GetFromJsonAsync<Latest>($"/api/investments/wallets/{wallet.Id}", Ct);

        detail!.LatestBalance.ShouldBe("200.00");
        detail.LatestAsOf.ShouldBe(new DateOnly(2026, 8, 31));
        var none = page!.Items.Single(w => w.Id == empty.Id);
        none.LatestBalance.ShouldBeNull();
        none.LatestAsOf.ShouldBeNull();
    }

    /// <summary>The composite foreign key stops a snapshot pointing at a wallet in another household.</summary>
    [Fact]
    public async Task Database_RejectsCrossHouseholdSnapshot()
    {
        var h = await _scenario.NewHouseholdAsync();
        var created = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        await using var db = new InvestmentsDbContext(
            new DbContextOptionsBuilder<InvestmentsDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var walletId = WalletId.From(created.Id);
        var wallet = await db.Wallets.SingleAsync(w => w.Id == walletId, Ct);
        var snapshot = BalanceSnapshot.Record(
            BalanceSnapshotId.New(), wallet, new DateOnly(2026, 9, 1), default, null, h.OwnerPersonId, "Owner",
            new DateOnly(2026, 9, 12), TestClock.Now);
        db.BalanceSnapshots.Add(snapshot);
        db.Entry(snapshot).Property(s => s.HouseholdId).CurrentValue = Guid.CreateVersion7();

        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
    }
}
