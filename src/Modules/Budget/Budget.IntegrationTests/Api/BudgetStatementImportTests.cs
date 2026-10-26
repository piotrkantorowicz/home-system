namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;

/// <summary>#672 — importing previewed rows: all or none, replay, overlap and the import as revision source.</summary>
public sealed class BudgetStatementImportTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private static readonly byte[] Mbank = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "mbank.csv"));

    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetStatementImportTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<List<CandidateBody>> PreviewAsync(HttpClient client, Guid accountId)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Mbank), "file", "statement.csv");
        form.Add(new StringContent(accountId.ToString()), "budgetAccountId");
        var response = await client.PostAsync("/api/budget/imports/preview", form, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<PreviewBody>(Ct))!.Candidates;
    }

    private static object Row(CandidateBody c, string? funding = "HouseholdFunds", Guid[]? participants = null, Guid? payer = null)
        => new
        {
            c.SourceKey,
            c.Amount,
            c.OccurredOn,
            category = "Other",
            fundingSource = funding,
            paidByPersonId = payer,
            participantIds = participants,
            c.Description,
        };

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, Guid requestId, Guid accountId, params object[] rows)
        => client.PostAsJsonAsync("/api/budget/imports", new { clientRequestId = requestId, accountId, rows }, Ct);

    private static async Task<int> CountAsync(HttpClient client)
        => (await client.GetFromJsonAsync<PageBody>("/api/budget/expenses?includeVoided=true", Ct))!.TotalCount;

    /// <summary>Every row becomes an expense whose first revision is the import; the same statement then previews as already imported.</summary>
    [Fact]
    public async Task Import_PreviewedRows_CreatesExpensesFromTheImport()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var candidates = await PreviewAsync(h.Owner, account);

        var response = await ImportAsync(h.Owner, Guid.NewGuid(), account, [.. candidates.Select(c => Row(c))]);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        var result = (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!;
        result.ExpenseIds.Count.ShouldBe(3);
        var first = (await h.Owner.GetFromJsonAsync<ExpenseBody>($"/api/budget/expenses/{result.ExpenseIds[0]}", Ct))!;
        (first.Amount, first.OccurredOn, first.Description).ShouldBe(("438.02", new DateOnly(2026, 10, 18), "APTEKA TESTOWA /BIALYSTOK"));
        first.History.ShouldHaveSingleItem().Operation.ShouldBe("Import");
        (await PreviewAsync(h.Owner, account)).Select(c => c.Status).ShouldBe(["AlreadyImported", "AlreadyImported", "AlreadyImported"]);
    }

    /// <summary>A retry returns the original expenses and creates nothing; the same ID with other rows is a conflict.</summary>
    [Fact]
    public async Task Import_SameRequestAgain_ReplaysOrConflicts()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var candidates = await PreviewAsync(h.Owner, account);
        var requestId = Guid.NewGuid();
        var created = (await (await ImportAsync(h.Owner, requestId, account, Row(candidates[0]), Row(candidates[1])))
            .Content.ReadFromJsonAsync<ResultBody>(Ct))!;

        var replay = await ImportAsync(h.Owner, requestId, account, Row(candidates[0]), Row(candidates[1]));
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await replay.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseIds.ShouldBe(created.ExpenseIds);

        (await ImportAsync(h.Owner, requestId, account, Row(candidates[0]), Row(candidates[2]))).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ImportAsync(h.Owner, requestId, account, Row(candidates[0], funding: "Individual", participants: [h.OwnerPersonId], payer: h.OwnerPersonId), Row(candidates[1])))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountAsync(h.Owner)).ShouldBe(2);
    }

    /// <summary>One row the household rules reject fails the whole import, naming its 0-based index like shape errors, and nothing is written.</summary>
    [Fact]
    public async Task Import_OneInvalidRow_WritesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var candidates = await PreviewAsync(h.Owner, account);

        var response = await ImportAsync(h.Owner, Guid.NewGuid(), account,
            Row(candidates[0]), Row(candidates[1], funding: "Individual", participants: [Guid.NewGuid()], payer: h.OwnerPersonId));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Rows[1]: Participants must be current owners or adults.");
        (await CountAsync(h.Owner)).ShouldBe(0);
        (await ImportAsync(h.Owner, Guid.NewGuid(), account, Row(candidates[0]), Row(candidates[0]))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ImportAsync(h.Owner, Guid.NewGuid(), account, Row(candidates[0] with { SourceKey = "not-a-key" }))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>A row imported by another request since the preview fails the whole later import, so a re-preview is needed.</summary>
    [Fact]
    public async Task Import_RowImportedSincePreview_ConflictsAndWritesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var candidates = await PreviewAsync(h.Owner, account);
        (await ImportAsync(bea.Client, Guid.NewGuid(), account, Row(candidates[0]))).StatusCode.ShouldBe(HttpStatusCode.Created);

        var late = await ImportAsync(h.Owner, Guid.NewGuid(), account, Row(candidates[0]), Row(candidates[1]));

        late.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await late.Content.ReadAsStringAsync(Ct)).ShouldContain("Preview the statement again");
        (await CountAsync(h.Owner)).ShouldBe(1);
    }

    /// <summary>A personal envelope is paid by its owner without a payer in the request; a guest cannot import at all.</summary>
    [Fact]
    public async Task Import_PersonalEnvelopeOrGuest_FollowsExpenseAccess()
    {
        var h = await _scenario.NewHouseholdAsync();
        var guest = await _scenario.MemberAsync(h, "Guest");
        var personal = (await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct))!.Id;
        var candidates = await PreviewAsync(h.Owner, personal);

        (await ImportAsync(h.Owner, Guid.NewGuid(), personal, Row(candidates[0], funding: null))).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await ImportAsync(guest.Client, Guid.NewGuid(), personal, Row(candidates[1], funding: null))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record AccountBody(Guid Id);
    private sealed record PageBody(int TotalCount);
    private sealed record ResultBody(List<Guid> ExpenseIds, bool Created);
    private sealed record RevisionBody(string Operation);
    private sealed record ExpenseBody(string Amount, DateOnly OccurredOn, string? Description, List<RevisionBody> History);
    private sealed record CandidateBody(string SourceKey, DateOnly OccurredOn, string Amount, string Description, string Status);
    private sealed record PreviewBody(List<CandidateBody> Candidates);
}
