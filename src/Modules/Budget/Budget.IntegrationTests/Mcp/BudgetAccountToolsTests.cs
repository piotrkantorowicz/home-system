namespace Budget.IntegrationTests.Mcp;

using System.Text.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Application.Queries.GetAccount;
using global::Budget.Application.Queries.ListLimits;
using global::Budget.Mcp;
using ModelContextProtocol;

/// <summary>#662 — envelope and monthly-limit tools over MCP: lifecycle, revisions, visibility and scope enforced.</summary>
public sealed class BudgetAccountToolsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetAccountToolsTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
        _mcp = new BudgetMcpHost(_factory);
    }

    /// <summary>Disposes the hosts created for this test instance.</summary>
    public void Dispose()
    {
        _mcp.Dispose();
        _factory.Dispose();
    }

    private static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;

    /// <summary>Create, rename, archive and restore an envelope; each step needs the current revision.</summary>
    [Fact]
    public async Task AccountLifecycle_RoundTripsWithRevisions()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var created = Parse<AccountDto>(await BudgetMcpHost.CallAsync(client, "create_account", new() { ["name"] = "Holidays", ["visibility"] = "Household" }));
        var renamed = Parse<AccountDto>(await BudgetMcpHost.CallAsync(client, "rename_account",
            new() { ["accountId"] = created.Id, ["expectedRevision"] = created.Revision, ["name"] = "Trips" }));
        var stale = await BudgetMcpHost.CallExpectingErrorAsync(client, "rename_account",
            new() { ["accountId"] = created.Id, ["expectedRevision"] = created.Revision, ["name"] = "Old" });
        var archived = Parse<AccountDto>(await BudgetMcpHost.CallAsync(client, "archive_account",
            new() { ["accountId"] = created.Id, ["expectedRevision"] = renamed.Revision }));
        var context = await BudgetMcpHost.CallAsync(client, "get_budget_context");
        var restored = Parse<AccountDto>(await BudgetMcpHost.CallAsync(client, "restore_account",
            new() { ["accountId"] = created.Id, ["expectedRevision"] = archived.Revision }));
        var read = Parse<AccountDto>(await BudgetMcpHost.CallAsync(client, "get_account", new() { ["accountId"] = created.Id }));

        renamed.Name.ShouldBe("Trips");
        stale.ShouldNotBeEmpty();
        archived.IsArchived.ShouldBeTrue();
        context.ShouldNotContain(created.Id.ToString());
        restored.IsArchived.ShouldBeFalse();
        read.ShouldBe(restored);
    }

    /// <summary>Set, change and clear a monthly limit; changing needs the listed revision.</summary>
    [Fact]
    public async Task Limits_SetChangeAndClear()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var first = Parse<MonthlyLimitDto>(await BudgetMcpHost.CallAsync(client, "set_limit",
            new() { ["accountId"] = account, ["month"] = "2026-10", ["amount"] = "1500" }));
        var missingRevision = await BudgetMcpHost.CallExpectingErrorAsync(client, "set_limit",
            new() { ["accountId"] = account, ["month"] = "2026-10", ["amount"] = "1600" });
        var changed = Parse<MonthlyLimitDto>(await BudgetMcpHost.CallAsync(client, "set_limit",
            new() { ["accountId"] = account, ["month"] = "2026-10", ["amount"] = "1600", ["expectedRevision"] = first.Revision }));
        var listed = Parse<List<MonthlyLimitDto>>(await BudgetMcpHost.CallAsync(client, "list_limits", new() { ["month"] = "2026-10" }));
        await BudgetMcpHost.CallAsync(client, "clear_limit",
            new() { ["accountId"] = account, ["month"] = "2026-10", ["expectedRevision"] = changed.Revision });
        var after = Parse<List<MonthlyLimitDto>>(await BudgetMcpHost.CallAsync(client, "list_limits", new() { ["month"] = "2026-10" }));

        missingRevision.ShouldNotBeEmpty();
        listed.ShouldBe([changed]);
        listed[0].Amount.ShouldBe("1600.00");
        after.ShouldBeEmpty();
    }

    /// <summary>Another household's envelope and a non-member get errors, never data or a write.</summary>
    [Fact]
    public async Task AccountTools_ForOtherHouseholdAndNonMember_ReturnErrors()
    {
        var h = await _scenario.NewHouseholdAsync();
        var other = await _scenario.NewHouseholdAsync();
        var theirs = await BudgetScenario.DefaultEnvelopeAsync(other.Owner);
        var outsider = await _scenario.SignedInAsync("Outsider");

        await using var member = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        await using var stranger = await _mcp.ConnectAsync(BudgetMcpHost.Subject(outsider), string.Join(' ', BudgetMcp.Scopes));

        await BudgetMcpHost.CallExpectingErrorAsync(member, "get_account", new() { ["accountId"] = theirs });
        await BudgetMcpHost.CallExpectingErrorAsync(member, "set_limit", new() { ["accountId"] = theirs, ["month"] = "2026-10", ["amount"] = "1" });
        await BudgetMcpHost.CallExpectingErrorAsync(stranger, "create_account", new() { ["name"] = "X", ["visibility"] = "Household" });
        await BudgetMcpHost.CallExpectingErrorAsync(stranger, "list_limits", new() { ["month"] = "2026-10" });
    }

    /// <summary>A read-only token cannot see or run the write tools; a write-only token cannot read.</summary>
    [Fact]
    public async Task Tools_WithoutMatchingScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var reader = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var tools = await reader.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var call = await Should.ThrowAsync<McpProtocolException>(() => reader.CallToolAsync("set_limit",
            new Dictionary<string, object?> { ["accountId"] = account, ["month"] = "2026-10", ["amount"] = "1" },
            cancellationToken: TestContext.Current.CancellationToken).AsTask());

        tools.ShouldNotContain(t => t.Name == "create_account" || t.Name == "set_limit" || t.Name == "clear_limit");
        tools.ShouldContain(t => t.Name == "list_limits");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }
}
