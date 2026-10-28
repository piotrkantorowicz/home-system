namespace Budget.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Text.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Application.Commands.RecordRepayment;
using global::Budget.Application.Queries.GetSettlement;
using global::Budget.Application.Queries.ListRepayments;
using global::Budget.Mcp;
using ModelContextProtocol;
using Shared.Abstractions.Core.Pagination;

/// <summary>#663 — settlement and repayment tools over MCP: same figures as REST, replay, void, adults only, scope enforced.</summary>
public sealed class BudgetSettlementToolsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetSettlementToolsTests(BudgetDatabaseFixture fixture)
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

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;

    private static async Task SharedExpenseAsync(HttpClient client, Guid account, string amount, Guid payer, Guid[] participants)
        => (await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = "Individual",
            paidByPersonId = payer,
            participantIds = participants,
        }, Ct)).EnsureSuccessStatusCode();

    private async Task<(BudgetScenario.Household H, BudgetScenario.Member Bea)> OwedAsync()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        await SharedExpenseAsync(h.Owner, await BudgetScenario.DefaultEnvelopeAsync(h.Owner), "80.00", h.OwnerPersonId, [h.OwnerPersonId, bea.PersonId]);
        return (h, bea);
    }

    /// <summary>Settlement and repayment reads match REST for the same caller.</summary>
    [Fact]
    public async Task ReadTools_MatchRest()
    {
        var (h, bea) = await OwedAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        await BudgetMcpHost.CallAsync(client, "record_repayment", new()
        {
            ["operationKey"] = Guid.CreateVersion7(),
            ["fromPersonId"] = bea.PersonId,
            ["toPersonId"] = h.OwnerPersonId,
            ["amount"] = "15.00",
            ["paidOn"] = "2026-10-02",
        });
        var settlement = await BudgetMcpHost.CallAsync(client, "get_settlement");
        var repayments = await BudgetMcpHost.CallAsync(client, "list_repayments");

        BudgetMcpHost.Normalize<SettlementDto>(settlement)
            .ShouldBe(BudgetMcpHost.Normalize<SettlementDto>(await h.Owner.GetStringAsync("/api/budget/settlement", Ct)));
        BudgetMcpHost.Normalize<PagedList<RepaymentDto>>(repayments)
            .ShouldBe(BudgetMcpHost.Normalize<PagedList<RepaymentDto>>(await h.Owner.GetStringAsync("/api/budget/settlements?pageSize=20", Ct)));
        settlement.ShouldContain("25.00");
    }

    /// <summary>Record is idempotent per operation key; void restores the debt and a stale revision is a tool error.</summary>
    [Fact]
    public async Task RecordReplayAndVoid()
    {
        var (h, bea) = await OwedAsync();
        var key = Guid.CreateVersion7();
        Dictionary<string, object?> Record() => new()
        {
            ["operationKey"] = key,
            ["fromPersonId"] = bea.PersonId,
            ["toPersonId"] = h.OwnerPersonId,
            ["amount"] = "40.00",
            ["paidOn"] = "2026-10-02",
            ["note"] = "cash",
        };

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var created = Parse<RepaymentMutationResult>(await BudgetMcpHost.CallAsync(client, "record_repayment", Record()));
        var replay = Parse<RepaymentMutationResult>(await BudgetMcpHost.CallAsync(client, "record_repayment", Record()));
        var settled = Parse<SettlementDto>(await BudgetMcpHost.CallAsync(client, "get_settlement"));
        var stale = await BudgetMcpHost.CallExpectingErrorAsync(client, "void_repayment",
            new() { ["repaymentId"] = created.RepaymentId, ["expectedRevision"] = 9, ["reason"] = "stale" });
        var voided = Parse<RepaymentMutationResult>(await BudgetMcpHost.CallAsync(client, "void_repayment",
            new() { ["repaymentId"] = created.RepaymentId, ["expectedRevision"] = created.Revision, ["reason"] = "entered twice" }));
        var list = Parse<PagedList<RepaymentDto>>(await BudgetMcpHost.CallAsync(client, "list_repayments"));
        var after = Parse<SettlementDto>(await BudgetMcpHost.CallAsync(client, "get_settlement"));

        created.Created.ShouldBeTrue();
        replay.Created.ShouldBeFalse();
        replay.RepaymentId.ShouldBe(created.RepaymentId);
        settled.IsSettled.ShouldBeTrue();
        stale.ShouldNotBeEmpty();
        voided.Revision.ShouldBe(2);
        list.Items.ShouldHaveSingleItem().IsVoided.ShouldBeTrue();
        after.IsSettled.ShouldBeFalse();
    }

    /// <summary>An all-zero operation key is refused under the name the agent sent.</summary>
    [Fact]
    public async Task RecordRepayment_WithEmptyOperationKey_NamesOperationKey()
    {
        var (h, bea) = await OwedAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.WriteScope);
        var error = await BudgetMcpHost.CallExpectingErrorAsync(client, "record_repayment", new()
        {
            ["operationKey"] = Guid.Empty,
            ["fromPersonId"] = bea.PersonId,
            ["toPersonId"] = h.OwnerPersonId,
            ["amount"] = "1.00",
            ["paidOn"] = "2026-10-02",
        });

        error.ShouldContain("operationKey");
        error.ShouldNotContain("ClientRequestId");
    }

    /// <summary>Children, guests and non-members get errors from every settlement tool, never data or a write.</summary>
    [Fact]
    public async Task Tools_ForNonAdultsAndNonMembers_ReturnErrors()
    {
        var (h, bea) = await OwedAsync();
        var child = await _scenario.MemberAsync(h, "Child");
        var outsider = await _scenario.SignedInAsync("Outsider");
        var scopes = string.Join(' ', BudgetMcp.Scopes);

        foreach (var subject in new[] { BudgetMcpHost.Subject(child.Client), BudgetMcpHost.Subject(outsider) })
        {
            await using var client = await _mcp.ConnectAsync(subject, scopes);
            await BudgetMcpHost.CallExpectingErrorAsync(client, "get_settlement");
            await BudgetMcpHost.CallExpectingErrorAsync(client, "list_repayments");
            await BudgetMcpHost.CallExpectingErrorAsync(client, "record_repayment", new()
            {
                ["operationKey"] = Guid.CreateVersion7(),
                ["fromPersonId"] = bea.PersonId,
                ["toPersonId"] = h.OwnerPersonId,
                ["amount"] = "1.00",
                ["paidOn"] = "2026-10-02",
            });
        }
    }

    /// <summary>A read-only token cannot see or run the write tools; page size is capped.</summary>
    [Fact]
    public async Task Tools_WithoutWriteScope_AreHiddenAndRejected_AndPagingIsCapped()
    {
        var (h, bea) = await OwedAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync("record_repayment",
            new Dictionary<string, object?>
            {
                ["operationKey"] = Guid.CreateVersion7(),
                ["fromPersonId"] = bea.PersonId,
                ["toPersonId"] = h.OwnerPersonId,
                ["amount"] = "1.00",
                ["paidOn"] = "2026-10-02",
            }, cancellationToken: Ct).AsTask());
        var tooBig = await BudgetMcpHost.CallExpectingErrorAsync(client, "list_repayments", new() { ["pageSize"] = 101 });

        tools.ShouldNotContain(t => t.Name == "record_repayment" || t.Name == "void_repayment");
        tools.ShouldContain(t => t.Name == "get_settlement");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        tooBig.ShouldContain("pageSize");
    }
}
