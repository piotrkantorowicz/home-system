namespace Investments.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Text.Json;
using global::Investments.Application.Commands.RecordCheckIn;
using global::Investments.Application.Commands.RecordSnapshot;
using global::Investments.Application.Queries.ListSnapshots;
using global::Investments.Mcp;
using Investments.IntegrationTests.Infrastructure;
using ModelContextProtocol;
using Shared.Abstractions.Core.Pagination;

/// <summary>#666 — balance write tools: same results as REST, repeats change nothing, other adults' private wallets stay out of reach, scope enforced.</summary>
public sealed class InvestmentsWriteToolsTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private const string Scopes = $"{InvestmentsMcp.ReadScope} {InvestmentsMcp.WriteScope}";

    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;
    private readonly InvestmentsMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public InvestmentsWriteToolsTests(InvestmentsDatabaseFixture fixture)
    {
        _factory = new InvestmentsApiFactory(fixture);
        _scenario = new InvestmentsScenario(_factory);
        _mcp = new InvestmentsMcpHost(_factory);
    }

    /// <summary>Disposes the hosts created for this test instance.</summary>
    public void Dispose()
    {
        _mcp.Dispose();
        _factory.Dispose();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<PagedList<SnapshotDto>> SnapshotsAsync(HttpClient client, Guid walletId)
        => (await client.GetFromJsonAsync<PagedList<SnapshotDto>>($"/api/investments/wallets/{walletId}/snapshots", Ct))!;

    private static JsonElement Entries(params object[] entries) => JsonSerializer.SerializeToElement(entries, JsonSerializerOptions.Web);

    /// <summary>A recorded balance reads back over REST; repeating it answers like a REST repeat and stores nothing new.</summary>
    [Fact]
    public async Task RecordSnapshot_WhenRepeated_MatchesRestAndKeepsOneBalance()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");
        Dictionary<string, object?> args = new() { ["walletId"] = wallet.Id, ["asOf"] = "2026-09-01", ["amount"] = "1200.5", ["note"] = "payday" };

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var first = await InvestmentsMcpHost.CallAsync(client, "record_snapshot", args);
        var repeat = await InvestmentsMcpHost.CallAsync(client, "record_snapshot", args);
        var rest = await h.Owner.PutAsJsonAsync(
            $"/api/investments/wallets/{wallet.Id}/snapshots/2026-09-01", new { amount = "1200.5", note = "payday" }, Ct);

        JsonSerializer.Deserialize<RecordSnapshotResult>(first, JsonSerializerOptions.Web)
            .ShouldBe(new RecordSnapshotResult(new DateOnly(2026, 9, 1), "1200.50", "payday", Created: true));
        InvestmentsMcpHost.Normalize<RecordSnapshotResult>(repeat)
            .ShouldBe(InvestmentsMcpHost.Normalize<RecordSnapshotResult>(await rest.Content.ReadAsStringAsync(Ct)));
        (await SnapshotsAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(1);
    }

    /// <summary>A check-in stores every balance once; the same check-in again creates nothing and answers like REST.</summary>
    [Fact]
    public async Task RecordCheckIn_WhenRepeated_MatchesRestAndCreatesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var broker = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");
        var savings = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Savings", "Private");
        object[] entries = [new { walletId = broker.Id, amount = "100" }, new { walletId = savings.Id, amount = "50.25" }];
        Dictionary<string, object?> args = new() { ["asOf"] = "2026-09-01", ["entries"] = Entries(entries) };

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var first = await InvestmentsMcpHost.CallAsync(client, "record_check_in", args);
        var repeat = await InvestmentsMcpHost.CallAsync(client, "record_check_in", args);
        var rest = await h.Owner.PutAsJsonAsync("/api/investments/snapshots", new { asOf = "2026-09-01", entries }, Ct);

        JsonSerializer.Deserialize<RecordCheckInResult>(first, JsonSerializerOptions.Web)
            .ShouldBe(new RecordCheckInResult(new DateOnly(2026, 9, 1), Saved: 2, Created: 2));
        InvestmentsMcpHost.Normalize<RecordCheckInResult>(repeat)
            .ShouldBe(InvestmentsMcpHost.Normalize<RecordCheckInResult>(await rest.Content.ReadAsStringAsync(Ct)));
        repeat.ShouldContain("\"created\":0");
        (await SnapshotsAsync(h.Owner, broker.Id)).TotalCount.ShouldBe(1);
        (await SnapshotsAsync(h.Owner, savings.Id)).TotalCount.ShouldBe(1);
    }

    /// <summary>Another adult's private wallet is unreachable: a check-in naming it saves nothing, and recording or deleting its balance fails like a missing wallet.</summary>
    [Fact]
    public async Task WriteTools_WithAnotherAdultsPrivateWallet_FailLikeMissingAndSaveNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var mine = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Mine", "Shared");
        var theirs = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Theirs", "Private");
        (await adult.Client.PutAsJsonAsync($"/api/investments/wallets/{theirs.Id}/snapshots/2026-09-01", new { amount = "99" }, Ct))
            .EnsureSuccessStatusCode();
        var absent = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var checkIn = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "record_check_in", new()
        {
            ["asOf"] = "2026-09-01",
            ["entries"] = Entries(new { walletId = mine.Id, amount = "10" }, new { walletId = theirs.Id, amount = "1" }),
        });
        var record = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "record_snapshot",
            new() { ["walletId"] = theirs.Id, ["asOf"] = "2026-09-01", ["amount"] = "1" });
        var recordMissing = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "record_snapshot",
            new() { ["walletId"] = absent, ["asOf"] = "2026-09-01", ["amount"] = "1" });
        var delete = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "delete_snapshot",
            new() { ["walletId"] = theirs.Id, ["asOf"] = "2026-09-01" });
        var deleteMissing = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "delete_snapshot",
            new() { ["walletId"] = absent, ["asOf"] = "2026-09-01" });

        checkIn.ShouldContain($"{theirs.Id}: wallet not found");
        (await SnapshotsAsync(h.Owner, mine.Id)).TotalCount.ShouldBe(0);
        record.Replace(theirs.Id.ToString(), "{id}", StringComparison.Ordinal)
            .ShouldBe(recordMissing.Replace(absent.ToString(), "{id}", StringComparison.Ordinal));
        delete.Replace(theirs.Id.ToString(), "{id}", StringComparison.Ordinal)
            .ShouldBe(deleteMissing.Replace(absent.ToString(), "{id}", StringComparison.Ordinal));
        (await SnapshotsAsync(adult.Client, theirs.Id)).Items.ShouldHaveSingleItem().Amount.ShouldBe("99.00");
    }

    /// <summary>Deleting removes the balance; a repeat reports that nothing is recorded for the date.</summary>
    [Fact]
    public async Task DeleteSnapshot_WhenRepeated_ReportsNothingRecorded()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");
        (await h.Owner.PutAsJsonAsync($"/api/investments/wallets/{wallet.Id}/snapshots/2026-09-01", new { amount = "10" }, Ct))
            .EnsureSuccessStatusCode();
        Dictionary<string, object?> args = new() { ["walletId"] = wallet.Id, ["asOf"] = "2026-09-01" };

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        await InvestmentsMcpHost.CallAsync(client, "delete_snapshot", args);
        var repeat = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "delete_snapshot", args);

        (await SnapshotsAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
        repeat.ShouldContain("No balance is recorded for 2026-09-01");
    }

    /// <summary>A token with only the read scope neither lists nor runs the write tools.</summary>
    [Fact]
    public async Task WriteTools_WithoutWriteScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync(
            "record_snapshot",
            new Dictionary<string, object?> { ["walletId"] = wallet.Id, ["asOf"] = "2026-09-01", ["amount"] = "1" },
            cancellationToken: Ct).AsTask());

        tools.ShouldNotContain(t => t.Name == "record_snapshot" || t.Name == "record_check_in" || t.Name == "delete_snapshot");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await SnapshotsAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }
}
