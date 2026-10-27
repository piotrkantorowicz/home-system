namespace Budget.IntegrationTests.Mcp;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Application.Queries.GetBudget;
using global::Budget.Application.Queries.GetBudgetContext;
using global::Budget.Application.Queries.GetExpense;
using global::Budget.Application.Queries.GetSummary;
using global::Budget.Application.Queries.ListExpenses;
using global::Budget.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using Shared.Infrastructure.Web;
using Shared.Testing.Mcp;

/// <summary>#660 — budget context, expense and summary read tools: same results as REST for the same caller, same visibility, scope enforced.</summary>
public sealed class BudgetReadToolsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetReadToolsTests(BudgetDatabaseFixture fixture)
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

    private static Task<string> RestAsync(HttpClient client, string path) => client.GetStringAsync(path, Ct);

    private static async Task<Guid> PersonalEnvelopeAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
    }

    private static async Task<Guid> SpendAsync(HttpClient client, Guid account, string amount, string funding, string description)
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId = account,
            amount,
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = funding,
            description,
        }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExpenseIdBody>(Ct))!.ExpenseId;
    }

    /// <summary>Expense search and one expense read the same over MCP as over REST; another adult's personal expense is absent.</summary>
    [Fact]
    public async Task ExpenseTools_MatchRestAndHideAnotherAdultsPersonalExpense()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var shared = await SpendAsync(h.Owner, await BudgetScenario.DefaultEnvelopeAsync(h.Owner), "42.50", "HouseholdFunds", "Market");
        var hidden = await SpendAsync(bea.Client, await PersonalEnvelopeAsync(bea.Client), "9.99", "Individual", "Secret");

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var list = await BudgetMcpHost.CallAsync(client, "search_expenses");
        var filtered = await BudgetMcpHost.CallAsync(client, "search_expenses", new() { ["search"] = "mark", ["from"] = "2026-10-01" });
        var one = await BudgetMcpHost.CallAsync(client, "get_expense", new() { ["expenseId"] = shared });

        BudgetMcpHost.Normalize<ExpenseListDto>(list)
            .ShouldBe(BudgetMcpHost.Normalize<ExpenseListDto>(await RestAsync(h.Owner, "/api/budget/expenses?pageSize=25")));
        BudgetMcpHost.Normalize<ExpenseListDto>(filtered)
            .ShouldBe(BudgetMcpHost.Normalize<ExpenseListDto>(await RestAsync(h.Owner, "/api/budget/expenses?pageSize=25&search=mark&from=2026-10-01")));
        BudgetMcpHost.Normalize<ExpenseDto>(one)
            .ShouldBe(BudgetMcpHost.Normalize<ExpenseDto>(await RestAsync(h.Owner, $"/api/budget/expenses/{shared}")));
        list.ShouldContain(shared.ToString());
        list.ShouldNotContain(hidden.ToString());
    }

    /// <summary>A guessed id of another adult's personal expense fails exactly like a missing id.</summary>
    [Fact]
    public async Task GetExpense_WithAnotherAdultsPersonalExpense_FailsLikeMissing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var hidden = await SpendAsync(bea.Client, await PersonalEnvelopeAsync(bea.Client), "9.99", "Individual", "Secret");
        var absent = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var expense = await BudgetMcpHost.CallExpectingErrorAsync(client, "get_expense", new() { ["expenseId"] = hidden });
        var missing = await BudgetMcpHost.CallExpectingErrorAsync(client, "get_expense", new() { ["expenseId"] = absent });

        expense.Replace(hidden.ToString(), "{id}", StringComparison.Ordinal)
            .ShouldBe(missing.Replace(absent.ToString(), "{id}", StringComparison.Ordinal));
    }

    /// <summary>The context lists the caller's envelopes, codes and adults; the summary reads the same as REST per scope.</summary>
    [Fact]
    public async Task ContextAndSummaryTools_MatchRest()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var envelope = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var mine = await PersonalEnvelopeAsync(h.Owner);
        var theirs = await PersonalEnvelopeAsync(bea.Client);
        await SpendAsync(h.Owner, envelope, "42.50", "HouseholdFunds", "Market");
        await SpendAsync(h.Owner, mine, "7.00", "Individual", "Coffee");

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var json = await BudgetMcpHost.CallAsync(client, "get_budget_context");
        var shared = await BudgetMcpHost.CallAsync(client, "get_budget_summary", new() { ["month"] = "2026-10" });
        var personal = await BudgetMcpHost.CallAsync(client, "get_budget_summary", new() { ["month"] = "2026-10", ["scope"] = "Personal" });
        var invalid = await BudgetMcpHost.CallExpectingErrorAsync(client, "get_budget_summary", new() { ["month"] = "October" });

        var context = JsonSerializer.Deserialize<BudgetContextDto>(json, JsonSerializerOptions.Web)!;
        BudgetMcpHost.Normalize<BudgetDto>(JsonSerializer.Serialize(context.Budget, JsonSerializerOptions.Web))
            .ShouldBe(BudgetMcpHost.Normalize<BudgetDto>(await RestAsync(h.Owner, "/api/budget")));
        context.Accounts.Select(a => a.Id).ShouldBe([envelope, mine], ignoreOrder: true);
        context.Accounts.ShouldNotContain(a => a.Id == theirs);
        context.Categories.ShouldContain("Groceries");
        context.FundingSources.ShouldBe(["Individual", "HouseholdFunds"]);
        context.Participants.Select(p => p.PersonId).ShouldBe([h.OwnerPersonId, bea.PersonId], ignoreOrder: true);

        BudgetMcpHost.Normalize<SummaryDto>(shared)
            .ShouldBe(BudgetMcpHost.Normalize<SummaryDto>(await RestAsync(h.Owner, "/api/budget/summary?month=2026-10&scope=Shared")));
        BudgetMcpHost.Normalize<SummaryDto>(personal)
            .ShouldBe(BudgetMcpHost.Normalize<SummaryDto>(await RestAsync(h.Owner, "/api/budget/summary?month=2026-10&scope=Personal")));
        shared.ShouldContain("42.50");
        personal.ShouldContain("7.00");
        invalid.ShouldContain("Month");
    }

    /// <summary>Someone outside any household gets an error from every read tool, never data.</summary>
    [Fact]
    public async Task ReadTools_ForNonMember_ReturnErrors()
    {
        var outsider = await _scenario.SignedInAsync("Outsider");

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(outsider), BudgetMcp.ReadScope);

        await BudgetMcpHost.CallExpectingErrorAsync(client, "get_budget_context");
        await BudgetMcpHost.CallExpectingErrorAsync(client, "search_expenses");
        await BudgetMcpHost.CallExpectingErrorAsync(client, "get_expense", new() { ["expenseId"] = Guid.CreateVersion7() });
        await BudgetMcpHost.CallExpectingErrorAsync(client, "get_budget_summary", new() { ["month"] = "2026-10" });
    }

    /// <summary>Expense search is paged: 1 to 100 per page, anything else is an actionable tool error.</summary>
    [Fact]
    public async Task SearchExpenses_WithPageSizeOverLimit_ReturnsToolError()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var tooBig = await BudgetMcpHost.CallExpectingErrorAsync(client, "search_expenses", new() { ["pageSize"] = 101 });
        var page = await BudgetMcpHost.CallAsync(client, "search_expenses");

        tooBig.ShouldContain("pageSize");
        JsonSerializer.Deserialize<ExpenseListDto>(page, JsonSerializerOptions.Web)!.PageSize.ShouldBe(25);
    }

    /// <summary>A token without the budget read scope neither lists nor runs the tools.</summary>
    [Fact]
    public async Task ReadTools_WithoutReadScope_AreHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), "mcp:diet:read");
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(
            () => client.CallToolAsync("get_budget_context", cancellationToken: Ct).AsTask());

        tools.ShouldNotContain(t => t.Name == "get_budget_context" || t.Name == "search_expenses");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>The assistant path runs the same tool in-process, without the MCP transport, with the same result.</summary>
    [Fact]
    public async Task GetBudgetContext_InProcess_MatchesMcp()
    {
        var h = await _scenario.NewHouseholdAsync();
        var subject = BudgetMcpHost.Subject(h.Owner);

        await using var client = await _mcp.ConnectAsync(subject, BudgetMcp.ReadScope);
        var overMcp = await BudgetMcpHost.CallAsync(client, "get_budget_context");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "test"));
        var inProcess = await _mcp.Services.GetRequiredService<McpToolInvoker>().InvokeAsync(user, "get_budget_context", null, Ct);

        inProcess.IsError.ShouldNotBe(true);
        BudgetMcpHost.Normalize<BudgetContextDto>(McpTestClient.Text(inProcess))
            .ShouldBe(BudgetMcpHost.Normalize<BudgetContextDto>(overMcp));
    }

    private sealed record IdBody(Guid Id);
    private sealed record ExpenseIdBody(Guid ExpenseId);
}
