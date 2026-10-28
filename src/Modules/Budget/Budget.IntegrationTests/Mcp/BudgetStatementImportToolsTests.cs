namespace Budget.IntegrationTests.Mcp;

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Application.Commands.ImportExpenses;
using global::Budget.Application.Queries.ListExpenses;
using global::Budget.Application.Queries.PreviewStatementImport;
using global::Budget.Mcp;
using ModelContextProtocol;

/// <summary>#676 (tools half) — statement preview and import over MCP: same candidates as REST, confirmed rows only, replay, duplicates reported, scope enforced.</summary>
public sealed class BudgetStatementImportToolsTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private static readonly byte[] MbankBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "mbank.csv"));

    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;
    private readonly BudgetMcpHost _mcp;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetStatementImportToolsTests(BudgetDatabaseFixture fixture)
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

    // The same decoding the parser applies: strict UTF-8, else the Polish banks' Windows-1250.
    private static string StatementText()
    {
        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(MbankBytes);
        }
        catch (DecoderFallbackException)
        {
            return CodePagesEncodingProvider.Instance.GetEncoding(1250)!.GetString(MbankBytes);
        }
    }

    private static Dictionary<string, object?> Preview(Guid account) => new() { ["accountId"] = account, ["statement"] = StatementText() };

    private static Dictionary<string, object?> Import(Guid key, Guid account, params StatementCandidateDto[] rows) => new()
    {
        ["operationKey"] = key,
        ["accountId"] = account,
        ["rows"] = rows.Select(c => new Dictionary<string, object?>
        {
            ["sourceKey"] = c.SourceKey,
            ["amount"] = c.Amount,
            ["occurredOn"] = c.OccurredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["category"] = "Groceries",
            ["fundingSource"] = "HouseholdFunds",
            ["description"] = c.Description,
            ["merchantKey"] = c.MerchantKey,
        }).ToList(),
    };

    private static async Task<StatementPreviewDto> RestPreviewAsync(HttpClient client, Guid account)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(MbankBytes), "file", "statement.csv");
        form.Add(new StringContent(account.ToString()), "budgetAccountId");
        var response = await client.PostAsync("/api/budget/imports/preview", form, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StatementPreviewDto>(Ct))!;
    }

    /// <summary>The MCP preview returns the same candidates as REST for the same file and writes nothing.</summary>
    [Fact]
    public async Task Preview_MatchesRestAndWritesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var preview = Parse<StatementPreviewDto>(await BudgetMcpHost.CallAsync(client, "preview_expense_import", Preview(account)));
        var rest = await RestPreviewAsync(h.Owner, account);

        preview.Candidates.Select(c => (c.SourceKey, c.Amount, c.OccurredOn, c.Status)).ShouldBe(rest.Candidates.Select(c => (c.SourceKey, c.Amount, c.OccurredOn, c.Status)));
        preview.Candidates.ShouldNotBeEmpty();
        preview.Profile.ShouldBe(rest.Profile);
        (await h.Owner.GetFromJsonAsync<ExpenseListDto>("/api/budget/expenses?includeVoided=true", Ct))!.TotalCount.ShouldBe(0);
    }

    /// <summary>Importing the confirmed rows creates them once; a replay returns the originals; re-previewing reports them as already imported.</summary>
    [Fact]
    public async Task Execute_CreatesConfirmedRowsReplaysAndReportsDuplicates()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var key = Guid.CreateVersion7();

        await using var client = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var preview = Parse<StatementPreviewDto>(await BudgetMcpHost.CallAsync(client, "preview_expense_import", Preview(account)));
        var chosen = preview.Candidates.Take(2).ToArray();
        var created = Parse<ImportExpensesResult>(await BudgetMcpHost.CallAsync(client, "execute_expense_import", Import(key, account, chosen)));
        var replay = Parse<ImportExpensesResult>(await BudgetMcpHost.CallAsync(client, "execute_expense_import", Import(key, account, chosen)));
        var again = await BudgetMcpHost.CallExpectingErrorAsync(client, "execute_expense_import", Import(Guid.CreateVersion7(), account, chosen[0]));
        var after = Parse<StatementPreviewDto>(await BudgetMcpHost.CallAsync(client, "preview_expense_import", Preview(account)));

        created.Created.ShouldBeTrue();
        created.ExpenseIds.Count.ShouldBe(2);
        replay.Created.ShouldBeFalse();
        replay.ExpenseIds.ShouldBe(created.ExpenseIds);
        again.ShouldNotBeEmpty();
        after.Candidates.Where(c => chosen.Any(x => x.SourceKey == c.SourceKey)).ShouldAllBe(c => c.Status == "AlreadyImported");
        (await h.Owner.GetFromJsonAsync<ExpenseListDto>("/api/budget/expenses", Ct))!.TotalCount.ShouldBe(2);
    }

    /// <summary>A non-member and a wrong scope get errors, never a preview or a write.</summary>
    [Fact]
    public async Task Tools_ForNonMemberAndWithoutScope_AreRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var outsider = await _scenario.SignedInAsync("Outsider");

        await using var stranger = await _mcp.ConnectAsync(BudgetMcpHost.Subject(outsider), string.Join(' ', BudgetMcp.Scopes));
        await BudgetMcpHost.CallExpectingErrorAsync(stranger, "preview_expense_import", Preview(account));

        await using var reader = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), BudgetMcp.ReadScope);
        var tools = await reader.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => reader.CallToolAsync("execute_expense_import",
            new Dictionary<string, object?> { ["operationKey"] = Guid.CreateVersion7(), ["accountId"] = account, ["rows"] = Array.Empty<object>() },
            cancellationToken: Ct).AsTask());

        tools.ShouldContain(t => t.Name == "preview_expense_import");
        tools.ShouldNotContain(t => t.Name == "execute_expense_import");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }

    /// <summary>Bulk import needs the import grant on top of write; an empty operation key is refused by name.</summary>
    [Fact]
    public async Task Execute_NeedsImportGrantAndAKey()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var withoutImport = $"{BudgetMcp.ReadScope} {BudgetMcp.WriteScope}";

        await using var writer = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), withoutImport);
        var tools = await writer.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => writer.CallToolAsync("execute_expense_import",
            new Dictionary<string, object?> { ["operationKey"] = Guid.CreateVersion7(), ["accountId"] = account, ["rows"] = Array.Empty<object>() },
            cancellationToken: Ct).AsTask());

        await using var importer = await _mcp.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var empty = await BudgetMcpHost.CallExpectingErrorAsync(importer, "execute_expense_import",
            new() { ["operationKey"] = Guid.Empty, ["accountId"] = account, ["rows"] = Array.Empty<object>() });

        tools.ShouldContain(t => t.Name == "create_expense");
        tools.ShouldNotContain(t => t.Name == "execute_expense_import");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
        empty.ShouldContain("operationKey");
    }

    /// <summary>With imports switched off, the tool is hidden and rejected even for a token holding every grant.</summary>
    [Fact]
    public async Task Execute_WithImportsSwitchedOff_IsHiddenAndRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        using var off = new BudgetMcpHost(_factory, importsEnabled: false);

        await using var client = await off.ConnectAsync(BudgetMcpHost.Subject(h.Owner), string.Join(' ', BudgetMcp.Scopes));
        var tools = await client.ListToolsAsync(cancellationToken: Ct);
        var call = await Should.ThrowAsync<McpProtocolException>(() => client.CallToolAsync("execute_expense_import",
            new Dictionary<string, object?> { ["operationKey"] = Guid.CreateVersion7(), ["accountId"] = account, ["rows"] = Array.Empty<object>() },
            cancellationToken: Ct).AsTask());

        tools.ShouldContain(t => t.Name == "preview_expense_import");
        tools.ShouldNotContain(t => t.Name == "execute_expense_import");
        call.Message.ShouldContain("forbidden", Case.Insensitive);
    }
}
