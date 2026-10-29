namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#671 — statement preview: candidates, skipped rows, already-imported and possible duplicates; writes nothing.</summary>
public sealed class BudgetStatementPreviewTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private static readonly byte[] Mbank = File.ReadAllBytes(Path.Join(AppContext.BaseDirectory, "Fixtures", "mbank.csv"));

    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetStatementPreviewTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<HttpResponseMessage> PreviewAsync(HttpClient client, Guid accountId, byte[] file)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(file), "file", "statement.csv");
        form.Add(new StringContent(accountId.ToString()), "budgetAccountId");
        return await client.PostAsync("/api/budget/imports/preview", form, Ct);
    }

    private static async Task<PreviewBody> PreviewOkAsync(HttpClient client, Guid accountId)
    {
        var response = await PreviewAsync(client, accountId, Mbank);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<PreviewBody>(Ct))!;
    }

    private static async Task<Guid> ExpenseAsync(HttpClient client, Guid accountId, string amount, string occurredOn, string? funding = "HouseholdFunds")
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", new
        {
            clientRequestId = Guid.NewGuid(),
            accountId,
            amount,
            occurredOn,
            category = "Other",
            fundingSource = funding,
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<IdBody>(Ct))!.ExpenseId;
    }

    private static async Task<Guid> PersonalEnvelopeAsync(HttpClient client)
        => (await (await client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct))!.Id;

    /// <summary>Simulates an earlier import of <paramref name="key"/> (#672 writes these; this slice only reads them).</summary>
    private async Task MarkImportedAsync(Guid expenseId, string scope, string key)
    {
        using var serviceScope = _factory.Services.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<BudgetDbContext>();
        await db.Database.ExecuteSqlAsync($"UPDATE expenses SET import_scope = {scope}, import_source_key = {key} WHERE id = {expenseId}", Ct);
    }

    /// <summary>Card payments are candidates, the card repayment is skipped, and the preview stores nothing.</summary>
    [Fact]
    public async Task Preview_MbankStatement_ClassifiesRowsAndWritesNothing()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        var preview = await PreviewOkAsync(h.Owner, account);

        preview.Profile.ShouldBe("mbank");
        preview.Candidates.Select(c => (c.LineNumber, c.OccurredOn, c.Amount, c.Status)).ShouldBe(
        [
            (39, new DateOnly(2026, 10, 18), "438.02", "New"),
            (40, new DateOnly(2026, 10, 20), "29.38", "New"),
            (42, new DateOnly(2026, 10, 23), "45.99", "New"),
        ]);
        preview.Candidates[0].Description.ShouldBe("APTEKA TESTOWA /BIALYSTOK");
        preview.Candidates.ShouldAllBe(c => c.SourceKey.Length == 64 && c.Matches.Count == 0);
        preview.Skipped.ShouldHaveSingleItem().ShouldBe(new SkippedBody(41, "-5636.78", "OwnTransfer"));
        preview.Issues.ShouldBeEmpty();

        (await PreviewOkAsync(h.Owner, account)).Candidates.Select(c => c.SourceKey).ShouldBe(preview.Candidates.Select(c => c.SourceKey));
        (await h.Owner.GetFromJsonAsync<PageBody>("/api/budget/expenses?includeVoided=true", Ct))!.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// A key imported earlier is <c>AlreadyImported</c> even after a void, but only in its own privacy
    /// scope: a row imported privately never shows in another adult's household preview (§4.2).
    /// </summary>
    [Fact]
    public async Task Preview_RowImportedEarlier_IsAlreadyImportedInItsScopeOnly()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var personal = await PersonalEnvelopeAsync(bea.Client);
        var keys = (await PreviewOkAsync(h.Owner, shared)).Candidates.Select(c => c.SourceKey).ToList();

        var sharedExpense = await ExpenseAsync(h.Owner, shared, "438.02", "2026-10-18");
        (await h.Owner.PostAsJsonAsync($"/api/budget/expenses/{sharedExpense}/void",
            new { clientRequestId = Guid.NewGuid(), expectedRevision = 1, reason = "test" }, Ct)).EnsureSuccessStatusCode();
        await MarkImportedAsync(sharedExpense, "household", keys[0]);
        await MarkImportedAsync(await ExpenseAsync(bea.Client, personal, "29.38", "2026-10-20", funding: null), bea.PersonId.ToString(), keys[1]);

        (await PreviewOkAsync(h.Owner, shared)).Candidates.Select(c => c.Status).ShouldBe(["AlreadyImported", "New", "New"]);
        (await PreviewOkAsync(bea.Client, personal)).Candidates.Select(c => c.Status).ShouldBe(["New", "AlreadyImported", "New"]);
    }

    /// <summary>
    /// A visible manual expense with the same amount within two days is a possible duplicate, whatever
    /// its category; three days away, or in another household, it is not.
    /// </summary>
    [Fact]
    public async Task Preview_ManualEntryNearPurchase_IsPossibleDuplicate()
    {
        var h = await _scenario.NewHouseholdAsync();
        var other = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var near = await ExpenseAsync(h.Owner, account, "29.38", "2026-10-22");
        await ExpenseAsync(h.Owner, account, "29.38", "2026-10-23");
        await ExpenseAsync(other.Owner, await BudgetScenario.DefaultEnvelopeAsync(other.Owner), "29.38", "2026-10-20");

        var candidate = (await PreviewOkAsync(h.Owner, account)).Candidates.Single(c => c.LineNumber == 40);

        candidate.Status.ShouldBe("PossibleDuplicate");
        candidate.Matches.ShouldHaveSingleItem().Id.ShouldBe(near);
    }

    /// <summary>A guest has no Budget access at all; another adult's personal envelope does not exist for the caller.</summary>
    [Fact]
    public async Task Preview_WithoutAccess_IsForbiddenOrNotFound()
    {
        var h = await _scenario.NewHouseholdAsync();
        var guest = await _scenario.MemberAsync(h, "Guest");
        var bea = await _scenario.MemberAsync(h, "Adult");
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        (await PreviewAsync(guest.Client, account, Mbank)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await PreviewAsync(h.Owner, await PersonalEnvelopeAsync(bea.Client), Mbank)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>An archived envelope takes no new expenses, so it cannot be previewed for import.</summary>
    [Fact]
    public async Task Preview_ArchivedEnvelope_IsUnprocessable()
    {
        var h = await _scenario.NewHouseholdAsync();
        var created = await (await h.Owner.PostAsJsonAsync("/api/budget/accounts", new { name = "Old", visibility = "Household" }, Ct))
            .Content.ReadFromJsonAsync<AccountBody>(Ct);
        (await h.Owner.PostAsJsonAsync($"/api/budget/accounts/{created!.Id}/archive", new { expectedRevision = created.Revision }, Ct)).EnsureSuccessStatusCode();

        (await PreviewAsync(h.Owner, created.Id, Mbank)).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>A file over the limit is a 400 that names the limit; an unknown format is a 422.</summary>
    [Fact]
    public async Task Preview_OversizedOrUnknownFile_IsRejected()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        var oversized = await PreviewAsync(h.Owner, account, new byte[2 * 1024 * 1024 + 1]);
        oversized.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await oversized.Content.ReadAsStringAsync(Ct)).ShouldContain("2 MiB");

        (await PreviewAsync(h.Owner, account, "Date,Amount\n2026-10-19,-1.00"u8.ToArray())).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record IdBody(Guid ExpenseId);
    private sealed record AccountBody(Guid Id, int Revision);
    private sealed record PageBody(int TotalCount);
    private sealed record MatchBody(Guid Id);
    private sealed record CandidateBody(int LineNumber, string SourceKey, DateOnly OccurredOn, string Amount, string Description, string Status, List<MatchBody> Matches);
    private sealed record SkippedBody(int LineNumber, string Amount, string Reason);
    private sealed record IssueBody(int LineNumber, string Code);
    private sealed record PreviewBody(string Profile, List<CandidateBody> Candidates, List<SkippedBody> Skipped, List<IssueBody> Issues);
}
