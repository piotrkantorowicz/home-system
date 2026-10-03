namespace Budget.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Budget.IntegrationTests.Infrastructure;
using global::Budget.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>#487 — optional expense description across create, correct, detail, history and search.</summary>
public sealed class BudgetExpenseDescriptionTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;
    private readonly BudgetScenario _scenario;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public BudgetExpenseDescriptionTests(BudgetDatabaseFixture fixture)
    {
        _factory = new BudgetApiFactory(fixture);
        _scenario = new BudgetScenario(_factory);
    }

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static object CreateBody(Guid accountId, string? description, Guid? requestId = null, string? funding = "HouseholdFunds")
        => new
        {
            clientRequestId = requestId ?? Guid.NewGuid(),
            accountId,
            amount = "10.00",
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = funding,
            description,
        };

    private static object EditBody(int expected, string? description)
        => new
        {
            clientRequestId = Guid.NewGuid(),
            expectedRevision = expected,
            reason = "note",
            amount = "10.00",
            occurredOn = "2026-10-01",
            category = "Groceries",
            fundingSource = "HouseholdFunds",
            description,
        };

    private static async Task<Guid> CreatedAsync(HttpClient client, object body)
    {
        var response = await client.PostAsJsonAsync("/api/budget/expenses", body, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ResultBody>(Ct))!.ExpenseId;
    }

    private static async Task<ExpenseBody> GetAsync(HttpClient client, Guid id)
        => (await client.GetFromJsonAsync<ExpenseBody>($"/api/budget/expenses/{id}", Ct))!;

    private static async Task<PageBody> SearchAsync(HttpClient client, string search, int pageSize = 20)
        => (await client.GetFromJsonAsync<PageBody>($"/api/budget/expenses?search={Uri.EscapeDataString(search)}&pageSize={pageSize}", Ct))!;

    /// <summary>Omitted, blank, 80 and 81 characters; whitespace is collapsed on the way in.</summary>
    [Fact]
    public async Task Create_ValidatesAndNormalisesDescription()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);

        (await GetAsync(h.Owner, await CreatedAsync(h.Owner, CreateBody(account, null)))).Description.ShouldBeNull();
        (await GetAsync(h.Owner, await CreatedAsync(h.Owner, CreateBody(account, "   ")))).Description.ShouldBeNull();
        (await GetAsync(h.Owner, await CreatedAsync(h.Owner, CreateBody(account, "  Weekly   shop "))))
            .Description.ShouldBe("Weekly shop");
        (await GetAsync(h.Owner, await CreatedAsync(h.Owner, CreateBody(account, new string('x', 80))))).Description!.Length.ShouldBe(80);

        var tooLong = await h.Owner.PostAsJsonAsync("/api/budget/expenses", CreateBody(account, new string('x', 81)), Ct);
        tooLong.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>Correcting changes then clears the description; every revision snapshots its own value.</summary>
    [Fact]
    public async Task Correction_RecordsDescriptionInHistory()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var id = await CreatedAsync(h.Owner, CreateBody(account, "first"));

        (await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{id}", EditBody(1, "second"), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{id}", EditBody(2, null), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{id}", EditBody(2, "stale"), Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{id}", EditBody(3, new string('x', 81)), Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var detail = await GetAsync(h.Owner, id);
        detail.Description.ShouldBeNull();
        detail.History!.Select(r => r.Snapshot.Description).ShouldBe(["first", "second", null]);
    }

    /// <summary>The same request ID with a different description is a different request.</summary>
    [Fact]
    public async Task Replay_WithDifferentDescription_IsConflict()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var key = Guid.NewGuid();
        await CreatedAsync(h.Owner, CreateBody(account, "one", key));

        (await h.Owner.PostAsJsonAsync("/api/budget/expenses", CreateBody(account, "one", key), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await h.Owner.PostAsJsonAsync("/api/budget/expenses", CreateBody(account, "two", key), Ct)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    /// <summary>Search is case-insensitive, treats % and _ literally, counts before paging, and never sees hidden envelopes.</summary>
    [Fact]
    public async Task Search_FiltersBeforePaging_AndRespectsPrivacy()
    {
        var h = await _scenario.NewHouseholdAsync();
        var bea = await _scenario.MemberAsync(h, "Adult");
        var other = await _scenario.NewHouseholdAsync();
        var shared = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var personal = (await (await bea.Client.PostAsJsonAsync("/api/budget/accounts", new { name = "Mine", visibility = "Personal" }, Ct))
            .Content.ReadFromJsonAsync<IdBody>(Ct))!.Id;
        var otherAccount = await BudgetScenario.DefaultEnvelopeAsync(other.Owner);
        foreach (var d in new[] { "Lidl run", "LIDL again", "Bakery", "50% off_deal", null })
            await CreatedAsync(h.Owner, CreateBody(shared, d));
        await CreatedAsync(bea.Client, CreateBody(personal, "lidl secret", funding: null));
        await CreatedAsync(other.Owner, CreateBody(otherAccount, "lidl elsewhere"));

        var firstPage = await SearchAsync(h.Owner, "lidl", pageSize: 1);
        firstPage.TotalCount.ShouldBe(2);
        firstPage.Items.Count.ShouldBe(1);
        (await SearchAsync(h.Owner, "lidl")).Items.Select(i => i.Description).Order().ShouldBe(["LIDL again", "Lidl run"]);
        (await SearchAsync(bea.Client, "lidl")).TotalCount.ShouldBe(3);
        (await SearchAsync(h.Owner, "%")).Items.Select(i => i.Description).ShouldBe(["50% off_deal"]);
        (await SearchAsync(h.Owner, "_")).Items.Select(i => i.Description).ShouldBe(["50% off_deal"]);
        (await SearchAsync(h.Owner, "   ")).TotalCount.ShouldBe(5);
    }

    /// <summary>Revisions and rows written before descriptions existed read back with a null description.</summary>
    [Fact]
    public async Task LegacySnapshots_ReadWithNullDescription()
    {
        var h = await _scenario.NewHouseholdAsync();
        var account = await BudgetScenario.DefaultEnvelopeAsync(h.Owner);
        var id = await CreatedAsync(h.Owner, CreateBody(account, "will be stripped"));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
            var expenseId = id;
            await db.Database.ExecuteSqlAsync(
                $"UPDATE expense_revisions SET snapshot = snapshot::jsonb - 'Description', request = request::jsonb - 'Description' WHERE expense_id = {expenseId}", Ct);
            await db.Database.ExecuteSqlAsync($"UPDATE expenses SET description = NULL WHERE id = {expenseId}", Ct);
        }

        var detail = await GetAsync(h.Owner, id);

        detail.Description.ShouldBeNull();
        detail.History!.Single().Snapshot.Description.ShouldBeNull();
        (await h.Owner.PutAsJsonAsync($"/api/budget/expenses/{id}", EditBody(1, "now set"), Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed record IdBody(Guid Id);
    private sealed record ResultBody(Guid ExpenseId, int Revision);
    private sealed record SnapshotBody(string? Description);
    private sealed record HistoryBody(SnapshotBody Snapshot);
    private sealed record ExpenseBody(string? Description, List<HistoryBody>? History);
    private sealed record Row(string? Description);
    private sealed record PageBody(int TotalCount, List<Row> Items);
}
