namespace Investments.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Investments.IntegrationTests.Infrastructure;

/// <summary>#578 — monthly check-in: all or nothing, idempotent, errors by wallet id without leaking hidden wallets.</summary>
public sealed class CheckInTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public CheckInTests(InvestmentsDatabaseFixture fixture)
    {
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record Result(DateOnly AsOf, int Saved, int Created);

    private sealed record Problem(string? Detail, Dictionary<string, string[]>? Errors);

    private sealed record HistoryPage(int TotalCount, List<HistoryRow> Items);

    private sealed record HistoryRow(string Amount);

    private static Task<HttpResponseMessage> CheckInAsync(HttpClient client, string asOf, params object[] entries)
        => client.PutAsJsonAsync("/api/investments/snapshots", new { asOf, entries }, Ct);

    private static async Task<HistoryPage> HistoryAsync(HttpClient client, Guid walletId)
        => (await client.GetFromJsonAsync<HistoryPage>($"/api/investments/wallets/{walletId}/snapshots", Ct))!;

    /// <summary>Two wallets are saved in one request; sending the same batch again changes nothing.</summary>
    [Fact]
    public async Task CheckIn_SavesAll_AndRepeatIsIdempotent()
    {
        var h = await _scenario.NewHouseholdAsync();
        var a = await InvestmentsScenario.CreateWalletAsync(h.Owner, "XTB");
        var b = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Savings", kind: "Savings");
        object[] entries = [new { walletId = a.Id, amount = "1000" }, new { walletId = b.Id, amount = "250.5", note = "bonus" }];

        var first = await CheckInAsync(h.Owner, "2026-09-01", entries);
        var repeat = await CheckInAsync(h.Owner, "2026-09-01", entries);

        (await first.Content.ReadFromJsonAsync<Result>(Ct)).ShouldBe(new Result(new DateOnly(2026, 9, 1), 2, 2));
        (await repeat.Content.ReadFromJsonAsync<Result>(Ct)).ShouldBe(new Result(new DateOnly(2026, 9, 1), 2, 0));
        (await HistoryAsync(h.Owner, a.Id)).Items.ShouldHaveSingleItem().Amount.ShouldBe("1000.00");
        (await HistoryAsync(h.Owner, b.Id)).Items.ShouldHaveSingleItem().Amount.ShouldBe("250.50");
    }

    /// <summary>One entry with too many decimals fails the whole batch, named by its wallet id.</summary>
    [Fact]
    public async Task CheckIn_OneInvalidAmount_SavesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var good = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Good");
        var bad = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Bad");

        var response = await CheckInAsync(h.Owner, "2026-09-01",
            new { walletId = good.Id, amount = "100" }, new { walletId = bad.Id, amount = "1.234" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = (await response.Content.ReadFromJsonAsync<Problem>(Ct))!;
        problem.Detail!.ShouldContain(bad.Id.ToString());
        problem.Detail!.ShouldNotContain(good.Id.ToString());
        problem.Errors!.Keys.ShouldBe([bad.Id.ToString()]);
        (await HistoryAsync(h.Owner, good.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>Another adult's private wallet is reported only as "not found" for the id sent, and nothing is saved.</summary>
    [Fact]
    public async Task CheckIn_InvisibleWallet_NotFoundByIdOnly()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var hidden = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Adult secret IKZE", "Private", kind: "IKZE");
        var mine = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Mine");

        var response = await CheckInAsync(h.Owner, "2026-09-01",
            new { walletId = mine.Id, amount = "10" }, new { walletId = hidden.Id, amount = "10" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = (await response.Content.ReadFromJsonAsync<Problem>(Ct))!;
        problem.Errors![hidden.Id.ToString()].ShouldBe(["wallet not found"]);
        var detail = problem.Detail!;
        detail.ShouldContain($"{hidden.Id}: wallet not found");
        detail.ShouldNotContain("Adult secret");
        detail.ShouldNotContain("IKZE");
        (await HistoryAsync(h.Owner, mine.Id)).TotalCount.ShouldBe(0);
        (await HistoryAsync(adult.Client, hidden.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>A new date on an archived wallet, or a future date, fails the batch.</summary>
    /// <param name="asOf">Check-in date.</param>
    /// <param name="archive">Whether the wallet is archived first.</param>
    [Theory]
    [InlineData("2026-09-01", true)]
    [InlineData("2026-09-13", false)]
    public async Task CheckIn_ArchivedOrFuture_Returns422(string asOf, bool archive)
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Wallet");
        if (archive)
            (await h.Owner.PostAsync($"/api/investments/wallets/{wallet.Id}/archive", null, Ct)).EnsureSuccessStatusCode();

        var response = await CheckInAsync(h.Owner, asOf, new { walletId = wallet.Id, amount = "1" });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await HistoryAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }

    /// <summary>An empty batch or the same wallet twice is a malformed request.</summary>
    [Fact]
    public async Task CheckIn_EmptyOrDuplicate_Returns400()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Wallet");

        var empty = await CheckInAsync(h.Owner, "2026-09-01");
        var duplicate = await CheckInAsync(h.Owner, "2026-09-01",
            new { walletId = wallet.Id, amount = "1" }, new { walletId = wallet.Id, amount = "2" });

        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
