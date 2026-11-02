namespace Investments.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Text.Json;
using global::Investments.Application.Queries.ListContributions;
using global::Investments.Mcp;
using Investments.IntegrationTests.Infrastructure;
using ModelContextProtocol;
using Shared.Abstractions.Core.Pagination;

/// <summary>#798 — contribution tools: same results as REST, other adults' private wallets stay out of reach, scope enforced.</summary>
public sealed class InvestmentsContributionToolsTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private const string Scopes = $"{InvestmentsMcp.ReadScope} {InvestmentsMcp.WriteScope}";

    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;
    private readonly InvestmentsMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public InvestmentsContributionToolsTests(InvestmentsDatabaseFixture fixture)
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

    private static async Task<PagedList<ContributionDto>> ContributionsAsync(HttpClient client, Guid walletId)
        => (await client.GetFromJsonAsync<PagedList<ContributionDto>>($"/api/investments/wallets/{walletId}/contributions", Ct))!;

    /// <summary>A recorded deposit reads back over REST and through list_contributions.</summary>
    [Fact]
    public async Task RecordContribution_ReadsBackOverRestAndMcp()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var recorded = await InvestmentsMcpHost.CallAsync(client, "record_contribution", new()
        {
            ["walletId"] = wallet.Id,
            ["date"] = "2026-09-01",
            ["direction"] = "Deposit",
            ["amount"] = "500",
            ["note"] = "payday",
        });
        var listed = await InvestmentsMcpHost.CallAsync(client, "list_contributions", new() { ["walletId"] = wallet.Id });

        var dto = JsonSerializer.Deserialize<ContributionDto>(recorded, JsonSerializerOptions.Web)!;
        (dto.Date, dto.Direction, dto.Amount, dto.Note).ShouldBe((new DateOnly(2026, 9, 1), "Deposit", "500.00", "payday"));
        (await ContributionsAsync(h.Owner, wallet.Id)).Items.ShouldHaveSingleItem().Id.ShouldBe(dto.Id);
        JsonSerializer.Deserialize<PagedList<ContributionDto>>(listed, JsonSerializerOptions.Web)!.Items.ShouldHaveSingleItem().Id.ShouldBe(dto.Id);
    }

    /// <summary>Deleting removes the contribution; a repeat reports it as gone.</summary>
    [Fact]
    public async Task DeleteContribution_WhenRepeated_ReportsNotFound()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");
        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var recorded = JsonSerializer.Deserialize<ContributionDto>(
            await InvestmentsMcpHost.CallAsync(client, "record_contribution", new()
            {
                ["walletId"] = wallet.Id,
                ["date"] = "2026-09-01",
                ["direction"] = "Withdrawal",
                ["amount"] = "20",
            }),
            JsonSerializerOptions.Web)!;
        Dictionary<string, object?> args = new() { ["walletId"] = wallet.Id, ["contributionId"] = recorded.Id };

        await InvestmentsMcpHost.CallAsync(client, "delete_contribution", args);
        var repeat = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "delete_contribution", args);

        (await ContributionsAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
        repeat.ShouldContain("not found", Case.Insensitive);
    }

    /// <summary>Another adult's private wallet fails like a missing one for list, record and delete, and nothing changes.</summary>
    [Fact]
    public async Task ContributionTools_WithAnotherAdultsPrivateWallet_FailLikeMissing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var theirs = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Theirs", "Private");
        var stored = (await adult.Client.PostAsJsonAsync(
            $"/api/investments/wallets/{theirs.Id}/contributions", new { date = "2026-09-01", direction = "Deposit", amount = "99" }, Ct))
            .EnsureSuccessStatusCode();
        var theirEntry = (await stored.Content.ReadFromJsonAsync<ContributionDto>(Ct))!;
        var absent = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        async Task<string> MaskedErrorAsync(string tool, Guid walletId, Dictionary<string, object?> args)
        {
            args["walletId"] = walletId;
            var error = await InvestmentsMcpHost.CallExpectingErrorAsync(client, tool, args);
            return error.Replace(walletId.ToString(), "{id}", StringComparison.Ordinal);
        }

        Dictionary<string, object?> Record() => new() { ["date"] = "2026-09-01", ["direction"] = "Deposit", ["amount"] = "1" };
        Dictionary<string, object?> Delete() => new() { ["contributionId"] = theirEntry.Id };

        (await MaskedErrorAsync("list_contributions", theirs.Id, [])).ShouldBe(await MaskedErrorAsync("list_contributions", absent, []));
        (await MaskedErrorAsync("record_contribution", theirs.Id, Record())).ShouldBe(await MaskedErrorAsync("record_contribution", absent, Record()));
        (await MaskedErrorAsync("delete_contribution", theirs.Id, Delete())).ShouldBe(await MaskedErrorAsync("delete_contribution", absent, Delete()));
        (await ContributionsAsync(adult.Client, theirs.Id)).Items.ShouldHaveSingleItem().Id.ShouldBe(theirEntry.Id);
    }

    /// <summary>A token with only the read scope lists contributions but neither lists nor runs the write tools.</summary>
    [Fact]
    public async Task WriteTools_WithoutWriteScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync(
            "record_contribution",
            new Dictionary<string, object?> { ["walletId"] = wallet.Id, ["date"] = "2026-09-01", ["direction"] = "Deposit", ["amount"] = "1" },
            cancellationToken: Ct).AsTask());

        tools.ShouldContain(t => t.Name == "list_contributions");
        tools.ShouldNotContain(t => t.Name == "record_contribution" || t.Name == "delete_contribution");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await ContributionsAsync(h.Owner, wallet.Id)).TotalCount.ShouldBe(0);
    }
}
