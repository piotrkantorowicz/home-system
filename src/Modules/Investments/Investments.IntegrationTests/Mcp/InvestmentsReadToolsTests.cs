namespace Investments.IntegrationTests.Mcp;

using System.Net.Http.Json;
using global::Investments.Application.Queries.GetNetWorth;
using global::Investments.Application.Queries.GetNetWorthHistory;
using global::Investments.Application.Queries.GetWallet;
using global::Investments.Application.Queries.ListSnapshots;
using global::Investments.Mcp;
using Investments.IntegrationTests.Infrastructure;
using ModelContextProtocol;
using Shared.Abstractions.Core.Pagination;

/// <summary>#665 — wallet and net-worth read tools: same results as REST for the same caller, same visibility, scope enforced.</summary>
public sealed class InvestmentsReadToolsTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;
    private readonly InvestmentsMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public InvestmentsReadToolsTests(InvestmentsDatabaseFixture fixture)
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

    private static Task<string> RestAsync(HttpClient client, string path) => client.GetStringAsync(path, Ct);

    private static async Task<Guid> WalletWithBalanceAsync(HttpClient client, string name, string visibility, string amount)
    {
        var wallet = await InvestmentsScenario.CreateWalletAsync(client, name, visibility);
        (await client.PutAsJsonAsync($"/api/investments/wallets/{wallet.Id}/snapshots/2026-09-01", new { amount }, Ct))
            .EnsureSuccessStatusCode();
        return wallet.Id;
    }

    /// <summary>Wallet list, one wallet and its balances read the same over MCP as over REST; another adult's private wallet is absent.</summary>
    [Fact]
    public async Task WalletTools_MatchRestAndHideAnotherAdultsPrivateWallet()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var shared = await WalletWithBalanceAsync(h.Owner, "Shared", "Shared", "1000.50");
        var hidden = await WalletWithBalanceAsync(adult.Client, "Theirs", "Private", "99.00");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var list = await InvestmentsMcpHost.CallAsync(client, "list_wallets");
        var one = await InvestmentsMcpHost.CallAsync(client, "get_wallet", new() { ["walletId"] = shared });
        var snapshots = await InvestmentsMcpHost.CallAsync(client, "list_snapshots", new() { ["walletId"] = shared });

        InvestmentsMcpHost.Normalize<PagedList<WalletDto>>(list)
            .ShouldBe(InvestmentsMcpHost.Normalize<PagedList<WalletDto>>(await RestAsync(h.Owner, "/api/investments/wallets")));
        InvestmentsMcpHost.Normalize<WalletDto>(one)
            .ShouldBe(InvestmentsMcpHost.Normalize<WalletDto>(await RestAsync(h.Owner, $"/api/investments/wallets/{shared}")));
        InvestmentsMcpHost.Normalize<PagedList<SnapshotDto>>(snapshots)
            .ShouldBe(InvestmentsMcpHost.Normalize<PagedList<SnapshotDto>>(await RestAsync(h.Owner, $"/api/investments/wallets/{shared}/snapshots")));
        list.ShouldContain(shared.ToString());
        list.ShouldNotContain(hidden.ToString());
    }

    /// <summary>A guessed id of another adult's private wallet fails exactly like a missing id, for the wallet and its balances.</summary>
    [Fact]
    public async Task WalletTools_WithAnotherAdultsPrivateWallet_FailLikeMissing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var hidden = await WalletWithBalanceAsync(adult.Client, "Theirs", "Private", "99.00");
        var absent = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var wallet = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "get_wallet", new() { ["walletId"] = hidden });
        var missing = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "get_wallet", new() { ["walletId"] = absent });
        var snapshots = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "list_snapshots", new() { ["walletId"] = hidden });
        var missingSnapshots = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "list_snapshots", new() { ["walletId"] = absent });

        wallet.Replace(hidden.ToString(), "{id}", StringComparison.Ordinal)
            .ShouldBe(missing.Replace(absent.ToString(), "{id}", StringComparison.Ordinal));
        snapshots.Replace(hidden.ToString(), "{id}", StringComparison.Ordinal)
            .ShouldBe(missingSnapshots.Replace(absent.ToString(), "{id}", StringComparison.Ordinal));
    }

    /// <summary>Net worth and its history read the same over MCP as over REST, per scope; a bad scope is an actionable tool error.</summary>
    [Fact]
    public async Task NetWorthTools_MatchRest()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        await WalletWithBalanceAsync(h.Owner, "Shared", "Shared", "1000.50");
        await WalletWithBalanceAsync(h.Owner, "Mine", "Private", "250.00");
        await WalletWithBalanceAsync(adult.Client, "Theirs", "Private", "99.00");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var household = await InvestmentsMcpHost.CallAsync(client, "get_net_worth");
        var mine = await InvestmentsMcpHost.CallAsync(client, "get_net_worth", new() { ["scope"] = "mine", ["asOf"] = "2026-09-05" });
        var history = await InvestmentsMcpHost.CallAsync(client, "get_net_worth_history", new() { ["from"] = "2026-06-01" });
        var invalid = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "get_net_worth", new() { ["scope"] = "everyone" });

        InvestmentsMcpHost.Normalize<NetWorthDto>(household)
            .ShouldBe(InvestmentsMcpHost.Normalize<NetWorthDto>(await RestAsync(h.Owner, "/api/investments/net-worth")));
        InvestmentsMcpHost.Normalize<NetWorthDto>(mine)
            .ShouldBe(InvestmentsMcpHost.Normalize<NetWorthDto>(await RestAsync(h.Owner, "/api/investments/net-worth?scope=mine&asOf=2026-09-05")));
        InvestmentsMcpHost.Normalize<NetWorthHistoryDto>(history)
            .ShouldBe(InvestmentsMcpHost.Normalize<NetWorthHistoryDto>(await RestAsync(h.Owner, "/api/investments/net-worth/history?from=2026-06-01")));
        household.ShouldContain("1250.50");
        invalid.ShouldContain("Scope");
    }

    /// <summary>A token without the investments read scope neither lists nor runs the tools.</summary>
    [Fact]
    public async Task ReadTools_WithoutReadScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), "mcp:diet:read");
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync("list_wallets", cancellationToken: Ct).AsTask());

        tools.ShouldNotContain(t => t.Name == "list_wallets" || t.Name == "get_net_worth");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }
}
