namespace Investments.IntegrationTests.Mcp;

using System.Text.Json;
using global::Investments.Application.Queries.GetWallet;
using global::Investments.Mcp;
using Investments.IntegrationTests.Infrastructure;
using ModelContextProtocol;

/// <summary>#667 — wallet management tools: same wallet as REST shows, other adults' private wallets out of reach, scope enforced.</summary>
public sealed class InvestmentsWalletWriteToolsTests : IClassFixture<InvestmentsDatabaseFixture>, IDisposable
{
    private const string Scopes = $"{InvestmentsMcp.ReadScope} {InvestmentsMcp.WriteScope}";

    private readonly InvestmentsApiFactory _factory;
    private readonly InvestmentsScenario _scenario;
    private readonly InvestmentsMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public InvestmentsWalletWriteToolsTests(InvestmentsDatabaseFixture fixture)
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

    private static async Task<string> RestWalletAsync(HttpClient client, Guid id)
        => InvestmentsMcpHost.Normalize<WalletDto>(await client.GetStringAsync($"/api/investments/wallets/{id}", Ct));

    private static WalletDto Wallet(string json) => JsonSerializer.Deserialize<WalletDto>(json, JsonSerializerOptions.Web)!;

    /// <summary>Each tool returns the wallet exactly as REST then reads it: created, renamed, archived (twice), restored.</summary>
    [Fact]
    public async Task WalletWriteTools_ReturnTheWalletAsRestReadsIt()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var created = await InvestmentsMcpHost.CallAsync(client, "create_wallet", new()
        {
            ["name"] = "Broker",
            ["institution"] = "XTB",
            ["kind"] = "IKE",
            ["currency"] = "PLN",
            ["visibility"] = "Shared",
        });
        var id = Wallet(created).Id;
        InvestmentsMcpHost.Normalize<WalletDto>(created).ShouldBe(await RestWalletAsync(h.Owner, id));

        var updated = await InvestmentsMcpHost.CallAsync(client, "update_wallet", new()
        {
            ["walletId"] = id,
            ["name"] = "Retirement",
            ["visibility"] = "Private",
            ["version"] = Wallet(created).Version,
        });
        InvestmentsMcpHost.Normalize<WalletDto>(updated).ShouldBe(await RestWalletAsync(h.Owner, id));
        Wallet(updated).Name.ShouldBe("Retirement");

        var archived = await InvestmentsMcpHost.CallAsync(client, "archive_wallet", new() { ["walletId"] = id });
        var archivedAgain = await InvestmentsMcpHost.CallAsync(client, "archive_wallet", new() { ["walletId"] = id });
        InvestmentsMcpHost.Normalize<WalletDto>(archivedAgain).ShouldBe(InvestmentsMcpHost.Normalize<WalletDto>(archived));
        InvestmentsMcpHost.Normalize<WalletDto>(archived).ShouldBe(await RestWalletAsync(h.Owner, id));
        Wallet(archived).IsArchived.ShouldBeTrue();

        var restored = await InvestmentsMcpHost.CallAsync(client, "restore_wallet", new() { ["walletId"] = id });
        InvestmentsMcpHost.Normalize<WalletDto>(restored).ShouldBe(await RestWalletAsync(h.Owner, id));
        Wallet(restored).IsArchived.ShouldBeFalse();
    }

    /// <summary>An update with a version the wallet has moved past is refused with a reload hint and changes nothing.</summary>
    [Fact]
    public async Task UpdateWallet_WithStaleVersion_IsRefused()
    {
        var h = await _scenario.NewHouseholdAsync();
        var wallet = await InvestmentsScenario.CreateWalletAsync(h.Owner, "Broker", "Shared");

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        var version = Wallet(await InvestmentsMcpHost.CallAsync(client, "get_wallet", new() { ["walletId"] = wallet.Id })).Version;
        await InvestmentsMcpHost.CallAsync(client, "update_wallet", new()
        {
            ["walletId"] = wallet.Id,
            ["name"] = "First",
            ["visibility"] = "Shared",
            ["version"] = version,
        });
        var stale = await InvestmentsMcpHost.CallExpectingErrorAsync(client, "update_wallet", new()
        {
            ["walletId"] = wallet.Id,
            ["name"] = "Second",
            ["visibility"] = "Shared",
            ["version"] = version,
        });

        stale.ShouldContain("changed by someone else");
        Wallet(await RestWalletAsync(h.Owner, wallet.Id)).Name.ShouldBe("First");
    }

    /// <summary>Updating, archiving or restoring another adult's private wallet fails exactly like a missing wallet.</summary>
    [Fact]
    public async Task WalletWriteTools_WithAnotherAdultsPrivateWallet_FailLikeMissing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var adult = await _scenario.MemberAsync(h, "Adult");
        var theirs = await InvestmentsScenario.CreateWalletAsync(adult.Client, "Theirs", "Private");
        var absent = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), Scopes);
        foreach (var (tool, extra) in new (string, Dictionary<string, object?>)[]
        {
            ("update_wallet", new() { ["name"] = "Mine now", ["visibility"] = "Shared", ["version"] = 1u }),
            ("archive_wallet", []),
            ("restore_wallet", []),
        })
        {
            var hidden = await InvestmentsMcpHost.CallExpectingErrorAsync(client, tool, new(extra) { ["walletId"] = theirs.Id });
            var missing = await InvestmentsMcpHost.CallExpectingErrorAsync(client, tool, new(extra) { ["walletId"] = absent });

            hidden.Replace(theirs.Id.ToString(), "{id}", StringComparison.Ordinal)
                .ShouldBe(missing.Replace(absent.ToString(), "{id}", StringComparison.Ordinal), tool);
        }

        var stored = Wallet(await RestWalletAsync(adult.Client, theirs.Id));
        stored.Name.ShouldBe("Theirs");
        stored.IsArchived.ShouldBeFalse();
    }

    /// <summary>A token with only the read scope neither lists nor runs the wallet write tools.</summary>
    [Fact]
    public async Task WalletWriteTools_WithoutWriteScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(InvestmentsMcpHost.Subject(h.Owner), InvestmentsMcp.ReadScope);
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync(
            "create_wallet",
            new Dictionary<string, object?>
            {
                ["name"] = "Broker",
                ["institution"] = "XTB",
                ["kind"] = "IKE",
                ["currency"] = "PLN",
                ["visibility"] = "Shared",
            },
            cancellationToken: Ct).AsTask());

        string[] writeTools = ["create_wallet", "update_wallet", "archive_wallet", "restore_wallet"];
        tools.ShouldNotContain(t => writeTools.Contains(t.Name));
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        (await InvestmentsScenario.ListAsync(h.Owner)).TotalCount.ShouldBe(0);
    }
}
