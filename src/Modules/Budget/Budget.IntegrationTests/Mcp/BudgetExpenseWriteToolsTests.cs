namespace Budget.IntegrationTests.Mcp;

using System.Text.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Application.Commands.CreateExpense;
using global::Budget.Application.Queries.GetExpense;
using global::Budget.Application.Queries.ListExpenses;
using global::Budget.Mcp;
using ModelContextProtocol;

/// <summary>#661 — create, correct and void expenses over MCP: replay by operation key, field-path errors, scope and visibility enforced.</summary>
public sealed class BudgetExpenseWriteToolsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetExpenseWriteToolsTests(BudgetDatabaseFixture fixture)
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

    private static Dictionary<string, object?> NewExpense(Guid key, Guid account, string amount = "42.50") => new()
    {
        ["operationKey"] = key,
        ["accountId"] = account,
        ["amount"] = amount,
        ["occurredOn"] = "2026-10-01",
        ["category"] = "Groceries",
        ["fundingSource"] = "HouseholdFunds",
        ["description"] = "Market",
    };

    private static T Parse<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!;

    /// <summary>Create, correct and void work end to end; the same operation key returns the original expense with no duplicate.</summary>
    [Fact]
    public async Task CreateUpdateVoid_RoundTripAndReplay()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var key = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var created = Parse<ExpenseMutationResult>(await BudgetMcpHost.CallAsync(client, "create_expense", NewExpense(key, account)));
        var replay = Parse<ExpenseMutationResult>(await BudgetMcpHost.CallAsync(client, "create_expense", NewExpense(key, account)));
        var list = Parse<ExpenseListDto>(await BudgetMcpHost.CallAsync(client, "search_expenses"));

        created.Created.ShouldBeTrue();
        replay.Created.ShouldBeFalse();
        replay.ExpenseId.ShouldBe(created.ExpenseId);
        list.Items.Count(e => e.Id == created.ExpenseId).ShouldBe(1);

        var updated = Parse<ExpenseMutationResult>(await BudgetMcpHost.CallAsync(client, "update_expense", new()
        {
            ["expenseId"] = created.ExpenseId,
            ["operationKey"] = Guid.CreateVersion7(),
            ["expectedRevision"] = created.Revision,
            ["reason"] = "typo",
            ["amount"] = "45.00",
            ["occurredOn"] = "2026-10-01",
            ["category"] = "Groceries",
            ["fundingSource"] = "HouseholdFunds",
        }));
        var voided = Parse<ExpenseMutationResult>(await BudgetMcpHost.CallAsync(client, "void_expense", new()
        {
            ["expenseId"] = created.ExpenseId,
            ["operationKey"] = Guid.CreateVersion7(),
            ["expectedRevision"] = updated.Revision,
            ["reason"] = "duplicate",
        }));
        var expense = Parse<ExpenseDto>(await BudgetMcpHost.CallAsync(client, "get_expense", new() { ["expenseId"] = created.ExpenseId }));

        updated.Revision.ShouldBe(created.Revision + 1);
        voided.Revision.ShouldBe(updated.Revision + 1);
        expense.IsVoided.ShouldBeTrue();
        expense.Amount.ShouldBe("45.00");
    }

    /// <summary>Invalid input fails as a tool error that names the field.</summary>
    [Fact]
    public async Task CreateExpense_WithInvalidAmountAndCategory_NamesFields()
    {
        var h = await _scenario.NewHouseholdAsync();
        var args = NewExpense(Guid.CreateVersion7(), await BudgetScenario.DefaultEnvelopeAsync(h.Owner), amount: "-3");
        args["category"] = "Nope";

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.WriteScope);
        var error = await BudgetMcpHost.CallExpectingErrorAsync(client, "create_expense", args);

        error.ShouldContain("Amount:");
        error.ShouldContain("Category:");
    }

    /// <summary>A stale revision is a conflict the agent can act on.</summary>
    [Fact]
    public async Task VoidExpense_WithStaleRevision_ReturnsToolError()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.WriteScope);
        var created = Parse<ExpenseMutationResult>(await BudgetMcpHost.CallAsync(client, "create_expense", NewExpense(Guid.CreateVersion7(), account)));

        await BudgetMcpHost.CallExpectingErrorAsync(client, "void_expense", new()
        {
            ["expenseId"] = created.ExpenseId,
            ["operationKey"] = Guid.CreateVersion7(),
            ["expectedRevision"] = created.Revision + 5,
            ["reason"] = "stale",
        });
    }

    /// <summary>Another household's envelope and a non-member get errors, never a write.</summary>
    [Fact]
    public async Task WriteTools_ForOtherHouseholdAndNonMember_ReturnErrors()
    {
        var h = await _scenario.NewHouseholdAsync();
        var other = await _scenario.NewHouseholdAsync();
        var otherAccount = await BudgetScenario.DefaultEnvelopeAsync(other.Owner);
        var outsider = await _scenario.SignedInAsync("Outsider");
        var mineAccount = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var member = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.WriteScope);
        await using var stranger = await _mcp.ConnectAsync(BudgetMcpHost.Subject(outsider), BudgetMcp.WriteScope);
        await BudgetMcpHost.CallExpectingErrorAsync(member, "create_expense", NewExpense(Guid.CreateVersion7(), otherAccount));
        await BudgetMcpHost.CallExpectingErrorAsync(stranger, "create_expense", NewExpense(Guid.CreateVersion7(), mineAccount));

        await using var reader = await _mcp.ConnectAsync(BudgetMcpHost.Subject(other.Owner), BudgetMcp.ReadScope);
        Parse<ExpenseListDto>(await BudgetMcpHost.CallAsync(reader, "search_expenses")).Items.ShouldBeEmpty();
    }

    /// <summary>A read-only token neither lists nor runs the write tools.</summary>
    [Fact]
    public async Task WriteTools_WithoutWriteScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync("create_expense", NewExpense(Guid.CreateVersion7(), account), cancellationToken: Ct).AsTask());

        tools.ShouldNotContain(t => t.Name == "create_expense" || t.Name == "update_expense" || t.Name == "void_expense");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }
}
